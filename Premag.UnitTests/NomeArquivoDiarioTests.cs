using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class NomeArquivoDiarioTests
{
    [Fact]
    public void Pdf_UsaEquipeEData()
    {
        NomeArquivoDiario.Pdf("ARMAÇÃO", new DateOnly(2026, 10, 8))
            .Should().Be("Diário ARMAÇÃO 08-10-2026.PDF");
    }

    [Fact]
    public void Pdf_ConsolidadoViraTodasAsEquipes()
    {
        NomeArquivoDiario.Pdf("consolidado de todas as equipes", new DateOnly(2026, 10, 8))
            .Should().Be("Diário todas as equipes 08-10-2026.PDF");
    }
}
