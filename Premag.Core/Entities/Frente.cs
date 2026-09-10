namespace Premag.Core.Entities;

public class Frente : EntidadeTenant
{
    public Guid ObraId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Guid EtapaId { get; set; }
    public Guid? EquipeId { get; set; }
    public string Unidade { get; set; } = "un";
    public decimal QuantidadePrevista { get; set; }
    public decimal QuantidadeConcluida { get; set; }
    public decimal? TaxaAcoKgPorUnidade { get; set; }
    public decimal? HhOrcadoPorUnidade { get; set; }
    public string? ItemOrcamentoSienge { get; set; }
    public string Cor { get; set; } = "#4A5560";
    public bool Ativa { get; set; } = true;

    public Obra Obra { get; set; } = null!;
    public Etapa Etapa { get; set; } = null!;
    public Equipe? Equipe { get; set; }
}
