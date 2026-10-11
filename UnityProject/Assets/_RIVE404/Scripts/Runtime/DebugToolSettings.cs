using UnityEngine;

namespace Rive404.Runtime
{
    [CreateAssetMenu(menuName = "RIVE404/Debug Tool", fileName = "DebugToolSettings")]
    public sealed class DebugToolSettings : ScriptableObject
    {
        [Min(0)] public float scanCost = 8;
        [Min(0)] public float patchCost = 20;
        [Min(0)] public float overrideCost = 30;
        [Min(1)] public float range = 18;
        [Min(1)] public float scanDuration = 8;
        [Min(1)] public float effectDuration = 24;
    }
}
