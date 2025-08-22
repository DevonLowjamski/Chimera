using UnityEngine;
using ProjectChimera.Data.Camera;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// Handles camera state persistence to/from PlayerPrefs and save systems.
    /// Separated from CameraStateManager to maintain modular architecture.
    /// </summary>
    public static class CameraStatePersistence
    {
        // PlayerPrefs keys
        private const string PREFS_PREFIX = "CameraState_";
        private const string KEY_POS_X = PREFS_PREFIX + "PosX";
        private const string KEY_POS_Y = PREFS_PREFIX + "PosY";
        private const string KEY_POS_Z = PREFS_PREFIX + "PosZ";
        private const string KEY_ROT_X = PREFS_PREFIX + "RotX";
        private const string KEY_ROT_Y = PREFS_PREFIX + "RotY";
        private const string KEY_ROT_Z = PREFS_PREFIX + "RotZ";
        private const string KEY_ROT_W = PREFS_PREFIX + "RotW";
        private const string KEY_FOV = PREFS_PREFIX + "FOV";
        private const string KEY_LEVEL = PREFS_PREFIX + "Level";
        private const string KEY_USER_CONTROL = PREFS_PREFIX + "UserControl";

        /// <summary>
        /// Save camera state to PlayerPrefs
        /// </summary>
        public static void SaveState(CameraStateManager.CameraSnapshot state)
        {
            PlayerPrefs.SetFloat(KEY_POS_X, state.position.x);
            PlayerPrefs.SetFloat(KEY_POS_Y, state.position.y);
            PlayerPrefs.SetFloat(KEY_POS_Z, state.position.z);
            PlayerPrefs.SetFloat(KEY_ROT_X, state.rotation.x);
            PlayerPrefs.SetFloat(KEY_ROT_Y, state.rotation.y);
            PlayerPrefs.SetFloat(KEY_ROT_Z, state.rotation.z);
            PlayerPrefs.SetFloat(KEY_ROT_W, state.rotation.w);
            PlayerPrefs.SetFloat(KEY_FOV, state.fieldOfView);
            PlayerPrefs.SetInt(KEY_LEVEL, (int)state.cameraLevel);
            PlayerPrefs.SetInt(KEY_USER_CONTROL, state.userControlActive ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load camera state from PlayerPrefs
        /// </summary>
        public static CameraStateManager.CameraSnapshot LoadState(Vector3 defaultPosition, Vector3 defaultRotation, float defaultFOV, CameraLevel defaultLevel)
        {
            if (!HasSavedState())
            {
                return CreateDefaultSnapshot(defaultPosition, defaultRotation, defaultFOV, defaultLevel);
            }

            var state = new CameraStateManager.CameraSnapshot
            {
                position = new Vector3(
                    PlayerPrefs.GetFloat(KEY_POS_X, defaultPosition.x),
                    PlayerPrefs.GetFloat(KEY_POS_Y, defaultPosition.y),
                    PlayerPrefs.GetFloat(KEY_POS_Z, defaultPosition.z)
                ),
                rotation = new Quaternion(
                    PlayerPrefs.GetFloat(KEY_ROT_X, 0),
                    PlayerPrefs.GetFloat(KEY_ROT_Y, 0),
                    PlayerPrefs.GetFloat(KEY_ROT_Z, 0),
                    PlayerPrefs.GetFloat(KEY_ROT_W, 1)
                ),
                fieldOfView = PlayerPrefs.GetFloat(KEY_FOV, defaultFOV),
                cameraLevel = (CameraLevel)PlayerPrefs.GetInt(KEY_LEVEL, (int)defaultLevel),
                userControlActive = PlayerPrefs.GetInt(KEY_USER_CONTROL, 1) == 1,
                timestamp = Time.time,
                focusPosition = Vector3.zero,
                focusTargetName = "",
                isTransitioning = false
            };

            return state;
        }

        /// <summary>
        /// Check if saved state exists
        /// </summary>
        public static bool HasSavedState()
        {
            return PlayerPrefs.HasKey(KEY_POS_X);
        }

        /// <summary>
        /// Clear saved state
        /// </summary>
        public static void ClearSavedState()
        {
            PlayerPrefs.DeleteKey(KEY_POS_X);
            PlayerPrefs.DeleteKey(KEY_POS_Y);
            PlayerPrefs.DeleteKey(KEY_POS_Z);
            PlayerPrefs.DeleteKey(KEY_ROT_X);
            PlayerPrefs.DeleteKey(KEY_ROT_Y);
            PlayerPrefs.DeleteKey(KEY_ROT_Z);
            PlayerPrefs.DeleteKey(KEY_ROT_W);
            PlayerPrefs.DeleteKey(KEY_FOV);
            PlayerPrefs.DeleteKey(KEY_LEVEL);
            PlayerPrefs.DeleteKey(KEY_USER_CONTROL);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Validate state data for integrity
        /// </summary>
        public static bool ValidateState(CameraStateManager.CameraSnapshot state)
        {
            // Validate position
            if (float.IsNaN(state.position.x) || float.IsNaN(state.position.y) || float.IsNaN(state.position.z))
                return false;

            if (state.position.magnitude > 10000f) // Arbitrary large distance check
                return false;

            // Validate rotation
            if (float.IsNaN(state.rotation.x) || float.IsNaN(state.rotation.y) || 
                float.IsNaN(state.rotation.z) || float.IsNaN(state.rotation.w))
                return false;

            // Validate field of view
            if (state.fieldOfView < 5f || state.fieldOfView > 180f)
                return false;

            return true;
        }

        /// <summary>
        /// Create default snapshot with given parameters
        /// </summary>
        private static CameraStateManager.CameraSnapshot CreateDefaultSnapshot(Vector3 position, Vector3 rotation, float fov, CameraLevel level)
        {
            return new CameraStateManager.CameraSnapshot
            {
                position = position,
                rotation = Quaternion.Euler(rotation),
                fieldOfView = fov,
                cameraLevel = level,
                focusPosition = Vector3.zero,
                focusTargetName = "",
                timestamp = Time.time,
                isTransitioning = false,
                userControlActive = true
            };
        }
    }
}