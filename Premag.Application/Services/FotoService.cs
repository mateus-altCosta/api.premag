using System.Security.Cryptography;
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

public class FotoService : IFotoService
{
    private readonly ApplicationDbContext _db;
    private readonly IArquivoStorage _storage;
    private readonly IProducaoService _producao;
    private readonly IRelogio _relogio;
    private readonly IFechamentoService _fechamento;

    public FotoService(
        ApplicationDbContext db,
        IArquivoStorage storage,
        IProducaoService producao,
        IRelogio relogio,
        IFechamentoService fechamento)
    {
        _db = db;
        _storage = storage;
        _producao = producao;
        _relogio = relogio;
        _fechamento = fechamento;
    }

    public async Task<IReadOnlyList<FotoDto>> ListarAsync(
        DateOnly? data,
        Guid? frenteId,
        Guid? colaboradorId,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var dia = data ?? _relogio.HojeSaoPaulo;
        var (inicio, fim) = JanelaDia.Utc(dia, _relogio.AgoraSaoPaulo.Offset);
        var query = _db.Fotos.AsNoTracking()
            .Include(f => f.Frente)
            .Include(f => f.Colaborador)
            .Where(f => f.CapturadaEm >= inicio && f.CapturadaEm < fim);

        if (frenteId is Guid fid)
            query = query.Where(f => f.FrenteId == fid);
        if (colaboradorId is Guid cid)
            query = query.Where(f => f.ColaboradorId == cid);

        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
        {
            if (quem.EquipeId is null)
                return [];
            var equipe = quem.EquipeId.Value;
            query = query.Where(f =>
                (f.Colaborador != null && f.Colaborador.EquipeId == equipe)
                || f.Frente.EquipeId == equipe);
        }

        var lista = await query.OrderByDescending(f => f.CapturadaEm).ToListAsync(cancellationToken);
        return lista.Select(Mapear).ToList();
    }

    public async Task<FotoDto> RegistrarAsync(
        Guid clienteUuid,
        Guid frenteId,
        Guid? colaboradorId,
        Guid? apontamentoId,
        TipoFoto tipo,
        decimal? quantidade,
        string? observacao,
        byte[] jpeg,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (clienteUuid == Guid.Empty)
            throw new RegraNegocioException("RN-12", "Informe ClienteUuid.", 422);

        // RN-12: repetir o mesmo ClienteUuid devolve a foto já gravada.
        var existente = await _db.Fotos
            .Include(f => f.Frente)
            .Include(f => f.Colaborador)
            .FirstOrDefaultAsync(f => f.ClienteUuid == clienteUuid, cancellationToken);
        if (existente is not null)
            return Mapear(existente);

        if (!JpegInfo.EhJpeg(jpeg))
            throw new RegraNegocioException("ARQUIVO_INVALIDO", "Envie uma imagem JPEG.", 422);
        if (jpeg.Length > JpegInfo.TetoBytes)
            throw new RegraNegocioException("ARQUIVO_GRANDE", "A foto não pode passar de 300 KB.", 422);

        var frente = await _db.Frentes
            .Include(f => f.Etapa)
            .Include(f => f.Equipe)
            .FirstOrDefaultAsync(f => f.Id == frenteId && f.Ativa, cancellationToken)
            ?? throw new RegraNegocioException("FRENTE_NAO_ENCONTRADA", "Frente não encontrada.", 404);

        Colaborador? colaborador = null;
        if (colaboradorId is Guid cid)
        {
            colaborador = await _db.Colaboradores.FirstOrDefaultAsync(c => c.Id == cid, cancellationToken)
                ?? throw new RegraNegocioException("COLABORADOR_NAO_ENCONTRADO", "Colaborador não encontrado.", 404);
            GarantirEscopoEquipe(quem, colaborador.EquipeId);
        }
        else if (frente.EquipeId is Guid eqFrente)
        {
            GarantirEscopoEquipe(quem, eqFrente);
        }
        else if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
        {
            throw new RegraNegocioException("RN-10", "Encarregado só registra foto da própria equipe.", 403);
        }

        await _fechamento.GarantirAbertoAsync(
            _relogio.HojeSaoPaulo,
            colaborador?.EquipeId ?? frente.EquipeId,
            cancellationToken);

        if (tipo == TipoFoto.Avanco && quantidade is > 0 && !frente.Etapa.Indireta)
        {
            ProducaoService.GarantirTetoPrevisao(
                frente.QuantidadePrevista,
                frente.QuantidadeConcluida + quantidade.Value,
                quem);
        }

        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        var (largura, altura) = JpegInfo.Dimensoes(jpeg);
        var hash = Convert.ToHexString(SHA256.HashData(jpeg));
        var id = GeradorId.Novo();
        var chave = $"{_db.TenantId:N}/{_relogio.HojeSaoPaulo:yyyy/MM}/{id:N}.jpg";

        await _storage.GravarAsync(chave, jpeg, cancellationToken);

        var foto = new Foto
        {
            Id = id,
            TenantId = _db.TenantId,
            ClienteUuid = clienteUuid,
            FrenteId = frente.Id,
            ColaboradorId = colaborador?.Id,
            ApontamentoId = apontamentoId,
            Tipo = tipo,
            Quantidade = quantidade is > 0 ? quantidade : null,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
            ObjectKey = chave,
            ThumbKey = chave,
            Bytes = jpeg.Length,
            Largura = largura,
            Altura = altura,
            HashSha256 = hash,
            CapturadaEm = _relogio.UtcAgora,
            EnviadaPorId = quem.Id,
            ExpiraEm = _relogio.HojeSaoPaulo.AddMonths(config.RetencaoFotosMeses)
        };
        _db.Fotos.Add(foto);
        await _db.SaveChangesAsync(cancellationToken);

        // RN-11: foto de avanço com quantidade lança Produção (mesmo ClienteUuid).
        if (tipo == TipoFoto.Avanco && quantidade is > 0 && !frente.Etapa.Indireta)
        {
            await _producao.RegistrarAsync(new RegistrarProducaoDto
            {
                ClienteUuid = clienteUuid,
                FrenteId = frente.Id,
                Quantidade = quantidade.Value,
                ApontamentoId = apontamentoId
            }, quem, cancellationToken);

            var prod = await _db.Producoes.FirstOrDefaultAsync(p => p.ClienteUuid == clienteUuid, cancellationToken);
            if (prod is not null && prod.FotoId is null)
            {
                prod.FotoId = foto.Id;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        foto.Frente = frente;
        foto.Colaborador = colaborador;
        return Mapear(foto);
    }

    public async Task<(byte[] Bytes, string ContentType)?> ObterArquivoAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var foto = await _db.Fotos.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (foto is null)
            return null;
        var bytes = await _storage.LerAsync(foto.ObjectKey, cancellationToken);
        return bytes is null ? null : (bytes, "image/jpeg");
    }

    public static FotoDto Mapear(Foto f) => new()
    {
        Id = f.Id,
        ClienteUuid = f.ClienteUuid,
        FrenteId = f.FrenteId,
        FrenteNome = f.Frente?.Nome ?? string.Empty,
        ColaboradorId = f.ColaboradorId,
        ColaboradorNome = f.Colaborador?.Nome,
        Tipo = f.Tipo,
        Quantidade = f.Quantidade,
        Observacao = f.Observacao,
        CapturadaEm = f.CapturadaEm,
        Url = $"fotos/{f.Id}/arquivo",
        UrlThumb = $"fotos/{f.Id}/thumb"
    };

    private static void GarantirEscopoEquipe(UsuarioLogado quem, Guid equipeId)
    {
        if (Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            return;
        if (quem.EquipeId != equipeId)
            throw new RegraNegocioException("RN-10", "Encarregado só registra foto da própria equipe.", 403);
    }
}
