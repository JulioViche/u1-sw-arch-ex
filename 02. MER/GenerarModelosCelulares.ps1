[CmdletBinding()]
param(
    [string]$CelularesRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$BanQuitoRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $BanQuitoRoot) {
    $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $CelularesRoot '..\..\..'))
    $BanQuitoRoot = Join-Path $repoRoot 'U1\TG.1\1.4. PAO202651_RESTFUL_DOTNET_CONBDD_NRC36884_GR09'
}

function New-Column([string]$Code, [string]$Type, [bool]$Nullable = $false, [bool]$Identity = $false) {
    [pscustomobject]@{ Code = $Code; Type = $Type; Nullable = $Nullable; Identity = $Identity }
}

function Add-Table([string]$Code, [object[]]$Columns, [string[]]$PrimaryKey, [object[]]$UniqueKeys = @(), [object[]]$ForeignKeys = @()) {
    $script:Tables[$Code] = [pscustomobject]@{
        Code = $Code; Name = $Code; Columns = @($Columns); PrimaryKey = @($PrimaryKey)
        UniqueKeys = @($UniqueKeys); ForeignKeys = @($ForeignKeys)
    }
}

function Label([string]$Code) {
    $words = ($Code -replace '_', ' ').ToLowerInvariant()
    [Globalization.CultureInfo]::GetCultureInfo('es-EC').TextInfo.ToTitleCase($words)
}

function ConceptualType([string]$Type) {
    if ($Type -match '^(INT|INTEGER)') { return 'Numero' }
    if ($Type -match '^(DECIMAL|NUMERIC|MONEY)') { return 'Importe' }
    if ($Type -match '^BIT') { return 'Booleano' }
    if ($Type -match '^VARBINARY') { return 'Binario' }
    if ($Type -match '^DATE$') { return 'Fecha' }
    if ($Type -match '^(DATETIME|DATETIME2|TIMESTAMP)') { return 'Fecha y hora' }
    if ($Type -match 'UNIQUEIDENTIFIER') { return 'Identificador' }
    'Texto'
}

function Set-SymbolPosition($Symbol, [int[]]$Position) {
    $point = $Symbol.GetAttribute('Position')
    $point.X = $Position[0]
    $point.Y = $Position[1]
    $Symbol.SetAttribute('Position', $point) | Out-Null
}

function Get-Views([string]$ModelCode) {
    if ($ModelCode -like 'GR09_Celulares_*') {
        return @([pscustomobject]@{
            Code = '01_Catalogo_Factura'; Name = 'Catalogo y facturacion'
            Entities = @('CELULAR', 'FACTURA'); Relationships = @('FK_FACTURA_CELULAR')
            Positions = @{ CELULAR = @(1000, 0); FACTURA = @(30000, 0) }
        })
    }
    @(
        [pscustomobject]@{
            Code = '01_Clientes_Cuentas'; Name = 'Clientes, cuentas y movimientos'
            Entities = @('CLIENTE', 'CUENTA', 'MOVIMIENTO')
            Relationships = @('FK_CUENTA_CLIENTE', 'FK_MOVIMIENTO_CUENTA')
            Positions = @{ CLIENTE = @(0, 18000); CUENTA = @(-22000, -7000); MOVIMIENTO = @(22000, -7000) }
        }
        [pscustomobject]@{
            Code = '02_Creditos'; Name = 'Clientes, creditos y amortizacion'
            Entities = @('CLIENTE', 'CREDITO', 'AMORTIZACION')
            Relationships = @('FK_CREDITO_CLIENTE', 'FK_AMORTIZACION_CREDITO')
            Positions = @{ CLIENTE = @(-24000, 18000); CREDITO = @(0, 0); AMORTIZACION = @(25000, 0) }
        }
    )
}

function Export-Views($Model, [string]$DiagramCollection, [hashtable]$Objects, [object[]]$Relations,
    [object[]]$Views, [string]$ImagePath, [bool]$Physical) {
    $collection = $Model.$DiagramCollection
    $base = [System.IO.Path]::GetFileNameWithoutExtension($ImagePath)
    $directory = [System.IO.Path]::GetDirectoryName($ImagePath)
    $coveredEntities = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $coveredRelations = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    for ($i = 0; $i -lt $Views.Count; $i++) {
        $view = $Views[$i]
        $diagram = if ($i -eq 0) { $collection.Item(0) } else { $collection.CreateNew() }
        $diagram.Name = $view.Name
        $diagram.Code = $Model.Code + '_' + $view.Code
        $symbols = @{}
        foreach ($entityCode in $view.Entities) {
            if (-not $Objects.ContainsKey($entityCode)) { throw "Missing entity $entityCode in $($view.Name)." }
            $symbol = $diagram.AttachObject($Objects[$entityCode])
            Set-SymbolPosition $symbol $view.Positions[$entityCode]
            $symbols[$entityCode] = $symbol
            [void]$coveredEntities.Add($entityCode)
        }
        foreach ($relation in $Relations) {
            if ($view.Relationships -notcontains $relation.Code) { continue }
            if ($Physical) {
                $from = $relation.ParentTable.Code
                $to = $relation.ChildTable.Code
            } else {
                $from = $relation.Entity1.Code
                $to = $relation.Entity2.Code
            }
            if (-not $symbols.ContainsKey($from) -or -not $symbols.ContainsKey($to)) {
                throw "Relation $($relation.Code) has a missing endpoint in $($view.Name)."
            }
            $diagram.AttachLinkObject($relation) | Out-Null
            [void]$coveredRelations.Add($relation.Code)
        }
        $output = if ($i -eq 0) { $ImagePath } else { Join-Path $directory ($base + '_' + $view.Code + '.png') }
        $diagram.ExportImage($output)
        if (-not (Test-Path -LiteralPath $output) -or (Get-Item -LiteralPath $output).Length -lt 1000) {
            throw "Diagram export failed: $output"
        }
    }
    if ($coveredEntities.Count -ne $Objects.Count -or $coveredRelations.Count -ne $Relations.Count) {
        throw "Views cover $($coveredEntities.Count)/$($Objects.Count) entities and $($coveredRelations.Count)/$($Relations.Count) relations."
    }
}

function New-Model($Application, $Tables, [string]$ModelKind, [string]$DiagramCollection,
    [string]$DiagramKind, [string]$Name, [string]$Code, [string]$Path, [string]$ImagePath,
    [string]$Mode, [string]$Dbms) {
    if (Test-Path -LiteralPath $Path) { throw "Model already exists; preserved: $Path" }
    $meta = $Application.MetaModel.GetMetaClassByPublicName($ModelKind)
    if ($null -eq $meta) { throw "PowerDesigner does not provide $ModelKind." }
    $options = if ($Mode -eq 'Physical') { "|DBMS=$Dbms|Diagram=$DiagramKind" } else { "|Diagram=$DiagramKind" }
    $model = $Application.CreateModel($meta.Kind, $options, 9)
    if ($null -eq $model) { throw "PowerDesigner did not create $Name." }
    $saved = $false
    try {
        $model.Name = $Name
        $model.Code = $Code
        $model.Comment = if ($Mode -eq 'Physical') { 'SQL Server physical model. No cross-database foreign keys.' } else { 'GR09 data model derived from the corresponding SQL Server schema.' }
        if ($Mode -eq 'Physical' -and $model.DBMS.Name -ne $Dbms) { throw "Unexpected DBMS: $($model.DBMS.Name)." }
        $diagram = $model.$DiagramCollection.Item(0)
        $diagram.Name = $Name
        $diagram.Code = $Code + '_ERD'
        $entities = @{}
        $columns = @{}
        foreach ($table in $Tables.Values) {
            if ($Mode -eq 'Physical') {
                $entity = $model.Tables.CreateNew()
                $entity.Name = $table.Name
                $entity.Code = $table.Code
            } else {
                $entity = $model.Entities.CreateNew()
                $entity.Name = $table.Name
                $entity.Code = $table.Code
            }
            $entities[$table.Code] = $entity
            foreach ($column in $table.Columns) {
                if ($Mode -eq 'Physical') {
                    $attribute = $entity.Columns.CreateNew()
                    $attribute.Name = $column.Code
                    $attribute.Code = $column.Code
                    $attribute.DataType = $column.Type
                    if ($column.Identity) { $attribute.Identity = $true }
                    if ($column.Code -in $table.PrimaryKey) { $attribute.Primary = $true }
                } else {
                    $attribute = $entity.Attributes.CreateNew()
                    $attribute.Name = Label $column.Code
                    $attribute.Code = if ($Mode -eq 'Conceptual') { $table.Code + '_' + $column.Code } else { $column.Code }
                    $attribute.DataType = if ($Mode -eq 'Conceptual') { ConceptualType $column.Type } else { $column.Type }
                }
                $attribute.Mandatory = -not $column.Nullable
                $columns[($table.Code + '.' + $column.Code)] = $attribute
            }
            if ($Mode -ne 'Physical') {
                $identifier = $entity.Identifiers.CreateNew()
                $identifier.Name = 'Primary identifier'
                $identifier.Code = 'PK_' + $table.Code
                $identifier.PrimaryIdentifier = $true
                foreach ($columnCode in $table.PrimaryKey) { $identifier.Attributes.Add($columns[($table.Code + '.' + $columnCode)]) }
                foreach ($unique in $table.UniqueKeys) {
                    if (($unique.Columns -join '|') -eq ($table.PrimaryKey -join '|')) { continue }
                    $alternate = $entity.Identifiers.CreateNew()
                    $alternate.Name = 'Alternate identifier ' + $unique.Name
                    $alternate.Code = $unique.Name
                    $alternate.PrimaryIdentifier = $false
                    foreach ($columnCode in $unique.Columns) { $alternate.Attributes.Add($columns[($table.Code + '.' + $columnCode)]) }
                }
            } else {
                foreach ($unique in $table.UniqueKeys) {
                    if (($unique.Columns -join '|') -eq ($table.PrimaryKey -join '|')) { continue }
                    $index = $entity.Indexes.CreateNew()
                    $index.Name = $unique.Name
                    $index.Code = $unique.Name
                    $index.Unique = $true
                    foreach ($columnCode in $unique.Columns) {
                        $indexColumn = $index.IndexColumns.CreateNew()
                        $indexColumn.Column = $columns[($table.Code + '.' + $columnCode)]
                    }
                }
            }
        }
        $relations = [System.Collections.Generic.List[object]]::new()
        foreach ($table in $Tables.Values) {
            foreach ($fk in $table.ForeignKeys) {
                if ($Mode -eq 'Physical') {
                    $relation = $model.References.CreateNew()
                    $relation.Code = $fk.Name
                    $relation.Name = $fk.Name
                    $relation.ParentTable = $entities[$fk.Parent]
                    $relation.ChildTable = $entities[$table.Code]
                    for ($j = 0; $j -lt $fk.Columns.Count; $j++) {
                        $join = if ($j -lt $relation.Joins.Count) { $relation.Joins.Item($j) } else { $relation.Joins.CreateNew() }
                        $join.ParentTableColumn = $columns[($fk.Parent + '.' + $fk.ParentColumns[$j])]
                        $join.ChildTableColumn = $columns[($table.Code + '.' + $fk.Columns[$j])]
                    }
                } else {
                    $relation = $model.Relationships.CreateNew()
                    $relation.Code = $fk.Name
                    $relation.Name = $entities[$fk.Parent].Name + ' - ' + $entities[$table.Code].Name
                    $relation.Entity1 = $entities[$fk.Parent]
                    $relation.Entity2 = $entities[$table.Code]
                    $relation.Entity1ToEntity2RoleMinimumCardinality = '0'
                    $uniqueFk = @($table.UniqueKeys | Where-Object { ($_.Columns -join '|') -eq ($fk.Columns -join '|') }).Count -gt 0
                    $relation.Entity1ToEntity2RoleMaximumCardinality = if ($uniqueFk) { '1' } else { 'n' }
                    $nullable = @($fk.Columns | Where-Object { ($table.Columns | Where-Object Code -eq $_).Nullable }).Count -gt 0
                    $relation.Entity2ToEntity1RoleMinimumCardinality = if ($nullable) { '0' } else { '1' }
                    $relation.Entity2ToEntity1RoleMaximumCardinality = '1'
                }
                $relations.Add($relation)
            }
        }
        $views = Get-Views $Code
        Export-Views $model $DiagramCollection $entities $relations.ToArray() $views $ImagePath ($Mode -eq 'Physical')
        $model.Save($Path)
        $saved = $true
        Write-Output "$Name : $($entities.Count) entities, $($relations.Count) relations"
    } finally {
        if ($saved) { $model.Close() }
    }
}

$script:Tables = [ordered]@{}
Add-Table 'CELULAR' @(
    (New-Column 'ID_CELULAR' 'INT' $false $true), (New-Column 'NOMBRE' 'NVARCHAR(100)'),
    (New-Column 'PRECIO' 'NUMERIC(10,2)'), (New-Column 'ACTIVO' 'BIT'),
    (New-Column 'IMAGEN' 'VARBINARY(MAX)' $true), (New-Column 'IMAGEN_TIPO' 'VARCHAR(40)' $true)
) @('ID_CELULAR')
Add-Table 'FACTURA' @(
    (New-Column 'ID_FACTURA' 'INT' $false $true), (New-Column 'ID_OPERACION' 'UNIQUEIDENTIFIER'),
    (New-Column 'FECHA' 'DATETIME2'), (New-Column 'CEDULA' 'NVARCHAR(100)'),
    (New-Column 'NOMBRE_CLIENTE' 'NVARCHAR(100)'), (New-Column 'ID_CELULAR' 'INT'),
    (New-Column 'NOMBRE_CELULAR' 'NVARCHAR(100)'), (New-Column 'FORMA_PAGO' 'VARCHAR(20)'),
    (New-Column 'PRECIO' 'NUMERIC(10,2)'), (New-Column 'DESCUENTO' 'NUMERIC(10,2)'),
    (New-Column 'TOTAL' 'NUMERIC(10,2)'), (New-Column 'COD_CLIENTE_BANCO' 'INT' $true),
    (New-Column 'COD_CREDITO' 'INT' $true), (New-Column 'NUMERO_CUOTAS' 'INT' $true),
    (New-Column 'ESTADO' 'VARCHAR(12)')
) @('ID_FACTURA') @([pscustomobject]@{ Name = 'UQ_FACTURA_ID_OPERACION'; Columns = @('ID_OPERACION') })
$Tables['FACTURA'].ForeignKeys = @([pscustomobject]@{
    Name = 'FK_FACTURA_CELULAR'; Columns = @('ID_CELULAR'); Parent = 'CELULAR'; ParentColumns = @('ID_CELULAR')
})
$cellTables = $Tables

$cellModels = @(
    @{ Mode = 'Conceptual'; Kind = 'PdCDM.Model'; Diagram = 'ConceptualDiagram'; Collection = 'ConceptualDiagrams'; Folder = '01. CONCEPTUAL'; Stem = 'GR09_Celulares_Conceptual_DotNet'; Extension = '.cdm' },
    @{ Mode = 'Logical'; Kind = 'PdLDM.Model'; Diagram = 'LogicalDiagram'; Collection = 'LogicalDiagrams'; Folder = '02. LOGICO'; Stem = 'GR09_Celulares_Logical_DotNet'; Extension = '.ldm' },
    @{ Mode = 'Physical'; Kind = 'PdPDM.Model'; Diagram = 'PhysicalDiagram'; Collection = 'PhysicalDiagrams'; Folder = '03. FISICO'; Stem = 'GR09_Celulares_Physical_DotNet_SQLServer'; Extension = '.pdm' }
)
$bankModels = @(
    @{ Mode = 'Conceptual'; Kind = 'PdCDM.Model'; Diagram = 'ConceptualDiagram'; Collection = 'ConceptualDiagrams'; Folder = '01. CONCEPTUAL'; Stem = 'GR09_BanQuito_Conceptual_DotNet'; Extension = '.cdm' },
    @{ Mode = 'Logical'; Kind = 'PdLDM.Model'; Diagram = 'LogicalDiagram'; Collection = 'LogicalDiagrams'; Folder = '02. LOGICO'; Stem = 'GR09_BanQuito_Logical_DotNet'; Extension = '.ldm' },
    @{ Mode = 'Physical'; Kind = 'PdPDM.Model'; Diagram = 'PhysicalDiagram'; Collection = 'PhysicalDiagrams'; Folder = '03. FISICO'; Stem = 'GR09_BanQuito_Physical_DotNet_SQLServer'; Extension = '.pdm' }
)

$cellFiles = Get-Content -Raw -LiteralPath (Join-Path $CelularesRoot '03. BDD\01_CrearBases.sql')
if ($cellFiles -notmatch 'CREATE TABLE dbo\.CELULAR' -or $cellFiles -notmatch 'CREATE TABLE dbo\.FACTURA') {
    throw 'Cellular SQL schema no longer matches the model definition.'
}
$bankSql = Get-Content -Raw -LiteralPath (Join-Path $BanQuitoRoot '03. BDD\BanQuito_01_CrearBase.sql')
if ($bankSql -notmatch 'CREATE TABLE dbo\.CREDITO' -or $bankSql -notmatch 'CREATE TABLE dbo\.AMORTIZACION') {
    throw 'BanQuito SQL schema no longer matches the model definition.'
}

$script:Tables = [ordered]@{}
Add-Table 'CLIENTE' @(
    (New-Column 'COD_CLIENTE' 'INT'), (New-Column 'CEDULA' 'VARCHAR(100)'),
    (New-Column 'NOMBRE' 'VARCHAR(100)'), (New-Column 'GENERO' 'VARCHAR(1)'),
    (New-Column 'FECHA_NACIMIENTO' 'DATE')
) @('COD_CLIENTE') @([pscustomobject]@{ Name = 'UQ_CLIENTE_CEDULA'; Columns = @('CEDULA') })
Add-Table 'CUENTA' @(
    (New-Column 'NUM_CUENTA' 'VARCHAR(8)'), (New-Column 'COD_CLIENTE' 'INT'),
    (New-Column 'SALDO' 'NUMERIC(10,2)')
) @('NUM_CUENTA')
Add-Table 'MOVIMIENTO' @(
    (New-Column 'COD_MOVIMIENTO' 'INT'), (New-Column 'NUM_CUENTA' 'VARCHAR(8)'),
    (New-Column 'TIPO' 'VARCHAR(3)'), (New-Column 'VALOR' 'NUMERIC(10,2)'),
    (New-Column 'FECHA' 'DATE')
) @('COD_MOVIMIENTO')
Add-Table 'CREDITO' @(
    (New-Column 'COD_CREDITO' 'INT' $false $true), (New-Column 'COD_CLIENTE' 'INT'),
    (New-Column 'REFERENCIA_VENTA' 'UNIQUEIDENTIFIER'), (New-Column 'MONTO' 'NUMERIC(10,2)'),
    (New-Column 'NUMERO_CUOTAS' 'INT'), (New-Column 'TASA_ANUAL' 'DECIMAL(7,5)'),
    (New-Column 'FECHA' 'DATE'), (New-Column 'ESTADO' 'VARCHAR(10)')
) @('COD_CREDITO') @([pscustomobject]@{ Name = 'UQ_CREDITO_REFERENCIA_VENTA'; Columns = @('REFERENCIA_VENTA') })
Add-Table 'AMORTIZACION' @(
    (New-Column 'COD_CREDITO' 'INT'), (New-Column 'NUMERO_CUOTA' 'INT'),
    (New-Column 'FECHA_PAGO' 'DATE'), (New-Column 'VALOR_CUOTA' 'NUMERIC(10,2)'),
    (New-Column 'INTERES_PAGADO' 'NUMERIC(10,2)'), (New-Column 'CAPITAL_PAGADO' 'NUMERIC(10,2)'),
    (New-Column 'SALDO' 'NUMERIC(10,2)')
) @('COD_CREDITO', 'NUMERO_CUOTA')
$Tables['CUENTA'].ForeignKeys = @([pscustomobject]@{ Name = 'FK_CUENTA_CLIENTE'; Columns = @('COD_CLIENTE'); Parent = 'CLIENTE'; ParentColumns = @('COD_CLIENTE') })
$Tables['MOVIMIENTO'].ForeignKeys = @([pscustomobject]@{ Name = 'FK_MOVIMIENTO_CUENTA'; Columns = @('NUM_CUENTA'); Parent = 'CUENTA'; ParentColumns = @('NUM_CUENTA') })
$Tables['CREDITO'].ForeignKeys = @([pscustomobject]@{ Name = 'FK_CREDITO_CLIENTE'; Columns = @('COD_CLIENTE'); Parent = 'CLIENTE'; ParentColumns = @('COD_CLIENTE') })
$Tables['AMORTIZACION'].ForeignKeys = @([pscustomobject]@{ Name = 'FK_AMORTIZACION_CREDITO'; Columns = @('COD_CREDITO'); Parent = 'CREDITO'; ParentColumns = @('COD_CREDITO') })
$bankTables = $Tables

$application = $null
try {
    $application = New-Object -ComObject PowerDesigner.Application
    $application.InteractiveMode = 2
    foreach ($target in @(
        @{ Root = $CelularesRoot; Models = $cellModels; Tables = $cellTables; Name = 'Celulares' },
        @{ Root = $BanQuitoRoot; Models = $bankModels; Tables = $bankTables; Name = 'BanQuito' }
    )) {
        foreach ($spec in $target.Models) {
            $merRoot = Join-Path $target.Root '02. MER'
            $directory = Join-Path $merRoot $spec.Folder
            New-Item -ItemType Directory -Force -Path $directory | Out-Null
            $modelName = "$($target.Name) $($spec.Mode) .NET GR09"
            $path = Join-Path $directory ($spec.Stem + $spec.Extension)
            $imagePath = Join-Path $directory ($spec.Stem + '.png')
            $dbms = if ($spec.Mode -eq 'Physical') { 'Microsoft SQL Server 2008' } else { '' }
            New-Model $application $target.Tables $spec.Kind $spec.Collection $spec.Diagram $modelName $spec.Stem $path $imagePath $spec.Mode $dbms
        }
    }
} finally {
    if ($application) { [void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($application) }
}
