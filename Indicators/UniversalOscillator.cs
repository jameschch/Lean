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
using QuantConnect.Data.Market;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// The Universal Oscillator by John Ehlers. This oscillator filters "pink noise" and the signal is then tranformed using automatic gain control (agc).
    /// </summary>
    public class UniversalOscillator : Indicator
    {
        private readonly int _bandEdge = 20;
        private readonly RollingWindow<double> _close;
        private readonly RollingWindow<double> _whiteNoise;
        private readonly RollingWindow<double> _filt;
        double _peak;
        double _a1;
        double _b1;
        double _c1;
        double _c2;
        double _c3;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalOscillator"/> class using the specified parameters
        /// </summary>
        /// <param name="bandEdge">The bandEdge</param>
        public UniversalOscillator(int bandEdge)
            : this(string.Format("UNIOSC({0})", bandEdge), bandEdge)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalOscillator"/> class using the specified parameters
        /// </summary>
        /// <param name="name">The name of this indicator</param>
        /// <param name="bandEdge">The bandEdge</param>
        public UniversalOscillator(string name, int bandEdge)
            : base(name)
        {
            _bandEdge = bandEdge;
            _close = new RollingWindow<double>(3);
            _whiteNoise = new RollingWindow<double>(2);
            _filt = new RollingWindow<double>(3) { 0, 0, 0 };

            // SuperSmoother Filter
            _a1 = Math.Exp(-1.414 * Math.PI / _bandEdge);
            _b1 = 2 * _a1 * Math.Cos(1.414 * Math.PI / _bandEdge);
            _c2 = _b1;
            _c3 = -_a1 * _a1;
            _c1 = 1 - _c2 - _c3;

        }

        /// <summary>
        /// Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return Samples > 3; }
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
           
            decimal universal = 0;

            _close.Add((double)input.Price);

            if (Samples == 1)
            {
                _close.Add(_close[0]);
                _close.Add(_close[0]);
            }

            _whiteNoise.Add((_close[0] - _close[2]) / 2);

            if (Samples == 1)
            {
                _whiteNoise.Add(_whiteNoise[0]);
            }

            _filt.Add(_c1 * (_whiteNoise[0] + _whiteNoise[1]) / 2.0 + _c2 * _filt[0] + _c3 * _filt[1]);

            if (Samples == 1)
            {
                _filt.Add(0);
            }
            else if (Samples == 2)
            {
                _filt.Add(_c1 * 0 * (_close[0] + _close[1]) / 2.0 + _c2 * _filt[0]);
            }
            else if (Samples == 3)
            {
                _filt.Add(_c1 * 0 * (_close[0] + _close[1]) / 2.0 + _c2 * _filt[0] + _c3 * _filt[1]);
            }

            // Automatic Gain Control (AGC)
            _peak = 0.991 * _peak;
            if (Samples == 1)
            {
                _peak = .0000001;
            }
            if (Math.Abs(_filt[0]) > _peak)
            {
                _peak = Math.Abs(_filt[0]);
            }
            if (_peak != 0)
            {
                System.Diagnostics.Debug.WriteLine(_filt[0] / _peak);
                universal = (decimal)(_filt[0] / _peak);
            }

            return universal;
        }

        /// <summary>
        /// Resets this indicator to its initial state
        /// </summary>
        public override void Reset()
        {
            _close.Reset();
            _whiteNoise.Reset();
            _filt.Reset();
            _peak = 0;
            base.Reset();
        }
    }
}
