using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Premag.Application.Common;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Core.Enums;
using Premag.Core.Exceptions;
using Premag.Core.Interfaces;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

public class RelatorioService : IRelatorioService
{
    private readonly ApplicationDbContext _db;
    private readonly IRelogio _relogio;

    public RelatorioService(ApplicationDbContext db, IRelogio relogio)
    {
        _db = db;
        _relogio = relogio;
    }

    public async Task<RelatorioDto> ObterAsync(
        string tipo,
        string periodo,
        Guid? obraId,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Relatórios são de Gerente ou acima.", 403);

        var verCusto = Permissoes.Tem(quem.Perfil, Permissoes.Gerente); // RN-13: Encarregado já bloqueado acima.
        var (de, ate, rotulo) = Janela(periodo, _relogio.HojeSaoPaulo);
        var tipoNorm = (tipo ?? "produtividade").Trim().ToLowerInvariant();
        if (tipoNorm is "avanço" or "avanco")
            tipoNorm = "avanco";
        else
            tipoNorm = "produtividade";

        var frentesQuery = _db.Frentes.AsNoTracking()
            .Include(f => f.Obra)
            .Include(f => f.Etapa)
            .Where(f => f.Ativa && !f.Obra.Interna && !f.Etapa.Indireta);
        if (obraId is Guid oid)
            frentesQuery = frentesQuery.Where(f => f.ObraId == oid);

        var frentes = await frentesQuery.OrderBy(f => f.Nome).ToListAsync(cancellationToken);
        var ids = frentes.Select(f => f.Id).ToList();

        var apontamentos = await _db.Apontamentos.AsNoTracking()
            .Where(a => a.HoraFim != null && a.MinutosEfetivos != null && a.Data >= de && a.Data <= ate)
            .Select(a => new
            {
                a.FrenteId,
                Interna = a.Frente.Etapa.Indireta,
                Minutos = a.MinutosEfetivos!.Value,
                Custo = a.Colaborador.OrigemCadastro == OrigemCadastro.Folha ? a.Colaborador.CustoHora : null,
                a.Data
            })
            .ToListAsync(cancellationToken);

        var producoes = await _db.Producoes.AsNoTracking()
            .Where(p => ids.Contains(p.FrenteId) && p.Data >= de && p.Data <= ate)
            .Select(p => new { p.FrenteId, p.Data, p.Quantidade })
            .ToListAsync(cancellationToken);

        var entradas = frentes.Select(f => new FrenteIndice(
            f.Id, false, f.QuantidadePrevista, f.QuantidadeConcluida, f.HhOrcadoPorUnidade, f.TaxaAcoKgPorUnidade)).ToList();
        var apt = apontamentos.Select(a => new ApontamentoIndice(a.FrenteId, a.Interna, a.Minutos, a.Custo, a.Data)).ToList();
        var prod = producoes.Select(p => new ProducaoIndice(p.FrenteId, p.Data, p.Quantidade)).ToList();
        var mapa = CalculoIndices.Calcular(entradas, apt, prod, ate);

        var linhas = frentes.Select(f =>
        {
            mapa.TryGetValue(f.Id, out var ix);
            return new RelatorioLinhaDto
            {
                FrenteId = f.Id,
                FrenteNome = f.Nome,
                Unidade = f.Unidade,
                Hh = ix?.HhTotal ?? 0,
                Quantidade = ix?.Quantidade ?? 0,
                HhPorUnidade = ix?.HhPorUnidade,
                DesvioPercentual = ix?.DesvioPercentual,
                QuantidadePrevista = f.QuantidadePrevista,
                QuantidadeConcluida = f.QuantidadeConcluida,
                PercentualAvanco = f.QuantidadePrevista <= 0
                    ? 0
                    : decimal.Round(f.QuantidadeConcluida / f.QuantidadePrevista * 100, 1),
                AcoEstimadoKg = ix?.AcoEstimadoKg,
                CustoPorUnidade = verCusto ? ix?.CustoPorUnidade : null
            };
        }).ToList();

        var nota = tipoNorm == "avanco"
            ? "O aço é estimado pela taxa de armadura cadastrada na frente. Consumo real exige apontamento de insumo, hoje no almoxarifado."
            : "HH vêm do apontamento; a quantidade vem dos lançamentos na frente. Índice só existe onde as duas pontas foram informadas.";

        return new RelatorioDto
        {
            Tipo = tipoNorm,
            Periodo = rotulo,
            Linhas = linhas,
            Nota = nota
        };
    }

    public async Task<byte[]> GerarCsvAsync(
        string tipo,
        string periodo,
        Guid? obraId,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var rel = await ObterAsync(tipo, periodo, obraId, quem, cancellationToken);
        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        if (rel.Tipo == "avanco")
        {
            sb.AppendLine("Frente;Previsto;Feito;Percentual;AcoEstimadoKg");
            foreach (var l in rel.Linhas)
            {
                sb.AppendLine(string.Join(';',
                    Csv(l.FrenteNome),
                    l.QuantidadePrevista.ToString("0.###", CultureInfo.InvariantCulture),
                    l.QuantidadeConcluida.ToString("0.###", CultureInfo.InvariantCulture),
                    l.PercentualAvanco.ToString("0.#", CultureInfo.InvariantCulture),
                    l.AcoEstimadoKg?.ToString("0.#", CultureInfo.InvariantCulture) ?? ""));
            }
        }
        else
        {
            sb.AppendLine("Frente;HH;Quantidade;Unidade;HHporUnidade;DesvioPercentual;CustoPorUnidade");
            foreach (var l in rel.Linhas)
            {
                sb.AppendLine(string.Join(';',
                    Csv(l.FrenteNome),
                    l.Hh.ToString("0.##", CultureInfo.InvariantCulture),
                    l.Quantidade.ToString("0.###", CultureInfo.InvariantCulture),
                    Csv(l.Unidade),
                    l.HhPorUnidade?.ToString("0.####", CultureInfo.InvariantCulture) ?? "",
                    l.DesvioPercentual?.ToString("0.#", CultureInfo.InvariantCulture) ?? "",
                    l.CustoPorUnidade?.ToString("0.##", CultureInfo.InvariantCulture) ?? ""));
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static (DateOnly De, DateOnly Ate, string Rotulo) Janela(string? periodo, DateOnly hoje)
    {
        var p = (periodo ?? "hoje").Trim().ToLowerInvariant();
        return p switch
        {
            "semana" => (hoje.AddDays(-6), hoje, "semana"),
            "mes" or "mês" => (hoje.AddDays(1 - CalculoIndices.DiasUteisRitmo), hoje, "mes"),
            _ => (hoje, hoje, "hoje")
        };
    }

    private static string Csv(string valor) =>
        valor.Contains(';') || valor.Contains('"')
            ? $"\"{valor.Replace("\"", "\"\"")}\""
            : valor;
}
