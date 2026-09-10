using Microsoft.EntityFrameworkCore;
using Premag.Core.Constants;
using Premag.Core.Entities;
using Premag.Core.Enums;
using Premag.Core.Interfaces;

namespace Premag.Infrastructure.Data.Seed;

public static class IdsSeed
{
    public static readonly Guid TenantPremag = Guid.Parse("11111111-1111-7111-8111-111111111111");
    public static readonly Guid Admin = Guid.Parse("11111111-1111-7111-8111-111111111112");
    public static readonly Guid Configuracao = Guid.Parse("11111111-1111-7111-8111-111111111117");
    public static readonly Guid ObraInterna = Guid.Parse("11111111-1111-7111-8111-111111111118");
    public static readonly Guid EtapaParada = Guid.Parse("11111111-1111-7111-8111-111111111130");
    public static readonly Guid FrenteIndiretos = Guid.Parse("11111111-1111-7111-8111-111111111119");
    public static readonly Guid EtapaForma = Guid.Parse("11111111-1111-7111-8111-111111111121");
    public static readonly Guid EtapaArmacao = Guid.Parse("11111111-1111-7111-8111-111111111122");
    public static readonly Guid EtapaProtensao = Guid.Parse("11111111-1111-7111-8111-111111111123");
    public static readonly Guid EtapaConcretagem = Guid.Parse("11111111-1111-7111-8111-111111111124");

    public static readonly Guid EquipeArmacao = Guid.Parse("11111111-1111-7111-8111-111111111151");
    public static readonly Guid EquipeFormas = Guid.Parse("11111111-1111-7111-8111-111111111152");
    public static readonly Guid EquipeConcreto = Guid.Parse("11111111-1111-7111-8111-111111111153");
    public static readonly Guid EquipeGalerias = Guid.Parse("11111111-1111-7111-8111-111111111154");
    public static readonly Guid EquipeEstacas = Guid.Parse("11111111-1111-7111-8111-111111111155");

    public static readonly Guid UsuarioEncarregado = Guid.Parse("11111111-1111-7111-8111-111111111160");
    public static readonly Guid UsuarioGerente = Guid.Parse("11111111-1111-7111-8111-111111111161");
    public static readonly Guid UsuarioDiretoria = Guid.Parse("11111111-1111-7111-8111-111111111162");

    public static readonly Guid ObraOae07 = Guid.Parse("11111111-1111-7111-8111-111111111171");
    public static readonly Guid ObraPonte = Guid.Parse("11111111-1111-7111-8111-111111111172");
    public static readonly Guid ObraGalerias = Guid.Parse("11111111-1111-7111-8111-111111111173");
    public static readonly Guid ObraEstacas = Guid.Parse("11111111-1111-7111-8111-111111111174");
}

public static class SeedData
{
    public const string AdminLogin = "admin";
    public const string SenhaPadraoDesenvolvimento = "Dev.Admin.123!";

    public static async Task AplicarAsync(
        ApplicationDbContext db,
        ITenantContext tenantContext,
        string senhaAdmin,
        bool incluirDemoFabrica = false,
        CancellationToken cancellationToken = default)
    {
        tenantContext.Definir(IdsSeed.TenantPremag);

        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == IdsSeed.TenantPremag, cancellationToken))
        {
            db.Tenants.Add(new Tenant
            {
                Id = IdsSeed.TenantPremag,
                Nome = "PREMAG",
                Slug = "premag",
                Ativo = true,
                CriadoEm = DateTimeOffset.UtcNow
            });
        }

        if (!await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == IdsSeed.Admin, cancellationToken))
        {
            db.Users.Add(new Usuario
            {
                Id = IdsSeed.Admin,
                TenantId = IdsSeed.TenantPremag,
                UserName = AdminLogin,
                Email = "admin@premag.local",
                NomeExibicao = "Administrador PREMAG",
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaAdmin),
                Perfil = NomesPerfil.Admin,
                Ativo = true,
                DataCriacao = DateTimeOffset.UtcNow
            });
        }

        if (!await db.Configuracoes.IgnoreQueryFilters().AnyAsync(c => c.TenantId == IdsSeed.TenantPremag, cancellationToken))
        {
            db.Configuracoes.Add(new Configuracao
            {
                Id = IdsSeed.Configuracao,
                TenantId = IdsSeed.TenantPremag,
                DiasFechamento = 3
            });
        }
        else
        {
            // RNF-03: janela de 72 h para sincronizar o que foi apontado sem rede.
            var cfg = await db.Configuracoes.IgnoreQueryFilters()
                .FirstAsync(c => c.Id == IdsSeed.Configuracao, cancellationToken);
            if (cfg.DiasFechamento < 3)
                cfg.DiasFechamento = 3;
        }

        if (!await db.Etapas.IgnoreQueryFilters().AnyAsync(e => e.TenantId == IdsSeed.TenantPremag, cancellationToken))
        {
            var itens = new (Guid Id, string Nome, int Ordem, bool Indireta)[]
            {
                (Guid.Parse("11111111-1111-7111-8111-111111111121"), "Fôrma", 1, false),
                (Guid.Parse("11111111-1111-7111-8111-111111111122"), "Armação", 2, false),
                (Guid.Parse("11111111-1111-7111-8111-111111111123"), "Protensão", 3, false),
                (Guid.Parse("11111111-1111-7111-8111-111111111124"), "Concretagem", 4, false),
                (Guid.Parse("11111111-1111-7111-8111-111111111125"), "Cura/Desforma", 5, false),
                (Guid.Parse("11111111-1111-7111-8111-111111111126"), "Acabamento", 6, false),
                (Guid.Parse("11111111-1111-7111-8111-111111111127"), "Expedição", 7, false),
                (Guid.Parse("11111111-1111-7111-8111-111111111128"), "Manutenção", 8, true),
                (IdsSeed.EtapaParada, "Parada", 9, true)
            };
            foreach (var (id, nome, ordem, indireta) in itens)
            {
                db.Etapas.Add(new Etapa
                {
                    Id = id,
                    TenantId = IdsSeed.TenantPremag,
                    Nome = nome,
                    Ordem = ordem,
                    Indireta = indireta
                });
            }
        }

        if (!await db.MotivosParada.IgnoreQueryFilters().AnyAsync(m => m.TenantId == IdsSeed.TenantPremag, cancellationToken))
        {
            var motivos = new (string Nome, bool ExigeObs)[]
            {
                ("Chuva", false),
                ("Falta de material", false),
                ("Manutenção de equipamento", false),
                ("DDS / treinamento", false),
                ("Aguardando ponte rolante", false),
                ("Falta de energia", false),
                ("Outro", true)
            };
            var i = 0;
            foreach (var (nome, exige) in motivos)
            {
                i++;
                db.MotivosParada.Add(new MotivoParada
                {
                    Id = Guid.Parse($"11111111-1111-7111-8111-11111111114{i:D1}"),
                    TenantId = IdsSeed.TenantPremag,
                    Nome = nome,
                    ExigeObservacao = exige
                });
            }
        }

        if (!await db.Obras.IgnoreQueryFilters().AnyAsync(o => o.Id == IdsSeed.ObraInterna, cancellationToken))
        {
            db.Obras.Add(new Obra
            {
                Id = IdsSeed.ObraInterna,
                TenantId = IdsSeed.TenantPremag,
                Nome = "Fábrica — Indiretos",
                Cliente = "PREMAG",
                Tipo = "Interno",
                Local = "Planta 01",
                CentroCusto = "CC 900",
                Interna = true,
                Status = StatusObra.EmExecucao
            });
        }

        if (!await db.Frentes.IgnoreQueryFilters().AnyAsync(f => f.Id == IdsSeed.FrenteIndiretos, cancellationToken))
        {
            db.Frentes.Add(new Frente
            {
                Id = IdsSeed.FrenteIndiretos,
                TenantId = IdsSeed.TenantPremag,
                ObraId = IdsSeed.ObraInterna,
                EtapaId = IdsSeed.EtapaParada,
                Nome = "Parada / manutenção / indireto",
                Unidade = "h",
                Cor = "#C1272D",
                Ativa = true
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        if (incluirDemoFabrica)
            await SemearDemoFabricaAsync(db, senhaAdmin, cancellationToken);
    }

    private static async Task SemearDemoFabricaAsync(
        ApplicationDbContext db,
        string senha,
        CancellationToken cancellationToken)
    {
        if (await db.Equipes.IgnoreQueryFilters().AnyAsync(e => e.Id == IdsSeed.EquipeArmacao, cancellationToken))
        {
            await GarantirPisDemoAsync(db, cancellationToken);
            return;
        }

        var agora = DateTimeOffset.UtcNow;
        var hash = BCrypt.Net.BCrypt.HashPassword(senha);

        db.Equipes.AddRange(
            new Equipe { Id = IdsSeed.EquipeArmacao, TenantId = IdsSeed.TenantPremag, Nome = "ARMAÇÃO", Cor = "#F2A900" },
            new Equipe { Id = IdsSeed.EquipeFormas, TenantId = IdsSeed.TenantPremag, Nome = "FÔRMAS", Cor = "#4A5560" },
            new Equipe { Id = IdsSeed.EquipeConcreto, TenantId = IdsSeed.TenantPremag, Nome = "CONCRETO", Cor = "#2E7D5B" },
            new Equipe { Id = IdsSeed.EquipeGalerias, TenantId = IdsSeed.TenantPremag, Nome = "GALERIAS", Cor = "#4A5560" },
            new Equipe { Id = IdsSeed.EquipeEstacas, TenantId = IdsSeed.TenantPremag, Nome = "ESTACAS", Cor = "#7B4B94" });
        await db.SaveChangesAsync(cancellationToken);

        db.Users.AddRange(
            new Usuario
            {
                Id = IdsSeed.UsuarioEncarregado,
                TenantId = IdsSeed.TenantPremag,
                UserName = "encarregado",
                Email = "encarregado@premag.local",
                NomeExibicao = "Carlos Encarregado",
                SenhaHash = hash,
                Perfil = NomesPerfil.Encarregado,
                EquipeId = IdsSeed.EquipeArmacao,
                Ativo = true,
                DataCriacao = agora
            },
            new Usuario
            {
                Id = IdsSeed.UsuarioGerente,
                TenantId = IdsSeed.TenantPremag,
                UserName = "gerente",
                Email = "gerente@premag.local",
                NomeExibicao = "Marina Gerente",
                SenhaHash = hash,
                Perfil = NomesPerfil.Gerente,
                Ativo = true,
                DataCriacao = agora
            },
            new Usuario
            {
                Id = IdsSeed.UsuarioDiretoria,
                TenantId = IdsSeed.TenantPremag,
                UserName = "diretoria",
                Email = "diretoria@premag.local",
                NomeExibicao = "Roberto Diretoria",
                SenhaHash = hash,
                Perfil = NomesPerfil.Diretoria,
                Ativo = true,
                DataCriacao = agora
            });
        await db.SaveChangesAsync(cancellationToken);

        var armação = await db.Equipes.IgnoreQueryFilters()
            .FirstAsync(e => e.Id == IdsSeed.EquipeArmacao, cancellationToken);
        armação.EncarregadoId = IdsSeed.UsuarioEncarregado;

        var colabs = new (Guid Id, string Mat, string Nome, string Funcao, Guid Equipe, decimal Custo, bool Ativo)[]
        {
            (G(201), "1067", "Jailson B. Matos", "Armador", IdsSeed.EquipeArmacao, 32.4m, true),
            (G(202), "1088", "Cleiton F. Alves", "Armador", IdsSeed.EquipeArmacao, 32.4m, true),
            (G(203), "1124", "Márcio A. Bastos", "Armador", IdsSeed.EquipeArmacao, 30.2m, true),
            (G(204), "1137", "Edinaldo C. Reis", "Meio oficial", IdsSeed.EquipeArmacao, 26.9m, true),
            (G(205), "1141", "Sidnei M. Faria", "Armador", IdsSeed.EquipeArmacao, 36.7m, true),
            (G(206), "1158", "Paulo H. Cardoso", "Servente", IdsSeed.EquipeArmacao, 24.6m, true),
            (G(207), "1166", "Denilson O. Braga", "Servente", IdsSeed.EquipeArmacao, 24.6m, true),
            (G(208), "1171", "Adriano L. Peixoto", "Meio oficial", IdsSeed.EquipeArmacao, 26.9m, true),
            (G(209), "1180", "Robson V. Teles", "Servente", IdsSeed.EquipeArmacao, 24.6m, true),
            (G(210), "1042", "Adilson R. Souza", "Protensor", IdsSeed.EquipeConcreto, 41.8m, true),
            (G(211), "1119", "Rogério T. Lima", "Op. ponte rolante", IdsSeed.EquipeConcreto, 38.9m, true),
            (G(212), "1103", "Wesley P. Nunes", "Carpinteiro de fôrma", IdsSeed.EquipeFormas, 35.1m, true),
            (G(213), "1195", "Fábio N. Guedes", "Armador", IdsSeed.EquipeEstacas, 32.4m, true),
            (G(214), "1201", "Luiz C. Amorim", "Servente", IdsSeed.EquipeEstacas, 24.6m, true),
            (G(215), "1210", "Anderson S. Pires", "Armador", IdsSeed.EquipeGalerias, 32.4m, true),
            (G(216), "1214", "Ivan R. Domingos", "Servente", IdsSeed.EquipeGalerias, 24.6m, true)
        };
        foreach (var c in colabs)
        {
            db.Colaboradores.Add(new Colaborador
            {
                Id = c.Id,
                TenantId = IdsSeed.TenantPremag,
                Matricula = c.Mat,
                Nome = c.Nome,
                Funcao = c.Funcao,
                EquipeId = c.Equipe,
                CustoHora = c.Custo,
                Ativo = c.Ativo,
                OrigemCadastro = OrigemCadastro.Folha,
                CodigoExterno = c.Mat == "1067" ? "12345678901" : null,
                CriadoEm = agora,
                AlteradoEm = agora
            });
        }

        db.Obras.AddRange(
            new Obra
            {
                Id = IdsSeed.ObraOae07,
                TenantId = IdsSeed.TenantPremag,
                Nome = "OAE 07 — Viaduto BR-116 km 82",
                Cliente = "Consórcio Rota Serrana",
                Tipo = "Obra de arte especial",
                Local = "BR-116 km 82",
                CodigoSienge = "OBR 102",
                Status = StatusObra.EmExecucao
            },
            new Obra
            {
                Id = IdsSeed.ObraPonte,
                TenantId = IdsSeed.TenantPremag,
                Nome = "Ponte Rio Paquequer — OAE 03",
                Cliente = "DER-RJ",
                Tipo = "Obra de arte especial",
                Local = "Teresópolis/RJ",
                CodigoSienge = "OBR 118",
                Status = StatusObra.EmExecucao
            },
            new Obra
            {
                Id = IdsSeed.ObraGalerias,
                TenantId = IdsSeed.TenantPremag,
                Nome = "Galerias Maricá",
                Cliente = "Prefeitura de Maricá",
                Tipo = "Galeria celular",
                Local = "Maricá/RJ",
                CodigoSienge = "OBR 121",
                Status = StatusObra.EmExecucao
            },
            new Obra
            {
                Id = IdsSeed.ObraEstacas,
                TenantId = IdsSeed.TenantPremag,
                Nome = "Estacas Portobelo",
                Cliente = "Construtora Portobelo",
                Tipo = "Fundações",
                Local = "São Gonçalo/RJ",
                CodigoSienge = "OBR 124",
                Status = StatusObra.EmExecucao
            });

        db.Frentes.AddRange(
            new Frente
            {
                Id = G(181),
                TenantId = IdsSeed.TenantPremag,
                ObraId = IdsSeed.ObraOae07,
                EtapaId = IdsSeed.EtapaArmacao,
                EquipeId = IdsSeed.EquipeArmacao,
                Nome = "Armadura das vigas VP-120",
                Unidade = "pç",
                QuantidadePrevista = 26,
                QuantidadeConcluida = 19,
                TaxaAcoKgPorUnidade = 1180,
                HhOrcadoPorUnidade = 38,
                Cor = "#F2A900"
            },
            new Frente
            {
                Id = G(182),
                TenantId = IdsSeed.TenantPremag,
                ObraId = IdsSeed.ObraOae07,
                EtapaId = IdsSeed.EtapaProtensao,
                EquipeId = IdsSeed.EquipeConcreto,
                Nome = "Protensão de cordoalhas",
                Unidade = "pç",
                QuantidadePrevista = 26,
                QuantidadeConcluida = 17,
                HhOrcadoPorUnidade = 12,
                Cor = "#2D6A9F"
            },
            new Frente
            {
                Id = G(183),
                TenantId = IdsSeed.TenantPremag,
                ObraId = IdsSeed.ObraPonte,
                EtapaId = IdsSeed.EtapaArmacao,
                EquipeId = IdsSeed.EquipeEstacas,
                Nome = "Armadura das estacas Ø28",
                Unidade = "pç",
                QuantidadePrevista = 58,
                QuantidadeConcluida = 22,
                TaxaAcoKgPorUnidade = 214,
                HhOrcadoPorUnidade = 9,
                Cor = "#7B4B94"
            },
            new Frente
            {
                Id = G(184),
                TenantId = IdsSeed.TenantPremag,
                ObraId = IdsSeed.ObraGalerias,
                EtapaId = IdsSeed.EtapaArmacao,
                EquipeId = IdsSeed.EquipeGalerias,
                Nome = "Corte e dobra — galerias 2x1",
                Unidade = "m",
                QuantidadePrevista = 420,
                QuantidadeConcluida = 118,
                TaxaAcoKgPorUnidade = 46,
                HhOrcadoPorUnidade = 1.4m,
                Cor = "#4A5560"
            },
            new Frente
            {
                Id = G(185),
                TenantId = IdsSeed.TenantPremag,
                ObraId = IdsSeed.ObraPonte,
                EtapaId = IdsSeed.EtapaConcretagem,
                EquipeId = IdsSeed.EquipeConcreto,
                Nome = "Concretagem de aduelas",
                Unidade = "m³",
                QuantidadePrevista = 148,
                QuantidadeConcluida = 46,
                HhOrcadoPorUnidade = 4.1m,
                Cor = "#2E7D5B"
            },
            new Frente
            {
                Id = G(186),
                TenantId = IdsSeed.TenantPremag,
                ObraId = IdsSeed.ObraEstacas,
                EtapaId = IdsSeed.EtapaArmacao,
                EquipeId = IdsSeed.EquipeEstacas,
                Nome = "Armadura de estacas Ø40",
                Unidade = "pç",
                QuantidadePrevista = 44,
                QuantidadeConcluida = 40,
                TaxaAcoKgPorUnidade = 388,
                HhOrcadoPorUnidade = 11,
                Cor = "#B07500"
            });

        await db.SaveChangesAsync(cancellationToken);
        await GarantirPisDemoAsync(db, cancellationToken);
    }

    private static async Task GarantirPisDemoAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var jailson = await db.Colaboradores.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == G(201), cancellationToken);
        if (jailson is not null && string.IsNullOrWhiteSpace(jailson.CodigoExterno))
        {
            jailson.CodigoExterno = "12345678901";
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static Guid G(int n) => Guid.Parse($"11111111-1111-7111-8111-111111111{n:D3}");
}
