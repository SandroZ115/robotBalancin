using Microsoft.Data.SqlClient;
using RobotSelfBalancing.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace RobotSelfBalancing.Persistence
{
    public class TelemetryRepository
    {
        private readonly string _connectionString;

        public TelemetryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Crea una nueva sesión en la base de datos y devuelve su SesionId.
        /// </summary>
        public async Task<int> CrearSesionAsync(string firmwareVer, float kp, float ki, float kd, float alpha, short frecHz, string observaciones, CancellationToken ct = default)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            const string sql = @"
            INSERT INTO Sesion (FirmwareVer, Kp, Ki, Kd, Alpha, FrecControlHz, Observaciones)
            OUTPUT INSERTED.SesionId
            VALUES (@Firmware, @Kp, @Ki, @Kd, @Alpha, @Frec, @Obs);";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Firmware", (object?)firmwareVer ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Kp", kp);
            cmd.Parameters.AddWithValue("@Ki", ki);
            cmd.Parameters.AddWithValue("@Kd", kd);
            cmd.Parameters.AddWithValue("@Alpha", alpha);
            cmd.Parameters.AddWithValue("@Frec", frecHz);
            cmd.Parameters.AddWithValue("@Obs", (object?)observaciones ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Finaliza la sesión actual actualizando la fecha de fin.
        /// </summary>
        public async Task FinalizarSesionAsync(int sesionId, CancellationToken ct = default)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            const string sql = "UPDATE Sesion SET FechaFin = SYSDATETIME() WHERE SesionId = @SesionId;";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SesionId", sesionId);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        /// <summary>
        /// Inserción masiva de tramas por lotes usando SqlBulkCopy (RF-06).
        /// </summary>
        public async Task BulkInsertAsync(int sesionId, List<Telemetry> lote, CancellationToken ct = default)
        {
            if (lote == null || lote.Count == 0) return;

            // 1. Construir DataTable en memoria
            using var table = new DataTable();
            table.Columns.Add("SesionId", typeof(int));
            table.Columns.Add("Seq", typeof(int));
            table.Columns.Add("TiempoMs", typeof(long));
            table.Columns.Add("PitchDeg", typeof(float));
            table.Columns.Add("GyroDps", typeof(float));
            table.Columns.Add("U", typeof(float));
            table.Columns.Add("PwmIzq", typeof(short));
            table.Columns.Add("PwmDer", typeof(short));
            table.Columns.Add("EncIzq", typeof(int));
            table.Columns.Add("EncDer", typeof(int));
            table.Columns.Add("VBat", typeof(float));
            table.Columns.Add("Estado", typeof(byte));

            foreach (var t in lote)
            {
                table.Rows.Add(sesionId, t.Seq, t.TiempoMs, t.PitchDeg, t.GyroDps, t.U,
                               t.PwmIzq, t.PwmDer, t.EncIzq, t.EncDer, t.VBat, t.Estado);
            }

            // 2. Ejecutar inserción en lote hacia SQL Server
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = "dbo.Telemetria",
                BatchSize = lote.Count,
                BulkCopyTimeout = 30
            };

            bulkCopy.ColumnMappings.Add("SesionId", "SesionId");
            bulkCopy.ColumnMappings.Add("Seq", "Seq");
            bulkCopy.ColumnMappings.Add("TiempoMs", "TiempoMs");
            bulkCopy.ColumnMappings.Add("PitchDeg", "PitchDeg");
            bulkCopy.ColumnMappings.Add("GyroDps", "GyroDps");
            bulkCopy.ColumnMappings.Add("U", "U");
            bulkCopy.ColumnMappings.Add("PwmIzq", "PwmIzq");
            bulkCopy.ColumnMappings.Add("PwmDer", "PwmDer");
            bulkCopy.ColumnMappings.Add("EncIzq", "EncIzq");
            bulkCopy.ColumnMappings.Add("EncDer", "EncDer");
            bulkCopy.ColumnMappings.Add("VBat", "VBat");
            bulkCopy.ColumnMappings.Add("Estado", "Estado");

            await bulkCopy.WriteToServerAsync(table, ct);
        }
    }
}
