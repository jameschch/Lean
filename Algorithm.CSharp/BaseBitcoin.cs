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
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{

    /// <summary>
    /// Base class for Bitcoin algorithms. This base class provides standard behaviour for trading BTCUSD on Bitfinex
    /// trading on Bitfinex
    /// </summary>
    public abstract class BaseBitcoin : QCAlgorithm
    {

        const string btcusd = "BTCUSD";
        protected virtual decimal StopLoss { get { return 0.04m; } }
        protected virtual Dictionary<string, decimal> TrailingTakeProfit { get; set; }
        protected virtual decimal TakeProfit { get { return 0.04m; } }
        protected virtual decimal AtrScale { get { return 2m; } }
        protected string BTCUSD { get { return btcusd; } }
        decimal unrealizedProfit;
        protected virtual Decimal MinimumPosition { get { return 0.05m; } }
        protected virtual Decimal[] TakeStep { get { return new[] { 0.01m, 0.005m }; } }

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
            AverageTrueRange,
            UntilReversal,
            TrailingTake
        }

        /// <summary>
        /// Constructor sets up some custom providers and handlers for Bitfinex
        /// </summary>
        public BaseBitcoin()
        {
            //Non default brokerage for Forex. Cash or Margin accounts are supported
            SetBrokerageModel(BrokerageName.BitfinexBrokerage, AccountType.Margin);
            SetTimeZone(DateTimeZone.Utc);
            //Can be slow to fill. 60 second timeout should be adequate in most conditions
            Transactions.MarketOrderFillTimeout = new TimeSpan(0, 0, 60);
            //Portfolio.MarginCallModel = new BitfinexMarginCallModel(Portfolio);
            TrailingTakeProfit = new Dictionary<string, decimal>();

        }

        //todo: this is temporary
        public BaseBitcoin(bool isBitcoin)
        {
            TrailingTakeProfit = new Dictionary<string, decimal>();

        }

        public override void Initialize()
        {
            //SetStartDate(2016, 5, 1);
            //SetEndDate(2016, 6, 30);
            SetCash("USD", 1000, 1m);
            var security = AddSecurity(SecurityType.Forex, BTCUSD, Resolution.Tick, Market.Bitfinex, false, 3.3m, false);
            if (LiveMode)
            {
                AddSecurity(SecurityType.Forex, "ETHUSD", Resolution.Tick, Market.Bitfinex, false, 3.3m, false);
                AddSecurity(SecurityType.Forex, "LTCUSD", Resolution.Tick, Market.Bitfinex, false, 3.3m, false);
                AddSecurity(SecurityType.Forex, "BFXUSD", Resolution.Tick, Market.Bitfinex, false, 3.3m, false);
            }
            SetBenchmark(security.Symbol);
        }

        public void OnData(Ticks data)
        {
            if (!IsWarmingUp)
            {
                foreach (var item in Portfolio)
                {
                    if (Portfolio[item.Key].AbsoluteHoldingsValue > 0 && Portfolio[item.Key].AbsoluteHoldingsValue / Portfolio.TotalPortfolioValue < MinimumPosition)
                    {
                        Liquidate();
                    }
                }
            }
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
            Output(title, btcusd);
        }

        protected virtual void Output(string title, string symbol = btcusd)
        {
            Log(title + ": " + this.UtcTime.ToString() + symbol + ": " + Portfolio.Securities[symbol].Price.ToString() + " Trade:" + Math.Round(Portfolio[symbol].LastTradeProfit, 2)
                + " Total:" + Math.Round(Portfolio.TotalPortfolioValue, 2));
        }

        /// <summary>
        /// Must liquidate before reversing position
        /// </summary>
        protected virtual void Long(string symbol = btcusd)
        {
            SetHoldings(symbol, 3.0m);
        }

        /// <summary>
        /// Must liquidate before reversing position
        /// </summary>
        protected virtual void Short(string symbol = btcusd)
        {
            SetHoldings(symbol, -3.0m);
        }

        protected void TryStopLoss(Symbol symbol, StopLossStrategy strategy = StopLossStrategy.TotalPortfolioValue, AverageTrueRange atr = null)
        {
            if (Portfolio[symbol].Invested)
            {
                if (strategy == StopLossStrategy.TotalPortfolioValue)
                {
                    if (Portfolio[symbol].TotalCloseProfit() / Portfolio.TotalPortfolioValue < -StopLoss)
                    {
                        Liquidate(symbol);
                        Output("stop", symbol);
                    }
                }
                else if (strategy == StopLossStrategy.UnrealizedProfit)
                {
                    if (Portfolio[symbol].UnrealizedProfitPercent < -StopLoss)
                    {
                        Liquidate(symbol);
                        Output("stop", symbol);
                    }
                }
                else if (strategy == StopLossStrategy.AverageTrueRange)
                {
                    decimal atrLimit = Portfolio[symbol].AbsoluteHoldingsCost * atr * AtrScale;
                    if (Portfolio.TotalUnrealisedProfit < -atrLimit)
                    {
                        Liquidate(symbol);
                        Output("stop", symbol);
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
                    if (Portfolio[symbol].TotalCloseProfit() / Portfolio.TotalPortfolioValue > TakeProfit)
                    {
                        Liquidate(symbol);
                        Output("take", symbol);
                    }
                }
                else if (strategy == TakeProfitStrategy.UnrealizedProfit)
                {
                    if (Portfolio[symbol].UnrealizedProfitPercent > TakeProfit)
                    {
                        Liquidate(symbol);
                        Output("take", symbol);
                    }
                }
                else if (strategy == TakeProfitStrategy.AverageTrueRange)
                {
                    decimal atrLimit = Portfolio[symbol].AbsoluteHoldingsCost * atr * AtrScale;
                    if (Portfolio.TotalUnrealisedProfit > atrLimit)
                    {
                        Liquidate(symbol);
                        Output("take", symbol);
                    }
                }
                else if (strategy == TakeProfitStrategy.UntilReversal)
                {
                    if (Portfolio[symbol].UnrealizedProfitPercent > unrealizedProfit)
                    {
                        unrealizedProfit = Portfolio[symbol].UnrealizedProfitPercent;
                    }
                    if (unrealizedProfit > 0 && Portfolio[symbol].UnrealizedProfitPercent > 0
                        && unrealizedProfit - Portfolio[symbol].UnrealizedProfitPercent > 0.01m)
                    {
                        unrealizedProfit = 0;
                        Liquidate(symbol);
                        Output("take", symbol);
                    }
                }
                else if (strategy == TakeProfitStrategy.TrailingTake)
                {
                    decimal profit = Portfolio[symbol].UnrealizedProfitPercent;
                    if (profit > TakeProfit)
                    {
                        if (!TrailingTakeProfit.ContainsKey(symbol))
                        {
                            TrailingTakeProfit.Add(symbol, TakeProfit);
                        }
                        else if (profit > TrailingTakeProfit[symbol])
                        {
                            TrailingTakeProfit[symbol] = profit;
                        }
                        else if (profit < TrailingTakeProfit[symbol])
                        {
                            TrailingTakeProfit.Remove(symbol);
                            Liquidate(symbol);
                            Output("take", symbol);
                        }
                    }
                    else
                    {
                        if (TrailingTakeProfit.ContainsKey(symbol))
                        {
                            Liquidate(symbol);
                            Output("take", symbol);
                            TrailingTakeProfit.Remove(symbol);
                        }
                    }
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Submitted)
            {
                unrealizedProfit = 0;
            }
        }

    }
}
