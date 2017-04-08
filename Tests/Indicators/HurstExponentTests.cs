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
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Indicators
{
    [TestFixture]
    public class HurstExponentTests
    {

        [Test]
        public void ComparesAgainstExternalData()
        {
            var hurst = new HurstExponent(350);
            foreach (var item in TestHelper.GetCsvFileStream("spy_10_min.txt"))
            {
                hurst.Update(Time.ParseDate(item["Date"]), decimal.Parse(item["Close"]));
            }

            Assert.AreEqual(HurstExponent.HurstExponentResult.GeometricBrownianMotion, hurst.CurrentResult());

            hurst = new HurstExponent("", 1750, 10);
            foreach (var item in TestHelper.GetCsvFileStream("frama.txt"))
            {
                hurst.Update(Time.ParseDate(item["Date"]), decimal.Parse(item["Close"]));
            }

            Assert.AreEqual(HurstExponent.HurstExponentResult.Trend, hurst.CurrentResult());

        }


        [Test]
        public void ComparesAgainstGeneratedData()
        {
            var rand = new Random();
            double[] samples = new double[5000];
            for (var i = 0; i < 5000; i++)
            {
                samples[i] = rand.NextDouble();
            }

            var gbm = CumulativeSum(samples).Select(c => c + 1000).Select(cc => Math.Log(cc));
            var mr = samples.Select(c => c + 1000).Select(cc => Math.Log(cc));
            var tr = CumulativeSum(samples.Select(c => c + 1)).Select(cc => cc + 1000).Select(ccc => Math.Log(ccc));

            var hurst = new HurstExponent(5000);

            foreach (var item in mr)
            {
                hurst.Update(new IndicatorDataPoint(DateTime.UtcNow, (decimal)item));
            }

            Assert.AreEqual(HurstExponent.HurstExponentResult.MeanReversion, hurst.CurrentResult());

            //foreach (var item in tr)
            //{
            //    hurst.Update(new IndicatorDataPoint(DateTime.UtcNow, (decimal)item));
            //}

            //Assert.AreEqual(HurstExponent.HurstExponentResult.MeanReversion, hurst.CurrentResult());
        }


        public static IEnumerable<double> CumulativeSum(IEnumerable<double> sequence)
        {
            double sum = 0;
            foreach (var item in sequence)
            {
                sum += item;
                yield return sum;
            }
        }

        [Test]
        public void ResetsProperly()
        {
            var hurst = new HurstExponent(10);
            hurst.Update(DateTime.Today, 1m);
            hurst.Update(DateTime.Today.AddSeconds(1), 2m);
            Assert.IsFalse(hurst.IsReady);

            hurst.Reset();
            TestHelper.AssertIndicatorIsInDefaultState(hurst);
        }
    }
}
