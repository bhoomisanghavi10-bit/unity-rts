using UnityEngine;

namespace KingdomsOfBharat.Audio
{
    // Central place every gameplay system reaches for a sound effect,
    // rather than each caller managing its own AudioSource. World-
    // positioned one-shots (attacks, deaths, gathering, construction) go
    // through AudioSource.PlayClipAtPoint - a static Unity API that spawns
    // and auto-destroys its own temporary GameObject, so nothing here
    // needs to track or clean up AudioSource instances. UI-anchored sounds
    // (select, move order, button clicks) have no meaningful world
    // position, so they go through one lazily-created, persistent 2D
    // AudioSource instead (spatialBlend 0 - always audible regardless of
    // camera distance).
    //
    // Clips are CC0 (Kenney.nl - RPG Audio, Impact Sounds, Interface
    // Sounds packs; https://kenney.nl, no attribution required) under
    // Resources/Audio/, loaded lazily and cached on first use.
    public static class SfxPlayer
    {
        private const float DefaultVolume = 0.6f;

        private static AudioSource _uiSource;

        private static AudioClip _select;
        private static AudioClip _move;
        private static AudioClip _uiClick;
        private static AudioClip _buildComplete;
        private static AudioClip _error;
        private static AudioClip _attackHit;
        private static AudioClip _unitDeath;
        private static AudioClip _buildingDestroyed;
        private static AudioClip _gather;

        public static void PlaySelect() => PlayUi(ref _select, "select");
        public static void PlayMove() => PlayUi(ref _move, "move");
        public static void PlayUiClick() => PlayUi(ref _uiClick, "uiClick");
        public static void PlayError() => PlayUi(ref _error, "error");

        public static void PlayAttackHit(Vector3 position) => PlayAtPoint(ref _attackHit, "attackHit", position);
        public static void PlayUnitDeath(Vector3 position) => PlayAtPoint(ref _unitDeath, "unitDeath", position);
        public static void PlayBuildingDestroyed(Vector3 position) => PlayAtPoint(ref _buildingDestroyed, "buildingDestroyed", position);
        public static void PlayBuildComplete(Vector3 position) => PlayAtPoint(ref _buildComplete, "buildComplete", position);
        public static void PlayGather(Vector3 position) => PlayAtPoint(ref _gather, "gather", position);

        private static void PlayAtPoint(ref AudioClip cached, string clipName, Vector3 position)
        {
            AudioClip clip = EnsureLoaded(ref cached, clipName);
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, DefaultVolume);
            }
        }

        private static void PlayUi(ref AudioClip cached, string clipName)
        {
            AudioClip clip = EnsureLoaded(ref cached, clipName);
            if (clip == null)
            {
                return;
            }

            EnsureUiSource();
            _uiSource.PlayOneShot(clip, DefaultVolume);
        }

        private static AudioClip EnsureLoaded(ref AudioClip cached, string clipName)
        {
            if (cached == null)
            {
                cached = Resources.Load<AudioClip>($"Audio/{clipName}");
                if (cached == null)
                {
                    Debug.LogWarning($"SfxPlayer: failed to load clip at Resources path 'Audio/{clipName}'");
                }
            }

            return cached;
        }

        private static void EnsureUiSource()
        {
            if (_uiSource != null)
            {
                return;
            }

            var go = new GameObject("SfxPlayer_UiSource");
            Object.DontDestroyOnLoad(go);
            _uiSource = go.AddComponent<AudioSource>();
            _uiSource.spatialBlend = 0f;
            _uiSource.playOnAwake = false;
        }
    }
}
