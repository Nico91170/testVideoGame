using System.Collections.Generic;
using UnityEngine;

namespace Rive404.Runtime
{
    public static class ModelFactory
    {
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static GameObject Attach(Transform parent, string resourceName, Vector3 localScale)
        {
            var source = Resources.Load<GameObject>(resourceName);
            if (source == null) return null;
            var instance = Object.Instantiate(source, parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = localScale;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var original = renderer.sharedMaterials;
                var replacements = new Material[original.Length];
                for (int index = 0; index < original.Length; index++)
                {
                    Material imported = original[index];
                    string key = imported == null ? "MAT_Default" : imported.name;
                    if (!Materials.TryGetValue(key, out Material converted))
                    {
                        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                        converted = new Material(shader) { name = key + "_URP" };
                        Color color = imported == null ? new Color(.55f, .62f, .65f) : imported.color;
                        if (imported != null && imported.HasProperty("_BaseColor")) color = imported.GetColor("_BaseColor");
                        converted.color = color;
                        if (converted.HasProperty("_BaseColor")) converted.SetColor("_BaseColor", color);
                        if (converted.HasProperty("_Smoothness")) converted.SetFloat("_Smoothness", .18f);
                        if (key.Contains("Glow") || key.Contains("Warning"))
                        {
                            converted.EnableKeyword("_EMISSION");
                            if (converted.HasProperty("_EmissionColor")) converted.SetColor("_EmissionColor", color * 1.6f);
                        }
                        Materials.Add(key, converted);
                    }
                    replacements[index] = converted;
                }
                renderer.sharedMaterials = replacements;
            }
            return instance;
        }
    }
}
