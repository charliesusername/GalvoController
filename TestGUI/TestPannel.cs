using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestGUI
{
    public class TestPannel : Panel
    {
        public event EventHandler<MouseEventArgs> MouseWheelWithCtrl;

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if(ModifierKeys.HasFlag(Keys.Control))
            {
                if (MouseWheelWithCtrl != null)
                    MouseWheelWithCtrl(this, e);
                return;
            }
            base.OnMouseWheel(e);
        }
    }
}
