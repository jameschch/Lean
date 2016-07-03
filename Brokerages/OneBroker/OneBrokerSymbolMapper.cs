using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages.OneBroker
{

    //todo: add symbols
    public class OneBrokerSymbolMapper : ISymbolMapper
    {

        public string GetBrokerageSymbol(Symbol symbol)
        {
            return _symbolList.Where(s => s.Symbol == symbol.Value).Single().BrokerSymbol;
        }

        public SecurityType GetSecurityType(string brokerSymbol)
        {
            return _symbolList.Where(s => s.BrokerSymbol == brokerSymbol).Single().SecurityType;
        }

        public Symbol GetLeanSymbol(string brokerageSymbol)
        {
            var match = _symbolList.Where(s => s.BrokerSymbol == brokerageSymbol).Single();
            return Symbol.Create(match.Symbol, match.SecurityType, Market.OneBroker);
        }

        public Symbol GetLeanSymbol(string brokerageSymbol, SecurityType securityType, string market)
        {
            return Symbol.Create(_symbolList.Where(s => s.BrokerSymbol == brokerageSymbol).Single().Symbol, securityType, market);
        }

        private static readonly List<OneBrokerSymbol> _symbolList = new List<OneBrokerSymbol>
        {
            new OneBrokerSymbol
            {
                 BrokerSymbol = "BTCUSD", Symbol = "BTCUSD", Leverage = 5, Maximum = 20, SecurityType = SecurityType.Forex
            },
            new OneBrokerSymbol
            {
                 BrokerSymbol = "GOLD", Symbol = "GOLD", Leverage = 50, Maximum = 300, SecurityType = SecurityType.Base
            }

        };

    }
}
