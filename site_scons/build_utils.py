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

def vs_version(env):
    cmd = [
        f"{env['VINTAGE_STORY']}/Vintagestory",
        "--version"
    ]
    print("Running:", {" ".join(cmd)})
    output = subprocess.check_output(cmd, text=True)
    print(f"Output:{output}")
    return output

def vs_run(env):
    cmd = [
        f"{env['VINTAGE_STORY']}/Vintagestory",
        "-o", f"moddebug-{vs_version(env)}",
        "--dataPath", str(env["VINTAGE_STORY_DATA"]),
    ]

    print("Running vs with:", " ".join(cmd))
    subprocess.run(cmd)


def dotnet_run(csproj, vs_path, dotnet_vers):
    proc_env = os.environ.copy()
    proc_env["VINTAGE_STORY"] = vs_path
    proc_env["DOTNET_VERS"] = dotnet_vers
    cmd = [
        "dotnet",
        "run",
        "--project",
        csproj,
    ]
    subprocess.run(cmd, env=proc_env)
