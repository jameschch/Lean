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
        Dictionary<string, Channel> _channelId = new Dictionary<string, Channel>();
        Task _checkConnectionTask = null;
        CancellationTokenSource _checkConnectionToken;
        DateTime _heartbeatCounter = DateTime.UtcNow;
        const int _heartBeatTimeout = 30;
        object _cashLock = new object();
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };
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
        string _baseCurrency = "usd";
        string _spotOrFuture = "spot";
        object _placeOrderLock = new object();
        int _responseTimeout = 30000;
        //todo: link to config
        private bool _isTradeTickerEnabled;
        IOKCoinWebsocketsFactory _websocketsFactory;

        enum OKCoinOrderStatus
        {
            Cancelled = -1,
            Unfilled = 0,
            PartiallyFilled  =1,
            FullyFilled = 2,
            CancelRequestInProcess = 4 
        }
        #endregion

        /// <summary>
        /// Create Brokerage instance
        /// </summary>
        public OKCoinWebsocketsBrokerage(string url, IWebSocket webSocket, IOKCoinWebsocketsFactory websocketsFactory, string baseCurrency, 
            string apiKey, string apiSecret, string spotOrFuture, bool isTradeTickerEnabled, ISecurityProvider securityProvider)
            : base(url, webSocket, apiKey, apiSecret, null, null, 1, securityProvider)
        {
            _spotOrFuture = spotOrFuture;
            _baseCurrency = baseCurrency;
            _websocketsFactory = websocketsFactory;
            _isTradeTickerEnabled = isTradeTickerEnabled;
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

                //ticker
                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "addChannel",
                    channel = string.Format("ok_sub_{0}{1}_{2}_ticker", _spotOrFuture, item.ToString().Substring(3, 3), item.ToString().Substring(0, 3))
                }));

                //trade fills
                var parameters = new Dictionary<string, string>
                {
                    {"api_key" , ApiKey},
                    {"sign" , ApiSecret},
                };

                var sign = BuildSign(parameters);
                parameters["sign"] = sign;
                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "addChannel",
                    channel = string.Format("ok_sub{0}{1}_trades", _spotOrFuture, item.ToString().Substring(3, 3)),
                    parameters = parameters
                }));

                //trade ticker
                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "addChannel",
                    channel = string.Format("ok_sub{0}{1}_{2}_trades", _spotOrFuture, item.ToString().Substring(3, 3), item.ToString().Substring(0, 3))
                }));

            }
        }

        /// <summary>
        /// Remove subscription from Websockets service
        /// </summary>
        /// <param name="job"></param>
        /// <param name="symbols"></param>
        public override void Unsubscribe(Packets.LiveNodePacket job, IEnumerable<Symbol> symbols)
        {

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
            try
            {
                this.Disconnect();
            }
            catch (Exception)
            {
            }
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
                    {"type" , MapOrderType(order.Type, order.Direction)},
                    {"price" , order.Type == OrderType.Market ?  (order.Quantity * ScaleFactor).ToString() : (((LimitOrder)order).LimitPrice * ScaleFactor).ToString()},
                    {"amount" , (order.Quantity * ScaleFactor).ToString()}
                };

            var sign = BuildSign(parameters);

            parameters["sign"] = sign;

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            IWebSocket orderWebSocket = _websocketsFactory.CreateInstance(Url);

            orderWebSocket.OnMessage += (sender, e) =>
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Data, settings)[0];
                if (raw.data.result == "true")
                {
                    order.BrokerId.Add((string)raw.data.order_id);
                    placed = true;
                    tcs.SetResult(true);
                }
            };

            CachedOrderIDs.AddOrUpdate(order.Id, order);
            orderWebSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "addChannel",
                channel = "ok" + _spotOrFuture + _baseCurrency + "_trade",
                parameters = parameters
            }));

            tcs.Task.Wait(_responseTimeout);
            orderWebSocket.Close();

            return placed;
        }

        private string MapOrderType(OrderType orderType, OrderDirection direction)
        {
            if (orderType == OrderType.Market)
            {
                if (direction == OrderDirection.Buy)
                {
                    return "buy_market";
                }
                else
                {
                    return "sell_market";
                }
            }
            else if (orderType == OrderType.Limit)
            {
                if (direction == OrderDirection.Buy)
                {
                    return "buy";
                }
                else
                {
                    return "sell";
                }
            }

            throw new NotSupportedException("OKCoin supports limit and market orders only.");
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
                WebSocket.Send("{'event':'ping'}");
                await Task.Delay(TimeSpan.FromSeconds(10), _checkConnectionToken.Token);
            }
        }

        protected override void Reconnect(bool wait = true)
        {
            if (wait)
            {
                this._checkConnectionTask.Wait(30000);
            }
            var subscribed = GetSubscribed();
            //try to clean up state
            try
            {
                WebSocket.Close();
            }
            catch (Exception)
            {
            }
            WebSocket.Connect();
            this.Subscribe(null, subscribed);
        }


        protected override IList<Symbol> GetSubscribed()
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
                Log.Trace("OKCoinWebsocketsBrokerage.CancelOrder(): Symbol: " + order.Symbol.Value + " Quantity: " + order.Quantity);

                foreach (var id in order.BrokerId)
                {
                    var parameters = new Dictionary<string, string>
                    {
                        {"api_key" , ApiKey},
                        {"sign" , ApiSecret},
                        {"symbol" , order.Symbol.Value.Substring(0, 3) + "_" + order.Symbol.Value.Substring(3, 3)},
                        {"order_id" , id.ToString()}
                    };
                    var sign = BuildSign(parameters);
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
                Log.Error("OKCoinWebsocketsBrokerage.CancelOrder(): OrderID: " + order.Id + " - " + err);
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
            var sign = BuildSign(parameters);
            parameters["sign"] = sign;

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            IWebSocket cashWebSocket = _websocketsFactory.CreateInstance(Url);

            cashWebSocket.OnMessage += (sender, e) =>
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Data, settings)[0];
                //todo: should include raw.data.info.funds.borrow?
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

            cashWebSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "addChannel",
                channel = "ok" + _spotOrFuture + _baseCurrency + "_userinfo",
                parameters = parameters
            }));

            tcs.Task.Wait(_responseTimeout);

            cashWebSocket.Close();

            return list;
        }

        private decimal GetConversionRate(string symbol)
        {
            if (symbol.ToLower() == "USD")
            {
                return 1m;
            }

            //todo: This may be needed if LEAN account currency must be USD
            string url;
            if (symbol == "CNY")
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

        /// <summary>
        /// Retreive holdings from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            var list = new List<Holding>();

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            IWebSocket holdingsWebSocket = _websocketsFactory.CreateInstance(Url);

            holdingsWebSocket.OnMessage += (sender, e) =>
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Data, settings)[0];

                foreach (var item in raw.data.orders)
                {
                    if (item.status == (int)OKCoinOrderStatus.FullyFilled || item.status == OKCoinOrderStatus.PartiallyFilled)
                    {
                        list.Add(new Holding
                        {
                            AveragePrice = (decimal)item.avg_price,
                            CurrencySymbol = ((string)item.symbol).Substring(0, 3).ToUpper(),
                            Quantity = (item.type == "buy_market" ? (decimal)item.amount : -(decimal)item.amount),
                            Symbol = Symbol.Create(((string)item.symbol).ToUpper().Replace("_", ""), SecurityType.Forex, Market.Bitfinex.ToString()),
                            Type = SecurityType.Forex,
                        });
                    }

                }

                tcs.SetResult(true);
            };

            GetOrders(holdingsWebSocket);

            tcs.Task.Wait(_responseTimeout);

            holdingsWebSocket.Close();

            return list;
        }

        /// <summary>
        /// Retreive orders from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var list = new List<Order>();

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            IWebSocket openOrdersWebSocket = _websocketsFactory.CreateInstance(Url);

            openOrdersWebSocket.OnMessage += (sender, e) =>
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Data, settings)[0];

                foreach (var item in raw.data.orders)
                {
                    if (item.status == (int)OKCoinOrderStatus.PartiallyFilled || item.status == (int)OKCoinOrderStatus.Unfilled)
                    {

                        string symbol = ((string)item.symbol).ToUpper().Replace("_", "");

                        list.Add(new Orders.MarketOrder
                        {
                            Price = (decimal)item.price,
                            BrokerId = new List<string> { (string)item.order_id },
                            Quantity = (item.type == "buy_market" ? (decimal)item.deal_amount : -(decimal)item.deal_amount),
                            Symbol = Symbol.Create(symbol, SecurityType.Forex, Market.Bitfinex.ToString()),
                            PriceCurrency = symbol,
                            Time = Time.UnixTimeStampToDateTime((double)item.create_date),
                            Status = MapOrderStatus((int)item.status)
                        });
                    }

                }

                tcs.SetResult(true);
            };

            GetOrders(openOrdersWebSocket);

            openOrdersWebSocket.Close();

            tcs.Task.Wait(_responseTimeout);

            return list;
        }

        private void GetOrders(IWebSocket openOrdersWebSocket)
        {
            var parameters = new Dictionary<string, string>
            {
                {"api_key" , ApiKey},
            };
            var sign = BuildSign(parameters);
            parameters["sign"] = sign;

            openOrdersWebSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "addChannel",
                channel = "ok" + _spotOrFuture + _baseCurrency + "_orderinfo",
                parameters = parameters
            }));
        }

        private OrderStatus MapOrderStatus(int status)
        {
            switch (status)
            {
                case -1:
                    return OrderStatus.Canceled;
                case 0:
                    return OrderStatus.Submitted;
                case 1:
                    return OrderStatus.PartiallyFilled;
                case 4:
                    return OrderStatus.Canceled;
            };

            return OrderStatus.None;
        }

        public string BuildSign(Dictionary<string, string> data)
        {
            var pairs = data.Keys.OrderBy(k => k).Select(k => k + "=" + data[k]);
            string joined = string.Join("&", pairs.ToArray());
            joined += "&secret_key=" + this.ApiSecret;

            return joined.ToMD5();
        }

    }
}
