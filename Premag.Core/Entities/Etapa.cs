namespace Premag.Core.Entities;

public class Etapa : EntidadeTenant
{
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Indireta { get; set; }
}
