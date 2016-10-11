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
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace QuantConnect.Brokerages.OKCoin
{

    //todo: try catch finally on tasks
    /// <summary>
    /// OKCoin WebSockets integration
    /// </summary>
    public partial class OKCoinBrokerage : BitfinexWebsocketsBrokerage
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
        protected ConcurrentDictionary<string, TradeMessage> UnknownOrders = new ConcurrentDictionary<string, TradeMessage>();
        public ConcurrentDictionary<int, OKCoinFill> FillSplit { get; set; }
        string _baseCurrency = "usd";
        string _spotOrFuture = "spot";
        object _placeOrderLock = new object();
        int _responseTimeout = 30000;
        //todo: link to config
        private bool _isTradeTickerEnabled;
        IOKCoinWebsocketsFactory _websocketsFactory;
        IRestClient _rest;
        IOKCoinRestFactory _restFactory;

        enum OKCoinOrderStatus
        {
            Cancelled = -1,
            Unfilled = 0,
            PartiallyFilled = 1,
            FullyFilled = 2,
            CancelRequestInProcess = 4
        }
        #endregion

        /// <summary>
        /// Create Brokerage instance
        /// </summary>
        public OKCoinBrokerage(string url, IWebSocket webSocket, IOKCoinWebsocketsFactory websocketsFactory, IRestClient rest, IOKCoinRestFactory restFactory,
            string baseCurrency, string apiKey, string apiSecret, string spotOrFuture, bool isTradeTickerEnabled, ISecurityProvider securityProvider)
            : base(url, webSocket, apiKey, apiSecret, null, null, 1, securityProvider)
        {
            _spotOrFuture = spotOrFuture;
            _baseCurrency = baseCurrency.ToLower();
            _websocketsFactory = websocketsFactory;
            _isTradeTickerEnabled = isTradeTickerEnabled;
            FillSplit = new ConcurrentDictionary<int, OKCoinFill>();
            _rest = rest;
            _restFactory = restFactory;
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

                string lowered = item.ToString().ToLower();
                string reversed = lowered.Substring(3, 3) + "_" + lowered.Substring(0, 3);

                //ticker
                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "addChannel",
                    channel = string.Format("ok_sub_{0}{1}_ticker", _spotOrFuture, reversed)
                }));

                //trade ticker
                if (_isTradeTickerEnabled)
                {
                    WebSocket.Send(JsonConvert.SerializeObject(new
                    {
                        @event = "addChannel",
                        channel = string.Format("ok_sub_{0}{1}_trades", _spotOrFuture, reversed)
                    }));
                }

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

            var parameters = new Dictionary<string, string>
            {
                {"api_key" , ApiKey},
            };
            var sign = BuildSign(parameters);
            parameters["sign"] = sign;

            WebSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "addChannel",
                channel = "ok_sub_" + _spotOrFuture + _baseCurrency + "_trades",
                parameters = parameters
            }));

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
                    {"symbol" , ConvertSymbol(order.Symbol) },
                    {"type" , MapOrderType(order.Type, order.Direction)},
                    {"amount" , (Math.Abs(order.Quantity)).ToString()}
                };

            if (order.Type == OrderType.Limit)
            {
                parameters["price"] = ((LimitOrder)order).LimitPrice.ToString();
            }

            if (order.Type == OrderType.Market && order.Direction == OrderDirection.Buy)
            {
                FixBrokenMarketBuyOrder(order, parameters);
            }

            var sign = BuildSign(parameters);
            parameters["sign"] = sign;

            CachedOrderIDs.AddOrUpdate(order.Id, order);

            string response = com.okcoin.rest.HttpUtilManager.getInstance().requestHttpPost("https://www.okcoin.com/api/v1/", "trade.do", parameters);

            var raw = JsonConvert.DeserializeObject<dynamic>(response, settings);
            if (raw != null && raw.result == "true")
            {
                order.BrokerId.Add((string)raw.order_id);
                this.FillSplit.TryAdd(order.Id, new OKCoinFill(order, ScaleFactor));
                placed = true;
            }
            Log.Trace("BitfinexBrokerage.PlaceOrder(): Order response:" + raw.ToString());

            if (placed)
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Submitted });
                Log.Trace("BitfinexBrokerage.PlaceOrder(): Order completed successfully orderid:" + order.Id.ToString());
            }
            else
            {
                OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0, "Bitfinex Order Event") { Status = OrderStatus.Invalid });
                Log.Trace("BitfinexBrokerage.PlaceOrder(): Failed to place order orderid:" + order.Id.ToString());
            }

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
                        {"api_key", ApiKey},
                        {"sign", ""},
                        {"symbol", ConvertSymbol(order.Symbol)},
                        {"order_id" , id.ToString()}
                    };
                    var sign = BuildSign(parameters);
                    parameters["sign"] = sign;

                    WebSocket.Send(JsonConvert.SerializeObject(new
                    {
                        @event = "addChannel",
                        channel = "ok_" + _spotOrFuture + _baseCurrency + "_cancel_order",
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
            cashWebSocket.Connect();

            cashWebSocket.OnMessage += (sender, e) =>
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(e.Data, settings)[0];
                //todo: should include raw.data.info.funds.borrow?
                if (raw.data.info.funds.free.btc != 0)
                {
                    list.Add(new Cash("BTC", (decimal)raw.data.info.funds.free.btc, GetConversionRate("BTC" + _baseCurrency)));
                }
                if (raw.data.info.funds.free.usd != null && raw.data.info.funds.free.usd != 0)
                {
                    list.Add(new Cash("USD", (decimal)raw.data.info.funds.free.usd, GetConversionRate("USD")));
                }
                if (raw.data.info.funds.free.ltc != 0)
                {
                    list.Add(new Cash("LTC", (decimal)raw.data.info.funds.free.ltc, GetConversionRate("LTC" + _baseCurrency)));
                }
                if (raw.data.info.funds.free.cny != null && raw.data.info.funds.free.cny != 0)
                {
                    list.Add(new Cash("CNY", (decimal)raw.data.info.funds.free.cny, GetConversionRate("CNY")));
                }

                tcs.SetResult(true);
            };

            cashWebSocket.Send(JsonConvert.SerializeObject(new
            {
                @event = "addChannel",
                channel = "ok_" + _spotOrFuture + _baseCurrency + "_userinfo",
                parameters = parameters
            }));

            tcs.Task.Wait(_responseTimeout);

            cashWebSocket.Close();

            return list;
        }

        //todo: inject xml client
        //todo: inject rest client
        private decimal GetConversionRate(string symbol)
        {
            if (symbol.Equals("USD", StringComparison.CurrentCultureIgnoreCase))
            {
                return 1m;
            }

            //todo: This may be needed if LEAN account currency must be USD
            if (symbol.Equals("CNY", StringComparison.CurrentCultureIgnoreCase))
            {
                const string rateUrl = "https://query.yahooapis.com/v1/public/yql?q=select%20Rate%20from%20yahoo.finance.xchange%20where%20pair%20in%20(%22USDCNY%22)&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys";
                var _rateRest = _restFactory.CreateInstance(rateUrl);
                var response = _rateRest.Execute(new RestRequest());
                var raw = JsonConvert.DeserializeObject<dynamic>(response.Content);

                return ((decimal)raw.query.results.rate.Rate) * 0.01m;
            }
            else
            {
                string url = string.Format("ticker.do?symbol={0}_{1}", symbol.Substring(0, 3), _baseCurrency);
                var response = _rest.Execute(new RestRequest(url, Method.GET));
                var raw = JsonConvert.DeserializeObject<dynamic>(response.Content, settings);
                return ((decimal)raw.ticker.buy + (decimal)raw.ticker.sell) / 2;

            }
        }


        //todo: get conv rate
        /// <summary>
        /// Retreive holdings from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Holding> GetAccountHoldings()
        {
            var list = new List<Holding>();
            //foreach (string symbol in new[] { "btsd_usd", "ltc_usd" })
            //{
            //    var raw = GetOrders(symbol, OKCoinOrderStatus.PartiallyFilled);

            //    if (raw.data != null && raw.data.orders != null)
            //    {
            //        foreach (var item in raw.data.orders)
            //        {
            //            decimal conversionRate = 1m;
            //            //todo: conversion rate
            //            string itemSymbol = (string)item.symbol;

            //            if (!itemSymbol.EndsWith(_baseCurrency))
            //            {
            //                var baseSymbol = "";//(TradingApi.ModelObjects.BtcInfo.PairTypeEnum)Enum.Parse(typeof(TradingApi.ModelObjects.BtcInfo.PairTypeEnum), item.Symbol.Substring(0, 3) + usd);
            //                                    //var baseTicker = _rest.Get(baseSymbol, TradingApi.ModelObjects.BtcInfo.BitfinexUnauthenicatedCallsEnum.pubticker);
            //                                    // conversionRate = decimal.Parse(baseTicker.Mid);
            //            }
            //            else
            //            {
            //                // conversionRate = decimal.Parse(ticker.Mid);
            //            }

            //            list.Add(new Holding
            //            {
            //                AveragePrice = (decimal)item.avg_price,
            //                CurrencySymbol = itemSymbol.Substring(0, 3).ToUpper(),
            //                Quantity = (item.type == "buy" ? (decimal)item.amount : -(decimal)item.amount),
            //                Symbol = Symbol.Create(itemSymbol.ToUpper().Replace("_", ""), SecurityType.Forex, Market.Bitfinex.ToString()),
            //                Type = SecurityType.Forex,
            //                ConversionRate = GetConversionRate(itemSymbol.Substring(0, 3) + "usd")
            //            });
            //        }
            //    }
            //}

            return list;
        }

        /// <summary>
        /// Retreive orders from exchange
        /// </summary>
        /// <returns></returns>
        public override List<Order> GetOpenOrders()
        {
            var list = new List<Order>();

            foreach (string symbol in new[] { "btc_usd", "ltc_usd" })
            {

                var raw = GetOrders(symbol, OKCoinOrderStatus.Unfilled);

                if (raw != null && raw.orders != null)
                {

                    foreach (var item in raw.orders)
                    {
                        var mapped = Symbol.Create(symbol.ToUpper().Replace("_", ""), SecurityType.Forex, Market.Bitfinex.ToString());
                        list.Add(new Orders.MarketOrder
                        {
                            Price = (decimal)item.price,
                            BrokerId = new List<string> { (string)item.order_id },
                            Quantity = (item.type == "buy" ? (decimal)item.amount : -(decimal)item.amount),
                            Symbol = mapped,
                            PriceCurrency = _baseCurrency,
                            Time = DateTime.UtcNow,
                            Status = MapOrderStatus((int)item.status)
                        });
                    }
                }

            }
            return list;

        }

        private dynamic GetOrders(string symbol, OKCoinOrderStatus status)
        {
            var parameters = new Dictionary<string, string>
            {
                {"api_key", ApiKey},
                {"symbol", symbol},
                {"status", ((int)status).ToString()},
                {"current_page", "1"},
                {"page_length", "200"},
                {"sign", ""}
            };

            var sign = BuildSign(parameters);
            parameters["sign"] = sign;
            string response = com.okcoin.rest.HttpUtilManager.getInstance().requestHttpPost("https://www.okcoin.com/api/v1/", "order_history.do", parameters);
            var raw = JsonConvert.DeserializeObject<dynamic>(response, settings);

            return raw;
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
            var pairs = data.Keys.Where(p => p != "sign").OrderBy(k => k).Select(k => k + "=" + data[k]);
            string joined = string.Join("&", pairs.ToArray());
            joined += "&secret_key=" + this.ApiSecret;

            return joined.ToMD5().ToUpper();
        }

        private string ConvertSymbol(Symbol symbol)
        {
            return symbol.Value.Substring(0, 3).ToLower() + "_" + symbol.Value.Substring(3, 3).ToLower();
        }

        //todo: inject rest client
        //todo: configure rest url
        /// <summary>
        /// OKCoin API is broken for market_buy. Instead, use limit at best ask.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="parameters"></param>
        private void FixBrokenMarketBuyOrder(Order order, Dictionary<string, string> parameters)
        {
            string tickerUrl = string.Format("https://www.okcoin.{0}/api/v1/ticker.do?symbol={1}", _baseCurrency == "usd" ? "com" : "cn", ConvertSymbol(order.Symbol));
            using (System.Net.WebClient rest = new System.Net.WebClient())
            {
                var raw = JsonConvert.DeserializeObject<dynamic>(rest.DownloadString(tickerUrl), settings);
                parameters["price"] = raw.ticker.sell;
            }
            parameters["type"] = "buy";
        }

    }
}
