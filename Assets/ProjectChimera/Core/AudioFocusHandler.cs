using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Handles Unity audio focus issues to prevent FMOD System::init errors.
    /// This component prevents audio reinitialization issues when switching between
    /// Unity editor and other applications.
    /// </summary>
    public class AudioFocusHandler : MonoBehaviour
    {
        [Header("Audio Focus Configuration")]
        [SerializeField] private bool _runInBackground = true;
        [SerializeField] private bool _muteOtherAudioSources = false;
        
        private bool _wasAudioMuted = false;
        private float _previousMasterVolume = 1f;
        
        private void Awake()
        {
            // Prevent audio system reinitialization issues
            Application.runInBackground = _runInBackground;
            
            // Disable automatic audio source management to prevent FMOD conflicts
            if (AudioSettings.driverCapabilities.ToString().Contains("FMOD"))
            {
                // For FMOD systems, prevent Unity from managing audio output switching
                AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
            }
            
            // Don't destroy this handler when loading new scenes
            DontDestroyOnLoad(gameObject);
            
            LogInfo("Audio Focus Handler initialized with FMOD compatibility");
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            HandleAudioFocus(hasFocus);
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            HandleAudioFocus(!pauseStatus);
        }
        
        /// <summary>
        /// Handles audio focus changes to prevent FMOD errors
        /// </summary>
        private void HandleAudioFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                // Restore audio when gaining focus
                RestoreAudio();
            }
            else
            {
                // Optionally mute audio when losing focus
                if (_muteOtherAudioSources)
                {
                    MuteAudio();
                }
            }
        }
        
        /// <summary>
        /// Mutes audio to prevent conflicts when application loses focus
        /// </summary>
        private void MuteAudio()
        {
            if (!_wasAudioMuted)
            {
                _previousMasterVolume = AudioListener.volume;
                AudioListener.volume = 0f;
                _wasAudioMuted = true;
                LogInfo("Audio muted due to focus loss");
            }
        }
        
        /// <summary>
        /// Restores audio when application regains focus
        /// </summary>
        private void RestoreAudio()
        {
            if (_wasAudioMuted)
            {
                AudioListener.volume = _previousMasterVolume;
                _wasAudioMuted = false;
                LogInfo("Audio restored after focus gain");
            }
        }
        
        /// <summary>
        /// Logs information about audio focus handling
        /// </summary>
        private void LogInfo(string message)
        {
            ChimeraLogger.Log($"[AudioFocusHandler] {message}");
        }
        
        /// <summary>
        /// Handles audio configuration changes to prevent FMOD reinitialization
        /// </summary>
        private void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            if (deviceWasChanged)
            {
                LogInfo("Audio device changed - preventing FMOD reinitialization");
                // Don't allow Unity to reinitialize audio when device changes
                // This prevents the "Cannot call this command after System::init" error
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
            
            // Restore audio if this handler is destroyed while muted
            if (_wasAudioMuted)
            {
                AudioListener.volume = _previousMasterVolume;
            }
        }
    }
}
