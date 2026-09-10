namespace Premag.Core.Entities;

public class MotivoParada : EntidadeTenant
{
    public string Nome { get; set; } = string.Empty;
    public bool ExigeObservacao { get; set; }
}
