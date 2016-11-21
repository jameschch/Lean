using System.Collections.Generic;

namespace QuantConnect.Brokerages.Bitfinex.Rest
{
    public static class BtcInfo
    {

        public enum BitfinexUnauthenicatedCallsEnum
        {
            pubticker = 0,
            stats = 1,
            trades = 2,
        }

    }
}
