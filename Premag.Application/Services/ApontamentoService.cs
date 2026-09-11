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

public class ApontamentoService : IApontamentoService
{
    private readonly ApplicationDbContext _db;
    private readonly IRelogio _relogio;
    private readonly IFechamentoService _fechamento;

    public ApontamentoService(ApplicationDbContext db, IRelogio relogio, IFechamentoService fechamento)
    {
        _db = db;
        _relogio = relogio;
        _fechamento = fechamento;
    }

    public async Task<TurnoDto> ObterTurnoAsync(
        Guid? equipeId,
        DateOnly? data,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default)
    {
        var config = await ObterConfigAsync(cancellationToken);
        var dia = data ?? _relogio.HojeSaoPaulo;
        var equipe = await ResolverEquipeAsync(equipeId, quem, cancellationToken);

        GarantirEscopoEquipe(quem, equipe.Id);

        var colaboradores = await _db.Colaboradores.AsNoTracking()
            .Where(c => c.EquipeId == equipe.Id)
            .OrderBy(c => c.Nome)
            .ToListAsync(cancellationToken);

        var ids = colaboradores.Select(c => c.Id).ToList();
        var apontamentos = await _db.Apontamentos
            .AsNoTracking()
            .Include(a => a.Frente).ThenInclude(f => f.Obra)
            .Include(a => a.Frente).ThenInclude(f => f.Etapa)
            .Include(a => a.MotivoParada)
            .Where(a => ids.Contains(a.ColaboradorId) && a.Data == dia)
            .OrderBy(a => a.HoraInicio)
            .ToListAsync(cancellationToken);

        var jornadas = await _db.JornadasDia.AsNoTracking()
            .Where(j => ids.Contains(j.ColaboradorId) && j.Data == dia)
            .ToListAsync(cancellationToken);

        var agora = HorarioPlanta(config);
        var porColab = apontamentos.GroupBy(a => a.ColaboradorId).ToDictionary(g => g.Key, g => g.ToList());

        var turnoColabs = colaboradores.Select(c =>
        {
            var lista = porColab.GetValueOrDefault(c.Id) ?? [];
            var jornada = jornadas.FirstOrDefault(j => j.ColaboradorId == c.Id);
            var situacao = jornada?.Situacao ?? (c.Ativo ? SituacaoJornada.Presente : SituacaoJornada.Afastado);
            var teto = jornada?.MinutosApurados > 0 ? jornada.MinutosApurados : config.JornadaPadraoMinutos;
            var semJornada = jornada is null;
            var aberto = lista.FirstOrDefault(a => a.HoraFim is null);
            var trabalhados = lista.Where(a => a.HoraFim is not null && !(a.Frente.Etapa?.Indireta ?? false))
                .Sum(a => a.MinutosEfetivos ?? 0);
            if (aberto is not null && !(aberto.Frente.Etapa?.Indireta ?? false))
                trabalhados += CalculoApontamento.MinutosEfetivos(aberto.HoraInicio, agora, config.IntervaloInicio, config.IntervaloFim);
            var parados = lista.Where(a => a.HoraFim is not null && (a.Frente.Etapa?.Indireta ?? false))
                .Sum(a => a.MinutosEfetivos ?? 0);
            if (aberto is not null && (aberto.Frente.Etapa?.Indireta ?? false))
                parados += CalculoApontamento.MinutosEfetivos(aberto.HoraInicio, agora, config.IntervaloInicio, config.IntervaloFim);

            var decorrido = CalculoApontamento.MinutosEfetivos(config.JornadaInicio, agora, config.IntervaloInicio, config.IntervaloFim);
            var naoApropriado = Math.Max(0, decorrido - trabalhados - parados);
            var ocioso = 0;
            if (aberto is null && situacao == SituacaoJornada.Presente)
            {
                var origem = lista.Where(a => a.HoraFim is not null).Select(a => a.HoraFim!.Value)
                    .DefaultIfEmpty(config.JornadaInicio).Max();
                ocioso = CalculoApontamento.MinutosGap(origem, agora, config.IntervaloInicio, config.IntervaloFim);
            }

            return new TurnoColaboradorDto
            {
                Id = c.Id,
                Matricula = c.Matricula,
                Nome = c.Nome,
                Funcao = c.Funcao,
                Ativo = c.Ativo,
                Situacao = situacao,
                MinutosApurados = teto,
                JornadaNaoVerificada = semJornada,
                JornadaEntrada = jornada?.Entrada,
                JornadaSaida = jornada?.Saida,
                MinutosTrabalhados = trabalhados,
                MinutosParados = parados,
                MinutosNaoApropriados = naoApropriado,
                MinutosOciosos = ocioso,
                // RN-13: CustoHora ausente para Encarregado.
                CustoHora = Permissoes.Tem(quem.Perfil, Permissoes.Gerente) ? c.CustoHora : null,
                Aberto = aberto is null ? null : Mapear(aberto),
                Apontamentos = lista.Select(Mapear).ToList()
            };
        }).ToList();

        var obras = await _db.Obras.AsNoTracking()
            .Where(o => !o.Interna)
            .OrderBy(o => o.Nome)
            .ToListAsync(cancellationToken);
        var frentes = await _db.Frentes.AsNoTracking()
            .Include(f => f.Obra)
            .Include(f => f.Etapa)
            .Include(f => f.Equipe)
            .Where(f => f.Ativa)
            .OrderBy(f => f.Nome)
            .ToListAsync(cancellationToken);
        var motivos = await _db.MotivosParada.AsNoTracking().OrderBy(m => m.Nome).ToListAsync(cancellationToken);

        return new TurnoDto
        {
            Data = dia,
            EquipeId = equipe.Id,
            EquipeNome = equipe.Nome,
            EquipeCor = equipe.Cor,
            JornadaInicio = config.JornadaInicio.ToString("HH:mm"),
            JornadaFim = config.JornadaFim.ToString("HH:mm"),
            IntervaloInicio = config.IntervaloInicio.ToString("HH:mm"),
            IntervaloFim = config.IntervaloFim.ToString("HH:mm"),
            JornadaPadraoMinutos = config.JornadaPadraoMinutos,
            Colaboradores = turnoColabs,
            Obras = obras.Select(o => new ObraDto
            {
                Id = o.Id,
                Nome = o.Nome,
                Cliente = o.Cliente,
                Tipo = o.Tipo,
                Local = o.Local,
                CodigoSienge = o.CodigoSienge,
                Interna = o.Interna,
                Status = o.Status
            }).ToList(),
            Frentes = frentes.Select(f => new FrenteDto
            {
                Id = f.Id,
                ObraId = f.ObraId,
                ObraNome = f.Obra.Nome,
                Nome = f.Nome,
                EtapaId = f.EtapaId,
                EtapaNome = f.Etapa.Nome,
                EtapaIndireta = f.Etapa.Indireta,
                EquipeId = f.EquipeId,
                EquipeNome = f.Equipe?.Nome,
                Unidade = f.Unidade,
                QuantidadePrevista = f.QuantidadePrevista,
                QuantidadeConcluida = f.QuantidadeConcluida,
                PercentualAvanco = f.QuantidadePrevista <= 0
                    ? 0
                    : Math.Round(f.QuantidadeConcluida / f.QuantidadePrevista * 100, 1),
                TaxaAcoKgPorUnidade = f.TaxaAcoKgPorUnidade,
                HhOrcadoPorUnidade = f.HhOrcadoPorUnidade,
                Cor = f.Cor,
                Ativa = f.Ativa
            }).ToList(),
            MotivosParada = motivos.Select(m => new MotivoParadaDto
            {
                Id = m.Id,
                Nome = m.Nome,
                ExigeObservacao = m.ExigeObservacao
            }).ToList()
        };
    }

    public async Task<ApontamentoDto> IniciarAsync(
        IniciarApontamentoDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default,
        bool origemLote = false)
    {
        var existente = await _db.Apontamentos
            .Include(a => a.Frente).ThenInclude(f => f.Obra)
            .Include(a => a.Frente).ThenInclude(f => f.Etapa)
            .Include(a => a.MotivoParada)
            .FirstOrDefaultAsync(a => a.ClienteUuid == dto.ClienteUuid, cancellationToken);
        if (existente is not null)
            return Mapear(existente);

        var config = await ObterConfigAsync(cancellationToken);
        var dia = dto.Data ?? _relogio.HojeSaoPaulo;

        var colaborador = await _db.Colaboradores.FirstOrDefaultAsync(c => c.Id == dto.ColaboradorId, cancellationToken)
            ?? throw new RegraNegocioException("COLABORADOR_NAO_ENCONTRADO", "Colaborador não encontrado.", 404);
        GarantirEscopoEquipe(quem, colaborador.EquipeId);
        await _fechamento.GarantirAbertoAsync(dia, colaborador.EquipeId, cancellationToken);

        if (!colaborador.Ativo)
            throw new RegraNegocioException("COLABORADOR_INATIVO", "Colaborador inativo não recebe apontamento.", 422);

        var frente = await _db.Frentes
            .Include(f => f.Obra)
            .Include(f => f.Etapa)
            .FirstOrDefaultAsync(f => f.Id == dto.FrenteId && f.Ativa, cancellationToken)
            ?? throw new RegraNegocioException("FRENTE_NAO_ENCONTRADA", "Frente não encontrada.", 404);

        var hora = LimitarHorario(dto.HoraInicio, config, tetoAgora: !origemLote);
        var agora = _relogio.UtcAgora;

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var aberto = await _db.Apontamentos
            .Include(a => a.Frente).ThenInclude(f => f.Etapa)
            .FirstOrDefaultAsync(a => a.ColaboradorId == colaborador.Id && a.HoraFim == null, cancellationToken);

        // RN-03: iniciar com serviço aberto encerra o anterior no mesmo horário.
        if (aberto is not null)
        {
            if (hora < aberto.HoraInicio)
                throw new RegraNegocioException("HORARIO_INVALIDO", "O início do novo serviço não pode ser antes do serviço aberto.");
            await FecharAsync(aberto, hora, config, quem, agora, cancellationToken);
        }
        else
        {
            var ultimoFim = await UltimoFimAsync(colaborador.Id, dia, cancellationToken);
            var origemGap = ultimoFim ?? config.JornadaInicio;
            var gap = CalculoApontamento.MinutosGap(origemGap, hora, config.IntervaloInicio, config.IntervaloFim);

            // RN-04: intervalo > 10 min exige motivo e vira parada na frente interna.
            if (CalculoApontamento.ExigeMotivoParada(gap))
            {
                if (dto.MotivoParadaId is null)
                    throw new RegraNegocioException("MOTIVO_OBRIGATORIO", "Informe o motivo da parada entre os serviços.", 422);

                var motivo = await _db.MotivosParada.FirstOrDefaultAsync(m => m.Id == dto.MotivoParadaId, cancellationToken)
                    ?? throw new RegraNegocioException("MOTIVO_NAO_ENCONTRADO", "Motivo de parada não encontrado.", 404);
                if (motivo.ExigeObservacao && string.IsNullOrWhiteSpace(dto.Observacao))
                    throw new RegraNegocioException("OBSERVACAO_OBRIGATORIA", "Este motivo exige observação.", 422);

                var frenteParada = await FrenteParadaAsync(cancellationToken);
                await FecharNovoAsync(
                    colaborador, frenteParada, dia, origemGap, hora, motivo.Id, dto.Observacao,
                    dto.DispositivoId, quem, agora, config, GeradorId.Novo(), cancellationToken);
            }
        }

        var criado = await FecharNovoAsync(
            colaborador, frente, dia, hora, null, null, dto.Observacao,
            dto.DispositivoId, quem, agora, config, dto.ClienteUuid, cancellationToken);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);
            throw new RegraNegocioException("RN-02", "Já existe um serviço aberto para este colaborador.", 409);
        }
        return Mapear(criado);
    }

    public async Task<EncerrarResultadoDto> EncerrarAsync(
        Guid id,
        EncerrarApontamentoDto dto,
        UsuarioLogado quem,
        CancellationToken cancellationToken = default,
        bool origemLote = false)
    {
        var apontamento = await _db.Apontamentos
            .Include(a => a.Frente).ThenInclude(f => f.Obra)
            .Include(a => a.Frente).ThenInclude(f => f.Etapa)
            .Include(a => a.MotivoParada)
            .Include(a => a.Colaborador)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new RegraNegocioException("APONTAMENTO_NAO_ENCONTRADO", "Apontamento não encontrado.", 404);

        GarantirEscopoEquipe(quem, apontamento.Colaborador.EquipeId);

        if (apontamento.HoraFim is not null)
            return new EncerrarResultadoDto { Apontamento = Mapear(apontamento) };

        var config = await ObterConfigAsync(cancellationToken);
        await _fechamento.GarantirAbertoAsync(apontamento.Data, apontamento.Colaborador.EquipeId, cancellationToken);

        var horaFim = LimitarHorario(dto.HoraFim, config, tetoAgora: !origemLote);
        if (horaFim <= apontamento.HoraInicio)
            throw new RegraNegocioException("HORARIO_INVALIDO", "O término deve ser depois do início.", 422);

        await FecharAsync(apontamento, horaFim, config, quem, _relogio.UtcAgora, cancellationToken);

        var avisos = new List<string>();
        decimal? jaHoje = null;
        if (dto.Producao is { Quantidade: > 0 } prod && !(apontamento.Frente.Etapa?.Indireta ?? false))
        {
            jaHoje = await _db.Producoes
                .Where(p => p.FrenteId == apontamento.FrenteId && p.Data == apontamento.Data)
                .SumAsync(p => p.Quantidade, cancellationToken);
            if (jaHoje > 0)
                avisos.Add("QUANTIDADE_JA_LANCADA_HOJE"); // RN-05

            var prodExistente = await _db.Producoes.FirstOrDefaultAsync(p => p.ClienteUuid == prod.ClienteUuid, cancellationToken);
            if (prodExistente is null)
            {
                // RN-06b: teto de 110% da previsão sem Gerente.
                ProducaoService.GarantirTetoPrevisao(
                    apontamento.Frente.QuantidadePrevista,
                    apontamento.Frente.QuantidadeConcluida + prod.Quantidade,
                    quem);

                _db.Producoes.Add(new Producao
                {
                    TenantId = _db.TenantId,
                    FrenteId = apontamento.FrenteId,
                    Data = apontamento.Data,
                    Quantidade = prod.Quantidade,
                    ApontamentoId = apontamento.Id,
                    RegistradoPorId = quem.Id,
                    RegistradoEm = _relogio.UtcAgora,
                    ClienteUuid = prod.ClienteUuid
                });
                apontamento.Frente.QuantidadeConcluida += prod.Quantidade;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new EncerrarResultadoDto
        {
            Apontamento = Mapear(apontamento),
            Avisos = avisos,
            QuantidadeJaLancadaHoje = jaHoje
        };
    }

    private async Task FecharAsync(
        Apontamento aberto,
        TimeOnly horaFim,
        Configuracao config,
        UsuarioLogado quem,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        if (CalculoApontamento.InteiramenteNoIntervalo(aberto.HoraInicio, horaFim, config.IntervaloInicio, config.IntervaloFim))
            throw new RegraNegocioException("INTERVALO_NAO_APONTAVEL", "O intervalo de refeição não é apontável.", 422);

        var minutos = CalculoApontamento.MinutosEfetivos(aberto.HoraInicio, horaFim, config.IntervaloInicio, config.IntervaloFim);
        var (teto, semJornada) = await TetoJornadaAsync(aberto.ColaboradorId, aberto.Data, config, cancellationToken);
        var ja = await MinutosDoDiaAsync(aberto.ColaboradorId, aberto.Data, aberto.Id, cancellationToken);
        var excedente = CalculoApontamento.ExcedenteJornada(ja, minutos, teto);
        if (excedente > 0)
        {
            throw new RegraNegocioException(
                "JORNADA_EXCEDIDA",
                $"O apontamento excede em {excedente} minutos a jornada apurada de {teto / 60:00}:{teto % 60:00}.",
                409,
                new Dictionary<string, object?>
                {
                    ["excedenteMinutos"] = excedente,
                    ["jornadaMinutos"] = teto
                });
        }

        aberto.HoraFim = horaFim;
        aberto.MinutosEfetivos = minutos;
        aberto.JornadaNaoVerificada = semJornada;
        aberto.AlteradoPorId = quem.Id;
        aberto.AlteradoEm = agora;
    }

    private async Task<Apontamento> FecharNovoAsync(
        Colaborador colaborador,
        Frente frente,
        DateOnly dia,
        TimeOnly inicio,
        TimeOnly? fim,
        Guid? motivoId,
        string? observacao,
        string? dispositivoId,
        UsuarioLogado quem,
        DateTimeOffset agora,
        Configuracao config,
        Guid clienteUuid,
        CancellationToken cancellationToken)
    {
        int? minutos = null;
        var semJornada = true;
        if (fim is TimeOnly horaFim)
        {
            if (CalculoApontamento.InteiramenteNoIntervalo(inicio, horaFim, config.IntervaloInicio, config.IntervaloFim))
                throw new RegraNegocioException("INTERVALO_NAO_APONTAVEL", "O intervalo de refeição não é apontável.", 422);
            minutos = CalculoApontamento.MinutosEfetivos(inicio, horaFim, config.IntervaloInicio, config.IntervaloFim);
            var (teto, sem) = await TetoJornadaAsync(colaborador.Id, dia, config, cancellationToken);
            semJornada = sem;
            var ja = await MinutosDoDiaAsync(colaborador.Id, dia, null, cancellationToken);
            var excedente = CalculoApontamento.ExcedenteJornada(ja, minutos.Value, teto);
            if (excedente > 0)
            {
                throw new RegraNegocioException(
                    "JORNADA_EXCEDIDA",
                    $"O apontamento excede em {excedente} minutos a jornada apurada de {teto / 60:00}:{teto % 60:00}.",
                    409,
                    new Dictionary<string, object?>
                    {
                        ["excedenteMinutos"] = excedente,
                        ["jornadaMinutos"] = teto
                    });
            }
        }
        else
        {
            (_, semJornada) = await TetoJornadaAsync(colaborador.Id, dia, config, cancellationToken);
        }

        var apontamento = new Apontamento
        {
            TenantId = _db.TenantId,
            ClienteUuid = clienteUuid,
            ColaboradorId = colaborador.Id,
            FrenteId = frente.Id,
            Data = dia,
            HoraInicio = inicio,
            HoraFim = fim,
            MinutosEfetivos = minutos,
            MotivoParadaId = motivoId,
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
            Origem = OrigemApontamento.App,
            JornadaNaoVerificada = semJornada,
            DispositivoId = dispositivoId,
            CriadoPorId = quem.Id,
            CriadoEm = agora
        };
        _db.Apontamentos.Add(apontamento);
        apontamento.Frente = frente;
        apontamento.Colaborador = colaborador;
        return apontamento;
    }

    private async Task<TimeOnly?> UltimoFimAsync(Guid colaboradorId, DateOnly dia, CancellationToken cancellationToken)
    {
        var lista = await _db.Apontamentos
            .Where(a => a.ColaboradorId == colaboradorId && a.Data == dia && a.HoraFim != null)
            .Select(a => a.HoraFim)
            .ToListAsync(cancellationToken);
        return lista.Count == 0 ? null : lista.Max();
    }

    private async Task<int> MinutosDoDiaAsync(Guid colaboradorId, DateOnly dia, Guid? excetoId, CancellationToken cancellationToken)
    {
        var query = _db.Apontamentos.Where(a =>
            a.ColaboradorId == colaboradorId && a.Data == dia && a.HoraFim != null && a.MinutosEfetivos != null);
        if (excetoId is Guid id)
            query = query.Where(a => a.Id != id);
        return await query.SumAsync(a => a.MinutosEfetivos!.Value, cancellationToken);
    }

    private async Task<(int Teto, bool SemJornada)> TetoJornadaAsync(
        Guid colaboradorId,
        DateOnly dia,
        Configuracao config,
        CancellationToken cancellationToken)
    {
        var jornada = await _db.JornadasDia.AsNoTracking()
            .FirstOrDefaultAsync(j => j.ColaboradorId == colaboradorId && j.Data == dia, cancellationToken);
        if (jornada is { MinutosApurados: > 0 })
            return (jornada.MinutosApurados, false);
        return (config.JornadaPadraoMinutos, true);
    }

    private async Task<Frente> FrenteParadaAsync(CancellationToken cancellationToken) =>
        await _db.Frentes
            .Include(f => f.Obra)
            .Include(f => f.Etapa)
            .FirstOrDefaultAsync(f => f.Obra.Interna && f.Etapa.Indireta, cancellationToken)
        ?? throw new RegraNegocioException("FRENTE_PARADA_AUSENTE", "Frente interna de parada não encontrada.", 500);

    private async Task<Equipe> ResolverEquipeAsync(Guid? equipeId, UsuarioLogado quem, CancellationToken cancellationToken)
    {
        var id = equipeId ?? quem.EquipeId
            ?? throw new RegraNegocioException("EQUIPE_OBRIGATORIA", "Informe a equipe.", 422);
        return await _db.Equipes.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new RegraNegocioException("EQUIPE_NAO_ENCONTRADA", "Equipe não encontrada.", 404);
    }

    private static void GarantirEscopoEquipe(UsuarioLogado quem, Guid equipeId)
    {
        if (Permissoes.Tem(quem.Perfil, Permissoes.Gerente))
            return;
        if (quem.EquipeId != equipeId)
            throw new RegraNegocioException("RN-10", "Encarregado só aponta a própria equipe.", 403);
    }

    private TimeOnly LimitarHorario(TimeOnly informado, Configuracao config, bool tetoAgora = true)
    {
        var hora = informado < config.JornadaInicio ? config.JornadaInicio : informado;
        var teto = config.JornadaFim;
        if (tetoAgora)
        {
            var agora = _relogio.HoraSaoPaulo;
            teto = agora < config.JornadaFim ? agora : config.JornadaFim;
        }
        return hora > teto ? teto : hora;
    }

    private TimeOnly HorarioPlanta(Configuracao config)
    {
        var agora = _relogio.HoraSaoPaulo;
        if (agora < config.JornadaInicio) return config.JornadaInicio;
        if (agora > config.JornadaFim) return config.JornadaFim;
        return agora;
    }

    private async Task<Configuracao> ObterConfigAsync(CancellationToken cancellationToken) =>
        await _db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
        ?? new Configuracao();

    private static ApontamentoDto Mapear(Apontamento a) => new()
    {
        Id = a.Id,
        ClienteUuid = a.ClienteUuid,
        ColaboradorId = a.ColaboradorId,
        FrenteId = a.FrenteId,
        FrenteNome = a.Frente?.Nome ?? string.Empty,
        FrenteCor = a.Frente?.Cor ?? "#4A5560",
        Unidade = a.Frente?.Unidade ?? "un",
        ObraId = a.Frente?.ObraId ?? Guid.Empty,
        ObraNome = a.Frente?.Obra?.Nome ?? string.Empty,
        FrenteIndireta = a.Frente?.Etapa?.Indireta ?? false,
        Data = a.Data,
        HoraInicio = a.HoraInicio,
        HoraFim = a.HoraFim,
        MinutosEfetivos = a.MinutosEfetivos,
        MotivoParadaId = a.MotivoParadaId,
        MotivoParadaNome = a.MotivoParada?.Nome,
        Observacao = a.Observacao,
        JornadaNaoVerificada = a.JornadaNaoVerificada
    };
}
