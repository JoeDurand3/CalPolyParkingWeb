﻿using ParkingProcessing.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ParkingProcessing.Services
{
    public class TimeseriesService
    {
        public static TimeseriesService Instance { get; } = new TimeseriesService();
        private ClientWebSocket _socket;

        private TimeseriesService() { }

        public async Task Initialize()
        {
            await OpenWebSocket();
        }

        public async Task<ClientWebSocket> OpenWebSocket()
        {
            _socket = new ClientWebSocket();
            _socket.Options.KeepAliveInterval = TimeSpan.FromDays(0.5);
            _socket.Options.SetRequestHeader(headerName: "predix-zone-id", headerValue: EnvironmentalService.PredixServices.PredixTimeSeries.First().Credentials.Ingest.ZoneHttpHeaderValue);
            _socket.Options.SetRequestHeader(headerName: "authorization", headerValue: "Bearer " + AuthenticationService.GetAuthToken());
            _socket.Options.SetRequestHeader(headerName: "Origin", headerValue: "https://" + EnvironmentalService.PredixApplication.ApplicationUris.First());
            
            PseudoLoggingService.Log("TimeseriesService", "Attempting websocket connection...");
            try
            {
                var uri = new Uri(uriString: EnvironmentalService.PredixServices.PredixTimeSeries[0].Credentials.Ingest.Uri, uriKind: UriKind.Absolute);
                await _socket.ConnectAsync(uri, cancellationToken: CancellationToken.None);
                PseudoLoggingService.Log("TimeseriesService", "Websocket status: " + _socket.State.ToString());

                return _socket;
            }
            catch (Exception e)
            {
                PseudoLoggingService.Log("TimeseriesService", e);
            }

            return null; 
        }

        /// <summary>
        /// Submits a timeseries ingestion payload.
        /// </summary>
        /// <param name="payload"></param>
        private async void IngestData(PredixTimeseriesIngestPayload payload)
        {
            try
            {
                var payloadJSON = JsonConvert.SerializeObject(payload);
                var payloadBytes = Encoding.ASCII.GetBytes(payloadJSON);
                var payloadArraySegment = new ArraySegment<byte>(payloadBytes);
                await _socket.SendAsync(payloadArraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                PseudoLoggingService.Log("Timeseries Service", "Payload sent!");
            }
            catch (Exception e)
            {
                PseudoLoggingService.Log("TimeseriesService", e);
            }
        }

        public void IngestData(List<PredixTimeseriesIngestPayload> payload)
        {
            foreach (PredixTimeseriesIngestPayload load in payload)
            {
                IngestData(load);
            }
        }
    }
}
