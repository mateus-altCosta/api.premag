namespace Premag.Core.Interfaces;

public interface IRelogio
{
    DateTimeOffset UtcAgora { get; }
    DateTimeOffset AgoraSaoPaulo { get; }
    DateOnly HojeSaoPaulo { get; }
    TimeOnly HoraSaoPaulo { get; }
}
