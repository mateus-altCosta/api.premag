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

    public async Task<CatalogoDto> ObterAsync(CancellationToken cancellationToken = default)
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

        var motivos = await _db.MotivosParada
            .AsNoTracking()
            .OrderBy(m => m.Nome)
            .Select(m => new MotivoParadaDto
            {
                Id = m.Id,
                Nome = m.Nome,
                ExigeObservacao = m.ExigeObservacao
            })
            .ToListAsync(cancellationToken);

        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        return new CatalogoDto
        {
            Etapas = etapas,
            MotivosParada = motivos,
            Configuracao = Mapear(config)
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
        return await ObterAsync(cancellationToken);
    }

    private static ConfiguracaoDto Mapear(Configuracao? config) => new()
    {
        JornadaPadraoMinutos = config?.JornadaPadraoMinutos ?? 528,
        JornadaInicio = (config?.JornadaInicio ?? new TimeOnly(7, 0)).ToString("HH:mm"),
        JornadaFim = (config?.JornadaFim ?? new TimeOnly(16, 48)).ToString("HH:mm"),
        IntervaloInicio = (config?.IntervaloInicio ?? new TimeOnly(12, 0)).ToString("HH:mm"),
        IntervaloFim = (config?.IntervaloFim ?? new TimeOnly(13, 0)).ToString("HH:mm")
    };
}
