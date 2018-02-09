using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfocalControlLibrary
{
    public class Scan
    {
        #region Constructor
        public Scan(double x0, double y0, double width, double height, int pixels, double dwellTimeInMs,
            ScanType type = ScanType.Ramp, double samplingFrequency = 5000)
            : this(x0, y0, width, height, width / pixels, dwellTimeInMs, type, samplingFrequency)
        {
        }

        protected Scan(double x0, double y0, double width, double height, double dx, double dwelltime_in_ms,
            ScanType type = ScanType.Ramp, double samplingFrequency = 5000)
        {
            X0 = x0;
            Y0 = y0;
            Width = width;
            Height = height;
            Dx = dx;
            DwellTime = dwelltime_in_ms;
            Type = type;
            SamplingFrequency = samplingFrequency;
        }
        #endregion

        #region Properties
        
        /// <summary>
        /// the x start point
        /// </summary>
        private double m_x0;

        public double X0
        { get { return m_x0; } set { Invalidate(); m_x0 = value; } }

        /// <summary>
        /// the y start point
        /// </summary>
        private double m_y0;

        public double Y0
        { get { return m_y0; } set { Invalidate(); m_y0 = value; } }

        /// <summary>
        /// the length of x in the matrix grid
        /// </summary>
        private double m_width;

        public double Width
        { get { return m_width; } set { Invalidate(); m_width = value; } }

        /// <summary>
        /// the length of y in the matrix grid
        /// </summary>
        private double m_height;

        public double Height
        { get { return m_height; } set { Invalidate(); m_height = value; } }

        /// <summary>
        /// resolution size (or the real distance between pixels)
        /// </summary>
        private double m_Dx;

        public double Dx
        { get { return m_Dx; } set { Invalidate(); m_Dx = value; } }

        /// <summary>
        /// number of pixels in X
        /// </summary>
        private int xN;

        public int XPixels
        { get { Validate(); return xN; } }

        /// <summary>
        /// number of pixels in Y
        /// </summary>
        private int yN;

        public int YPixels
        { get { Validate(); return yN; } }

        /// <summary>
        /// time taken between scan positions
        /// </summary>
        private double m_dwell;

		/// <summary>
        /// The dwell time in ms.
        /// </summary>
        public double DwellTime
        { get { return m_dwell; } set { Invalidate(); m_dwell = value; } }

        /// <summary>
        /// type of scan (user defined)
        /// </summary>
        private ScanType m_type;

        public ScanType Type
        { get { return m_type; } set { Invalidate(); m_type = value; } }

        /// <summary>
        /// frequency at which the processor runs at
        /// </summary>
        private double m_samplingFrequency;

        public double SamplingFrequency
        { get { return m_samplingFrequency; } set { Invalidate(); m_samplingFrequency = value; } }

        public bool IsValid { get { return m_positions != null; } }

        /// <summary>
        /// find out what this is
        /// </summary>
        private double[,] m_positions;

        public double[,] Positions
        { get { Validate(); return m_positions; } private set { m_positions = value; } }

        private int m_TicksPerPixel;

        public int TicksPerPixel
        {
            get { Validate(); return m_TicksPerPixel; }
            private set { m_TicksPerPixel = value; }
        }

        public TimeSpan ScanTime
        {
            get { return TimeSpan.FromSeconds(Ticks * 1.0 / SamplingFrequency); }
        }

        private bool m_multiDirectional = true;

        public bool MultiDirectional
        {
            get { return m_multiDirectional; }
            set { m_multiDirectional = value; }
        }

        /// <summary>
        /// The total number of ticks in the scan.
        /// </summary>
        public int Ticks { get { return Positions.GetLength(1); } }

        private double m_TrigerDelay=0;

        /// <summary>
        /// The trigger delay in ms.
        /// </summary>
        public double TriggerDelay
        {
            get { return m_TrigerDelay; }
            set { m_TrigerDelay = value; }
        }

        public double XVolAfterScan { get; set; }
        public double YVolAfterScan { get; set; }
        public bool SetXYVolAfterScan { get; set; }


        #endregion

        #region Methods

        public void MoveToXYPositionAfterScan(double xvol, double yvol, bool doSetPos=true)
        {
            SetXYVolAfterScan = doSetPos;
            XVolAfterScan = xvol;
            YVolAfterScan = yvol;

        }

        #endregion

        #region Validation

        public void Invalidate()
        {
            m_positions = null;
        }

        object validateObjectSync = new object();

        public void Validate()
        {
            if (IsValid)
                return;
            lock(validateObjectSync)
            {
                if (IsValid)
                    return;
                CalculateScan();
            }
        }

        #endregion

        #region Calculations

        public long getTotalNumberOfVoltagePoints(double samplingFreq)
        {
            return Positions.LongLength;
        }

        public unsafe void PositionFromPixelIndex(int index, out int xi, out int yi)
        {
            xi = index % YPixels; // leftover is xpos.
            yi = (index-xi) / XPixels;

            if (MultiDirectional && yi % 2 != 0)
            {
                xi = (XPixels - xi - 1);
            }
        }

        protected delegate void CalcAction(ref double xVol, ref double yVol, double durStep, double halfStep, int idx);

        protected void Ramp(ref double xVol, ref double yVol, double durStep, double halfStep, int idx)
        {
            // write current voltage to array
            Positions[0, idx] = xVol;
            Positions[1, idx] = yVol + halfStep;

            xVol += durStep;
        }

        protected void Step(ref double xVol, ref double yVol, double durStep, double halfStep, int idx)
        {
            // write current voltage to array
            Positions[0, idx] = xVol + halfStep;
            Positions[1, idx] = yVol + halfStep;
        }

        protected void Diagonal(ref double xVol, ref double yVol, double durStep, double halfStep, int idx)
        {
            // write current voltage to array
            Positions[0, idx] = xVol;
            Positions[1, idx] = yVol;

            xVol += durStep;
            yVol += durStep;
        }

        protected unsafe virtual void CalculateScan()
        {
            yN = Convert.ToInt32(Height / Dx);
            xN = Convert.ToInt32(Width / Dx);

            // calculate the dwell time.
            int durTics = Convert.ToInt32(Math.Ceiling(DwellTime / 1000 * SamplingFrequency));
            TicksPerPixel = durTics;

            double durDx = Dx / durTics;
            double durDxHalf = durDx / 2;

            // creates scan matrix.
            Positions = new double[2, durTics * yN * xN];

            //Action<double, double, double, int> calcAction = null;
            CalcAction calcAction;

            switch (Type)
            {
                case ScanType.Step:
                    calcAction = Step;
                    //calcAction = (xVol, yVol, durStep, idx) =>
                    //{
                    //    // write current voltage to array
                    //    Positions[0, idx] = xVol + durDxHalf;
                    //    Positions[1, idx] = yVol + durDxHalf;
                    //};
                    break;
                case ScanType.Diagonal:
                    calcAction = Diagonal;
                    //calcAction = (xVol, yVol, durStep, idx) =>
                    //{
                    //    // write current voltage to array
                    //    Positions[0, idx] = xVol;
                    //    Positions[1, idx] = yVol;

                    //    xVol += durStep;
                    //    yVol += durStep;
                    //};
                    break;
                default: //ramp
                    calcAction = Ramp;
                    //calcAction = (xVol, yVol, durStep, idx) =>
                    //{
                    //    // write current voltage to array
                    //    Positions[0, idx] = xVol;
                    //    Positions[1, idx] = yVol + durDxHalf;

                    //    xVol += durStep;
                    //};
                    break;
            }

            //for (yi = 0; yi < yN; yi++)
            Parallel.For(0, yN, yi =>
              {

                  int cellIndex, index;
                  // yVol is the current position for the y mirror
                  int rowIndex = yi * xN;

                  double durStep = 0;
                  double xVol = 0;
                  double yVol = Dx * yi + Y0;
                  int xi, di;

                  //checking for directionality.
                  if (!MultiDirectional || yi % 2 == 0)
                  {
                      durStep = durDx;
                      for (xi = 0; xi < xN; xi++)
                      {
                          // xVol is the current position for the x mirror
                          xVol = Dx * xi + X0;

                          cellIndex = (rowIndex + xi) * durTics;

                          for (di = 0; di < durTics; di++)
                          {
                              //calculates the array index
                              index = cellIndex + di;

                              // write current voltage to array
                              calcAction(ref xVol, ref yVol, durStep, durDxHalf, index);
                          }
                      }
                  }
                  else
                  {
                      durStep = -durDx;
                      for (xi = 0; xi < xN; xi++)
                      {
                          // xVol is the current position for the x mirror
                          xVol = Dx * (xN - xi - 1) + X0 + durTics * durDx;

                          cellIndex = (rowIndex + xi) * durTics;

                          for (di = 0; di < durTics; di++)
                          {
                              //calculates the array index
                              index = cellIndex + di;

                              // write current voltage to array
                              calcAction(ref xVol, ref yVol, durStep, durDxHalf, index);
                          }
                      }
                  }
              });
        }
        #endregion
    }

    public enum ScanType
    {
        /// <summary>
        /// Stop at the top left corner of each scaninterval box.
        /// </summary>
        Step = 1,

        /// <summary>
        /// Contius ramp up from left to right at the middle of the scaninterval box.
        /// </summary>
        Ramp = 2,

        /// <summary>
        /// Do a diagnal scan over the box.
        /// </summary>
        Diagonal = 4,
    }
}
