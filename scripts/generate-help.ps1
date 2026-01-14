Param(
    [string]$Configuration = "Release"
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Resolve-Path "$root/.."
$project = Join-Path $repo "src/Proligent.XmlGenerator/Proligent.XmlGenerator.csproj"
$outputDir = Join-Path $repo "build/help"

Write-Output "Building Proligent.XmlGenerator ($Configuration)..."
dotnet build $project -c $Configuration | Out-Default

$docPath = Get-ChildItem -Path (Join-Path $repo "src/Proligent.XmlGenerator/bin/$Configuration") -Recurse -Filter "Proligent.XmlGenerator.xml" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $docPath) {
    throw "Documentation XML not found. Ensure the project built correctly."
}

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
Copy-Item $docPath $outputDir -Force

# Generate a lightweight HTML index from the XML doc comments
[xml]$doc = Get-Content $docPath
$items = @()
foreach ($member in $doc.doc.members.member) {
    $name = $member.name
    $summary = ($member.summary | Out-String).Trim() -replace "\s+", " "
    $items += "<li><code>$name</code> - $summary</li>"
}
$html = @"
<html>
  <head><meta charset="utf-8"><title>Proligent.XmlGenerator API</title></head>
  <body>
    <h1>Proligent.XmlGenerator API</h1>
    <ul>
      $($items -join "`n      ")
    </ul>
  </body>
</html>
"@

Set-Content -Path (Join-Path $outputDir "proligent-xml-generator.html") -Value $html -Encoding UTF8
Write-Output "Generated help docs in $outputDir"

