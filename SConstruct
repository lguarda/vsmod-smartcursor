import sys
from pathlib import Path

sys.path.insert(0, "vscons-build-utils/site_scons")

from build_utils import (
    git_version,
    dotnet_fmt,
    build_mod_release,
    vs_run_game,
    roslynator,
    get_scons_vs_option,
    setup_modinfo,
    make_copy_target,
)

# Handle options/args
vars = Variables(".sconscache.py")
get_scons_vs_option(vars)
env = Environment(variables=vars)
vars.Update(env)
vars.Save(".sconscache.py", env)
env.Help(vars.GenerateHelpText(env))
env["GIT_VERSION"] = git_version()

# This should be the source path dir it will be lower cased for read modid in json modinfo.file
mod_id="SmartCursor"

# Define source so scons know when to rebuild
# I should probably check if dotnet don't already handle this
src_dir = Path('./src')
sources = [
    str(p) for p in src_dir.rglob("*.cs")
]

# I should probably put this in the buid utils
def build_mod(env, sources, mod_id, mod_name, desc, server=False, client=True):
    csproj = f"{mod_id}.csproj"
    zip_label = f"{env['GIT_VERSION']}-debug" if env["DEBUG"] else env["GIT_VERSION"]
    release_zip = f"Release/{mod_id.lower()}_{zip_label}.zip"

    modinfo = setup_modinfo(env, "./bin", server, client, mod_id.lower(), mod_name, desc)

    def _build_release(target, source, env):
        build_mod_release("./src", mod_id.lower(), zip_label, env)

    env.Command(release_zip, sources, _build_release)
    env.Clean(release_zip, [f"bin", f"obj", "Release"])
    env.Depends(release_zip, modinfo)

    def _fmt(target, source, env):
        dotnet_fmt(csproj, env)

    fmt = env.Command("fmt", sources, _fmt)
    env.AlwaysBuild(fmt)
    env.Alias("format", fmt)

    # not working yet
    # def _check(target, source, env):
    #     roslynator(csproj, env)

    # check = env.Command("check", [], _check)
    # env.AlwaysBuild(check)

    install = env.InstallAs(
        target=f"{env['VINTAGE_STORY_DATA']}/Mods/{mod_id.lower()}.zip",
        source=release_zip,
    )
    env.Alias("install", install)

    return release_zip

SConscript('./vsqa/SConscript', exports='env')
Import('vsqa_csproj_path', 'vsqa_sources')


def _build_vsqa_dotnet(csproj):
    proc_env = os.environ.copy()
    proc_env["VINTAGE_STORY"] = env["VINTAGE_STORY"]
    cmd = [
        "dotnet",
        "publish",
        csproj,
    ]
    subprocess.run(cmd, env=proc_env)

def build_vsqa_dotnet(target, source, env):
    _build_vsqa_dotnet(vsqa_csproj_path)

build = env.Command("build", [vsqa_sources,], build_vsqa_dotnet)


# Builds targets
smartcursor_release = build_mod(
    env,
    sources,
    mod_id="SmartCursor",
    mod_name="Smart cursor",
    desc="This mod aims to implement the smart cursor feature from Terraria",
)

env.Default(smartcursor_release)

# Tools targets
def _run_action(target, source, env):
    vs_run_game(env)

run = env.Command("run", [], _run_action)
env.AlwaysBuild(run)

make_copy_target("backupsave", f"{env['VINTAGE_STORY_DATA']}/Saves", f"{env['VINTAGE_STORY_DATA']}/Saves.bak")
make_copy_target("restoresave", f"{env['VINTAGE_STORY_DATA']}/Saves.bak", f"{env['VINTAGE_STORY_DATA']}/Saves")
