﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Parkix.CurrentSensor.Entities.IeParking;
using Parkix.Process.Services;

namespace Parkix.Process
{
    /// <summary>
    /// The program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            PseudoLoggingService.Log("Application", "It's alive!");

            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

            Initialize();

            host.Run();
        }


        private static async void Initialize()
        {

            try
            {
                await AuthenticationService.Instance.Initialize();
                await TimeseriesIngestService.Instance.Initialize();
                await IeParkingIngestService.Instance.Initialize();
                await SensorLotDatabaseService.Instance.Initialize();
                await SensorConfigurationService.Instance.Initialize();
                PseudoLoggingService.Log("Application", "Initialization Completed. System Ready.");
            }
            catch (Exception e)
            {
                PseudoLoggingService.Log("Application", e);
            }
        }
    }
}