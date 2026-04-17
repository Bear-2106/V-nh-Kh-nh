param(
    [Parameter(Mandatory = $true)]
    [string]$Data,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$version = 4
$size = 4 * $version + 17
$dataCodewordCount = 80
$eccCodewordCount = 20

function Add-Bits {
    param(
        [System.Collections.Generic.List[bool]]$Bits,
        [int]$Value,
        [int]$Count
    )

    for ($i = $Count - 1; $i -ge 0; $i--) {
        $Bits.Add((($Value -shr $i) -band 1) -ne 0)
    }
}

function Build-GfTables {
    $exp = @(0) * 512
    $log = @(0) * 256
    $x = 1

    for ($i = 0; $i -lt 255; $i++) {
        $exp[$i] = $x
        $log[$x] = $i
        $x = $x -shl 1
        if (($x -band 0x100) -ne 0) {
            $x = $x -bxor 0x11D
        }
    }

    for ($i = 255; $i -lt 512; $i++) {
        $exp[$i] = $exp[$i - 255]
    }

    return @{
        Exp = $exp
        Log = $log
    }
}

function Gf-Multiply {
    param(
        [int]$A,
        [int]$B,
        [int[]]$Exp,
        [int[]]$Log
    )

    if ($A -eq 0 -or $B -eq 0) {
        return 0
    }

    return $Exp[$Log[$A] + $Log[$B]]
}

function New-RsGenerator {
    param(
        [int]$Degree,
        [int[]]$Exp,
        [int[]]$Log
    )

    $generator = @(1)

    for ($i = 0; $i -lt $Degree; $i++) {
        $next = @(0) * ($generator.Count + 1)
        $root = $Exp[$i]

        for ($j = 0; $j -lt $generator.Count; $j++) {
            $next[$j] = $next[$j] -bxor $generator[$j]
            $next[$j + 1] = $next[$j + 1] -bxor (Gf-Multiply $generator[$j] $root $Exp $Log)
        }

        $generator = $next
    }

    return $generator
}

function New-RsRemainder {
    param(
        [int[]]$DataCodewords,
        [int[]]$Generator,
        [int]$Degree,
        [int[]]$Exp,
        [int[]]$Log
    )

    $remainder = @(0) * $Degree

    foreach ($dataByte in $DataCodewords) {
        $factor = $dataByte -bxor $remainder[0]

        for ($i = 0; $i -lt $Degree - 1; $i++) {
            $remainder[$i] = $remainder[$i + 1]
        }
        $remainder[$Degree - 1] = 0

        for ($i = 0; $i -lt $Degree; $i++) {
            $remainder[$i] = $remainder[$i] -bxor (Gf-Multiply $Generator[$i + 1] $factor $Exp $Log)
        }
    }

    return $remainder
}

function Get-BitLength {
    param([int]$Value)

    $length = 0
    while ($Value -ne 0) {
        $length++
        $Value = $Value -shr 1
    }

    return $length
}

function Get-Bit {
    param(
        [int]$Value,
        [int]$Index
    )

    return (($Value -shr $Index) -band 1) -ne 0
}

$payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($Data)
if ($payloadBytes.Length -gt 53) {
    throw "The QR payload is too long for the built-in Version 4-L encoder."
}

$bits = [System.Collections.Generic.List[bool]]::new()
Add-Bits $bits 0x4 4
Add-Bits $bits $payloadBytes.Length 8
foreach ($payloadByte in $payloadBytes) {
    Add-Bits $bits $payloadByte 8
}

$dataCapacityBits = $dataCodewordCount * 8
$terminatorBits = [Math]::Min(4, $dataCapacityBits - $bits.Count)
Add-Bits $bits 0 $terminatorBits

while (($bits.Count % 8) -ne 0) {
    $bits.Add($false)
}

$dataCodewords = [System.Collections.Generic.List[int]]::new()
for ($i = 0; $i -lt $bits.Count; $i += 8) {
    $value = 0
    for ($j = 0; $j -lt 8; $j++) {
        $value = ($value -shl 1) -bor ($(if ($bits[$i + $j]) { 1 } else { 0 }))
    }
    $dataCodewords.Add($value)
}

$padBytes = @(0xEC, 0x11)
$padIndex = 0
while ($dataCodewords.Count -lt $dataCodewordCount) {
    $dataCodewords.Add($padBytes[$padIndex % 2])
    $padIndex++
}

$gf = Build-GfTables
$generator = New-RsGenerator $eccCodewordCount $gf.Exp $gf.Log
$ecc = New-RsRemainder $dataCodewords.ToArray() $generator $eccCodewordCount $gf.Exp $gf.Log
$codewords = @($dataCodewords.ToArray()) + @($ecc)

$modules = New-Object 'bool[,]' $size, $size
$isFunction = New-Object 'bool[,]' $size, $size

function Set-Module {
    param(
        [int]$X,
        [int]$Y,
        [bool]$Dark,
        [bool]$Function = $true
    )

    if ($X -lt 0 -or $Y -lt 0 -or $X -ge $script:size -or $Y -ge $script:size) {
        return
    }

    $script:modules[$Y, $X] = $Dark
    if ($Function) {
        $script:isFunction[$Y, $X] = $true
    }
}

function Reserve-Module {
    param(
        [int]$X,
        [int]$Y
    )

    if ($X -lt 0 -or $Y -lt 0 -or $X -ge $script:size -or $Y -ge $script:size) {
        return
    }

    $script:isFunction[$Y, $X] = $true
}

function Draw-Finder {
    param(
        [int]$X,
        [int]$Y
    )

    for ($dy = -1; $dy -le 7; $dy++) {
        for ($dx = -1; $dx -le 7; $dx++) {
            $xx = $X + $dx
            $yy = $Y + $dy
            $isDark =
                $dx -ge 0 -and $dx -le 6 -and
                $dy -ge 0 -and $dy -le 6 -and
                ($dx -eq 0 -or $dx -eq 6 -or $dy -eq 0 -or $dy -eq 6 -or
                    ($dx -ge 2 -and $dx -le 4 -and $dy -ge 2 -and $dy -le 4))

            Set-Module $xx $yy $isDark $true
        }
    }
}

function Draw-Alignment {
    param(
        [int]$CenterX,
        [int]$CenterY
    )

    for ($dy = -2; $dy -le 2; $dy++) {
        for ($dx = -2; $dx -le 2; $dx++) {
            $distance = [Math]::Max([Math]::Abs($dx), [Math]::Abs($dy))
            Set-Module ($CenterX + $dx) ($CenterY + $dy) ($distance -ne 1) $true
        }
    }
}

Draw-Finder 0 0
Draw-Finder ($size - 7) 0
Draw-Finder 0 ($size - 7)
Draw-Alignment 26 26

for ($i = 8; $i -le $size - 9; $i++) {
    Set-Module $i 6 (($i % 2) -eq 0) $true
    Set-Module 6 $i (($i % 2) -eq 0) $true
}

for ($i = 0; $i -le 8; $i++) {
    if ($i -ne 6) {
        Reserve-Module 8 $i
        Reserve-Module $i 8
    }
}

for ($i = $size - 8; $i -lt $size; $i++) {
    Reserve-Module $i 8
    Reserve-Module 8 $i
}

Set-Module 8 ($size - 8) $true $true

$dataBits = [System.Collections.Generic.List[bool]]::new()
foreach ($codeword in $codewords) {
    Add-Bits $dataBits $codeword 8
}

$bitIndex = 0
$upward = $true
$right = $size - 1
while ($right -ge 1) {
    if ($right -eq 6) {
        $right--
    }

    for ($vertical = 0; $vertical -lt $size; $vertical++) {
        $y = if ($upward) { $size - 1 - $vertical } else { $vertical }

        for ($columnOffset = 0; $columnOffset -lt 2; $columnOffset++) {
            $x = $right - $columnOffset
            if (-not $isFunction[$y, $x]) {
                $bit = $false
                if ($bitIndex -lt $dataBits.Count) {
                    $bit = $dataBits[$bitIndex]
                }

                if ((($x + $y) % 2) -eq 0) {
                    $bit = -not $bit
                }

                $modules[$y, $x] = $bit
                $bitIndex++
            }
        }
    }

    $upward = -not $upward
    $right -= 2
}

$formatData = 1 -shl 3
$formatRemainder = $formatData -shl 10
while ((Get-BitLength $formatRemainder) -ge 11) {
    $shift = (Get-BitLength $formatRemainder) - 11
    $formatRemainder = $formatRemainder -bxor (0x537 -shl $shift)
}
$formatBits = (($formatData -shl 10) -bor $formatRemainder) -bxor 0x5412

for ($i = 0; $i -le 5; $i++) {
    Set-Module 8 $i (Get-Bit $formatBits $i) $true
}
Set-Module 8 7 (Get-Bit $formatBits 6) $true
Set-Module 8 8 (Get-Bit $formatBits 7) $true
Set-Module 7 8 (Get-Bit $formatBits 8) $true
for ($i = 9; $i -le 14; $i++) {
    Set-Module (14 - $i) 8 (Get-Bit $formatBits $i) $true
}
for ($i = 0; $i -le 7; $i++) {
    Set-Module ($size - 1 - $i) 8 (Get-Bit $formatBits $i) $true
}
for ($i = 8; $i -le 14; $i++) {
    Set-Module 8 ($size - 15 + $i) (Get-Bit $formatBits $i) $true
}
Set-Module 8 ($size - 8) $true $true

$quietZone = 4
$dimension = $size + $quietZone * 2
$escapedData = [System.Security.SecurityElement]::Escape($Data)
$svg = [System.Text.StringBuilder]::new()
[void]$svg.AppendLine("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 $dimension $dimension' width='512' height='512' shape-rendering='crispEdges'>")
[void]$svg.AppendLine("  <title>$escapedData</title>")
[void]$svg.AppendLine("  <rect width='100%' height='100%' fill='#ffffff'/>")
[void]$svg.AppendLine("  <path fill='#000000' d='")

for ($y = 0; $y -lt $size; $y++) {
    for ($x = 0; $x -lt $size; $x++) {
        if ($modules[$y, $x]) {
            $drawX = $x + $quietZone
            $drawY = $y + $quietZone
            [void]$svg.Append("M$drawX,$drawY h1 v1 h-1 z ")
        }
    }
}

[void]$svg.AppendLine("'/>")
[void]$svg.AppendLine("</svg>")

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
}

[System.IO.File]::WriteAllText($OutputPath, $svg.ToString(), [System.Text.Encoding]::UTF8)
