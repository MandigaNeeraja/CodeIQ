from fastapi import FastAPI, HTTPException

from app.executor import execute_code
from app.models import ExecuteRequest, ExecuteResponse

app = FastAPI(
    title="CodeIQ Runner",
    description="Isolated code execution service for online assessments",
    version="1.0.0",
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/execute", response_model=ExecuteResponse)
def execute(request: ExecuteRequest) -> ExecuteResponse:
    try:
        return execute_code(
            language=request.language,
            source=request.source,
            stdin=request.stdin,
            timeout_seconds=request.timeoutSeconds,
        )
    except Exception as exc:
        raise HTTPException(status_code=500, detail=str(exc)) from exc
