using FluentAssertions;
using Premag.Core;
using Premag.Core.Enums;

namespace Premag.UnitTests;

public class CalculoAlertasTests
{
    private static readonly DateOnly Dia = new(2026, 8, 27);
    private static readonly TimeOnly Agora = new(9, 30);
    private static readonly Guid C1 = Guid.Parse("11111111-1111-7111-8111-111111111201");
    private static readonly Guid E1 = Guid.Parse("11111111-1111-7111-8111-111111111202");

    [Fact]
    public void RN07_Ociosidade60Min_EscalaParaGerencia()
    {
        var colab = new ColaboradorAlerta(C1, E1, true, SituacaoJornada.Presente, new TimeOnly(7, 0), null, 0);
        var lista = CalculoAlertas.Detectar(
            Dia, Agora, new TimeOnly(12, 0), new TimeOnly(13, 0),
            60, 15, 300, [colab], []);
        lista.Should().Contain(a => a.Tipo == TipoOcorrencia.OciosidadeEscalonada
                                    && a.Severidade == SeveridadeOcorrencia.GerenciaDiretoria);
    }

    [Fact]
    public void FaltaOuFerias_NaoGeraAlerta()
    {
        var colab = new ColaboradorAlerta(C1, E1, true, SituacaoJornada.Ferias, new TimeOnly(7, 0), null, 0);
        CalculoAlertas.Detectar(Dia, Agora, new TimeOnly(12, 0), new TimeOnly(13, 0), 60, 15, 300, [colab], [])
            .Should().BeEmpty();
    }
}
