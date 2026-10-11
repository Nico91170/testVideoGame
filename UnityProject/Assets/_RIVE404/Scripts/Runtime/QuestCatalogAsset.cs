using System.Collections.Generic;
using Rive404.Domain;
using UnityEngine;

namespace Rive404.Runtime
{
    [CreateAssetMenu(menuName = "RIVE404/Campagne", fileName = "Campaign")]
    public sealed class QuestCatalogAsset : ScriptableObject
    {
        public List<QuestDefinition> definitions = new List<QuestDefinition>();
    }
}
