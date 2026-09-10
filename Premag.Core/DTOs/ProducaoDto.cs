using System.ComponentModel.DataAnnotations;

namespace Premag.Core.DTOs;

public class RegistrarProducaoDto
{
    [Required] public Guid ClienteUuid { get; set; }
    [Required] public Guid FrenteId { get; set; }
    public DateOnly? Data { get; set; }
    public decimal Quantidade { get; set; }
    public Guid? ApontamentoId { get; set; }
}

public class ProducaoResultadoDto
{
    public Guid Id { get; set; }
    public Guid FrenteId { get; set; }
    public DateOnly Data { get; set; }
    public decimal Quantidade { get; set; }
    public decimal QuantidadeConcluidaFrente { get; set; }
    public IReadOnlyList<string> Avisos { get; set; } = [];
    public decimal? QuantidadeJaLancadaHoje { get; set; }
}
