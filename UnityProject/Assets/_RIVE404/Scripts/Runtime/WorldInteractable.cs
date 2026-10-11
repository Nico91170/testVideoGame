using UnityEngine;

namespace Rive404.Runtime
{
    public sealed class WorldInteractable : MonoBehaviour, IInteractable, IDebugTarget
    {
        public string id;
        public string title;
        public string eventId;
        public string npcId;
        public string sideQuestId;
        public InteractableKind kind;
        public bool debugTarget;
        public DebugOperation operation;
        public GameObject controlledObject;
        public bool controlledEnabledOnPatch = true;
        public float duration = 24;
        public bool collected;
        private float effectUntil;
        private bool initialControlledState;
        private bool initialized;
        private float revealedUntil;

        public bool CanInteract
        {
            get
            {
                var game = GameRuntime.Instance;
                return game != null && game.CanAcceptInput && game.Player != null && !collected &&
                    Vector3.Distance(game.Player.transform.position, transform.position) <= 4.5f && game.CanUseTarget(this);
            }
        }

        private void Start()
        {
            initialControlledState = controlledObject != null && controlledObject.activeSelf;
            initialized = true;
            RefreshFromState();
        }

        private void Update()
        {
            if (effectUntil > 0 && Time.time >= effectUntil)
            {
                effectUntil = 0;
                if (controlledObject != null) controlledObject.SetActive(initialControlledState);
            }
        }

        public void RefreshFromState()
        {
            var game = GameRuntime.Instance;
            if (game == null || game.State == null) return;
            collected = game.State.flags.Contains("collected:" + id);
            if (kind == InteractableKind.Collectible) gameObject.SetActive(!collected);
        }

        public bool Accepts(DebugOperation action)
        { return debugTarget && (action == DebugOperation.Scan || action == operation); }

        public void Interact() { if (CanInteract) GameRuntime.Instance.Interact(this); }

        public void Reveal(float seconds) { revealedUntil = Time.time + seconds; }

        public void ApplyEffect(DebugOperation action, float seconds)
        {
            if (!initialized)
                initialControlledState = controlledObject != null && controlledObject.activeSelf;
            if (controlledObject != null)
                controlledObject.SetActive(action == DebugOperation.Patch && controlledEnabledOnPatch);
            effectUntil = Time.time + Mathf.Max(1, seconds);
            Reveal(seconds);
        }

        private void OnGUI()
        {
            if (Time.time >= revealedUntil || Camera.main == null || GameRuntime.Instance == null ||
                !GameRuntime.Instance.IsPlaying) return;
            Vector3 screen = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
            if (screen.z <= 0 || Vector3.Distance(Camera.main.transform.position, transform.position) > 80) return;
            Color old = GUI.color;
            GUI.color = new Color(.3f, 1, .9f);
            string label = title + (debugTarget ? " [" + operation.ToString().ToUpperInvariant() + "]" : "");
            GUI.Label(new Rect(screen.x - 110, Screen.height - screen.y, 230, 35), label);
            GUI.color = old;
        }
    }
}
