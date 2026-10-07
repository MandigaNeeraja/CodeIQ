from enum import Enum
from typing import Literal

from pydantic import BaseModel, Field


class Language(str, Enum):
    PYTHON3 = "python3"
    CPP17 = "cpp17"
    C = "c"
    JAVASCRIPT = "javascript"


class ExecuteRequest(BaseModel):
    language: Language
    source: str = Field(..., min_length=1, max_length=65536)
    stdin: str = Field(default="", max_length=65536)
    timeoutSeconds: int = Field(default=5, ge=1, le=30)


class ExecuteResponse(BaseModel):
    stdout: str
    stderr: str
    exitCode: int
    timeMs: int
    compileError: str | None = None
