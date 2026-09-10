using Microsoft.AspNetCore.Mvc;
using Premag.Application.Common;
using Premag.Core.Exceptions;

namespace Premag.API.Controllers;

internal static class CadastroHttp
{
    public static UsuarioLogado? Quem(ControllerBase controller)
    {
        var ip = controller.HttpContext.Connection.RemoteIpAddress?.ToString();
        return UsuarioLogadoFactory.From(controller.User, ip);
    }

    public static IActionResult Falha(Exception ex, ILogger logger, string contexto)
    {
        if (ex is RegraNegocioException regra)
        {
            var slug = regra.Codigo.ToLowerInvariant().Replace('_', '-');
            return new ObjectResult(new
            {
                type = $"https://premag.app/errors/{slug}",
                title = regra.Codigo,
                status = regra.Status,
                codigo = regra.Codigo,
                detail = regra.Message,
                message = regra.Message,
                extensions = regra.Extensoes
            })
            {
                StatusCode = regra.Status,
                ContentTypes = { "application/problem+json" }
            };
        }

        logger.LogError(ex, "Erro em {Contexto}", contexto);
        return new ObjectResult(new { message = "Erro interno do servidor" }) { StatusCode = 500 };
    }
}
