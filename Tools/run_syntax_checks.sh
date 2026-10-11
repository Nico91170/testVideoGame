#!/usr/bin/env bash
set -euo pipefail
RIVE_REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RIVE_DOTNET="${RIVE_DOTNET:-/workspace/tools/dotnet/dotnet}"
if [[ ! -x "$RIVE_DOTNET" ]]; then
    if command -v dotnet >/dev/null 2>&1; then RIVE_DOTNET="$(command -v dotnet)";
    else printf 'SDK .NET absent. Exécutez Tools/install_dotnet.sh.\n' >&2; exit 1; fi
fi
export DOTNET_CLI_HOME="${RIVE_DOTNET_CLI_HOME:-/workspace/tools/dotnet-home}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1
cd "$RIVE_REPO_ROOT/Tools/SyntaxChecks"
"$RIVE_DOTNET" restore SyntaxChecks.csproj --configfile "$RIVE_REPO_ROOT/Tools/DomainChecks/NuGet.Config" --verbosity quiet
"$RIVE_DOTNET" run --project SyntaxChecks.csproj --no-restore --configuration Release -- "$RIVE_REPO_ROOT/UnityProject/Assets"
