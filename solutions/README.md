# CodeIQ reference solutions

Python 3 reference implementations for the seeded question bank. Use these to verify the runner and API end-to-end.

## Quick test (runner)

From repo root, with the runner on `http://localhost:8000`:

```powershell
$body = @{
  language = "python3"
  source = (Get-Content -Raw ".\solutions\hello-world\solution.py")
  stdin = ""
  timeoutSeconds = 5
} | ConvertTo-Json
Invoke-RestMethod "http://localhost:8000/execute" -Method POST -ContentType "application/json" -Body $body
```

Each problem folder may include `sample-*.in` files for manual stdin testing.

## Problems

| Slug | Folder |
|------|--------|
| hello-world | `hello-world/` |
| sum-two-numbers | `sum-two-numbers/` |
| reverse-string | `reverse-string/` |
| fizzbuzz-single | `fizzbuzz-single/` |
| palindrome-check | `palindrome-check/` |
| array-max | `array-max/` |
| count-vowels | `count-vowels/` |
| two-sum-count | `two-sum-count/` |
