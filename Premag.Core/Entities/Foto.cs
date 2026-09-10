using Premag.Core.Enums;

namespace Premag.Core.Entities;

public class Foto : EntidadeTenant
{
    public Guid ClienteUuid { get; set; }
    public Guid FrenteId { get; set; }
    public Guid? ColaboradorId { get; set; }
    public Guid? ApontamentoId { get; set; }
    public TipoFoto Tipo { get; set; }
    public decimal? Quantidade { get; set; }
    public string? Observacao { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
    public string ThumbKey { get; set; } = string.Empty;
    public int Bytes { get; set; }
    public int Largura { get; set; }
    public int Altura { get; set; }
    public string HashSha256 { get; set; } = string.Empty;
    public DateTimeOffset CapturadaEm { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Guid EnviadaPorId { get; set; }
    public DateOnly? ExpiraEm { get; set; }
    public bool Excluido { get; set; }

    public Frente Frente { get; set; } = null!;
    public Colaborador? Colaborador { get; set; }
    public Apontamento? Apontamento { get; set; }
    public Usuario EnviadaPor { get; set; } = null!;
}
