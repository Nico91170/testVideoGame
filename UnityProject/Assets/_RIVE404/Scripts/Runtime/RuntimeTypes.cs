using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rive404.Runtime
{
    public enum DebugOperation { Scan, Patch, Override }
    public enum InteractableKind { Dialogue, Terminal, Collectible, Checkpoint, EndingRestore, EndingLiberate }

    public sealed class DialogueView
    {
        public string speaker;
        public string text;
        public List<DialogueOption> choices = new List<DialogueOption>();
    }

    public sealed class DialogueOption
    {
        public string label;
        public Action action;
        public DialogueOption(string label, Action action) { this.label = label; this.action = action; }
    }

    public interface IInteractable { bool CanInteract { get; } void Interact(); }
    public interface IDamageable { void TakeDamage(float amount); }
    public interface IDebugTarget { bool Accepts(DebugOperation operation); }
}
