using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Premag.Application.Common;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Core.Entities;
using Premag.Core.Enums;
using Premag.Core.Exceptions;
using Premag.Core.Interfaces;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

public class EquipeService : IEquipeService
{
    private readonly ApplicationDbContext _db;
    private readonly IRelogio _relogio;

    public EquipeService(ApplicationDbContext db, IRelogio relogio)
    {
        _db = db;
        _relogio = relogio;
    }

    public async Task<IReadOnlyList<EquipeDto>> ListarAsync(
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Equipes.AsNoTracking().Where(e => e.Ativa);

        // RN-10: Encarregado só vê a própria equipe.
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
        {
            if (quem.EquipeId is null)
                return [];
            query = query.Where(e => e.Id == quem.EquipeId);
        }

        var equipes = await query.OrderBy(e => e.Nome).ToListAsync(cancellationToken);
        var ids = equipes.Select(e => e.Id).ToList();
        var presentes = await _db.Colaboradores.AsNoTracking()
            .Where(c => ids.Contains(c.EquipeId) && c.Ativo)
            .GroupBy(c => c.EquipeId)
            .Select(g => new { EquipeId = g.Key, Qtd = g.Count() })
            .ToListAsync(cancellationToken);
        var mapa = presentes.ToDictionary(x => x.EquipeId, x => x.Qtd);

        return equipes.Select(e => new EquipeDto
        {
            Id = e.Id,
            Nome = e.Nome,
            Cor = e.Cor,
            Ativa = e.Ativa,
            EncarregadoId = e.EncarregadoId,
            Presentes = mapa.GetValueOrDefault(e.Id)
        }).ToList();
    }

    public async Task<IReadOnlyList<ColaboradorDto>> ListarColaboradoresAsync(
        Guid equipeId,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        GarantirEscopoEquipe(quem, equipeId);

        var equipe = await _db.Equipes.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == equipeId, cancellationToken)
            ?? throw new RegraNegocioException("EQUIPE_NAO_ENCONTRADA", "Equipe não encontrada.", 404);

        var lista = await _db.Colaboradores.AsNoTracking()
            .Where(c => c.EquipeId == equipeId)
            .OrderBy(c => c.Nome)
            .ToListAsync(cancellationToken);

        var verCusto = Permissoes.Tem(quem.Perfil, Permissoes.Gerente);
        return lista.Select(c => Mapear(c, equipe.Nome, verCusto)).ToList();
    }

    public async Task<ColaboradorDto> CriarColaboradorAsync(
        CriarColaboradorDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só Gerente ou acima cadastra colaborador.", 403);

        var matricula = dto.Matricula.Trim();
        var nome = dto.Nome.Trim();
        if (matricula.Length == 0 || nome.Length == 0)
            throw new RegraNegocioException("DADOS_INVALIDOS", "Matrícula e nome são obrigatórios.");

        var equipe = await _db.Equipes.FirstOrDefaultAsync(e => e.Id == dto.EquipeId && e.Ativa, cancellationToken)
            ?? throw new RegraNegocioException("EQUIPE_NAO_ENCONTRADA", "Equipe não encontrada.", 404);

        var existe = await _db.Colaboradores.AnyAsync(c => c.Matricula == matricula, cancellationToken);
        if (existe)
            throw new RegraNegocioException("MATRICULA_DUPLICADA", "Já existe colaborador com essa matrícula.", 409);

        var agora = _relogio.UtcAgora;
        var colaborador = new Colaborador
        {
            TenantId = _db.TenantId,
            Matricula = matricula,
            Nome = nome,
            Funcao = string.IsNullOrWhiteSpace(dto.Funcao) ? "—" : dto.Funcao.Trim(),
            EquipeId = equipe.Id,
            // RN-14: cadastro manual não entra em relatório de custo.
            OrigemCadastro = OrigemCadastro.Manual,
            CustoHora = null,
            Ativo = true,
            CriadoEm = agora,
            AlteradoEm = agora
        };
        _db.Colaboradores.Add(colaborador);
        await _db.SaveChangesAsync(cancellationToken);

        var verCusto = Permissoes.Tem(quem.Perfil, Permissoes.Gerente);
        return Mapear(colaborador, equipe.Nome, verCusto);
    }

    public async Task<ColaboradorDto> TransferirAsync(
        Guid colaboradorId,
        TransferirColaboradorDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só Gerente ou acima transfere colaborador.", 403);

        var colaborador = await _db.Colaboradores
            .Include(c => c.Equipe)
            .FirstOrDefaultAsync(c => c.Id == colaboradorId, cancellationToken)
            ?? throw new RegraNegocioException("COLABORADOR_NAO_ENCONTRADO", "Colaborador não encontrado.", 404);

        var destino = await _db.Equipes.FirstOrDefaultAsync(e => e.Id == dto.EquipeId && e.Ativa, cancellationToken)
            ?? throw new RegraNegocioException("EQUIPE_NAO_ENCONTRADA", "Equipe de destino não encontrada.", 404);

        if (colaborador.EquipeId == destino.Id)
            return Mapear(colaborador, destino.Nome, incluirCusto: true);

        var antes = JsonSerializer.Serialize(new { equipeId = colaborador.EquipeId, equipe = colaborador.Equipe.Nome });
        colaborador.EquipeId = destino.Id;
        colaborador.AlteradoEm = _relogio.UtcAgora;

        // RN-16: transferência gera audit log.
        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = _db.TenantId,
            Entidade = "Colaborador",
            EntidadeId = colaborador.Id,
            Acao = "transferir-equipe",
            Antes = antes,
            Depois = JsonSerializer.Serialize(new { equipeId = destino.Id, equipe = destino.Nome }),
            UsuarioId = quem.Id,
            Em = _relogio.UtcAgora,
            Ip = quem.Ip
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Mapear(colaborador, destino.Nome, incluirCusto: true);
    }

    public async Task ExcluirColaboradorAsync(
        Guid colaboradorId,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só Gerente ou acima exclui colaborador.", 403);

        var colaborador = await _db.Colaboradores.FirstOrDefaultAsync(c => c.Id == colaboradorId, cancellationToken)
            ?? throw new RegraNegocioException("COLABORADOR_NAO_ENCONTRADO", "Colaborador não encontrado.", 404);

        colaborador.Ativo = false;
        colaborador.AlteradoEm = _relogio.UtcAgora;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void GarantirEscopoEquipe(UsuarioLogado quem, Guid equipeId)
    {
        if (Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            return;
        if (quem.EquipeId != equipeId)
            throw new RegraNegocioException("RN-10", "Encarregado só acessa colaboradores da própria equipe.", 403);
    }

    private static ColaboradorDto Mapear(Colaborador c, string equipeNome, bool incluirCusto) => new()
    {
        Id = c.Id,
        Matricula = c.Matricula,
        Nome = c.Nome,
        Funcao = c.Funcao,
        EquipeId = c.EquipeId,
        EquipeNome = equipeNome,
        Ativo = c.Ativo,
        OrigemCadastro = c.OrigemCadastro,
        // RN-13: CustoHora ausente do DTO de Encarregado.
        CustoHora = incluirCusto ? c.CustoHora : null
    };
}
