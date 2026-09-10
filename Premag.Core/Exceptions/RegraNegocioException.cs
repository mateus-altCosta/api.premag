namespace Premag.Core.Exceptions;

public class RegraNegocioException : Exception
{
    public string Codigo { get; }
    public int Status { get; }
    public IReadOnlyDictionary<string, object?> Extensoes { get; }

    public RegraNegocioException(
        string codigo,
        string mensagem,
        int status = 422,
        IReadOnlyDictionary<string, object?>? extensoes = null)
        : base(mensagem)
    {
        Codigo = codigo;
        Status = status;
        Extensoes = extensoes ?? new Dictionary<string, object?>();
    }
}
