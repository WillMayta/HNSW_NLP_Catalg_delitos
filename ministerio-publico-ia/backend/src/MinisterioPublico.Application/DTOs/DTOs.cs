namespace MinisterioPublico.Application.DTOs;

public record DelitoOriginalDto(
    Guid Id, string IdUnicoCaso, string TextoOriginal, DateTime FechaIngresoCaso,
    string Estado, string SiglaFiscalia, string NombreFiscal);

public record DelitoNormalizadoDto(
    Guid Id, Guid DelitoOriginalId, string TextoOriginal, string TextoNormalizado,
    IReadOnlyList<string> ReglasAplicadas);

public record CargaMasivaResultadoDto(
    Guid LoteCargaId, int TotalRegistros, int RegistrosExitosos, int RegistrosConError,
    IReadOnlyList<string> Errores);

public record BusquedaInteligenteRequestDto(string TextoDelito, int CantidadResultados = 10);

public record BusquedaInteligenteResultadoDto(
    string TextoConsultado, string TextoNormalizado,
    IReadOnlyList<DelitoSimilarDto> DelitosRelacionados);

public record DelitoSimilarDto(
    string IdDelito, string TextoNormalizado, double PorcentajeSimilitud,
    string? FamiliaJuridica, string? DelitoCatalogoAsociado);

public record PropuestaAgrupamientoResumenDto(
    Guid Id, string DelitoRepresentativoSugerido, int CantidadVariantes,
    double CohesionPromedio, string Estado, IReadOnlyList<string> EjemplosVariantes);

public record ValidarPropuestaRequestDto(
    Guid PropuestaId, string Decision, // "Aprobar" | "Rechazar" | "AprobarConModificaciones"
    string? NombreGenericoFinal, Guid? FamiliaDelictivaId, string? ArticuloPrincipal,
    string? Observaciones);

public record DelitoCatalogoDto(
    Guid Id, string NombreGenerico, string? Descripcion, string FamiliaDelictiva,
    string? ArticuloPrincipal, string Estado, int CantidadVariantes, DateTime FechaCreacion);

/// <summary>Resultado de procesar una validación jurídica sobre una propuesta de agrupamiento.</summary>
public record ResultadoValidacionDto(
    bool Aprobada, string Mensaje, DelitoCatalogoDto? DelitoCatalogoCreado);


public record CrearDelitoCatalogoRequestDto(
    string NombreGenerico, string? Descripcion, Guid FamiliaDelictivaId,
    string? ArticuloPrincipal, IReadOnlyList<string>? LeyesComplementarias);

public record IndicadoresDashboardDto(
    int TotalDelitosOriginales,
    int TotalVariantesDetectadas,
    int TotalDelitosCatalogoConsolidados,
    int PropuestasPendientesValidacion,
    double PorcentajeRegistrosConsolidados,
    double TiempoPromedioBusquedaMs,
    IReadOnlyList<DelitoConInconsistenciaDto> DelitosConMayorInconsistencia,
    IReadOnlyList<FiscaliaVariabilidadDto> FiscaliasConMayorVariabilidad,
    IReadOnlyList<EvolucionTemporalDto> EvolucionTemporal);

public record DelitoConInconsistenciaDto(string NombreGenerico, int CantidadVariantes);
public record FiscaliaVariabilidadDto(string SiglaFiscalia, int CantidadDenominacionesDistintas);
public record EvolucionTemporalDto(string Periodo, int CantidadCasos, int CantidadConsolidados);
