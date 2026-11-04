using System.Data;

namespace dashboardforms;

// ===== Peças de tela repetidas nas páginas (cabeçalho, cartões, KPIs, tabela) =====
// Assim Profissionais, Produtos, Estoque e Configurações ficam com o mesmo visual
// do Dashboard e de Serviços sem copiar e colar o mesmo código em cada página.
static class Ui
{
    public static readonly Font Negrito = new("Segoe UI", 9f, FontStyle.Bold);
    public static readonly Font Pequena = new("Segoe UI", 8.5f);

    public static TableLayoutPanel Raiz(params float[] alturasLinhas)
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 16),
            BackColor = Cores.Fundo,
            ColumnCount = 2,
            RowCount = alturasLinhas.Length
        };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        foreach (var h in alturasLinhas)
            raiz.RowStyles.Add(h > 0 ? new RowStyle(SizeType.Absolute, h) : new RowStyle(SizeType.Percent, 100));
        return raiz;
    }

    // Título da página à esquerda e controles (filtros, botões) à direita
    public static Panel Cabecalho(string titulo, params Control[] direita)
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = Cores.Fundo };
        p.Controls.Add(new Label
        {
            Text = titulo, Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = Cores.Escuro,
            AutoSize = true, Location = new Point(0, 8)
        });
        var fluxo = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 14, 0, 0)
        };
        fluxo.Controls.AddRange(direita);
        p.Controls.Add(fluxo);
        return p;
    }

    public static Card Cartao() => new() { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 12) };

    public static Label Titulo(string txt) => new()
    {
        Text = txt, Dock = DockStyle.Top, Height = 32, BackColor = Color.Transparent,
        ForeColor = Cores.Escuro, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold)
    };

    public static Label Dica(string txt, DockStyle dock = DockStyle.Bottom) => new()
    {
        Text = txt, Dock = dock, Height = 22, ForeColor = Cores.Cinza, BackColor = Color.Transparent,
        Font = Pequena, TextAlign = ContentAlignment.BottomLeft
    };

    // Linha com 4 KPIs; devolve os rótulos de valor e de subtítulo para a página preencher
    public static TableLayoutPanel Kpis(string[] titulos, out Label[] valores, out Label[] subs)
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = titulos.Length, RowCount = 1, BackColor = Cores.Fundo, Margin = new Padding(0) };
        valores = new Label[titulos.Length];
        subs = new Label[titulos.Length];
        for (int i = 0; i < titulos.Length; i++)
        {
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / titulos.Length));
            bool escuro = i == 0;
            var c = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, i == titulos.Length - 1 ? 0 : 12, 12), Preenchimento = escuro ? Cores.Escuro : Color.White };
            subs[i] = new Label { Dock = DockStyle.Top, Height = 22, ForeColor = escuro ? Cores.Rosa : Cores.Cinza, BackColor = Color.Transparent, Font = Negrito };
            valores[i] = new Label { Dock = DockStyle.Top, Height = 40, ForeColor = escuro ? Color.White : Cores.Escuro, BackColor = Color.Transparent, Font = new Font("Segoe UI", 20f, FontStyle.Bold) };
            var lt = new Label { Text = titulos[i], Dock = DockStyle.Top, Height = 24, ForeColor = escuro ? Cores.Rosa : Cores.Cinza, BackColor = Color.Transparent };
            c.Controls.Add(subs[i]); c.Controls.Add(valores[i]); c.Controls.Add(lt);
            t.Controls.Add(c, i, 0);
        }
        return t;
    }

    public static BotaoAnimado Botao(string texto, Color fundo, Color letra, int largura = 110) => new()
    {
        Text = texto, Size = new Size(largura, 34), BackColor = fundo, ForeColor = letra,
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand, Margin = new Padding(8, 0, 0, 0)
    };

    public static BotaoAnimado BotaoPrincipal(string texto, int largura = 150) => Botao(texto, Cores.Escuro, Color.White, largura);

    // Caixa de busca arredondada (igual à do Dashboard)
    public static Card Busca(string dica, Action<string> aoDigitar)
    {
        var caixa = new Card { Raio = 17, Size = new Size(210, 34), Padding = new Padding(0), Margin = new Padding(0, 0, 8, 0) };
        var txt = new TextBox { BorderStyle = BorderStyle.None, PlaceholderText = dica, Font = new Font("Segoe UI", 10f), Location = new Point(14, 9), Width = 180 };
        txt.TextChanged += (_, _) => aoDigitar(txt.Text.Trim());
        caixa.Controls.Add(txt);
        return caixa;
    }

    // Tabela no padrão visual Sallona, ligada a um DataTable
    public static DataGridView Tabela(DataTable dados)
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            DataSource = dados,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            GridColor = Color.FromArgb(240, 230, 228),
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 34,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        };
        g.RowTemplate.Height = 34;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Cores.Cinza;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.Font = Negrito;
        g.DefaultCellStyle.ForeColor = Cores.Texto;
        g.DefaultCellStyle.SelectionBackColor = Cores.Fundo;
        g.DefaultCellStyle.SelectionForeColor = Cores.Escuro;
        g.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn c in g.Columns)
            {
                if (c.Name == "Id") c.Visible = false;
                if (c.ValueType == typeof(decimal) && !c.Name.EndsWith("%"))
                {
                    c.DefaultCellStyle.Format = "C2";
                    c.DefaultCellStyle.FormatProvider = Formato.Br;
                }
                if (c.ValueType == typeof(DateTime)) c.DefaultCellStyle.Format = "dd/MM/yy HH:mm";
            }
        };
        return g;
    }

    // Pinta a coluna "Situação"/"Status" (verde = ok, vinho = alerta, cinza = inativo)
    public static void ColorirSituacao(DataGridView g, string coluna, Func<string, Color?> cor)
    {
        g.CellFormatting += (_, e) =>
        {
            if (e.CellStyle == null || e.ColumnIndex < 0 || g.Columns[e.ColumnIndex].Name != coluna || e.Value is not string s) return;
            if (cor(s) is Color c) e.CellStyle.ForeColor = c;
            e.CellStyle.Font = Negrito;
        };
    }

    // Seleciona de novo a linha com esse Id (depois de recarregar a tabela)
    public static void Selecionar(DataGridView g, int? id)
    {
        if (g.Rows.Count == 0) return;
        var alvo = g.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.DataBoundItem is DataRowView v && (int)v["Id"] == id) ?? g.Rows[0];
        int col = g.Columns.Cast<DataGridViewColumn>().First(c => c.Visible).Index;
        g.CurrentCell = alvo.Cells[col];
    }

    public static int? IdSelecionado(DataGridView g) =>
        g.CurrentRow?.DataBoundItem is DataRowView v ? (int)v["Id"] : null;

    public static bool Confirmar(IWin32Window? dono, string texto, string titulo) =>
        MessageBox.Show(dono, texto, titulo, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
}
