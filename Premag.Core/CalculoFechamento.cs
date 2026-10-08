namespace Premag.Core;

/// <summary>RN-15: o dia fecha pelo calendário (DiasFechamento) ou por registro explícito em FechamentoDia.</summary>
public static class CalculoFechamento
{
    public static bool FechadoPorCalendario(DateOnly data, DateOnly hoje, int diasFechamento) =>
        data < hoje.AddDays(-Math.Max(0, diasFechamento));

    /// <summary>
    /// Planta (EquipeId nulo) fecha todas as equipes; registro da equipe fecha só ela.
    /// ReabertoEm preenchido anula aquele registro até um novo fechamento.
    /// </summary>
    public static bool FechadoPorRegistro(
        DateOnly data,
        Guid? equipeId,
        IEnumerable<RegistroFechamento> registros)
    {
        return registros.Any(r =>
            r.Data == data
            && r.ReabertoEm is null
            && (r.EquipeId is null || (equipeId is Guid eq && r.EquipeId == eq)));
    }

    /// <summary>
    /// Consulta da planta (EquipeId nulo): fechado se qualquer registro do dia ainda vale.
    /// Equipe específica segue FechadoPorRegistro (planta trava todas).
    /// </summary>
    public static bool FechadoNaConsulta(
        DateOnly data,
        Guid? equipeId,
        IEnumerable<RegistroFechamento> registros)
    {
        if (equipeId is Guid eq)
            return FechadoPorRegistro(data, eq, registros);
        return registros.Any(r => r.Data == data && r.ReabertoEm is null);
    }

    /// <summary>
    /// Planta reabre todos os registros vigentes do dia.
    /// Equipe reabre o próprio e o da planta (que trava todas).
    /// </summary>
    public static bool EncaixaReabertura(Guid? equipePedido, Guid? equipeRegistro) =>
        equipePedido is null || equipeRegistro is null || equipePedido == equipeRegistro;
}

public readonly record struct RegistroFechamento(DateOnly Data, Guid? EquipeId, DateTimeOffset? ReabertoEm);
