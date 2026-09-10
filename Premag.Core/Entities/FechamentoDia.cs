namespace Premag.Core.Entities;

public class FechamentoDia : EntidadeTenant
{
    public DateOnly Data { get; set; }
    public Guid? EquipeId { get; set; }
    public DateTimeOffset FechadoEm { get; set; }
    public Guid FechadoPorId { get; set; }
    public DateTimeOffset? ReabertoEm { get; set; }
    public Guid? ReabertoPorId { get; set; }
    public string? MotivoReabertura { get; set; }

    public Equipe? Equipe { get; set; }
    public Usuario FechadoPor { get; set; } = null!;
    public Usuario? ReabertoPor { get; set; }
}
