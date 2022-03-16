import os
import shutil
from glob import glob
from pathlib import Path

import yaml
from invoke import task

REPO = Path(os.path.dirname(__file__))


def get_version():
    manifest = (REPO / "extension.yaml").read_text()
    return yaml.safe_load(manifest)["Version"]


@task
def build(ctx):
    ctx.run("dotnet build src -c Release")


@task
def pack(ctx, toolbox="~/AppData/Local/Playnite/Toolbox.exe"):
    target = REPO / "dist/raw"
    if target.exists():
        shutil.rmtree(str(target))
    # os.makedirs(str(target))
    shutil.copytree(str(REPO / "src/bin/Release/net462/"), target)
    # for file in glob(str(REPO / "src/bin/Release/net462/*")):
    #     shutil.copy(file, target)

    toolbox = Path(toolbox).expanduser()
    ctx.run('"{}" pack "{}" dist'.format(toolbox, target))
    for file in glob(str(REPO / "dist/*.pext")):
        if "_" in file:
            shutil.move(file, str(REPO / "dist/LudusaviRestic_v{}.pext".format(get_version())))

    shutil.make_archive(str(REPO / "dist/LudusaviRestic_v{}".format(get_version())), "zip", str(target))

@task
def style(ctx):
    ctx.run("dotnet format src")

@task
def clean(ctx):
  shutil.rmtree("dist")