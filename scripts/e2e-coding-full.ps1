$ErrorActionPreference = "Stop"
$api = "http://localhost:5200"
$runner = "http://localhost:8000"
$failures = @()

function Assert($name, $cond, $detail = "") {
  if (-not $cond) { $script:failures += "$name $detail" }
  else { Write-Host "[OK] $name" -ForegroundColor Green }
}

Write-Host "=== CodeIQ Full Stack Coding Test ===" -ForegroundColor Cyan

# 1) Runner direct
$health = Invoke-RestMethod "$runner/health"
Assert "runner health" ($health.status -eq "ok")

$runDirect = Invoke-RestMethod "$runner/execute" -Method POST -ContentType "application/json" -Body (@{
  language = "python3"
  source = "print(2 + 3)"
  stdin = ""
  timeoutSeconds = 5
} | ConvertTo-Json)
Assert "runner execute direct" ($runDirect.stdout.Trim() -eq "5")

# 2) Admin auth
$login = Invoke-RestMethod "$api/api/auth/login" -Method POST -ContentType "application/json" `
  -Body '{"email":"admin@codeiq.local","password":"Admin@123"}'
$h = @{ Authorization = "Bearer $($login.token)" }

# 3) Create + publish assessment with bank question
$slug = "coding-e2e-$(Get-Random -Maximum 99999)"
$created = Invoke-RestMethod "$api/api/admin/assessments" -Method POST -Headers $h -ContentType "application/json" -Body (@{
  title = "Coding E2E"
  description = "Full path test"
  slug = $slug
  durationMinutes = 45
  candidateInstructions = "Run samples before submit."
} | ConvertTo-Json)

$bank = Invoke-RestMethod "$api/api/admin/question-bank" -Headers $h
Assert "question bank" ($bank.Count -ge 1)
$helloBank = $bank | Where-Object { $_.slug -eq "hello-world" } | Select-Object -First 1
$twoSumBank = $bank | Where-Object { $_.slug -eq "two-sum-count" } | Select-Object -First 1
Assert "bank hello-world" ($null -ne $helloBank)
Assert "bank two-sum-count" ($null -ne $twoSumBank)

$added = Invoke-RestMethod "$api/api/admin/assessments/$($created.id)/questions/from-bank" -Method POST -Headers $h `
  -ContentType "application/json" -Body (@{ questionBankItemIds = @($helloBank.id, $twoSumBank.id) } | ConvertTo-Json)
Assert "add from bank" ($added.addedQuestions.Count -eq 2)

$published = Invoke-RestMethod "$api/api/admin/assessments/$($created.id)" -Method PUT -Headers $h -ContentType "application/json" -Body (@{
  title = $created.title
  description = $created.description
  slug = $created.slug
  durationMinutes = 45
  opensAtUtc = $null
  closesAtUtc = $null
  isPublished = $true
  candidateInstructions = "Run samples before submit."
  shuffleQuestions = $false
  allowCodeRun = $true
} | ConvertTo-Json)
Assert "published" ($published.isPublished -eq $true)

# 4) Candidate flow
$email = "coding-e2e-$(Get-Random)@test.local"
$reg = Invoke-RestMethod "$api/api/public/assessments/$slug/register" -Method POST -ContentType "application/json" `
  -Body (@{ fullName = "E2E User"; email = $email } | ConvertTo-Json)
$access = $reg.accessToken

$started = Invoke-RestMethod "$api/api/public/sessions/$access/start" -Method POST
Assert "attempt started" ($started.status -eq 1)
Assert "two questions" ($started.questions.Count -eq 2)

$helloQ = $started.questions | Where-Object { $_.title -like "*Hello*" } | Select-Object -First 1
$twoQ = $started.questions | Where-Object { $_.title -like "*Two Sum*" } | Select-Object -First 1
Assert "hello question" ($null -ne $helloQ.id)
Assert "two sum question" ($null -ne $twoQ.id)

# 5) Hello World - run + submit (API -> runner)
$helloCode = "print('Hello')"
$runHello = Invoke-RestMethod "$api/api/public/sessions/$access/questions/$($helloQ.id)/run" -Method POST -ContentType "application/json" `
  -Body (@{ language = 0; sourceCode = $helloCode } | ConvertTo-Json)
Assert "hello run samples passed" ($runHello.passed -eq $runHello.total -and $runHello.total -ge 1)
Assert "hello run has output" ($runHello.results[0].actualOutput -match "Hello")

$subHello = Invoke-RestMethod "$api/api/public/sessions/$access/questions/$($helloQ.id)/submit" -Method POST -ContentType "application/json" `
  -Body (@{ language = 0; sourceCode = $helloCode; finalize = $true } | ConvertTo-Json)
Assert "hello submit tests" ($subHello.passedTests -eq $subHello.totalTests -and $subHello.totalTests -ge 1)

# 6) Two Sum count pairs - correct solution
$twoSumCode = @"
n = int(input())
nums = list(map(int, input().split()))
t = int(input())
seen = {}
count = 0
for x in nums:
    count += seen.get(t - x, 0)
    seen[x] = seen.get(x, 0) + 1
print(count)
"@

$runTwo = Invoke-RestMethod "$api/api/public/sessions/$access/questions/$($twoQ.id)/run" -Method POST -ContentType "application/json" `
  -Body (@{ language = 0; sourceCode = $twoSumCode } | ConvertTo-Json)
Assert "two sum sample run" ($runTwo.passed -eq $runTwo.total -and $runTwo.total -ge 1)

$subTwo = Invoke-RestMethod "$api/api/public/sessions/$access/questions/$($twoQ.id)/submit" -Method POST -ContentType "application/json" `
  -Body (@{ language = 0; sourceCode = $twoSumCode; finalize = $true } | ConvertTo-Json)
Assert "two sum full submit" ($subTwo.passedTests -eq $subTwo.totalTests)

# 7) Wrong code should fail samples
$badRun = Invoke-RestMethod "$api/api/public/sessions/$access/questions/$($helloQ.id)/run" -Method POST -ContentType "application/json" `
  -Body (@{ language = 0; sourceCode = "print('Wrong')" } | ConvertTo-Json)
Assert "bad code fails" ($badRun.passed -lt $badRun.total)

# 8) Finalize + admin results
$done = Invoke-RestMethod "$api/api/public/sessions/$access/finalize" -Method POST
Assert "finalized" ($done.status -eq 2)

$results = Invoke-RestMethod "$api/api/admin/assessments/$($created.id)/results" -Headers $h
Assert "results row" ($results.Count -ge 1)
Assert "results score positive" ($results[0].totalScore -gt 0)

Invoke-RestMethod "$api/api/admin/assessments/$($created.id)" -Method DELETE -Headers $h | Out-Null

if ($failures.Count -gt 0) {
  Write-Host "`nFAILED:" -ForegroundColor Red
  $failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
  exit 1
}

Write-Host "`nAll coding path checks passed (runner -> API -> candidate)." -ForegroundColor Cyan
