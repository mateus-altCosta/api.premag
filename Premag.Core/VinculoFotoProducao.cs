namespace Premag.Core;

/// <summary>
/// RN-11: foto de avanço com quantidade só lança Produção se ainda não houver
/// lançamento na frente naquele dia. Se já lançou no encerrar, a foto é evidência
/// e vincula-se ao registro existente — não soma de novo.
/// </summary>
public static class VinculoFotoProducao
{
    public readonly record struct LancamentoDia(
        Guid Id,
        Guid? ApontamentoId,
        Guid? FotoId,
        decimal Quantidade,
        DateTimeOffset RegistradoEm);

    public readonly record struct Decisao(bool LancarNova, Guid? ProducaoIdParaVincular);

    public static Decisao Decidir(
        IReadOnlyList<LancamentoDia> doDia,
        Guid? apontamentoId,
        decimal quantidade)
    {
        if (doDia.Count == 0)
            return new Decisao(true, null);

        if (apontamentoId is Guid aid)
        {
            var doApt = doDia.FirstOrDefault(p => p.ApontamentoId == aid);
            if (doApt.Id != Guid.Empty)
                return new Decisao(false, doApt.FotoId is null ? doApt.Id : null);
        }

        var semFotoMesmaQtd = doDia
            .Where(p => p.FotoId is null && p.Quantidade == quantidade)
            .OrderByDescending(p => p.RegistradoEm)
            .FirstOrDefault();
        if (semFotoMesmaQtd.Id != Guid.Empty)
            return new Decisao(false, semFotoMesmaQtd.Id);

        var semFoto = doDia
            .Where(p => p.FotoId is null)
            .OrderByDescending(p => p.RegistradoEm)
            .FirstOrDefault();
        if (semFoto.Id != Guid.Empty)
            return new Decisao(false, semFoto.Id);

        return new Decisao(false, null);
    }
}
