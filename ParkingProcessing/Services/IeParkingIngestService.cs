﻿using ParkingProcessing.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

using ParkingProcessing.Entities.IeParking;
using ParkingProcessing.Entities.Uaa;
using ParkingProcessing.Helpers;

namespace ParkingProcessing.Services
{
    /// <summary>
    /// Used to ingest data from GE's Intelligent Environment Parking simulators.
    /// </summary>
    public class IeParkingIngestService
    {
        /// <summary>
        /// The instance of the Intelligent Environment Parking ingest service interface.
        /// </summary>
        public static IeParkingIngestService Instance { get; } = new IeParkingIngestService();
        private ClientWebSocket _socket;
        private List<PredixIeParkingAsset> _availableAssets;

        private Task _streamTask;

        private IeParkingIngestService()
        {
        }

        /// <summary>
        /// Queries IE for available assets within the given coordinates.
        /// </summary>
        /// <param name="latitudeOne">The latitude one.</param>
        /// <param name="longitudeOne">The longitude one.</param>
        /// <param name="latitudeTwo">The latitude two.</param>
        /// <param name="longitudeTwo">The longitude two.</param>
        /// <returns></returns>
        public async Task<List<string>> FindAssets(double latitudeOne, double longitudeOne, double latitudeTwo, double longitudeTwo)
        {
            var request = EnvironmentalService.IeParkingService.Credentials.Url + "/v1/assets/search?";
            request += "bbox=" + latitudeOne + ":" + longitudeOne + ", " + latitudeTwo + ":" + longitudeTwo;

            List<Tuple<string, string>> headers = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("predix-zone-id", EnvironmentalService.IeParkingService.Credentials.Zone.HttpHeaderValue),
                new Tuple<string, string>("Authorization", "Bearer " + AuthenticationService.GetAuthToken())
            };
            var result = await ServiceHelpers.SendAync<PredixIeParkingAssetSearchResult>(HttpMethod.Get, service: request,
                request: "", methodName: "", headers: headers);

            _availableAssets = result.Embedded.Assets;

            List<string> ids = _availableAssets.Select((asset => asset.DeviceId)).ToList();
            return ids;
        }

        /// <summary>
        /// Opens an ingestion websocket to the specified deviceid's live event stream.
        /// </summary>
        /// <param name="deviceid">The deviceid.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task<ClientWebSocket> OpenConnection(string deviceid)
        {
            try
            {
                var websocketAddress = await GetLiveEventWebsocketAddress(deviceid, "PKIN,PKOUT");
                
                _socket = new ClientWebSocket();
                _socket.Options.KeepAliveInterval = TimeSpan.FromHours(1);
                _socket.Options.SetRequestHeader(headerName: "predix-zone-id", headerValue: EnvironmentalService.IeParkingService.Credentials.Zone.HttpHeaderValue);
                _socket.Options.SetRequestHeader(headerName: "authorization", headerValue: "Bearer " + AuthenticationService.GetAuthToken());
                _socket.Options.SetRequestHeader(headerName: "Origin", headerValue: "https://" + EnvironmentalService.ApplicationUri);

                PseudoLoggingService.Log("IeParkingIngestService", "Attempting websocket connection...");
                var uri = new Uri(uriString: websocketAddress, uriKind: UriKind.Absolute);
                await _socket.ConnectAsync(uri, cancellationToken: CancellationToken.None);
                PseudoLoggingService.Log("IeParkingIngestService", "Websocket status: " + _socket.State.ToString());

                if (_socket.State == WebSocketState.Open)
                {
                    _streamTask = IngestLoop();
                }

                return _socket;
            }
            catch (Exception e)
            {
                PseudoLoggingService.Log("IeParkingIngestService", e);
            }

            return null;
        }

        private ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);

        private async Task IngestLoop()
        {
            while (_socket.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(buffer: buffer, cancellationToken: CancellationToken.None);

                PseudoLoggingService.Log("IeParkingIngestService", "Payload recieved. Size is " + result.Count);
            }
        }

        /// <summary>
        /// Gets the live event websocket address for the given device's event stream.
        /// </summary>
        /// <param name="deviceid">The deviceid.</param>
        /// <param name="events">The event types to subscribe to.</param>
        /// <returns></returns>
        public async Task<string> GetLiveEventWebsocketAddress(string deviceid, string events)
        {
            //get the record for the specified device
            var asset = _availableAssets.First((ass => ass.DeviceId == deviceid));

            //build the CURL command
            var serviceAddress = asset.Links.Self.Href.Replace(oldValue: "http://", newValue: "https://") + "/live-events?event-types=" + events;

            //add required headers
            List<Tuple<string, string>> headers = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("predix-zone-id",
                EnvironmentalService.IeParkingService.Credentials.Zone.HttpHeaderValue),
                new Tuple<string, string>("Authorization",
                "Bearer " + AuthenticationService.GetAuthToken())
            };

            //send request
            var socketAddress = await ServiceHelpers.SendAync<PredixIeParkingAssetWebsocketResponse>(HttpMethod.Get,
                service: serviceAddress, headers: headers);

            return socketAddress.Url;
        }
    }
}
