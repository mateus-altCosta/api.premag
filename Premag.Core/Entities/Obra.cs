using Premag.Core.Enums;

namespace Premag.Core.Entities;

public class Obra : EntidadeTenant
{
    public string Nome { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public string? CodigoSienge { get; set; }
    public string? CentroCusto { get; set; }
    public bool Interna { get; set; }
    public StatusObra Status { get; set; } = StatusObra.EmExecucao;
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataPrevistaFim { get; set; }

    public ICollection<Frente> Frentes { get; set; } = [];
}
