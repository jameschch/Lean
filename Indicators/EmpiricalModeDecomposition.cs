using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Indicators
{
    
    /// <summary>
    /// The Empirical Mode Decomposition by John Ehlers utilizies a BandPass filter to determine if the market ove a given period is cyclical (1) or trending (-1).
    /// </summary>
    public class EmpiricalModeDecomposition : Indicator
    {

        #region declarations
        double _delta = 0.4; // band sensitivity
        int _period = 26; // lags to consider
        decimal _fraction = 0.1m; // fraction sensitivity
        int _mesaLength = 26;
        private readonly RollingWindow<double> _price;
        private readonly RollingWindow<double> _bandPass;
        private readonly RollingWindow<double> _peak;
        private readonly RollingWindow<double> _valley;
        private readonly SimpleMovingAverage _bandPassSma;
        private readonly SimpleMovingAverage _peakSma;
        private readonly SimpleMovingAverage _valleySma;
        double _beta;
        double _gamma;
        double _alpha;
        #endregion

        /// <summary>
        /// Returns whether the indicator is ready to return valid results
        /// </summary>
        public override bool IsReady
        {
            get
            {
                return _price.IsReady && _bandPass.IsReady && _peakSma.IsReady;
            }
        }

        /// <summary>
        /// Creates a new instance of the indicator
        /// </summary>
        /// <param name="name"></param>
        /// <param name="period"></param>
        public EmpiricalModeDecomposition(string name, int period)
            : this(name, period, 0.4, 0.1m, period)
        {

        }

        /// <summary>
        /// Creates a new instance of the indicator
        /// </summary>
        /// <param name="name"></param>
        /// <param name="period"></param>
        /// <param name="delta"></param>
        /// <param name="fraction"></param>
        /// <param name="mesaLength"></param>
        public EmpiricalModeDecomposition(string name, int period, double delta, decimal fraction, int mesaLength)
            : base(name)
        {
            _period = period;
            _delta = delta;
            _fraction = fraction;
            _mesaLength = mesaLength;

            _price = new RollingWindow<double>(_mesaLength);
            _bandPass = new RollingWindow<double>(_period);
            _peak = new RollingWindow<double>(25) { 0 };
            _valley = new RollingWindow<double>(25) { 0 };
            _bandPassSma = new SimpleMovingAverage(2 * _period);
            _peakSma = new SimpleMovingAverage(12);
            _valleySma = new SimpleMovingAverage(12);
            _beta = Math.Cos(2 * Math.PI / _period);
            _gamma = (1 / Math.Cos(4 * Math.PI * _delta / _period));
            _alpha = _gamma - Math.Sqrt(Math.Pow(_gamma, 2) - 1);
        }

        /// <summary>
        /// Computes the next value of this indicator from the given state
        /// </summary>
        /// <param name="input">The input given to the indicator</param>
        /// <returns>A positive value if market is cyclical. A negative value indicates trending</returns>
        protected override decimal ComputeNextValue(IndicatorDataPoint input)
        {
            _price.Add((double)input.Price);

            if (!_price.IsReady) return 0;

            //Calculate band pass
            double bp = 0;
            if (_bandPass.IsReady)
            {
                bp = 0.5 * (1 - _alpha) * (_price[0] - _price[2]) + _beta * (1 + _alpha) * _bandPass[0] - _alpha * _bandPass[1];
            }
            else
            {
                bp = 0.5 * (1 - _alpha) * (_price[0] - _price[2]);
            }

            _bandPass.Add(bp);
            _bandPassSma.Update(new IndicatorDataPoint(input.Time, (decimal)bp));

            if (!_bandPass.IsReady) return 0;

            // calculate peak and valley
            double currentPeak = _peak[0];
            double currentValley = _valley[0];
            if (_bandPass[1] > _bandPass[0] && _bandPass[1] > _bandPass[2])
            {
                currentPeak = _bandPass[1];
            }
            if (_bandPass[1] < _bandPass[0] && _bandPass[1] < _bandPass[2])
            {
                currentValley = _bandPass[1];
            }

            _peak.Add(currentPeak);
            _valley.Add(currentValley);
            _peakSma.Update(new IndicatorDataPoint(input.Time, (decimal)currentPeak));
            _valleySma.Update(new IndicatorDataPoint(input.Time, (decimal)currentValley));

            if (!_peakSma.IsReady) return 0;

            // calculate final indicators
            decimal upperBand = _fraction * _peakSma;
            decimal lowerBand = _fraction * _valleySma;

            return _bandPassSma > lowerBand && _bandPassSma < upperBand ? 1 : -1;
        }

        /// <summary>
        /// Resets the indicator to initial state
        /// </summary>
        public override void Reset()
        {
            _price.Reset();
            _bandPass.Reset();
            _peak.Reset();
            _valley.Reset();
            _bandPassSma.Reset();
            _peakSma.Reset();
            _valleySma.Reset();
            base.Reset();
        }

    }
}
