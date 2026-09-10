namespace Premag.Core.Entities;

public class InscricaoPush : EntidadeTenant
{
    public Guid UsuarioId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset? UltimoEnvioEm { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
