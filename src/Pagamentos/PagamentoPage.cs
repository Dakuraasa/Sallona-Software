using System.Globalization;

namespace dashboardforms.Pagamentos;

// ===== Página de Pagamento do atendimento =====
// O visual (posições, cores, fontes) está no PagamentoPage.Designer.cs.
// Aqui fica só a lógica. O Dashboard abre esta tela com Carregar(...) quando se clica
// duas vezes em um atendimento pendente; os dados abaixo são só o exemplo do Designer.
public partial class PagamentoPage : UserControl
{
    static readonly CultureInfo Br = Formato.Br;           // mesmo padrão brasileiro do resto do sistema
    static string Moeda(decimal v) => Formato.Dinheiro(v);

    // fontes das linhas de serviço: criadas uma vez só (evita vazar recursos a cada atualização)
    static readonly Font FonteNome = new("Segoe UI", 9.5F, FontStyle.Bold);
    static readonly Font FonteDetalhe = new("Segoe UI", 8.5F);

    public record ServicoRealizado(string Nome, string Detalhe, decimal Valor);

    enum Forma { Dinheiro, Pix, Cartao }

    // ---------- eventos para integrar com o resto do sistema ----------
    public event Action? VoltarSolicitado;                       // "Voltar" / "Cancelar e voltar"
    public event Action<string, decimal>? PagamentoConfirmado;  // (forma, valor) -> gravar no banco

    // ---------- estado ----------
    string cliente = "Júlia Reis";
    string profissional = "Patrícia";
    DateTime data = new(2026, 10, 21);
    List<ServicoRealizado> servicos = new()
    {
        new("Coloração", "Cabelo · 2h", 320m),
        new("Hidratação", "Cabelo · 1h", 90m),
    };
    Forma forma = Forma.Pix;
    bool pago;

    decimal Total => servicos.Sum(s => s.Valor);

    public PagamentoPage()
    {
        InitializeComponent();
        Formas.Arredondar(btnConfirmar, 10);
        Atualizar();
    }

    /// <summary>Preenche a tela com um atendimento real (ex.: vindo da Agenda / banco).</summary>
    public void Carregar(string nomeCliente, DateTime dataAtendimento, string nomeProfissional, IEnumerable<ServicoRealizado> lista)
    {
        cliente = nomeCliente;
        data = dataAtendimento;
        profissional = nomeProfissional;
        servicos = lista.ToList();
        pago = false;
        forma = Forma.Pix;
        txtRecebido.Text = "";
        Atualizar();
    }

    // ================= eventos dos controles =================
    void lblVoltar_Click(object? sender, EventArgs e) => VoltarSolicitado?.Invoke();
    void lblCancelar_Click(object? sender, EventArgs e) => VoltarSolicitado?.Invoke();
    void lblVoltar_MouseEnter(object? sender, EventArgs e) => lblVoltar.ForeColor = CoresPagamento.Primaria;
    void lblVoltar_MouseLeave(object? sender, EventArgs e) => lblVoltar.ForeColor = CoresPagamento.Suave;

    void opcDinheiro_Click(object? sender, EventArgs e) => Escolher(Forma.Dinheiro);
    void opcPix_Click(object? sender, EventArgs e) => Escolher(Forma.Pix);
    void opcCartao_Click(object? sender, EventArgs e) => Escolher(Forma.Cartao);

    void txtRecebido_TextChanged(object? sender, EventArgs e) => AtualizarTroco();
    void btnConfirmar_Click(object? sender, EventArgs e) => Confirmar();

    void pnlSubtotal_Paint(object? sender, PaintEventArgs e)
    {
        using var caneta = new Pen(CoresPagamento.Borda);
        e.Graphics.DrawLine(caneta, 24, 0, pnlSubtotal.Width - 24, 0);
    }

    void Escolher(Forma f)
    {
        if (pago) return;
        forma = f;
        Atualizar();
    }

    // ================= atualiza a tela com o estado atual =================
    void Atualizar()
    {
        // resumo
        avatar.Iniciais = Iniciais(cliente);
        avatar.Invalidate();
        lblCliente.Text = cliente;
        lblSubCliente.Text = $"{data:dd/MM/yyyy} · Profissional: {profissional}";

        listaServicos.SuspendLayout();
        foreach (var antigo in listaServicos.Controls.Cast<Control>().ToList()) antigo.Dispose();
        for (int i = servicos.Count - 1; i >= 0; i--) listaServicos.Controls.Add(LinhaServico(servicos[i]));
        listaServicos.ResumeLayout();

        lblSubtotal.Text = $"Subtotal · {servicos.Count} {(servicos.Count == 1 ? "serviço" : "serviços")}";
        lblSubtotalValor.Text = Moeda(Total);
        lblTotal.Text = Moeda(Total);
        lblTotalSub.Text = $"Atendimento de {cliente}";

        // forma de pagamento
        opcDinheiro.Selecionada = forma == Forma.Dinheiro;
        opcPix.Selecionada = forma == Forma.Pix;
        opcCartao.Selecionada = forma == Forma.Cartao;
        opcDinheiro.Invalidate(); opcPix.Invalidate(); opcCartao.Invalidate();

        bool dinheiro = forma == Forma.Dinheiro;
        caixaValorRecebido.Visible = lblTroco.Visible = dinheiro;
        lblInfo.Text = forma switch
        {
            Forma.Dinheiro => "Valor recebido:",
            Forma.Pix => "ⓘ  Confira o recebimento do Pix na conta do salão antes de confirmar o pagamento.",
            _ => "ⓘ  Passe o cartão na maquininha e confirme só depois que a transação for aprovada."
        };
        AtualizarTroco();

        lblReceberVia.Text = "Receber via " + NomeForma(forma);
        lblValorReceber.Text = Moeda(Total);

        btnConfirmar.Text = pago ? "✓   Pagamento confirmado" : "✓   Confirmar pagamento";
        btnConfirmar.BackColor = pago ? CoresPagamento.Verde : CoresPagamento.Primaria;
        etapas.Atual = pago ? 2 : 1;
        etapas.Invalidate();
    }

    Control LinhaServico(ServicoRealizado s)
    {
        var l = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.White };
        var nome = new Label
        {
            Text = s.Nome, AutoSize = true, Location = new Point(24, 10), BackColor = Color.Transparent,
            ForeColor = CoresPagamento.Texto, Font = FonteNome
        };
        var det = new Label
        {
            Text = s.Detalhe, AutoSize = true, Location = new Point(24, 31), BackColor = Color.Transparent,
            ForeColor = CoresPagamento.Suave, Font = FonteDetalhe
        };
        var valor = new Label
        {
            Text = Moeda(s.Valor), AutoSize = false, TextAlign = ContentAlignment.MiddleRight,
            BackColor = Color.Transparent, ForeColor = CoresPagamento.Texto,
            Font = FonteNome
        };
        l.Controls.AddRange(new Control[] { nome, det, valor });
        l.Resize += (_, _) => valor.SetBounds(l.Width - 184, 0, 160, l.Height);
        return l;
    }

    void AtualizarTroco()
    {
        if (forma != Forma.Dinheiro) return;
        if (!TentarLerValor(txtRecebido.Text, out var recebido) || recebido == 0)
        {
            lblTroco.Text = "Troco: —";
            lblTroco.ForeColor = CoresPagamento.Suave;
        }
        else if (recebido < Total)
        {
            lblTroco.Text = $"Faltam {Moeda(Total - recebido)}";
            lblTroco.ForeColor = Cores.Coral;
        }
        else
        {
            lblTroco.Text = $"Troco: {Moeda(recebido - Total)}";
            lblTroco.ForeColor = CoresPagamento.Texto;
        }
        lblTroco.Left = caixaInfo.Width - 14 - lblTroco.PreferredWidth;
    }

    void Confirmar()
    {
        if (pago) return;

        if (servicos.Count == 0)
        {
            MessageBox.Show("Não há serviços neste atendimento.", "Pagamento", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (forma == Forma.Dinheiro && (!TentarLerValor(txtRecebido.Text, out var recebido) || recebido < Total))
        {
            MessageBox.Show($"Informe o valor recebido (mínimo {Moeda(Total)}).", "Pagamento",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtRecebido.Focus();
            return;
        }

        var resp = MessageBox.Show($"Confirmar o recebimento de {Moeda(Total)} via {NomeForma(forma)}?",
            "Confirmar pagamento", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (resp != DialogResult.Yes) return;

        pago = true;
        Atualizar();
        PagamentoConfirmado?.Invoke(NomeForma(forma), Total);   // TODO: gravar no MySQL
    }

    // ================= utilitários =================
    static string NomeForma(Forma f) => f switch { Forma.Dinheiro => "Dinheiro", Forma.Pix => "Pix", _ => "Cartão" };

    static bool TentarLerValor(string texto, out decimal valor)
    {
        texto = texto.Replace("R$", "").Trim();
        return decimal.TryParse(texto, NumberStyles.Number, Br, out valor);
    }

    static string Iniciais(string nome) => Util.Iniciais(nome);
}
