using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    public class EmpiricalModeDecompositionOld
    {

        public static double DC_delta = 0.4;			// band sensitivity
        public static int DC_period = 13; 				// lags to consider
        public static decimal DC_frac = 0.1M; 			// fraction sensitivity
        public static int MESA_length = 13;
        private readonly RollingWindow<TradeBar> Closes = new RollingWindow<TradeBar>(MESA_length);
        private readonly RollingWindow<double> BPHist = new RollingWindow<double>(DC_period);
        private readonly RollingWindow<double> PeakHist = new RollingWindow<double>(25);
        private readonly RollingWindow<double> ValleyHist = new RollingWindow<double>(25);
        private readonly SimpleMovingAverage MA_BP = new SimpleMovingAverage(2 * DC_period);
        private readonly SimpleMovingAverage MA_Peak = new SimpleMovingAverage(12);
        private readonly SimpleMovingAverage MA_Valley = new SimpleMovingAverage(12);
        decimal _price;

        public bool IsReady
        {
            get
            {
                return Closes.IsReady;
            }
        }

        public EmpiricalModeDecompositionOld()
        {
            PeakHist.Add(0.0);	 // add one empty Peak entry
            ValleyHist.Add(0.0); // add one empty Valley entry
        }

        public void OnData(TradeBars data)
        {
            // ignore this for now
        }

        public bool IsCyclical(TradeBar data)
        {
            Closes.Add(data);
            _price = data.Close;
            if (!Closes.IsReady) return false;

            // Empirical Mode Decomposition
            // *********************************************************************************************************
            double beta = Math.Cos(2 * Math.PI / DC_period);
            double gamma = (1 / Math.Cos(4 * Math.PI * DC_delta / DC_period));
            double alpha = gamma - Math.Sqrt(Math.Pow(gamma, 2) - 1);
            double p0 = (double)((decimal)Closes[0].High + (decimal)Closes[0].Low) / 2;
            double p2 = (double)((decimal)Closes[2].High + (decimal)Closes[2].Low) / 2;
            double BP = 0;
            if (BPHist.IsReady)
            {
                BP = 0.5 * (1 - alpha) * (p0 - p2) + beta * (1 + alpha) * BPHist[0] - alpha * BPHist[1];
            }
            else
            {
                BP = 0.5 * (1 - alpha) * (p0 - p2);
            }
            // update data
            BPHist.Add(BP); // so current BP is now [0]
            MA_BP.Update(new IndicatorDataPoint(data.Time, (decimal)BP));
            decimal Mean = MA_BP;

            if (!BPHist.IsReady) return false;

            // calculate peak and valley
            double Peak = PeakHist[0];
            double Valley = ValleyHist[0];
            if (BPHist[1] > BPHist[0] && BPHist[1] > BPHist[2])
            {
                Peak = BPHist[1];
            }
            if (BPHist[1] < BPHist[0] && BPHist[1] < BPHist[2])
            {
                Valley = BPHist[1];
            }
            // update data
            PeakHist.Add(Peak);		// so current Peak is now [0]
            ValleyHist.Add(Valley);	// so current Valley is now [0]
            MA_Peak.Update(new IndicatorDataPoint(data.Time, (decimal)Peak));
            MA_Valley.Update(new IndicatorDataPoint(data.Time, (decimal)Valley));

            if (!MA_Peak.IsReady)
                return false;

            decimal AvgPeak = MA_Peak;
            decimal AvgValley = MA_Valley;

            // calculate final indicators
            decimal upperBand = DC_frac * AvgPeak;
            decimal lowerBand = DC_frac * AvgValley;

            return Mean > lowerBand && Mean < upperBand;

        }
    }
}
