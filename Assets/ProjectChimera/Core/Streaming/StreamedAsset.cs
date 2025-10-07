using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using ProjectChimera.Core.Memory;
using System;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Core.Streaming
{
    public class StreamedAsset
    {
        public string AssetKey;
        public Vector3 Position;
        public Core.StreamingPriority Priority;
        public string[] Tags;
        public Core.AssetLoadState LoadState;
        public object AssetHandle;
        public float RegistrationTime;
        public float LastAccessTime;
        public float DistanceFromCenter;
    }

    /// <summary>
    /// Asset load states
    /// </summary>
}
