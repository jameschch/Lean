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

namespace QuantConnect.Brokerages.Bitfinex.Messages
{
    /// <summary>
    /// Ticker message object
    /// </summary>
    public class OrderUpdate : BaseMessage
    {
        private const int _id = 0;
        private const int _pair = 1;
        private const int _amount = 2;
        private const int _amount_orig = 3;
        private const int _type = 4;
        private const int _status = 5;
        private const int _price = 6;
        private const int _price_avg = 7;
        private const int _created_at = 8;
        private const int _notify = 9;
        private const int _hidden = 10;
        private const int _oco = 11;

        /// <summary>
        /// L2Update Message constructor
        /// </summary>
        /// <param name="values"></param>
        public OrderUpdate(string[] values)
            : base(values)
        {
            OrderId = GetLong(_id);
            OrderPair = GetString(_pair);
            OrderAmount = TryGetDecimal(_id);
            //OrderAmountOrig = 
            OrderType = GetString(_type);
            OrderStatus = GetString(_status);
            OrderPrice = TryGetDecimal(_price);
            OrderPriceAvg = TryGetDecimal(_price_avg);
            OrderCreatedAt = GetString(_created_at);
            //OrderNotify = GetString(_notify);
            //OrderHidden = GetInt(_hidden);
            //OrderOco = GetInt(_oco);
        }

        /// <summary>
        /// Order Id
        /// </summary>
        public long OrderId { get; set; }

        /// <summary>
        /// Order Pair
        /// </summary>
        public string OrderPair { get; set; }

        /// <summary>
        /// Order Amount
        /// </summary>
        public decimal OrderAmount { get; set; }

        /// <summary>
        /// Order Type
        /// </summary>
        public string OrderType { get; set; }

        /// <summary>
        /// Order Status
        /// </summary>
        public string OrderStatus { get; set; }

        /// <summary>
        /// Order Price
        /// </summary>
        public decimal OrderPrice { get; set; }

        /// <summary>
        /// Order Price Avg
        /// </summary>
        public decimal OrderPriceAvg { get; set; }

        /// <summary>
        /// Order Created At
        /// </summary>
        public string OrderCreatedAt { get; set; }
    }
}