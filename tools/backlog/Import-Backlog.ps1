<#
Importa el backlog de NexTalent (backlog.csv) como issues de GitHub y las agrega
a un GitHub Project con los campos Epica, Fase, Prioridad y Servicio.

Usa IDs de GraphQL directamente para gastar muy poco cupo de la API.

Uso (desde la raiz del repo):
  .\tools\backlog\Import-Backlog.ps1                           prueba en seco, no crea nada
  .\tools\backlog\Import-Backlog.ps1 -Execute -Only HU-01.01   procesa solo una historia
  .\tools\backlog\Import-Backlog.ps1 -Execute                  procesa todas

Se puede repetir sin duplicar: si una historia ya existe, solo se asegura de que
este en el proyecto con sus campos.

IMPORTANTE: este archivo debe contener solo caracteres ASCII (sin acentos) para que
funcione igual en Windows PowerShell 5.1 y en PowerShell 7. Los textos con acentos
vienen del CSV o se construyen con [char].
#>
[CmdletBinding()]
param(
    [string]$Repo = 'AngelMontws/NexTalent',
    [string]$Owner = 'AngelMontws',
    [string]$ProjectTitle = 'NexTalent',
    [string]$CsvPath = (Join-Path $PSScriptRoot 'backlog.csv'),
    [string[]]$Only,
    [switch]$Execute
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$EAcute = [char]0x00C9
$OAcute = [char]0x00F3

function Invoke-Gh {
    $salida = & gh @args
    if ($LASTEXITCODE -ne 0) {
        throw ('Fallo el comando: gh ' + ($args -join ' '))
    }
    return ($salida -join "`n")
}

function ConvertTo-UnaLinea([string]$texto) {
    return ($texto -replace '\s+', ' ').Trim()
}

function Invoke-GraphQL {
    param(
        [string]$Consulta,
        [hashtable]$Variables = @{},
        [hashtable]$Enteros = @{}
    )
    $argumentos = @('api', 'graphql', '-f', ('query=' + (ConvertTo-UnaLinea $Consulta)))
    foreach ($k in $Variables.Keys) { $argumentos += @('-f', ($k + '=' + $Variables[$k])) }
    foreach ($k in $Enteros.Keys) { $argumentos += @('-F', ($k + '=' + $Enteros[$k])) }
    $json = Invoke-Gh @argumentos
    return ($json | ConvertFrom-Json)
}

function Get-OpcionEpica($h) {
    return (($h.epica_id + ' - ' + $h.epica_nombre) -replace ',', '')
}

# ---------- Leer el CSV ----------
if (-not (Test-Path -LiteralPath $CsvPath)) { throw "No encuentro el CSV: $CsvPath" }
$todas = @(Import-Csv -LiteralPath $CsvPath -Encoding UTF8 -Delimiter ',')
$historias = $todas
if ($Only) { $historias = @($todas | Where-Object { $Only -contains $_.hu_id }) }
if ($historias.Count -eq 0) { throw 'No hay historias que procesar. Revisa -Only.' }
$total = $historias.Count

Write-Host ''
Write-Host "Repo: $Repo | Proyecto: $ProjectTitle | Historias: $total"
foreach ($h in $historias) {
    Write-Host ('{0}  [{1}]  [{2}]  [{3}]  {4}' -f $h.hu_id, $h.prioridad, $h.fase, $h.servicio, $h.hu_nombre)
}
Write-Host ''

if (-not $Execute) {
    Write-Host 'PRUEBA EN SECO: no se creo nada. Agrega -Execute para crear de verdad.'
    return
}

# ---------- 0. Revisar el cupo de la API ----------
$rl = (Invoke-Gh api rate_limit) | ConvertFrom-Json
$restantes = $rl.resources.graphql.remaining
if ($restantes -lt 300) {
    $reinicio = [DateTimeOffset]::FromUnixTimeSeconds([int64]$rl.resources.graphql.reset).LocalDateTime
    throw "Cupo de la API casi agotado ($restantes puntos). Se restablece a las $reinicio. Espera y vuelve a correr el script."
}
Write-Host "Puntos de API disponibles: $restantes"

# ---------- Opciones de los campos (siempre sobre todo el CSV) ----------
$opcionesEpica = @($todas | ForEach-Object { Get-OpcionEpica $_ } | Select-Object -Unique)
$opcionesFase = @($todas | ForEach-Object { $_.fase } | Select-Object -Unique | Sort-Object)
$opcionesPrioridad = @(@('Must', 'Should', 'Could') | Where-Object { $todas.prioridad -contains $_ })
$opcionesServicio = @($todas | ForEach-Object { $_.servicio } | Select-Object -Unique | Sort-Object)

$campos = [ordered]@{
    'Epica'     = $opcionesEpica
    'Fase'      = $opcionesFase
    'Prioridad' = $opcionesPrioridad
    'Servicio'  = $opcionesServicio
}

# ---------- 1. Etiquetas ----------
Write-Host 'Creando etiquetas...'
Invoke-Gh label create 'historia-de-usuario' --repo $Repo --color 'C5DEF5' --description 'Historia de usuario del backlog' --force | Out-Null
foreach ($e in ($todas | Select-Object epica_id, epica_nombre -Unique)) {
    Invoke-Gh label create $e.epica_id --repo $Repo --color '5319E7' --description $e.epica_nombre --force | Out-Null
}
$colorPrioridad = @{ 'Must' = 'B60205'; 'Should' = 'FBCA04'; 'Could' = '0E8A16' }
foreach ($p in $opcionesPrioridad) {
    Invoke-Gh label create ('prio: ' + $p) --repo $Repo --color $colorPrioridad[$p] --description ('MoSCoW: ' + $p) --force | Out-Null
}

# ---------- 2. Proyecto ----------
$lista = (Invoke-Gh project list --owner $Owner --format json) | ConvertFrom-Json
$proyecto = $lista.projects | Where-Object { $_.title -eq $ProjectTitle } | Select-Object -First 1
if (-not $proyecto) {
    $proyecto = (Invoke-Gh project create --owner $Owner --title $ProjectTitle --format json) | ConvertFrom-Json
    Write-Host "Proyecto creado: $($proyecto.url)"
}
else {
    Write-Host "Usando el proyecto existente: $($proyecto.url)"
}
$n = $proyecto.number

$consultaProyecto = 'query($login:String!,$number:Int!){user(login:$login){projectV2(number:$number){id}}}'
$datosProyecto = Invoke-GraphQL $consultaProyecto @{ login = $Owner } @{ number = $n }
$projectId = $datosProyecto.data.user.projectV2.id
if (-not $projectId) { throw 'No pude obtener el id del proyecto.' }

try {
    Invoke-Gh project link $n --owner $Owner --repo $Repo | Out-Null
}
catch {
    Write-Warning 'No se pudo vincular el proyecto al repo (o ya estaba vinculado). Se puede hacer desde la web.'
}

# ---------- 3. Campos del proyecto (con los ids de sus opciones) ----------
$consultaCampos = 'query($project:ID!){node(id:$project){... on ProjectV2{fields(first:50){nodes{... on ProjectV2SingleSelectField{id name options{id name}}}}}}}'

function Get-MapaCampos {
    $resp = Invoke-GraphQL $consultaCampos @{ project = $projectId }
    $mapa = @{}
    foreach ($nodo in $resp.data.node.fields.nodes) {
        if ($nodo.name) {
            $opciones = @{}
            foreach ($op in $nodo.options) { $opciones[$op.name] = $op.id }
            $mapa[$nodo.name] = @{ id = $nodo.id; opciones = $opciones }
        }
    }
    return $mapa
}

$mapa = Get-MapaCampos
$creeCampos = $false
foreach ($nombre in $campos.Keys) {
    if (-not $mapa.ContainsKey($nombre)) {
        Write-Host "Creando el campo $nombre..."
        Invoke-Gh project field-create $n --owner $Owner --name $nombre --data-type SINGLE_SELECT --single-select-options ($campos[$nombre] -join ',') | Out-Null
        $creeCampos = $true
    }
}
if ($creeCampos) { $mapa = Get-MapaCampos }

# ---------- 4. Issues ya existentes (para no duplicar) ----------
$issuesJson = Invoke-Gh issue list --repo $Repo --state all --limit 1000 --json 'id,title,url'
$issues = @()
if ($issuesJson) { $issues = @($issuesJson | ConvertFrom-Json) }
$urlPorHu = @{}
$idPorHu = @{}
foreach ($i in $issues) {
    if ($i.title -match '^(HU-\d{2}\.\d{2})') {
        $urlPorHu[$Matches[1]] = $i.url
        $idPorHu[$Matches[1]] = $i.id
    }
}

# ---------- 5. Mutaciones de GraphQL ----------
$mutAgregar = 'mutation($project:ID!,$content:ID!){addProjectV2ItemById(input:{projectId:$project,contentId:$content}){item{id}}}'
$mutActualizar = 'mutation($project:ID!,$item:ID!,$f1:ID!,$o1:String!,$f2:ID!,$o2:String!,$f3:ID!,$o3:String!,$f4:ID!,$o4:String!){' +
    'a:updateProjectV2ItemFieldValue(input:{projectId:$project,itemId:$item,fieldId:$f1,value:{singleSelectOptionId:$o1}}){projectV2Item{id}}' +
    'b:updateProjectV2ItemFieldValue(input:{projectId:$project,itemId:$item,fieldId:$f2,value:{singleSelectOptionId:$o2}}){projectV2Item{id}}' +
    'c:updateProjectV2ItemFieldValue(input:{projectId:$project,itemId:$item,fieldId:$f3,value:{singleSelectOptionId:$o3}}){projectV2Item{id}}' +
    'd:updateProjectV2ItemFieldValue(input:{projectId:$project,itemId:$item,fieldId:$f4,value:{singleSelectOptionId:$o4}}){projectV2Item{id}}}'

# ---------- 6. Crear issues, agregarlas al proyecto y llenar campos ----------
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) 'nextalent_issue.md'
$utf8SinBom = New-Object System.Text.UTF8Encoding($false)
$contador = 0

foreach ($h in $historias) {
    $contador++
    $url = $urlPorHu[$h.hu_id]
    $contentId = $idPorHu[$h.hu_id]

    if (-not $url) {
        $titulo = ($h.hu_id + ' - ' + $h.hu_nombre) -replace '"', "'"
        $criterios = (($h.criterios -split "\r?\n") | Where-Object { $_.Trim() } | ForEach-Object { '- [ ] ' + ($_ -replace '^\s*\d+\.\s*', '') }) -join "`n"
        $cuerpo = @(
            "**${EAcute}pica:** $($h.epica_id) - $($h.epica_nombre)",
            "**CDA que cubre:** $($h.cda)",
            "**Servicio responsable:** $($h.servicio)",
            "**Fase del plan:** $($h.fase)",
            "**Prioridad (MoSCoW):** $($h.prioridad)",
            '',
            '## Historia',
            $h.hu_descripcion,
            '',
            "## Criterios de aceptaci${OAcute}n",
            $criterios,
            '',
            '## Anexos',
            $h.anexos
        ) -join "`n"
        [System.IO.File]::WriteAllText($tmp, $cuerpo, $utf8SinBom)

        $salida = Invoke-Gh issue create --repo $Repo --title $titulo --body-file $tmp --label 'historia-de-usuario' --label $h.epica_id --label ('prio: ' + $h.prioridad)
        $url = ($salida -split "`n" | Where-Object { $_ -match '^https://' } | Select-Object -Last 1).Trim()
        $contentId = (Invoke-Gh issue view $url --json id --jq '.id').Trim()
        Write-Host "[$contador/$total] Creada: $titulo"
        Start-Sleep -Seconds 1
    }
    else {
        Write-Host "[$contador/$total] Ya existia: $($h.hu_id)"
        Start-Sleep -Milliseconds 300
    }

    # Agregar al proyecto (si ya estaba, devuelve el mismo item)
    $resp1 = Invoke-GraphQL $mutAgregar @{ project = $projectId; content = $contentId }
    $itemId = $resp1.data.addProjectV2ItemById.item.id
    if (-not $itemId) { throw "No pude agregar $($h.hu_id) al proyecto." }

    # Llenar los 4 campos en una sola llamada
    $valores = [ordered]@{
        'Epica'     = (Get-OpcionEpica $h)
        'Fase'      = $h.fase
        'Prioridad' = $h.prioridad
        'Servicio'  = $h.servicio
    }
    $vars = @{ project = $projectId; item = $itemId }
    $k = 0
    foreach ($campo in $valores.Keys) {
        $k++
        $idOpcion = $mapa[$campo].opciones[$valores[$campo]]
        if (-not $idOpcion) { throw "No existe la opcion '$($valores[$campo])' en el campo $campo." }
        $vars["f$k"] = $mapa[$campo].id
        $vars["o$k"] = $idOpcion
    }
    Invoke-GraphQL $mutActualizar $vars | Out-Null
}

Write-Host ''
Write-Host "Listo. Historias procesadas: $total"
Write-Host "Proyecto: $($proyecto.url)"