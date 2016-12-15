using QuantConnect.Orders;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Algorithm.CSharp
{
    public class RiskModel
    {

        private decimal _risk;
        Security _security;

        // Calclate the quantity based on the target risk in dollars.
        public decimal Quantity()
        {
            if (!_security.VolatilityModel.IsReady || _security.VolatilityModel.Volatility == 0)
            {
                return 0;
            }

            var quantity = _risk / _security.VolatilityModel.Volatility;

            return quantity;
        }

        //What is the stoploss move from current price
        public decimal StopLossDelta()
        {
            if (!_security.VolatilityModel.IsReady || Quantity() == 0)
            {
                return 0;
            }

            return _risk / Quantity();
        }

        public decimal StopLossPrice(OrderDirection direction)
        {
            return (direction == OrderDirection.Buy ? _security.Price - StopLossDelta() : _security.Price + StopLossDelta());
        }

        /// <summary>
        /// Calculates  the Profit:Loss ratio
        /// </summary>
        public decimal ProfitLossRatio(OrderTicket ticket)
        {

            if (ticket != null)
            {
                return ticket.Quantity * (_security.Price - ticket.AverageFillPrice) / _risk;
            }
            return 0m;

        }

        /// <summary>
        /// Create a new tradeProfile and limit the maximum risk.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="volatility"></param>
        /// <param name="risk"></param>
        /// <param name="currentPrice"></param>
        /// <param name="maximumTradeSize"></param>
        /// <param name="exitSignal"></param>
        public RiskModel(Security security, decimal risk)
        {
            _security = security;
            _risk = risk;
        }
    }
}