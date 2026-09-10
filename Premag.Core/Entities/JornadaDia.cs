using Premag.Core.Enums;

namespace Premag.Core.Entities;

public class JornadaDia : EntidadeTenant
{
    public Guid ColaboradorId { get; set; }
    public DateOnly Data { get; set; }
    public TimeOnly? Entrada { get; set; }
    public TimeOnly? Saida { get; set; }
    public int IntervaloMinutos { get; set; } = 60;
    public int MinutosApurados { get; set; }
    public SituacaoJornada Situacao { get; set; } = SituacaoJornada.Presente;
    public OrigemJornada Origem { get; set; } = OrigemJornada.Afd;
    public DateTimeOffset ImportadoEm { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
}
