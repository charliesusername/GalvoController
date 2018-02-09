using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGUI
{
    public class Viewer : Cyotek.Windows.Forms.ImageBox
    {
        public Viewer()
            :base()
        {
            this.SelectionMode = Cyotek.Windows.Forms.ImageBoxSelectionMode.Rectangle;
            this.ZoomOnSelect = true;
        }

        public bool ZoomOnSelect { get; private set; }

        protected override void OnSelected(EventArgs e)
        {
            if (ZoomOnSelect)
            {
                this.ZoomToRegion(this.SelectionRegion);
                this.SelectNone();
            }
            base.OnSelected(e);
        }

        List<Image> layers = new List<Image>();
    }

    
}
