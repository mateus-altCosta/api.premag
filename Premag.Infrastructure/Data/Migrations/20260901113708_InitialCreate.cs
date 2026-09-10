using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Premag.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entidade = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    entidade_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    antes = table.Column<string>(type: "jsonb", nullable: true),
                    depois = table.Column<string>(type: "jsonb", nullable: true),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configuracoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    jornada_padrao_minutos = table.Column<int>(type: "integer", nullable: false),
                    jornada_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    jornada_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    intervalo_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    intervalo_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    minutos_ociosidade_escalonamento = table.Column<int>(type: "integer", nullable: false),
                    minutos_sem_servico = table.Column<int>(type: "integer", nullable: false),
                    minutos_servico_aberto_demais = table.Column<int>(type: "integer", nullable: false),
                    dias_fechamento = table.Column<int>(type: "integer", nullable: false),
                    retencao_fotos_meses = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuracoes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "etapas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    indireta = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_etapas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "motivos_parada",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    exige_observacao = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_motivos_parada", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "obras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    cliente = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    local = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    codigo_sienge = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    centro_custo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    interna = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: true),
                    data_prevista_fim = table.Column<DateOnly>(type: "date", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_obras", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "apontamentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fim = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    minutos_efetivos = table.Column<int>(type: "integer", nullable: true),
                    motivo_parada_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    origem = table.Column<short>(type: "smallint", nullable: false),
                    jornada_nao_verificada = table.Column<bool>(type: "boolean", nullable: false),
                    dispositivo_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    criado_por_id = table.Column<Guid>(type: "uuid", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    alterado_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_apontamentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_apontamentos_motivos_parada_motivo_parada_id",
                        column: x => x.motivo_parada_id,
                        principalTable: "motivos_parada",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "colaboradores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    matricula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    funcao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    equipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custo_hora = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    data_admissao = table.Column<DateOnly>(type: "date", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    origem_cadastro = table.Column<short>(type: "smallint", nullable: false),
                    codigo_externo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    alterado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_colaboradores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "jornadas_dia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    entrada = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    saida = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    intervalo_minutos = table.Column<int>(type: "integer", nullable: false),
                    minutos_apurados = table.Column<int>(type: "integer", nullable: false),
                    situacao = table.Column<short>(type: "smallint", nullable: false),
                    origem = table.Column<short>(type: "smallint", nullable: false),
                    importado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jornadas_dia", x => x.id);
                    table.ForeignKey(
                        name: "fk_jornadas_dia_colaboradores_colaborador_id",
                        column: x => x.colaborador_id,
                        principalTable: "colaboradores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "equipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    encarregado_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "frentes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    obra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    etapa_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unidade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    quantidade_prevista = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    quantidade_concluida = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    taxa_aco_kg_por_unidade = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    hh_orcado_por_unidade = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    item_orcamento_sienge = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    cor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_frentes", x => x.id);
                    table.ForeignKey(
                        name: "fk_frentes_equipes_equipe_id",
                        column: x => x.equipe_id,
                        principalTable: "equipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_frentes_etapas_etapa_id",
                        column: x => x.etapa_id,
                        principalTable: "etapas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_frentes_obras_obra_id",
                        column: x => x.obra_id,
                        principalTable: "obras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    nome_exibicao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    perfil = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ultimo_acesso = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    data_criacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuarios_colaboradores_colaborador_id",
                        column: x => x.colaborador_id,
                        principalTable: "colaboradores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuarios_equipes_equipe_id",
                        column: x => x.equipe_id,
                        principalTable: "equipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fechamentos_dia",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    equipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fechado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fechado_por_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reaberto_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reaberto_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    motivo_reabertura = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fechamentos_dia", x => x.id);
                    table.ForeignKey(
                        name: "fk_fechamentos_dia_equipes_equipe_id",
                        column: x => x.equipe_id,
                        principalTable: "equipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fechamentos_dia_users_fechado_por_id",
                        column: x => x.fechado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fechamentos_dia_users_reaberto_por_id",
                        column: x => x.reaberto_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fotos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    frente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    apontamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tipo = table.Column<short>(type: "smallint", nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    object_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    thumb_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bytes = table.Column<int>(type: "integer", nullable: false),
                    largura = table.Column<int>(type: "integer", nullable: false),
                    altura = table.Column<int>(type: "integer", nullable: false),
                    hash_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    capturada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    enviada_por_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expira_em = table.Column<DateOnly>(type: "date", nullable: true),
                    excluido = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fotos", x => x.id);
                    table.ForeignKey(
                        name: "fk_fotos_apontamentos_apontamento_id",
                        column: x => x.apontamento_id,
                        principalTable: "apontamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fotos_colaboradores_colaborador_id",
                        column: x => x.colaborador_id,
                        principalTable: "colaboradores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fotos_frentes_frente_id",
                        column: x => x.frente_id,
                        principalTable: "frentes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fotos_users_enviada_por_id",
                        column: x => x.enviada_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inscricoes_push",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "text", nullable: false),
                    p256dh = table.Column<string>(type: "text", nullable: false),
                    auth = table.Column<string>(type: "text", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ultimo_envio_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inscricoes_push", x => x.id);
                    table.ForeignKey(
                        name: "fk_inscricoes_push_users_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lotes_sincronizacao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispositivo_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recebido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    itens_recebidos = table.Column<int>(type: "integer", nullable: false),
                    itens_aceitos = table.Column<int>(type: "integer", nullable: false),
                    itens_rejeitados = table.Column<int>(type: "integer", nullable: false),
                    payload_bytes = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lotes_sincronizacao", x => x.id);
                    table.ForeignKey(
                        name: "fk_lotes_sincronizacao_users_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ocorrencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<short>(type: "smallint", nullable: false),
                    severidade = table.Column<short>(type: "smallint", nullable: false),
                    colaborador_id = table.Column<Guid>(type: "uuid", nullable: true),
                    frente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    equipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    detectada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    janela_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    minutos_decorridos = table.Column<int>(type: "integer", nullable: true),
                    notificada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reconhecida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reconhecida_por_id = table.Column<Guid>(type: "uuid", nullable: true),
                    justificativa = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    apontamento_gerado_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ocorrencias", x => x.id);
                    table.ForeignKey(
                        name: "fk_ocorrencias_apontamentos_apontamento_gerado_id",
                        column: x => x.apontamento_gerado_id,
                        principalTable: "apontamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ocorrencias_colaboradores_colaborador_id",
                        column: x => x.colaborador_id,
                        principalTable: "colaboradores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ocorrencias_equipes_equipe_id",
                        column: x => x.equipe_id,
                        principalTable: "equipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ocorrencias_frentes_frente_id",
                        column: x => x.frente_id,
                        principalTable: "frentes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ocorrencias_users_reconhecida_por_id",
                        column: x => x.reconhecida_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dispositivo_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "producoes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    frente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<DateOnly>(type: "date", nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    foto_id = table.Column<Guid>(type: "uuid", nullable: true),
                    apontamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registrado_por_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registrado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cliente_uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    excluido = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_producoes", x => x.id);
                    table.ForeignKey(
                        name: "fk_producoes_apontamentos_apontamento_id",
                        column: x => x.apontamento_id,
                        principalTable: "apontamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_producoes_fotos_foto_id",
                        column: x => x.foto_id,
                        principalTable: "fotos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_producoes_frentes_frente_id",
                        column: x => x.frente_id,
                        principalTable: "frentes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_producoes_users_registrado_por_id",
                        column: x => x.registrado_por_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_apontamentos_aberto_por_colaborador",
                table: "apontamentos",
                columns: new[] { "tenant_id", "colaborador_id" },
                unique: true,
                filter: "hora_fim IS NULL AND excluido = FALSE");

            migrationBuilder.CreateIndex(
                name: "ix_apontamentos_alterado_por_id",
                table: "apontamentos",
                column: "alterado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_apontamentos_colaborador_id",
                table: "apontamentos",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_apontamentos_criado_por_id",
                table: "apontamentos",
                column: "criado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_apontamentos_frente_id",
                table: "apontamentos",
                column: "frente_id");

            migrationBuilder.CreateIndex(
                name: "ix_apontamentos_motivo_parada_id",
                table: "apontamentos",
                column: "motivo_parada_id");

            migrationBuilder.CreateIndex(
                name: "ix_apontamentos_tenant_id_cliente_uuid",
                table: "apontamentos",
                columns: new[] { "tenant_id", "cliente_uuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_apontamentos_tenant_id_colaborador_id_data",
                table: "apontamentos",
                columns: new[] { "tenant_id", "colaborador_id", "data" });

            migrationBuilder.CreateIndex(
                name: "ix_apontamentos_tenant_id_frente_id_data",
                table: "apontamentos",
                columns: new[] { "tenant_id", "frente_id", "data" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_tenant_id_entidade_entidade_id",
                table: "audit_logs",
                columns: new[] { "tenant_id", "entidade", "entidade_id" });

            migrationBuilder.CreateIndex(
                name: "ix_colaboradores_equipe_id",
                table: "colaboradores",
                column: "equipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_colaboradores_tenant_id_matricula",
                table: "colaboradores",
                columns: new[] { "tenant_id", "matricula" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuracoes_tenant_id",
                table: "configuracoes",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_equipes_encarregado_id",
                table: "equipes",
                column: "encarregado_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipes_tenant_id_nome",
                table: "equipes",
                columns: new[] { "tenant_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_etapas_tenant_id_nome",
                table: "etapas",
                columns: new[] { "tenant_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fechamentos_dia_equipe_id",
                table: "fechamentos_dia",
                column: "equipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_fechamentos_dia_fechado_por_id",
                table: "fechamentos_dia",
                column: "fechado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_fechamentos_dia_reaberto_por_id",
                table: "fechamentos_dia",
                column: "reaberto_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_fechamentos_dia_tenant_id_data_equipe_id",
                table: "fechamentos_dia",
                columns: new[] { "tenant_id", "data", "equipe_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fotos_apontamento_id",
                table: "fotos",
                column: "apontamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_fotos_colaborador_id",
                table: "fotos",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_fotos_enviada_por_id",
                table: "fotos",
                column: "enviada_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_fotos_frente_id",
                table: "fotos",
                column: "frente_id");

            migrationBuilder.CreateIndex(
                name: "ix_fotos_tenant_id_cliente_uuid",
                table: "fotos",
                columns: new[] { "tenant_id", "cliente_uuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_frentes_equipe_id",
                table: "frentes",
                column: "equipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_frentes_etapa_id",
                table: "frentes",
                column: "etapa_id");

            migrationBuilder.CreateIndex(
                name: "ix_frentes_obra_id",
                table: "frentes",
                column: "obra_id");

            migrationBuilder.CreateIndex(
                name: "ix_frentes_tenant_id_obra_id_nome",
                table: "frentes",
                columns: new[] { "tenant_id", "obra_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inscricoes_push_usuario_id",
                table: "inscricoes_push",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_jornadas_dia_colaborador_id",
                table: "jornadas_dia",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_jornadas_dia_tenant_id_colaborador_id_data",
                table: "jornadas_dia",
                columns: new[] { "tenant_id", "colaborador_id", "data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lotes_sincronizacao_usuario_id",
                table: "lotes_sincronizacao",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_motivos_parada_tenant_id_nome",
                table: "motivos_parada",
                columns: new[] { "tenant_id", "nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ocorrencias_apontamento_gerado_id",
                table: "ocorrencias",
                column: "apontamento_gerado_id");

            migrationBuilder.CreateIndex(
                name: "ix_ocorrencias_colaborador_id",
                table: "ocorrencias",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_ocorrencias_dedup",
                table: "ocorrencias",
                columns: new[] { "tenant_id", "tipo", "colaborador_id", "data", "janela_inicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ocorrencias_equipe_id",
                table: "ocorrencias",
                column: "equipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_ocorrencias_frente_id",
                table: "ocorrencias",
                column: "frente_id");

            migrationBuilder.CreateIndex(
                name: "ix_ocorrencias_reconhecida_por_id",
                table: "ocorrencias",
                column: "reconhecida_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_producoes_apontamento_id",
                table: "producoes",
                column: "apontamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_producoes_foto_id",
                table: "producoes",
                column: "foto_id");

            migrationBuilder.CreateIndex(
                name: "ix_producoes_frente_id",
                table: "producoes",
                column: "frente_id");

            migrationBuilder.CreateIndex(
                name: "ix_producoes_registrado_por_id",
                table: "producoes",
                column: "registrado_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_producoes_tenant_id_cliente_uuid",
                table: "producoes",
                columns: new[] { "tenant_id", "cliente_uuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_producoes_tenant_id_frente_id_data",
                table: "producoes",
                columns: new[] { "tenant_id", "frente_id", "data" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_tenant_id",
                table: "refresh_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_usuario_id",
                table: "refresh_tokens",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_slug",
                table: "tenants",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_colaborador_id",
                table: "usuarios",
                column: "colaborador_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_equipe_id",
                table: "usuarios",
                column: "equipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_tenant_id_user_name",
                table: "usuarios",
                columns: new[] { "tenant_id", "user_name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_apontamentos_colaboradores_colaborador_id",
                table: "apontamentos",
                column: "colaborador_id",
                principalTable: "colaboradores",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_apontamentos_frentes_frente_id",
                table: "apontamentos",
                column: "frente_id",
                principalTable: "frentes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_apontamentos_users_alterado_por_id",
                table: "apontamentos",
                column: "alterado_por_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_apontamentos_users_criado_por_id",
                table: "apontamentos",
                column: "criado_por_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_colaboradores_equipes_equipe_id",
                table: "colaboradores",
                column: "equipe_id",
                principalTable: "equipes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_equipes_usuarios_encarregado_id",
                table: "equipes",
                column: "encarregado_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_usuarios_colaboradores_colaborador_id",
                table: "usuarios");

            migrationBuilder.DropForeignKey(
                name: "fk_equipes_usuarios_encarregado_id",
                table: "equipes");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "configuracoes");

            migrationBuilder.DropTable(
                name: "fechamentos_dia");

            migrationBuilder.DropTable(
                name: "inscricoes_push");

            migrationBuilder.DropTable(
                name: "jornadas_dia");

            migrationBuilder.DropTable(
                name: "lotes_sincronizacao");

            migrationBuilder.DropTable(
                name: "ocorrencias");

            migrationBuilder.DropTable(
                name: "producoes");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "tenants");

            migrationBuilder.DropTable(
                name: "fotos");

            migrationBuilder.DropTable(
                name: "apontamentos");

            migrationBuilder.DropTable(
                name: "frentes");

            migrationBuilder.DropTable(
                name: "motivos_parada");

            migrationBuilder.DropTable(
                name: "etapas");

            migrationBuilder.DropTable(
                name: "obras");

            migrationBuilder.DropTable(
                name: "colaboradores");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "equipes");
        }
    }
}
