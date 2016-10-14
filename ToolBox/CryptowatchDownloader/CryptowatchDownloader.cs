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
using System.Net;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using Newtonsoft.Json;
using System.Linq;
using System.IO.Compression;
using System.IO;
using Newtonsoft.Json.Linq;

namespace QuantConnect.ToolBox.CryptowatchDownloader
{
    /// <summary>
    /// Cryptoiq Data Downloader class 
    /// </summary>
    public class CryptowatchDownloader : IDataDownloader
    {
        private readonly string _exchange;

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoiqDownloader"/> class
        /// </summary>
        /// <param name="exchange">The bitcoin exchange</param>
        public CryptowatchDownloader(string exchange)
        {
            _exchange = exchange;
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Only Tick is currently supported</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            if (resolution != Resolution.Tick)
            {
                throw new ArgumentException("Only tick data is currently supported.");
            }
            const string url = "https://cryptowat.ch/{0}/{1}.json";

            using (var cl = new WebClient())
            {
                var request = string.Format(url, _exchange, symbol.Value);
                var responseStream = new GZipStream(cl.OpenRead(request), CompressionMode.Decompress);
                var reader = new StreamReader(responseStream);
                var data = reader.ReadToEnd();

                JObject raw = (JObject)JsonConvert.DeserializeObject(data);

                foreach (var array in raw.First)
                {
                    foreach (var item in array)
                    {
                        var split = item.Value<string>().Split(' ');

                        yield return new Tick
                        {
                            Time = QuantConnect.Time.UnixTimeStampToDateTime(double.Parse(split[0])),
                            Symbol = symbol,
                            AskPrice = decimal.Parse(split[2]),
                            BidPrice = decimal.Parse(split[3]),
                            Value = decimal.Parse(split[4]),
                            TickType = TickType.Quote,
                            Quantity = decimal.Parse(split[2])
                        };
                    }
                }
            }
        }

    }
}