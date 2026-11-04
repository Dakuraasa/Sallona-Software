namespace dashboardforms.Pagamentos
{
    partial class PagamentoPage
    {
        /// <summary>
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Designer de Componentes

        private void InitializeComponent()
        {
            lblVoltar = new Label();
            lblEyebrow = new Label();
            lblTitulo = new Label();
            lblSubtitulo = new Label();
            etapas = new Etapas();
            cardResumo = new Caixa();
            lblResumoTitulo = new Label();
            lblIconeRecibo = new Label();
            avatar = new AvatarIniciais();
            lblCliente = new Label();
            lblSubCliente = new Label();
            divResumo = new Panel();
            lblColServicos = new Label();
            lblColValor = new Label();
            listaServicos = new Panel();
            pnlSubtotal = new Panel();
            lblSubtotal = new Label();
            lblSubtotalValor = new Label();
            blocoTotal = new BlocoInferior();
            lblTotalTitulo = new Label();
            lblTotalSub = new Label();
            lblTotal = new Label();
            cardPagamento = new Caixa();
            lblFormaTitulo = new Label();
            lblFormaSub = new Label();
            opcDinheiro = new OpcaoPagamento();
            opcPix = new OpcaoPagamento();
            opcCartao = new OpcaoPagamento();
            caixaInfo = new Caixa();
            caixaValorRecebido = new Caixa();
            txtRecebido = new TextBox();
            lblTroco = new Label();
            lblInfo = new Label();
            divPagamento = new Panel();
            lblReceberVia = new Label();
            lblValorReceber = new Label();
            btnConfirmar = new Button();
            lblNota = new Label();
            lblCadeado = new Label();
            lblSeguranca = new Label();
            lblCancelar = new Label();
            cardResumo.SuspendLayout();
            pnlSubtotal.SuspendLayout();
            blocoTotal.SuspendLayout();
            cardPagamento.SuspendLayout();
            caixaInfo.SuspendLayout();
            caixaValorRecebido.SuspendLayout();
            SuspendLayout();
            //
            // lblVoltar
            //
            lblVoltar.AutoSize = true;
            lblVoltar.BackColor = Color.Transparent;
            lblVoltar.Cursor = Cursors.Hand;
            lblVoltar.Font = new Font("Segoe UI", 9F);
            lblVoltar.ForeColor = Color.FromArgb(142, 122, 125);
            lblVoltar.Location = new Point(36, 20);
            lblVoltar.Name = "lblVoltar";
            lblVoltar.Text = "←   Voltar";
            lblVoltar.Click += lblVoltar_Click;
            lblVoltar.MouseEnter += lblVoltar_MouseEnter;
            lblVoltar.MouseLeave += lblVoltar_MouseLeave;
            //
            // lblEyebrow
            //
            lblEyebrow.AutoSize = true;
            lblEyebrow.BackColor = Color.Transparent;
            lblEyebrow.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            lblEyebrow.ForeColor = Color.FromArgb(191, 65, 99);
            lblEyebrow.Location = new Point(36, 60);
            lblEyebrow.Name = "lblEyebrow";
            lblEyebrow.Text = "UM CUIDADO COMPLETO, DO INÍCIO AO FIM";
            //
            // lblTitulo
            //
            lblTitulo.AutoSize = true;
            lblTitulo.BackColor = Color.Transparent;
            lblTitulo.Font = new Font("Georgia", 26F);
            lblTitulo.ForeColor = Color.FromArgb(58, 26, 31);
            lblTitulo.Location = new Point(32, 76);
            lblTitulo.Name = "lblTitulo";
            lblTitulo.Text = "Pagamento do atendimento";
            //
            // lblSubtitulo
            //
            lblSubtitulo.AutoSize = true;
            lblSubtitulo.BackColor = Color.Transparent;
            lblSubtitulo.Font = new Font("Segoe UI", 9.5F);
            lblSubtitulo.ForeColor = Color.FromArgb(142, 122, 125);
            lblSubtitulo.Location = new Point(36, 132);
            lblSubtitulo.Name = "lblSubtitulo";
            lblSubtitulo.Text = "Confira os serviços e escolha como receber.";
            //
            // etapas
            //
            etapas.Location = new Point(36, 166);
            etapas.Name = "etapas";
            etapas.Size = new Size(540, 26);
            //
            // cardResumo
            //
            cardResumo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            cardResumo.Controls.Add(lblResumoTitulo);
            cardResumo.Controls.Add(lblIconeRecibo);
            cardResumo.Controls.Add(avatar);
            cardResumo.Controls.Add(lblCliente);
            cardResumo.Controls.Add(lblSubCliente);
            cardResumo.Controls.Add(divResumo);
            cardResumo.Controls.Add(lblColServicos);
            cardResumo.Controls.Add(lblColValor);
            cardResumo.Controls.Add(listaServicos);
            cardResumo.Controls.Add(pnlSubtotal);
            cardResumo.Controls.Add(blocoTotal);
            cardResumo.Location = new Point(36, 208);
            cardResumo.Name = "cardResumo";
            cardResumo.Size = new Size(560, 484);
            //
            // lblResumoTitulo
            //
            lblResumoTitulo.AutoSize = true;
            lblResumoTitulo.BackColor = Color.Transparent;
            lblResumoTitulo.Font = new Font("Georgia", 15F);
            lblResumoTitulo.ForeColor = Color.FromArgb(58, 26, 31);
            lblResumoTitulo.Location = new Point(24, 22);
            lblResumoTitulo.Name = "lblResumoTitulo";
            lblResumoTitulo.Text = "Resumo do atendimento";
            //
            // lblIconeRecibo
            //
            lblIconeRecibo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblIconeRecibo.AutoSize = true;
            lblIconeRecibo.BackColor = Color.Transparent;
            lblIconeRecibo.Font = new Font("Segoe UI Emoji", 12F);
            lblIconeRecibo.ForeColor = Color.FromArgb(191, 65, 99);
            lblIconeRecibo.Location = new Point(506, 22);
            lblIconeRecibo.Name = "lblIconeRecibo";
            lblIconeRecibo.Text = "🧾";
            //
            // avatar
            //
            avatar.BackColor = Color.White;
            avatar.Cor = Color.FromArgb(191, 65, 99);
            avatar.Fundo = Color.FromArgb(243, 220, 228);
            avatar.Iniciais = "JR";
            avatar.Location = new Point(24, 72);
            avatar.Name = "avatar";
            avatar.Size = new Size(44, 44);
            //
            // lblCliente
            //
            lblCliente.AutoSize = true;
            lblCliente.BackColor = Color.Transparent;
            lblCliente.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            lblCliente.ForeColor = Color.FromArgb(58, 26, 31);
            lblCliente.Location = new Point(80, 74);
            lblCliente.Name = "lblCliente";
            lblCliente.Text = "Júlia Reis";
            //
            // lblSubCliente
            //
            lblSubCliente.AutoSize = true;
            lblSubCliente.BackColor = Color.Transparent;
            lblSubCliente.Font = new Font("Segoe UI", 8.5F);
            lblSubCliente.ForeColor = Color.FromArgb(142, 122, 125);
            lblSubCliente.Location = new Point(80, 98);
            lblSubCliente.Name = "lblSubCliente";
            lblSubCliente.Text = "21/10/2026 · Profissional: Patrícia";
            //
            // divResumo
            //
            divResumo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            divResumo.BackColor = Color.FromArgb(232, 226, 223);
            divResumo.Location = new Point(24, 136);
            divResumo.Name = "divResumo";
            divResumo.Size = new Size(512, 1);
            //
            // lblColServicos
            //
            lblColServicos.AutoSize = true;
            lblColServicos.BackColor = Color.Transparent;
            lblColServicos.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            lblColServicos.ForeColor = Color.FromArgb(142, 122, 125);
            lblColServicos.Location = new Point(24, 152);
            lblColServicos.Name = "lblColServicos";
            lblColServicos.Text = "SERVIÇOS REALIZADOS";
            //
            // lblColValor
            //
            lblColValor.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblColValor.AutoSize = true;
            lblColValor.BackColor = Color.Transparent;
            lblColValor.Font = new Font("Segoe UI", 7.5F, FontStyle.Bold);
            lblColValor.ForeColor = Color.FromArgb(142, 122, 125);
            lblColValor.Location = new Point(496, 152);
            lblColValor.Name = "lblColValor";
            lblColValor.Text = "VALOR";
            //
            // listaServicos  (as linhas de serviço são criadas pelo código)
            //
            listaServicos.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listaServicos.AutoScroll = true;
            listaServicos.BackColor = Color.White;
            listaServicos.Location = new Point(1, 176);
            listaServicos.Name = "listaServicos";
            listaServicos.Size = new Size(558, 158);
            //
            // pnlSubtotal
            //
            pnlSubtotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlSubtotal.BackColor = Color.White;
            pnlSubtotal.Controls.Add(lblSubtotal);
            pnlSubtotal.Controls.Add(lblSubtotalValor);
            pnlSubtotal.Location = new Point(1, 334);
            pnlSubtotal.Name = "pnlSubtotal";
            pnlSubtotal.Size = new Size(558, 54);
            pnlSubtotal.Paint += pnlSubtotal_Paint;
            //
            // lblSubtotal
            //
            lblSubtotal.AutoSize = true;
            lblSubtotal.BackColor = Color.Transparent;
            lblSubtotal.Font = new Font("Segoe UI", 9F);
            lblSubtotal.ForeColor = Color.FromArgb(142, 122, 125);
            lblSubtotal.Location = new Point(23, 18);
            lblSubtotal.Name = "lblSubtotal";
            lblSubtotal.Text = "Subtotal · 2 serviços";
            //
            // lblSubtotalValor
            //
            lblSubtotalValor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblSubtotalValor.BackColor = Color.Transparent;
            lblSubtotalValor.Font = new Font("Segoe UI", 9F);
            lblSubtotalValor.ForeColor = Color.FromArgb(142, 122, 125);
            lblSubtotalValor.Location = new Point(334, 0);
            lblSubtotalValor.Name = "lblSubtotalValor";
            lblSubtotalValor.Size = new Size(200, 54);
            lblSubtotalValor.Text = "R$ 410,00";
            lblSubtotalValor.TextAlign = ContentAlignment.MiddleRight;
            //
            // blocoTotal
            //
            blocoTotal.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            blocoTotal.Controls.Add(lblTotalTitulo);
            blocoTotal.Controls.Add(lblTotalSub);
            blocoTotal.Controls.Add(lblTotal);
            blocoTotal.Location = new Point(1, 388);
            blocoTotal.Name = "blocoTotal";
            blocoTotal.Preenchimento = Color.FromArgb(38, 1, 8);
            blocoTotal.Raio = 13;
            blocoTotal.Size = new Size(558, 95);
            //
            // lblTotalTitulo
            //
            lblTotalTitulo.AutoSize = true;
            lblTotalTitulo.BackColor = Color.Transparent;
            lblTotalTitulo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblTotalTitulo.ForeColor = Color.White;
            lblTotalTitulo.Location = new Point(23, 26);
            lblTotalTitulo.Name = "lblTotalTitulo";
            lblTotalTitulo.Text = "Total a receber";
            //
            // lblTotalSub
            //
            lblTotalSub.AutoSize = true;
            lblTotalSub.BackColor = Color.Transparent;
            lblTotalSub.Font = new Font("Segoe UI", 8.5F);
            lblTotalSub.ForeColor = Color.FromArgb(217, 157, 143);
            lblTotalSub.Location = new Point(23, 50);
            lblTotalSub.Name = "lblTotalSub";
            lblTotalSub.Text = "Atendimento de Júlia Reis";
            //
            // lblTotal
            //
            lblTotal.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lblTotal.BackColor = Color.Transparent;
            lblTotal.Font = new Font("Georgia", 24F);
            lblTotal.ForeColor = Color.White;
            lblTotal.Location = new Point(274, 0);
            lblTotal.Name = "lblTotal";
            lblTotal.Size = new Size(260, 95);
            lblTotal.Text = "R$ 410,00";
            lblTotal.TextAlign = ContentAlignment.MiddleRight;
            //
            // cardPagamento
            //
            cardPagamento.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            cardPagamento.Controls.Add(lblFormaTitulo);
            cardPagamento.Controls.Add(lblFormaSub);
            cardPagamento.Controls.Add(opcDinheiro);
            cardPagamento.Controls.Add(opcPix);
            cardPagamento.Controls.Add(opcCartao);
            cardPagamento.Controls.Add(caixaInfo);
            cardPagamento.Controls.Add(divPagamento);
            cardPagamento.Controls.Add(lblReceberVia);
            cardPagamento.Controls.Add(lblValorReceber);
            cardPagamento.Controls.Add(btnConfirmar);
            cardPagamento.Controls.Add(lblNota);
            cardPagamento.Location = new Point(620, 208);
            cardPagamento.Name = "cardPagamento";
            cardPagamento.Size = new Size(404, 484);
            //
            // lblFormaTitulo
            //
            lblFormaTitulo.AutoSize = true;
            lblFormaTitulo.BackColor = Color.Transparent;
            lblFormaTitulo.Font = new Font("Georgia", 15F);
            lblFormaTitulo.ForeColor = Color.FromArgb(58, 26, 31);
            lblFormaTitulo.Location = new Point(24, 22);
            lblFormaTitulo.Name = "lblFormaTitulo";
            lblFormaTitulo.Text = "Forma de pagamento";
            //
            // lblFormaSub
            //
            lblFormaSub.AutoSize = true;
            lblFormaSub.BackColor = Color.Transparent;
            lblFormaSub.Font = new Font("Segoe UI", 8.5F);
            lblFormaSub.ForeColor = Color.FromArgb(142, 122, 125);
            lblFormaSub.Location = new Point(24, 54);
            lblFormaSub.Name = "lblFormaSub";
            lblFormaSub.Text = "Selecione como o pagamento foi feito.";
            //
            // opcDinheiro
            //
            opcDinheiro.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            opcDinheiro.Descricao = "Recebimento em espécie";
            opcDinheiro.Icone = "💵";
            opcDinheiro.Location = new Point(24, 82);
            opcDinheiro.Name = "opcDinheiro";
            opcDinheiro.Size = new Size(356, 52);
            opcDinheiro.Titulo = "Dinheiro";
            opcDinheiro.Click += opcDinheiro_Click;
            //
            // opcPix
            //
            opcPix.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            opcPix.Descricao = "Transferência instantânea";
            opcPix.Icone = "◇";
            opcPix.Location = new Point(24, 142);
            opcPix.Name = "opcPix";
            opcPix.Selecionada = true;
            opcPix.Size = new Size(356, 52);
            opcPix.Titulo = "Pix";
            opcPix.Click += opcPix_Click;
            //
            // opcCartao
            //
            opcCartao.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            opcCartao.Descricao = "Débito ou crédito na maquininha";
            opcCartao.Icone = "💳";
            opcCartao.Location = new Point(24, 202);
            opcCartao.Name = "opcCartao";
            opcCartao.Size = new Size(356, 52);
            opcCartao.Titulo = "Cartão";
            opcCartao.Click += opcCartao_Click;
            //
            // caixaInfo
            //
            caixaInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            caixaInfo.Borda = Color.FromArgb(249, 245, 244);
            caixaInfo.Controls.Add(caixaValorRecebido);
            caixaInfo.Controls.Add(lblTroco);
            caixaInfo.Controls.Add(lblInfo);
            caixaInfo.Location = new Point(24, 264);
            caixaInfo.Name = "caixaInfo";
            caixaInfo.Preenchimento = Color.FromArgb(249, 245, 244);
            caixaInfo.Raio = 10;
            caixaInfo.Size = new Size(356, 56);
            //
            // caixaValorRecebido  (só aparece quando a forma é Dinheiro)
            //
            caixaValorRecebido.Controls.Add(txtRecebido);
            caixaValorRecebido.Location = new Point(118, 12);
            caixaValorRecebido.Name = "caixaValorRecebido";
            caixaValorRecebido.Raio = 8;
            caixaValorRecebido.Size = new Size(110, 32);
            caixaValorRecebido.Visible = false;
            //
            // txtRecebido
            //
            txtRecebido.BackColor = Color.White;
            txtRecebido.BorderStyle = BorderStyle.None;
            txtRecebido.Font = new Font("Segoe UI", 9.5F);
            txtRecebido.ForeColor = Color.FromArgb(58, 26, 31);
            txtRecebido.Location = new Point(10, 7);
            txtRecebido.Name = "txtRecebido";
            txtRecebido.PlaceholderText = "0,00";
            txtRecebido.Size = new Size(90, 17);
            txtRecebido.TextChanged += txtRecebido_TextChanged;
            //
            // lblTroco
            //
            lblTroco.AutoSize = true;
            lblTroco.BackColor = Color.Transparent;
            lblTroco.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTroco.ForeColor = Color.FromArgb(142, 122, 125);
            lblTroco.Location = new Point(270, 19);
            lblTroco.Name = "lblTroco";
            lblTroco.Text = "Troco: —";
            lblTroco.Visible = false;
            //
            // lblInfo
            //
            lblInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblInfo.BackColor = Color.Transparent;
            lblInfo.Font = new Font("Segoe UI", 8.5F);
            lblInfo.ForeColor = Color.FromArgb(142, 122, 125);
            lblInfo.Location = new Point(14, 8);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(328, 40);
            lblInfo.Text = "ⓘ  Confira o recebimento do Pix na conta do salão antes de confirmar o pagamento.";
            lblInfo.TextAlign = ContentAlignment.MiddleLeft;
            //
            // divPagamento
            //
            divPagamento.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            divPagamento.BackColor = Color.FromArgb(232, 226, 223);
            divPagamento.Location = new Point(24, 336);
            divPagamento.Name = "divPagamento";
            divPagamento.Size = new Size(356, 1);
            //
            // lblReceberVia
            //
            lblReceberVia.AutoSize = true;
            lblReceberVia.BackColor = Color.Transparent;
            lblReceberVia.Font = new Font("Segoe UI", 9F);
            lblReceberVia.ForeColor = Color.FromArgb(142, 122, 125);
            lblReceberVia.Location = new Point(24, 356);
            lblReceberVia.Name = "lblReceberVia";
            lblReceberVia.Text = "Receber via Pix";
            //
            // lblValorReceber
            //
            lblValorReceber.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblValorReceber.BackColor = Color.Transparent;
            lblValorReceber.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
            lblValorReceber.ForeColor = Color.FromArgb(58, 26, 31);
            lblValorReceber.Location = new Point(180, 344);
            lblValorReceber.Name = "lblValorReceber";
            lblValorReceber.Size = new Size(200, 36);
            lblValorReceber.Text = "R$ 410,00";
            lblValorReceber.TextAlign = ContentAlignment.MiddleRight;
            //
            // btnConfirmar
            //
            btnConfirmar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnConfirmar.BackColor = Color.FromArgb(191, 65, 99);
            btnConfirmar.Cursor = Cursors.Hand;
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.FlatStyle = FlatStyle.Flat;
            btnConfirmar.Font = new Font("Segoe UI Semibold", 10F);
            btnConfirmar.ForeColor = Color.White;
            btnConfirmar.Location = new Point(24, 392);
            btnConfirmar.Name = "btnConfirmar";
            btnConfirmar.Size = new Size(356, 46);
            btnConfirmar.Text = "✓   Confirmar pagamento";
            btnConfirmar.UseVisualStyleBackColor = false;
            btnConfirmar.Click += btnConfirmar_Click;
            //
            // lblNota
            //
            lblNota.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblNota.BackColor = Color.Transparent;
            lblNota.Font = new Font("Segoe UI", 8F);
            lblNota.ForeColor = Color.FromArgb(142, 122, 125);
            lblNota.Location = new Point(24, 444);
            lblNota.Name = "lblNota";
            lblNota.Size = new Size(356, 22);
            lblNota.Text = "🕘  O atendimento será marcado como pago.";
            lblNota.TextAlign = ContentAlignment.MiddleCenter;
            //
            // lblCadeado
            //
            lblCadeado.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblCadeado.AutoSize = true;
            lblCadeado.BackColor = Color.Transparent;
            lblCadeado.Font = new Font("Segoe UI Emoji", 9F);
            lblCadeado.ForeColor = Color.FromArgb(142, 122, 125);
            lblCadeado.Location = new Point(36, 704);
            lblCadeado.Name = "lblCadeado";
            lblCadeado.Text = "ⓘ";
            //
            // lblSeguranca
            //
            lblSeguranca.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblSeguranca.AutoSize = true;
            lblSeguranca.BackColor = Color.Transparent;
            lblSeguranca.Font = new Font("Segoe UI", 8.5F);
            lblSeguranca.ForeColor = Color.FromArgb(142, 122, 125);
            lblSeguranca.Location = new Point(60, 706);
            lblSeguranca.Name = "lblSeguranca";
            lblSeguranca.Text = "Só confirme depois de receber o valor.";
            //
            // lblCancelar
            //
            lblCancelar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            lblCancelar.AutoSize = true;
            lblCancelar.BackColor = Color.Transparent;
            lblCancelar.Cursor = Cursors.Hand;
            lblCancelar.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblCancelar.ForeColor = Color.FromArgb(191, 65, 99);
            lblCancelar.Location = new Point(908, 705);
            lblCancelar.Name = "lblCancelar";
            lblCancelar.Text = "Cancelar e voltar";
            lblCancelar.Click += lblCancelar_Click;
            //
            // PagamentoPage
            //
            AutoScaleMode = AutoScaleMode.None;
            AutoScroll = true;
            AutoScrollMinSize = new Size(900, 700);
            BackColor = Color.FromArgb(246, 236, 234);
            Controls.Add(lblVoltar);
            Controls.Add(lblEyebrow);
            Controls.Add(lblTitulo);
            Controls.Add(lblSubtitulo);
            Controls.Add(etapas);
            Controls.Add(cardResumo);
            Controls.Add(cardPagamento);
            Controls.Add(lblCadeado);
            Controls.Add(lblSeguranca);
            Controls.Add(lblCancelar);
            DoubleBuffered = true;
            Name = "PagamentoPage";
            Size = new Size(1060, 738);
            cardResumo.ResumeLayout(false);
            cardResumo.PerformLayout();
            pnlSubtotal.ResumeLayout(false);
            pnlSubtotal.PerformLayout();
            blocoTotal.ResumeLayout(false);
            blocoTotal.PerformLayout();
            cardPagamento.ResumeLayout(false);
            cardPagamento.PerformLayout();
            caixaInfo.ResumeLayout(false);
            caixaInfo.PerformLayout();
            caixaValorRecebido.ResumeLayout(false);
            caixaValorRecebido.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblVoltar;
        private Label lblEyebrow;
        private Label lblTitulo;
        private Label lblSubtitulo;
        private Etapas etapas;
        private Caixa cardResumo;
        private Label lblResumoTitulo;
        private Label lblIconeRecibo;
        private AvatarIniciais avatar;
        private Label lblCliente;
        private Label lblSubCliente;
        private Panel divResumo;
        private Label lblColServicos;
        private Label lblColValor;
        private Panel listaServicos;
        private Panel pnlSubtotal;
        private Label lblSubtotal;
        private Label lblSubtotalValor;
        private BlocoInferior blocoTotal;
        private Label lblTotalTitulo;
        private Label lblTotalSub;
        private Label lblTotal;
        private Caixa cardPagamento;
        private Label lblFormaTitulo;
        private Label lblFormaSub;
        private OpcaoPagamento opcDinheiro;
        private OpcaoPagamento opcPix;
        private OpcaoPagamento opcCartao;
        private Caixa caixaInfo;
        private Caixa caixaValorRecebido;
        private TextBox txtRecebido;
        private Label lblTroco;
        private Label lblInfo;
        private Panel divPagamento;
        private Label lblReceberVia;
        private Label lblValorReceber;
        private Button btnConfirmar;
        private Label lblNota;
        private Label lblCadeado;
        private Label lblSeguranca;
        private Label lblCancelar;
    }
}
