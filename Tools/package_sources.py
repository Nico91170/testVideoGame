#!/usr/bin/env python3
"""Package editable sources, with hashes and a verified ZIP archive."""

import argparse
import hashlib
import json
from pathlib import Path
import zipfile


ROOT = Path(__file__).resolve().parent.parent
ROOT_FILES = ("README.md", ".gitignore", ".gitattributes")
SOURCE_DIRS = (
    "UnityProject/Assets", "UnityProject/Packages", "UnityProject/ProjectSettings",
    "Blender", "Documentation", "Tools",
)
SKIP_DIRS = {"bin", "obj", "__pycache__", ".git", ".vs", ".idea"}
SKIP_SUFFIXES = {".pyc", ".blend1", ".blend2", ".log", ".ulf"}


def include(path):
    relative = path.relative_to(ROOT)
    if any(part in SKIP_DIRS for part in relative.parts):
        return False
    if path.suffix.lower() in SKIP_SUFFIXES or path.name.startswith(".env"):
        return False
    if path.is_symlink():
        raise ValueError("Source symbolique à vérifier avant livraison : " + str(relative))
    return True


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output", nargs="?", type=Path,
                        default=ROOT.parent / "RIVE404_Sources_v0_1.zip")
    destination = parser.parse_args().output.resolve()
    if destination.is_relative_to(ROOT):
        raise ValueError("Placer l'archive hors du dépôt des sources.")
    files = [ROOT / filename for filename in ROOT_FILES]
    directories = set()
    for name in SOURCE_DIRS:
        base = ROOT / name
        directories.add(base)
        for path in base.rglob("*"):
            if not include(path):
                continue
            if path.is_dir():
                directories.add(path)
            elif path.is_file():
                files.append(path)
    files.sort(key=lambda path: path.relative_to(ROOT).as_posix())
    for path in files:
        if not path.is_file():
            raise FileNotFoundError(path)
    hashes = {path.relative_to(ROOT).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest()
              for path in files}
    manifest = {
        "version": "0.1.0-sources",
        "files": len(files),
        "validation": "Tests de domaine et syntaxe exécutés ; compilation Unity non exécutée.",
        "sha256": hashes,
    }
    destination.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(destination, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in sorted(directories):
            archive.write(path, "RIVE404/" + path.relative_to(ROOT).as_posix() + "/")
        for path in files:
            archive.write(path, "RIVE404/" + path.relative_to(ROOT).as_posix())
        archive.writestr("RIVE404/DELIVERY_SHA256.json", json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    with zipfile.ZipFile(destination) as archive:
        bad = archive.testzip()
        if bad:
            raise ValueError("CRC incorrect : " + bad)
        for relative, expected in hashes.items():
            actual = hashlib.sha256(archive.read("RIVE404/" + relative)).hexdigest()
            if actual != expected:
                raise ValueError("Hash incorrect : " + relative)
    digest = hashlib.sha256(destination.read_bytes()).hexdigest()
    destination.with_suffix(destination.suffix + ".sha256").write_text(
        digest + "  " + destination.name + "\n", encoding="utf-8")
    print(json.dumps({"archive": str(destination), "source_files": len(files),
                      "bytes": destination.stat().st_size, "sha256": digest,
                      "crc_and_manifest": "PASS"}, ensure_ascii=False))


if __name__ == "__main__":
    main()
