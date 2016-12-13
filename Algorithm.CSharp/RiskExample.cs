using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using System.Linq;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class RiskExample : QCAlgorithm, IRequiredOrderMethods
    {
        //Configure which securities you'd like to use:
        public string[] Symbols = { "EURUSD" };

        //Risk in dollars per trade ($ or the quote currency of the assets)
        public decimal RiskPerTrade = 100;

        //Sets the profit to loss ratio we want to hit before we exit
        public decimal TargetProfitLossRatio = 0.05m;

        //Roughly how long does our "alpha" take to run
        public TimeSpan AverageInvestmentPeriod = TimeSpan.FromHours(6.5);

        //Cap the investment maximum size ($).
        public decimal MaximumTradeSize = 100;

        private Resolution _dataResolution = Resolution.Minute;
        private Dictionary<Symbol, TradingAsset> _tradingAssets;

        RelativeStrengthIndex rsi;

        public override void Initialize()
        {
            SetStartDate(2015, 9, 1);
            SetEndDate(2016, 3, 01);
            SetCash(1000);
            _tradingAssets = new Dictionary<Symbol, TradingAsset>();

            foreach (var symbol in Symbols)
            {
                AddCfd(symbol, Resolution.Minute, Market.Oanda);
                SetBrokerageModel(Brokerages.BrokerageName.OandaBrokerage);

                rsi = RSI(Symbols[0], 144, MovingAverageType.Exponential, Resolution.Daily);
                Securities[symbol].VolatilityModel = new ThreeSigmaVolatilityModel(_dataResolution.ToTimeSpan(), (int)AverageInvestmentPeriod.TotalMinutes);
                _tradingAssets.Add(symbol,
                    new TradingAsset(Securities[symbol],
                        this.GetEntrySignal,
                        this.GetExitSignal,
                        RiskPerTrade,
                        MaximumTradeSize,
                        this
                    ));
            }
        }

        private int GetEntrySignal()
        {
            if (Portfolio.Securities[Symbols[0]].Exchange.ExchangeOpen)
            {
                if (rsi.IsReady && rsi > 70)
                {
                    return 1;
                }
                else if (rsi.IsReady && rsi < 30)
                {
                    return -1;
                }
            }
            return 0;
        }

        private int GetExitSignal()
        {
            if (Portfolio.Securities[Symbols[0]].Exchange.ExchangeOpen && rsi.IsReady && rsi < 70 && rsi > 30)
            {
                return 1;  
            }
            return 0;
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        public void OnData(TradeBars data)
        {
            foreach (var symbol in Symbols)
            {
                //Create a trading asset package for each symbol 
                _tradingAssets[symbol].Scan(data[symbol]);
            }
        }
    }

    /// <summary>
    /// Interface for the two types of orders required to make the trade
    /// </summary>
    public interface IRequiredOrderMethods
    {
        OrderTicket StopMarketOrder(Symbol symbol, int quantity, decimal stopPrice, string tag = "");
        OrderTicket MarketOrder(Symbol symbol, int quantity, bool asynchronous = false, string tag = "");
    }


}