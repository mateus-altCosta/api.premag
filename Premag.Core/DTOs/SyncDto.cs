using System.ComponentModel.DataAnnotations;

namespace Premag.Core.DTOs;

public class EnviarLoteDto
{
    [Required] public string DispositivoId { get; set; } = string.Empty;
    [Required] public IReadOnlyList<ItemLoteDto> Itens { get; set; } = [];
}

public class ItemLoteDto
{
    [Required] public string Tipo { get; set; } = string.Empty;
    public Guid? ApontamentoId { get; set; }
    public Guid? ApontamentoClienteUuid { get; set; }
    public IniciarApontamentoDto? Iniciar { get; set; }
    public EncerrarApontamentoDto? Encerrar { get; set; }
    public RegistrarProducaoDto? Producao { get; set; }
}

public class ResultadoItemLoteDto
{
    public int Indice { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public Guid? ClienteUuid { get; set; }
    public bool Aceito { get; set; }
    public string? Codigo { get; set; }
    public string? Detalhe { get; set; }
    public IReadOnlyList<string> Avisos { get; set; } = [];
}

public class LoteResultadoDto
{
    public Guid Id { get; set; }
    public string DispositivoId { get; set; } = string.Empty;
    public DateTimeOffset RecebidoEm { get; set; }
    public int ItensRecebidos { get; set; }
    public int ItensAceitos { get; set; }
    public int ItensRejeitados { get; set; }
    public int PayloadBytes { get; set; }
    public IReadOnlyList<ResultadoItemLoteDto> Resultados { get; set; } = [];
}
