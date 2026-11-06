using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dashboardforms.Login
{
    public class BotaoArredondado : Button
    {
        public int Raio = 10;
        public Color CorNormal = Color.Gray;
        public Color CorHover = Color.DarkGray;
        public Color CorBorda = Color.Transparent;
        bool mouseEmCima = false;

        public BotaoArredondado()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            mouseEmCima = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            mouseEmCima = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle area = new Rectangle(0, 0, Width - 1, Height - 1);
            GraphicsPath caminho = FormaArredondada.Criar(area, Raio);

            Color corAtual = mouseEmCima ? CorHover : CorNormal;
            SolidBrush pincel = new SolidBrush(corAtual);
            g.FillPath(pincel, caminho);

            if (CorBorda != Color.Transparent)
            {
                Pen caneta = new Pen(CorBorda, 1.5F);
                g.DrawPath(caneta, caminho);
                caneta.Dispose();
            }

            TextRenderer.DrawText(g, Text, Font, area, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            pincel.Dispose();
            caminho.Dispose();
        }
    }
}
