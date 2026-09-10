using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Premag.Application.Common;
using Premag.Application.Interfaces.Services;
using Premag.Core;
using Premag.Core.DTOs;
using Premag.Core.Entities;
using Premag.Core.Exceptions;
using Premag.Core.Interfaces;
using Premag.Infrastructure.Data;

namespace Premag.Application.Services;

public class SincronizacaoService : ISincronizacaoService
{
    private static readonly JsonSerializerOptions JsonBytes = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationDbContext _db;
    private readonly IApontamentoService _apontamentos;
    private readonly IProducaoService _producoes;
    private readonly IRelogio _relogio;

    public SincronizacaoService(
        ApplicationDbContext db,
        IApontamentoService apontamentos,
        IProducaoService producoes,
        IRelogio relogio)
    {
        _db = db;
        _apontamentos = apontamentos;
        _producoes = producoes;
        _relogio = relogio;
    }

    public async Task<LoteResultadoDto> ProcessarLoteAsync(
        EnviarLoteDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.DispositivoId))
            throw new RegraNegocioException("DISPOSITIVO_OBRIGATORIO", "Informe o identificador do aparelho.", 422);

        var itens = dto.Itens ?? [];
        if (itens.Count == 0)
            throw new RegraNegocioException("LOTE_VAZIO", "O lote não tem itens para sincronizar.", 422);

        var bytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(dto, JsonBytes));
        var resultados = new List<ResultadoItemLoteDto>(itens.Count);

        for (var i = 0; i < itens.Count; i++)
            resultados.Add(await ProcessarItemAsync(itens[i], i, quem, cancellationToken));

        var aceitos = resultados.Count(r => r.Aceito);
        var lote = new LoteSincronizacao
        {
            TenantId = _db.TenantId,
            DispositivoId = dto.DispositivoId.Trim(),
            UsuarioId = quem.Id,
            RecebidoEm = _relogio.UtcAgora,
            ItensRecebidos = itens.Count,
            ItensAceitos = aceitos,
            ItensRejeitados = itens.Count - aceitos,
            PayloadBytes = bytes
        };
        _db.LotesSincronizacao.Add(lote);
        await _db.SaveChangesAsync(cancellationToken);

        return new LoteResultadoDto
        {
            Id = lote.Id,
            DispositivoId = lote.DispositivoId,
            RecebidoEm = lote.RecebidoEm,
            ItensRecebidos = lote.ItensRecebidos,
            ItensAceitos = lote.ItensAceitos,
            ItensRejeitados = lote.ItensRejeitados,
            PayloadBytes = lote.PayloadBytes,
            Resultados = resultados
        };
    }

    private async Task<ResultadoItemLoteDto> ProcessarItemAsync(
        ItemLoteDto item,
        int indice,
        UsuarioLogado quem,
        CancellationToken cancellationToken)
    {
        var tipo = item.Tipo ?? string.Empty;
        var resultado = new ResultadoItemLoteDto
        {
            Indice = indice,
            Tipo = tipo,
            ClienteUuid = ClienteDoItem(item)
        };

        if (!TiposLote.EhValido(tipo))
        {
            resultado.Codigo = "TIPO_INVALIDO";
            resultado.Detalhe = "Tipo de item não reconhecido. Use iniciar, encerrar ou producao.";
            return resultado;
        }

        try
        {
            var normalizado = TiposLote.Normalizar(tipo);
            resultado.Tipo = normalizado;
            IReadOnlyList<string> avisos = [];

            if (normalizado == TiposLote.Iniciar)
            {
                if (item.Iniciar is null)
                    throw new RegraNegocioException("ITEM_INVALIDO", "Falta o corpo do início de serviço.", 422);
                await _apontamentos.IniciarAsync(item.Iniciar, quem, cancellationToken, origemLote: true);
            }
            else if (normalizado == TiposLote.Encerrar)
            {
                if (item.Encerrar is null)
                    throw new RegraNegocioException("ITEM_INVALIDO", "Falta o corpo do encerramento.", 422);
                var id = await ResolverApontamentoIdAsync(item, cancellationToken)
                    ?? throw new RegraNegocioException("APONTAMENTO_NAO_ENCONTRADO", "Apontamento não encontrado para encerrar.", 404);
                var encerrado = await _apontamentos.EncerrarAsync(id, item.Encerrar, quem, cancellationToken, origemLote: true);
                avisos = encerrado.Avisos;
            }
            else
            {
                if (item.Producao is null)
                    throw new RegraNegocioException("ITEM_INVALIDO", "Falta o corpo da produção.", 422);
                var prod = await _producoes.RegistrarAsync(item.Producao, quem, cancellationToken);
                avisos = prod.Avisos;
            }

            resultado.Aceito = true;
            resultado.Avisos = avisos;
            return resultado;
        }
        catch (RegraNegocioException ex)
        {
            resultado.Aceito = false;
            resultado.Codigo = ex.Codigo;
            resultado.Detalhe = ex.Message;
            return resultado;
        }
        catch (Exception)
        {
            resultado.Aceito = false;
            resultado.Codigo = "ERRO_INTERNO";
            resultado.Detalhe = "Não foi possível gravar este item.";
            return resultado;
        }
    }

    private async Task<Guid?> ResolverApontamentoIdAsync(ItemLoteDto item, CancellationToken cancellationToken)
    {
        if (item.ApontamentoId is Guid id && id != Guid.Empty)
            return id;
        if (item.ApontamentoClienteUuid is Guid cliente && cliente != Guid.Empty)
        {
            var encontrado = await _db.Apontamentos
                .Where(a => a.ClienteUuid == cliente)
                .Select(a => (Guid?)a.Id)
                .FirstOrDefaultAsync(cancellationToken);
            return encontrado;
        }
        return null;
    }

    private static Guid? ClienteDoItem(ItemLoteDto item) =>
        item.Iniciar?.ClienteUuid
        ?? item.Encerrar?.ClienteUuid
        ?? item.Producao?.ClienteUuid;
}
