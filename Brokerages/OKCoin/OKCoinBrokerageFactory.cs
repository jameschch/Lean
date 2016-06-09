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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Util;
using QuantConnect.Interfaces;
using QuantConnect.Configuration;
using QuantConnect.Brokerages.Bitfinex;

namespace QuantConnect.Brokerages.OKCoin
{

    /// <summary>
    /// Factory method to create OKCoin Websockets brokerage
    /// </summary>
    public class OKCoinBrokerageFactory : BrokerageFactory
    {

        /// <summary>
        /// Factory constructor
        /// </summary>
        public OKCoinBrokerageFactory()
            : base(typeof(OKCoinWebsocketsBrokerage))
        {
        }

        /// <summary>
        /// Not required
        /// </summary>
        public override void Dispose()
        {

        }


        /// <summary>
        /// provides brokerage connection data
        /// </summary>
        public override Dictionary<string, string> BrokerageData
        {
            get
            {
                return new Dictionary<string, string>
                {
                    {"apiSecret" ,Config.Get("OKCoin-api-secret", "123")},
                    {"apiKey" ,Config.Get("OKCoin-api-key", "123")},
                    {"wallet" ,Config.Get("OKCoin-wallet", "123")},
                    {"url" , Config.Get("OKCoin-wss", "wss://real.okcoin.cn:10440/websocket/okcoinapi")},
                    {"url-international" , Config.Get("OKCoin-wss", "wss://real.okcoin.com:10440/websocket/okcoinapi")},
                    {"scaleFactor", Config.Get("OKCoin-scale-factor", "1")},
                    {"spotOrFuture", Config.Get("spotOrFuture", "spot")}
                };
            }
        }

        /// <summary>
        /// The brokerage model
        /// </summary>
        public override IBrokerageModel BrokerageModel
        {
            get { return new OKCoinBrokerageModel(); }
        }

        /// <summary>
        /// Create the Brokerage instance
        /// </summary>
        /// <param name="job"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public override Interfaces.IBrokerage CreateBrokerage(Packets.LiveNodePacket job, Interfaces.IAlgorithm algorithm)
        {

            //it's desirable to throw an exception here when failing parse
            decimal scaleFactor = decimal.Parse(job.BrokerageData["scaleFactor"]);          

            if (string.IsNullOrEmpty(job.BrokerageData["apiSecret"]))
                throw new Exception("Missing OKCoin-api-secret in config.json");

            if (string.IsNullOrEmpty(job.BrokerageData["apiKey"]))
                throw new Exception("Missing OKCoin-api-key in config.json");


            var webSocketClient = new WebSocketWrapper();
            var orderClient = new WebSocketWrapper();

            var brokerage = new OKCoinWebsocketsBrokerage(job.BrokerageData["url-international"], webSocketClient, orderClient, job.BrokerageData["apiKey"], job.BrokerageData["apiSecret"],
                job.BrokerageData["spotOrFuture"], scaleFactor, algorithm.Portfolio);
            Composer.Instance.AddPart<IDataQueueHandler>(brokerage);

            return brokerage;
        }
    }
}
