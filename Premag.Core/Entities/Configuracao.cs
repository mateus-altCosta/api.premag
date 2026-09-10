namespace Premag.Core.Entities;

public class Configuracao : EntidadeTenant
{
    public int JornadaPadraoMinutos { get; set; } = 528;
    public TimeOnly JornadaInicio { get; set; } = new(7, 0);
    public TimeOnly JornadaFim { get; set; } = new(16, 48);
    public TimeOnly IntervaloInicio { get; set; } = new(12, 0);
    public TimeOnly IntervaloFim { get; set; } = new(13, 0);
    public int MinutosOciosidadeEscalonamento { get; set; } = 60;
    public int MinutosSemServico { get; set; } = 15;
    public int MinutosServicoAbertoDemais { get; set; } = 300;
    public int DiasFechamento { get; set; } = 3;
    public int RetencaoFotosMeses { get; set; } = 24;
}
