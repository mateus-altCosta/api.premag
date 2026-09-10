using Premag.Core.Enums;

namespace Premag.Core.Entities;

public class Ocorrencia : EntidadeTenant
{
    public TipoOcorrencia Tipo { get; set; }
    public SeveridadeOcorrencia Severidade { get; set; }
    public Guid? ColaboradorId { get; set; }
    public Guid? FrenteId { get; set; }
    public Guid? EquipeId { get; set; }
    public DateOnly Data { get; set; }
    public DateTimeOffset DetectadaEm { get; set; }
    public TimeOnly? JanelaInicio { get; set; }
    public int? MinutosDecorridos { get; set; }
    public DateTimeOffset? NotificadaEm { get; set; }
    public DateTimeOffset? ReconhecidaEm { get; set; }
    public Guid? ReconhecidaPorId { get; set; }
    public string? Justificativa { get; set; }
    public Guid? ApontamentoGeradoId { get; set; }

    public Colaborador? Colaborador { get; set; }
    public Frente? Frente { get; set; }
    public Equipe? Equipe { get; set; }
    public Usuario? ReconhecidaPor { get; set; }
    public Apontamento? ApontamentoGerado { get; set; }
}
