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

namespace QuantConnect.Brokerages.OKCoin
{

    /// <summary>
    /// Ticker message object
    /// </summary>
    public class TickerMessage : BaseMessage
    {

        /// <summary>
        /// Bid
        /// </summary>
        public decimal Buy { get; set; }
        /// <summary>
        /// Ask
        /// </summary>
        public decimal Sell { get; set; }
        /// <summary>
        /// Last Price
        /// </summary>
        public decimal Last { get; set; }
        /// <summary>
        /// Volume
        /// </summary>
        public decimal Volume { get; set; }
        /// <summary>
        /// High
        /// </summary>
        public decimal High { get; set; }
        /// <summary>
        /// Low
        /// </summary>
        public decimal Low { get; set; }
        /// <summary>
        /// Timestamp
        /// </summary>
        public int Timestamp { get; set; }

    }
}
