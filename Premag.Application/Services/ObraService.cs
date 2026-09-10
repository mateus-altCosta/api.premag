using Microsoft.EntityFrameworkCore;
using Premag.Application.Interfaces.Services;
using Premag.Core.DTOs;
using Premag.Core.Entities;
using Premag.Core.Enums;
using Premag.Core.Exceptions;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

public class ObraService : IObraService
{
    private static readonly HashSet<string> Unidades =
        new(StringComparer.OrdinalIgnoreCase) { "pç", "m³", "m", "kg", "h", "un" };

    private readonly ApplicationDbContext _db;

    public ObraService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ObraDto>> ListarAsync(
        short? status,
        bool incluirInternas,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Obras.AsNoTracking().Include(o => o.Frentes).AsQueryable();
        if (!incluirInternas)
            query = query.Where(o => !o.Interna);
        if (status is not null)
            query = query.Where(o => (short)o.Status == status.Value);

        var obras = await query.OrderBy(o => o.Nome).ToListAsync(cancellationToken);
        return obras.Select(Mapear).ToList();
    }

    public async Task<ObraDto?> ObterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var obra = await _db.Obras.AsNoTracking()
            .Include(o => o.Frentes)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        return obra is null ? null : Mapear(obra);
    }

    public async Task<ObraDto> CriarAsync(CriarObraDto dto, CancellationToken cancellationToken = default)
    {
        var obra = new Obra
        {
            TenantId = _db.TenantId,
            Nome = dto.Nome.Trim(),
            Cliente = dto.Cliente.Trim(),
            Tipo = dto.Tipo.Trim(),
            Local = dto.Local.Trim(),
            CodigoSienge = string.IsNullOrWhiteSpace(dto.CodigoSienge) ? null : dto.CodigoSienge.Trim(),
            Interna = false,
            Status = StatusObra.EmExecucao
        };
        _db.Obras.Add(obra);
        await _db.SaveChangesAsync(cancellationToken);
        return Mapear(obra);
    }

    public async Task<ObraDto> AtualizarAsync(
        Guid id,
        AtualizarObraDto dto,
        CancellationToken cancellationToken = default)
    {
        var obra = await _db.Obras.Include(o => o.Frentes)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new RegraNegocioException("OBRA_NAO_ENCONTRADA", "Obra não encontrada.", 404);

        obra.Nome = dto.Nome.Trim();
        obra.Cliente = dto.Cliente.Trim();
        obra.Tipo = dto.Tipo.Trim();
        obra.Local = dto.Local.Trim();
        obra.CodigoSienge = string.IsNullOrWhiteSpace(dto.CodigoSienge) ? null : dto.CodigoSienge.Trim();
        if (!obra.Interna)
            obra.Status = dto.Status;

        await _db.SaveChangesAsync(cancellationToken);
        return Mapear(obra);
    }

    public async Task<IReadOnlyList<FrenteDto>> ListarFrentesAsync(
        Guid? obraId,
        Guid? equipeId,
        bool? ativa,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Frentes.AsNoTracking()
            .Include(f => f.Obra)
            .Include(f => f.Etapa)
            .Include(f => f.Equipe)
            .AsQueryable();

        if (obraId is Guid oid)
            query = query.Where(f => f.ObraId == oid);
        if (equipeId is Guid eid)
            query = query.Where(f => f.EquipeId == eid);
        if (ativa is bool a)
            query = query.Where(f => f.Ativa == a);

        var lista = await query.OrderBy(f => f.Nome).ToListAsync(cancellationToken);
        return lista.Select(Mapear).ToList();
    }

    public async Task<FrenteDto> CriarFrenteAsync(CriarFrenteDto dto, CancellationToken cancellationToken = default)
    {
        var obra = await _db.Obras.FirstOrDefaultAsync(o => o.Id == dto.ObraId, cancellationToken)
            ?? throw new RegraNegocioException("OBRA_NAO_ENCONTRADA", "Obra não encontrada.", 404);

        var etapa = await _db.Etapas.FirstOrDefaultAsync(e => e.Id == dto.EtapaId, cancellationToken)
            ?? throw new RegraNegocioException("ETAPA_NAO_ENCONTRADA", "Etapa não encontrada.", 404);

        Equipe? equipe = null;
        if (dto.EquipeId is Guid eid)
        {
            equipe = await _db.Equipes.FirstOrDefaultAsync(e => e.Id == eid, cancellationToken)
                ?? throw new RegraNegocioException("EQUIPE_NAO_ENCONTRADA", "Equipe não encontrada.", 404);
        }

        var unidade = string.IsNullOrWhiteSpace(dto.Unidade) ? "pç" : dto.Unidade.Trim();
        if (!Unidades.Contains(unidade))
            throw new RegraNegocioException("UNIDADE_INVALIDA", "Unidade deve ser pç, m³, m, kg, h ou un.");

        var nome = dto.Nome.Trim();
        var duplicada = await _db.Frentes.AnyAsync(f => f.ObraId == obra.Id && f.Nome == nome, cancellationToken);
        if (duplicada)
            throw new RegraNegocioException("FRENTE_DUPLICADA", "Já existe uma frente com esse nome nesta obra.", 409);

        var frente = new Frente
        {
            TenantId = _db.TenantId,
            ObraId = obra.Id,
            Nome = nome,
            EtapaId = etapa.Id,
            EquipeId = equipe?.Id,
            Unidade = unidade,
            QuantidadePrevista = dto.QuantidadePrevista < 0 ? 0 : dto.QuantidadePrevista,
            TaxaAcoKgPorUnidade = dto.TaxaAcoKgPorUnidade,
            HhOrcadoPorUnidade = dto.HhOrcadoPorUnidade,
            ItemOrcamentoSienge = string.IsNullOrWhiteSpace(dto.ItemOrcamentoSienge)
                ? null
                : dto.ItemOrcamentoSienge.Trim(),
            Cor = string.IsNullOrWhiteSpace(dto.Cor) ? "#B07500" : dto.Cor.Trim(),
            Ativa = true
        };
        _db.Frentes.Add(frente);
        await _db.SaveChangesAsync(cancellationToken);

        frente.Obra = obra;
        frente.Etapa = etapa;
        frente.Equipe = equipe;
        return Mapear(frente);
    }

    public async Task<FrenteDto> AtualizarFrenteAsync(
        Guid id,
        CriarFrenteDto dto,
        CancellationToken cancellationToken = default)
    {
        var frente = await _db.Frentes
            .Include(f => f.Obra)
            .Include(f => f.Etapa)
            .Include(f => f.Equipe)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new RegraNegocioException("FRENTE_NAO_ENCONTRADA", "Frente não encontrada.", 404);

        if (frente.Obra.Interna && dto.ObraId != frente.ObraId)
            throw new RegraNegocioException("RN-17", "A frente de indiretos não muda de obra.", 422);

        var etapa = await _db.Etapas.FirstOrDefaultAsync(e => e.Id == dto.EtapaId, cancellationToken)
            ?? throw new RegraNegocioException("ETAPA_NAO_ENCONTRADA", "Etapa não encontrada.", 404);

        Equipe? equipe = null;
        if (dto.EquipeId is Guid eid)
        {
            equipe = await _db.Equipes.FirstOrDefaultAsync(e => e.Id == eid, cancellationToken)
                ?? throw new RegraNegocioException("EQUIPE_NAO_ENCONTRADA", "Equipe não encontrada.", 404);
        }

        var unidade = string.IsNullOrWhiteSpace(dto.Unidade) ? frente.Unidade : dto.Unidade.Trim();
        if (!Unidades.Contains(unidade))
            throw new RegraNegocioException("UNIDADE_INVALIDA", "Unidade deve ser pç, m³, m, kg, h ou un.");

        var nome = dto.Nome.Trim();
        var duplicada = await _db.Frentes.AnyAsync(
            f => f.ObraId == frente.ObraId && f.Nome == nome && f.Id != frente.Id,
            cancellationToken);
        if (duplicada)
            throw new RegraNegocioException("FRENTE_DUPLICADA", "Já existe uma frente com esse nome nesta obra.", 409);

        frente.Nome = nome;
        frente.EtapaId = etapa.Id;
        frente.EquipeId = equipe?.Id;
        frente.Unidade = unidade;
        frente.QuantidadePrevista = dto.QuantidadePrevista < 0 ? 0 : dto.QuantidadePrevista;
        frente.TaxaAcoKgPorUnidade = dto.TaxaAcoKgPorUnidade;
        frente.HhOrcadoPorUnidade = dto.HhOrcadoPorUnidade;
        frente.ItemOrcamentoSienge = string.IsNullOrWhiteSpace(dto.ItemOrcamentoSienge)
            ? null
            : dto.ItemOrcamentoSienge.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Cor))
            frente.Cor = dto.Cor.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        frente.Etapa = etapa;
        frente.Equipe = equipe;
        return Mapear(frente);
    }

    private static ObraDto Mapear(Obra obra)
    {
        var frentes = obra.Frentes?.Where(f => f.Ativa).ToList() ?? [];
        var prevista = frentes.Sum(f => f.QuantidadePrevista);
        var feita = frentes.Sum(f => f.QuantidadeConcluida);
        return new ObraDto
        {
            Id = obra.Id,
            Nome = obra.Nome,
            Cliente = obra.Cliente,
            Tipo = obra.Tipo,
            Local = obra.Local,
            CodigoSienge = obra.CodigoSienge,
            Interna = obra.Interna,
            Status = obra.Status,
            QuantidadeFrentes = frentes.Count,
            PercentualAvanco = prevista <= 0 ? 0 : Math.Round(feita / prevista * 100, 1)
        };
    }

    private static FrenteDto Mapear(Frente f)
    {
        var prevista = f.QuantidadePrevista;
        return new FrenteDto
        {
            Id = f.Id,
            ObraId = f.ObraId,
            ObraNome = f.Obra?.Nome ?? string.Empty,
            Nome = f.Nome,
            EtapaId = f.EtapaId,
            EtapaNome = f.Etapa?.Nome ?? string.Empty,
            EtapaIndireta = f.Etapa?.Indireta ?? false,
            EquipeId = f.EquipeId,
            EquipeNome = f.Equipe?.Nome,
            Unidade = f.Unidade,
            QuantidadePrevista = f.QuantidadePrevista,
            QuantidadeConcluida = f.QuantidadeConcluida,
            PercentualAvanco = prevista <= 0 ? 0 : Math.Round(f.QuantidadeConcluida / prevista * 100, 1),
            TaxaAcoKgPorUnidade = f.TaxaAcoKgPorUnidade,
            HhOrcadoPorUnidade = f.HhOrcadoPorUnidade,
            Cor = f.Cor,
            Ativa = f.Ativa
        };
    }
}
