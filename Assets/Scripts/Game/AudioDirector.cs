using UnityEngine;

namespace SaucerSiege.Game
{
    /// <summary>Cross-fades between the title and battle themes, plays effects, and keeps the volume settings.</summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        const string MusicKey = "SaucerSiege.MusicVolume";
        const string EffectsKey = "SaucerSiege.EffectsVolume";
        const string MutedKey = "SaucerSiege.Muted";
        const float MusicLevel = 0.5f;
        const int Voices = 8;

        AudioSource music;
        AudioSource[] voices;
        int nextVoice;
        AudioClip wanted;
        float fade = 1f;
        bool ducked;
        float[] lastPlayed;
        AudioClip[] lastClips;

        public bool Muted { get; private set; }
        /// <summary>0 to 1.</summary>
        public float MusicVolume { get; private set; } = 0.7f;
        /// <summary>0 to 1.</summary>
        public float EffectsVolume { get; private set; } = 0.8f;

        public void Init()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
            lastPlayed = new float[Voices * 4];
            lastClips = new AudioClip[Voices * 4];
            Muted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.7f);
            EffectsVolume = PlayerPrefs.GetFloat(EffectsKey, 0.8f);
            AudioListener.volume = Muted ? 0f : 1f;
        }

        public void PlayMusic(AudioClip clip) => wanted = clip;

        public void SetDucked(bool value) => ducked = value;

        public void ToggleMute()
        {
            Muted = !Muted;
            AudioListener.volume = Muted ? 0f : 1f;
            PlayerPrefs.SetInt(MutedKey, Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
            PlayerPrefs.Save();
        }

        public void SetEffectsVolume(float value)
        {
            EffectsVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(EffectsKey, EffectsVolume);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Plays a one-shot effect. The same clip is not restarted within <paramref name="minGap"/>
        /// seconds, so a dozen towers firing at once do not turn into noise.
        /// </summary>
        public void Play(AudioClip clip, float volume = 1f, float pitchJitter = 0.05f, float minGap = 0.04f)
        {
            if (clip == null || voices == null)
            {
                return;
            }
            float now = Time.unscaledTime;
            int slot = -1;
            for (int i = 0; i < lastClips.Length; i++)
            {
                if (lastClips[i] == clip)
                {
                    slot = i;
                    break;
                }
                if (slot < 0 && lastClips[i] == null)
                {
                    slot = i;
                }
            }
            if (slot >= 0 && lastClips[slot] == clip && now - lastPlayed[slot] < minGap)
            {
                return;
            }
            if (slot >= 0)
            {
                lastClips[slot] = clip;
                lastPlayed[slot] = now;
            }
            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            voice.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            voice.PlayOneShot(clip, volume * EffectsVolume);
        }

        void Update()
        {
            if (music == null)
            {
                return;
            }
            if (music.clip != wanted)
            {
                fade = Mathf.MoveTowards(fade, 0f, Time.unscaledDeltaTime * 2.5f);
                if (fade <= 0f || !music.isPlaying)
                {
                    music.clip = wanted;
                    if (wanted != null)
                    {
                        music.Play();
                    }
                }
            }
            else
            {
                fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime * 1.5f);
            }
            music.volume = MusicLevel * MusicVolume * fade * (ducked ? 0.45f : 1f);
        }
    }
}
