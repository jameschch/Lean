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
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Indicators
{
    public class HurstExponent : IndicatorBase<IndicatorDataPoint>
    {

        int _period;
        int _lagVector;
        int[] _lag;
        FixedSizeHashQueue<double> p;
        decimal _hurst;

        public enum HurstExponentResult
        {
            GeometricBrownianMotion,
            MeanReversion,
            Trend,
            StrongTrend,
            None
        }

        public HurstExponent(int period)
            : this(string.Format("Hurst({0})", period), period)
        {
        }

        public HurstExponent(string name, int period, int lagVector = 100)
            : base(name)
        {
            if (period <= lagVector)
            {
                throw new ArgumentException("The period must be greater than lag vector.");
            }

            _period = period;
            _lagVector = lagVector;

            _lag = new int[lagVector - 2];

            for (int i = 2; i < lagVector; i++)
            {
                _lag[i - 2] = i;
            }

            p = new FixedSizeHashQueue<double>(period);
        }

        public override bool IsReady
        {
            get
            {
                return p.Count() >= _period;
            }
        }

        public HurstExponentResult CurrentResult()
        {
            if (_hurst >= 0.4m && _hurst <= 0.6m)
            {
                return HurstExponentResult.GeometricBrownianMotion;
            }
            else if (_hurst > -0.1m && _hurst < 0.1m)
            {
                return HurstExponentResult.MeanReversion;
            }
            else if (_hurst >= 0.9m)
            {
                return HurstExponentResult.StrongTrend;
            }
            else if (_hurst >= 0.6m)
            {
                return HurstExponentResult.Trend;
            }

            return HurstExponentResult.None;
        }

        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            p.Add((double)input.Price);

            this.Current.Symbol = input.Symbol;

            if (!this.IsReady) { return 0; }

            var tau = new List<double>();

            foreach (var item in _lag)
            {
                var start = p.Skip(item).ToArray();
                var end = p.Take(p.Count() - item).ToArray();

                var pp = new double[start.Length];
                for (int ii = 0; ii < start.Count(); ii++)
                {
                    pp[ii] = start[ii] - end[ii];
                }

                tau.Add(Math.Sqrt(pp.PopulationStandardDeviation()));
            }

            var x = _lag.Select(l => Math.Log(l)).ToArray();
            var y = tau.Select(t => Math.Log(t)).ToArray();
            var fit = Fit.Polynomial(x, y, 1);

            if (double.IsNaN(fit[1]) || double.IsInfinity(fit[1]))
            {
                return -10m;
            }

            _hurst = (decimal)(fit[1] * 2.0);

            return _hurst;
        }
    }
}
