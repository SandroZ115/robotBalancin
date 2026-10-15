CREATE DATABASE robotBalancinDB;

USE robotBalancinDB;

CREATE TABLE Sesion (
    SesionId INT IDENTITY(1,1) PRIMARY KEY,
    FechaInicio DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    FechaFin DATETIME2 NULL,
    FirmwareVer VARCHAR(20) NULL,
    Kp DECIMAL(9,4) NULL,
    Ki DECIMAL(9,4) NULL,
    Kd DECIMAL(9,4) NULL,
    Alpha DECIMAL(5,4) NULL,
    FrecControlHz SMALLINT NULL,
    Observaciones NVARCHAR(MAX) NULL
);
GO

CREATE TABLE Telemetria (
    TelemetriaId BIGINT IDENTITY(1,1) PRIMARY KEY,
    SesionId INT NOT NULL,
    Seq INT NOT NULL,
    TiempoMs BIGINT NOT NULL,
    PitchDeg REAL NOT NULL,
    GyroDps REAL NOT NULL,
    U REAL NOT NULL,
    PwmIzq SMALLINT NOT NULL,
    PwmDer SMALLINT NOT NULL,
    EncIzq INT NOT NULL,
    EncDer INT NOT NULL,
    VBat REAL NOT NULL,
    Estado TINYINT NOT NULL,
    
    CONSTRAINT FK_Telemetria_Sesion FOREIGN KEY (SesionId) REFERENCES Sesion(SesionId) ON DELETE CASCADE,
    CONSTRAINT UQ_Tel UNIQUE (SesionId, Seq)
);
GO

CREATE INDEX IX_Tel_Sesion_Tiempo ON Telemetria (SesionId, TiempoMs);
GO

CREATE OR ALTER VIEW vw_ResumenPerdidas AS
SELECT 
    SesionId, 
    COUNT(*) AS Recibidos, 
    (MAX(Seq) - MIN(Seq) + 1) AS Esperados,
    (MAX(Seq) - MIN(Seq) + 1) - COUNT(*) AS Perdidos,
    CAST(100.0 * (1 - COUNT(*) * 1.0 / NULLIF((MAX(Seq) - MIN(Seq) + 1), 0)) AS DECIMAL(5,2)) AS PerdidaPct
FROM Telemetria 
GROUP BY SesionId;
GO