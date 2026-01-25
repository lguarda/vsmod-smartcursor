import subprocess
import os
from build_utils import git_version, dotnet_run


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

env["GIT_VERSION"] = git_version()

def smartcursor_cake(target, source, env):
    dotnet_run("./CakeBuild/CakeBuild.csproj", str(env["VINTAGE_STORY"]))

def smartcursorplus_cake(target, source, env):
    dotnet_run("./CakeBuildPlus/CakeBuild.csproj", str(env["VINTAGE_STORY"]))

smartcursor_sources = Glob("SmartCursor/*.cs")
smartcursorplus_sources = Glob("SmartCursorPlus/*.cs")

smartcursor_release = f"Releases/smartcursor_{env["GIT_VERSION"]}.zip"
smartcursorplus_release = f"ReleasesServer/smartcursorplus_{env["GIT_VERSION"]}.zip"

env.Command(smartcursor_release, smartcursor_sources, smartcursor_cake)
env.Command(smartcursorplus_release, smartcursorplus_sources, smartcursorplus_cake)
env.Alias("server", smartcursorplus_release)

env.Clean(smartcursor_release, ['SmartCursor/bin', 'SmartCursor/obj', 'Releases'])
env.Clean(smartcursorplus_release, ['SmartCursorPlus/bin', 'SmartCursorPlus/obj', 'ReleasesServer'])
env.Default(smartcursor_release)

fmt = env.Command(
    target=None,          # no build artifact
    source=[smartcursor_sources,smartcursorplus_sources],
    action="clang-format -i $SOURCES"
)

env.Alias("format", fmt)
env.Alias("fmt", fmt)

smartcursor_install_release = env.InstallAs(target=f"{str(env["VINTAGE_STORY_DATA"])}/Mods/smartcursor.zip", source=smartcursor_release)
smartcursorplus_install_release = env.Command(None, smartcursorplus_release, f"cp $SOURCE {str(env["VINTAGE_STORY_DATA"])}/Mods/smartcursor.zip")

env.Alias("install", smartcursor_install_release)
env.Alias("sinstall", smartcursorplus_install_release)

def run_program(target, source, env):
    cmd = [
        f"{env['VINTAGE_STORY']}/Vintagestory",
        "-o", "moddebug",
    ]

    print("Running:", " ".join(cmd))
    subprocess.run(cmd)


smartcursor_modinfo = env.Substfile(
    target="SmartCursor/modinfo.json",
    source="SmartCursor/modinfo.json.in",
    SUBST_DICT={
        "@GIT_VERSION@": env["GIT_VERSION"],
        "@WITH_SERVER@": 'false',
        "@MOD_ID@": "smartcursor",
        "@MOD_NAME@": "Smart cursor",
        }
)

smartcursorplus_modinfo = env.Substfile(
    target="SmartCursorPlus/modinfo.json",
    source="SmartCursor/modinfo.json.in",
    SUBST_DICT={
        "@GIT_VERSION@": env["GIT_VERSION"],
        "@WITH_SERVER@": 'true',
        "@MOD_ID@": "smartcursorplus",
        "@MOD_NAME@": "Smart cursor plus",
        }
)

env.Depends(smartcursor_release, smartcursor_modinfo)
env.Depends(smartcursorplus_release, smartcursorplus_modinfo)

smartcursorplus_cakebuild_program = env.Substfile(
    target="CakeBuildPlus/Program.cs",
    source="CakeBuild/Program.cs",
    SUBST_DICT={
        "SmartCursor": "SmartCursorPlus",
        "Releases" : "ReleasesServer"
        }
)
smartcursorplus_cakebuild = env.Install("CakeBuildPlus/", "CakeBuild/CakeBuild.csproj")
env.Depends(smartcursorplus_release, [smartcursorplus_cakebuild, smartcursorplus_cakebuild_program])

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
        "SmartCursorPlus/SmartCursorPlus.csproj",
    ]
    print("Running:", " ".join(cmd))
    subprocess.run(cmd, env=proc_env)

check = env.Command("check", [], roslynator)
env.AlwaysBuild(check)

