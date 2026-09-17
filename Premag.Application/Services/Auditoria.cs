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
            Antes = antes,
            Depois = depois,
            UsuarioId = quem.Id,
            Em = em,
            Ip = quem.Ip
        };
}
