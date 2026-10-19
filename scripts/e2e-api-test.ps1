$ErrorActionPreference = "Stop"
$base = "http://localhost:5200"
$failures = @()

function Assert($name, $cond, $detail = "") {
  if (-not $cond) { $script:failures += "$name $detail" }
  else { Write-Host "[OK] $name" -ForegroundColor Green }
}

Write-Host "=== CodeIQ API E2E ===" -ForegroundColor Cyan

try {
  Invoke-RestMethod "$base/api/auth/login" -Method POST -ContentType "application/json" -Body '{}' | Out-Null
  Assert "login rejects empty" $false
} catch {
  $code = [int]$_.Exception.Response.StatusCode
  Assert "login rejects empty" ($code -in 400, 401, 422)
}

$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method POST -ContentType "application/json" `
  -Body '{"email":"admin@codeiq.local","password":"Admin@123"}'
Assert "login success" ($null -ne $login.token)
$token = $login.token
$headers = @{ Authorization = "Bearer $token" }

$list = Invoke-RestMethod -Uri "$base/api/admin/assessments" -Headers $headers
Assert "list assessments" ($null -ne $list)

$createBody = @{
  title = "E2E Test $(Get-Date -Format 'HHmmss')"
  description = "Automated test assessment"
  slug = "e2e-test-$(Get-Random -Maximum 99999)"
  durationMinutes = 30
} | ConvertTo-Json
$created = Invoke-RestMethod -Uri "$base/api/admin/assessments" -Method POST -Headers $headers -ContentType "application/json" -Body $createBody
Assert "create assessment" ($created.id)
$id = $created.id

$qBody = @{
  title = "Hello World"
  problemStatement = "Print Hello"
  defaultLanguage = 0
  starterCode = "print('Hello')"
  points = 100
  timeLimitSeconds = 5
  orderIndex = 0
  testCases = @(
    @{ id = $null; input = ""; expectedOutput = "Hello`n"; isSample = $true; orderIndex = 0 }
    @{ id = $null; input = ""; expectedOutput = "Hello`n"; isSample = $false; orderIndex = 1 }
  )
} | ConvertTo-Json -Depth 5
$question = Invoke-RestMethod -Uri "$base/api/admin/assessments/$id/questions" -Method POST -Headers $headers -ContentType "application/json" -Body $qBody
Assert "create question" ($question.id)

$pubBody = @{
  title = $created.title
  description = $created.description
  slug = $created.slug
  durationMinutes = 30
  opensAtUtc = $null
  closesAtUtc = $null
  isPublished = $true
  candidateInstructions = ""
  shuffleQuestions = $false
  allowCodeRun = $true
} | ConvertTo-Json
$published = Invoke-RestMethod -Uri "$base/api/admin/assessments/$id" -Method PUT -Headers $headers -ContentType "application/json" -Body $pubBody
Assert "publish assessment" ($published.isPublished -eq $true)

$slug = $published.slug
$public = Invoke-RestMethod -Uri "$base/api/public/assessments/$slug"
Assert "public assessment" ($public.registrationOpen -eq $true)

$reg = Invoke-RestMethod -Uri "$base/api/public/assessments/$slug/register" -Method POST -ContentType "application/json" `
  -Body '{"fullName":"E2E Candidate","email":"e2e@test.local"}'
Assert "register candidate" ($reg.accessToken)
$access = $reg.accessToken

$session = Invoke-RestMethod -Uri "$base/api/public/sessions/$access"
Assert "session registered" ($session.status -eq 0)

$started = Invoke-RestMethod -Uri "$base/api/public/sessions/$access/start" -Method POST
Assert "start attempt" ($started.status -eq 1)

$qid = $started.questions[0].id
try {
  $runnerOk = (Invoke-RestMethod "http://localhost:8000/health").status -eq "ok"
} catch { $runnerOk = $false }
if ($runnerOk) {
  $run = Invoke-RestMethod -Uri "$base/api/public/sessions/$access/questions/$qid/run" -Method POST -ContentType "application/json" `
    -Body '{"language":0,"sourceCode":"print(\"Hello\")"}'
  Assert "run samples" ($run.passed -ge 1)
  $submit = Invoke-RestMethod -Uri "$base/api/public/sessions/$access/questions/$qid/submit" -Method POST -ContentType "application/json" `
    -Body '{"language":0,"sourceCode":"print(\"Hello\")","finalize":true}'
  Assert "submit question" ($submit.score -gt 0)
} else {
  Write-Host "[SKIP] runner not on :8000" -ForegroundColor Yellow
}

$final = Invoke-RestMethod -Uri "$base/api/public/sessions/$access/finalize" -Method POST
Assert "finalize" ($final.status -eq 2)

$results = Invoke-RestMethod -Uri "$base/api/admin/assessments/$id/results" -Headers $headers
Assert "admin results" ($results.Count -ge 1)

Invoke-RestMethod -Uri "$base/api/admin/assessments/$id" -Method DELETE -Headers $headers | Out-Null
Assert "delete assessment" $true

if ($failures.Count -gt 0) {
  Write-Host "`nFAILED:" -ForegroundColor Red
  $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
  exit 1
}
Write-Host "`nAll API checks passed." -ForegroundColor Cyan
