using ProjectChimera.Core;
using UnityEngine;

namespace ProjectChimera.Systems.Camera
{
    public class CameraService : ChimeraSystem, ICameraProvider
    {
        public UnityEngine.Camera main { get; private set; }

        protected override void OnSystemInitialize()
        {
            main = UnityEngine.Camera.main;
            if (main == null)
            {
                main = FindObjectOfType<UnityEngine.Camera>();
            }

            if (main == null)
            {
                LogError("Could not find main camera");
            }
        }

        protected override void OnSystemShutdown()
        {
            main = null;
        }
    }
}
