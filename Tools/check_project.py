#!/usr/bin/env python3
"""Checks source wiring, metadata and pinned setup; does not emulate Unity import."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import re
import sys


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    project = root / "UnityProject"
    assets = project / "Assets"
    errors: list[str] = []

    def check(condition: bool, message: str) -> None:
        if not condition:
            errors.append(message)

    version = project / "ProjectSettings/ProjectVersion.txt"
    check(version.exists() and "m_EditorVersion: 6000.0.84f1" in version.read_text(), "Version Unity 6000.0.84f1 non épinglée.")
    manifest = json.loads((project / "Packages/manifest.json").read_text())
    expected = {
        "com.unity.render-pipelines.universal": "17.0.3",
        "com.unity.inputsystem": "1.11.2",
        "com.unity.cinemachine": "3.1.3",
        "com.unity.ai.navigation": "2.0.6",
        "com.unity.test-framework": "1.4.5",
        "com.unity.ide.visualstudio": "2.0.22",
    }
    for package, pin in expected.items():
        check(manifest["dependencies"].get(package) == pin, f"Package non épinglé : {package} {pin}.")

    guids: dict[str, Path] = {}
    metas = 0
    for meta in assets.rglob("*.meta"):
        metas += 1
        match = re.search(r"^guid: ([a-f0-9]{32})$", meta.read_text(), re.MULTILINE)
        if not match:
            errors.append(f"GUID absent/invalide : {meta.relative_to(root)}.")
            continue
        guid = match.group(1)
        check(guid not in guids, f"GUID en double : {guid}.")
        guids[guid] = meta
        check(meta.with_suffix("").exists(), f".meta orphelin : {meta.relative_to(root)}.")
    for path in assets.rglob("*"):
        if path.name.endswith(".meta") or path.name.startswith("."):
            continue
        check(Path(str(path) + ".meta").exists(), f".meta absent : {path.relative_to(root)}.")

    scene = assets / "_RIVE404/Scenes/Bootstrap.unity"
    check(scene.exists(), "Scène Bootstrap absente.")
    runtime_path = "Assets/_RIVE404/Scripts/Runtime/GameRuntime.cs"
    expected_guid = hashlib.sha256(runtime_path.encode()).hexdigest()[:32]
    if scene.exists():
        text = scene.read_text()
        check(f"guid: {expected_guid}, type: 3" in text, "Bootstrap ne référence pas le GUID GameRuntime convenu.")
        check(expected_guid in guids, "Script GameRuntime non résolu par une .meta.")
        for guid in re.findall(r"guid: ([a-f0-9]{32})", text):
            check(guid in guids, f"Référence Bootstrap manquante : {guid}.")

    assemblies: set[str] = set()
    assembly_data = []
    for path in assets.rglob("*.asmdef"):
        data = json.loads(path.read_text())
        check(data["name"] not in assemblies, f"Assembly en double : {data['name']}.")
        assemblies.add(data["name"])
        assembly_data.append((path, data))
    for path, data in assembly_data:
        for reference in data.get("references", []):
            if reference.startswith("Rive404."):
                check(reference in assemblies, f"Référence assembly absente : {path.name} -> {reference}.")
    domain = assets / "_RIVE404/Scripts/Domain"
    for path in domain.glob("*.cs"):
        check("using UnityEngine" not in path.read_text(), f"Domaine dépendant de Unity : {path.name}.")

    lock = project / "Packages/packages-lock.json"
    if not lock.exists():
        print("INFO packages-lock.json : à produire par le Package Manager Unity ; registry Unity inaccessible ici.")
    print(f"Intégrité structurelle : {metas} .meta, {len(assemblies)} assemblies, {len(errors)} erreurs.")
    for message in errors:
        print("FAIL " + message, file=sys.stderr)
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
