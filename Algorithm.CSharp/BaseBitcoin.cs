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
using NodaTime;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{

    /// <summary>
    /// Base class for Bitcoin algorithms. This base class provides standard behaviour for trading BTCUSD on Bitfinex
    /// trading on Bitfinex
    /// </summary>
    public abstract class BaseBitcoin : QCAlgorithm
    {

        const string _bitcoinSymbol = "BTCUSD";

        protected string BitcoinSymbol { get { return _bitcoinSymbol; } }


        /// <summary>
        /// Constructor sets up some custom providers and handlers for Bitfinex
        /// </summary>
        public BaseBitcoin()
        {
            //This is needed for margin call warning behaviour
            Portfolio = new BitfinexSecurityPortfolioManager(Securities, Transactions);
            //Non default brokerage for Forex. Cash or Margin accounts are supported
            SetBrokerageModel(BrokerageName.BitfinexBrokerage, AccountType.Margin);
            SetTimeZone(DateTimeZone.Utc);
            //Can be slow to fill. 20 second timeout should be adequate in most conditions
            Transactions.MarketOrderFillTimeout = new TimeSpan(0, 0, 20);
        }

        public override void Initialize()
        {
            SetStartDate(2015, 11, 10);
            SetEndDate(2016, 2, 20);
            SetCash("USD", 1000, 1m);
            AddSecurity(SecurityType.Forex, BitcoinSymbol, Resolution.Tick, Market.Bitcoin, false, 3.3m, false);
        }

        public void OnData(Ticks data)
        {
            foreach (var item in data)
            {
                foreach (var tick in item.Value)
                {
                    OnData(tick);
                }
            }
        }

        public abstract void OnData(Tick data);

        protected virtual void Output(string title)
        {
            Log(title + ": " + this.UtcTime.ToString() + ": " + Portfolio.Securities[BitcoinSymbol].Price.ToString() + " Trade:" + Portfolio[BitcoinSymbol].LastTradeProfit
                + " Total:" + Portfolio.TotalPortfolioValue);
        }

        /// <summary>
        /// Must liquidate before reversing position
        /// </summary>
        //todo: implement transaction limits in brokerage model
        protected virtual void Long()
        {
            if (Portfolio[BitcoinSymbol].IsShort)
            {
                Liquidate();
            }
            SetHoldings(BitcoinSymbol, 3.0m);
        }

        /// <summary>
        /// Must liquidate before reversing position
        /// </summary>
        //todo: implement transaction limits in brokerage model
        protected virtual void Short()
        {
            if (Portfolio[BitcoinSymbol].IsLong)
            {
                Liquidate();
            }
            SetHoldings(BitcoinSymbol, -3.0m);
        }

    }
}
