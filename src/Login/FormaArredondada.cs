using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dashboardforms.Login
{
    public static class FormaArredondada
    {
        public static GraphicsPath Criar(Rectangle r, int raio)
        {
            int d = raio * 2;
            GraphicsPath caminho = new GraphicsPath();
            caminho.AddArc(r.X, r.Y, d, d, 180, 90);
            caminho.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            caminho.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            caminho.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            caminho.CloseFigure();
            return caminho;
        }
    }
}
