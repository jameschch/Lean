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

using Moq;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Securities;
using QuantConnect.Securities.Forex;
using System;

namespace QuantConnect.Tests.Brokerages
{
    [TestFixture]
    public class DefaultBrokerageModelTests
    {

        [Test]
        public void CanSubmitOrderValidateQuantityTest()
        {
            var unit = new DefaultBrokerageModel();
            var symbol = new Mock<SymbolProperties>(null, "USD", 0m, 0m, 1m);
            var cash = new Mock<Cash>("USD", 0m, 0m);
            var config = new Mock<SubscriptionDataConfig>(typeof(object), Symbol.Create("APL", SecurityType.Equity, Market.USA), Resolution.Tick, DateTimeZone.Utc, DateTimeZone.Utc,
                false, false, false, false, TickType.Quote, false);

            var security = new Mock<Securities.Security>(null, config.Object, cash.Object, symbol.Object);
            var order = new Orders.MarketOrder
            {
                Quantity = 1.1m
            };
            BrokerageMessageEvent msg = null;

            bool actual = unit.CanSubmitOrder(security.Object, order, out msg);
            Assert.IsFalse(actual);
            Assert.IsNotNull(msg);

            msg = null;
            order.Quantity = 1m;
            actual = unit.CanSubmitOrder(security.Object, order, out msg);
            Assert.IsTrue(actual);
            Assert.IsNull(msg);

            msg = null;
            order.Quantity = -1.1m;
            actual = unit.CanSubmitOrder(security.Object, order, out msg);
            Assert.IsFalse(actual);
            Assert.IsNotNull(msg);

            msg = null;
            order.Quantity = -1.0m;
            actual = unit.CanSubmitOrder(security.Object, order, out msg);
            Assert.IsTrue(actual);
            Assert.IsNull(msg);
        }

        [Test]
        public void CanSubmitOrderValidateQuantityFractionalTest()
        {
            var unit = new DefaultBrokerageModel();
            var symbol = new Mock<SymbolProperties>(null, "USD", 0m, 0m, 0.01m);
            var cash = new Mock<Cash>("USD", 0m, 0m);
            var config = new Mock<SubscriptionDataConfig>(typeof(object), Symbol.Create("APL", SecurityType.Equity, Market.USA), Resolution.Tick, DateTimeZone.Utc, DateTimeZone.Utc,
                false, false, false, false, TickType.Quote, false);

            var security = new Mock<Securities.Security>(null, config.Object, cash.Object, symbol.Object);
            var order = new Orders.MarketOrder
            {
                Quantity = 1.1m
            };
            BrokerageMessageEvent msg = null;

            bool actual = unit.CanSubmitOrder(security.Object, order, out msg);
            Assert.IsTrue(actual);
            Assert.IsNull(msg);

            msg = null;
            order.Quantity = 1m;
            actual = unit.CanSubmitOrder(security.Object, order, out msg);
            Assert.IsTrue(actual);
            Assert.IsNull(msg);

            msg = null;
            order.Quantity = -1.1m;
            actual = unit.CanSubmitOrder(security.Object, order, out msg);
            Assert.IsTrue(actual);
            Assert.IsNull(msg);

            msg = null;
            order.Quantity = -1.01m;
            actual = unit.CanSubmitOrder(security.Object, order, out msg);
            Assert.IsTrue(actual);
            Assert.IsNull(msg);

            msg = null;
            order.Quantity = 1.001m;
            actual = unit.CanSubmitOrder(security.Object, order, out msg);
            Assert.IsFalse(actual);
            Assert.IsNotNull(msg);

        }

    }
}
