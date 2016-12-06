using QuantConnect.Util;
using System;
using System.Collections.Generic;

namespace QuantConnect.Indicators
{
    /// <summary>
    /// Computes Ehler's Cyber Cycle indicator based on his EasyLanguage code 
    /// in Cybernetic Analysis for Stocks and Futures.  Figure 4.2 Page 34.
    /// </summary>
    public class CyberCycle : WindowIndicator<IndicatorDataPoint>
    {
        // the alpha for the formula
        double _alpha = 0.07;
        private readonly RollingWindow<double> _smooth;
        private readonly RollingWindow<double> _cycle;
        private readonly int _period;
        private int barcount;
        private readonly RollingWindow<double> _instPeriod;
        private readonly RollingWindow<double> _periodWindow;
        private readonly RollingWindow<double> _q1;
        private readonly RollingWindow<double> _i1;
        private readonly FixedSizeHashQueue<double> _delta;
        private readonly RollingWindow<double> _adaptCycle;

        /// <summary>
        /// Instanciates the indicator and the two Rolling Windows
        /// </summary>
        /// <param name="name">string - a custom name for your indicator</param>
        /// <param name="period">int - the number of bar in the RollingWindow histories</param>
        /// <remarks>Ehlers only uses the last 4 bars of the history, but he maintains a list
        /// of bars for 7 bars on both indicators.  I recommend a period of 7 and use IsReady to warm up your algo</remarks>
        public CyberCycle(string name, int period, double alpha)
            : base(name, period)
        {
            // Creates the smoother data set to which the resulting cybercycle is applied
            _smooth = new RollingWindow<double>(period);
            // CyberCycle history
            _cycle = new RollingWindow<double>(period);
            _instPeriod = new RollingWindow<double>(2);
            _q1 = new RollingWindow<double>(2);
            _i1 = new RollingWindow<double>(2);
            _delta = new FixedSizeHashQueue<double>(5);
            _periodWindow = new RollingWindow<double>(2);
            _adaptCycle = new RollingWindow<double>(2);
            _period = period;
            _alpha = alpha;
            barcount = 0;

        }
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="period">int - the number of periods in the indicator warmup</param>
        public CyberCycle(int period)
            : this("CCY" + period, period, 0.07)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool IsReady
        {
            get { return _smooth.IsReady; }
        }

        /// <summary>
        /// Computes the next value for the indicator
        /// </summary>
        /// <param name="price"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> price, IndicatorDataPoint input)
        {

            if (barcount > 2)
            {
                //Smooth = (Price + 2 * Price[1] + 2 * Price[2] + Price[3]) / 6;
                _smooth.Add((double)(price[0].Value + 2 * price[1].Value + 2 * price[2].Value + price[3].Value / 6));

                //If currentbar < 7 then Cycle = (Price - 2 * Price[1] + Price[2]) / 4;
                //If currentbar <7 then AdaptCycle=(Price-2*Price[1]+Price[2])/4;
                if (barcount < _period)
                {
                    _cycle.Add((double)(price[0].Value - 2 * price[1].Value + price[2].Value) / 4);
                    _adaptCycle.Add((double)(price[0].Value - 2 * price[1].Value + price[2].Value) / 4);
                }
                else
                {
                    //Cycle = (1 - .5 * alpha) * (1 - .5 * alpha) * (Smooth - 2 * Smooth[1] + Smooth[2]) 
                    //+ 2 * (1 - alpha) * Cycle[1] - (1 - alpha) * (1 - alpha) * Cycle[2];
                    _cycle.Add((1 - 0.5 * _alpha) * (1 - 0.5 * _alpha) * (_smooth[0] - 2 * _smooth[1] + _smooth[2])
                             + 2 * (1 - _alpha) * _cycle[0] - (1 - _alpha) * (1 - _alpha) * _cycle[1]);

                    //Q1 = (.0962 * Cycle + .5769 * Cycle[2] - .5769 * Cycle[4] - .0962 * Cycle[6]) * (.5 + .08 * InstPeriod[1]);
                    _q1.Add((.0962 * _cycle[0] + .5769 * _cycle[2] - .5769 * _cycle[4] - .0962 * _cycle[6]) * (.5 + .08 * _instPeriod[1]));
                    //I1 = Cycle[3];
                    _i1.Add(_cycle[3]);

                    double newDelta = 0;
                    //If Q1 <> 0 and Q1[1] <> 0 then DeltaPhase = (I1 / Q1 - I1[1] / Q1[1]) / (1 + I1 * I1[1] / (Q1 * Q1[1]));       
                    if (_q1[0] != 0 && _q1[1] != 0) { newDelta = ((_i1[0] / _q1[0] - _i1[1] / _q1[1]) / (1 + _i1[0] * _i1[1] / (_q1[0] * _q1[1]))); }

                    //If DeltaPhase < 0.1 then DeltaPhase = 0.1;
                    //If DeltaPhase > 1.1 then DeltaPhase = 1.1;
                    if (newDelta < 0.1) { newDelta = 0.1; };
                    if (newDelta > 1.1) { newDelta = 1.1; };

                    _delta.Add(newDelta);

                    //MedianDelta = Median(DeltaPhase, 5);
                    var median = _delta.Median<double>();

                    //If MedianDelta = 0 then DC = 15 else DC = 6.28318 / MedianDelta + .5;
                    double dc = 0;
                    if (median == 0) { dc = 15; } else { dc = 6.28318 / median + .5; }

                    //InstPeriod = .33 * DC + .67 * Instperiod[1];
                    _instPeriod.Add(.33 * dc + .67 * _instPeriod[1]);
                    //Period = .15 * InstPeriod + .85 * Period[1];
                    _periodWindow.Add(.15 * _instPeriod[0] + .85 * _periodWindow[1]);
                    //alpha1 = 2 / (Period + 1);
                    var alpha1 = 2 / (_periodWindow[0] + 1);
                    //If currentbar < 7 then AdaptCycle = (Price - 2 * Price[1] + Price[2]) / 4;

                    //AdaptCycle = (1 - .5 * alpha1) * (1 - .5 * alpha) * (Smooth - 2 * Smooth[1] + Smooth[2]) + 2 * (1 - alpha1) * AdaptCycle[1] - (1 - alpha1) * (1 - alpha1) * AdaptCycle[2];
                    _adaptCycle.Add((1 - .5 * alpha1) * (1 - .5 * _alpha) * (_smooth[0] - 2 * _smooth[1] + _smooth[2]) + 2 * (1 - alpha1) * _adaptCycle[0] - (1 - alpha1) * (1 - alpha1) * _adaptCycle[1]);
                }

            }
            else
            {
                _smooth.Add((double)price[0].Value);
                _cycle.Add(0);
                _adaptCycle.Add(0);
                _instPeriod.Add(0);
                _q1.Add(0);
                _i1.Add(0);
                _periodWindow.Add(0);
            }

            barcount++;
            return (decimal)_adaptCycle[0];
        }

    }
}
