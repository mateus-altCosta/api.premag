namespace Premag.Core.Entities;

public class Equipe : EntidadeTenant
{
    public string Nome { get; set; } = string.Empty;
    public Guid? EncarregadoId { get; set; }
    public string Cor { get; set; } = "#4A5560";
    public bool Ativa { get; set; } = true;

    public Usuario? Encarregado { get; set; }
    public ICollection<Colaborador> Colaboradores { get; set; } = [];
}
