param(
    [int]$Port = 8080,
    [string]$Root = $PSScriptRoot
)

$resolvedRoot = (Resolve-Path -LiteralPath $Root).Path
$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $Port)
$listener.Start()

Write-Host "Serving $resolvedRoot on port $Port"
Write-Host "Press Ctrl+C to stop."

function Get-ContentType {
    param([string]$Path)

    switch ([System.IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        ".apk" { return "application/vnd.android.package-archive" }
        ".html" { return "text/html; charset=utf-8" }
        ".svg" { return "image/svg+xml" }
        ".txt" { return "text/plain; charset=utf-8" }
        default { return "application/octet-stream" }
    }
}

function Send-Response {
    param(
        [System.Net.Sockets.NetworkStream]$Stream,
        [int]$StatusCode,
        [string]$StatusText,
        [byte[]]$Body,
        [string]$ContentType
    )

    $header = "HTTP/1.1 $StatusCode $StatusText`r`nContent-Type: $ContentType`r`nContent-Length: $($Body.Length)`r`nConnection: close`r`n`r`n"
    $headerBytes = [System.Text.Encoding]::ASCII.GetBytes($header)
    $Stream.Write($headerBytes, 0, $headerBytes.Length)
    if ($Body.Length -gt 0) {
        $Stream.Write($Body, 0, $Body.Length)
    }
}

while ($true) {
    $client = $listener.AcceptTcpClient()

    try {
        $stream = $client.GetStream()
        $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::ASCII, $false, 1024, $true)
        $requestLine = $reader.ReadLine()

        if ([string]::IsNullOrWhiteSpace($requestLine)) {
            continue
        }

        while (($line = $reader.ReadLine()) -ne $null -and $line.Length -gt 0) {
        }

        $parts = $requestLine.Split(" ")
        if ($parts.Count -lt 2 -or $parts[0] -ne "GET") {
            $body = [System.Text.Encoding]::UTF8.GetBytes("Method not allowed")
            Send-Response $stream 405 "Method Not Allowed" $body "text/plain; charset=utf-8"
            continue
        }

        $relativePath = [System.Uri]::UnescapeDataString($parts[1].Split("?")[0]).TrimStart("/")
        if ([string]::IsNullOrWhiteSpace($relativePath)) {
            $relativePath = "index.html"
        }

        if ($relativePath.Contains("..")) {
            $body = [System.Text.Encoding]::UTF8.GetBytes("Bad request")
            Send-Response $stream 400 "Bad Request" $body "text/plain; charset=utf-8"
            continue
        }

        $filePath = Join-Path $resolvedRoot $relativePath
        if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
            $body = [System.Text.Encoding]::UTF8.GetBytes("Not found")
            Send-Response $stream 404 "Not Found" $body "text/plain; charset=utf-8"
            continue
        }

        $body = [System.IO.File]::ReadAllBytes($filePath)
        Send-Response $stream 200 "OK" $body (Get-ContentType $filePath)
    }
    finally {
        $client.Close()
    }
}
