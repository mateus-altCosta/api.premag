namespace Premag.Core.Entities;

public class Producao : EntidadeTenant
{
    public Guid FrenteId { get; set; }
    public DateOnly Data { get; set; }
    public decimal Quantidade { get; set; }
    public Guid? FotoId { get; set; }
    public Guid? ApontamentoId { get; set; }
    public Guid RegistradoPorId { get; set; }
    public DateTimeOffset RegistradoEm { get; set; }
    public Guid ClienteUuid { get; set; }
    public bool Excluido { get; set; }

    public Frente Frente { get; set; } = null!;
    public Foto? Foto { get; set; }
    public Apontamento? Apontamento { get; set; }
    public Usuario RegistradoPor { get; set; } = null!;
}
