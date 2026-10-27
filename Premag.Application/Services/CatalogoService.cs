using Microsoft.EntityFrameworkCore;
using Premag.Application.Common;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Core.Entities;
using Premag.Core.Exceptions;
using Premag.Core.Interfaces;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

public class CatalogoService : ICatalogoService
{
    private readonly ApplicationDbContext _db;
    private readonly IRelogio _relogio;

    public CatalogoService(ApplicationDbContext db, IRelogio relogio)
    {
        _db = db;
        _relogio = relogio;
    }

    public async Task<CatalogoDto> ObterAsync(UsuarioLogado? quem, CancellationToken cancellationToken = default)
    {
        var etapas = await _db.Etapas
            .AsNoTracking()
            .OrderBy(e => e.Ordem)
            .Select(e => new EtapaDto
            {
                Id = e.Id,
                Nome = e.Nome,
                Ordem = e.Ordem,
                Indireta = e.Indireta
            })
            .ToListAsync(cancellationToken);

        var gerir = quem is not null && Permissoes.Tem(quem.Perfil, Permissoes.Gerente);
        var motivosQuery = _db.MotivosParada.AsNoTracking();
        if (!gerir)
            motivosQuery = motivosQuery.Where(m => m.Ativo);
        var motivos = await motivosQuery
            .OrderBy(m => m.Nome)
            .Select(m => new MotivoParadaDto
            {
                Id = m.Id,
                Nome = m.Nome,
                ExigeObservacao = m.ExigeObservacao,
                Ativo = m.Ativo
            })
            .ToListAsync(cancellationToken);

        var encarregados = gerir
            ? await _db.Users.AsNoTracking()
                .Where(u => u.Ativo && u.Perfil == Permissoes.Encarregado)
                .OrderBy(u => u.NomeExibicao)
                .Select(u => new UsuarioResumoDto
                {
                    Id = u.Id,
                    Nome = u.NomeExibicao,
                    EquipeId = u.EquipeId
                })
                .ToListAsync(cancellationToken)
            : [];

        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        return new CatalogoDto
        {
            Etapas = etapas,
            MotivosParada = motivos,
            Configuracao = Mapear(config),
            Encarregados = encarregados
        };
    }

    public async Task<CatalogoDto> AtualizarJornadaAsync(
        ConfiguracaoDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só Gerente ou acima altera a jornada.", 403);

        var inicio = HorarioJornada.Interpretar(dto.JornadaInicio, "Início da jornada");
        var fim = HorarioJornada.Interpretar(dto.JornadaFim, "Fim da jornada");
        var intIni = HorarioJornada.Interpretar(dto.IntervaloInicio, "Início do almoço");
        var intFim = HorarioJornada.Interpretar(dto.IntervaloFim, "Fim do almoço");
        var minutos = HorarioJornada.MinutosEfetivos(inicio, fim, intIni, intFim);

        var config = await _db.Configuracoes.FirstOrDefaultAsync(cancellationToken);
        if (config is null)
        {
            config = new Configuracao { TenantId = _db.TenantId };
            _db.Configuracoes.Add(config);
        }

        var antes = $"{config.JornadaInicio:HH\\:mm}-{config.JornadaFim:HH\\:mm} {config.IntervaloInicio:HH\\:mm}-{config.IntervaloFim:HH\\:mm}";
        config.JornadaInicio = inicio;
        config.JornadaFim = fim;
        config.IntervaloInicio = intIni;
        config.IntervaloFim = intFim;
        config.JornadaPadraoMinutos = minutos;
        _db.AuditLogs.Add(Auditoria.Novo(
            _db.TenantId, "Configuracao", config.Id, "editar-jornada", quem, _relogio.UtcAgora,
            antes: antes, depois: $"{inicio:HH\\:mm}-{fim:HH\\:mm} {intIni:HH\\:mm}-{intFim:HH\\:mm}"));
        await _db.SaveChangesAsync(cancellationToken);
        return await ObterAsync(quem, cancellationToken);
    }

    public async Task<MotivoParadaDto> CriarMotivoAsync(
        CriarMotivoParadaDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        GarantirGerencia(quem);
        var nome = CadastroTexto.NomeMotivo(dto.Nome);
        await GarantirNomeMotivoLivreAsync(nome, null, cancellationToken);

        var motivo = new MotivoParada
        {
            TenantId = _db.TenantId,
            Nome = nome,
            ExigeObservacao = dto.ExigeObservacao,
            Ativo = true
        };
        _db.MotivosParada.Add(motivo);
        _db.AuditLogs.Add(Auditoria.Novo(
            _db.TenantId, "MotivoParada", motivo.Id, "criar-motivo", quem, _relogio.UtcAgora, depois: nome));
        await _db.SaveChangesAsync(cancellationToken);
        return Mapear(motivo);
    }

    public async Task<MotivoParadaDto> AtualizarMotivoAsync(
        Guid id,
        CriarMotivoParadaDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        GarantirGerencia(quem);
        var motivo = await _db.MotivosParada.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new RegraNegocioException("MOTIVO_NAO_ENCONTRADO", "Motivo de parada não encontrado.", 404);

        var nome = CadastroTexto.NomeMotivo(dto.Nome);
        await GarantirNomeMotivoLivreAsync(nome, id, cancellationToken);
        var antes = $"{motivo.Nome} obs={motivo.ExigeObservacao} ativo={motivo.Ativo}";
        motivo.Nome = nome;
        motivo.ExigeObservacao = dto.ExigeObservacao;
        motivo.Ativo = dto.Ativo;
        _db.AuditLogs.Add(Auditoria.Novo(
            _db.TenantId, "MotivoParada", motivo.Id, "editar-motivo", quem, _relogio.UtcAgora,
            antes: antes, depois: $"{nome} obs={motivo.ExigeObservacao} ativo={motivo.Ativo}"));
        await _db.SaveChangesAsync(cancellationToken);
        return Mapear(motivo);
    }

    public async Task InativarMotivoAsync(
        Guid id,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        GarantirGerencia(quem);
        var motivo = await _db.MotivosParada.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new RegraNegocioException("MOTIVO_NAO_ENCONTRADO", "Motivo de parada não encontrado.", 404);
        if (!motivo.Ativo)
            return;
        motivo.Ativo = false;
        _db.AuditLogs.Add(Auditoria.Novo(
            _db.TenantId, "MotivoParada", motivo.Id, "inativar-motivo", quem, _relogio.UtcAgora,
            antes: motivo.Nome, depois: "inativo"));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task GarantirNomeMotivoLivreAsync(string nome, Guid? ignorarId, CancellationToken cancellationToken)
    {
        var query = _db.MotivosParada.Where(m => m.Nome.ToLower() == nome.ToLower());
        if (ignorarId is Guid id)
            query = query.Where(m => m.Id != id);
        if (await query.AnyAsync(cancellationToken))
            throw new RegraNegocioException("MOTIVO_DUPLICADO", "Já existe motivo com esse nome.", 409);
    }

    private static void GarantirGerencia(UsuarioLogado quem)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só Gerente ou acima altera o catálogo.", 403);
    }

    private static MotivoParadaDto Mapear(MotivoParada m) => new()
    {
        Id = m.Id,
        Nome = m.Nome,
        ExigeObservacao = m.ExigeObservacao,
        Ativo = m.Ativo
    };

    private static ConfiguracaoDto Mapear(Configuracao? config) => new()
    {
        JornadaPadraoMinutos = config?.JornadaPadraoMinutos ?? 528,
        JornadaInicio = (config?.JornadaInicio ?? new TimeOnly(7, 0)).ToString("HH:mm"),
        JornadaFim = (config?.JornadaFim ?? new TimeOnly(16, 48)).ToString("HH:mm"),
        IntervaloInicio = (config?.IntervaloInicio ?? new TimeOnly(12, 0)).ToString("HH:mm"),
        IntervaloFim = (config?.IntervaloFim ?? new TimeOnly(13, 0)).ToString("HH:mm")
    };
}
