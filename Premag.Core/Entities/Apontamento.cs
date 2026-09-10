using Premag.Core.Enums;

namespace Premag.Core.Entities;

public class Apontamento : EntidadeTenant
{
    public Guid ClienteUuid { get; set; }
    public Guid ColaboradorId { get; set; }
    public Guid FrenteId { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }
    public int? MinutosEfetivos { get; set; }
    public Guid? MotivoParadaId { get; set; }
    public string? Observacao { get; set; }
    public OrigemApontamento Origem { get; set; } = OrigemApontamento.App;
    public bool JornadaNaoVerificada { get; set; }
    public string? DispositivoId { get; set; }
    public Guid CriadoPorId { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public Guid? AlteradoPorId { get; set; }
    public DateTimeOffset? AlteradoEm { get; set; }
    public bool Excluido { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
    public Frente Frente { get; set; } = null!;
    public MotivoParada? MotivoParada { get; set; }
    public Usuario CriadoPor { get; set; } = null!;
    public Usuario? AlteradoPor { get; set; }
}
