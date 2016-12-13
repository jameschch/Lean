using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    class TradingAsset
    {
        public Func<int> ExitSignal;
        public Func<int> EnterSignal;
       
        private decimal _risk;
        private Symbol _symbol;
        private Security _security;
        private decimal _maximumTradeSize;
        private List<TradeProfile> _tradeProfiles;
        private IRequiredOrderMethods _orderMethods;

        /// <summary>
        /// Initializes each Trading Asset
        /// </summary>
        /// <param name="security"></param>
        /// <param name="enterSignal"></param>
        /// <param name="exitSignal"></param>
        /// <param name="risk"></param>
        /// <param name="maximumTradeSize"></param>
        /// <param name="orderMethods"></param>
        public TradingAsset(Security security, Func<int> enterSignal, Func<int> exitSignal, decimal risk, decimal maximumTradeSize, IRequiredOrderMethods orderMethods)
        {
            _security = security;
            _symbol = _security.Symbol;
            EnterSignal = enterSignal;
            ExitSignal = exitSignal;
            _risk = risk;
            _maximumTradeSize = maximumTradeSize;
            _orderMethods = orderMethods;
            _tradeProfiles = new List<TradeProfile>();
        }
        
        /// <summary>
        /// Scan
        /// </summary>
        /// <param name="data"></param>
        public void Scan(TradeBar data)
        {
            MarkStopTicketsFilled();
            EnterTradeSignal(data);
            ExitTradeSignal(data);
            RemoveAllFinishedTrades();
        }

        /// <summary>
        /// Executes all the logic when the Enter Signal is triggered
        /// </summary>
        /// <param name="data"></param>
        public void EnterTradeSignal(TradeBar data)
        {
            var signal = EnterSignal.Invoke();
            if (signal != 0)
            {
                //Creates a new trade profile once it enters a trade
                var profile = new TradeProfile(_symbol, _security.VolatilityModel.Volatility, _risk, data.Close, _maximumTradeSize, ExitSignal);

                if (profile.Quantity > 0)
                {
                    profile.OpenTicket = _orderMethods.MarketOrder(_symbol, signal * profile.Quantity);
                    profile.StopTicket = _orderMethods.StopMarketOrder(_symbol, -signal * profile.Quantity,
                        profile.OpenTicket.AverageFillPrice - signal * profile.DeltaStopLoss);

                    _tradeProfiles.Add(profile);
                }

            }
         }

        /// <summary>
        /// Executes all the logic when the Exit Signal is triggered
        /// </summary>
        /// <param name="data"></param>
        public void ExitTradeSignal(TradeBar data)
        {
            foreach (var tradeProfile in _tradeProfiles.Where(x => x.IsTradeFinished == false))
            {
                int signal = tradeProfile.ExitSignal.Invoke();

                if (signal != 0)
                {
                    if (tradeProfile.StopTicket.Status != OrderStatus.Filled)
                    {
                        tradeProfile.ExitTicket = _orderMethods.MarketOrder(_symbol, -(int)tradeProfile.OpenTicket.QuantityFilled);

                        tradeProfile.StopTicket.Cancel(); 
                       
                        tradeProfile.IsTradeFinished = true;
                    }
                }
            }
        }

        /// <summary>
        /// Marks all the trades as finished which are completed due to hitting the stop loss
        /// </summary>
        public void MarkStopTicketsFilled()
        {
            foreach (var tradeProfile in _tradeProfiles)
            {
                if (tradeProfile.StopTicket.Status == OrderStatus.Filled)
                {
                    tradeProfile.IsTradeFinished = true;
                }
            }
        }

        /// <summary>
        /// Removes all the completed trades from the trade profile list
        /// </summary>
        public void RemoveAllFinishedTrades()
        {
            _tradeProfiles = _tradeProfiles.Where(x => !x.IsTradeFinished).ToList();
        }
    }
}