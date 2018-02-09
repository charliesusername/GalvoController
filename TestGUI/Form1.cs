using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestGUI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            System.Windows.Forms.Form.CheckForIllegalCrossThreadCalls=false;
            InitializeComponent();
           
            //// loading the image.
            string fname = @"E:\Dropbox\Charlie\Code\C#\ConfocalControl\TestFiles\bigimage.jpg";

            viewer1.Image = Image.FromFile(fname);
            viewer1.ZoomToRegion(new Rectangle(0, 0, viewer1.Image.Width, viewer1.Image.Height));

            scanImageViewer1.Load(fname);
            scanImageViewer1.SelectionMode = Cyotek.Windows.Forms.ImageBoxSelectionMode.Zoom;
        }
    }
}
