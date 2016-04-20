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
using QuantConnect.Data.Market;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.Bitfinex
{

    /// <summary>
    /// Ticker message object
    /// </summary>
    public class TradeTickerMessage : BaseMessage
    {

        const int _channel_id = 0;
        const int _term = 1;
        const int _seq = 2;
        const int _id = 3;
        const int _timestamp = 4;
        const int _price = 5;
        const int _amount = 6;

        /// <summary>
        /// Ticker Message constructor
        /// </summary>
        /// <param name="values"></param>
        public TradeTickerMessage(string[] values)
            : base(values)
        {
            CHANNEL_ID = GetInt(_channel_id);
            TERM = values[_term];
            SEQ = values[_seq];
            ID = GetInt(_id);
            TIMESTAMP = GetInt(_timestamp);
            PRICE = TryGetDecimal(_price);
            AMOUNT = TryGetDecimal(_amount);
        }

        /// <summary>
        /// Channel Id
        /// </summary>
        public int CHANNEL_ID { get; set; }
        /// <summary>
        /// te or tu
        /// </summary>
        public string TERM { get; set; }
        /// <summary>
        /// sequqnce number
        /// </summary>
        public string SEQ { get; set; }
        /// <summary>
        /// Id
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// Timestamp
        /// </summary>
        public int TIMESTAMP { get; set; }
        /// <summary>
        /// Price
        /// </summary>
        public decimal PRICE { get; set; }
        /// <summary>
        /// Amount
        /// </summary>
        public decimal AMOUNT { get; set; }

    }
}
