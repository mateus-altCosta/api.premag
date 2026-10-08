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
        CancellationToken cancellationToken = default,
        decimal? latitude = null,
        decimal? longitude = null)
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

        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente) && frente.EquipeId != quem.EquipeId)
            throw new RegraNegocioException("FRENTE_FORA_DA_EQUIPE", "Esta frente não está associada à equipe.", 422);

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

        var lancarPelaFoto = false;
        Guid? producaoParaVincular = null;
        if (tipo == TipoFoto.Avanco && quantidade is > 0 && !frente.Etapa.Indireta)
        {
            var dia = _relogio.HojeSaoPaulo;
            var doDia = (await _db.Producoes.AsNoTracking()
                    .Where(p => p.FrenteId == frente.Id && p.Data == dia)
                    .Select(p => new { p.Id, p.ApontamentoId, p.FotoId, p.Quantidade, p.RegistradoEm })
                    .ToListAsync(cancellationToken))
                .Select(p => new VinculoFotoProducao.LancamentoDia(
                    p.Id, p.ApontamentoId, p.FotoId, p.Quantidade, p.RegistradoEm))
                .ToList();
            var decisao = VinculoFotoProducao.Decidir(doDia, apontamentoId, quantidade.Value);
            lancarPelaFoto = decisao.LancarNova;
            producaoParaVincular = decisao.ProducaoIdParaVincular;
            if (lancarPelaFoto)
            {
                ProducaoService.GarantirTetoPrevisao(
                    frente.QuantidadePrevista,
                    frente.QuantidadeConcluida + quantidade.Value,
                    quem);
            }
        }

        var config = await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Configuracao();
        var (largura, altura) = JpegInfo.Dimensoes(jpeg);
        var hash = Convert.ToHexString(SHA256.HashData(jpeg));
        var id = GeradorId.Novo();
        var chave = $"{_db.TenantId:N}/{_relogio.HojeSaoPaulo:yyyy/MM}/{id:N}.jpg";

        await _storage.GravarAsync(chave, jpeg, cancellationToken);

        var (lat, lng) = CoordenadaGps.Normalizar(latitude, longitude);
        var foto = new Foto
        {
            Id = id,
            TenantId = _db.TenantId,
            ClienteUuid = clienteUuid,
            FrenteId = frente.Id,
            ColaboradorId = colaborador?.Id,
            ApontamentoId = apontamentoId,
            Tipo = tipo,
            Quantidade = quantidade is > 0 ? decimal.Round(quantidade.Value, 3, MidpointRounding.AwayFromZero) : null,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
            ObjectKey = chave,
            ThumbKey = chave,
            Bytes = jpeg.Length,
            Largura = largura,
            Altura = altura,
            HashSha256 = hash,
            CapturadaEm = _relogio.UtcAgora,
            Latitude = lat,
            Longitude = lng,
            EnviadaPorId = quem.Id,
            ExpiraEm = _relogio.HojeSaoPaulo.AddMonths(config.RetencaoFotosMeses)
        };
        _db.Fotos.Add(foto);
        await _db.SaveChangesAsync(cancellationToken);

        // RN-11: foto de avanço só lança se ainda não houver quantidade no dia.
        if (lancarPelaFoto)
        {
            await _producao.RegistrarAsync(new RegistrarProducaoDto
            {
                ClienteUuid = clienteUuid,
                FrenteId = frente.Id,
                Quantidade = quantidade!.Value,
                ApontamentoId = apontamentoId
            }, quem, cancellationToken);

            var prod = await _db.Producoes.FirstOrDefaultAsync(p => p.ClienteUuid == clienteUuid, cancellationToken);
            if (prod is not null && prod.FotoId is null)
            {
                prod.FotoId = foto.Id;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        else if (producaoParaVincular is Guid pid)
        {
            var prod = await _db.Producoes.FirstOrDefaultAsync(p => p.Id == pid, cancellationToken);
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
        bool thumb = false,
        CancellationToken cancellationToken = default)
    {
        var foto = await _db.Fotos.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (foto is null)
            return null;
        var chave = thumb && !string.IsNullOrWhiteSpace(foto.ThumbKey) ? foto.ThumbKey : foto.ObjectKey;
        var bytes = await _storage.LerAsync(chave, cancellationToken);
        return bytes is null ? null : (bytes, "image/jpeg");
    }

    public async Task ExcluirAsync(Guid id, UsuarioLogado quem, CancellationToken cancellationToken = default)
    {
        if (!Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            throw new RegraNegocioException("SEM_PERMISSAO", "Só a gerência retira foto do diário.", 403);

        var foto = await _db.Fotos.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new RegraNegocioException("FOTO_NAO_ENCONTRADA", "Foto não encontrada.", 404);

        try
        {
            if (!string.IsNullOrWhiteSpace(foto.ObjectKey))
                await _storage.RemoverAsync(foto.ObjectKey, cancellationToken);
            if (!string.IsNullOrWhiteSpace(foto.ThumbKey) && foto.ThumbKey != foto.ObjectKey)
                await _storage.RemoverAsync(foto.ThumbKey, cancellationToken);
        }
        catch
        {
            /* exclusão lógica segue mesmo se o arquivo já não existir */
        }

        foto.Excluido = true;
        _db.AuditLogs.Add(Auditoria.Novo(
            _db.TenantId, "Foto", foto.Id, "excluir-foto", quem, _relogio.UtcAgora));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ExpurgarExpiradasAsync(CancellationToken cancellationToken = default)
    {
        var hoje = _relogio.HojeSaoPaulo;
        var fotos = await _db.Fotos
            .Where(f => f.ExpiraEm != null && f.ExpiraEm < hoje)
            .ToListAsync(cancellationToken);
        foreach (var f in fotos)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(f.ObjectKey))
                    await _storage.RemoverAsync(f.ObjectKey, cancellationToken);
                if (!string.IsNullOrWhiteSpace(f.ThumbKey) && f.ThumbKey != f.ObjectKey)
                    await _storage.RemoverAsync(f.ThumbKey, cancellationToken);
            }
            catch
            {
                /* segue para marcar excluído mesmo se o arquivo já não existir */
            }
            f.Excluido = true;
        }
        if (fotos.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
        return fotos.Count;
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
        Latitude = f.Latitude,
        Longitude = f.Longitude,
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
