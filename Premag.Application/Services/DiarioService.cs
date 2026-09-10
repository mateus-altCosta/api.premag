using Microsoft.EntityFrameworkCore;
using Premag.Application.Common;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Core.Entities;
using Premag.Core.Enums;
using Premag.Core.Exceptions;
using Premag.Core.Interfaces;
using Premag.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Premag.Application.Services;

public class DiarioService : IDiarioService
{
    private readonly ApplicationDbContext _db;
    private readonly IFotoService _fotos;
    private readonly IRelogio _relogio;

    public DiarioService(
        ApplicationDbContext db,
        IFotoService fotos,
        IRelogio relogio)
    {
        _db = db;
        _fotos = fotos;
        _relogio = relogio;
    }

    public async Task<DiarioDto> ObterAsync(
        DateOnly? data,
        Guid? equipeId,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var dia = data ?? _relogio.HojeSaoPaulo;
        var equipe = await ResolverEquipeAsync(equipeId, quem, cancellationToken);
        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        var agora = HorarioPlanta(config);

        var colaboradoresQuery = _db.Colaboradores.AsNoTracking().AsQueryable();
        if (equipe is not null)
            colaboradoresQuery = colaboradoresQuery.Where(c => c.EquipeId == equipe.Id);
        else if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            colaboradoresQuery = colaboradoresQuery.Where(c => c.EquipeId == quem.EquipeId);

        var colaboradores = await colaboradoresQuery.OrderBy(c => c.Nome).ToListAsync(cancellationToken);
        var ids = colaboradores.Select(c => c.Id).ToList();

        var apontamentos = await _db.Apontamentos
            .AsNoTracking()
            .Include(a => a.Frente).ThenInclude(f => f.Obra)
            .Include(a => a.Frente).ThenInclude(f => f.Etapa)
            .Where(a => ids.Contains(a.ColaboradorId) && a.Data == dia)
            .ToListAsync(cancellationToken);

        var jornadas = await _db.JornadasDia.AsNoTracking()
            .Where(j => ids.Contains(j.ColaboradorId) && j.Data == dia)
            .ToListAsync(cancellationToken);

        var producoes = await _db.Producoes.AsNoTracking()
            .Where(p => p.Data == dia)
            .ToListAsync(cancellationToken);
        var qtdPorFrente = producoes.GroupBy(p => p.FrenteId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));

        var porFrente = new Dictionary<Guid, DiarioFrenteDto>();
        foreach (var a in apontamentos)
        {
            var min = a.MinutosEfetivos
                ?? (a.HoraFim is null
                    ? CalculoApontamento.MinutosEfetivos(a.HoraInicio, agora, config.IntervaloInicio, config.IntervaloFim)
                    : 0);
            if (!porFrente.TryGetValue(a.FrenteId, out var item))
            {
                item = new DiarioFrenteDto
                {
                    FrenteId = a.FrenteId,
                    Nome = a.Frente.Nome,
                    ObraNome = a.Frente.Obra.Nome,
                    Cor = a.Frente.Cor,
                    Unidade = a.Frente.Unidade,
                    Quantidade = qtdPorFrente.GetValueOrDefault(a.FrenteId),
                    SemQuantidade = false
                };
                porFrente[a.FrenteId] = item;
            }

            item.Horas += min / 60m;
            item.Pessoas = apontamentos.Where(x => x.FrenteId == a.FrenteId).Select(x => x.ColaboradorId).Distinct().Count();
            item.SemQuantidade = item.Quantidade <= 0 && !(a.Frente.Etapa?.Indireta ?? false);
        }

        var funcoes = colaboradores
            .Where(c => (jornadas.FirstOrDefault(j => j.ColaboradorId == c.Id)?.Situacao ?? SituacaoJornada.Presente) == SituacaoJornada.Presente && c.Ativo)
            .GroupBy(c => c.Funcao)
            .Select(g => new DiarioFuncaoDto { Funcao = g.Key, Quantidade = g.Count() })
            .OrderBy(x => x.Funcao)
            .ToList();

        var situacoes = colaboradores.Select(c =>
        {
            var sit = jornadas.FirstOrDefault(j => j.ColaboradorId == c.Id)?.Situacao
                ?? (c.Ativo ? SituacaoJornada.Presente : SituacaoJornada.Afastado);
            return sit;
        })
            .Where(s => s != SituacaoJornada.Presente)
            .GroupBy(s => s)
            .Select(g => new DiarioSituacaoDto { Situacao = g.Key.ToString(), Quantidade = g.Count() })
            .ToList();

        var fotos = await _fotos.ListarAsync(dia, null, null, quem, cancellationToken);
        if (equipe is not null)
            fotos = fotos.Where(f => colaboradores.Any(c => c.Id == f.ColaboradorId) || f.ColaboradorId is null).ToList();

        return new DiarioDto
        {
            Data = dia,
            Escopo = equipe?.Nome ?? "consolidado de todas as equipes",
            Frentes = porFrente.Values.OrderBy(f => f.Nome).Select(f =>
            {
                f.Horas = decimal.Round(f.Horas, 1);
                return f;
            }).ToList(),
            Funcoes = funcoes,
            Situacoes = situacoes,
            Fotos = fotos,
            FotosAvanco = fotos.Count(f => f.Tipo == TipoFoto.Avanco)
        };
    }

    public async Task<byte[]> GerarPdfAsync(
        DateOnly? data,
        Guid? equipeId,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var diario = await ObterAsync(data, equipeId, quem, cancellationToken);
        var imagens = new List<(FotoDto Foto, byte[] Bytes)>();
        foreach (var f in diario.Fotos.Take(12))
        {
            var arq = await _fotos.ObterArquivoAsync(f.Id, cancellationToken);
            if (arq is not null)
                imagens.Add((f, arq.Value.Bytes));
        }

        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(32);
                page.Size(PageSizes.A4);
                page.Header().Column(col =>
                {
                    col.Item().Text("PREMAG — Diário de obra").FontSize(16).Bold();
                    col.Item().Text($"{diario.Data:dd/MM/yyyy} · {diario.Escopo}").FontSize(10).FontColor(Colors.Grey.Darken1);
                });
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text("Serviços do dia").Bold();
                    if (diario.Frentes.Count == 0)
                        col.Item().Text("Nenhum serviço apontado neste dia.").FontSize(10);
                    foreach (var f in diario.Frentes)
                    {
                        var qtd = f.Quantidade > 0 ? $" · {f.Quantidade:0.##} {f.Unidade}" : (f.SemQuantidade ? " · sem quantidade" : "");
                        col.Item().Text($"{f.Nome} — {f.ObraNome} · {f.Horas:0.0}h · {f.Pessoas} pessoas{qtd}").FontSize(10);
                    }

                    col.Item().Text("Equipe").Bold();
                    foreach (var fn in diario.Funcoes)
                        col.Item().Text($"{fn.Funcao}: {fn.Quantidade}").FontSize(10);
                    if (diario.Situacoes.Count > 0)
                    {
                        col.Item().Text("Observações").Bold();
                        foreach (var s in diario.Situacoes)
                            col.Item().Text($"{s.Situacao}: {s.Quantidade}").FontSize(10);
                    }

                    if (imagens.Count > 0)
                    {
                        col.Item().Text("Registro fotográfico").Bold();
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            foreach (var (foto, bytes) in imagens)
                            {
                                t.Cell().Padding(4).Column(c =>
                                {
                                    c.Item().Image(bytes).FitArea();
                                    c.Item().Text($"{foto.Tipo} · {foto.FrenteNome}").FontSize(8);
                                });
                            }
                        });
                    }
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Gerado em ").FontSize(8);
                    t.Span(_relogio.AgoraSaoPaulo.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private async Task<Equipe?> ResolverEquipeAsync(Guid? equipeId, UsuarioLogado quem, CancellationToken cancellationToken)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
        {
            var id = quem.EquipeId
                ?? throw new RegraNegocioException("EQUIPE_OBRIGATORIA", "Encarregado sem equipe.", 403);
            return await _db.Equipes.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
                ?? throw new RegraNegocioException("EQUIPE_NAO_ENCONTRADA", "Equipe não encontrada.", 404);
        }

        if (equipeId is Guid eid)
        {
            return await _db.Equipes.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eid, cancellationToken)
                ?? throw new RegraNegocioException("EQUIPE_NAO_ENCONTRADA", "Equipe não encontrada.", 404);
        }

        return null;
    }

    private TimeOnly HorarioPlanta(Configuracao config)
    {
        var agora = _relogio.HoraSaoPaulo;
        if (agora < config.JornadaInicio) return config.JornadaInicio;
        if (agora > config.JornadaFim) return config.JornadaFim;
        return agora;
    }
}
