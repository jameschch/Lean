﻿/*
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

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Custom.TradingEconomics;
using QuantConnect.Data.UniverseSelection;
using System;
using System.Linq;

namespace QuantConnect.Tests.Common.Data.Custom
{
    [TestFixture]
    public class TradingEconomicsTests
    {
        public const string TestCalendarJson =
@"[{
  ""CalendarId"": ""0"",
  ""Date"": ""2019-01-01T00:00:00"",
  ""Country"": ""United States"",
  ""Category"": ""PPI PCE"",
  ""Event"": ""PPI PCE YoY"",
  ""Reference"": ""Jan"",
  ""Source"": ""U.S."",
  ""Actual"": 0,
  ""Previous"": 0,
  ""Forecast"": null,
  ""TEForecast"": 0,
  ""DateSpan"": ""0"",
  ""Importance"": 2,
  ""LastUpdate"": ""2019-01-01T00:00:00"",
  ""Revised"": 0,
  ""OCountry"": ""United States"",
  ""OCategory"": ""PPI PCE"",
  ""Ticker"": ""US"",
  ""Symbol"": ""US"",
  ""IsPercentage"": true,
  ""DataType"": 0,
  ""IsFillForward"": false,
  ""Time"": ""0001-01-01T00:00:00"",
  ""Value"": 0.0,
  ""Price"": 0.0
}]";

        [TestCase("EarnInGs n.s.a", "earnings not seasonally adjusted")]
        [TestCase("                 GDP", "gdp")]
        [TestCase("  GDP  ", "gdp")]
        [TestCase("GDP 1st est", "gdp first estimate")]
        [TestCase("GDP PPI", "gdp producer price index")]
        [TestCase("GDP PPI 1st EsT", "gdp producer price index first estimate")]
        [TestCase("G.d.P. P.P.I. 1st EsT .... n.s.a", "gdp producer price index first estimate not seasonally adjusted")]
        public void CalendarFilterAppliedCorrectly(string eventName, string expected)
        {
            var filteredName = TradingEconomicsEventFilter.FilterEvent(eventName);

            Assert.AreEqual(expected, filteredName);
        }

        [Test]
        public void DeserializesProperly()
        {
            var instance = new TradingEconomicsCalendar();
            var result = instance.Reader(
                new SubscriptionDataConfig(
                    typeof(TradingEconomicsCalendar),
                    Symbol.CreateBase(typeof(TradingEconomicsCalendar), Symbol.None, QuantConnect.Market.USA),
                    Resolution.Daily,
                    TimeZones.Utc,
                    TimeZones.Utc,
                    false,
                    false,
                    false,
                    isCustom: true
                ),
                TestCalendarJson,
                new DateTime(2019, 1, 1),
                false
            );

            var calendar = (TradingEconomicsCalendar)((BaseDataCollection)result).Data.Single();

            Assert.AreEqual("0", calendar.CalendarId);
            Assert.AreEqual(new DateTime(2019, 1, 1), calendar.EndTime.Date);
            Assert.AreEqual("United States", calendar.Country);
            Assert.AreEqual("PPI PCE", calendar.Category);
            Assert.AreEqual("producer price index personal consumption expenditure price index yoy", calendar.Event);
            Assert.AreEqual("Jan", calendar.Reference);
            Assert.AreEqual("U.S.", calendar.Source);
            Assert.AreEqual(0m, calendar.Actual);
            Assert.AreEqual(0m, calendar.Previous);
            Assert.AreEqual(null, calendar.Forecast);
            Assert.AreEqual(0m, calendar.TradingEconomicsForecast);
            Assert.AreEqual("0", calendar.DateSpan);
            Assert.AreEqual(TradingEconomicsImportance.High, calendar.Importance);
            Assert.AreEqual(new DateTime(2019, 1, 1), calendar.LastUpdate.Date);
            Assert.AreEqual(0m, calendar.Revised);
            Assert.AreEqual("United States", calendar.OCountry);
            Assert.AreEqual("PPI PCE", calendar.OCategory);
            Assert.AreEqual("US", calendar.Ticker);
            Assert.AreEqual(true, calendar.IsPercentage);
        }
    }
}
