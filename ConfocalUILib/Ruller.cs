using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConfocalUILib
{
    /// <summary>
    /// Implements a ruller that shows a ruller according to a start value and a scale.
    /// </summary>
    public class Ruller : UserControl
    {
        private Panel drawPannel;

        public Ruller()
        {
            InitializeComponent();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawRuller();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        #region Ticks and display members

        private int m_MajorTicks=10;

        public int MajorTicks
        {
            get { return m_MajorTicks; }
            set { m_MajorTicks = value; Invalidate(); }
        }

        private int m_minorTicks=5;

        public int MinorTicks
        {
            get { return m_minorTicks; }
            set { m_minorTicks = value; Invalidate(); }
        }

        private bool m_AutoShiftMajorTickTo10Div=true;

        public bool AutoShiftMajorTickTo10Div
        {
            get { return m_AutoShiftMajorTickTo10Div; }
            set { m_AutoShiftMajorTickTo10Div = value; Invalidate(); }
        }

        private double m_MajorTickHeightPrecentage=0.8;

        public double MajorTickHeightPrecentage
        {
            get { return m_MajorTickHeightPrecentage; }
            set { m_MajorTickHeightPrecentage = value; Invalidate(); }
        }

        private double m_MinorTickHeightPrecentage=0.5;

        public double MinorTickHeightPrecentage
        {
            get { return m_MinorTickHeightPrecentage; }
            set { m_MinorTickHeightPrecentage = value; Invalidate(); }
        }

        #endregion

        #region Sizes members

        private double m_StartValue;

        public double StartValue
        {
            get { return m_StartValue; }
            set { m_StartValue = value; Invalidate(); }
        }


        private int m_length = 100;
        public int Length
        {
            get { return m_length; }
            set { m_length = value; Invalidate(); }
        }

        #endregion

        #region Members

        private bool m_IsVertical;

        public bool IsVertical
        {
            get { return m_IsVertical; }
            set { m_IsVertical = value; Invalidate(); }
        }

        private bool m_Invert;

        public bool Invert
        {
            get { return m_Invert; }
            set { m_Invert = value; Invalidate(); }
        }

        private bool m_ShowCursor;

        public bool ShowCursor
        {
            get { return m_ShowCursor; }
            set { m_ShowCursor = value; Invalidate(); }
        }


        private double m_CursorPosition;

        public double CursorPosition
        {
            get { return m_CursorPosition; }
            set { m_CursorPosition = value; Invalidate(); }
        }

        #endregion

        #region Graphics

        struct TickLoc
        {
            public int pos;
            public bool isMajor;
            public double val;
            public int spaceAvailable;
        }

        void DrawRuller()
        {
            // stop from updaing.
            drawPannel.SuspendLayout();

            Pen p = new Pen(ForeColor);

            // getting the layout.
            Graphics g = drawPannel.CreateGraphics();

            // Calculating ticks and offsets.
            double majorTickSize = Length * 1.0 / MajorTicks;

            // shifting to base 10.
            if (AutoShiftMajorTickTo10Div)
            {
                majorTickSize = Math.Pow(10, Math.Floor(Math.Log10(majorTickSize)));
                if (majorTickSize < 1)
                    majorTickSize = 1;
            }

            double minorTicksSize = majorTickSize / MinorTicks;
            double lengthToPixels = (IsVertical ? drawPannel.Height : drawPannel.Width) * 1.0 / Length;

            // calculating total number of ticks.
            int tickNum = Convert.ToInt32(Length / minorTicksSize);
            List<TickLoc> locs = new List<TickLoc>();
            int startPix = Convert.ToInt32((StartValue / minorTicksSize) % 1 * lengthToPixels);

            // searching for the next position of the start value.
            int lastPos = startPix;
            for (int i = 0; i < tickNum; i++)
            {
                TickLoc loc;
                loc.val = StartValue + i * minorTicksSize;
                loc.pos = startPix + Convert.ToInt32(loc.val * lengthToPixels);
                lastPos = loc.pos;
                loc.isMajor = Math.Floor(loc.val / minorTicksSize) % majorTickSize == 0;
                loc.spaceAvailable = loc.pos - lastPos;
                locs.Add(loc);
            }

            g.Clear(BackColor);

            foreach (TickLoc loc in locs)
            {
                DrawLoc(p, loc, g);
                if (loc.isMajor)
                    DrawText(p, loc, g);
            }

            //drawPannel.ResumeLayout();

        }

        void DrawLoc(Pen p,TickLoc loc, Graphics g)
        {
            int length = Convert.ToInt32((IsVertical ? drawPannel.Width : drawPannel.Height) *
                (loc.isMajor ? MajorTickHeightPrecentage : MinorTickHeightPrecentage));

            int startAt = Invert ? 0 :
                (IsVertical ? drawPannel.Width : drawPannel.Height);

            int endAt = Invert ? length :
                (IsVertical ? drawPannel.Width - length : drawPannel.Height - length);

            Point start = IsVertical ?
                new Point(startAt, loc.pos) : new Point(loc.pos, startAt);

            Point end= IsVertical ?
                new Point(endAt, loc.pos) : new Point(loc.pos, endAt);

            g.DrawLine(p, start, end);
        }

        void DrawText(Pen p, TickLoc loc, Graphics g)
        {
            // The sizing operation is common to all options
            StringFormat format = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
            if (IsVertical)
                format.FormatFlags |= StringFormatFlags.DirectionVertical;

            SizeF size = g.MeasureString((loc.val).ToString(), this.Font, loc.spaceAvailable, format);

            Point drawingPoint;
            int iX = 0;
            int iY = 0;

            if (drawPannel.IsHandleCreated)
            {
                if(!Invert)
                {
                    iX = loc.pos + loc.spaceAvailable - (int)size.Width - 2;
                    iY = 2;
                }
                else
                {
                    iX = loc.pos + loc.spaceAvailable - (int)size.Width - 2;
                    iY =  drawPannel.Height - 2 - (int)size.Height;
                }

                drawingPoint = new Point(iX, iY);
            }
            else
            {
                if (!Invert)
                {
                    iX = 2;
                    iY = loc.pos + loc.spaceAvailable - (int)size.Height - 2;
                }
                else
                {
                    iX = Width - 2 - (int)size.Width;
                    iY = loc.pos + loc.spaceAvailable - (int)size.Height - 2;
                }
                    
                drawingPoint = new Point(iX, iY);
            }

            // The drawstring function is common to all operations
            g.DrawString(loc.val.ToString(), this.Font, new SolidBrush(this.ForeColor), drawingPoint, format);
        }

        #endregion

        #region Component Editor
        private void InitializeComponent()
        {
            this.drawPannel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // drawPannel
            // 
            this.drawPannel.BackColor = System.Drawing.Color.DimGray;
            this.drawPannel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.drawPannel.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.drawPannel.Location = new System.Drawing.Point(0, 0);
            this.drawPannel.Name = "drawPannel";
            this.drawPannel.Size = new System.Drawing.Size(457, 31);
            this.drawPannel.TabIndex = 0;
            // 
            // Ruller
            // 
            this.Controls.Add(this.drawPannel);
            this.Name = "Ruller";
            this.Size = new System.Drawing.Size(457, 31);
            this.ResumeLayout(false);

        }
        #endregion

    }
}
