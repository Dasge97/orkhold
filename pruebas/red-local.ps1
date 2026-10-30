# Prueba de red del pueblo (tarea 7.8): un servidor de mundo y dos clientes bot en este PC.
# Necesita Builds/Windows/Orkhold.exe (Unity: CompilarEscritorio.WindowsEnBatch).
# Cada cliente anda en círculos y escribe "BOT <nombre> en x,z | VEO <otro>@x,z" cada 2 s.
# La prueba pasa si cada cliente ve al otro y la posición del otro cambia.
param(
    [int]$Segundos = 25,
    [int]$Puerto = 7777,
    [string]$Servidor = "",          # vacío = arranca uno local; si no, "host:puerto" de un servidor existente
    [string]$Exe = "$PSScriptRoot\..\Builds\Windows\Orkhold.exe",
    [string]$Registros = "$PSScriptRoot\..\Revision\red"
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Exe)) { throw "No existe $Exe (compila con CompilarEscritorio.WindowsEnBatch)" }
New-Item -ItemType Directory -Force $Registros | Out-Null
$procesos = @()
$mundo = $Servidor
if (-not $mundo) {
    $mundo = "127.0.0.1:$Puerto"
    $procesos += Start-Process $Exe -PassThru -ArgumentList @("-batchmode", "-nographics", "-servidor", "-puerto", "$Puerto", "-logFile", "$Registros\servidor.log")
    Start-Sleep -Seconds 6
}
foreach ($n in @("Ana", "Bruno")) {
    $procesos += Start-Process $Exe -PassThru -ArgumentList @("-batchmode", "-nographics", "-bot", "-nombre", $n, "-mundo", $mundo, "-logFile", "$Registros\cliente-$n.log")
}
Start-Sleep -Seconds $Segundos
$procesos | ForEach-Object { if (-not $_.HasExited) { Stop-Process -Id $_.Id -Force } }

function Revisar($yo, $otro) {
    $lineas = Select-String -Path "$Registros\cliente-$yo.log" -Pattern "BOT $yo en" | ForEach-Object { $_.Line }
    $vistas = @($lineas | ForEach-Object { if ($_ -match "VEO .*$otro@([-\d\.,]+)") { $Matches[1] } } | Where-Object { $_ })
    $distintas = @($vistas | Sort-Object -Unique).Count
    Write-Host "$yo ve a $otro en $($vistas.Count) de $($lineas.Count) informes, en $distintas posiciones distintas"
    return ($vistas.Count -ge 3 -and $distintas -ge 3)
}
$ok = (Revisar "Ana" "Bruno") -and (Revisar "Bruno" "Ana")
if ($ok) { Write-Host "RED: OK" } else { Write-Host "RED: FALLO (registros en $Registros)"; exit 1 }
