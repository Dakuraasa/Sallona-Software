using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dashboardforms.Login
{
    public class PainelArredondado : Panel
    {
        public int Raio = 10;
        public Color CorBorda = Color.LightGray;

        public PainelArredondado()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle area = new Rectangle(0, 0, Width - 1, Height - 1);
            GraphicsPath caminho = FormaArredondada.Criar(area, Raio);

            SolidBrush pincel = new SolidBrush(BackColor);
            Pen caneta = new Pen(CorBorda, 1.5F);

            g.FillPath(pincel, caminho);
            g.DrawPath(caneta, caminho);

            pincel.Dispose();
            caneta.Dispose();
            caminho.Dispose();
        }
    }
}