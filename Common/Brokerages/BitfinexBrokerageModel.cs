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
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Forex;
using QuantConnect.Securities.Interfaces;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Slippage;
using QuantConnect.Configuration;
using System.Linq;
using QuantConnect.Logging;

namespace QuantConnect.Brokerages
{

    /// <summary>
    /// Provides Bitfinex specific properties
    /// </summary>
    public class BitfinexBrokerageModel : DefaultBrokerageModel
    {

        string _wallet;
        const string exchange = "exchange";

        /// <summary>
        /// Initializes a new instance of the <see cref="BitfinexBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to 
        /// <see cref="QuantConnect.AccountType.Margin"/></param>
        public BitfinexBrokerageModel(AccountType accountType = AccountType.Margin)
            : base(accountType)
        {
            _wallet = Config.Get("bitfinex-wallet");

            if (_wallet == "exchange" && accountType == AccountType.Margin)
            {
                accountType = AccountType.Cash;
                Log.Trace("Exchange wallet does not allow margin trades. Defaulting to cash");
            }

        }

        /// <summary>
        /// Bitfinex global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            return this.AccountType == AccountType.Margin ? 3.3m : 1;
        }

        /// <summary>
        /// Provides Bitfinex fee model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new BitfinexFeeModel();
        }

        /// <summary>
        /// Provides Bitfinex slippage model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override ISlippageModel GetSlippageModel(Security security)
        {
            return new BitfinexSlippageModel();
        }

        //todo: check minimum trade limits
        /// <summary>
        /// Validates pending orders based on currency pair, order amount, security type
        /// </summary>
        /// <param name="security"></param>
        /// <param name="order"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            message = null;
            Dictionary<string, int> symbol = new Dictionary<string,int> { {"BTCUSD", 2}, {"ETHUSD", 2}, {"ETHBTC", 4}, {"LTCUSD", 2}, {"LTCBTC", 4} };
            
            var securityType = order.SecurityType;
            if (securityType != SecurityType.Forex || !symbol.ContainsKey(order.Symbol.Value))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported", "This trade is not supported.");
                return false;
            }

            if (NumberOfDecimals(order.Quantity) > symbol[order.Symbol.Value])
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported", string.Format("Exceeded {0} decimal places for currency pair {1}.",
                    symbol[security.Symbol.Value].ToString(), order.Symbol.Value));
                return false;            
            }

            return true;
        }

        private int NumberOfDecimals(decimal quantity)
        {
            return BitConverter.GetBytes(decimal.GetBits(quantity)[3])[2];
        }

    }
}