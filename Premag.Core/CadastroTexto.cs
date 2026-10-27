using Premag.Core.Exceptions;

namespace Premag.Core;

public static class CadastroTexto
{
    public static string NomeMotivo(string? valor)
    {
        var t = (valor ?? "").Trim();
        if (t.Length == 0)
            throw new RegraNegocioException("DADOS_INVALIDOS", "Nome do motivo é obrigatório.");
        if (t.Length > 80)
            throw new RegraNegocioException("DADOS_INVALIDOS", "Nome do motivo pode ter no máximo 80 caracteres.");
        return t;
    }

    public static string? CodigoExterno(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;
        var t = valor.Trim();
        if (t.Length > 40)
            throw new RegraNegocioException("CODIGO_INVALIDO", "Código externo pode ter no máximo 40 caracteres.");
        return t;
    }

    public static string SenhaNova(string? valor)
    {
        var t = (valor ?? "").Trim();
        if (t.Length < 8)
            throw new RegraNegocioException("SENHA_FRACA", "A senha nova precisa ter pelo menos 8 caracteres.", 422);
        return t;
    }
}
