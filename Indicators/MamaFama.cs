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

namespace QuantConnect.Indicators
{

    public class MamaFama : Indicator
    {

        #region Declarations
        double _fast;		// fast parameter
        double _slow;	// slow parameter
        const int window = 7;
        private readonly RollingWindow<double> _price = new RollingWindow<double>(4);
        private readonly RollingWindow<double> _smooth = new RollingWindow<double>(window);
        private readonly RollingWindow<double> _period = new RollingWindow<double>(2);
        private readonly RollingWindow<double> _detrender = new RollingWindow<double>(window);
        private readonly RollingWindow<double> _q1 = new RollingWindow<double>(window);
        private readonly RollingWindow<double> _i1 = new RollingWindow<double>(window);
        private readonly RollingWindow<double> _q2 = new RollingWindow<double>(2);
        private readonly RollingWindow<double> _i2 = new RollingWindow<double>(2);
        double _re;
        double _im;
        private readonly RollingWindow<double> _phase = new RollingWindow<double>(2);
        public readonly RollingWindow<double> Mama = new RollingWindow<double>(2);
        public readonly RollingWindow<double> Fama = new RollingWindow<double>(2);
        #endregion

        public MamaFama(string name, double slow, double fast)
            : base(name)
        {
            _slow = slow;
            _fast = fast;

            //Warm up the variables
            for (int i = 0; i <= window; i++)
            {
                _period.Add(0.0);
                _smooth.Add(0.0);
                _detrender.Add(0.0);
                _q1.Add(0.0);
                _i1.Add(0.0);
                _q2.Add(0.0);
                _i2.Add(0.0);
                _phase.Add(0.0);
                Mama.Add(0.0);
                Fama.Add(0.0);
            }
        }

        public MamaFama(double slow, double fast)
            : this("MAMAFAMA" + slow + " " + fast, slow, fast)
        {
        }

        /// <summary>
        ///     Gets a flag indicating when this indicator is ready and fully initialized
        /// </summary>
        public override bool IsReady
        {
            get { return Mama.IsReady; }
        }

        /// <summary>
        ///     Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A new value for this indicator</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _price.Add((double)input.Value);
            if (!_price.IsReady) return 0;

            // MAMA and FAMA
            // *********************************************************************************************************
            double currentSmooth = (double)((4 * _price[0] + 3 * _price[1] + 2 * _price[2] + _price[3]) / 10);
            _smooth.Add(currentSmooth);
            double currentDetrender = (.0962 * _smooth[0] + .5769 * _smooth[2] - .5769 * _smooth[4] - .0962 * _smooth[6]) * (.075 * _period[1] + .54);
            _detrender.Add(currentDetrender);

            // Compute InPhase and Quadrature components
            _q1.Add((.0962 * _detrender[0] + .5769 * _detrender[2] - .5769 * _detrender[4] - .0962 * _detrender[6]) * (.075 * _period[1] + .54));
            _i1.Add(_detrender[3]);

            // Advance the phase of I1 and Q1 by 90 degrees
            double ji = (.0962 * _i1[0] + .5769 * _i1[2] - .5769 * _i1[4] - .0962 * _i1[6]) * (.075 * _period[1] + .54);
            double jq = (.0962 * _q1[0] + .5769 * _q1[2] - .5769 * _q1[4] - .0962 * _q1[6]) * (.075 * _period[1] + .54);

            // Phasor addition for 3 bar averaging
            double currentI2 = _i1[0] - jq;
            double currentQ2 = _q1[0] + ji;

            // Smooth the I and Q components before applying the discriminator
            _i2.Add(.2 * currentI2 + .8 * _i2[0]);
            _q2.Add(.2 * currentQ2 + .8 * _q2[0]);

            // Homodyne Discriminator
            double currentRe = _i2[0] * _i2[1] + _q2[0] * _q2[1];
            double currentIm = _i2[0] * _q2[1] - _q2[0] * _i2[1];
            _re = .2 * currentRe + .8 * _re;
            _im = .2 * currentIm + .8 * _im;
            double currentPeriod = 0;
            if (currentIm != 0 && currentRe != 0)
                currentPeriod = (2 * Math.PI) / Math.Atan(currentIm / currentRe);
            if (currentPeriod > 1.5 * _period[0])
                currentPeriod = 1.5 * _period[0];
            if (currentPeriod < .67 * _period[0])
                currentPeriod = .67 * _period[0];
            if (currentPeriod < 6)
                currentPeriod = 6;
            if (currentPeriod > 50)
                currentPeriod = 50;
            _period.Add(.2 * currentPeriod + .8 * _period[0]);

            if (_i1[0] != 0)
                _phase.Add(Math.Atan(_q1[0] / _i1[0]));
            double deltaPhase = _phase[1] - _phase[0];
            if (deltaPhase < 1)
                deltaPhase = 1;
            double alpha = _fast / deltaPhase;
            if (alpha < _slow)
                alpha = _slow;
            Mama.Add(alpha * _price[0] + (1 - alpha) * Mama[0]);
            Fama.Add(.5 * alpha * Mama[0] + (1 - .5 * alpha) * Fama[0]);

            return (decimal)Mama[0];

        }
    }
}