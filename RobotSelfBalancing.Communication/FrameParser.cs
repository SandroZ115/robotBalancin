using System;
using System.Globalization;
using RobotSelfBalancing.Domain;

namespace RobotSelfBalancing.Communication
{
    public static class FrameParser
    {
        public static bool TryParse(string line, out Telemetry? telemetry)
        {
            telemetry = null;

            if (string.IsNullOrWhiteSpace(line)) return false;

            line = line.Trim();

            if (!line.StartsWith("$") || !line.Contains('*')) return false;

            int asteriskIndex = line.IndexOf('*');
            string payload = line.Substring(1, asteriskIndex - 1);
            string checksumHex = line.Substring(asteriskIndex + 1);

            byte computedXor = 0;
            foreach (char c in payload)
            {
                computedXor ^= (byte)c;
            }

            if (!byte.TryParse(checksumHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte receivedXor))
            {
                return false;
            }

            if (computedXor != receivedXor) return false;

            string[] parts = payload.Split(',');
            if (parts.Length < 11) return false; // La trama completa contiene 11 campos

            try
            {
                int seq = int.Parse(parts[0], CultureInfo.InvariantCulture);
                long tiempoMs = long.Parse(parts[1], CultureInfo.InvariantCulture);
                float pitch = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float gyro = float.Parse(parts[3], CultureInfo.InvariantCulture);
                float u = float.Parse(parts[4], CultureInfo.InvariantCulture);
                short pwmIzq = short.Parse(parts[5], CultureInfo.InvariantCulture);
                short pwmDer = short.Parse(parts[6], CultureInfo.InvariantCulture);
                int encIzq = int.Parse(parts[7], CultureInfo.InvariantCulture);
                int encDer = int.Parse(parts[8], CultureInfo.InvariantCulture);
                float vbat = float.Parse(parts[9], CultureInfo.InvariantCulture);
                byte estado = byte.Parse(parts[10], CultureInfo.InvariantCulture);

                // Instanciación del Record posicional de Telemetry
                telemetry = new Telemetry(
                    seq,
                    tiempoMs,
                    pitch,
                    gyro,
                    u,
                    pwmIzq,
                    pwmDer,
                    encIzq,
                    encDer,
                    vbat,
                    estado
                );

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}