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
}

public readonly record struct RegistroFechamento(DateOnly Data, Guid? EquipeId, DateTimeOffset? ReabertoEm);
