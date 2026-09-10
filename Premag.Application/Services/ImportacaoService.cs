using System.Globalization;
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

namespace Premag.Application.Services;

public class ImportacaoService : IImportacaoService
{
    private readonly ApplicationDbContext _db;
    private readonly IRelogio _relogio;

    public ImportacaoService(ApplicationDbContext db, IRelogio relogio)
    {
        _db = db;
        _relogio = relogio;
    }

    public async Task<ImportacaoResultadoDto> ImportarAfdAsync(
        string conteudo,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        GarantirGerente(quem);
        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        var marcas = ParserAfd.Ler(conteudo);
        var avisos = new List<string>();
        if (marcas.Count == 0)
            return new ImportacaoResultadoDto { Lidos = 0, Gravados = 0, Ignorados = 0, Avisos = ["Nenhuma marcação tipo 3/4 encontrada."] };

        var colaboradores = await _db.Colaboradores.ToListAsync(cancellationToken);
        var porDoc = new Dictionary<string, Colaborador>(StringComparer.Ordinal);
        foreach (var c in colaboradores)
        {
            foreach (var chave in ChavesDocumento(c))
                porDoc.TryAdd(chave, c);
        }

        var gravados = 0;
        var ignorados = 0;
        var grupos = marcas.GroupBy(m => (m.Documento, m.Data));
        foreach (var g in grupos)
        {
            if (!porDoc.TryGetValue(NormalizarDoc(g.Key.Documento), out var colab)
                && !porDoc.TryGetValue(g.Key.Documento, out colab))
            {
                ignorados += g.Count();
                avisos.Add($"PIS/documento {g.Key.Documento} sem colaborador (Código externo ou matrícula).");
                continue;
            }

            var horas = g.Select(x => x.Hora).Distinct().OrderBy(h => h).ToList();
            var entrada = horas[0];
            TimeOnly? saida = horas.Count >= 2 ? horas[^1] : null;
            var minutos = saida is TimeOnly s
                ? CalculoApontamento.MinutosEfetivos(entrada, s, config.IntervaloInicio, config.IntervaloFim)
                : 0;

            var jornada = await _db.JornadasDia
                .FirstOrDefaultAsync(j => j.ColaboradorId == colab.Id && j.Data == g.Key.Data, cancellationToken);
            if (jornada is null)
            {
                jornada = new JornadaDia
                {
                    TenantId = _db.TenantId,
                    ColaboradorId = colab.Id,
                    Data = g.Key.Data,
                    Origem = OrigemJornada.Afd
                };
                _db.JornadasDia.Add(jornada);
            }

            jornada.Entrada = entrada;
            jornada.Saida = saida;
            jornada.IntervaloMinutos = Math.Max(0,
                CalculoApontamento.ParaMinutos(config.IntervaloFim) - CalculoApontamento.ParaMinutos(config.IntervaloInicio));
            jornada.MinutosApurados = minutos;
            jornada.Situacao = SituacaoJornada.Presente;
            jornada.Origem = OrigemJornada.Afd;
            jornada.ImportadoEm = _relogio.UtcAgora;
            gravados++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new ImportacaoResultadoDto
        {
            Lidos = marcas.Count,
            Gravados = gravados,
            Ignorados = ignorados,
            Avisos = avisos.Distinct().Take(20).ToList()
        };
    }

    public async Task<ImportacaoResultadoDto> ImportarColaboradoresAsync(
        string csv,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        GarantirGerente(quem);
        var linhas = csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var avisos = new List<string>();
        var lidos = 0;
        var gravados = 0;
        var ignorados = 0;
        var equipes = await _db.Equipes.ToListAsync(cancellationToken);
        var existentes = await _db.Colaboradores.ToListAsync(cancellationToken);
        var agora = _relogio.UtcAgora;

        foreach (var bruta in linhas)
        {
            var linha = bruta.Trim();
            if (linha.Length == 0)
                continue;
            var cols = linha.Split(';', ',');
            if (cols.Length < 4)
            {
                ignorados++;
                continue;
            }

            var c0 = cols[0].Trim().Trim('"');
            if (lidos == 0 && c0.StartsWith("matricula", StringComparison.OrdinalIgnoreCase))
                continue;

            lidos++;
            var matricula = c0;
            var nome = cols[1].Trim().Trim('"');
            var funcao = cols[2].Trim().Trim('"');
            var equipeNome = cols[3].Trim().Trim('"');
            decimal? custo = null;
            if (cols.Length >= 5
                && decimal.TryParse(cols[4].Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var ch))
                custo = ch;
            string? codigo = cols.Length >= 6 ? cols[5].Trim().Trim('"') : null;
            if (string.IsNullOrWhiteSpace(codigo))
                codigo = null;

            if (string.IsNullOrWhiteSpace(matricula) || string.IsNullOrWhiteSpace(nome))
            {
                ignorados++;
                avisos.Add($"Linha {lidos}: matrícula e nome são obrigatórios.");
                continue;
            }

            var equipe = equipes.FirstOrDefault(e => e.Nome.Equals(equipeNome, StringComparison.OrdinalIgnoreCase));
            if (equipe is null)
            {
                ignorados++;
                avisos.Add($"Linha {lidos}: equipe '{equipeNome}' não encontrada.");
                continue;
            }

            var colab = existentes.FirstOrDefault(c => c.Matricula == matricula);
            if (colab is null)
            {
                colab = new Colaborador
                {
                    TenantId = _db.TenantId,
                    Matricula = matricula,
                    Nome = nome,
                    Funcao = string.IsNullOrWhiteSpace(funcao) ? "—" : funcao,
                    EquipeId = equipe.Id,
                    CustoHora = custo,
                    OrigemCadastro = OrigemCadastro.Folha,
                    CodigoExterno = codigo,
                    Ativo = true,
                    CriadoEm = agora,
                    AlteradoEm = agora
                };
                _db.Colaboradores.Add(colab);
                existentes.Add(colab);
            }
            else
            {
                colab.Nome = nome;
                colab.Funcao = string.IsNullOrWhiteSpace(funcao) ? colab.Funcao : funcao;
                colab.EquipeId = equipe.Id;
                colab.CustoHora = custo;
                colab.OrigemCadastro = OrigemCadastro.Folha;
                if (codigo is not null)
                    colab.CodigoExterno = codigo;
                colab.AlteradoEm = agora;
                colab.Ativo = true;
            }

            gravados++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new ImportacaoResultadoDto
        {
            Lidos = lidos,
            Gravados = gravados,
            Ignorados = ignorados,
            Avisos = avisos.Take(20).ToList()
        };
    }

    private static void GarantirGerente(UsuarioLogado quem)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só Gerente ou acima importa folha/AFD.", 403);
    }

    private static IEnumerable<string> ChavesDocumento(Colaborador c)
    {
        if (!string.IsNullOrWhiteSpace(c.CodigoExterno))
            yield return NormalizarDoc(c.CodigoExterno);
        if (!string.IsNullOrWhiteSpace(c.Matricula))
            yield return NormalizarDoc(c.Matricula);
    }

    private static string NormalizarDoc(string bruto)
    {
        var dig = new string(bruto.Where(char.IsDigit).ToArray());
        return dig.TrimStart('0').Length == 0 ? dig : dig.TrimStart('0') is var t && t.Length > 0 ? t : dig;
    }
}
