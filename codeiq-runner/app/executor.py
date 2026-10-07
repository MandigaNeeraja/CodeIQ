import os
import shutil
import subprocess
import tempfile
import time
from pathlib import Path

from app.models import ExecuteResponse, Language

MAX_OUTPUT_BYTES = 65536


def _truncate(text: str) -> str:
    if len(text.encode("utf-8")) <= MAX_OUTPUT_BYTES:
        return text
    return text.encode("utf-8")[:MAX_OUTPUT_BYTES].decode("utf-8", errors="ignore")


def _run_process(
    command: list[str],
    cwd: Path,
    stdin: str,
    timeout_seconds: int,
) -> tuple[str, str, int, int]:
    start = time.perf_counter()
    try:
        result = subprocess.run(
            command,
            cwd=cwd,
            input=stdin,
            capture_output=True,
            text=True,
            timeout=timeout_seconds,
            shell=False,
        )
        elapsed_ms = int((time.perf_counter() - start) * 1000)
        return (
            _truncate(result.stdout),
            _truncate(result.stderr),
            result.returncode,
            elapsed_ms,
        )
    except subprocess.TimeoutExpired as exc:
        elapsed_ms = int((time.perf_counter() - start) * 1000)
        stdout = _truncate(exc.stdout or "")
        stderr = _truncate((exc.stderr or "") + "\nTime limit exceeded")
        return stdout, stderr, 124, elapsed_ms


def _compile_and_run(
    compile_cmd: list[str],
    run_cmd: list[str],
    cwd: Path,
    stdin: str,
    timeout_seconds: int,
) -> ExecuteResponse:
    compile_start = time.perf_counter()
    compile_result = subprocess.run(
        compile_cmd,
        cwd=cwd,
        capture_output=True,
        text=True,
        timeout=timeout_seconds,
        shell=False,
    )
    compile_ms = int((time.perf_counter() - compile_start) * 1000)

    if compile_result.returncode != 0:
        compile_error = _truncate(compile_result.stderr or compile_result.stdout)
        return ExecuteResponse(
            stdout="",
            stderr=compile_error,
            exitCode=compile_result.returncode,
            timeMs=compile_ms,
            compileError=compile_error,
        )

    stdout, stderr, exit_code, run_ms = _run_process(
        run_cmd, cwd, stdin, timeout_seconds
    )
    return ExecuteResponse(
        stdout=stdout,
        stderr=stderr,
        exitCode=exit_code,
        timeMs=compile_ms + run_ms,
        compileError=None,
    )


def execute_code(
    language: Language,
    source: str,
    stdin: str,
    timeout_seconds: int,
) -> ExecuteResponse:
    work_dir = Path(tempfile.mkdtemp(prefix="codeiq-job-"))
    try:
        if language == Language.PYTHON3:
            source_path = work_dir / "main.py"
            source_path.write_text(source, encoding="utf-8")
            stdout, stderr, exit_code, time_ms = _run_process(
                ["python3", str(source_path)],
                work_dir,
                stdin,
                timeout_seconds,
            )
            return ExecuteResponse(
                stdout=stdout,
                stderr=stderr,
                exitCode=exit_code,
                timeMs=time_ms,
                compileError=None,
            )

        if language == Language.CPP17:
            source_path = work_dir / "main.cpp"
            source_path.write_text(source, encoding="utf-8")
            binary = work_dir / "main"
            return _compile_and_run(
                ["g++", "-std=c++17", "-O2", "-o", str(binary), str(source_path)],
                [str(binary)],
                work_dir,
                stdin,
                timeout_seconds,
            )

        if language == Language.C:
            source_path = work_dir / "main.c"
            source_path.write_text(source, encoding="utf-8")
            binary = work_dir / "main"
            return _compile_and_run(
                ["gcc", "-std=c11", "-O2", "-o", str(binary), str(source_path)],
                [str(binary)],
                work_dir,
                stdin,
                timeout_seconds,
            )

        if language == Language.JAVASCRIPT:
            source_path = work_dir / "main.js"
            source_path.write_text(source, encoding="utf-8")
            stdout, stderr, exit_code, time_ms = _run_process(
                ["node", str(source_path)],
                work_dir,
                stdin,
                timeout_seconds,
            )
            return ExecuteResponse(
                stdout=stdout,
                stderr=stderr,
                exitCode=exit_code,
                timeMs=time_ms,
                compileError=None,
            )

        raise ValueError(f"Unsupported language: {language}")
    finally:
        shutil.rmtree(work_dir, ignore_errors=True)
