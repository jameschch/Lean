using QuantConnect.Orders;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    public class TradeProfile
    {

        //Ticket tracking the open order
        public OrderTicket OpenTicket, StopTicket, ExitTicket;
        //Keeps track of the current price and the direction of the trade
        public decimal CurrentPrice;
        public int TradeDirection;
        public Symbol TradeSymbol;
        private decimal _risk;
        private int _maximumTradeQuantity;
        protected decimal _volatility;

        // Calclate the quantity based on the target risk in dollars.
        public int Quantity
        {
            get
            {
                long quantity = (long)(_risk / _volatility);
                if (quantity > _maximumTradeQuantity) return _maximumTradeQuantity;
                return (int)quantity;
            }
        }

        //What is the stoploss move from current price
        public decimal DeltaStopLoss
        {
            get
            {
                return _risk / Quantity;
            }
        }

        /// <summary>
        /// Calculates  the Profit:Loss ratio
        /// </summary>
        public decimal ProfitLossRatio
        {
            get
            {
                if (OpenTicket != null)
                {
                    return OpenTicket.Quantity * (CurrentPrice - OpenTicket.AverageFillPrice) / _risk;
                }
                return 0m;
            }
        }

        /// <summary>
        /// Exit signal for each trade
        /// </summary>
        public Func<int> ExitSignal { get; set; }
        public bool IsTradeFinished { get; set; }


        /// <summary>
        /// Create a new tradeProfile and limit the maximum risk.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="volatility"></param>
        /// <param name="risk"></param>
        /// <param name="currentPrice"></param>
        /// <param name="maximumTradeSize"></param>
        /// <param name="exitSignal"></param>
        public TradeProfile(Symbol symbol, decimal volatility, decimal risk, decimal currentPrice, decimal maximumTradeSize, Func<int> exitSignal)
        {
            TradeSymbol = symbol;
            _volatility = volatility;
            _risk = risk;
            CurrentPrice = currentPrice;
            _maximumTradeQuantity = (int)(maximumTradeSize / CurrentPrice);
            ExitSignal = exitSignal;
        }
    }
}