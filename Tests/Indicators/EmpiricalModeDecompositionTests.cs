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
using NUnit.Framework;
using QuantConnect.Indicators;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Data.Market;
using System.IO;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class EmpiricalModeDecompositionTests
    {
        //[Test]
        //public void ComputesCorrectly()
        //{
        //    var nu = new EmpiricalModeDecomposition("", 26, 0.4, 0.1m, 26);
        //    var old = new EmpiricalModeDecompositionOld();
        //    bool first = true;

        //    foreach (var line in File.ReadLines(Path.Combine("TestData", "spy_kama.txt")))
        //    {

        //        if (first)
        //        {
        //            first = false;
        //            continue;
        //        }

        //        string[] parts = line.Split(new[] { ',' }, StringSplitOptions.None);

        //        decimal price = decimal.Parse(parts[1]);

        //        var now = DateTime.UtcNow;
        //        nu.Update(new TradeBar { Time = now, High = price * 2, Low = 0 });
        //        var expected = old.IsCyclical(new TradeBar { Time = now, High = price * 2, Low = 0 });

        //        if (nu.IsReady && old.IsReady)
        //        {
        //            Assert.IsTrue((expected && nu > 0) || (!expected && nu < 0));
        //        }
        //    }
        //}

        [Test]
        public void ResetsProperly()
        {


        }

        //[Test]
        public void ComparesAgainstExternalData()
        {
            //var ema = new EmpiricalModeDecomposition(14);
            //TestHelper.TestIndicator(ema, "spy_with_indicators.txt", "EMA14", TestHelper.AssertDeltaDecreases(2.5e-2));
        }
    }
}
