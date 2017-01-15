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
using QuantConnect.Configuration;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Bitfinex.Rest;
using WebSocketSharp;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Bitfinex WebSockets integration
    /// </summary>
    public partial class BitfinexWebsocketsBrokerage : BitfinexBrokerage, IDataQueueHandler, IDisposable
    {

        #region Declarations
        Dictionary<int, Channel> _channelId = new Dictionary<int, Channel>();
        Thread _connectionMonitorThread;
        CancellationTokenSource _cancellationTokenSource;
        private readonly object _lockerConnectionMonitor = new object();
        DateTime _lastHeartbeatUtcTime = DateTime.UtcNow;
        const int _heartbeatTimeout = 300;
        private volatile bool _connectionLost;
        protected IWebSocket WebSocket;
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };
        DateTime _previousAuthentication = DateTime.Now.AddSeconds(-10);
        bool _isReconnecting = false;
        protected string Url { get; set; }
        #endregion

        /// <summary>
        /// Create Brokerage instance
        /// </summary>
        public BitfinexWebsocketsBrokerage(string url, IWebSocket websocket, string apiKey, string apiSecret, string wallet, BitfinexApi restClient,
            ISecurityProvider securityProvider)
            : base(apiKey, apiSecret, wallet, restClient, securityProvider)
        {
            WebSocket = websocket;
            WebSocket.Initialize(url);
            Url = url;
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
                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "subscribe",
                    channel = "ticker",
                    pair = item.ToString()
                }));
                Log.Trace("Sent subcribe: " + item.ToString());

                //_webSocket.Send(JsonConvert.SerializeObject(new
                //{
                //    @event = "subscribe",
                //    channel = "trades",
                //    pair = item.ToString()
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
                lock (_channelId)
                {
                    foreach (var channel in _channelId.Where(c => c.Value.Symbol == item.ToString()))
                    {
                        Unsubscribe(channel.Key);
                    }
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
            get { return WebSocket.ReadyState == WebSocketState.Connecting || WebSocket.ReadyState == WebSocketState.Open; }
        }

        /// <summary>
        /// Creates wss connection
        /// </summary>
        public override void Connect()
        {
            WebSocket.OnMessage += OnMessage;
            WebSocket.OnError += OnError;
            WebSocket.OnOpen += (o, e) => { this.Authenticate(); };

            WebSocket.Connect();
            _cancellationTokenSource = new CancellationTokenSource();
            _connectionMonitorThread = new Thread(() =>
            {
                var nextReconnectionAttemptUtcTime = DateTime.UtcNow;
                double nextReconnectionAttemptSeconds = 1;

                lock (_lockerConnectionMonitor)
                {
                    _lastHeartbeatUtcTime = DateTime.UtcNow;
                }

                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {

                        TimeSpan elapsed;
                        lock (_lockerConnectionMonitor)
                        {
                            elapsed = DateTime.UtcNow - _lastHeartbeatUtcTime;
                        }

                        if (!_connectionLost && elapsed > TimeSpan.FromSeconds(_heartbeatTimeout))
                        {
                            _connectionLost = true;
                            nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);

                            OnMessage(BrokerageMessageEvent.Disconnected("Connection with server lost. " +
                                                                         "This could be because of internet connectivity issues. "));
                        }
                        else if (_connectionLost)
                        {
                            try
                            {
                                if (elapsed <= TimeSpan.FromSeconds(_heartbeatTimeout))
                                {
                                    _connectionLost = false;
                                    nextReconnectionAttemptSeconds = 1;

                                    OnMessage(BrokerageMessageEvent.Reconnected("Connection with server restored."));
                                }
                                else
                                {
                                    if (DateTime.UtcNow > nextReconnectionAttemptUtcTime)
                                    {
                                        try
                                        {
                                            Reconnect();
                                        }
                                        catch (Exception)
                                        {
                                            // double the interval between attempts (capped to 1 minute)
                                            nextReconnectionAttemptSeconds = Math.Min(nextReconnectionAttemptSeconds * 2, 60);
                                            nextReconnectionAttemptUtcTime = DateTime.UtcNow.AddSeconds(nextReconnectionAttemptSeconds);
                                        }
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                Log.Error(exception);
                            }
                        }

                        Thread.Sleep(10000);
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                }
            });
            _connectionMonitorThread.Start();
            while (!_connectionMonitorThread.IsAlive)
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Logs out and closes connection
        /// </summary>
        public override void Disconnect()
        {
            this.UnAuthenticate();
            _cancellationTokenSource.Cancel();
            _connectionMonitorThread.Join();
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
        /// Add bitfinex order and prepare for fill message
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool PlaceOrder(Order order)
        {
            decimal quantity = order.Quantity;
            this.FillSplit.TryAdd(order.Id, new BitfinexFill(order));
            var result = base.PlaceOrder(order);
            order.Quantity = quantity;
            return result;
        }

        protected virtual void Reconnect()
        {
            try
            {
                var subscribed = GetSubscribed();
                //try to clean up state
                try
                {
                    if (IsConnected)
                    {
                        WebSocket.Close();
                    }
                    WebSocket.OnError -= OnError;
                    this.UnAuthenticate();
                    this.Unsubscribe();
                }
                catch (Exception ex)
                {
                    Log.Trace("Exception encountered cleaning up state.", ex);
                }
                if (!IsConnected)
                {
                    WebSocket.Connect();
                }
                Log.Trace("Attempting Subscribe");
                this.Subscribe(null, subscribed);
                Log.Trace("Attempting Auth");
                this.Authenticate();
            }
            catch (Exception ex)
            {
                Log.Trace("Exception encountered reconnecting.", ex);
            }
            finally
            {
                WebSocket.OnError += OnError;
            }
        }

        private void Unsubscribe()
        {
            this.Unsubscribe(null, GetSubscribed());
        }

        protected virtual IList<Symbol> GetSubscribed()
        {
            IList<Symbol> list = new List<Symbol>();
            lock (_channelId)
            {
                foreach (var item in _channelId)
                {
                    list.Add(Symbol.Create(item.Value.Symbol, SecurityType.Forex, Market.Bitfinex));
                }
            }
            return list;
        }

    }
}
