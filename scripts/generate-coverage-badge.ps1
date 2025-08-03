#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory=$true)]
    [string]$SummaryFile,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputFile
)

# Read the coverage summary JSON
$summaryContent = Get-Content $SummaryFile -Raw | ConvertFrom-Json
$coverage = [math]::Round($summaryContent.summary.linecoverage, 1)

# Determine color based on coverage percentage
$color = "red"
if ($coverage -ge 80) {
    $color = "brightgreen"
} elseif ($coverage -ge 60) {
    $color = "yellow"
} elseif ($coverage -ge 40) {
    $color = "orange"
}

# Generate SVG badge
$svgTemplate = @"
<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="104" height="20" role="img" aria-label="coverage: $coverage%">
  <title>coverage: $coverage%</title>
  <linearGradient id="s" x2="0" y2="100%">
    <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
    <stop offset="1" stop-opacity=".1"/>
  </linearGradient>
  <clipPath id="r">
    <rect width="104" height="20" rx="3" fill="#fff"/>
  </clipPath>
  <g clip-path="url(#r)">
    <rect width="61" height="20" fill="#555"/>
    <rect x="61" width="43" height="20" fill="$color"/>
    <rect width="104" height="20" fill="url(#s)"/>
  </g>
  <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" text-rendering="geometricPrecision" font-size="110">
    <text aria-hidden="true" x="315" y="150" fill="#010101" fill-opacity=".3" transform="scale(.1)" textLength="510">coverage</text>
    <text x="315" y="140" transform="scale(.1)" fill="#fff" textLength="510">coverage</text>
    <text aria-hidden="true" x="815" y="150" fill="#010101" fill-opacity=".3" transform="scale(.1)" textLength="330">$coverage%</text>
    <text x="815" y="140" transform="scale(.1)" fill="#fff" textLength="330">$coverage%</text>
  </g>
</svg>
"@

# Write the SVG file
$svgTemplate | Out-File -FilePath $OutputFile -Encoding UTF8
Write-Host "Coverage badge generated: $coverage% -> $OutputFile"