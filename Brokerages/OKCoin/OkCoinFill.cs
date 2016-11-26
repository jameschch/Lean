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

namespace QuantConnect.Brokerages.OKCoin
{

    /// <summary>
    /// Tracks fill messages
    /// </summary>
    public class OKCoinFill
    {

        Orders.Order _order;

        /// <summary>
        /// Lean orderId
        /// </summary>
        public int OrderId
        {
            get
            {
                return _order.Id;
            }
        }

        List<TradeMessage> messages = new List<TradeMessage>();

        /// <summary>
        /// Creates instance of BitfinexFill
        /// </summary>
        /// <param name="order"></param>
        public OKCoinFill(Orders.Order order)
        {
            _order = order;
        }

        /// <summary>
        /// Adds a trade message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public void Add(TradeMessage msg)
        {
            messages.Add(msg);
        }

        //todo: docs say tradeprice is filled amount. Check this
        /// <summary>
        /// Compares fill amouns to determine if fill is complete
        /// </summary>
        /// <returns></returns>
        public bool IsCompleted()
        {
            decimal quantity = messages.Sum(m => m.CompletedTradeAmount);
            return quantity >= _order.Quantity;
        }

        //todo: docs say tradeprice is filled amount. Check this
        /// <summary>
        /// Total amount executed across all fills
        /// </summary>
        /// <returns></returns>
        public decimal TotalQuantity()
        {
            return messages.Sum(m => m.CompletedTradeAmount);
        }


    }
}
