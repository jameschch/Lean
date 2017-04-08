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
using QuantConnect.Orders;

namespace QuantConnect.Securities
{
    /// <summary>
    /// Represents a simple, constant margining model by specifying the percentages of required margin.
    /// </summary>
    public class BitfinexSecurityMarginModel : SecurityMarginModel
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityMarginModel"/>
        /// </summary>
        /// <remarks>Bitfinex will not liquidate a position at maximum leverage unless debt/equity ratio limit of 15% is reached.</remarks>
        public BitfinexSecurityMarginModel() : base(0.33m, 0.01m)
        {

        }

        /// <summary>
        /// Generates a new order for the specified security taking into account the total margin
        /// used by the account. Returns null when no margin call is to be issued.
        /// </summary>
        /// <param name="security">The security to generate a margin call order for</param>
        /// <param name="totalPortfolioValue">ignored</param>
        /// <param name="totalMargin">ignored</param>
        /// <returns>An order object representing a liquidation order to be executed to bring the account within margin requirements</returns>
        public override SubmitOrderRequest GenerateMarginCallOrder(Security security, decimal totalPortfolioValue, decimal totalMargin)
        {
            const decimal maximumShortRatio = 1.45m;

            if (!security.Holdings.Invested)
            {
                return null;
            }

            if (security.QuoteCurrency.ConversionRate == 0m)
            {
                // check for div 0 - there's no conv rate, so we can't place an order
                return null;
            }


            //Will force liquidate when Ticker to Position price ratio > 1.5           
            decimal ratio = security.Holdings.Price / security.Holdings.AveragePrice;
            if ((security.Holdings.IsShort && ratio < maximumShortRatio) || (security.Holdings.IsLong && ratio > 0.75m))
            {
                return null;
            }

            var delta = security.Holdings.IsShort ? ratio - maximumShortRatio : 0.75m - ratio;

            var quantity = security.Holdings.AbsoluteQuantity * Math.Abs(delta);
            quantity = Math.Max(security.SymbolProperties.LotSize, Math.Min(security.Holdings.AbsoluteQuantity, quantity));
            quantity = Math.Round(quantity, BitConverter.GetBytes(decimal.GetBits(security.SymbolProperties.LotSize)[3])[2], MidpointRounding.AwayFromZero);

            // don't try and liquidate more share than we currently hold, minimum value of lot size, maximum value for absolute quantity
            if (security.Holdings.IsLong)
            {
                // adjust to a sell for long positions
                quantity *= -1;
            }

            Logging.Log.Debug("margin call was attempted");

            return new SubmitOrderRequest(OrderType.Market, security.Type, security.Symbol, quantity, 0, 0, DateTime.UtcNow, "Margin Call");
        }

        /// <summary>
        /// Gets the margin currently alloted to the specified holding
        /// </summary>
        /// <param name="security">The security to compute maintenance margin for</param>
        /// <returns>The maintenance margin required for the </returns>
        public override decimal GetMaintenanceMargin(Security security)
        {
            return security.Holdings.AbsoluteHoldingsValue * GetMaintenanceMarginRequirement(security) - security.Holdings.UnrealizedProfit;
        }

        private decimal DebtToEquityRatio(Security security)
        {
            decimal ratio = security.Holdings.Price / security.Holdings.AveragePrice;
            return ratio;
        }

    }
}