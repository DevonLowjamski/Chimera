using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Save.Structures;

namespace ProjectChimera.Data.Save.Structures
{
    public class UIStateDTO
    {
        public System.DateTime SaveTimestamp;
        public string SaveVersion;
        public bool EnableUISystem;
        public UIModeStateDTO UIModeState;
        public UnityEngine.Vector3 CameraPosition;
        public UnityEngine.Vector3 CameraRotation;
        public float ZoomLevel;
        public bool IsPaused;
        public System.DateTime LastUpdate;
    }

    /// <summary>
    /// UI Mode state data
    /// </summary>
    [System.Serializable]
    public class UIModeStateDTO
    {
        public string CurrentMode;
        public List<string> AvailableModes = new List<string>();
        public System.DateTime LastModeChange;
    }
}
