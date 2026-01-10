#!/usr/bin/env python3

# Import future so people on py2 still get the clear error that they need to upgrade.
from __future__ import print_function
import sys
import subprocess

version = sys.version_info
if version.major < 3 or (version.major == 3 and version.minor < 5):
	print("ERROR: You need at least Python 3.5 to build SS14.")
	sys.exit(1)

patchPath = "Tools/patches/arm64.patch"
checkResult = subprocess.run(
	["git", "apply", "-R", "--check", patchPath],
	stdout=subprocess.DEVNULL,
	stderr=subprocess.DEVNULL,
)
if checkResult.returncode != 0:
	subprocess.run(["git", "apply", patchPath], check=True)

subprocess.run([sys.executable, "git_helper.py"], cwd="BuildChecker")
