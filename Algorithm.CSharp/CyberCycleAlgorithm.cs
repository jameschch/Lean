using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
//using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Util;
using QuantConnect.Configuration;
using QuantConnect.Algorithm.CSharp;

namespace QuantConnect.Algorithm.MyAlgorithms
{
    internal class CyberCycleAlgorithm : BaseBitcoin
    {

        public CyberCycleAlgorithm() : base(false)
        { }

        decimal stop = Config.GetValue<decimal>("stop", 0.01m);
        decimal take = Config.GetValue<decimal>("take", 0.06m);
        protected override decimal StopLoss { get { return stop; } }
        protected override decimal TakeProfit { get { return take; } }
        DateTime startTime = DateTime.Now;
        private DateTime _startDate = new DateTime(2015, 5, 19);
        private DateTime _endDate = new DateTime(2015, 6, 1);
        private string symbol = "BCOUSD";
        private int barcount = 0;
        private RollingWindow<IndicatorDataPoint> Price;
        private CyberCycle cycle;
        private RollingWindow<IndicatorDataPoint> cycleSignal;
        private StandardDeviation standardDeviation;
        private InverseFisherTransform fish;
        private RollingWindow<IndicatorDataPoint> diff;
        private RollingWindow<IndicatorDataPoint> fishHistory;
        private RollingWindow<IndicatorDataPoint> fishDirectionHistory;
        bool fishDirectionChanged;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        /// <seealso cref="QCAlgorithm.SetStartDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetEndDate(System.DateTime)"/>
        /// <seealso cref="QCAlgorithm.SetCash(decimal)"/>
        public override void Initialize()
        {
            //mylog.Debug(transheader);
            //mylog.Debug(ondataheader);

            //Initialize dates
            SetStartDate(_startDate);
            SetEndDate(_endDate);
            SetCash(1000);

            //Add as many securities as you like. All the data will be passed into the event handler:
            AddCfd(symbol, Resolution.Minute, Market.Oanda);
            SetBrokerageModel(Brokerages.BrokerageName.OandaBrokerage);
            Price = new RollingWindow<IndicatorDataPoint>(14);
            cycleSignal = new RollingWindow<IndicatorDataPoint>(14);
            cycle = new CyberCycle(Config.GetInt("period", 7));
            Price = new RollingWindow<IndicatorDataPoint>(14);
            diff = new RollingWindow<IndicatorDataPoint>(20);
            standardDeviation = new StandardDeviation(30);
            fish = new InverseFisherTransform(Config.GetInt("fisherPeriod", 10));
            fishHistory = new RollingWindow<IndicatorDataPoint>(2);
            fishDirectionHistory = new RollingWindow<IndicatorDataPoint>(2);


        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">TradeBars IDictionary object with your stock data</param>
        public void OnData(TradeBars data)
        {

            if (Portfolio.TotalPortfolioValue < 900)
            {
                Quit();
            }

            barcount++;
            var time = this.Time;
            Price.Add(idp(time, data[symbol].Close));
            cycleSignal.Add(idp(time, cycle.Current.Value));        //add last iteration value for the cycle
            cycle.Update(time, data[symbol].Close);
            diff.Add(idp(time, cycle.Current.Value - cycleSignal[0].Value));
            fish.Update(idp(time, cycle.Current.Value));

            try
            {
                standardDeviation.Update(idp(time, fish.Current.Value));

            }
            catch (Exception ex)
            {

                //  throw;
            }
            fishHistory.Add(idp(time, fish.Current.Value));


            Strategy(data);

        }

        private void Strategy(TradeBars data)
        {
            if (barcount < 2)
            {
                fishDirectionHistory.Add(idp(this.Time, 0));
                fishDirectionChanged = false;
            }
            else
            {
                fishDirectionHistory.Add(idp(this.Time, Math.Sign(fishHistory[0].Value - fishHistory[1].Value)));
                fishDirectionChanged = fishDirectionHistory[0].Value != fishDirectionHistory[1].Value;
            }

            if (Securities[symbol].Exchange.ExchangeOpen)
            {
                if (cycle.IsReady)
                {
                    Invested();
                    NotInvested();
                }
            }
            //sharesOwned = Portfolio[symbol].Quantity;
        }

        private void NotInvested()
        {
            if (!Portfolio[symbol].Invested)
            {
                if (fishDirectionHistory[0].Value > 0 && fishDirectionChanged)  // if it started up
                {
                    Buy(symbol, 1);
                    //Output("buy ", symbol);
                }
                if (fishDirectionHistory[0].Value < 0 && fishDirectionChanged) // if it started going down
                {
                    Sell(symbol, 1);
                    //Output("sell", symbol);
                }
            }
        }

        private void Invested()
        {
            if (Portfolio[symbol].Invested)
            {
                if (fishDirectionHistory[0].Value > 0 && fishDirectionChanged)  // if it started up
                {
                    Buy(symbol, 1);
                    //Output("buy ", symbol);
                }
                if (fishDirectionHistory[0].Value < 0 && fishDirectionChanged) // if it started going down
                {
                    Sell(symbol, 1);
                   // Output("sell", symbol);
                }
                //if (Portfolio[symbol].IsShort)
                //{
                //    if (fish.Current.Value > standardDeviation.Current.Value * factor)
                //    {
                //        Buy(symbol, 200);
                //    }
                //}
                //if (Portfolio[symbol].IsLong)
                //{
                //    if (fish.Current.Value < (standardDeviation.Current.Value * -factor))
                //    {
                //        Sell(symbol, 200);
                //    }
                //}
            }
        }

        private IndicatorDataPoint idp(DateTime time, decimal value)
        {
            return new IndicatorDataPoint(time, value);
        }

        public override void OnData(Tick data)
        {
            TryTakeProfit(symbol, TakeProfitStrategy.UnrealizedProfit);
            TryStopLoss(symbol, StopLossStrategy.UnrealizedProfit);
        }
    }
}
