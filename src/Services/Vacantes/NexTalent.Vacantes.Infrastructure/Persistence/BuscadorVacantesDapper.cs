using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace NexTalent.Vacantes.Infrastructure.Persistence;

public sealed record FiltroHabilidad(Guid HabilidadId, byte? NivelMinimo = null);

public sealed record FiltroBusquedaVacantes(
    string? Texto = null,
    decimal? SalarioMin = null,
    string? Modalidad = null,
    string? Ubicacion = null,
    string? Jornada = null,
    IReadOnlyCollection<FiltroHabilidad>? Habilidades = null,
    int Pagina = 1,
    int TamanoPagina = 20);

public sealed record TarjetaVacanteDto(
    Guid Id,
    string Titulo,
    Guid EmpresaId,
    string NombreEmpresa,
    bool EmpresaVerificada,
    decimal IndiceRespuestaPct,
    decimal SalarioMin,
    decimal SalarioMax,
    string TipoSalario,
    string Modalidad,
    string Jornada,
    string Ubicacion,
    DateTime? PublicadaEn,
    DateTime? ExpiraEn,
    int TotalResultados);

/// <summary>
/// Ejecuta vacantes.sp_BuscarVacantes con Dapper y parametro TVP vacantes.FiltroHabilidadTipo
/// para reutilizar el plan de ejecucion compilado y cumplir RNF-01 (&lt; 200 ms sobre 50,000 vacantes).
/// </summary>
public sealed class BuscadorVacantesDapper(string connectionString)
{
    public async Task<IReadOnlyList<TarjetaVacanteDto>> BuscarAsync(
        FiltroBusquedaVacantes filtro, CancellationToken ct)
    {
        var tablaHabilidades = new DataTable();
        tablaHabilidades.Columns.Add("habilidad_id", typeof(Guid));
        tablaHabilidades.Columns.Add("nivel_minimo", typeof(byte));

        if (filtro.Habilidades is { Count: > 0 })
        {
            foreach (var h in filtro.Habilidades.DistinctBy(x => x.HabilidadId))
            {
                tablaHabilidades.Rows.Add(h.HabilidadId, h.NivelMinimo is { } n ? n : DBNull.Value);
            }
        }

        var parametros = new DynamicParameters();
        parametros.Add("@texto", string.IsNullOrWhiteSpace(filtro.Texto) ? null : filtro.Texto.Trim(), DbType.String, size: 200);
        parametros.Add("@salario_min", filtro.SalarioMin, DbType.Decimal);
        parametros.Add("@modalidad", string.IsNullOrWhiteSpace(filtro.Modalidad) ? null : filtro.Modalidad.Trim(), DbType.AnsiString, size: 20);
        parametros.Add("@ubicacion", string.IsNullOrWhiteSpace(filtro.Ubicacion) ? null : filtro.Ubicacion.Trim(), DbType.String, size: 120);
        parametros.Add("@jornada", string.IsNullOrWhiteSpace(filtro.Jornada) ? null : filtro.Jornada.Trim(), DbType.AnsiString, size: 30);
        parametros.Add("@habilidades", tablaHabilidades.AsTableValuedParameter("[vacantes].[FiltroHabilidadTipo]"));
        parametros.Add("@pagina", Math.Max(1, filtro.Pagina), DbType.Int32);
        parametros.Add("@tamano_pagina", Math.Clamp(filtro.TamanoPagina, 1, 50), DbType.Int32);

        await using var conexion = new SqlConnection(connectionString);
        await conexion.OpenAsync(ct);

        var comando = new CommandDefinition(
            "[vacantes].[sp_BuscarVacantes]",
            parametros,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct);

        var filas = await conexion.QueryAsync<TarjetaVacanteDto>(comando);
        return [.. filas];
    }
}
