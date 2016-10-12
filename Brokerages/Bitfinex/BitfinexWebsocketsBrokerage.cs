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
using TradingApi.Bitfinex;
using WebSocketSharp;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Bitfinex WebSockets integration
    /// </summary>
    public partial class BitfinexWebsocketsBrokerage : BitfinexBrokerage, IDataQueueHandler, IDisposable
    {

        #region Declarations
        List<Securities.Cash> _cash = new List<Securities.Cash>();
        Dictionary<int, Channel> _channelId = new Dictionary<int, Channel>();
        Task _checkConnectionTask = null;
        CancellationTokenSource _checkConnectionToken;
        DateTime _heartbeatCounter = DateTime.UtcNow;
        const int _heartBeatTimeout = 30;
        protected IWebSocket WebSocket;
        object _cashLock = new object();
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            FloatParseHandling = FloatParseHandling.Decimal
        };
        protected string Url { get; set; }
        #endregion

        /// <summary>
        /// Create Brokerage instance
        /// </summary>
        public BitfinexWebsocketsBrokerage(string url, IWebSocket websocket, string apiKey, string apiSecret, string wallet, BitfinexApi restClient,
            decimal scaleFactor, ISecurityProvider securityProvider)
            : base(apiKey, apiSecret, wallet, restClient, scaleFactor, securityProvider)
        {
            Url = url;
            WebSocket = websocket;
            WebSocket.Initialize(url);
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

                WebSocket.Send(JsonConvert.SerializeObject(new
                {
                    @event = "subscribe",
                    channel = "trades",
                    pair = item.ToString()
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
            WebSocket.OnError += OnError;
            this.Authenticate();
        }

        /// <summary>
        /// Logs out and closes connection
        /// </summary>
        public override void Disconnect()
        {
            this.UnAuthenticate();
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
        /// Add bitfinex order and prepare for fill message
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        public override bool PlaceOrder(Order order)
        {
            var result = base.PlaceOrder(order);
            this.FillSplit.TryAdd(order.Id, new BitfinexFill(order, ScaleFactor));
            return result;
        }


        private async Task CheckConnection()
        {
            while (!_checkConnectionToken.Token.IsCancellationRequested)
            {
                if (!this.IsConnected || (DateTime.UtcNow - _heartbeatCounter).TotalSeconds > _heartBeatTimeout)
                {
                    Log.Trace("BitfinexWebsocketsBrokerage.CheckConnection(): Heartbeat timeout. Reconnecting");
                    Reconnect(false);
                }
                await Task.Delay(TimeSpan.FromSeconds(10), _checkConnectionToken.Token);
            }
        }

        protected virtual void Reconnect(bool wait = true)
        {
            if (wait)
            {
                this._checkConnectionTask.Wait(30);
            }
            var subscribed = GetSubscribed();
            //try to clean up state
            try
            {
                this.UnAuthenticate();
                this.Unsubscribe();
                WebSocket.Close();
            }
            catch (Exception)
            {
            }
            WebSocket.Connect();
            this.Subscribe(null, subscribed);
            this.Authenticate();
        }

        private void Unsubscribe()
        {
            this.Unsubscribe(null, GetSubscribed());
        }

        protected virtual IList<Symbol> GetSubscribed()
        {
            IList<Symbol> list = new List<Symbol>();

            foreach (var item in _channelId)
            {
                list.Add(Symbol.Create(item.Value.Symbol, SecurityType.Forex, Market.Bitfinex));
            }
            return list;
        }

    }
}
