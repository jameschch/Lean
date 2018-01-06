﻿/*
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

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    /// <summary>
    /// Ticker message object
    /// </summary>
    public class L2Update : BaseMessage
    {
        private const int _channelId = 0;
        private const int _price = 1;
        private const int _count = 2;
        private const int _amount = 3;

        /// <summary>
        /// L2Update Message constructor
        /// </summary>
        /// <param name="values"></param>
        public L2Update(string[] values)
            : base(values)
        {
            ChannelId = GetInt(_channelId);
            Price = TryGetDecimal(_price);
            Count = GetInt(_count);
            Amount = TryGetDecimal(_amount);
        }

        /// <summary>
        /// Channel Id
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Count
        /// </summary>
        public decimal Count { get; set; }

        /// <summary>
        /// Amount
        /// </summary>
        public decimal Amount { get; set; }
    }
}