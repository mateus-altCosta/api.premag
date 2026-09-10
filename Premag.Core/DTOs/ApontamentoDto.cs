using System.ComponentModel.DataAnnotations;
using Premag.Core.Enums;

namespace Premag.Core.DTOs;

public class IniciarApontamentoDto
{
    [Required] public Guid ClienteUuid { get; set; }
    [Required] public Guid ColaboradorId { get; set; }
    [Required] public Guid FrenteId { get; set; }
    public DateOnly? Data { get; set; }
    [Required] public TimeOnly HoraInicio { get; set; }
    public Guid? MotivoParadaId { get; set; }
    public string? Observacao { get; set; }
    public string? DispositivoId { get; set; }
}

public class EncerrarApontamentoDto
{
    [Required] public Guid ClienteUuid { get; set; }
    [Required] public TimeOnly HoraFim { get; set; }
    public LancarProducaoDto? Producao { get; set; }
}

public class LancarProducaoDto
{
    [Required] public Guid ClienteUuid { get; set; }
    public decimal Quantidade { get; set; }
    public Guid? FotoClienteUuid { get; set; }
}

public class EncerrarResultadoDto
{
    public ApontamentoDto Apontamento { get; set; } = new();
    public IReadOnlyList<string> Avisos { get; set; } = [];
    public decimal? QuantidadeJaLancadaHoje { get; set; }
}

public class ApontamentoDto
{
    public Guid Id { get; set; }
    public Guid ClienteUuid { get; set; }
    public Guid ColaboradorId { get; set; }
    public Guid FrenteId { get; set; }
    public string FrenteNome { get; set; } = string.Empty;
    public string FrenteCor { get; set; } = string.Empty;
    public string Unidade { get; set; } = "un";
    public Guid ObraId { get; set; }
    public string ObraNome { get; set; } = string.Empty;
    public bool FrenteIndireta { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly? HoraFim { get; set; }
    public int? MinutosEfetivos { get; set; }
    public Guid? MotivoParadaId { get; set; }
    public string? MotivoParadaNome { get; set; }
    public string? Observacao { get; set; }
    public bool JornadaNaoVerificada { get; set; }
    public bool Aberto => HoraFim is null;
}

public class TurnoDto
{
    public DateOnly Data { get; set; }
    public Guid EquipeId { get; set; }
    public string EquipeNome { get; set; } = string.Empty;
    public string EquipeCor { get; set; } = string.Empty;
    public string JornadaInicio { get; set; } = "07:00";
    public string JornadaFim { get; set; } = "16:48";
    public string IntervaloInicio { get; set; } = "12:00";
    public string IntervaloFim { get; set; } = "13:00";
    public int JornadaPadraoMinutos { get; set; }
    public IReadOnlyList<TurnoColaboradorDto> Colaboradores { get; set; } = [];
    public IReadOnlyList<ObraDto> Obras { get; set; } = [];
    public IReadOnlyList<FrenteDto> Frentes { get; set; } = [];
    public IReadOnlyList<MotivoParadaDto> MotivosParada { get; set; } = [];
}

public class TurnoColaboradorDto
{
    public Guid Id { get; set; }
    public string Matricula { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Funcao { get; set; } = string.Empty;
    public bool Ativo { get; set; }
    public SituacaoJornada Situacao { get; set; }
    public int MinutosApurados { get; set; }
    public bool JornadaNaoVerificada { get; set; }
    public TimeOnly? JornadaEntrada { get; set; }
    public TimeOnly? JornadaSaida { get; set; }
    public int MinutosTrabalhados { get; set; }
    public int MinutosParados { get; set; }
    public int MinutosNaoApropriados { get; set; }
    public int MinutosOciosos { get; set; }
    /// <summary>RN-13: só Gerente ou acima.</summary>
    public decimal? CustoHora { get; set; }
    public ApontamentoDto? Aberto { get; set; }
    public IReadOnlyList<ApontamentoDto> Apontamentos { get; set; } = [];
}
