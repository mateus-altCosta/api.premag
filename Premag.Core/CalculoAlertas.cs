using Premag.Core.Enums;

namespace Premag.Core;

public sealed record ColaboradorAlerta(
    Guid Id,
    Guid EquipeId,
    bool Ativo,
    SituacaoJornada Situacao,
    TimeOnly? UltimoFimOuJornadaInicio,
    TimeOnly? ServicoAbertoInicio,
    int MinutosTrabalhados);

public sealed record FrenteAlerta(
    Guid Id,
    bool Interna,
    decimal QuantidadeHoje,
    int MinutosEfetivosHoje,
    bool TemFotoAvancoHoje);

public sealed record AlertaDetectado(
    TipoOcorrencia Tipo,
    SeveridadeOcorrencia Severidade,
    Guid? ColaboradorId,
    Guid? FrenteId,
    Guid? EquipeId,
    TimeOnly? JanelaInicio,
    int? MinutosDecorridos,
    string Titulo,
    string Detalhe);

/// <summary>RN-07: ociosidade e demais alertas do dia — cálculo puro (job no servidor).</summary>
public static class CalculoAlertas
{
    public static IReadOnlyList<AlertaDetectado> Detectar(
        DateOnly data,
        TimeOnly agora,
        TimeOnly intervaloInicio,
        TimeOnly intervaloFim,
        int minutosOciosidade,
        int minutosSemServico,
        int minutosAbertoDemais,
        IReadOnlyList<ColaboradorAlerta> colaboradores,
        IReadOnlyList<FrenteAlerta> frentes)
    {
        var lista = new List<AlertaDetectado>();

        foreach (var c in colaboradores.Where(x => x.Ativo && x.Situacao == SituacaoJornada.Presente))
        {
            if (c.ServicoAbertoInicio is TimeOnly iniAberto)
            {
                var aberto = CalculoApontamento.MinutosEfetivos(iniAberto, agora, intervaloInicio, intervaloFim);
                if (aberto >= minutosAbertoDemais)
                {
                    lista.Add(new AlertaDetectado(
                        TipoOcorrencia.ServicoAbertoDemais,
                        SeveridadeOcorrencia.Equipe,
                        c.Id, null, c.EquipeId, iniAberto, aberto,
                        "Serviço aberto há muito tempo",
                        $"Há mais de {minutosAbertoDemais / 60} h no mesmo serviço."));
                }
                continue;
            }

            var origem = c.UltimoFimOuJornadaInicio ?? agora;
            var ocioso = CalculoApontamento.MinutosGap(origem, agora, intervaloInicio, intervaloFim);
            if (ocioso >= minutosOciosidade)
            {
                lista.Add(new AlertaDetectado(
                    TipoOcorrencia.OciosidadeEscalonada,
                    SeveridadeOcorrencia.GerenciaDiretoria,
                    c.Id, null, c.EquipeId, origem, ocioso,
                    "Sem alocação há mais de 1 hora",
                    "Colaborador presente e fora de frente. Escalonado para gerência e diretoria."));
            }
            else if (ocioso >= minutosSemServico)
            {
                lista.Add(new AlertaDetectado(
                    TipoOcorrencia.SemServico,
                    SeveridadeOcorrencia.Equipe,
                    c.Id, null, c.EquipeId, origem, ocioso,
                    "Sem serviço aberto",
                    $"{ocioso} min sem apontamento desde o último término ou o início da jornada."));
            }
        }

        foreach (var f in frentes.Where(x => !x.Interna))
        {
            if (f.MinutosEfetivosHoje > 0 && f.QuantidadeHoje <= 0)
            {
                lista.Add(new AlertaDetectado(
                    TipoOcorrencia.FrenteSemQuantidade,
                    SeveridadeOcorrencia.Equipe,
                    null, f.Id, null, null, f.MinutosEfetivosHoje,
                    "Frente sem quantidade",
                    "Houve apontamento nesta frente hoje e nenhuma quantidade foi lançada."));
            }
            if (f.QuantidadeHoje > 0 && !f.TemFotoAvancoHoje)
            {
                lista.Add(new AlertaDetectado(
                    TipoOcorrencia.AvancoSemFoto,
                    SeveridadeOcorrencia.Equipe,
                    null, f.Id, null, null, null,
                    "Avanço sem registro fotográfico",
                    "Houve quantidade apontada hoje e nenhuma foto de avanço anexada."));
            }
        }

        return lista;
    }
}
