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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using Ionic.Zlib;

namespace QuantConnect.ToolBox.FXCMDownloader
{
    /// <summary>
    /// FXCM Data Downloader class
    /// </summary>
    public class FXCMTickDownloader : IDataDownloader
    {

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end times(in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Only Tick is currently supported</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            var returnData = new List<BaseData>();

            DateTime windowStartTime = startUtc;
            DateTime windowEndTime = endUtc;
            var year = windowStartTime.Year;
            var weeks = Math.Max((int)startUtc.Subtract(new DateTime(year, 1, 1)).TotalDays / 7, 1);

            do
            {
                for (int i = weeks; i < 254; i++)
                {
                    Log.Trace(String.Format("Getting data for timeperiod from {0} to {1}..", i, year));
                    var requestURL = $"http://tickdata.fxcorporate.com/{symbol.Value}/{year}/{i}.csv.gz";
                    var request = (HttpWebRequest)WebRequest.Create(requestURL);
                    request.UserAgent = ".NET Framework Test Client";
                    var reader = GetWithRetry(request);
                    if (reader == null)
                    {
                        return returnData;
                    }
                    returnData.AddRange(ParseCandleData(symbol, reader));

                }
                year++;
                weeks = 1;
            }
            while (windowEndTime.Year >= year);

            return returnData;
        }

        /// <summary>
        /// Get request with retry on failure
        /// </summary>
        /// <param name="request">Web request to get.</param>
        /// <returns>web response as string</returns>
        StreamReader GetWithRetry(HttpWebRequest request)
        {
            string data = string.Empty;
            int retryCount = 0;
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var unzipped = new GZipStream(response.GetResponseStream(), CompressionMode.Decompress);
                var reader = new StreamReader(unzipped, Encoding.Unicode);

                //reader.ReadToEnd();
                return reader;

            }
            catch (WebException ex)
            {
                return null;

                ++retryCount;
                if (retryCount > 3)
                {
                    Log.Error("REQUEST FAILED: " + request.Address);
                    throw;
                }
                Log.Trace("WARNING: Web request failed with message " + ex.Message + "Retrying... " + retryCount + " times");
            }
        }

        /// <summary>
        /// Parse string response from web response
        /// </summary>
        /// <param name="symbol">Crypto security symbol.</param>
        /// <param name="granularity">Resolution in seconds.</param>
        /// <param name="data">Web response as string.</param>
        /// <returns>web response as string</returns>
        List<BaseData> ParseCandleData(Symbol symbol, StreamReader reader)
        {
            List<BaseData> returnData = new List<BaseData>();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line.Length < 2 || line.StartsWith("Date")) continue;

                var split = line.Split(',');
                if (split.Count() < 2)
                {
                    continue;
                }

                var bid = decimal.Parse(split.ElementAt(1), System.Globalization.NumberStyles.Any);
                var ask = decimal.Parse(split.ElementAt(2), System.Globalization.NumberStyles.Any);

                var tick = new Tick()
                {
                    Time = DateTime.Parse(split.ElementAt(0)),
                    Symbol = symbol,
                    Value = (bid + ask) / 2,
                    DataType = MarketDataType.Tick,
                    TickType = TickType.Quote,
                    BidPrice = bid,
                    AskPrice = ask
                };
                returnData.Add(tick);
                split = null;
            }

            return returnData.OrderBy(datapoint => datapoint.Time).ToList();
        }

    }
}
