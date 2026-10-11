using System;
using UnityEngine;

namespace Rive404.World
{
    [Serializable]
    public sealed class MapEntry
    {
        public string id;
        public string label;
        public Vector3 position;
        public int district;

        public MapEntry(string id, string label, Vector3 position, int district)
        {
            this.id = id;
            this.label = label;
            this.position = position;
            this.district = district;
        }
    }
}
