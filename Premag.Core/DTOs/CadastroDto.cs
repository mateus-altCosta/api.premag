using System.ComponentModel.DataAnnotations;
using Premag.Core.Enums;

namespace Premag.Core.DTOs;

public class CatalogoDto
{
    public IReadOnlyList<EtapaDto> Etapas { get; set; } = [];
    public IReadOnlyList<MotivoParadaDto> MotivosParada { get; set; } = [];
    public IReadOnlyList<string> Unidades { get; set; } = ["pç", "m³", "m", "kg", "h", "un"];
    public ConfiguracaoDto Configuracao { get; set; } = new();
}

public class EtapaDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public bool Indireta { get; set; }
}

public class MotivoParadaDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool ExigeObservacao { get; set; }
}

public class ConfiguracaoDto
{
    public int JornadaPadraoMinutos { get; set; }
    public string JornadaInicio { get; set; } = "07:00";
    public string JornadaFim { get; set; } = "16:48";
    public string IntervaloInicio { get; set; } = "12:00";
    public string IntervaloFim { get; set; } = "13:00";
}

public class EquipeDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cor { get; set; } = string.Empty;
    public bool Ativa { get; set; }
    public int Presentes { get; set; }
    public Guid? EncarregadoId { get; set; }
}

public class ColaboradorDto
{
    public Guid Id { get; set; }
    public string Matricula { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Funcao { get; set; } = string.Empty;
    public Guid EquipeId { get; set; }
    public string EquipeNome { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public OrigemCadastro OrigemCadastro { get; set; }
    public decimal? CustoHora { get; set; }
}

public class CriarColaboradorDto
{
    [Required] public string Matricula { get; set; } = string.Empty;
    [Required] public string Nome { get; set; } = string.Empty;
    public string Funcao { get; set; } = "—";
    [Required] public Guid EquipeId { get; set; }
}

public class TransferirColaboradorDto
{
    [Required] public Guid EquipeId { get; set; }
}

public class ObraDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public string? CodigoSienge { get; set; }
    public bool Interna { get; set; }
    public StatusObra Status { get; set; }
    public int QuantidadeFrentes { get; set; }
    public decimal PercentualAvanco { get; set; }
}

public class CriarObraDto
{
    [Required] public string Nome { get; set; } = string.Empty;
    [Required] public string Cliente { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public string? CodigoSienge { get; set; }
}

public class AtualizarObraDto : CriarObraDto
{
    public StatusObra Status { get; set; } = StatusObra.EmExecucao;
}

public class FrenteDto
{
    public Guid Id { get; set; }
    public Guid ObraId { get; set; }
    public string ObraNome { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public Guid EtapaId { get; set; }
    public string EtapaNome { get; set; } = string.Empty;
    public bool EtapaIndireta { get; set; }
    public Guid? EquipeId { get; set; }
    public string? EquipeNome { get; set; }
    public string Unidade { get; set; } = "un";
    public decimal QuantidadePrevista { get; set; }
    public decimal QuantidadeConcluida { get; set; }
    public decimal PercentualAvanco { get; set; }
    public decimal? TaxaAcoKgPorUnidade { get; set; }
    public decimal? HhOrcadoPorUnidade { get; set; }
    public string Cor { get; set; } = string.Empty;
    public bool Ativa { get; set; }
    public decimal? HhDireto { get; set; }
    public decimal? HhIndiretoRateado { get; set; }
    public decimal? HhTotal { get; set; }
    public decimal? HhPorUnidade { get; set; }
    public decimal? DesvioPercentual { get; set; }
    public bool AmostraInsuficiente { get; set; }
    public decimal? AcoEstimadoKg { get; set; }
    public decimal? CustoTotal { get; set; }
    public decimal? CustoPorUnidade { get; set; }
    public decimal? Ritmo { get; set; }
    public int? DiasParaConcluir { get; set; }
}

public class CriarFrenteDto
{
    [Required] public Guid ObraId { get; set; }
    [Required] public string Nome { get; set; } = string.Empty;
    [Required] public Guid EtapaId { get; set; }
    public Guid? EquipeId { get; set; }
    public string Unidade { get; set; } = "pç";
    public decimal QuantidadePrevista { get; set; }
    public decimal? TaxaAcoKgPorUnidade { get; set; }
    public decimal? HhOrcadoPorUnidade { get; set; }
    public string? ItemOrcamentoSienge { get; set; }
    public string Cor { get; set; } = "#B07500";
}
