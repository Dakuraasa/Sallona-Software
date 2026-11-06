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
        public static GraphicsPath Criar(Rectangle r, int raio) => Formas.Caminho(r, raio);
    }
}
