using Microsoft.EntityFrameworkCore;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

public class CatalogoService : ICatalogoService
{
    private readonly ApplicationDbContext _db;

    public CatalogoService(ApplicationDbContext db)
    {
        _db = db;
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
            Configuracao = new ConfiguracaoDto
            {
                JornadaPadraoMinutos = config?.JornadaPadraoMinutos ?? 528,
                JornadaInicio = (config?.JornadaInicio ?? new TimeOnly(7, 0)).ToString("HH:mm"),
                JornadaFim = (config?.JornadaFim ?? new TimeOnly(16, 48)).ToString("HH:mm"),
                IntervaloInicio = (config?.IntervaloInicio ?? new TimeOnly(12, 0)).ToString("HH:mm"),
                IntervaloFim = (config?.IntervaloFim ?? new TimeOnly(13, 0)).ToString("HH:mm")
            }
        };
    }
}
