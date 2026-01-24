import subprocess
import os
from build_utils import git_version


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
vars.Add(
    BoolVariable(
        'SMARTCURSOR_PLUS',
        'Build with server side part',
        False,   # default
    )
)

env = Environment(variables=vars)
vars.Update(env)
vars.Save('.sconscache.py', env)
env.Help(vars.GenerateHelpText(env))

env["GIT_VERSION"] = git_version()
if env["SMARTCURSOR_PLUS"]:
    env["WITH_SERVER"] = True
    env["MOD_ID"] = "smartcursorplus"
    env["MOD_NAME"] = "Smart cursor plus"
else:
    env["WITH_SERVER"] = False
    env["MOD_ID"] = "smartcursor"
    env["MOD_NAME"] = "Smart cursor"

def run_cake(target, source, env):
    proc_env = os.environ.copy()
    proc_env["VINTAGE_STORY"] = str(env["VINTAGE_STORY"])
    cmd = [
        "dotnet",
        "run",
        "--project",
        "./CakeBuild/CakeBuild.csproj",
        '-p:DefineConstants="WITH_SERVER"'
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
        #"--addModPath", "SmartCursor/bin/Release/Mods",
    ]

    print("Running:", " ".join(cmd))
    subprocess.run(cmd)

modinfo = env.Substfile(
    target="SmartCursor/modinfo.json",
    source="SmartCursor/modinfo.json.in",
    SUBST_DICT={
        "@GIT_VERSION@": env["GIT_VERSION"],
        "@WITH_SERVER@": 'true' if env["WITH_SERVER"] else 'false' ,
        "@MOD_ID@": env["MOD_ID"],
        "@MOD_NAME@": env["MOD_NAME"],
        }
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
