using System;

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
        /// <param name="window"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            // for convenience
            var time = input.Time;

            if (barcount > 2)
            {
                _smooth.Add((double)(window[0].Value + 2 * window[1].Value + 2 * window[2].Value + window[3].Value / 6));

                if (barcount < _period)
                {
                    _cycle.Add((double)(window[0].Value - 2 * window[1].Value + window[2].Value) / 4);
                }
                else
                {
                    // Calc the high pass filter _cycle value
                    var hfp = (1 - 0.5 * _alpha) * (1 - 0.5 * _alpha) * (_smooth[0] - 2 * _smooth[1] + _smooth[2])
                             + 2 * (1 - _alpha) * _cycle[0] - (1 - _alpha) * (1 - _alpha) * _cycle[1];
                    _cycle.Add(hfp);
                }
            }
            else
            {
                _smooth.Add((double)window[0].Value);
                _cycle.Add(0);
            }
            barcount++;
            return (decimal)_cycle[0];
        }

    }
}
