using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Audio
{
    /// <summary>
    /// SIMPLE: Basic audio data structures aligned with Project Chimera's audio system vision.
    /// Focuses on essential audio clip definitions for cultivation activities.
    /// </summary>

    /// <summary>
    /// Basic audio categories
    /// </summary>
    public enum AudioCategory
    {
        Effect,
        Ambient,
        UI,
        Voice
    }

    /// <summary>
    /// Basic audio state for different game modes
    /// </summary>
    public enum AudioState
    {
        Menu,
        Facility,
        Construction,
        Genetics
    }

    /// <summary>
    /// Simple audio clip data
    /// </summary>
    [System.Serializable]
    public class AudioClipData
    {
        public string ClipId;
        public string ClipName;
        public AudioClip AudioClip;
        public AudioCategory Category;
        public float DefaultVolume = 1f;
        public bool Loop = false;
    }

    /// <summary>
    /// Simple audio collection
    /// </summary>
    [System.Serializable]
    public class AudioCollection
    {
        public string CollectionName;
        public List<AudioClipData> AudioClips = new List<AudioClipData>();
        public AudioState AssociatedState;
    }

    /// <summary>
    /// Basic audio settings
    /// </summary>
    [System.Serializable]
    public class AudioSettings
    {
        public float MasterVolume = 1f;
        public float EffectsVolume = 1f;
        public float AmbientVolume = 1f;
        public float UIVolume = 1f;
        public bool AudioEnabled = true;
    }

    /// <summary>
    /// Simple audio manager for basic playback
    /// </summary>
    public static class SimpleAudioManager
    {
        private static readonly Dictionary<string, AudioClipData> _audioLibrary = new Dictionary<string, AudioClipData>();

        /// <summary>
        /// Register an audio clip
        /// </summary>
        public static void RegisterAudioClip(AudioClipData clipData)
        {
            if (clipData != null && !string.IsNullOrEmpty(clipData.ClipId))
            {
                _audioLibrary[clipData.ClipId] = clipData;
            }
        }

        /// <summary>
        /// Get an audio clip by ID
        /// </summary>
        public static AudioClipData GetAudioClip(string clipId)
        {
            return _audioLibrary.GetValueOrDefault(clipId);
        }

        /// <summary>
        /// Get all clips in a category
        /// </summary>
        public static List<AudioClipData> GetClipsByCategory(AudioCategory category)
        {
            return _audioLibrary.Values.Where(clip => clip.Category == category).ToList();
        }

        /// <summary>
        /// Clear all registered clips
        /// </summary>
        public static void ClearLibrary()
        {
            _audioLibrary.Clear();
        }

        /// <summary>
        /// Get library size
        /// </summary>
        public static int LibrarySize => _audioLibrary.Count;
    }
}
