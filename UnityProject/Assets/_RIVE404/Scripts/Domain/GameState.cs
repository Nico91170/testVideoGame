using System;
using System.Collections.Generic;

namespace Rive404.Domain
{
    [Serializable]
    public sealed class PositionData
    {
        public float x;
        public float y;
        public float z;

        public PositionData() { }
        public PositionData(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public PositionData Copy() { return new PositionData(x, y, z); }
    }

    public enum QuestStatus { NotStarted, Active, Completed, Failed }

    [Serializable]
    public sealed class QuestProgress
    {
        public string id;
        public QuestStatus status;
        public int stepIndex;
    }

    [Serializable]
    public sealed class GameState
    {
        public int version = StateRules.CurrentVersion;
        public PositionData position = new PositionData(-340f, 1f, -300f);
        public PositionData checkpoint = new PositionData(-340f, 1f, -300f);
        public float health = 100f;
        public float energy = 100f;
        public int upgrades;
        public double playSeconds;
        public string ending = string.Empty;
        public List<string> flags = new List<string>();
        public List<QuestProgress> quests = new List<QuestProgress>();
    }

    [Serializable]
    public sealed class QuestStep
    {
        public string eventId;
        public string description;
        public string targetId;

        public QuestStep() { }
        public QuestStep(string eventId, string description, string targetId)
        {
            this.eventId = eventId;
            this.description = description;
            this.targetId = targetId;
        }
    }

    [Serializable]
    public sealed class QuestDefinition
    {
        public string id;
        public string title;
        public string description;
        public bool isMain;
        public int order;
        public List<QuestStep> steps = new List<QuestStep>();
    }
}
