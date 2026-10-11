#!/usr/bin/env bash
set -euo pipefail
RIVE_REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RIVE_MODE="${1:-editmode}"
if [[ -z "${UNITY_EDITOR:-}" || ! -x "$UNITY_EDITOR" ]]; then
    printf 'Définissez UNITY_EDITOR vers Unity 6000.0.84f1 avec une licence et le module Windows Build Support.\n' >&2
    exit 1
fi
mkdir -p "$RIVE_REPO_ROOT/Builds/TestResults"
RIVE_PROJECT="$RIVE_REPO_ROOT/UnityProject"
case "$RIVE_MODE" in
    prepare)
        "$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$RIVE_PROJECT" \
            -executeMethod Rive404.Editor.Rive404ProjectSetup.Prepare \
            -logFile "$RIVE_REPO_ROOT/Builds/TestResults/prepare.log" ;;
    editmode|playmode)
        if [[ "$RIVE_MODE" == editmode ]]; then RIVE_PLATFORM=EditMode; else RIVE_PLATFORM=PlayMode; fi
        "$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$RIVE_PROJECT" \
            -executeMethod Rive404.Editor.Rive404ProjectSetup.Prepare \
            -logFile "$RIVE_REPO_ROOT/Builds/TestResults/prepare.log"
        "$UNITY_EDITOR" -batchmode -nographics -projectPath "$RIVE_PROJECT" -runTests \
            -testPlatform "$RIVE_PLATFORM" -testResults "$RIVE_REPO_ROOT/Builds/TestResults/$RIVE_MODE.xml" \
            -logFile "$RIVE_REPO_ROOT/Builds/TestResults/$RIVE_MODE.log" ;;
    windows)
        "$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$RIVE_PROJECT" \
            -executeMethod Rive404.Editor.Rive404ProjectSetup.BuildWindows \
            -logFile "$RIVE_REPO_ROOT/Builds/TestResults/windows.log" ;;
    *) printf 'Usage : Tools/unity_checks.sh prepare|editmode|playmode|windows\n' >&2; exit 1 ;;
esac
