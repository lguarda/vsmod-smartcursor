import subprocess
import os

def git_version():
    try:
        return subprocess.check_output(
            ["git", "describe", "--tags", "--always"],
            stderr=subprocess.DEVNULL
        ).decode().strip()
    except Exception:
        return "unknown"


def dotnet_run(csproj, vs_path):
    proc_env = os.environ.copy()
    proc_env["VINTAGE_STORY"] = vs_path
    cmd = [
        "dotnet",
        "run",
        "--project",
        csproj,
    ]
    subprocess.run(cmd, env=proc_env)
