using QuantConnect.Brokerages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
    public abstract class BaseOK : BaseBitcoin
    {

        public override void Initialize()
        {
            SetCash("USD", 500, 1m);
            SetBrokerageModel(BrokerageName.OKCoin, AccountType.Cash);
            var security = AddSecurity(SecurityType.Forex, BTCUSD, Resolution.Tick, Market.OKCoin, false, 1m, false);
            SetCash("BTC", 1, 608);
            SetBenchmark(security.Symbol);
            var date = new DateTime(2016, 9, 10);
            SetStartDate(date);
            SetEndDate(new DateTime(2016, 10, 20));
        }

        public abstract override void OnData(Tick data);

    }
}
