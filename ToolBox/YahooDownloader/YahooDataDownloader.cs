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
using System.Globalization;
using System.Linq;
using System.Net;
using QuantConnect.Data;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Market;

namespace QuantConnect.ToolBox.YahooDownloader
{
    /// <summary>
    /// Yahoo Data Downloader class 
    /// </summary>
    public class YahooDataDownloader : IDataDownloader
    {



        //Initialize
        private const string events = "div|split";
        private const string history = "history";

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="symbol">Symbol for the data we're looking for.</param>
        /// <param name="resolution">Resolution of the data request</param>
        /// <param name="startUtc">Start time of the data in UTC</param>
        /// <param name="endUtc">End time of the data in UTC</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        {
            if (resolution != Resolution.Daily)
                throw new ArgumentException("The YahooDataDownloader can only download daily data.");

            if (symbol.ID.SecurityType != SecurityType.Equity)
                throw new NotSupportedException("SecurityType not available: " + symbol.ID.SecurityType);

            if (endUtc < startUtc)
                throw new ArgumentException("The end date must be greater or equal than the start date.");

            // Note: Yahoo syntax requires the month zero-based (0-11)

            var data = Historical.Get(symbol.Value, startUtc, endUtc, history);

            foreach (var item in data)
            {
                yield return new TradeBar(item.Date, symbol, item.Open, item.High, item.Low, item.Close, (long)item.Volume, TimeSpan.FromDays(1));
            }

            //todo:
            //var eventData = Historical.Get(symbol.Value, startUtc, endUtc, events);

        }

        /// <summary>
        /// Download Dividend and Split data from Yahoo
        /// </summary>
        /// <param name="symbol">Symbol of the data to download</param>
        /// <param name="startUtc">Get data after this time</param>
        /// <param name="endUtc">Get data before this time</param>
        /// <returns></returns>
        public Queue<BaseData> DownloadSplitAndDividendData(Symbol symbol, DateTime startUtc, DateTime endUtc)
        {
            var data = Historical.GetRaw(symbol.Value, startUtc, endUtc, events);

            var parsed = new List<BaseData>();

            bool isSplit = false;

            foreach (var item in data.Split('\n'))
            {
                if (item == "Date,Stock Splits")
                {
                    isSplit = true;
                    continue;
                }
                if (item == "")
                {
                    break;
                }

                string[] values = item.Split(',');

                if (isSplit)
                {
                    parsed.Add(new Split
                    {
                        Time = DateTime.ParseExact(values[0].Replace("-", String.Empty), DateFormat.EightCharacter, CultureInfo.InvariantCulture),
                        Value = ParseAmount(values[1])
                    });
                }
                else
                {
                    parsed.Add(new Dividend
                    {
                        Time = DateTime.ParseExact(values[0].Replace("-", String.Empty), DateFormat.EightCharacter, CultureInfo.InvariantCulture),
                        Value = Decimal.Parse(values[1])
                    });
                }
            }

            return new Queue<BaseData>(parsed.OrderByDescending(x => x.Time));
        }

        /// <summary>
        /// Put the split ratio into a decimal format
        /// </summary>
        /// <param name="splitFactor">Split ratio</param>
        /// <returns>Decimal representing the split ratio</returns>
        private decimal ParseAmount(string splitFactor)
        {
            var factors = splitFactor.Split('/');
            return Decimal.Parse(factors[0]) / Decimal.Parse(factors[1]);
        }

    }
}
