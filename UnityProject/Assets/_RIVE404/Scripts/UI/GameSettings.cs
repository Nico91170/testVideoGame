using UnityEngine;
using Rive404.Player;

namespace Rive404.UI
{
    /// <summary>Local preferences, deliberately separate from campaign saves.</summary>
    public sealed class GameSettings
    {
        private const string Prefix = "rive404.preferences.";
        public float Volume = .8f;
        public float MusicVolume = .7f;
        public float EffectsVolume = .8f;
        public float Sensitivity = .15f;
        public int Quality = 1;
        public bool VSync = true;
        public int FrameLimit = 60;
        public int Width = 1280;
        public int Height = 720;
        public bool Fullscreen;
        public bool Azerty = true;

        public static GameSettings Load()
        {
            return new GameSettings
            {
                Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "volume", .8f)),
                MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "music", .7f)),
                EffectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "effects", .8f)),
                Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "sensitivity", .15f), .05f, .5f),
                Quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "quality", 1), 0, 2),
                VSync = PlayerPrefs.GetInt(Prefix + "vsync", 1) != 0,
                FrameLimit = NormalizeFrameLimit(PlayerPrefs.GetInt(Prefix + "fps", 60)),
                Width = Mathf.Max(640, PlayerPrefs.GetInt(Prefix + "width", 1280)),
                Height = Mathf.Max(360, PlayerPrefs.GetInt(Prefix + "height", 720)),
                Fullscreen = PlayerPrefs.GetInt(Prefix + "fullscreen", 0) != 0,
                Azerty = PlayerPrefs.GetInt(Prefix + "azerty", 1) != 0
            };
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(Prefix + "volume", Volume);
            PlayerPrefs.SetFloat(Prefix + "music", MusicVolume);
            PlayerPrefs.SetFloat(Prefix + "effects", EffectsVolume);
            PlayerPrefs.SetFloat(Prefix + "sensitivity", Sensitivity);
            PlayerPrefs.SetInt(Prefix + "quality", Quality);
            PlayerPrefs.SetInt(Prefix + "vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "fps", FrameLimit);
            PlayerPrefs.SetInt(Prefix + "width", Width);
            PlayerPrefs.SetInt(Prefix + "height", Height);
            PlayerPrefs.SetInt(Prefix + "fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "azerty", Azerty ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Apply(bool applyDisplay)
        {
            AudioListener.volume = Mathf.Clamp01(Volume);
            int count = QualitySettings.names.Length;
            if (count > 0) QualitySettings.SetQualityLevel(Mathf.Min(Quality, count - 1), true);
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FrameLimit;
            ApplyCamera();
            if (applyDisplay)
                Screen.SetResolution(Width, Height, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        public void ApplyCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            ThirdPersonCamera controller = camera.GetComponent<ThirdPersonCamera>();
            if (controller != null) controller.Sensitivity = Sensitivity;
        }

        public bool HasSavedDisplay => PlayerPrefs.HasKey(Prefix + "width");

        private static int NormalizeFrameLimit(int value)
        {
            return value == 30 || value == 60 || value == 120 || value == -1 ? value : 60;
        }
    }
}
