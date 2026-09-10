using Premag.Core.Enums;

namespace Premag.Core.Entities;

public class Colaborador : EntidadeTenant
{
    public string Matricula { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Funcao { get; set; } = string.Empty;
    public Guid EquipeId { get; set; }
    public decimal? CustoHora { get; set; }
    public DateOnly? DataAdmissao { get; set; }
    public bool Ativo { get; set; } = true;
    public OrigemCadastro OrigemCadastro { get; set; } = OrigemCadastro.Folha;
    public string? CodigoExterno { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset AlteradoEm { get; set; }

    public Equipe Equipe { get; set; } = null!;
}
