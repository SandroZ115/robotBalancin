namespace RobotSelfBalancing.Domain;

/// <summary>
/// Representa una trama individual de telemetría del robot.
/// </summary>
public record Telemetry(
    int Seq,
    long TiempoMs,
    float PitchDeg,
    float GyroDps,
    float U,
    short PwmIzq,
    short PwmDer,
    int EncIzq,
    int EncDer,
    float VBat,
    byte Estado
);

/// <summary>
/// Parámetros de sintonía PID para enviar al robot (RF-04).
/// </summary>
public class PidParameters
{
    public float Kp { get; set; }
    public float Ki { get; set; }
    public float Kd { get; set; }
    public float Alpha { get; set; }
    public float ThetaRef { get; set; }
}

/// <summary>
/// Métricas calculadas al finalizar una sesión (RF-07).
/// </summary>
public class SessionMetrics
{
    public double SettlingTimeMs { get; set; }
    public double OvershootPct { get; set; }
    public double PacketLossPct { get; set; }
    public double JitterMs { get; set; }
}