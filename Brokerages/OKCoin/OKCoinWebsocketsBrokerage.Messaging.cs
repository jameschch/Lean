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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Brokerages.OKCoin
{
    public partial class OKCoinWebsocketsBrokerage
    {

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Data, settings);

                if (raw.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    raw = raw[0];

                    if (((string)raw.channel).EndsWith("ticker"))
                    {
                        //ticker
                        PopulateTicker(raw);
                        return;
                    }
                    else if (((string)raw.channel) == "ok_sub_spot" + _baseCurrency + "_trades")
                    {
                        //trade update
                        PopulateTrade(raw);
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(((string)raw.channel), @"ok_sub_spot(usd|cny)_\w{3}_trades"))
                    {
                        if (_isTradeTickerEnabled)
                        {
                            //trade ticker update
                            PopulateTradeTicker(raw);
                        }
                    }
                }
                else if (raw.@event = "pong")
                {
                    _heartbeatCounter = DateTime.UtcNow;
                }

                Log.Trace("OKCoinWebsocketsBrokerage.OnMessage(): " + e.Data);
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Format("Parsing wss message failed. Data: {0}", e.Data));
                throw;
            }
        }

        private void PopulateTicker(dynamic raw)
        {

            string pair = GetPair(raw);
            string channel = (string)raw.channel;
            this._channelId[channel] = new Channel { Name = channel, Symbol = pair };

            lock (Ticks)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = (decimal)raw.data.sell / ScaleFactor,
                    BidPrice = (decimal)raw.data.buy / ScaleFactor,
                    Time = DateTime.UtcNow,
                    Value = (((decimal)raw.data.sell + (decimal)raw.data.buy) / 2m) / ScaleFactor,
                    TickType = TickType.Quote,
                    Symbol = Symbol.Create(pair.ToUpper(), SecurityType.Forex, Market.OKCoin),
                    DataType = MarketDataType.Tick
                });
            }
        }

        private void PopulateTradeTicker(dynamic raw)
        {
            string pair = GetPair(raw);

            lock (Ticks)
            {
                foreach(var item in raw.data)
                {
                    Ticks.Add(new Tick
                    {
                        Time = DateTime.UtcNow,
                        Value = (decimal)item[1] / ScaleFactor,
                        TickType = TickType.Trade,
                        Symbol = Symbol.Create(pair.ToUpper(), SecurityType.Forex, Market.OKCoin),
                        DataType = MarketDataType.Tick,
                        Quantity = (int)(Math.Round((decimal)item[2] * ScaleFactor))
                    });
                }

            }

        }

        private void PopulateTrade(dynamic raw)
        {
            var msg = JsonConvert.DeserializeObject<TradeMessage>(raw.data.ToString(), settings);
            int brokerId = msg.Id;

            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(brokerId.ToString()));

            if (cached.Count() > 0 && cached.First().Value != null)
            {

                var split = this.FillSplit[cached.First().Key];
                bool added = split.Add(msg);
                if (!added)
                {
                    //ignore fill message duplicate
                    return;
                }

                var fill = new OrderEvent
                (
                    cached.First().Key, Symbol.Create(msg.Symbol.Replace("_", ""), SecurityType.Forex, Market.OKCoin), Time.UnixTimeStampToDateTime(msg.CreatedDate),
                    OrderStatus.PartiallyFilled, msg.TradeType.StartsWith("buy") ? OrderDirection.Buy : OrderDirection.Sell,
                    msg.AveragePrice / ScaleFactor, msg.CompletedTradeAmount,
                    0, "OKCoin Fill Event"
                );
                fill.FillPrice = msg.AveragePrice / ScaleFactor;

                if (split.IsCompleted())
                {
                    fill.Status = OrderStatus.Filled;
                    fill.FillQuantity = split.TotalQuantity() * ScaleFactor;
                    FilledOrderIDs.Add(cached.First().Key);

                    Order outOrder = cached.First().Value;
                    CachedOrderIDs.TryRemove(cached.First().Key, out outOrder);
                    FillSplit.TryRemove(split.OrderId, out split);
                }

                OnOrderEvent(fill);
            }
            else
            {
                UnknownOrderIDs.Add(brokerId);
            }
        }

        private string GetPair(dynamic raw)
        {
            string channel = (string)raw.channel;
            return channel.Substring(15, 3).ToUpper() + channel.Substring(11, 3).ToUpper();
        }

    }
}
