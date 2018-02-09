using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConfocalUILib
{
    [ToolboxItem(true)]
    public class ScanImageViewer : UserControl
    {
        private Panel panel1;
        private Panel panel2;
        private RulerControl rulerHorz;
        private RulerControl rulerVert;
        private Panel imageBoxContainer;
        private Panel panel3;

        /// <summary>
        /// Implements an image viewer.
        /// </summary>
        public ScanImageViewer()
        {
            // Adding the rullers. 
            InitializeComponent();

            // The implementation of the image box.
            ImageBox = new Cyotek.Windows.Forms.ImageBox();
            this.imageBoxContainer.Controls.Add(ImageBox);
            ImageBox.Dock = DockStyle.Fill;
            MouseAnnotationToolTip = new ToolTip();
            MouseAnnotationToolTip.InitialDelay = 20;
            MouseAnnotationToolTip.IsBalloon = false;
            MouseAnnotationToolTip.SetToolTip(this, "");
            
            // The layers.
            Layers = new List<Tuple<Point, Image>>();
            InitializeEvents();
        }

        #region graphics

        void UpdateRullerRanges()
        {
            //orzRuller.StartValue = ImageBox.Se

            // getting the current view port.s
            RectangleF rect = getViewPortInPoints();
            rulerHorz.ZoomFactor = ImageBox.ZoomFactor;
            rulerVert.ZoomFactor = ImageBox.ZoomFactor;
            rulerHorz.StartValue = rect.Left;
            rulerVert.StartValue = rect.Top;
        }

        #endregion


        #region members

        /// <summary>
        /// The internal image box.
        /// </summary>
        public Cyotek.Windows.Forms.ImageBox ImageBox { get; private set; }

        public ToolTip MouseAnnotationToolTip { get; private set; }

        public Cyotek.Windows.Forms.ImageBoxSelectionMode SelectionMode
        {
            get { return ImageBox.SelectionMode; }
            set { ImageBox.SelectionMode = value; }
        }

        /// <summary>
        /// Set the image to make.
        /// </summary>
        public Image Image { get; private set; }

        /// <summary>
        /// Sets the image.
        /// </summary>
        public List<Tuple<Point,Image>> Layers  { get; private set; }

        #endregion

        #region Mouse Events

        void InitializeEvents()
        {
            ImageBox.MouseMove += ImageBox_MouseMove;
            ImageBox.Paint += ImageBox_Paint;
        }

        private void ImageBox_Paint(object sender, EventArgs e)
        {
            UpdateRullerRanges();
        }

        /// <summary>
        /// Mouse move event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (ImageBox.IsPointInImage(e.Location))
            {
                Point onImage = ImageBox.PointToImage(e.Location.X, e.Location.Y);
                MouseAnnotationToolTip.SetToolTip(ImageBox, "" + onImage.X + "," + onImage.Y + ":" + GetValue(onImage).ToString("#0.000"));
                MouseAnnotationToolTip.Active = true;
            }
            else
            {
                MouseAnnotationToolTip.Active = false;
            }
        }

        #endregion

        #region Image Methods

        public double GetValue(Point location)
        {
            if (Image is Bitmap)
                return ((Bitmap)Image).GetPixel(location.X, location.Y).GetBrightness();
            return 0;
        }

        public void Load(string filename, bool ZoomTofit=true)
        {
            Load(Image.FromFile(filename),ZoomTofit);
        }

        public void Load(Image img, bool ZoomToFit=true)
        {
            this.Image = img;
            UpdateImage();
            if (ZoomToFit)
                this.ImageBox.ZoomToFit();
        }

        public RectangleF getViewPortInPoints()
        {
            //Rectangle view = ImageBox.GetInsideViewPort();
            //Point offset = new Point(ImageBox.HorizontalScroll.Value, ImageBox.VerticalScroll.Value);
            //view = new Rectangle(offset, view.Size);

            // conveting to image.
            //Point topLeft = ImageBox.PointToImage(view.X, view.Y);
            //Point bottomRight= ImageBox.PointToImage(view.Bottom, view.Right);
            //return new RectangleF(ImageBox.PointToImage(view.X, view.Y),
            //    new Size(bottomRight.X-topLeft.X,bottomRight.Y-topLeft.Y));

            RectangleF imageRegion = ImageBox.GetSourceImageRegion();
            Point p = ImageBox.PointToImage(0,0);
            RectangleF viewport = new RectangleF(p, imageRegion.Size);
            return viewport;
        }

        #endregion

        #region Drawing

        void UpdateImage()
        {
            ImageBox.Image = Image;
            UpdateRullerRanges();
        }

        #endregion

        #region Component Designer
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.rulerHorz = new ConfocalUILib.RulerControl();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.rulerVert = new ConfocalUILib.RulerControl();
            this.imageBoxContainer = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.rulerHorz);
            this.panel1.Controls.Add(this.panel2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 493);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(856, 30);
            this.panel1.TabIndex = 0;
            // 
            // rulerHorz
            // 
            this.rulerHorz.ActualSize = false;
            this.rulerHorz.BackColor = System.Drawing.SystemColors.WindowFrame;
            this.rulerHorz.BorderStyle = System.Windows.Forms.Border3DStyle.Flat;
            this.rulerHorz.DivisionMarkFactor = 5;
            this.rulerHorz.Divisions = 10;
            this.rulerHorz.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rulerHorz.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.rulerHorz.Location = new System.Drawing.Point(30, 0);
            this.rulerHorz.MajorInterval = 100;
            this.rulerHorz.MiddleMarkFactor = 3;
            this.rulerHorz.MouseTrackingOn = true;
            this.rulerHorz.Name = "rulerHorz";
            this.rulerHorz.Orientation = ConfocalUILib.enumOrientation.orHorizontal;
            this.rulerHorz.RulerAlignment = ConfocalUILib.enumRulerAlignment.raBottomOrRight;
            this.rulerHorz.ScaleMode = ConfocalUILib.enumScaleMode.smPoints;
            this.rulerHorz.Size = new System.Drawing.Size(826, 30);
            this.rulerHorz.StartValue = 0D;
            this.rulerHorz.TabIndex = 1;
            this.rulerHorz.Text = "rulerControl1";
            this.rulerHorz.VerticalNumbers = true;
            this.rulerHorz.ZoomFactor = 1D;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(30, 30);
            this.panel2.TabIndex = 0;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.rulerVert);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(30, 493);
            this.panel3.TabIndex = 1;
            // 
            // rulerVert
            // 
            this.rulerVert.ActualSize = false;
            this.rulerVert.BackColor = System.Drawing.SystemColors.WindowFrame;
            this.rulerVert.BorderStyle = System.Windows.Forms.Border3DStyle.Flat;
            this.rulerVert.DivisionMarkFactor = 5;
            this.rulerVert.Divisions = 10;
            this.rulerVert.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rulerVert.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.rulerVert.Location = new System.Drawing.Point(0, 0);
            this.rulerVert.MajorInterval = 100;
            this.rulerVert.MiddleMarkFactor = 3;
            this.rulerVert.MouseTrackingOn = true;
            this.rulerVert.Name = "rulerVert";
            this.rulerVert.Orientation = ConfocalUILib.enumOrientation.orVertical;
            this.rulerVert.RulerAlignment = ConfocalUILib.enumRulerAlignment.raBottomOrRight;
            this.rulerVert.ScaleMode = ConfocalUILib.enumScaleMode.smPoints;
            this.rulerVert.Size = new System.Drawing.Size(30, 493);
            this.rulerVert.StartValue = 0D;
            this.rulerVert.TabIndex = 2;
            this.rulerVert.Text = "rulerControl2";
            this.rulerVert.VerticalNumbers = true;
            this.rulerVert.ZoomFactor = 1D;
            // 
            // imageBoxContainer
            // 
            this.imageBoxContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBoxContainer.Location = new System.Drawing.Point(30, 0);
            this.imageBoxContainer.Name = "imageBoxContainer";
            this.imageBoxContainer.Size = new System.Drawing.Size(826, 493);
            this.imageBoxContainer.TabIndex = 2;
            // 
            // ScanImageViewer
            // 
            this.Controls.Add(this.imageBoxContainer);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel1);
            this.Name = "ScanImageViewer";
            this.Size = new System.Drawing.Size(856, 523);
            this.panel1.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

    }
}
