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

namespace QuantConnect.Brokerages.Bitfinex
{
    public partial class BitfinexWebsocketsBrokerage
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
                    int id = raw[0];
                    string term = raw[1].Type == Newtonsoft.Json.Linq.JTokenType.String ? raw[1] : "";

                    if (term == "hb")
                    {
                        //heartbeat
                        _heartbeatCounter = DateTime.UtcNow;
                        return;
                    }
                    else if (_channelId.ContainsKey(id) && _channelId[id].Name == "ticker")
                    {
                        //ticker
                        PopulateTicker(e.Data, _channelId[id].Symbol);
                        return;
                    }
                    else if (_channelId.ContainsKey(id) && _channelId[id].Name == "trades" && (term == "te" || term == "tu"))
                    {
                        if (term == "tu")
                        {
                            return;
                        }
                        //todo: optional trade ticker
                        //PopulateTradeTicker(e.Data, _channelId[id].Symbol);
                        return;
                    }
                    else if (id == 0 && (term == "tu" || term == "te"))
                    {
                        //trade update
                        var data = raw[2].ToObject(typeof(string[]));
                        PopulateTrade(term, data);
                    }
                    else if (term == "ws")
                    {
                        //wallet
                        var data = raw[2].ToObject(typeof(string[][]));
                        PopulateWallet(data);
                    }
                }
                else if ((raw.channel == "ticker" || raw.channel == "trades") && raw.@event == "subscribed")
                {
                    string channel = (string)raw.channel;
                    int chanId = (int)raw.chanId;
                    string pair = (string)raw.pair;

                    var removing = this._channelId.Where(c => c.Value.Name == channel && c.Value.Symbol == pair).Select(c => c.Key).ToArray();

                    foreach (var item in removing)
                    {
                        if (item != chanId)
                        {
                            this._channelId.Remove(item);
                        }
                    }
                    this._channelId[chanId] = new Channel { Name = channel, Symbol = raw.pair };
                }
                else if (raw.chanId == 0)
                {
                    if (raw.status == "FAIL")
                    {
                        throw new Exception("Failed to authenticate with ws gateway");
                    }
                    Log.Trace("BitfinexWebsocketsBrokerage.OnMessage(): Successful wss auth");
                }
                else if (raw.@event == "info" && (raw.code == "20051" || raw.code == "20061"))
                {
                    //hard reset
                    if (!_isReconnecting)
                    {
                        try
                        {
                            _isReconnecting = true;
                            this.Reconnect();
                        }
                        finally
                        {
                            _isReconnecting = false;
                        }
                    }
                }

                Log.Trace("BitfinexWebsocketsBrokerage.OnMessage(): " + e.Data);
            }
            catch (Exception ex)
            {
                Log.Error(ex, string.Format("Parsing wss message failed. Data: {0}", e.Data));
                throw;
            }
        }

        private void PopulateTicker(string response, string symbol)
        {

            var data = JsonConvert.DeserializeObject<string[]>(response, settings);
            var msg = new TickerMessage(data);
            lock (Ticks)
            {
                Ticks.Add(new Tick
                {
                    AskPrice = msg.Ask / ScaleFactor,
                    BidPrice = msg.Bid / ScaleFactor,
                    AskSize = (long)Math.Round(msg.AskSize * ScaleFactor, 0),
                    BidSize = (long)Math.Round(msg.BidSize * ScaleFactor, 0),
                    Time = DateTime.UtcNow,
                    Value = ((msg.Ask + msg.Bid) / 2m) / ScaleFactor,
                    TickType = TickType.Quote,
                    Symbol = Symbol.Create(symbol.ToUpper(), SecurityType.Forex, Market.Bitfinex),
                    DataType = MarketDataType.Tick
                });
            }
        }

        private void PopulateTradeTicker(string response, string symbol)
        {

            var data = JsonConvert.DeserializeObject<string[]>(response, settings);

            var msg = new TradeTickerMessage(data);
            lock (Ticks)
            {
                Ticks.Add(new Tick
                {
                    Time = DateTime.UtcNow,
                    Value = msg.Price / ScaleFactor,
                    TickType = TickType.Trade,
                    Symbol = Symbol.Create(symbol.ToUpper(), SecurityType.Forex, Market.Bitfinex),
                    DataType = MarketDataType.Tick,
                    Quantity = (int)(Math.Round(msg.Amount * ScaleFactor))
                });
            }


        }

        private void PopulateWallet(string[][] data)
        {
            foreach (var item in data)
            {
                var msg = new WalletMessage(item);
                if (msg.WLT_NAME == this.Wallet)
                {
                    this.OnAccountChanged(new Securities.AccountEvent(msg.WLT_CURRENCY.ToUpper(), msg.WLT_BALANCE * ScaleFactor));
                }
            }
        }

        private void PopulateTrade(string term, string[] data)
        {
            var msg = new TradeMessage(term, data);
            int brokerId = msg.TrdOrdId;

            var cached = CachedOrderIDs.Where(o => o.Value.BrokerId.Contains(brokerId.ToString()));

            if (cached.Count() > 0 && cached.First().Value != null)
            {
                if (msg.FeeCurrency == "BTC")
                {
                    msg.Fee = msg.Fee * msg.TrdPriceExecuted;
                    msg.FeeCurrency = "USD";
                }
                var split = this.FillSplit[cached.First().Key];
                bool added = split.Add(msg);
                if (!added)
                {
                    //ignore fill message duplicate
                    Log.Trace("BitfinexWebsocketsBrokerage.TradeMessage:" + "Fill message duplicate:" + string.Join(",", data));
                    return;
                }

                var fill = new OrderEvent
                (
                    cached.First().Key, Symbol.Create(msg.TrdPair, SecurityType.Forex, Market.Bitfinex), msg.TrdTimestamp, OrderStatus.PartiallyFilled,
                    msg.TrdAmountExecuted > 0 ? OrderDirection.Buy : OrderDirection.Sell,
                    msg.TrdPriceExecuted / ScaleFactor, 0,
                    0, "Bitfinex Fill Event"
                );
                fill.FillPrice = msg.TrdPriceExecuted / ScaleFactor;

                if (split.IsCompleted())
                {
                    fill.Status = OrderStatus.Filled;
                    fill.OrderFee = Math.Abs(split.TotalFee());
                    fill.FillQuantity = split.TotalQuantity * ScaleFactor;
                    FilledOrderIDs.Add(cached.First().Key);

                    Order outOrder = cached.First().Value;
                    CachedOrderIDs.TryRemove(cached.First().Key, out outOrder);
                    //FillSplit.TryRemove(split.OrderId, out split);
                }

                OnOrderEvent(fill);
            }
            else
            {
                UnknownOrderIDs.Add(brokerId);
            }
        }


        /// <summary>
        /// Authenticate with wss
        /// </summary>
        protected override void Authenticate()
        {
            //prevent attempting auth more than every 10 seconds
            if (DateTime.Now > _previousAuthentication.AddSeconds(10))
            {
                string key = ApiKey;
                string payload = "AUTH" + DateTime.UtcNow.Ticks.ToString();
                _webSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "auth",
                    apiKey = key,
                    authSig = GetHexHashSignature(payload, ApiSecret),
                    authPayload = payload
                }));
                _previousAuthentication = DateTime.Now;
            }
        }

        private void UnAuthenticate()
        {
            _webSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "unauth"
            }));
            _webSocket.Close();
        }

        public void OnError(object sender, ErrorEventArgs e)
        {
            this.Reconnect();
        }

    }
}
