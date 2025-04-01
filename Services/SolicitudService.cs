using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using WebPlan.Models;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace WebPlan.Services
{
    public class SolicitudService : ISolicitudService
    {
        private readonly IConfiguration _config;

        public SolicitudService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<Solicitud>> GetSolicitudesAsync(int nidAgencia, string cEstados)
        {
            var solicitudes = new List<Solicitud>();
            var connectionString = _config.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var cmd = new SqlCommand("CRE_LisSolGenPPD_SP", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nidAgencia", nidAgencia);
                    cmd.Parameters.AddWithValue("@cEstados", cEstados);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            solicitudes.Add(new Solicitud
                            {
                                IdSolicitud = reader.GetInt32(reader.GetOrdinal("idSolicitud")),
                                CNombre = reader.IsDBNull(reader.GetOrdinal("cNombre")) ? null : reader.GetString(reader.GetOrdinal("cNombre")),
                                NCapitalSolicitado = reader.GetDecimal(reader.GetOrdinal("nCapitalSolicitado")),
                                NCuotas = reader.GetInt32(reader.GetOrdinal("nCuotas")),
                                DFechaRegistro = reader.GetDateTime(reader.GetOrdinal("dFechaRegistro")),
                                HFechaRegistro = ParseTimeSpan(reader["hFechaRegistro"].ToString()),
                                CProducto = reader.IsDBNull(reader.GetOrdinal("cProducto")) ? null : reader.GetString(reader.GetOrdinal("cProducto")),
                                CCodCliente = reader.IsDBNull(reader.GetOrdinal("cCodCliente")) ? null : reader.GetString(reader.GetOrdinal("cCodCliente")),
                                CDocumentoID = reader.IsDBNull(reader.GetOrdinal("cDocumentoID")) ? null : reader.GetString(reader.GetOrdinal("cDocumentoID")),
                                DFechaAprobacion = reader.IsDBNull(reader.GetOrdinal("dFechaAprobacion")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("dFechaAprobacion")),
                                DHoraAprobacion = GetTimeSpanFromReader(reader, "hFechaAprobacion"),
                                IdEstado = reader.GetInt32(reader.GetOrdinal("IdEstado")),
                                CEstado = reader.IsDBNull(reader.GetOrdinal("cEstado")) ? null : reader.GetString(reader.GetOrdinal("cEstado")),
                                CAsesor = reader.IsDBNull(reader.GetOrdinal("cAsesor")) ? null : reader.GetString(reader.GetOrdinal("cAsesor")),
                                IdTipoDocumento = reader.GetInt32(reader.GetOrdinal("idTipoDocumento")),
                                IdTipoPersona = reader.GetInt32(reader.GetOrdinal("idTipoPersona")),
                                CTelefono = reader.IsDBNull(reader.GetOrdinal("cTelefono")) ? null : reader.GetString(reader.GetOrdinal("cTelefono"))
                            });
                        }
                    }
                }
            }
            return solicitudes;
        }
    private TimeSpan ParseTimeSpan(string timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return TimeSpan.Zero;

            // Intenta varios formatos comunes
            if (TimeSpan.TryParse(timeString, out var result))
                return result;

            // Formato HH:mm:ss
            if (timeString.Contains(":"))
            {
                var parts = timeString.Split(':');
                if (parts.Length >= 2)
                {
                    int hours = int.Parse(parts[0]);
                    int minutes = int.Parse(parts[1]);
                    int seconds = parts.Length > 2 ? int.Parse(parts[2]) : 0;
                    return new TimeSpan(hours, minutes, seconds);
                }
            }

            // Formato HHmmss
            if (timeString.Length == 6)
            {
                return new TimeSpan(
                    int.Parse(timeString.Substring(0, 2)), // horas
                    int.Parse(timeString.Substring(2, 2)), // minutos
                    int.Parse(timeString.Substring(4, 2))  // segundos
                );
            }

            throw new FormatException($"Formato de hora no reconocido: {timeString}");
        }

       
        // Método auxiliar mejorado
        private TimeSpan? GetTimeSpanFromReader(SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);

                if (reader.IsDBNull(ordinal))
                    return null;

                object rawValue = reader[ordinal];

                // Caso 1: Ya es TimeSpan
                if (rawValue is TimeSpan timeSpan)
                    return timeSpan;

                // Caso 2: Es DateTime (tomamos solo la parte horaria)
                if (rawValue is DateTime dateTime)
                    return dateTime.TimeOfDay;

                // Caso 3: Es string (convertimos)
                string stringValue = rawValue.ToString()!;

                // Intenta formatos comunes
                if (TimeSpan.TryParse(stringValue, out var parsedTime))
                    return parsedTime;

                // Formato HHmmss (sin separadores)
                if (stringValue.Length == 6 && int.TryParse(stringValue, out _))
                {
                    return new TimeSpan(
                        int.Parse(stringValue.Substring(0, 2)), // Horas
                        int.Parse(stringValue.Substring(2, 2)), // Minutos
                        int.Parse(stringValue.Substring(4, 2))  // Segundos
                    );
                }

                throw new FormatException($"Formato no reconocido para {columnName}: {stringValue}");
            }
            catch (Exception ex)
            {
                // Log detallado para diagnóstico
                Console.WriteLine($"Error al mapear {columnName}: {ex.Message}");
                return null; // O lanza la excepción si prefieres manejo estricto
            }
        }
    }
}