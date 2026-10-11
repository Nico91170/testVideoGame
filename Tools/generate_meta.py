#!/usr/bin/env python3
"""Generate deterministic initial Unity GUIDs; preserve every existing metadata file."""
import hashlib
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "UnityProject"
assets = root / "Assets"
created = 0
for path in sorted(assets.rglob("*")):
    if path.name.endswith(".meta") or path.name.startswith("."):
        continue
    meta = Path(str(path) + ".meta")
    if meta.exists():
        continue
    guid = hashlib.sha256(path.relative_to(root).as_posix().encode()).hexdigest()[:32]
    text = "fileFormatVersion: 2\nguid: " + guid + "\n"
    if path.is_dir():
        text += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.suffix == ".cs":
        text += "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.suffix == ".asmdef":
        text += "AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    meta.write_text(text, encoding="utf-8")
    created += 1
print(f"Métadonnées initiales créées : {created}; les .meta existantes ont été conservées.")
