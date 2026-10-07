# CodeIQ Runner

Docker image that compiles and runs candidate code in an isolated container.

**Supported languages:** `python3`, `cpp17`, `c`, `javascript`

---

## Project structure

```
codeiq-runner/
├── app/
│   ├── main.py        # FastAPI app — /health and /execute
│   ├── models.py      # Request/response schemas
│   └── executor.py    # Runs code per language
├── Dockerfile         # Builds the image for Azure ACR
├── docker-compose.yml # Local testing
├── requirements.txt
└── postman-examples.json
```

---

## API

### GET /health

Returns `{ "status": "ok" }`

### POST /execute

**Request:**

```json
{
  "language": "python3",
  "source": "print('hello')",
  "stdin": "",
  "timeoutSeconds": 5
}
```

**Response:**

```json
{
  "stdout": "hello\n",
  "stderr": "",
  "exitCode": 0,
  "timeMs": 42,
  "compileError": null
}
```

| Field | Values |
|-------|--------|
| `language` | `python3`, `cpp17`, `c`, `javascript` |
| `timeoutSeconds` | 1–30 (default 5) |

---

## Run locally (without Docker)

```powershell
cd C:\Users\NM162673\Desktop\codeiq-runner

python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt

# You also need gcc, g++, and node installed on your machine
uvicorn app.main:app --host 0.0.0.0 --port 8080
```

Test: http://localhost:8080/health

---

## Run locally (with Docker — recommended)

```powershell
cd C:\Users\NM162673\Desktop\codeiq-runner

docker compose up --build
```

Test in Postman:

- **GET** `http://localhost:8080/health`
- **POST** `http://localhost:8080/execute` with JSON body (see postman-examples.json)

---

## Build and push to Azure ACR

Replace with your registry name:

```powershell
# Login to ACR
az acr login --name crcodeiqrunnerdev

# Build and tag
docker build -t crcodeiqrunnerdev.azurecr.io/codeiq-runner:1.0 .

# Push
docker push crcodeiqrunnerdev.azurecr.io/codeiq-runner:1.0
```

Then update **ca-codeiq-runner-dev** Container App to use the new image tag and create a new revision.

---

## Postman quick test (Python sum)

```
POST http://localhost:8080/execute
Content-Type: application/json

{
  "language": "python3",
  "source": "a, b = map(int, input().split())\nprint(a + b)",
  "stdin": "5 7",
  "timeoutSeconds": 5
}
```

Expected: `"stdout": "12\n"`
