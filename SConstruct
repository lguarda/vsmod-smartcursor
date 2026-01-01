import subprocess
import os

vars = Variables('.sconscache.py')

vars.Add(
    PathVariable(
        'VINTAGE_STORY',
        'Vintage story path',
        '/opt/Vintagestory/',   # default
        PathVariable.PathAccept
    )
)

env = Environment(variables=vars)
vars.Update(env)
vars.Save('.sconscache.py', env)
env.Help(vars.GenerateHelpText(env))

def run_cake(target, source, env):
    proc_env = os.environ.copy()
    proc_env["VINTAGE_STORY"] = str(env["VINTAGE_STORY"])
    cmd = [
        "dotnet",
        "run",
        "--project",
        "./CakeBuild/CakeBuild.csproj"
    ]
    subprocess.check_call(cmd, env=proc_env)

sources = Glob("SmartCursor/*.cs")

env.Command("cake-build", sources, run_cake)
env.Clean("cake-build", ['SmartCursor/bin', 'SmartCursor/obj', 'Releases'])

env.Default("cake-build")

def run_program(target, source, env):
    cmd = [
        f"{env['VINTAGE_STORY']}/Vintagestory",
        "-o", "moddebug",
        "--addModPath", "SmartCursor/bin/Release/Mods",
    ]

    print("Running:", " ".join(cmd))
    subprocess.check_call(cmd)

# 2. Add a command target
run = env.Command("run", [], run_program)

# 3. Always rebuild/run it (so it runs every time)
env.AlwaysBuild(run)
