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
using QuantConnect.Indicators;
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

        const string btcusd = "BTCUSD";
        protected virtual decimal StopLoss { get { return 0.1m; } }
        protected virtual decimal TakeProfit { get { return 0.1m; } }
        protected virtual decimal AtrScale { get { return 2m; } }
        protected string BTCUSD { get { return btcusd; } }

        public enum StopLossStrategy
        {
            UnrealizedProfit,
            TotalPortfolioValue,
            AverageTrueRange
        }

        public enum TakeProfitStrategy
        {
            UnrealizedProfit,
            TotalPortfolioValue,
            AverageTrueRange
        }

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
            SetStartDate(2016, 1, 2);
            SetEndDate(2016, 5, 5);
            SetCash("USD", 1000, 1m);
            var security = AddSecurity(SecurityType.Forex, BTCUSD, Resolution.Tick, Market.Bitfinex, false, 3.3m, false);
            SetBenchmark(security.Symbol);
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
            Log(title + ": " + this.UtcTime.ToString() + ": " + Portfolio.Securities[BTCUSD].Price.ToString() + " Trade:" + Math.Round(Portfolio[BTCUSD].LastTradeProfit, 2)
                + " Total:" + Portfolio.TotalPortfolioValue);
        }

        /// <summary>
        /// Must liquidate before reversing position
        /// </summary>
        //todo: implement transaction limits in brokerage model
        protected virtual void Long()
        {
            if (Portfolio[BTCUSD].IsShort)
            {
                Liquidate();
            }
            SetHoldings(BTCUSD, 3.0m);
        }

        /// <summary>
        /// Must liquidate before reversing position
        /// </summary>
        //todo: implement transaction limits in brokerage model
        protected virtual void Short()
        {
            if (Portfolio[BTCUSD].IsLong)
            {
                Liquidate();
            }
            SetHoldings(BTCUSD, -3.0m);
        }

        protected void TryStopLoss(string symbol = btcusd, StopLossStrategy strategy = StopLossStrategy.TotalPortfolioValue, AverageTrueRange atr = null)
        {
            if (Portfolio[symbol].Invested)
            {
                if (strategy == StopLossStrategy.TotalPortfolioValue)
                {
                    if (Portfolio[BTCUSD].TotalCloseProfit() / Portfolio.TotalPortfolioValue < -StopLoss)
                    {
                        Liquidate();
                        Output("stop");
                    }
                }
                else if (strategy == StopLossStrategy.UnrealizedProfit)
                {
                    if (Portfolio[symbol].UnrealizedProfitPercent < -StopLoss)
                    {
                        Liquidate();
                        Output("stop");
                    }
                }
                else if (strategy == StopLossStrategy.AverageTrueRange)
                {
                    decimal atrLimit = Portfolio[BTCUSD].AbsoluteHoldingsCost * atr * AtrScale;
                    if (Portfolio.TotalUnrealisedProfit < -atrLimit)
                    {
                        Liquidate();
                        Output("stop");
                    }
                }
            }
        }

        protected void TryTakeProfit(string symbol = btcusd, TakeProfitStrategy strategy = TakeProfitStrategy.TotalPortfolioValue, AverageTrueRange atr = null)
        {
            if (Portfolio[symbol].Invested)
            {
                if (strategy == TakeProfitStrategy.TotalPortfolioValue)
                {
                    if (Portfolio[BTCUSD].TotalCloseProfit() / Portfolio.TotalPortfolioValue > TakeProfit)
                    {
                        Liquidate();
                        Output("take");
                    }
                }
                else if (strategy == TakeProfitStrategy.UnrealizedProfit)
                {
                    if (Portfolio[symbol].UnrealizedProfitPercent > TakeProfit)
                    {
                        Liquidate();
                        Output("take");
                    }
                }
                else if (strategy == TakeProfitStrategy.AverageTrueRange)
                {
                    decimal atrLimit = Portfolio[BTCUSD].AbsoluteHoldingsCost * atr * AtrScale;
                    if (Portfolio.TotalUnrealisedProfit > atrLimit)
                    {
                        Liquidate();
                        Output("take");
                    }
                }
            }
        }

    }
}
