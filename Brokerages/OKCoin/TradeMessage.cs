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
    /// Trade Message
    /// </summary>
    public class TradeMessage : BaseMessage
    {

        /// <summary>
        /// Trade sequence
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Trade Id
        /// </summary>
        public int OrderId { get; set; }
        /// <summary>
        /// Currency Pair
        /// </summary>
        public string Symbol { get; set; }
        /// <summary>
        /// Timestamp
        /// </summary>
        public int CreatedDate { get; set; }
        /// <summary>
        /// Amount Executed
        /// </summary>
        public decimal CompletedTradeAmount { get; set; }
        /// <summary>
        /// Price Executed
        /// </summary>
        public decimal AveragePrice { get; set; }
        public decimal TradePrice { get; set; }
        public decimal SigTradeAmount { get; set; }
        public decimal SigTradePrice { get; set; }
        public int Status { get; set; }
        public string TradeType { get; set; }
        public decimal TradeUnitPrice { get; set; }
        public decimal UnTrade { get; set; }
    }
}
