/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
using System;
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Logging;

namespace QuantConnect.ToolBox.CryptowatchDownloader
{
    class Program
    {
        /// <summary>
        /// Cryptoiq Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: CryptowatchDownloader EXCHANGE SYMBOL");
                //Environment.Exit(1);
                //useful detault params
                args = new string[] { "bitfinex", "BFXUSD" };
            }

            try
            {

                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");

                // Create an instance of the downloader
                //const string market = Market.Bitfinex;
                var downloader = new CryptowatchDownloader(args[0]);

                // Download the data
                var symbolObject = Symbol.Create(args[1], SecurityType.Forex, args[0]);
                var data = downloader.Get(symbolObject, Resolution.Tick, DateTime.MinValue, DateTime.MaxValue);

                // Save the data
                var writer = new LeanDataWriter(Resolution.Tick, symbolObject, dataDirectory, TickType.Quote);
                writer.Write(data);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }
    }
}