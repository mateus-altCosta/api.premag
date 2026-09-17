using Microsoft.EntityFrameworkCore;
using Premag.Application.Common;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Core.Exceptions;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

public class HistoricoService : IHistoricoService
{
    private readonly ApplicationDbContext _db;

    public HistoricoService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AuditLogDto>> ListarAsync(
        UsuarioLogado quem,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só a gerência consulta o histórico.", 403);

        var n = Math.Clamp(take, 1, 200);
        var lista = await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.Em)
            .Take(n)
            .ToListAsync(cancellationToken);
        var ids = lista.Select(a => a.UsuarioId).Distinct().ToList();
        var nomes = await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.NomeExibicao, cancellationToken);

        return lista.Select(a => new AuditLogDto
        {
            Id = a.Id,
            Entidade = a.Entidade,
            EntidadeId = a.EntidadeId,
            Acao = a.Acao,
            Antes = a.Antes,
            Depois = a.Depois,
            UsuarioId = a.UsuarioId,
            UsuarioNome = nomes.GetValueOrDefault(a.UsuarioId) ?? "—",
            Em = a.Em
        }).ToList();
    }
}
