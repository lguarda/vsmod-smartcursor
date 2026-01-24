import subprocess

def git_version():
    try:
        return subprocess.check_output(
            ["git", "describe", "--tags", "--always"],
            stderr=subprocess.DEVNULL
        ).decode().strip()
    except Exception:
        return "unknown"


