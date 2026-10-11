using UnityEngine;

namespace Rive404.Runtime
{
    public sealed class AudioDirector : MonoBehaviour
    {
        private AudioSource music;
        private AudioSource effects;
        private AudioSource ambience;
        private int currentDistrict = -1;
        public void Initialize()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = .28f;
            effects = gameObject.AddComponent<AudioSource>();
            effects.volume = .65f;
            ambience = gameObject.AddComponent<AudioSource>();
            ambience.loop = true;
            ambience.volume = .2f;
            music.clip = Resources.Load<AudioClip>("Audio/Theme");
            if (music.clip != null) music.Play();
        }
        public void SetDistrict(int district)
        {
            if (district == currentDistrict || ambience == null) return;
            currentDistrict = district;
            string clip = district == 0 ? "PortAmbience" : district == 1 ? "NeonAmbience" : "CliffsAmbience";
            ambience.clip = Resources.Load<AudioClip>("Audio/" + clip);
            if (ambience.clip != null) ambience.Play();
        }
        public void SetLevels(float musicLevel, float effectsLevel)
        {
            if (music != null) music.volume = .35f * Mathf.Clamp01(musicLevel);
            if (ambience != null) ambience.volume = .28f * Mathf.Clamp01(musicLevel);
            if (effects != null) effects.volume = .65f * Mathf.Clamp01(effectsLevel);
        }
        public bool Owns(AudioSource source) { return source == music || source == ambience || source == effects; }
        public void Play(string clip)
        {
            var sound = Resources.Load<AudioClip>("Audio/" + clip);
            if (effects != null && sound != null) effects.PlayOneShot(sound);
        }
    }
}
