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

    public abstract class BaseForex : BaseBitcoin
    {

        RollingWindow<decimal> unrealizedProfit = new RollingWindow<decimal>(2);
        protected override Decimal MinimumPosition { get { return Decimal.MinValue; } }

        public BaseForex() : base(false)
        { }


        public override void Initialize()
        {
            SetCash("USD", 1000, 1m);

            SetBrokerageModel(BrokerageName.OandaBrokerage, AccountType.Margin);

            var bench = AddForex("EURUSD", Resolution.Minute, Market.Oanda);
            SetBenchmark(bench.Symbol);

        }

        public override void OnData(Tick data)
        { }

        protected override void Output(string title)
        {
            Log(title + ": " + this.UtcTime.ToString() + ": " + Portfolio.Securities.First().Value.Price.ToString() 
                + " Trade:" + Math.Round(Portfolio.First().Value.LastTradeProfit, 2)
                + " Total:" + Math.Round(Portfolio.TotalPortfolioValue, 2));
        }


    }
}
