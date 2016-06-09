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
using QuantConnect.Brokerages.Bitfinex;
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Brokerages.OKCoin
{

    /// <summary>
    /// OKCoin WebSockets integration
    /// </summary>
    public partial class OKCoinWebsocketsBrokerage : BitfinexWebsocketsBrokerage
    {

        #region Declarations
        List<Securities.Cash> _cash = new List<Securities.Cash>();
        Dictionary<int, Channel> _channelId = new Dictionary<int, Channel>();
        Task _checkConnectionTask = null;
        CancellationTokenSource _checkConnectionToken;
        DateTime _heartbeatCounter = DateTime.UtcNow;
        const int _heartBeatTimeout = 30;
        object _cashLock = new object();
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };
        protected decimal ScaleFactor = 1;
        protected List<Tick> Ticks = new List<Tick>();
        /// <summary>
        /// List of known orders
        /// </summary>
        public ConcurrentDictionary<int, Order> CachedOrderIDs = new ConcurrentDictionary<int, Order>();
        /// <summary>
        /// List of filled orders
        /// </summary>
        protected readonly FixedSizeHashQueue<int> FilledOrderIDs = new FixedSizeHashQueue<int>(10000);
        /// <summary>
        /// List of unknown orders
        /// </summary>
        protected readonly FixedSizeHashQueue<int> UnknownOrderIDs = new FixedSizeHashQueue<int>(1000);
        public ConcurrentDictionary<int, OKCoinFill> FillSplit { get; set; }
        //todo: support cny
        string _baseCurrency = "usd";
        string _spotOrFuture = "spot";
        IWebSocket _orderWebSocket;
        object _placeOrderLock = new object();
        int _placeOrderTimeout = 3000;
        #endregion

        /// <summary>
        /// Create Brokerage instance
        /// </summary>
        public OKCoinWebsocketsBrokerage(string url, IWebSocket webSocket, IWebSocket orderWebSocket, string baseCurrency, string apiKey, string apiSecret, string spotOrFuture,
            decimal scaleFactor, ISecurityProvider securityProvider)
            : base(url, webSocket, apiKey, apiSecret, null, null, scaleFactor, securityProvider)
        {
            _spotOrFuture = spotOrFuture;
            _orderWebSocket = orderWebSocket;
            _orderWebSocket.Initialize(url);
            _baseCurrency = baseCurrency;
        }

        /// <summary>
        /// Add subscription to Websockets service
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public override void Subscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            if (!this.IsConnected)
            {
                this.Connect();
            }

            foreach (var item in symbols)
            {
                string ticker = string.Format("ok_sub_{0}{1}_{2}_ticker", _spotOrFuture, item.ToString().Substring(3, 3), item.ToString().Substring(0, 3));

                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "addChannel",
                    channel = ticker,
                }));


                var parameters = new Dictionary<string, string>
                {
                    {"api_key" , ApiKey},
                    {"sign" , ApiSecret},
                };

                var sign = MD5Util.BuildSign(parameters, ApiSecret);

                parameters["sign"] = sign;

                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "addChannel",
                    channel = "ok_sub" + _spotOrFuture + _baseCurrency + "_trades",
                    parameters = parameters
                }));

                //todo: subscribe trades
                //_webSocket.Send(JsonConvert.SerializeObject(new
                //{
                //    @event = "subscribe",
                //    channel = "trades",
                //}));
            }
        }

        /// <summary>
        /// Remove subscription from Websockets service
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public override void Unsubscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            foreach (var item in symbols)
            {
                foreach (var channel in _channelId.Where(c => c.Value.Symbol == item.ToString()))
                {
                    Unsubscribe(channel.Key);
                }
            }
        }

        private void Unsubscribe(int id)
        {
            try
            {
                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "unsubscribe",
                    channelId = id,
                }));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error encountered whilst attempting unsubscribe.");
            }
        }

        /// <summary>
        /// Returns if wss is connected
        /// </summary>
        public override bool IsConnected
        {
            get { return WebSocket.IsAlive; }
        }

        /// <summary>
        /// Creates wss connection
        /// </summary>
        public override void Connect()
        {
            WebSocket.Connect();
            if (this._checkConnectionTask == null || this._checkConnectionTask.IsFaulted || this._checkConnectionTask.IsCanceled || this._checkConnectionTask.IsCompleted)
            {
                this._checkConnectionTask = Task.Run(() => CheckConnection());
                this._checkConnectionToken = new CancellationTokenSource();
            }
            WebSocket.OnMessage += OnMessage;
        }

        /// <summary>
        /// Logs out and closes connection
        /// </summary>
        public override void Disconnect()
        {
            _checkConnectionToken.Cancel();
            this.WebSocket.Close();
        }

        /// <summary>
        /// Ensures any wss connection or authentication is closed
        /// </summary>
        public void Dispose()
        {
            this.Disconnect();
        }


        /// <summary>
        /// Add OKCoin order and prepare for fill message
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool PlaceOrder(Order order)
        {
            bool placed = false;

            var parameters = new Dictionary<string, string>
                {
                    {"api_key" , ApiKey},
                    {"symbol" , order.Symbol.Value.Substring(0, 3) + "_" + order.Symbol.Value.Substring(3, 3)},
                    {"type" , order.Direction.ToString().ToLower()},
                    {"price" , (order.Price * ScaleFactor).ToString()},
                    {"amount" , (order.Quantity * ScaleFactor).ToString()}
                };

            var sign = MD5Util.BuildSign(parameters, ApiSecret);

            parameters["sign"] = sign;

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            _orderWebSocket.OnMessage += (sender, e) =>
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Data, settings)[0];
                if (raw.data.result == "true")
                {
                    order.BrokerId.Add((string)raw.data.order_id);
                    placed = true;
                    tcs.SetResult(true);
                }
            };

            //wait for order respose before sending another
            lock (_placeOrderLock)
            {
                _orderWebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "addChannel",
                    channel = "ok" + _spotOrFuture + _baseCurrency + "_trade",
                    parameters = parameters
                }));

                tcs.Task.Wait(_placeOrderTimeout);

                CachedOrderIDs.AddOrUpdate(order.Id, order);
            }

            return placed;
        }

        private async Task CheckConnection()
        {
            while (!_checkConnectionToken.Token.IsCancellationRequested)
            {
                if (!this.IsConnected || (DateTime.UtcNow - _heartbeatCounter).TotalSeconds > _heartBeatTimeout)
                {
                    Log.Trace("OKCoinWebsocketsBrokerage.CheckConnection(): Heartbeat timeout. Reconnecting");
                    Reconnect(false);
                }
                await Task.Delay(TimeSpan.FromSeconds(10), _checkConnectionToken.Token);
            }
        }

        private void Reconnect(bool wait = true)
        {
            if (wait)
            {
                this._checkConnectionTask.Wait(30);
            }
            var subscribed = GetSubscribed();
            //try to clean up state
            try
            {
                this.Unsubscribe();
                WebSocket.Close();
            }
            catch (Exception)
            {
            }
            WebSocket.Connect();
            this.Subscribe(null, subscribed);
        }

        private void Unsubscribe()
        {
            this.Unsubscribe(null, GetSubscribed());
        }

        private IList<Symbol> GetSubscribed()
        {
            IList<Symbol> list = new List<Symbol>();

            foreach (var item in _channelId)
            {
                list.Add(Symbol.Create(item.Value.Symbol, SecurityType.Forex, Market.OKCoin));
            }
            return list;
        }

        /// <summary>
        /// Cancel an existing order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool CancelOrder(Orders.Order order)
        {
            try
            {
                Log.Trace("BitfinexBrokerage.CancelOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

                foreach (var id in order.BrokerId)
                {
                    var parameters = new Dictionary<string, string>
                    {
                        {"api_key" , ApiKey},
                        {"sign" , ApiSecret},
                        {"symbol" , order.Symbol.Value.Substring(0, 3) + "_" + order.Symbol.Value.Substring(3, 3)},
                        {"order_id" , id.ToString()}
                    };
                    var sign = MD5Util.BuildSign(parameters, ApiSecret);
                    parameters["sign"] = sign;

                    WebSocket.Send(JsonConvert.SerializeObject(new
                    {
                        @event = "addChannel",
                        channel = "ok" + _spotOrFuture + _baseCurrency + "_cancel_order",
                        parameters = parameters
                    }));

                    return true;
                }
            }
            catch (Exception err)
            {
                Log.Error("CancelOrder(): OrderID: " + order.Id + " - " + err);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Update an existing order
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool UpdateOrder(Orders.Order order)
        {
            bool cancelled;
            foreach (string id in order.BrokerId)
            {
                cancelled = this.CancelOrder(order);
                if (!cancelled)
                {
                    return false;
                }

            }
            return this.PlaceOrder(order);
        }

        //todo: getcashbalance
        /// <summary>
        /// Get Cash Balances from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Securities.Cash> GetCashBalance()
        {
            var list = new List<Securities.Cash>();

            var parameters = new Dictionary<string, string>
                {
                    {"api_key" , ApiKey},
                };
            var sign = MD5Util.BuildSign(parameters, ApiSecret);
            parameters["sign"] = sign;

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            _orderWebSocket.OnMessage += (sender, e) =>
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Data, settings)[0];

                if (raw.data.info.funds.free.btc != 0)
                {
                    list.Add(new Cash("BTC", (decimal)raw.data.info.funds.free.btc, GetConversionRate("BTC" + _baseCurrency)));
                }
                if (raw.data.info.funds.free.usd != 0)
                {
                    list.Add(new Cash("USD", (decimal)raw.data.info.funds.free.usd, GetConversionRate("USD")));
                }
                if (raw.data.info.funds.free.ltc != 0)
                {
                    list.Add(new Cash("LTC", (decimal)raw.data.info.funds.free.ltc, GetConversionRate("LTC" + _baseCurrency)));
                }
                if (raw.data.info.funds.free.cny != 0)
                {
                    list.Add(new Cash("CNY", (decimal)raw.data.info.funds.free.cny, GetConversionRate("CNY")));
                }

                tcs.SetResult(true);
            };

            _orderWebSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "addChannel",
                channel = "ok" + _spotOrFuture + _baseCurrency + "_userinfo",
                parameters = parameters
            }));

            tcs.Task.Wait(_placeOrderTimeout);

            return list;
        }

        private decimal GetConversionRate(string symbol)
        {
            if (_baseCurrency == symbol.ToLower())
            {
                return 1m;
            }

            string url;
            if (symbol == "USD" || symbol == "CNY")
            {
                url = @"http://query.yahooapis.com/v1/public/yql?q=select%20Rate%20from%20yahoo.finance.xchange%20where%20pair%20in%20(%22" 
                    + symbol + _baseCurrency + "%22)&env=store://datatables.org/alltableswithkeys";
                return decimal.Parse(new System.Xml.XPath.XPathDocument(url).CreateNavigator().SelectSingleNode("//Rate").InnerXml);
            }
            else
            {
                url = string.Format("https://www.okcoin.{0}/api/v1/ticker.do?symbol={1}_{2}", _baseCurrency == "usd" ? "com" : "cn", symbol.Substring(0, 3), _baseCurrency);
                using (System.Net.WebClient rest = new System.Net.WebClient())
                {
                    var raw = JsonConvert.DeserializeObject<dynamic>(rest.DownloadString(url), settings);
                    return ((decimal)raw.ticker.high + (decimal)raw.ticker.low) / 2;
                }
            }
        }

        //todo:
        /// <summary>
        /// Retreive holdings from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            var list = new List<Holding>();
            return list;
        }

        /// <summary>
        /// Retreive orders from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {

            var list = new List<Order>();

            return list;
        }

    }
}
