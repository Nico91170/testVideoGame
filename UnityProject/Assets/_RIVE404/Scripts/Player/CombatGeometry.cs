using UnityEngine;

namespace Rive404.Player
{
    internal static class CombatGeometry
    {
        // Damage cannot travel through walls; own and victim colliders are excluded.
        internal static bool ClearPath(Vector3 from, Vector3 to, Transform attacker, Transform victim)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.001f) return true;
            RaycastHit[] hits = Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0,
                QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(attacker) || hit.transform.IsChildOf(victim)) continue;
                // Other actors do not substitute for the intended target or hide behind walls.
                if (hit.collider.GetComponentInParent<EnemyAgent>() != null
                    || hit.collider.GetComponentInParent<PlayerController>() != null) continue;
                return false;
            }
            return true;
        }

        internal static Material CreateEffectMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material material = new Material(shader);
            material.name = "RIVE404 Combat Effect";
            return material;
        }
    }
}
