namespace Premag.Core;

/// <summary>
/// GPS em numeric(9,6): arredonda 6 casas. Fora de −90/90 ou −180/180 (ex.: ponto
/// lido como milhar no Windows pt-BR) descarta a coordenada em vez de estourar o INSERT.
/// </summary>
public static class CoordenadaGps
{
    public static (decimal? Latitude, decimal? Longitude) Normalizar(decimal? latitude, decimal? longitude) =>
        (Ajustar(latitude, 90), Ajustar(longitude, 180));

    private static decimal? Ajustar(decimal? valor, decimal teto)
    {
        if (valor is null)
            return null;
        var x = decimal.Round(valor.Value, 6, MidpointRounding.AwayFromZero);
        if (x < -teto || x > teto)
            return null;
        return x;
    }
}
