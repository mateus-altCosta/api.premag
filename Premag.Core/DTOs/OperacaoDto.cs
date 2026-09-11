using Premag.Core.Enums;

namespace Premag.Core.DTOs;

public class FotoDto
{
    public Guid Id { get; set; }
    public Guid ClienteUuid { get; set; }
    public Guid FrenteId { get; set; }
    public string FrenteNome { get; set; } = string.Empty;
    public Guid? ColaboradorId { get; set; }
    public string? ColaboradorNome { get; set; }
    public TipoFoto Tipo { get; set; }
    public decimal? Quantidade { get; set; }
    public string? Observacao { get; set; }
    public DateTimeOffset CapturadaEm { get; set; }
    public string Url { get; set; } = string.Empty;
    public string UrlThumb { get; set; } = string.Empty;
}

public class DiarioDto
{
    public DateOnly Data { get; set; }
    public string Escopo { get; set; } = string.Empty;
    public IReadOnlyList<DiarioFrenteDto> Frentes { get; set; } = [];
    public IReadOnlyList<DiarioFuncaoDto> Funcoes { get; set; } = [];
    public IReadOnlyList<DiarioSituacaoDto> Situacoes { get; set; } = [];
    public IReadOnlyList<FotoDto> Fotos { get; set; } = [];
    public int FotosAvanco { get; set; }
}

public class DiarioFrenteDto
{
    public Guid FrenteId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string ObraNome { get; set; } = string.Empty;
    public string Cor { get; set; } = string.Empty;
    public string Unidade { get; set; } = "un";
    public decimal Horas { get; set; }
    public int Pessoas { get; set; }
    public decimal Quantidade { get; set; }
    public bool SemQuantidade { get; set; }
}

public class DiarioFuncaoDto
{
    public string Funcao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class DiarioSituacaoDto
{
    public string Situacao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class OcorrenciaDto
{
    public Guid Id { get; set; }
    public TipoOcorrencia Tipo { get; set; }
    public SeveridadeOcorrencia Severidade { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Detalhe { get; set; } = string.Empty;
    public Guid? ColaboradorId { get; set; }
    public string? ColaboradorNome { get; set; }
    public Guid? FrenteId { get; set; }
    public string? FrenteNome { get; set; }
    public Guid? EquipeId { get; set; }
    public DateOnly Data { get; set; }
    public DateTimeOffset DetectadaEm { get; set; }
    public int? MinutosDecorridos { get; set; }
    public DateTimeOffset? ReconhecidaEm { get; set; }
    public string? Justificativa { get; set; }
}

public class ReconhecerOcorrenciaDto
{
    public string? Justificativa { get; set; }
}

public class RelatorioLinhaDto
{
    public Guid FrenteId { get; set; }
    public string FrenteNome { get; set; } = string.Empty;
    public string Unidade { get; set; } = "un";
    public decimal Hh { get; set; }
    public decimal Quantidade { get; set; }
    public decimal? HhPorUnidade { get; set; }
    public decimal? DesvioPercentual { get; set; }
    public decimal QuantidadePrevista { get; set; }
    public decimal QuantidadeConcluida { get; set; }
    public decimal PercentualAvanco { get; set; }
    public decimal? AcoEstimadoKg { get; set; }
    public decimal? CustoPorUnidade { get; set; }
}

public class RelatorioDto
{
    public string Tipo { get; set; } = "produtividade";
    public string Periodo { get; set; } = "hoje";
    public IReadOnlyList<RelatorioLinhaDto> Linhas { get; set; } = [];
    public string Nota { get; set; } = string.Empty;
}

public class ImportacaoResultadoDto
{
    public int Lidos { get; set; }
    public int Gravados { get; set; }
    public int Ignorados { get; set; }
    public IReadOnlyList<string> Avisos { get; set; } = [];
}

public class FechamentoDiaDto
{
    public DateOnly Data { get; set; }
    public Guid? EquipeId { get; set; }
    public string Escopo { get; set; } = string.Empty;
    public bool Fechado { get; set; }
    public bool FechadoPorCalendario { get; set; }
    public DateTimeOffset? FechadoEm { get; set; }
    public string? FechadoPorNome { get; set; }
    public DateTimeOffset? ReabertoEm { get; set; }
    public string? MotivoReabertura { get; set; }
    public int DiasFechamento { get; set; }
    public int ApontamentosAbertos { get; set; }
    public int PresentesSemServico { get; set; }
    public int Fotos { get; set; }
    public decimal HorasApontadas { get; set; }
}

public class FecharDiaDto
{
    public DateOnly? Data { get; set; }
    public Guid? EquipeId { get; set; }
}

public class ReabrirDiaDto
{
    public DateOnly? Data { get; set; }
    public Guid? EquipeId { get; set; }
    public string MotivoReabertura { get; set; } = string.Empty;
}

public class InscreverPushDto
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public class PushChaveDto
{
    public string ChavePublica { get; set; } = string.Empty;
}
