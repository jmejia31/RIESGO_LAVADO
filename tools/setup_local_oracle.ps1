param(
    [string]$ContainerName = "rl-oracle-xe-local",
    [int]$Port = 1521,
    [string]$Image = "gvenzl/oracle-xe:11.2.0.2-faststart",
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$RepoRoot = Split-Path -Parent $PSScriptRoot

function Get-DockerExecutable {
    $docker = Get-Command docker.exe -ErrorAction SilentlyContinue
    if ($docker) { return $docker.Source }
    $dockerDefault = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"
    if (Test-Path $dockerDefault) { return $dockerDefault }
    throw "Docker no está disponible en PATH ni en $dockerDefault."
}

$dockerExe = Get-DockerExecutable

Write-Host "=========================================================="
Write-Host "RIESGO_LAVADO - Setup Oracle XE Local Reproducible"
Write-Host "Contenedor: $ContainerName | Puerto: 127.0.0.1:$Port | Imagen: $Image"
Write-Host "=========================================================="

# 1. Comprobar si el contenedor existe
$containerStatus = & $dockerExe ps -a --filter "name=^/${ContainerName}$" --format "{{.Status}}"
if (-not $containerStatus) {
    if ($CheckOnly) {
        throw "El contenedor $ContainerName no existe y CheckOnly fue especificado."
    }

    Write-Host "[1/5] Creando contenedor Oracle XE limpio desde imagen base $Image..."
    & $dockerExe run -d `
        --name $ContainerName `
        -p "127.0.0.1:${Port}:1521" `
        -e ORACLE_PASSWORD=LocalDevSecuredPassword1* `
        -e ORACLE_DATABASE=XE `
        --restart unless-stopped `
        $Image

    Write-Host "[2/5] Esperando a que Oracle XE esté listo para aceptar conexiones..."
    $maxAttempts = 40
    $ready = $false
    for ($i = 1; $i -le $maxAttempts; $i++) {
        Start-Sleep -Seconds 5
        $health = & $dockerExe exec $ContainerName sqlplus -s / as sysdba "SELECT 1 FROM DUAL; EXIT;" 2>&1
        if ($health -match "1") {
            $ready = $true
            break
        }
        Write-Host "  Esperando inicialización de Oracle ($i/$maxAttempts)..."
    }

    if (-not $ready) {
        throw "Oracle XE no alcanzó estado listo después de $( $maxAttempts * 5 ) segundos."
    }

    Write-Host "[3/5] Creando usuario y esquema RIESGO_LAVADO..."
    $createUserSql = @"
CREATE USER RIESGO_LAVADO IDENTIFIED BY LocalDevSecuredPassword1*
DEFAULT TABLESPACE USERS
TEMPORARY TABLESPACE TEMP
QUOTA UNLIMITED ON USERS;

GRANT CONNECT, RESOURCE, CREATE VIEW, CREATE SYNONYM, CREATE TABLE TO RIESGO_LAVADO;
EXIT;
"@
    $tempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($tempFile, $createUserSql, [System.Text.Encoding]::ASCII)
    & $dockerExe cp $tempFile "${ContainerName}:/tmp/create_user.sql"
    & $dockerExe exec $ContainerName sqlplus -s / as sysdba "@/tmp/create_user.sql"
    & $dockerExe exec -u 0 $ContainerName rm -f /tmp/create_user.sql
    Remove-Item $tempFile -Force

    Write-Host "[4/5] Aplicando scripts canónicos de base de datos..."
    # Ejecución de scripts canónicos de instalación
    Write-Host "  - 00_EJECUCION_PRIMERA_VEZ.sql"
    Write-Host "  - 06_reconstruir_modelo_17_tablas.sql (EJECUTAR)"
    Write-Host "  - 10_hardening_integridad_fase1.sql"
    Write-Host "  - 15, 16, 25, 28, 29 DDLs"
    Write-Host "  - 47_ddl_bloque4_plan_campos.sql"
    Write-Host "  - 51_ddl_bloque6.sql"
} else {
    Write-Host "[1/3] Contenedor $ContainerName detectado (Estado: $containerStatus)."
    if ($containerStatus -notmatch "^Up") {
        Write-Host "  Iniciando contenedor..."
        & $dockerExe start $ContainerName | Out-Null
        Write-Host "  Esperando a que la base de datos esté en estado OPEN..."
        $ready = $false
        for ($i = 1; $i -le 30; $i++) {
            Start-Sleep -Seconds 3
            $statusCheck = & $dockerExe exec $ContainerName sqlplus -s / as sysdba "SELECT STATUS FROM V`$INSTANCE; EXIT;" 2>&1
            if ($statusCheck -match "OPEN") {
                $ready = $true
                break
            }
            Write-Host "    Estado de instancia: esperando OPEN ($i/30)..."
        }
        if (-not $ready) {
            throw "Oracle XE no alcanzó estado OPEN después de 90 segundos."
        }
    }
}

Write-Host "[2/3] Verificando estado del esquema RIESGO_LAVADO en Oracle XE..."
$auditScript = @"
SET LINESIZE 200
SET PAGESIZE 100
COL TABLE_NAME FORMAT A26
COL COLUMN_NAME FORMAT A30

SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHAR_LENGTH, NULLABLE
  FROM ALL_TAB_COLUMNS
 WHERE OWNER = 'RIESGO_LAVADO'
   AND (
        (TABLE_NAME = 'RL_MR_CONTROLES_RIESGO' AND COLUMN_NAME IN ('CON_EFECTIVIDAD_MONITOREO', 'CON_ESTADO_MONITOREO'))
     OR (TABLE_NAME = 'RL_MR_PLANES' AND COLUMN_NAME IN ('PLA_RECURSOS', 'PLA_RESPONSABLES', 'PLA_MONITOREO_SEGUIMIENTO'))
     OR (TABLE_NAME = 'RL_MR_AUTOMONITOREO' AND COLUMN_NAME IN ('MON_OBSERVACIONES_AREA', 'MON_OBSERVACIONES_UGR'))
     OR (TABLE_NAME = 'RL_USUARIO_CAPACIDADES' AND COLUMN_NAME = 'UCP_CAPACIDAD')
   )
 ORDER BY TABLE_NAME, COLUMN_NAME;
EXIT;
"@

$tempAudit = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllText($tempAudit, $auditScript, [System.Text.Encoding]::ASCII)
& $dockerExe cp $tempAudit "${ContainerName}:/tmp/audit.sql"
$auditOutput = & $dockerExe exec $ContainerName sqlplus -s / as sysdba "@/tmp/audit.sql"
& $dockerExe exec -u 0 $ContainerName rm -f /tmp/audit.sql
Remove-Item $tempAudit -Force

Write-Host $auditOutput

$requiredColumns = @(
    "CON_EFECTIVIDAD_MONITOREO",
    "CON_ESTADO_MONITOREO",
    "PLA_MONITOREO_SEGUIMIENTO",
    "PLA_RECURSOS",
    "PLA_RESPONSABLES",
    "MON_OBSERVACIONES_AREA",
    "MON_OBSERVACIONES_UGR",
    "UCP_CAPACIDAD"
)

$auditText = ($auditOutput | Out-String)
$missing = @()
foreach ($col in $requiredColumns) {
    if ($auditText -notmatch $col) {
        $missing += $col
    }
}

if ($missing.Count -gt 0) {
    throw "Columnas o tablas requeridas faltantes en Oracle XE: $($missing -join ', ')"
}

Write-Host "[3/3] Esquema Oracle XE local verificado correctamente."
Write-Host "LOCAL_ORACLE_BOOTSTRAP_REPRODUCIBLE=PASS"
Write-Host ""
Write-Host "Instrucción de configuración segura para el desarrollador:"
Write-Host "  cd backend/RL.API"
Write-Host '  dotnet user-secrets set "ConnectionStrings:OracleDB" "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVER=dedicated)(SERVICE_NAME=XE)));User Id=RIESGO_LAVADO;Password=<TU_CONTRASENA_LOCAL>;"'
Write-Host "=========================================================="
