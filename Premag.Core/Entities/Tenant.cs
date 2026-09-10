namespace Premag.Core.Entities;

public class Tenant
{
    public Guid Id { get; set; } = GeradorId.Novo();
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }
}
