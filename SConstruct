import sys
from pathlib import Path

sys.path.insert(0, "vscons-build-utils/site_scons")

from build_utils import git_version, cake_package, vs_run, roslynator, get_scons_vs_option, setup_modinfo, setup_cake_build, make_copy_target

vars = Variables('.sconscache.py')
get_scons_vs_option(vars)
env = Environment(variables=vars)
vars.Update(env)
vars.Save('.sconscache.py', env)
env.Help(vars.GenerateHelpText(env))
env["GIT_VERSION"] = git_version()

smartcursor_modinfo = setup_modinfo(env, "SmartCursor", False, True, "smartcursor", "Smart cursor", "This mod aim to implement the smartcursor feature from terraria")
smartcursor_cake = setup_cake_build(env, "CakeBuild", "SmartCursor", "Release")
smartcursor_sources = [str(p) for p in Path('./SmartCursor').rglob('*.cs')]

fmt = env.Command(
    target=None,          # no build artifact
    source=[smartcursor_sources],
    action="clang-format -i $SOURCES"
)

env.Alias("format", fmt)
env.Alias("fmt", fmt)

smartcursor_release = f"Release/smartcursor_{env["GIT_VERSION"]}.zip"

def package(target, source, env):
    cake_package("./CakeBuild/CakeBuild.csproj", str(env["VINTAGE_STORY"]), str(env["DOTNET_VERS"]))

env.Command(smartcursor_release, smartcursor_sources, package)
env.Clean(smartcursor_release, ['SmartCursor/bin', 'SmartCursor/obj', 'Release'])
env.Default(smartcursor_release)
env.Depends(smartcursor_release, [smartcursor_modinfo, smartcursor_cake])

smartcursor_install_release = env.InstallAs(target=f"{str(env["VINTAGE_STORY_DATA"])}/Mods/smartcursor.zip", source=smartcursor_release)

env.Alias("install", smartcursor_install_release)

def run_program(target, source, env):
    vs_run(env)

run = env.Command("run", [], run_program)

env.AlwaysBuild(run)

check = env.Command("check", [], roslynator)
env.AlwaysBuild(check)

make_copy_target("backupsave", f"{env["VINTAGE_STORY_DATA"]}/Saves", f"{env["VINTAGE_STORY_DATA"]}/Saves.bak")
make_copy_target("restoresave", f"{env["VINTAGE_STORY_DATA"]}/Saves.bak", f"{env["VINTAGE_STORY_DATA"]}/Saves")
