namespace Premag.Core.Entities;

public class LoteSincronizacao : EntidadeTenant
{
    public string DispositivoId { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public DateTimeOffset RecebidoEm { get; set; }
    public int ItensRecebidos { get; set; }
    public int ItensAceitos { get; set; }
    public int ItensRejeitados { get; set; }
    public int PayloadBytes { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
