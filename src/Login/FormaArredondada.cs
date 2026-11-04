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
        // Usa o mesmo desenho dos outros controles (Formas.Caminho, em controles.cs),
        // que também funciona quando o controle é menor que o raio.
        public static GraphicsPath Criar(Rectangle r, int raio) => Formas.Caminho(r, raio);
    }
}
