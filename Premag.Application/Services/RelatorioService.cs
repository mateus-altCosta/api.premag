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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
                AcoEstimadoKg = f.TaxaAcoKgPorUnidade is > 0 ? ix?.AcoEstimadoKg : null,
                TaxaAcoUnidade = UnidadeAco.Normalizar(f.TaxaAcoUnidade),
                CustoPorUnidade = verCusto ? ix?.CustoPorUnidade : null,
                AmostraInsuficiente = ix?.AmostraInsuficiente ?? true
            };
        }).ToList();

        var nota = tipoNorm == "avanco"
            ? "Un. Est. é o aço estimado (kg) quando a frente tem taxa de armadura; nas demais, a quantidade feita na unidade da frente (m³, m², m). Consumo real de aço exige apontamento de insumo, hoje no almoxarifado."
            : "HH vêm do apontamento; a quantidade vem dos lançamentos na frente. O índice aparece como HH/m³, HH/kg etc., conforme a unidade da frente.";

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
        GarantirAmostraExportacao(rel, quem);

        var sb = new StringBuilder();
        sb.Append('\uFEFF');
        if (rel.Tipo == "avanco")
        {
            sb.AppendLine("Frente;Previsto;Feito;Unidade;Percentual;UnEst;UnidadeEst");
            foreach (var l in rel.Linhas)
            {
                var (unEst, unEstUn) = RelatorioRotulos.UnidadeEstimada(
                    l.AcoEstimadoKg, l.TaxaAcoUnidade, l.QuantidadeConcluida, l.Unidade);
                sb.AppendLine(string.Join(';',
                    Csv(l.FrenteNome),
                    l.QuantidadePrevista.ToString("0.###", CultureInfo.InvariantCulture),
                    l.QuantidadeConcluida.ToString("0.###", CultureInfo.InvariantCulture),
                    Csv(l.Unidade),
                    l.PercentualAvanco.ToString("0.#", CultureInfo.InvariantCulture),
                    unEst.ToString("0.###", CultureInfo.InvariantCulture),
                    Csv(unEstUn)));
            }
        }
        else
        {
            sb.AppendLine("Frente;HH;Quantidade;Unidade;HHporUnidade;Indice;DesvioPercentual;CustoPorUnidade");
            foreach (var l in rel.Linhas)
            {
                sb.AppendLine(string.Join(';',
                    Csv(l.FrenteNome),
                    l.Hh.ToString("0.##", CultureInfo.InvariantCulture),
                    l.Quantidade.ToString("0.###", CultureInfo.InvariantCulture),
                    Csv(l.Unidade),
                    l.HhPorUnidade?.ToString("0.####", CultureInfo.InvariantCulture) ?? "",
                    Csv(RelatorioRotulos.HhPorUnidade(l.Unidade)),
                    l.DesvioPercentual?.ToString("0.#", CultureInfo.InvariantCulture) ?? "",
                    l.CustoPorUnidade?.ToString("0.##", CultureInfo.InvariantCulture) ?? ""));
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> GerarPdfAsync(
        string tipo,
        string periodo,
        Guid? obraId,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var rel = await ObterAsync(tipo, periodo, obraId, quem, cancellationToken);
        GarantirAmostraExportacao(rel, quem);

        QuestPDF.Settings.License = LicenseType.Community;
        var titulo = rel.Tipo == "avanco" ? "Avanço" : "Produtividade";
        var unidadesHh = rel.Linhas
            .Select(l => l.Unidade)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct()
            .ToList();
        var hhCab = unidadesHh.Count == 1
            ? RelatorioRotulos.HhPorUnidade(unidadesHh[0])
            : "HH/un.";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Size(PageSizes.A4);
                page.Header().Column(col =>
                {
                    col.Item().Text($"PREMAG — Relatório de {titulo.ToLowerInvariant()}").FontSize(16).Bold();
                    col.Item().Text($"Período: {rel.Periodo}").FontSize(10).FontColor(Colors.Grey.Darken1);
                });
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    if (rel.Linhas.Count == 0)
                        col.Item().Text("Nenhuma frente no período.").FontSize(10);
                    else
                    {
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            t.Header(h =>
                            {
                                if (rel.Tipo == "avanco")
                                {
                                    h.Cell().Text("Frente").Bold().FontSize(9);
                                    h.Cell().Text("Previsto").Bold().FontSize(9);
                                    h.Cell().Text("Feito").Bold().FontSize(9);
                                    h.Cell().Text("%").Bold().FontSize(9);
                                    h.Cell().Text("Un. Est.").Bold().FontSize(9);
                                }
                                else
                                {
                                    h.Cell().Text("Frente").Bold().FontSize(9);
                                    h.Cell().Text("HH").Bold().FontSize(9);
                                    h.Cell().Text("Qtd").Bold().FontSize(9);
                                    h.Cell().Text(hhCab).Bold().FontSize(9);
                                    h.Cell().Text("vs cotado").Bold().FontSize(9);
                                }
                            });
                            foreach (var l in rel.Linhas)
                            {
                                t.Cell().PaddingVertical(2).Text(l.FrenteNome).FontSize(9);
                                if (rel.Tipo == "avanco")
                                {
                                    t.Cell().PaddingVertical(2).Text($"{l.QuantidadePrevista:0.###} {l.Unidade}").FontSize(9);
                                    t.Cell().PaddingVertical(2).Text($"{l.QuantidadeConcluida:0.###} {l.Unidade}").FontSize(9);
                                    t.Cell().PaddingVertical(2).Text($"{l.PercentualAvanco:0.#}%").FontSize(9);
                                    var (unEst, unEstUn) = RelatorioRotulos.UnidadeEstimada(
                                        l.AcoEstimadoKg, l.TaxaAcoUnidade, l.QuantidadeConcluida, l.Unidade);
                                    t.Cell().PaddingVertical(2).Text($"{unEst:0.###} {unEstUn}").FontSize(9);
                                }
                                else
                                {
                                    t.Cell().PaddingVertical(2).Text(l.Hh.ToString("0.##")).FontSize(9);
                                    t.Cell().PaddingVertical(2).Text($"{l.Quantidade:0.###} {l.Unidade}").FontSize(9);
                                    var hhTxt = l.HhPorUnidade is decimal hpu
                                        ? (unidadesHh.Count == 1
                                            ? hpu.ToString("0.####")
                                            : $"{hpu:0.####} {RelatorioRotulos.HhPorUnidade(l.Unidade)}")
                                        : "—";
                                    t.Cell().PaddingVertical(2).Text(hhTxt).FontSize(9);
                                    t.Cell().PaddingVertical(2).Text(
                                        l.DesvioPercentual is decimal d
                                            ? $"{(d > 0 ? "+" : "")}{d:0.#}%"
                                            : "—").FontSize(9);
                                }
                            }
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(rel.Nota))
                        col.Item().Text(rel.Nota).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Gerado em ").FontSize(8);
                    t.Span(_relogio.AgoraSaoPaulo.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private static void GarantirAmostraExportacao(RelatorioDto rel, UsuarioLogado quem)
    {
        // Diretoria (e Admin) exporta mesmo com amostra curta; o * permanece na tela.
        if (Permissoes.Tem(quem.Perfil, Permissoes.Diretoria))
            return;
        if (rel.Tipo == "produtividade"
            && CalculoIndices.BloqueiaExportacaoIndice(rel.Linhas.Select(l => new IndiceFrente
            {
                AmostraInsuficiente = l.AmostraInsuficiente,
                Quantidade = l.Quantidade
            })))
        {
            throw new RegraNegocioException(
                "AMOSTRA_INSUFICIENTE",
                "Não exporta índice de custo com menos de 5 lançamentos na frente. Espere mais amostra ou use o avanço.",
                422);
        }
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
