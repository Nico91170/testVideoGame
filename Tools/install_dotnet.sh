#!/usr/bin/env bash
set -euo pipefail

# Official .NET 8 SDK. Hash from Microsoft's 8.0 release metadata, verified
# before extraction. No TLS or artifact-verification bypass is supported.
RIVE_DOTNET_ROOT="${RIVE_DOTNET_ROOT:-/workspace/tools/dotnet}"
RIVE_SDK_VERSION=8.0.425
RIVE_SDK_SHA512=934b8060a7190e5909ad1fd0785db542f487b3bbf6cdd14826b02095fdd0d0394298b1634085eff302928fccc33f7c1a7253e9b87df555fc36fce819bcd2e798
if [[ -x "$RIVE_DOTNET_ROOT/dotnet" ]] && "$RIVE_DOTNET_ROOT/dotnet" --list-sdks | cut -d' ' -f1 | rg -qx "$RIVE_SDK_VERSION"; then
    printf '.NET SDK %s déjà disponible.\n' "$RIVE_SDK_VERSION"
    exit 0
fi
[[ "$(uname -s)" == Linux && "$(uname -m)" == x86_64 ]] || {
    printf 'Cet installateur cloud exige Linux x86_64 ; installez .NET 8 pour votre machine depuis Microsoft.\n' >&2
    exit 1
}
RIVE_TEMP_DIR="$(mktemp -d)"
trap 'rm -rf "$RIVE_TEMP_DIR"' EXIT
curl --fail --location --show-error --connect-timeout 15 --max-time 240 \
    "https://builds.dotnet.microsoft.com/dotnet/Sdk/$RIVE_SDK_VERSION/dotnet-sdk-$RIVE_SDK_VERSION-linux-x64.tar.gz" \
    --output "$RIVE_TEMP_DIR/sdk.tar.gz"
printf '%s  %s\n' "$RIVE_SDK_SHA512" "$RIVE_TEMP_DIR/sdk.tar.gz" | sha512sum --check --status
mkdir -p "$RIVE_DOTNET_ROOT"
tar -xzf "$RIVE_TEMP_DIR/sdk.tar.gz" -C "$RIVE_DOTNET_ROOT"
"$RIVE_DOTNET_ROOT/dotnet" --version
