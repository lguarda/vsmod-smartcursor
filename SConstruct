import subprocess
import os

vars = Variables('.sconscache.py')
home = os.environ.get("HOME")

# TODO: make some order in this file by useing scons-site directory

vars.Add(
    PathVariable(
        'VINTAGE_STORY',
        'Vintage story path',
        '/opt/Vintagestory/',   # default
        PathVariable.PathAccept
    )
)
vars.Add(
    PathVariable(
        'VINTAGE_STORY_DATA',
        'Vintage story data path where mod folder is located',
        f'{home}/.config/VintagestoryData/',   # default
        PathVariable.PathAccept
    )
)

env = Environment(variables=vars)
vars.Update(env)
vars.Save('.sconscache.py', env)
env.Help(vars.GenerateHelpText(env))

def git_version():
    try:
        return subprocess.check_output(
            ["git", "describe", "--tags", "--always"],
            stderr=subprocess.DEVNULL
        ).decode().strip()
    except Exception:
        return "unknown"

env["GIT_VERSION"] = git_version()

def run_cake(target, source, env):
    proc_env = os.environ.copy()
    proc_env["VINTAGE_STORY"] = str(env["VINTAGE_STORY"])
    cmd = [
        "dotnet",
        "run",
        "--project",
        "./CakeBuild/CakeBuild.csproj"
    ]
    subprocess.run(cmd, env=proc_env)

sources = Glob("SmartCursor/*.cs")

smartcursor_release = f"Releases/smartcursor_{env["GIT_VERSION"]}.zip"

env.Command(smartcursor_release, sources, run_cake)
env.Clean(smartcursor_release, ['SmartCursor/bin', 'SmartCursor/obj', 'Releases'])
env.Default(smartcursor_release)

fmt = env.Command(
    target=None,          # no build artifact
    source=sources,
    action="clang-format -i $SOURCES"
)

env.Alias("format", fmt)
env.Alias("fmt", fmt)

install_release = env.InstallAs(target=f"{str(env["VINTAGE_STORY_DATA"])}/Mods/smartcursor.zip", source=smartcursor_release)
env.Alias("install", install_release)

def run_program(target, source, env):
    cmd = [
        f"{env['VINTAGE_STORY']}/Vintagestory",
        "-o", "moddebug",
        "--addModPath", "SmartCursor/bin/Release/Mods",
    ]

    print("Running:", " ".join(cmd))
    subprocess.run(cmd)

modinfo = env.Substfile(
    target="SmartCursor/modinfo.json",
    source="SmartCursor/modinfo.json.in",
    SUBST_DICT={"@GIT_VERSION@": env["GIT_VERSION"]}
)

env.Depends(smartcursor_release, modinfo)

# 2. Add a command target
run = env.Command("run", [], run_program)

# 3. Always rebuild/run it (so it runs every time)
env.AlwaysBuild(run)

def roslynator(target, source, env):
    proc_env = os.environ.copy()
    proc_env["VINTAGE_STORY"] = str(env["VINTAGE_STORY"])
    cmd = [
        f"{home}/.dotnet/tools/roslynator",
        "analyze",
        "SmartCursor/SmartCursor.csproj",
    ]
    print("Running:", " ".join(cmd))
    subprocess.run(cmd, env=proc_env)

check = env.Command("check", [], roslynator)
env.AlwaysBuild(check)
