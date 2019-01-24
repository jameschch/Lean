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
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using QuantConnect.Configuration;
using QuantConnect.Logging;
using System.Threading;
using QuantConnect.Util;

namespace QuantConnect.ToolBox.FXCMDownloader
{
    public static class FXCMTickDownloaderProgram
    {
        /// <summary>
        /// FXCM Downloader Toolbox Project For LEAN Algorithmic Trading Engine.
        /// </summary>
        public static void FXCMTickDownloader(IList<string> tickers, DateTime fromDate, DateTime toDate)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            try
            {
                // Load settings from config.json
                var dataDirectory = Config.Get("data-directory", "../../../Data");
                //todo: will download any exchange but always save as FXCM
                // Create an instance of the downloader
                const string market = Market.FXCM;
                var downloader = new FXCMTickDownloader();

                // Download the data
                var symbolObject = Symbol.Create(tickers.Single(), SecurityType.Forex, market);
                var data = downloader.Get(symbolObject, Resolution.Tick, fromDate, toDate);

                // Save the data

                var writer = new LeanDataWriter(Resolution.Tick, symbolObject, dataDirectory, TickType.Quote);
                var distinctData = data.GroupBy(i => i.Time, (key, group) => group.First()).ToArray();

                writer.Write(distinctData);

                Log.Trace("Finish data download. Press any key to continue..");

            }
            catch (Exception err)
            {
                Log.Error(err);
                Log.Trace(err.Message);
                Log.Trace(err.StackTrace);
            }
           // Console.ReadLine();
        }
    }
}
