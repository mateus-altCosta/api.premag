using System.Text.Json;
using Premag.Application.Common;
using Premag.Core.Entities;

namespace Premag.Application.Services;

internal static class Auditoria
{
    public static AuditLog Novo(
        Guid tenantId,
        string entidade,
        Guid entidadeId,
        string acao,
        UsuarioLogado quem,
        DateTimeOffset em,
        string? antes = null,
        string? depois = null) =>
        new()
        {
            TenantId = tenantId,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Acao = acao.Length <= 20 ? acao : acao[..20],
            Antes = Jsonb(antes),
            Depois = Jsonb(depois),
            UsuarioId = quem.Id,
            Em = em,
            Ip = quem.Ip
        };

    /// <summary>audit_logs.antes/depois são jsonb: texto livre vira string JSON.</summary>
    private static string? Jsonb(string? valor)
    {
        if (valor is null) return null;
        var t = valor.TrimStart();
        if (t.Length > 0 && (t[0] == '{' || t[0] == '['))
            return valor;
        return JsonSerializer.Serialize(valor);
    }
}
