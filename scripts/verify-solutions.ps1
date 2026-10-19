$ErrorActionPreference = "Stop"
$runner = "http://localhost:8000"
$root = Join-Path $PSScriptRoot "..\solutions"

$cases = @(
  @{ dir = "hello-world"; stdin = "" },
  @{ dir = "sum-two-numbers"; stdin = "2`n3`n" },
  @{ dir = "reverse-string"; stdin = "hello`n" },
  @{ dir = "fizzbuzz-single"; stdin = "15`n" },
  @{ dir = "palindrome-check"; stdin = "level`n" },
  @{ dir = "array-max"; stdin = "3`n1 5 2`n" },
  @{ dir = "count-vowels"; stdin = "hello`n" },
  @{ dir = "two-sum-count"; stdin = "4`n1 2 3 2`n4`n" }
)

Invoke-RestMethod "$runner/health" | Out-Null

foreach ($c in $cases) {
  $path = Join-Path $root "$($c.dir)\solution.py"
  $source = Get-Content -Raw $path
  $res = Invoke-RestMethod "$runner/execute" -Method POST -ContentType "application/json" -Body (@{
    language = "python3"
    source = $source
    stdin = $c.stdin
    timeoutSeconds = 5
  } | ConvertTo-Json)
  if ($res.exitCode -ne 0 -or $res.compileError) {
    throw "$($c.dir) failed: $($res.compileError) $($res.stderr)"
  }
  Write-Host "[OK] $($c.dir) -> $($res.stdout.Trim())" -ForegroundColor Green
}

Write-Host "All reference solutions executed successfully." -ForegroundColor Cyan
