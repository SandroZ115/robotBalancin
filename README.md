# 🤖 Robot Autoequilibrado - Panel de Control & Telemetría

Sistema de supervisión en tiempo real, adquisición de telemetría y sintonía de parámetros PID para un robot autoequilibrado basado en **ESP32** e interfaz de control desarrollada en **.NET 10 (WPF)**.

---

## 🏗️ Arquitectura del Proyecto

El sistema está estructurado en una arquitectura en capas (.NET 10):

* **`RobotSelfBalancing.Domain`**: Entidades principales (`Telemetry`, `PidParameters`, `SessionMetrics`) y motor de cálculo de métricas de control.
* **`RobotSelfBalancing.Communication`**: Cliente para puerto serie/Bluetooth SPP y parser de tramas con verificación de integridad por Checksum XOR.
* **`RobotSelfBalancing.Persistence`**: Repositorio de base de datos SQL Server y módulo de exportación a formato CSV.
* **`RobotSelfBalancing.UI`**: Interfaz gráfica WPF con visualización de gráficas en tiempo real mediante ScottPlot v5.

---

## 📡 Protocolo de Comunicación (Bluetooth / UART)

Formato de trama delimitada enviada desde el ESP32 ($115200$ baudios):

`$SEQ,TIEMPO_MS,PITCH,GYRO,U,PWM_IZQ,PWM_DER,ENC_IZQ,ENC_DER,VBAT,ESTADO*XOR`

* **Validación:** El checksum XOR se calcula sobre los datos entre el `$` y el `*`.

---

## 📊 Métricas de Desempeño Calculadas

* **Jitter (ms):** Variación en los tiempos de llegada de tramas.
* **Pérdida de Paquetes (%):** Detección de saltos en la secuencia `SEQ`.
* **Sobreimpulso / Overshoot (%):** Pico máximo de inclinación respecto a la referencia.
* **Tiempo de Establecimiento (ms):** Tiempo de entrada a la banda de tolerancia de $\pm 2\%$.

---

## 🚀 Requisitos e Instalación

1. **Prerrequisitos:** .NET 10 SDK y un ESP32 vinculado por Bluetooth (SPP) o USB.
2. **Compilar y Ejecutar:**

```bash
git clone [https://github.com/SandroZ115/robotBalancin.git](https://github.com/SandroZ115/robotBalancin.git)
cd robotBalancin
dotnet build
dotnet run --project RobotSelfBalancing.UI
