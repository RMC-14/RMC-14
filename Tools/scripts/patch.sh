#!/bin/sh

getPath() {
	repoRoot=$(git rev-parse --show-toplevel 2>/dev/null)

	[ -z "$repoRoot" ] && echo "Unable to locate repo root" >&2 && return 1

	echo "$repoRoot/Tools/patches/arm64.patch"
}

patchArm64() {
	patchPath=$(getPath)

	git apply -R --check "$patchPath" >/dev/null 2>&1 &&
		echo 'RobustToolbox patch already applied' && return 0
	git apply "$patchPath" && echo 'Applied RobustToolbox patch' && return 0

	echo 'RobustToolbox patch not applied' >&2
	return 1
}

revertArm64() {
	patchPath=$(getPath)

	git apply --check "$patchPath" >/dev/null 2>&1 &&
		echo 'RobustToolbox already reverted' && return 0
	git apply -R "$patchPath" && echo 'Reverted RobustToolbox patch' && return 0

	echo 'RobustToolbox patch not reverted' >&2
	return 1
}
