using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.OneBroker
{

    /// <summary>
    /// https://1broker.com/?c=cfds
    /// </summary>
    public class OneBrokerSymbol
    {

        public string Symbol { get; set; }
        public string BrokerSymbol { get; set; }
        public decimal Leverage { get; set; }
        public decimal Maximum { get; set; }
        public SecurityType SecurityType { get; set; }

    }


}
