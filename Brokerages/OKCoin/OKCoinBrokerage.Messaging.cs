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
    public partial class OKCoinBrokerage
    {

        /// <summary>
        /// Wss message handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnMessage(object sender, MessageEventArgs e)
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
                    else if (((string)raw.channel) == "ok_sub_" + _spotOrFuture + _baseCurrency + "_trades" && raw.data != null)
                    {
                        //trade update
                        PopulateTrade(raw);
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(((string)raw.channel), @"ok_sub_(spot|future)(usd|cny)_\w{3}_trades"))
                    {
                        if (_isTradeTickerEnabled)
                        {
                            //trade ticker update
                            PopulateTradeTicker(raw);
                        }
                    }
                }
                else if (raw.@event == "pong")
                {
                    _heartbeatCounter = DateTime.UtcNow;
                    CheckUnknownForFills();
                    return;
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

            if (raw.data != null)
            {
                lock (Ticks)
                {
                    Ticks.Add(new Tick
                    {
                        AskPrice = (decimal)raw.data.sell,
                        BidPrice = (decimal)raw.data.buy,
                        Time = DateTime.UtcNow,
                        Value = (((decimal)raw.data.sell + (decimal)raw.data.buy) / 2m),
                        TickType = TickType.Quote,
                        Symbol = Symbol.Create(pair.ToUpper(), SecurityType.Forex, Market.OKCoin),
                        DataType = MarketDataType.Tick
                    });

                }            }
        }

        private void PopulateTradeTicker(dynamic raw)
        {
            string pair = GetPair(raw);

            lock (Ticks)
            {
                foreach (var item in raw.data)
                {
                    Ticks.Add(new Tick
                    {
                        Time = DateTime.UtcNow,
                        Value = (decimal)item[1],
                        TickType = TickType.Trade,
                        Symbol = Symbol.Create(pair.ToUpper(), SecurityType.Forex, Market.OKCoin),
                        DataType = MarketDataType.Tick,
                        Quantity = (int)(Math.Round((decimal)item[2]))
                    });
                }

            }

        }

        private void PopulateTrade(dynamic raw)
        {
            var msg = JsonConvert.DeserializeObject<TradeMessage>(raw.data.ToString(), settings);
            PopulateTrade(msg);
        }

        private void PopulateTrade(TradeMessage msg)
        {
            int brokerId = msg.Id;

            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(brokerId.ToString()));

            if (cached.Count() > 0 && cached.First().Value != null && this.OKCoinFillSplit.ContainsKey(cached.First().Key))
            {
                var split = this.OKCoinFillSplit[cached.First().Key];
                split.Add(msg);

                var fill = new OrderEvent
                (
                    cached.First().Key, Symbol.Create(msg.Symbol.Replace("_", ""), SecurityType.Forex, Market.OKCoin), DateTime.UtcNow,
                    msg.CompletedTradeAmount != 0 ? OrderStatus.PartiallyFilled : OrderStatus.Submitted, msg.TradeType.StartsWith("buy") ? OrderDirection.Buy : OrderDirection.Sell,
                    msg.AveragePrice, msg.CompletedTradeAmount,
                    0, "OKCoin Fill Event"
                );
                fill.FillPrice = msg.AveragePrice;

                if (split.IsCompleted())
                {
                    Order outOrder = cached.First().Value;
                    fill.Status = OrderStatus.Filled;
                    //todo:check values of tradeprice
                    fill.FillQuantity = split.TotalQuantity();
                    FilledOrderIDs.Add(cached.First().Key);

                    //CachedOrderIDs.TryRemove(cached.First().Key, out outOrder);
                    OKCoinFillSplit.TryRemove(split.OrderId, out split);
                }

                if (UnknownOrders.ContainsKey(msg.Id.ToString()))
                {
                    UnknownOrders.TryRemove(msg.Id.ToString(), out msg);
                }

                OnOrderEvent(fill);
            }
            else if (!UnknownOrders.ContainsKey(msg.Id.ToString()))
            {
                UnknownOrders.AddOrUpdate(msg.Id.ToString(), msg);
            }
        }

        private string GetPair(dynamic raw)
        {
            string channel = (string)raw.channel;
            return channel.Substring(15, 3).ToUpper() + channel.Substring(11, 3).ToUpper();
        }

        private void CheckUnknownForFills()
        {
            if (UnknownOrders.Count() > 10)
            {
                UnknownOrders = new System.Collections.Concurrent.ConcurrentDictionary<string, TradeMessage>(UnknownOrders.Take(5));
            }
            foreach (var item in UnknownOrders)
            {
                PopulateTrade(item.Value);
            }
        }

    }
}
