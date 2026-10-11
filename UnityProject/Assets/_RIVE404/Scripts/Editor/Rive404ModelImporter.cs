using System;
using System.IO;
using UnityEditor;

namespace Rive404.Editor
{
    /// <summary>Editable Blender FBX rigs use generic clips consumed by LocomotionAnimation Playables.</summary>
    public sealed class Rive404ModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/_RIVE404/Resources/Models/", StringComparison.Ordinal)) return;
            if (!assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) return;
            var importer = (ModelImporter)assetImporter;
            Configure(importer);
        }

        public static void Configure(ModelImporter importer)
        {
            string assetPath = importer.assetPath;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            bool animated = name == "Nico" || name == "Sillage" || name == "NPC" || name == "Corrupted";
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = animated;
            importer.animationType = animated ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            importer.animationWrapMode = UnityEngine.WrapMode.Loop;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }
    }
}
