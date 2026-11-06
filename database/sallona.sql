-- =====================================================================
--  Sallona - Gestão para salões de beleza
--  Banco de dados MySQL 8 (Grupo SinapsES)
--
--  Como usar: abra no MySQL Workbench e execute tudo (Ctrl+Shift+Enter).
--  O script apaga e recria o banco "sallona" do zero.
--
--  Cada tabela tem estabelecimento_id: um mesmo banco guarda vários
--  salões e um não enxerga os dados do outro (igual ao login do app).
-- =====================================================================

DROP DATABASE IF EXISTS sallona;
CREATE DATABASE sallona CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE sallona;

-- ---------------------------------------------------------------------
-- Conta do estabelecimento (login). A senha nunca é guardada:
-- só o hash PBKDF2-SHA256 e o sal, em Base64 (igual ao app).
-- ---------------------------------------------------------------------
CREATE TABLE estabelecimentos (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    nome            VARCHAR(80)  NOT NULL,
    responsavel     VARCHAR(80)  NOT NULL,
    email           VARCHAR(120) NOT NULL,
    telefone        VARCHAR(20)  NOT NULL DEFAULT '',
    endereco        VARCHAR(150) NOT NULL DEFAULT '',
    documento       VARCHAR(20)  NOT NULL DEFAULT '',
    senha_hash      CHAR(44)     NOT NULL,
    senha_sal       CHAR(24)     NOT NULL,
    criado_em       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_estabelecimento_email UNIQUE (email)
);

CREATE TABLE configuracoes (
    estabelecimento_id    INT PRIMARY KEY,
    meta_mensal           DECIMAL(12,2) NOT NULL DEFAULT 0,
    periodo_padrao        ENUM('Dia','Semana','Mês','Ano') NOT NULL DEFAULT 'Mês',
    comissao_padrao       DECIMAL(5,2)  NOT NULL DEFAULT 0,
    estoque_minimo_padrao INT           NOT NULL DEFAULT 2,
    abertura              TIME NOT NULL DEFAULT '08:00',
    fechamento            TIME NOT NULL DEFAULT '18:00',
    almoco_inicio         TIME NULL     DEFAULT '12:00',
    almoco_fim            TIME NULL     DEFAULT '13:00',
    CONSTRAINT fk_config_estab FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    CONSTRAINT ck_config_meta      CHECK (meta_mensal >= 0),
    CONSTRAINT ck_config_comissao  CHECK (comissao_padrao BETWEEN 0 AND 100),
    CONSTRAINT ck_config_horario   CHECK (abertura < fechamento),
    CONSTRAINT ck_config_almoco    CHECK (almoco_inicio IS NULL OR (almoco_inicio < almoco_fim
                                          AND almoco_inicio >= abertura AND almoco_fim <= fechamento))
);

-- ---------------------------------------------------------------------
-- Cadastros
-- ---------------------------------------------------------------------
CREATE TABLE clientes (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    estabelecimento_id  INT          NOT NULL,
    nome                VARCHAR(80)  NOT NULL,
    telefone            VARCHAR(20)  NOT NULL,
    email               VARCHAR(120) NULL,
    cadastro            DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_cliente_estab FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    CONSTRAINT uq_cliente_nome UNIQUE (estabelecimento_id, nome)
);

CREATE TABLE servicos (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    estabelecimento_id  INT           NOT NULL,
    nome                VARCHAR(60)   NOT NULL,
    categoria           VARCHAR(30)   NOT NULL DEFAULT 'Cabelo',
    preco               DECIMAL(10,2) NOT NULL,
    duracao_min         INT           NOT NULL DEFAULT 30,
    ativo               BOOLEAN       NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_servico_estab FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    CONSTRAINT uq_servico_nome UNIQUE (estabelecimento_id, nome),
    CONSTRAINT ck_servico_preco   CHECK (preco > 0),
    CONSTRAINT ck_servico_duracao CHECK (duracao_min > 0)
);

CREATE TABLE profissionais (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    estabelecimento_id  INT          NOT NULL,
    nome                VARCHAR(80)  NOT NULL,
    funcao              VARCHAR(60)  NOT NULL DEFAULT '',
    telefone            VARCHAR(20)  NOT NULL DEFAULT '',
    comissao_pct        DECIMAL(5,2) NOT NULL DEFAULT 0,
    ativo               BOOLEAN      NOT NULL DEFAULT TRUE,
    cadastro            DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_prof_estab FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    CONSTRAINT uq_prof_nome UNIQUE (estabelecimento_id, nome),
    CONSTRAINT ck_prof_comissao CHECK (comissao_pct BETWEEN 0 AND 100)
);

-- ---------------------------------------------------------------------
-- Agenda: um agendamento tem um ou mais serviços.
-- Quando é concluído, vira um atendimento (atendimento_id).
-- ---------------------------------------------------------------------
CREATE TABLE atendimentos (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    estabelecimento_id  INT          NOT NULL,
    cliente_id          INT          NOT NULL,
    profissional_id     INT          NULL,
    cliente_nome        VARCHAR(80)  NOT NULL,
    profissional_nome   VARCHAR(80)  NOT NULL,
    data                DATETIME     NOT NULL,
    forma_pagamento     ENUM('Dinheiro','Pix','Cartão') NULL,
    pago_em             DATETIME     NULL,
    CONSTRAINT fk_atend_estab   FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    CONSTRAINT fk_atend_cliente FOREIGN KEY (cliente_id)        REFERENCES clientes(id),
    CONSTRAINT fk_atend_prof    FOREIGN KEY (profissional_id)   REFERENCES profissionais(id),
    CONSTRAINT ck_atend_pago    CHECK ((forma_pagamento IS NULL) = (pago_em IS NULL))
);
CREATE INDEX ix_atend_data ON atendimentos (estabelecimento_id, data);

CREATE TABLE atendimento_itens (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    atendimento_id  INT           NOT NULL,
    servico_nome    VARCHAR(60)   NOT NULL,
    valor           DECIMAL(10,2) NOT NULL,
    CONSTRAINT fk_item_atend FOREIGN KEY (atendimento_id) REFERENCES atendimentos(id) ON DELETE CASCADE,
    CONSTRAINT ck_item_valor CHECK (valor >= 0)
);

CREATE TABLE agendamentos (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    estabelecimento_id  INT          NOT NULL,
    cliente_id          INT          NOT NULL,
    profissional_id     INT          NOT NULL,
    cliente_nome        VARCHAR(80)  NOT NULL,
    profissional_nome   VARCHAR(80)  NOT NULL,
    inicio              DATETIME     NOT NULL,
    duracao_min         INT          NOT NULL,
    status              ENUM('Pendente','Confirmado','Concluído','Cancelado') NOT NULL DEFAULT 'Confirmado',
    observacoes         VARCHAR(300) NOT NULL DEFAULT '',
    criado_em           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atendimento_id      INT          NULL,
    CONSTRAINT fk_agend_estab   FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    CONSTRAINT fk_agend_cliente FOREIGN KEY (cliente_id)        REFERENCES clientes(id),
    CONSTRAINT fk_agend_prof    FOREIGN KEY (profissional_id)   REFERENCES profissionais(id),
    CONSTRAINT fk_agend_atend   FOREIGN KEY (atendimento_id)    REFERENCES atendimentos(id) ON DELETE SET NULL,
    CONSTRAINT uq_agend_atend   UNIQUE (atendimento_id),
    CONSTRAINT ck_agend_duracao CHECK (duracao_min > 0)
);
CREATE INDEX ix_agend_inicio ON agendamentos (estabelecimento_id, profissional_id, inicio);

CREATE TABLE agendamento_itens (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    agendamento_id  INT           NOT NULL,
    servico_nome    VARCHAR(60)   NOT NULL,
    valor           DECIMAL(10,2) NOT NULL,
    CONSTRAINT fk_agitem_agend FOREIGN KEY (agendamento_id) REFERENCES agendamentos(id) ON DELETE CASCADE,
    CONSTRAINT ck_agitem_valor CHECK (valor >= 0)
);

-- ---------------------------------------------------------------------
-- Produtos e estoque. A quantidade só muda por movimentação.
-- ---------------------------------------------------------------------
CREATE TABLE produtos (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    estabelecimento_id  INT           NOT NULL,
    nome                VARCHAR(80)   NOT NULL,
    categoria           VARCHAR(30)   NOT NULL DEFAULT 'Cabelo',
    marca               VARCHAR(60)   NOT NULL DEFAULT '',
    unidade             VARCHAR(10)   NOT NULL DEFAULT 'un',
    preco_venda         DECIMAL(10,2) NOT NULL DEFAULT 0,
    custo               DECIMAL(10,2) NOT NULL DEFAULT 0,
    quantidade          INT           NOT NULL DEFAULT 0,
    estoque_minimo      INT           NOT NULL DEFAULT 0,
    ativo               BOOLEAN       NOT NULL DEFAULT TRUE,
    cadastro            DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_prod_estab FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    CONSTRAINT uq_prod_nome UNIQUE (estabelecimento_id, nome),
    CONSTRAINT ck_prod_valores CHECK (preco_venda >= 0 AND custo >= 0 AND estoque_minimo >= 0),
    CONSTRAINT ck_prod_qtd     CHECK (quantidade >= 0)
);

CREATE TABLE movimentos_estoque (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    estabelecimento_id  INT          NOT NULL,
    produto_id          INT          NOT NULL,
    produto_nome        VARCHAR(80)  NOT NULL,
    data                DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    tipo                ENUM('Entrada','Saída','Ajuste') NOT NULL,
    motivo              VARCHAR(40)  NOT NULL,
    quantidade          INT          NOT NULL,
    saldo_apos          INT          NOT NULL,
    observacao          VARCHAR(120) NOT NULL DEFAULT '',
    CONSTRAINT fk_mov_estab FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    CONSTRAINT fk_mov_prod  FOREIGN KEY (produto_id) REFERENCES produtos(id) ON DELETE CASCADE,
    CONSTRAINT ck_mov_saldo CHECK (saldo_apos >= 0),
    CONSTRAINT ck_mov_sinal CHECK ((tipo = 'Entrada' AND quantidade > 0) OR (tipo = 'Saída' AND quantidade < 0)
                                   OR (tipo = 'Ajuste' AND quantidade <> 0))
);
CREATE INDEX ix_mov_data ON movimentos_estoque (estabelecimento_id, data);

-- ---------------------------------------------------------------------
-- Financeiro
-- ---------------------------------------------------------------------
CREATE TABLE despesas (
    id                  INT AUTO_INCREMENT PRIMARY KEY,
    estabelecimento_id  INT           NOT NULL,
    data                DATE          NOT NULL,
    descricao           VARCHAR(80)   NOT NULL,
    categoria           VARCHAR(30)   NOT NULL DEFAULT 'Outros',
    valor               DECIMAL(10,2) NOT NULL,
    CONSTRAINT fk_desp_estab FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
    CONSTRAINT ck_desp_valor CHECK (valor > 0)
);
CREATE INDEX ix_desp_data ON despesas (estabelecimento_id, data);

-- =====================================================================
-- Regras automáticas (triggers)
-- =====================================================================
DELIMITER //

-- cada conta nova já nasce com as configurações padrão
CREATE TRIGGER trg_estabelecimento_config
AFTER INSERT ON estabelecimentos FOR EACH ROW
BEGIN
    INSERT INTO configuracoes (estabelecimento_id) VALUES (NEW.id);
END//

-- a movimentação calcula o saldo e atualiza o produto (não deixa ficar negativo)
CREATE TRIGGER trg_movimento_saldo
BEFORE INSERT ON movimentos_estoque FOR EACH ROW
BEGIN
    DECLARE atual INT;
    SELECT quantidade INTO atual FROM produtos WHERE id = NEW.produto_id FOR UPDATE;
    SET NEW.saldo_apos = atual + NEW.quantidade;
    IF NEW.saldo_apos < 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Estoque insuficiente para essa saída.';
    END IF;
    UPDATE produtos SET quantidade = NEW.saldo_apos WHERE id = NEW.produto_id;
END//

-- o mesmo profissional não pode ter dois horários ao mesmo tempo
CREATE TRIGGER trg_agendamento_conflito_ins
BEFORE INSERT ON agendamentos FOR EACH ROW
BEGIN
    IF NEW.status <> 'Cancelado' AND EXISTS (
        SELECT 1 FROM agendamentos a
        WHERE a.profissional_id = NEW.profissional_id AND a.status <> 'Cancelado'
          AND NEW.inicio < a.inicio + INTERVAL a.duracao_min MINUTE
          AND a.inicio < NEW.inicio + INTERVAL NEW.duracao_min MINUTE) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'O profissional já tem um agendamento nesse horário.';
    END IF;
END//

CREATE TRIGGER trg_agendamento_conflito_upd
BEFORE UPDATE ON agendamentos FOR EACH ROW
BEGIN
    IF NEW.status <> 'Cancelado' AND EXISTS (
        SELECT 1 FROM agendamentos a
        WHERE a.id <> NEW.id AND a.profissional_id = NEW.profissional_id AND a.status <> 'Cancelado'
          AND NEW.inicio < a.inicio + INTERVAL a.duracao_min MINUTE
          AND a.inicio < NEW.inicio + INTERVAL NEW.duracao_min MINUTE) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'O profissional já tem um agendamento nesse horário.';
    END IF;
END//

DELIMITER ;

-- =====================================================================
-- Consultas prontas (views) usadas pelo Dashboard, Financeiro e Estoque
-- =====================================================================

-- total de cada atendimento (soma dos serviços)
CREATE VIEW vw_atendimentos AS
SELECT a.id, a.estabelecimento_id, a.data, a.cliente_id, a.cliente_nome,
       a.profissional_id, a.profissional_nome, a.forma_pagamento, a.pago_em,
       a.forma_pagamento IS NOT NULL                 AS pago,
       COALESCE(SUM(i.valor), 0)                     AS total,
       GROUP_CONCAT(i.servico_nome SEPARATOR ' + ')  AS servicos
FROM atendimentos a
LEFT JOIN atendimento_itens i ON i.atendimento_id = a.id
GROUP BY a.id;

-- faturamento (só pagos) por mês
CREATE VIEW vw_faturamento_mensal AS
SELECT estabelecimento_id,
       DATE_FORMAT(data, '%Y-%m')      AS mes,
       COUNT(*)                        AS atendimentos_pagos,
       SUM(total)                      AS faturamento,
       ROUND(SUM(total) / COUNT(*), 2) AS ticket_medio
FROM vw_atendimentos
WHERE pago
GROUP BY estabelecimento_id, DATE_FORMAT(data, '%Y-%m');

-- produção e comissão de cada profissional por mês
CREATE VIEW vw_comissoes_mensais AS
SELECT v.estabelecimento_id, DATE_FORMAT(v.data, '%Y-%m') AS mes,
       p.id AS profissional_id, p.nome AS profissional, p.comissao_pct,
       COUNT(*)                                      AS atendimentos,
       SUM(v.total)                                  AS faturado,
       ROUND(SUM(v.total) * p.comissao_pct / 100, 2) AS comissao
FROM vw_atendimentos v
JOIN profissionais p ON p.id = v.profissional_id
WHERE v.pago
GROUP BY v.estabelecimento_id, DATE_FORMAT(v.data, '%Y-%m'), p.id;

-- receitas, despesas, comissões e lucro por mês (tela Financeiro)
CREATE VIEW vw_financeiro_mensal AS
SELECT m.estabelecimento_id, m.mes,
       COALESCE(f.faturamento, 0)                                                AS receitas,
       COALESCE(d.despesas, 0)                                                   AS despesas,
       COALESCE(c.comissoes, 0)                                                  AS comissoes,
       COALESCE(f.faturamento, 0) - COALESCE(d.despesas, 0) - COALESCE(c.comissoes, 0) AS lucro
FROM (SELECT estabelecimento_id, mes FROM vw_faturamento_mensal
      UNION SELECT estabelecimento_id, DATE_FORMAT(data, '%Y-%m') FROM despesas) m
LEFT JOIN vw_faturamento_mensal f ON f.estabelecimento_id = m.estabelecimento_id AND f.mes = m.mes
LEFT JOIN (SELECT estabelecimento_id, DATE_FORMAT(data, '%Y-%m') AS mes, SUM(valor) AS despesas
           FROM despesas GROUP BY estabelecimento_id, DATE_FORMAT(data, '%Y-%m')) d
       ON d.estabelecimento_id = m.estabelecimento_id AND d.mes = m.mes
LEFT JOIN (SELECT estabelecimento_id, mes, SUM(comissao) AS comissoes
           FROM vw_comissoes_mensais GROUP BY estabelecimento_id, mes) c
       ON c.estabelecimento_id = m.estabelecimento_id AND c.mes = m.mes;

-- produtos que precisam de reposição
CREATE VIEW vw_estoque_baixo AS
SELECT estabelecimento_id, id, nome, categoria, quantidade, estoque_minimo, unidade,
       CASE WHEN quantidade <= 0 THEN 'Sem estoque' ELSE 'Baixo' END AS situacao
FROM produtos
WHERE ativo AND quantidade <= estoque_minimo;

-- agenda completa com horário de término e valor previsto
CREATE VIEW vw_agenda AS
SELECT a.id, a.estabelecimento_id, a.inicio,
       a.inicio + INTERVAL a.duracao_min MINUTE AS fim,
       a.cliente_nome, a.profissional_id, a.profissional_nome, a.status, a.observacoes,
       COALESCE(SUM(i.valor), 0)                    AS valor_previsto,
       GROUP_CONCAT(i.servico_nome SEPARATOR ' + ') AS servicos
FROM agendamentos a
LEFT JOIN agendamento_itens i ON i.agendamento_id = a.id
GROUP BY a.id;

-- pedidos de horário ainda não confirmados (painel "Solicitações")
CREATE VIEW vw_solicitacoes AS
SELECT * FROM vw_agenda
WHERE status = 'Pendente' AND fim >= CURDATE()
ORDER BY inicio;
