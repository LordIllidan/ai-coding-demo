"""End-to-end coverage for src/health.py.

No HTTP framework or e2e harness exists in this repo (issue title says
"endpoint" but the implementation is a plain function, not a served
route). As a minimal e2e stand-in, this invokes health() in a fresh
subprocess (real process boundary, no in-process import/mocking) the
same way a user running the script would, and asserts on stdout.
"""
import json
import subprocess
import sys


def test_health_cli_invocation_returns_expected_status():
    result = subprocess.run(
        [sys.executable, "-c", "import json; from src.health import health; print(json.dumps(health()))"],
        capture_output=True,
        text=True,
        check=True,
    )

    assert json.loads(result.stdout.strip()) == {"status": "ok"}


def test_health_cli_invocation_exits_cleanly():
    result = subprocess.run(
        [sys.executable, "-c", "from src.health import health; health()"],
        capture_output=True,
        text=True,
    )

    assert result.returncode == 0
    assert result.stderr == ""
