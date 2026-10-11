using System;
using System.IO;
using System.Reflection;
using Rive404.Domain;
using Rive404.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rive404.Editor
{
    /// <summary>Explicit, repeatable preparation. No dependency on a hidden prebuilt project.</summary>
    public static class Rive404ProjectSetup
    {
        public const string BootstrapPath = "Assets/_RIVE404/Scenes/Bootstrap.unity";
        private const string SettingsRoot = "Assets/_RIVE404/Settings";
        private const string ResourcesRoot = "Assets/_RIVE404/Resources";

        [MenuItem("RIVE 404/1. Préparer le projet")]
        public static void Prepare()
        {
            EnsureFolder(SettingsRoot);
            EnsureFolder(ResourcesRoot);
            ConfigureRendering();
            IncludeRuntimeShaders();
            CreateCampaignAssets();
            PrepareModelImports();
            ConfigurePlayer();
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootstrapPath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateProject();
            Debug.Log("RIVE 404 : URP, données et scène Bootstrap préparés. Ouvrez Bootstrap puis lancez Play.");
        }

        private static void ConfigureRendering()
        {
            const string rendererPath = SettingsRoot + "/Rive404Renderer.asset";
            const string pipelinePath = SettingsRoot + "/Rive404URP.asset";
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                // URP's own factory initializes renderer and post-process resources.
                var factory = typeof(UniversalRenderPipelineAsset).GetMethod("CreateRendererAsset",
                    BindingFlags.Static | BindingFlags.NonPublic, null,
                    new[] { typeof(string), typeof(RendererType), typeof(bool), typeof(string) }, null);
                if (factory == null) throw new InvalidOperationException("Fabrique URP 17 du renderer introuvable.");
                renderer = factory.Invoke(null, new object[] { rendererPath, RendererType.UniversalRenderer, false, "Renderer" }) as UniversalRendererData;
                if (renderer == null) throw new InvalidOperationException("Création du renderer URP impossible.");
            }
            ResourceReloader.ReloadAllNullIn(renderer, UniversalRenderPipelineAsset.packagePath);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            // URP 17 serializes the renderer list; using SerializedObject preserves
            // the package's own asset type and avoids hand-written URP YAML.
            var serialized = new SerializedObject(pipeline);
            var rendererList = serialized.FindProperty("m_RendererDataList");
            if (rendererList == null || !rendererList.isArray)
                throw new InvalidOperationException("URP incompatible : liste des renderers introuvable. Version attendue 17.0.3.");
            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            var defaultRenderer = serialized.FindProperty("m_DefaultRendererIndex");
            if (defaultRenderer != null) defaultRenderer.intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            pipeline.renderScale = 1f;
            pipeline.msaaSampleCount = 2;
            pipeline.shadowDistance = 80f;
            pipeline.supportsHDR = false;
            pipeline.shadowCascadeCount = 2;
            pipeline.supportsMainLightShadows = true;
            pipeline.supportsAdditionalLightShadows = false;
            pipeline.mainLightShadowmapResolution = 2048;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            // Unity 6 needs registered global settings, including shader resources.
            var ensureGlobal = typeof(UniversalRenderPipelineGlobalSettings).GetMethod("Ensure",
                BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(bool) }, null);
            if (ensureGlobal == null || ensureGlobal.Invoke(null, new object[] { true }) == null)
                throw new InvalidOperationException("Initialisation des Global Settings URP impossible.");
            int selectedQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                var preset = pipeline;
                if (i != 1)
                {
                    string presetPath = SettingsRoot + (i == 0 ? "/Rive404URP_Low.asset" : "/Rive404URP_High.asset");
                    preset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(presetPath);
                    if (preset == null)
                    {
                        preset = UnityEngine.Object.Instantiate(pipeline);
                        AssetDatabase.CreateAsset(preset, presetPath);
                    }
                    preset.renderScale = i == 0 ? 0.75f : 1f;
                    preset.msaaSampleCount = i == 0 ? 1 : 4;
                    preset.shadowDistance = i == 0 ? 35f : 130f;
                    preset.shadowCascadeCount = i == 0 ? 1 : 4;
                    preset.mainLightShadowmapResolution = i == 0 ? 1024 : 4096;
                    preset.supportsMainLightShadows = i != 0;
                    preset.supportsAdditionalLightShadows = false;
                    preset.supportsHDR = false;
                    EditorUtility.SetDirty(preset);
                }
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = preset;
            }
            QualitySettings.SetQualityLevel(selectedQuality, false);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);
        }

        private static void IncludeRuntimeShaders()
        {
            var settings = GraphicsSettings.GetGraphicsSettings();
            if (settings == null) throw new InvalidOperationException("GraphicsSettings indisponible.");
            var serialized = new SerializedObject(settings);
            var shaders = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (shaders == null || !shaders.isArray)
                throw new InvalidOperationException("La liste Always Included Shaders est indisponible.");
            string[] names = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Universal Render Pipeline/Particles/Unlit", "Sprites/Default" };
            foreach (string name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null) throw new InvalidOperationException("Shader requis absent : " + name);
                bool found = false;
                for (int i = 0; i < shaders.arraySize; i++)
                    if (shaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
                if (found) continue;
                int next = shaders.arraySize;
                shaders.arraySize = next + 1;
                shaders.GetArrayElementAtIndex(next).objectReferenceValue = shader;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCampaignAssets()
        {
            var campaign = LoadOrCreate<QuestCatalogAsset>(ResourcesRoot + "/Campaign.asset");
            // Content lives in the catalog after creation; preparation preserves edits.
            if (campaign.definitions == null || campaign.definitions.Count == 0)
            {
                campaign.definitions = Campaign.CreateDefinitions();
                EditorUtility.SetDirty(campaign);
            }
            LoadOrCreate<DebugToolSettings>(ResourcesRoot + "/DebugToolSettings.asset");
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "RIVE404";
            PlayerSettings.productName = "RIVE 404 - Le Dernier Commit";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        }

        private static void PrepareModelImports()
        {
            string modelFolder = ResourcesRoot + "/Models";
            if (!AssetDatabase.IsValidFolder(modelFolder)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { modelFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || !path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                bool animated = name == "Nico" || name == "Sillage" || name == "NPC" || name == "Corrupted";
                var expectedType = animated ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
                bool changed = importer.animationType != expectedType || importer.importAnimation != animated
                    || importer.importCameras || importer.importLights || !importer.useFileScale || importer.globalScale != 1f
                    || importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard
                    || importer.animationWrapMode != WrapMode.Loop;
                Rive404ModelImporter.Configure(importer);
                if (changed) importer.SaveAndReimport();
            }
        }

        [MenuItem("RIVE 404/2. Valider la campagne")]
        public static void ValidateProject()
        {
            var catalog = Resources.Load<QuestCatalogAsset>("Campaign");
            var definitions = catalog != null ? catalog.definitions : Campaign.CreateDefinitions();
            var errors = StateRules.ValidateDefinitions(definitions);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapPath) == null)
                throw new InvalidOperationException("Scène Bootstrap absente : " + BootstrapPath);
            int main = 0, side = 0;
            foreach (var quest in definitions) { if (quest.isMain) main++; else side++; }
            if (main != 9 || side != 5) throw new InvalidOperationException("La campagne doit contenir 9 missions principales et 5 secondaires.");
            Debug.Log("RIVE 404 : campagne validée (9 principales, 5 secondaires).");
        }

        [MenuItem("RIVE 404/3. Construire Windows x64")]
        public static void BuildWindows()
        {
            Prepare();
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Module Windows Build Support requis dans Unity Hub.");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            string output = Path.Combine(root, "Builds", "Windows", "RIVE404.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { BootstrapPath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build Windows échouée : " + report.summary.result + ", erreurs " + report.summary.totalErrors);
            Debug.Log("RIVE 404 : build Windows créée : " + output);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = parent + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(parent, parts[i]);
                parent = next;
            }
        }
    }
}
