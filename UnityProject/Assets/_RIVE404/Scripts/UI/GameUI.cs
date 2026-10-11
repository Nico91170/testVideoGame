using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Rive404.Domain;
using Rive404.Runtime;
using Rive404.World;

namespace Rive404.UI
{
    /// <summary>Resolution-independent French interface, without a Canvas or external UI package.</summary>
    public sealed class GameUI : MonoBehaviour
    {
        private enum Panel { None, Pause, Journal, Map, Settings, Credits, Saves }
        private const float CanvasWidth = 1280f;
        private const float CanvasHeight = 720f;
        private static readonly Color Ink = new Color(.035f, .065f, .085f, .96f);
        private static readonly Color Paper = new Color(.9f, .96f, .97f);
        private static readonly Color Muted = new Color(.55f, .69f, .73f);
        private static readonly Color Cyan = new Color(.18f, .91f, .86f);
        private static readonly Color Red = new Color(1f, .36f, .39f);
        private static readonly Color Gold = new Color(1f, .8f, .38f);

        private GameRuntime runtime;
        private GameSettings settings;
        private Panel panel;
        private Panel backPanel;
        private string selectedQuestId;
        private string mapFocusTarget;
        private Vector2 journalScroll;
        private Vector2 objectiveScroll;
        private Vector2 mapScroll;
        private Vector2 dialogueScroll;
        private DialogueView displayedDialogue;
        private GameState observedState;
        private bool showHelp;
        private bool stylesReady;
        private float nextCameraSync;
        private float fpsClock;
        private int fpsFrames;
        private int fps;
        private readonly List<Notice> notices = new List<Notice>();
        private string confirmText;
        private Action confirmAction;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle small;
        private GUIStyle button;
        private GUIStyle primaryButton;
        private GUIStyle mutedButton;
        private GUIStyle textCenter;
        private GUIStyle toggle;
        private Texture2D buttonIdle;
        private Texture2D buttonHover;
        private Texture2D buttonActive;
        private Texture2D primaryIdle;
        private Texture2D primaryHover;

        private sealed class Notice
        {
            public string Text;
            public float Expires;
        }

        public void Bind(GameRuntime game)
        {
            runtime = game;
            observedState = game.State;
            panel = Panel.None;
            confirmAction = null;
            confirmText = null;
            if (settings == null)
            {
                settings = GameSettings.Load();
                settings.Apply(settings.HasSavedDisplay);
            }
            runtime.ApplyAudioSettings(settings.MusicVolume, settings.EffectsVolume);
        }

        public void Notify(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            notices.Add(new Notice { Text = message, Expires = Time.unscaledTime + 6f });
            if (notices.Count > 4) notices.RemoveAt(0);
        }

        private void Update()
        {
            if (runtime == null) return;
            if (!ReferenceEquals(observedState, runtime.State))
            {
                observedState = runtime.State;
                panel = Panel.None;
                backPanel = Panel.None;
                selectedQuestId = null;
                mapFocusTarget = null;
                journalScroll = objectiveScroll = mapScroll = dialogueScroll = Vector2.zero;
                displayedDialogue = null;
                confirmAction = null;
                confirmText = null;
                runtime.ApplyAudioSettings(settings.MusicVolume, settings.EffectsVolume);
            }
            if (runtime.IsPlaying && runtime.IsPaused && panel == Panel.None && !runtime.IsDialogueOpen && !HasEnding)
                panel = Panel.Pause;
            notices.RemoveAll(n => n.Expires <= Time.unscaledTime);
            fpsClock += Time.unscaledDeltaTime;
            fpsFrames++;
            if (fpsClock >= .5f)
            {
                fps = Mathf.RoundToInt(fpsFrames / fpsClock);
                fpsClock = 0f;
                fpsFrames = 0;
            }
            if (settings != null && Time.unscaledTime >= nextCameraSync)
            {
                settings.ApplyCamera();
                nextCameraSync = Time.unscaledTime + .5f;
            }

            Keyboard keys = Keyboard.current;
            if (keys == null) return;
            if (keys.escapeKey.wasPressedThisFrame)
            {
                if (confirmAction != null) { confirmAction = null; confirmText = null; }
                else if (runtime.IsDialogueOpen) runtime.CloseDialogue();
                else if (panel == Panel.Settings || panel == Panel.Credits || panel == Panel.Saves) Back();
                else if (panel != Panel.None) ClosePanel();
                else if (runtime.IsPlaying && !HasEnding) OpenPanel(Panel.Pause);
            }
            if (!runtime.IsPlaying || runtime.IsDialogueOpen || HasEnding || confirmAction != null) return;
            if (panel == Panel.Settings || panel == Panel.Credits || panel == Panel.Saves) return;
            if (keys.jKey.wasPressedThisFrame) TogglePanel(Panel.Journal);
            if (keys.mKey.wasPressedThisFrame) TogglePanel(Panel.Map);
            if (keys.hKey.wasPressedThisFrame && panel == Panel.None) showHelp = !showHelp;
        }

        private bool HasEnding => runtime != null && runtime.State != null && !string.IsNullOrEmpty(runtime.State.ending);

        private void OnGUI()
        {
            if (runtime == null || settings == null) return;
            EnsureStyles();
            float scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
            if (scale <= 0) return;
            Matrix4x4 previous = GUI.matrix;
            Vector2 offset = new Vector2((Screen.width - CanvasWidth * scale) * .5f,
                                        (Screen.height - CanvasHeight * scale) * .5f);
            GUI.matrix = Matrix4x4.TRS(new Vector3(offset.x, offset.y, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.depth = -10;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && confirmAction == null;

            if (!runtime.IsPlaying)
            {
                DrawMenuBackground();
                if (panel == Panel.None) DrawMainMenu();
                else DrawPanel();
            }
            else if (HasEnding)
            {
                DrawEnding();
            }
            else
            {
                DrawHUD();
                if (runtime.IsDialogueOpen) DrawDialogue();
                else if (panel != Panel.None) DrawPanel();
                else if (showHelp) DrawHelp();
            }
            if (!runtime.IsDialogueOpen) DrawNotices();
            if (!string.IsNullOrEmpty(runtime.ErrorMessage))
            {
                Box(new Rect(220, 10, 840, 46), new Color(.34f, .06f, .09f, .98f));
                Label(new Rect(234, 17, 812, 34), runtime.ErrorMessage, small, Paper);
            }
            GUI.enabled = previousEnabled;
            if (confirmAction != null) DrawConfirmation();
            GUI.matrix = previous;
            GUI.color = Color.white;
            GUI.enabled = previousEnabled;
        }

        private void EnsureStyles()
        {
            if (stylesReady) return;
            buttonIdle = Solid(new Color(.1f, .19f, .22f));
            buttonHover = Solid(new Color(.14f, .32f, .35f));
            buttonActive = Solid(new Color(.2f, .43f, .43f));
            primaryIdle = Solid(Cyan);
            primaryHover = Solid(new Color(.48f, 1f, .92f));
            title = TextStyle(64, FontStyle.Bold, Paper);
            heading = TextStyle(28, FontStyle.Bold, Paper);
            body = TextStyle(19, FontStyle.Normal, Paper);
            small = TextStyle(15, FontStyle.Normal, Muted);
            textCenter = TextStyle(18, FontStyle.Normal, Paper);
            textCenter.alignment = TextAnchor.MiddleCenter;
            button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(0, 0, 0, 0), padding = new RectOffset(12, 12, 8, 8),
                wordWrap = true
            };
            button.normal.background = buttonIdle;
            button.hover.background = buttonHover;
            button.active.background = buttonActive;
            button.focused.background = buttonHover;
            button.normal.textColor = Paper;
            button.hover.textColor = Paper;
            button.active.textColor = Paper;
            button.focused.textColor = Paper;
            button.onNormal.background = buttonHover;
            button.onNormal.textColor = Paper;
            primaryButton = new GUIStyle(button);
            primaryButton.normal.background = primaryIdle;
            primaryButton.hover.background = primaryHover;
            primaryButton.active.background = primaryHover;
            primaryButton.normal.textColor = Ink;
            primaryButton.hover.textColor = Ink;
            primaryButton.active.textColor = Ink;
            mutedButton = new GUIStyle(button) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
            toggle = new GUIStyle(GUI.skin.toggle) { fontSize = 18, wordWrap = true };
            toggle.normal.textColor = Paper;
            toggle.onNormal.textColor = Cyan;
            toggle.hover.textColor = Paper;
            toggle.onHover.textColor = Cyan;
            stylesReady = true;
        }

        private static GUIStyle TextStyle(int size, FontStyle weight, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size, fontStyle = weight, wordWrap = true,
                alignment = TextAnchor.UpperLeft, padding = new RectOffset(0, 0, 0, 0), richText = false
            };
            style.normal.textColor = color;
            return style;
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void Box(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void Label(Rect rect, string text, GUIStyle style, Color color)
        {
            Color previous = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(rect, text ?? string.Empty, style);
            style.normal.textColor = previous;
        }

        private bool Button(Rect rect, string text, bool primary = false)
        {
            return GUI.Button(rect, text, primary ? primaryButton : button);
        }

        private void DrawMenuBackground()
        {
            Box(new Rect(-1280, -720, 3840, 2160), Ink);
            Color grid = new Color(.12f, .28f, .3f, .3f);
            for (int x = 0; x < 1280; x += 64) Box(new Rect(x, 0, 1, 720), grid);
            for (int y = 0; y < 720; y += 64) Box(new Rect(0, y, 1280, 1), grid);
            Box(new Rect(64, 64, 5, 590), Cyan);
            Box(new Rect(815, 168, 282, 10), Cyan);
            Box(new Rect(815, 192, 212, 5), new Color(.2f, .8f, .75f, .5f));
            Box(new Rect(1065, 230, 32, 280), new Color(.15f, .48f, .5f, .18f));
            Label(new Rect(84, 26, 1110, 24), "ARCHIPEL / RIVE 404                                             TERMINAL LOCAL • SILLAGE", small, Muted);
            Label(new Rect(84, 672, 1000, 25), "Une île. Un système corrompu. Une décision qui change sa mémoire.", small, Muted);
        }

        private void DrawMainMenu()
        {
            Label(new Rect(110, 111, 650, 85), "RIVE 404", title, Paper);
            Label(new Rect(114, 209, 620, 54), "Retrouvez la vérité dans une île qui a oublié son passé.", body, Muted);
            float y = 299;
            if (Button(new Rect(114, y, 360, 48), "Nouvelle partie", true))
            {
                if (runtime.HasSave)
                    Confirm("Créer une nouvelle partie ? La sauvegarde existante sera remplacée lorsque cette partie sera sauvegardée.", StartNew);
                else StartNew();
            }
            y += 58;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && runtime.HasSave;
            if (Button(new Rect(114, y, 360, 48), "Continuer")) LoadSave();
            GUI.enabled = previousEnabled;
            y += 58;
            if (Button(new Rect(114, y, 360, 43), "Charger une sauvegarde")) OpenPanel(Panel.Saves);
            y += 53;
            if (Button(new Rect(114, y, 172, 43), "Options")) OpenPanel(Panel.Settings);
            if (Button(new Rect(302, y, 172, 43), "Crédits")) OpenPanel(Panel.Credits);
            y += 53;
            if (Button(new Rect(114, y, 360, 43), "Quitter")) Confirm("Quitter RIVE 404 ?", Quit);
            Label(new Rect(825, 263, 310, 230), "PROTOCOLE DEBUG\n\nSCAN : comprendre\nPATCH : réparer\nOVERRIDE : reprendre le contrôle\n\nLa mémoire de l'île attend votre choix.", body, Muted);
            if (!runtime.HasSave) Label(new Rect(500, 370, 235, 42), "Aucune sauvegarde locale.", small, Muted);
        }

        private void StartNew()
        {
            panel = Panel.None;
            selectedQuestId = null;
            mapFocusTarget = null;
            showHelp = true;
            runtime.StartNewGame();
        }

        private void DrawHUD()
        {
            if (runtime.State == null) return;
            Box(new Rect(28, 27, 278, 112), Ink);
            Label(new Rect(45, 39, 175, 24), "NICO / LIEN SILLAGE", small, Cyan);
            DrawBar(new Rect(45, 72, 245, 20), runtime.State.health / 100f, Red, "SANTÉ  " + Mathf.CeilToInt(runtime.State.health));
            DrawBar(new Rect(45, 105, 245, 17), runtime.State.energy / 100f, Cyan, "ÉNERGIE  " + Mathf.CeilToInt(runtime.State.energy));
            Label(new Rect(1144, 27, 105, 28), fps + " FPS", small, Muted);

            QuestDefinition quest = runtime.Quests != null ? runtime.Quests.CurrentMain : null;
            QuestStep objective = quest != null ? runtime.Quests.Objective(quest) : null;
            if (quest != null)
            {
                Box(new Rect(790, 62, 460, 105), Ink);
                Label(new Rect(807, 77, 415, 25), quest.title, body, Gold);
                Label(new Rect(807, 109, 415, 50), objective != null ? objective.description : "Mission terminée.", small, Paper);
            }
            if (panel == Panel.None && !runtime.IsDialogueOpen)
            {
                DrawObjectiveMarker(objective);
                WorldInteractable target = runtime.CurrentTarget;
                if (target != null && (target.CanInteract || target.debugTarget))
                {
                    bool nearby = runtime.Player != null && Vector3.Distance(runtime.Player.transform.position, target.transform.position) <= 4.5f;
                    bool interactive = target.CanInteract && nearby;
                    Box(new Rect(396, 500, 488, target.debugTarget && interactive ? 92 : 66), Ink);
                    Label(new Rect(414, 511, 450, 27), target.title, body, Cyan);
                    if (interactive) Label(new Rect(414, 543, 450, 24), "[E] " + InteractionVerb(target.kind), small, Paper);
                    if (target.debugTarget)
                        Label(new Rect(414, interactive ? 571 : 543, 450, 22), "[Tab] SCAN     [1] PATCH     [2] OVERRIDE", small, Cyan);
                }
                Box(new Rect(28, 665, 1222, 35), Ink);
                Label(new Rect(43, 673, 1190, 24), "[J] Journal   [M] Carte   [H] Aide   [F5] Sauvegarder   [Échap] Pause", small, Paper);
                Box(new Rect(635, 354, 10, 2), new Color(.8f, 1f, .96f, .8f));
                Box(new Rect(639, 350, 2, 10), new Color(.8f, 1f, .96f, .8f));
            }
        }

        private void DrawBar(Rect rect, float fraction, Color color, string caption)
        {
            Box(rect, new Color(.12f, .2f, .23f));
            Box(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), color);
            Box(new Rect(rect.x, rect.y, 132, rect.height), new Color(.025f, .055f, .075f, .76f));
            var captionRect = new Rect(rect.x + 6, rect.y - 1, rect.width - 8, rect.height + 3);
            Label(captionRect, caption, small, Paper);
        }

        private void DrawObjectiveMarker(QuestStep objective)
        {
            if (objective == null || string.IsNullOrEmpty(objective.targetId) || runtime.World == null || runtime.Player == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 target = runtime.World.PositionOf(objective.targetId);
            Vector3 screen = camera.WorldToScreenPoint(target + Vector3.up * 3f);
            float distance = Vector3.Distance(runtime.Player.transform.position, target);
            if (distance <= 7f) return;
            bool behind = screen.z <= 0;
            float scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
            float offsetX = (Screen.width - CanvasWidth * scale) * .5f;
            float offsetY = (Screen.height - CanvasHeight * scale) * .5f;
            float x = behind ? (screen.x < Screen.width * .5f ? 60 : 1220) : Mathf.Clamp((screen.x - offsetX) / scale, 60, 1220);
            float y = behind ? 345 : Mathf.Clamp((Screen.height - screen.y - offsetY) / scale, 190, 600);
            Box(new Rect(x - 28, y - 17, 56, 34), Ink);
            Label(new Rect(x - 28, y - 15, 56, 30), Mathf.RoundToInt(distance) + " m", textCenter, Gold);
            Box(new Rect(x - 3, y - 27, 6, 6), Gold);
        }

        private static string InteractionVerb(InteractableKind kind)
        {
            switch (kind)
            {
                case InteractableKind.Dialogue: return "Parler";
                case InteractableKind.Collectible: return "Collecter";
                case InteractableKind.Checkpoint: return "Activer le point de contrôle";
                case InteractableKind.EndingRestore: return "Choisir la restauration";
                case InteractableKind.EndingLiberate: return "Choisir la libération";
                default: return "Interagir";
            }
        }

        private void DrawHelp()
        {
            Box(new Rect(330, 149, 620, 408), Ink);
            Label(new Rect(352, 169, 576, 34), "PRISE EN MAIN", heading, Cyan);
            string movement = settings.Azerty ? "Z Q S D" : "W A S D";
            Label(new Rect(352, 220, 576, 298),
                movement + " : se déplacer   •   Souris : regarder\n" +
                "Maj : sprinter   •   Espace : sauter   •   Alt gauche : esquiver\n" +
                "Clic gauche : attaque   •   Clic droit : attaque puissante\n" +
                "E : interaction   •   Tab : SCAN\n" +
                "1 : PATCH   •   2 : OVERRIDE\n" +
                "J : journal   •   M : carte   •   F5 : sauvegarder\n\n" +
                "Approchez d'un objectif, puis utilisez l'action indiquée.", body, Paper);
            Label(new Rect(352, 526, 576, 23), "[H] Masquer cette aide — touches selon leur position physique.", small, Muted);
        }

        private void DrawPanel()
        {
            Box(new Rect(-1280, -720, 3840, 2160), new Color(.015f, .03f, .05f, .78f));
            Box(new Rect(68, 54, 1144, 610), Ink);
            Box(new Rect(68, 54, 1144, 4), Cyan);
            switch (panel)
            {
                case Panel.Pause: DrawPause(); break;
                case Panel.Journal: DrawJournal(); break;
                case Panel.Map: DrawMap(); break;
                case Panel.Settings: DrawSettings(); break;
                case Panel.Credits: DrawCredits(); break;
                case Panel.Saves: DrawSaves(); break;
            }
        }

        private void PanelHeader(string text, string subtitle = null)
        {
            Label(new Rect(98, 78, 860, 42), text, heading, Cyan);
            if (!string.IsNullOrEmpty(subtitle)) Label(new Rect(98, 126, 1000, 37), subtitle, small, Muted);
            if (Button(new Rect(1058, 80, 125, 38), "Retour")) Back();
        }

        private void DrawPause()
        {
            PanelHeader("PARTIE EN PAUSE", "Le monde est suspendu. Votre progression attend ici.");
            float y = 188;
            if (Button(new Rect(100, y, 368, 47), "Reprendre", true)) ClosePanel();
            y += 59;
            if (Button(new Rect(100, y, 368, 47), "Sauvegarder")) runtime.SaveGame();
            y += 59;
            if (Button(new Rect(100, y, 368, 47), "Charger une sauvegarde")) OpenPanel(Panel.Saves);
            y += 59;
            if (Button(new Rect(100, y, 176, 43), "Journal")) OpenPanel(Panel.Journal);
            if (Button(new Rect(292, y, 176, 43), "Carte")) OpenPanel(Panel.Map);
            y += 55;
            if (Button(new Rect(100, y, 176, 43), "Options")) OpenPanel(Panel.Settings);
            if (Button(new Rect(292, y, 176, 43), "Crédits")) OpenPanel(Panel.Credits);
            y += 55;
            if (Button(new Rect(100, y, 368, 43), "Menu principal"))
                Confirm("Retourner au menu principal ? La progression sera sauvegardée avant le retour.", ReturnToMenu);
            Label(new Rect(566, 197, 540, 255),
                "Conseil\n\nSCAN révèle les possibilités du monde. PATCH répare une structure. OVERRIDE détourne un système.\n\nLa santé et l'énergie se lisent en haut à gauche. Les missions et la carte indiquent où poursuivre l'enquête.", body, Paper);
            Label(new Rect(566, 521, 540, 65), "Temps de partie : " + PlayTime() + "\nSauvegarde : " + (runtime.HasSave ? "disponible" : "non créée"), small, Muted);
        }

        private void DrawSaves()
        {
            PanelHeader("SAUVEGARDE LOCALE", "Un emplacement de campagne, conservé sur cet appareil.");
            Box(new Rect(100, 185, 1080, 150), new Color(.07f, .14f, .17f));
            Label(new Rect(122, 209, 850, 33), runtime.HasSave ? "Campagne RIVE 404" : "Aucune sauvegarde disponible", heading, Paper);
            Label(new Rect(122, 256, 1000, 56), runtime.HasSave
                ? "Le fichier contient votre position, vos missions, vos décisions et vos améliorations. Les préférences graphiques et audio sont enregistrées séparément."
                : "Commencez une partie, puis utilisez F5 ou le menu Pause pour enregistrer votre progression.", body, Muted);
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && runtime.HasSave;
            if (Button(new Rect(100, 365, 350, 48), "Charger la campagne", true))
            {
                if (runtime.IsPlaying)
                    Confirm("Charger le dernier enregistrement ? La progression actuelle non sauvegardée sera perdue.", LoadSave);
                else LoadSave();
            }
            GUI.enabled = previousEnabled;
            if (runtime.IsPlaying && Button(new Rect(473, 365, 350, 48), "Sauvegarder maintenant")) runtime.SaveGame();
        }

        private void LoadSave()
        {
            showHelp = false;
            runtime.ContinueGame();
            panel = runtime.IsPlaying && runtime.IsPaused ? Panel.Saves : Panel.None;
        }

        private void DrawJournal()
        {
            PanelHeader("JOURNAL", "Les missions principales suivent l'enquête ; les habitants proposent des missions secondaires.");
            if (runtime.Quests == null) return;
            List<QuestDefinition> definitions = runtime.Quests.Definitions;
            if (string.IsNullOrEmpty(selectedQuestId))
                selectedQuestId = runtime.Quests.CurrentMain != null ? runtime.Quests.CurrentMain.id : definitions.FirstOrDefault()?.id;
            var ordered = definitions.OrderByDescending(q => q.isMain).ThenBy(q => q.order).ToList();
            float height = ordered.Count * 70f + 65;
            journalScroll = GUI.BeginScrollView(new Rect(99, 178, 355, 454), journalScroll, new Rect(0, 0, 330, height));
            float y = 0;
            bool? main = null;
            foreach (QuestDefinition quest in ordered)
            {
                if (main != quest.isMain)
                {
                    Label(new Rect(0, y, 320, 23), quest.isMain ? "ENQUÊTE PRINCIPALE" : "HISTOIRES DES HABITANTS", small, Cyan);
                    y += 29;
                    main = quest.isMain;
                }
                QuestProgress progress = runtime.Quests.GetProgress(quest.id);
                QuestStatus status = progress != null ? progress.status : QuestStatus.NotStarted;
                Color color = selectedQuestId == quest.id ? new Color(.13f, .32f, .34f) : new Color(.065f, .12f, .15f);
                Box(new Rect(0, y, 321, 59), color);
                if (GUI.Button(new Rect(0, y, 321, 59), string.Empty, GUIStyle.none)) { selectedQuestId = quest.id; objectiveScroll = Vector2.zero; }
                Label(new Rect(12, y + 7, 296, 26), quest.title, small, status == QuestStatus.Completed ? Cyan : Paper);
                Label(new Rect(12, y + 34, 296, 21), StatusName(status), small, status == QuestStatus.Active ? Gold : Muted);
                y += 68;
            }
            GUI.EndScrollView();
            Box(new Rect(473, 177, 1, 455), new Color(.17f, .3f, .33f));
            QuestDefinition selected = definitions.FirstOrDefault(q => q.id == selectedQuestId);
            if (selected == null) return;
            QuestProgress selectedProgress = runtime.Quests.GetProgress(selected.id);
            QuestStatus selectedStatus = selectedProgress != null ? selectedProgress.status : QuestStatus.NotStarted;
            Label(new Rect(500, 184, 648, 62), selected.title, heading, Paper);
            Label(new Rect(500, 246, 648, 65), selected.description, body, Muted);
            Label(new Rect(500, 317, 648, 23), StatusName(selectedStatus), small, selectedStatus == QuestStatus.Active ? Gold : Cyan);
            float stepHeight = selected.steps.Count * 79f + 8;
            objectiveScroll = GUI.BeginScrollView(new Rect(500, 354, 650, 200), objectiveScroll, new Rect(0, 0, 626, stepHeight));
            float sy = 0;
            for (int i = 0; i < selected.steps.Count; i++)
            {
                bool complete = selectedStatus == QuestStatus.Completed || (selectedProgress != null && i < selectedProgress.stepIndex);
                bool current = selectedStatus == QuestStatus.Active && selectedProgress != null && i == selectedProgress.stepIndex;
                Label(new Rect(0, sy + 5, 34, 27), complete ? "✓" : current ? "→" : "○", body, complete ? Cyan : current ? Gold : Muted);
                Label(new Rect(36, sy + 5, 578, 62), selected.steps[i].description, body, complete ? Muted : current ? Paper : Muted);
                sy += 79;
            }
            GUI.EndScrollView();
            QuestStep currentObjective = runtime.Quests.Objective(selected);
            if (currentObjective != null && !string.IsNullOrEmpty(currentObjective.targetId)
                && Button(new Rect(500, 582, 310, 40), "Repérer sur la carte"))
            {
                mapFocusTarget = currentObjective.targetId;
                OpenPanel(Panel.Map);
            }
        }

        private static string StatusName(QuestStatus status)
        {
            switch (status)
            {
                case QuestStatus.Active: return "En cours";
                case QuestStatus.Completed: return "Terminée";
                case QuestStatus.Failed: return "Échouée";
                default: return "À découvrir";
            }
        }

        private void DrawMap()
        {
            PanelHeader("CARTE DE RIVE", "Nord en haut • exploration progressive • 1 000 × 1 000 mètres");
            if (runtime.World == null) return;
            Rect map = new Rect(100, 171, 450, 450);
            Box(map, new Color(.065f, .13f, .16f));
            if (IsDistrictDiscovered(0))
                Box(new Rect(map.x, map.y, map.width * .38f, map.height), new Color(.05f, .32f, .31f, .16f));
            if (IsDistrictDiscovered(1))
                Box(new Rect(map.x + map.width * .38f, map.y, map.width * .34f, map.height), new Color(.25f, .14f, .4f, .16f));
            if (IsDistrictDiscovered(2))
                Box(new Rect(map.x + map.width * .72f, map.y, map.width * .28f, map.height), new Color(.4f, .13f, .09f, .16f));
            Color grid = new Color(.15f, .29f, .32f);
            for (int i = 0; i <= 10; i++)
            {
                Box(new Rect(map.x + i * 45, map.y, 1, map.height), grid);
                Box(new Rect(map.x, map.y + i * 45, map.width, 1), grid);
            }
            Label(new Rect(map.x + 9, map.y + 9, 35, 25), "N ↑", small, Paper);
            if (IsDistrictDiscovered(0)) Label(new Rect(map.x + 9, map.y + 39, 156, 25), "VIEUX-PORT", small, Cyan);
            if (IsDistrictDiscovered(1)) Label(new Rect(map.x + 180, map.y + 39, 150, 25), "NÉON", small, DistrictColor(1));
            if (IsDistrictDiscovered(2)) Label(new Rect(map.x + 335, map.y + 39, 113, 45), "FALAISES\nROUGES", small, DistrictColor(2));
            List<MapEntry> entries = runtime.World.Entries != null
                ? runtime.World.Entries.Where(entry => IsDistrictDiscovered(entry.district)).ToList()
                : new List<MapEntry>();
            QuestDefinition currentMain = runtime.Quests != null ? runtime.Quests.CurrentMain : null;
            QuestStep mainObjective = currentMain != null ? runtime.Quests.Objective(currentMain) : null;
            string focused = !string.IsNullOrEmpty(mapFocusTarget) ? mapFocusTarget : mainObjective?.targetId;
            bool focusedInUnknownDistrict = false;
            if (entries != null)
            {
                foreach (MapEntry entry in entries)
                {
                    Vector2 point = MapPoint(map, entry.position);
                    Color marker = DistrictColor(entry.district);
                    Box(new Rect(point.x - 3, point.y - 3, 6, 6), marker);
                    if (map.Contains(Event.current.mousePosition) && Vector2.Distance(Event.current.mousePosition, point) < 9f)
                    {
                        Box(new Rect(map.x + 8, map.yMax - 34, map.width - 16, 26), Ink);
                        Label(new Rect(map.x + 17, map.yMax - 29, map.width - 30, 22), entry.label, small, Paper);
                    }
                }
            }
            if (!string.IsNullOrEmpty(focused))
            {
                Vector3 targetPosition = runtime.World.PositionOf(focused);
                int targetDistrict = targetPosition.x < -120 ? 0 : targetPosition.x < 220 ? 1 : 2;
                focusedInUnknownDistrict = !IsDistrictDiscovered(targetDistrict);
                Vector2 target = MapPoint(map, targetPosition);
                Box(new Rect(target.x - 8, target.y - 8, 16, 16), Gold);
                Box(new Rect(target.x - 4, target.y - 4, 8, 8), Ink);
            }
            if (runtime.Player != null)
            {
                Vector2 player = MapPoint(map, runtime.Player.transform.position);
                Box(new Rect(player.x - 5, player.y - 5, 10, 10), Paper);
                Vector3 forward = runtime.Player.transform.forward;
                Box(new Rect(player.x + forward.x * 10 - 2, player.y - forward.z * 10 - 2, 4, 4), Paper);
            }
            Label(new Rect(590, 181, 500, 30), "VOTRE POSITION ET VOS OBJECTIFS", body, Cyan);
            Label(new Rect(590, 225, 500, 83), "■ Vous     □ Objectif sélectionné\nLes lieux et les habitants apparaissent lorsqu'un quartier est exploré.", body, Paper);
            Label(new Rect(590, 322, 540, 57), mainObjective != null ? "Enquête : " + mainObjective.description : "Consultez le journal pour choisir une mission.", body, Gold);
            if (focusedInUnknownDistrict)
                Label(new Rect(590, 380, 540, 22), "Objectif en zone inconnue : rejoignez-le pour découvrir le quartier.", small, Muted);
            if (Button(new Rect(590, 407, 245, 40), "Objectif principal")) mapFocusTarget = null;
            if (Button(new Rect(858, 407, 245, 40), "Ouvrir le journal")) OpenPanel(Panel.Journal);
            Label(new Rect(590, 461, 510, 25), "LIEUX DÉCOUVERTS", small, Muted);
            if (entries != null)
            {
                mapScroll = GUI.BeginScrollView(new Rect(590, 499, 553, 123), mapScroll, new Rect(0, 0, 525, entries.Count * 32));
                for (int i = 0; i < entries.Count; i++)
                {
                    MapEntry entry = entries[i];
                    Box(new Rect(1, i * 32 + 9, 8, 8), DistrictColor(entry.district));
                    if (GUI.Button(new Rect(21, i * 32, 493, 29), entry.label, mutedButton)) mapFocusTarget = entry.id;
                }
                GUI.EndScrollView();
            }
            if (entries.Count == 0)
                Label(new Rect(590, 505, 530, 64), "Explorez les environs pour compléter la carte.", body, Muted);
            Label(new Rect(103, 633, 450, 22), "Un carreau représente 100 mètres.", small, Muted);
        }

        private bool IsDistrictDiscovered(int district)
        {
            return runtime.State != null && runtime.State.flags != null && runtime.State.flags.Contains("visited:" + district);
        }

        private static Vector2 MapPoint(Rect map, Vector3 position)
        {
            return new Vector2(map.x + Mathf.Clamp01((position.x + 500f) / 1000f) * map.width,
                               map.y + (1f - Mathf.Clamp01((position.z + 500f) / 1000f)) * map.height);
        }

        private static Color DistrictColor(int district)
        {
            return district == 0 ? Cyan : district == 1 ? new Color(.68f, .59f, 1f) : new Color(1f, .48f, .32f);
        }

        private void DrawSettings()
        {
            PanelHeader("OPTIONS", "Préférences de cet appareil, communes à toutes vos parties.");
            Label(new Rect(101, 185, 470, 30), "SON ET CAMÉRA", body, Cyan);
            Label(new Rect(101, 225, 315, 24), "Volume général", body, Paper);
            Label(new Rect(490, 225, 85, 24), Mathf.RoundToInt(settings.Volume * 100) + " %", small, Muted);
            settings.Volume = GUI.HorizontalSlider(new Rect(101, 253, 449, 22), settings.Volume, 0f, 1f);
            AudioListener.volume = settings.Volume;
            Label(new Rect(101, 280, 315, 24), "Musique et ambiance", body, Paper);
            Label(new Rect(490, 280, 85, 24), Mathf.RoundToInt(settings.MusicVolume * 100) + " %", small, Muted);
            settings.MusicVolume = GUI.HorizontalSlider(new Rect(101, 308, 449, 22), settings.MusicVolume, 0f, 1f);
            Label(new Rect(101, 335, 315, 24), "Effets sonores", body, Paper);
            Label(new Rect(490, 335, 85, 24), Mathf.RoundToInt(settings.EffectsVolume * 100) + " %", small, Muted);
            settings.EffectsVolume = GUI.HorizontalSlider(new Rect(101, 363, 449, 22), settings.EffectsVolume, 0f, 1f);
            runtime.ApplyAudioSettings(settings.MusicVolume, settings.EffectsVolume);
            Label(new Rect(101, 397, 315, 24), "Sensibilité de la souris", body, Paper);
            Label(new Rect(490, 397, 85, 24), settings.Sensitivity.ToString("0.00"), small, Muted);
            settings.Sensitivity = GUI.HorizontalSlider(new Rect(101, 425, 449, 22), settings.Sensitivity, .05f, .5f);
            settings.ApplyCamera();
            Label(new Rect(101, 459, 450, 24), "Disposition du clavier", body, Paper);
            if (Button(new Rect(101, 495, 210, 40), settings.Azerty ? "● AZERTY / ZQSD" : "AZERTY / ZQSD")) settings.Azerty = true;
            if (Button(new Rect(329, 495, 221, 40), !settings.Azerty ? "● QWERTY / WASD" : "QWERTY / WASD")) settings.Azerty = false;
            Label(new Rect(101, 547, 449, 32), "Son et souris : effet immédiat. Le profil adapte l'aide aux positions physiques des touches.", small, Muted);

            Box(new Rect(584, 177, 1, 380), new Color(.17f, .3f, .33f));
            Label(new Rect(621, 185, 520, 30), "AFFICHAGE", body, Cyan);
            Label(new Rect(621, 230, 170, 27), "Qualité", body, Paper);
            string[] quality = { "Bas", "Standard", "Élevé" };
            settings.Quality = GUI.SelectionGrid(new Rect(790, 226, 354, 40), settings.Quality, quality, 3, mutedButton);
            Label(new Rect(621, 285, 170, 27), "Résolution", body, Paper);
            if (Button(new Rect(790, 281, 354, 40), settings.Width + " × " + settings.Height + "   ›")) CycleResolution();
            settings.Fullscreen = GUI.Toggle(new Rect(621, 341, 520, 32), settings.Fullscreen, " Plein écran", toggle);
            settings.VSync = GUI.Toggle(new Rect(621, 387, 520, 32), settings.VSync, " Synchronisation verticale (VSync)", toggle);
            Label(new Rect(621, 439, 166, 27), "Limite FPS", body, Paper);
            string[] caps = { "30", "60", "120", "Libre" };
            int[] values = { 30, 60, 120, -1 };
            int index = Array.IndexOf(values, settings.FrameLimit);
            settings.FrameLimit = values[GUI.SelectionGrid(new Rect(790, 435, 354, 40), Mathf.Max(0, index), caps, 4, mutedButton)];
            Label(new Rect(621, 497, 520, 52), "Avec VSync, le rythme de l'écran peut primer sur la limite FPS. La qualité élevée demande plus de ressources.", small, Muted);
            if (Button(new Rect(100, 583, 310, 47), "Appliquer et enregistrer", true))
            {
                settings.Save();
                settings.Apply(true);
                runtime.ApplyAudioSettings(settings.MusicVolume, settings.EffectsVolume);
                Notify("Options enregistrées.");
            }
            if (Button(new Rect(432, 583, 225, 47), "Valeurs par défaut"))
            {
                settings = new GameSettings();
                settings.Apply(false);
                runtime.ApplyAudioSettings(settings.MusicVolume, settings.EffectsVolume);
            }
            Label(new Rect(688, 587, 455, 44), "Le bouton Retour conserve aussi les préférences.", small, Muted);
        }

        private void CycleResolution()
        {
            var candidates = new List<Vector2Int>
            {
                new Vector2Int(1280, 720), new Vector2Int(1600, 900), new Vector2Int(1920, 1080)
            };
            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (size.x >= 1024 && size.y >= 600 && !candidates.Contains(size)) candidates.Add(size);
            }
            candidates = candidates.OrderBy(v => (long)v.x * v.y).ThenBy(v => v.x).ToList();
            int current = candidates.FindIndex(v => v.x == settings.Width && v.y == settings.Height);
            Vector2Int next = candidates[(current + 1) % candidates.Count];
            settings.Width = next.x;
            settings.Height = next.y;
        }

        private void DrawCredits()
        {
            PanelHeader("CRÉDITS", "RIVE 404 • aventure d'exploration, d'enquête et de combat");
            Label(new Rect(101, 194, 965, 65), "RIVE 404", title, Paper);
            Label(new Rect(101, 290, 925, 249),
                "Projet : Nico91170/testVideoGame\n\n" +
                "Moteur : Unity 6 • Universal Render Pipeline\n" +
                "Conception des modèles : Blender\n\n" +
                "Merci d'explorer Rive, de rencontrer ses habitants et de décider ce que leur mémoire deviendra.\n\n" +
                "Les licences et notices accompagnent la distribution dans la documentation du projet.", body, Paper);
            Label(new Rect(101, 592, 1020, 38), "Aucune connexion réseau n'est nécessaire pour jouer. Votre sauvegarde reste locale.", small, Muted);
        }

        private void DrawDialogue()
        {
            DialogueView dialogue = runtime.CurrentDialogue;
            if (dialogue == null) return;
            if (!ReferenceEquals(dialogue, displayedDialogue))
            {
                displayedDialogue = dialogue;
                dialogueScroll = Vector2.zero;
            }
            Box(new Rect(-1280, -720, 3840, 2160), new Color(.02f, .03f, .045f, .48f));
            int count = dialogue.choices != null ? dialogue.choices.Count : 0;
            Rect panelRect = new Rect(160, 76, 960, 584);
            Box(panelRect, Ink);
            Box(new Rect(panelRect.x, panelRect.y, panelRect.width, 4), Cyan);
            Label(new Rect(panelRect.x + 25, panelRect.y + 24, 694, 38), dialogue.speaker, heading, Cyan);
            if (runtime.CanSkipDialogue && Button(new Rect(panelRect.xMax - 214, panelRect.y + 24, 189, 36), "Passer"))
            {
                runtime.SkipDialogue();
                return;
            }
            float textHeight = Mathf.Max(150, panelRect.height - Mathf.Max(1, count) * 54 - 120);
            Rect textRect = new Rect(panelRect.x + 25, panelRect.y + 88, panelRect.width - 50, textHeight);
            float textWidth = textRect.width - 24;
            float contentHeight = Mathf.Max(textHeight, body.CalcHeight(new GUIContent(dialogue.text ?? string.Empty), textWidth) + 12);
            if (contentHeight > textHeight)
                Label(new Rect(panelRect.x + 25, panelRect.y + 63, 850, 22), "Faites défiler le texte avec la molette pour poursuivre la lecture.", small, Muted);
            dialogueScroll = GUI.BeginScrollView(textRect, dialogueScroll, new Rect(0, 0, textWidth, contentHeight));
            Label(new Rect(0, 0, textWidth, contentHeight), dialogue.text, body, Paper);
            GUI.EndScrollView();
            float y = textRect.yMax + 14;
            if (count == 0)
            {
                if (Button(new Rect(panelRect.x + 25, y, 905, 44), "Fermer")) runtime.CloseDialogue();
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (Button(new Rect(panelRect.x + 25, y, 905, 44), dialogue.choices[i].label, i == 0))
                    {
                        runtime.ChooseDialogue(i);
                        break;
                    }
                    y += 54;
                }
            }
        }

        private void DrawEnding()
        {
            Box(new Rect(-1280, -720, 3840, 2160), Ink);
            bool liberated = runtime.State.ending == "liberate";
            Color accent = liberated ? Gold : Cyan;
            Label(new Rect(102, 63, 1050, 27), "PROTOCOLE FINAL / " + (liberated ? "LIBÉRATION" : "RESTAURATION"), small, accent);
            Box(new Rect(100, 108, 1080, 3), accent);
            Label(new Rect(102, 145, 1050, 83), liberated ? "La mémoire appartient aux vivants." : "Rive retrouve sa continuité.", heading, Paper);
            string narrative = liberated
                ? "Vous avez rompu la tutelle du système. Les souvenirs libérés se mêlent aux voix des habitants. La promesse de Sillage vous accompagne, mais aucun programme ne décidera plus pour l'île.\n\nL'avenir est incertain. Pour la première fois, il leur appartient."
                : "Vous avez réparé le cœur de Rive. Les archives retrouvent leur cohérence et les quartiers se reconnectent. L'ancre laissée par Sillage relie une mémoire enfin lisible.\n\nL'île pourra reconstruire son histoire, avec les traces de celles et ceux qui ont traversé la corruption.";
            Label(new Rect(102, 235, 1060, 171), narrative, body, Paper);
            int main = 0;
            int side = 0;
            if (runtime.Quests != null)
            {
                foreach (QuestDefinition quest in runtime.Quests.Definitions)
                {
                    QuestProgress progress = runtime.Quests.GetProgress(quest.id);
                    if (progress != null && progress.status == QuestStatus.Completed)
                    {
                        if (quest.isMain) main++; else side++;
                    }
                }
            }
            Box(new Rect(100, 433, 1080, 83), new Color(.075f, .15f, .18f));
            string saveStatus = string.IsNullOrEmpty(runtime.ErrorMessage)
                ? "Votre choix final a été conservé dans la sauvegarde."
                : "La sauvegarde n'a pas pu être confirmée. Consultez le message affiché en haut.";
            Label(new Rect(121, 449, 1008, 49), "Temps : " + PlayTime() + "     •     Enquête : " + main + "/9     •     Histoires des habitants : " + side + "/5\n" + saveStatus, body, accent);
            if (Button(new Rect(100, 562, 322, 48), "Menu principal", true)) ReturnToMenu();
            if (Button(new Rect(446, 562, 300, 48), "Crédits"))
            {
                // Ending stays visible until the player returns to the menu; credits remain accessible there.
                ReturnToMenu();
                OpenPanel(Panel.Credits);
            }
            if (Button(new Rect(770, 562, 300, 48), "Quitter")) Confirm("Quitter RIVE 404 ?", Quit);
            Label(new Rect(102, 651, 1040, 27), "Merci d'avoir parcouru Rive.", small, Muted);
        }

        private string PlayTime()
        {
            double seconds = runtime.State != null ? Math.Max(0, runtime.State.playSeconds) : 0;
            var duration = TimeSpan.FromSeconds(Math.Min(seconds, TimeSpan.MaxValue.TotalSeconds - 1));
            return ((int)duration.TotalHours).ToString("00") + " h " + duration.Minutes.ToString("00") + " min";
        }

        private void DrawNotices()
        {
            if (panel != Panel.None && notices.Count > 0)
            {
                Box(new Rect(330, 668, 920, 35), Ink);
                Label(new Rect(346, 675, 884, 24), notices[notices.Count - 1].Text, small, Paper);
                return;
            }
            float y = 183;
            foreach (Notice notice in notices)
            {
                float height = Mathf.Clamp(small.CalcHeight(new GUIContent(notice.Text), 396) + 24, 45, 120);
                Box(new Rect(814, y, 436, height), Ink);
                Box(new Rect(814, y, 3, height), Cyan);
                Label(new Rect(832, y + 12, 399, height - 19), notice.Text, small, Paper);
                y += height + 8;
            }
        }

        private void Confirm(string text, Action action)
        {
            confirmText = text;
            confirmAction = action;
        }

        private void DrawConfirmation()
        {
            Box(new Rect(-1280, -720, 3840, 2160), new Color(.01f, .02f, .035f, .9f));
            Box(new Rect(315, 236, 650, 239), Ink);
            Box(new Rect(315, 236, 650, 4), Gold);
            Label(new Rect(340, 259, 600, 107), confirmText, body, Paper);
            if (Button(new Rect(340, 397, 282, 49), "Confirmer", true))
            {
                Action action = confirmAction;
                confirmAction = null;
                confirmText = null;
                action?.Invoke();
            }
            if (Button(new Rect(640, 397, 300, 49), "Annuler")) { confirmAction = null; confirmText = null; }
        }

        private void TogglePanel(Panel requested)
        {
            if (panel == requested) ClosePanel();
            else OpenPanel(requested);
        }

        private void OpenPanel(Panel requested)
        {
            if (requested == Panel.Settings || requested == Panel.Credits || requested == Panel.Saves)
                backPanel = panel;
            panel = requested;
            if (runtime.IsPlaying) runtime.Pause(true);
        }

        private void ClosePanel()
        {
            panel = Panel.None;
            if (runtime.IsPlaying) runtime.Pause(false);
        }

        private void Back()
        {
            if (panel == Panel.Settings)
            {
                settings.Save();
                settings.Apply(true);
                runtime.ApplyAudioSettings(settings.MusicVolume, settings.EffectsVolume);
            }
            if (panel == Panel.Settings || panel == Panel.Credits || panel == Panel.Saves)
            {
                Panel previous = backPanel;
                panel = previous;
                if (previous == Panel.None && runtime.IsPlaying) runtime.Pause(false);
            }
            else ClosePanel();
        }

        private void ReturnToMenu()
        {
            panel = Panel.None;
            confirmAction = null;
            runtime.ReturnToMenu();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            if (buttonIdle != null) Destroy(buttonIdle);
            if (buttonHover != null) Destroy(buttonHover);
            if (buttonActive != null) Destroy(buttonActive);
            if (primaryIdle != null) Destroy(primaryIdle);
            if (primaryHover != null) Destroy(primaryHover);
        }
    }
}
