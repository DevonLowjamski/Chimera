using ProjectChimera.Core;
using ProjectChimera.Core.Interfaces;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Camera
{
    public class CameraService : ChimeraSystem, ICameraProvider, ICameraService
    {
        public UnityEngine.Camera main { get; private set; }
        public UnityEngine.Camera MainCamera => main;
        public bool IsInitialized { get; private set; }
        public CameraMode CurrentMode { get; private set; } = CameraMode.Free;

        private Transform _currentTarget;

        protected override void OnSystemStart()
        {
            // Primary: Try ServiceContainer resolution
            if (ServiceContainerFactory.Instance.TryGetService<UnityEngine.Camera>(out var serviceCamera))
            {
                main = serviceCamera;
                LogInfo("[CameraService] Using camera from ServiceContainer");
            }
            else
            {
                // Fallback: Standard Unity camera discovery
                main = UnityEngine.Camera.main;

                // Auto-register discovered camera in ServiceContainer for other systems
                if (main != null)
                {
                    ServiceContainerFactory.Instance.RegisterInstance<UnityEngine.Camera>(main);
                    LogInfo("[CameraService] Camera registered in ServiceContainer for system-wide access");
                }
            }

            if (main == null)
            {
                LogError("Could not find main camera");
            }

            IsInitialized = main != null;
        }

        protected override void OnSystemStop()
        {
            main = null;
            IsInitialized = false;
        }

        public void Initialize()
        {
            StartSystem();
        }

        public void SetMainCamera(UnityEngine.Camera camera)
        {
            main = camera;
            IsInitialized = camera != null;
        }

        public void SwitchToMode(CameraMode mode)
        {
            CurrentMode = mode;
        }

        public void SetTarget(Transform target)
        {
            _currentTarget = target;
        }

        public void MoveTo(Vector3 position, float duration = 1f)
        {
            if (main != null)
            {
                main.transform.position = position;
            }
        }

        public void LookAt(Transform target, float duration = 1f)
        {
            if (main != null && target != null)
            {
                main.transform.LookAt(target);
            }
        }

        public CameraState GetCurrentState()
        {
            if (main == null) return new CameraState();

            return new CameraState
            {
                Position = main.transform.position,
                Rotation = main.transform.rotation,
                FieldOfView = main.fieldOfView,
                Mode = CurrentMode,
                Target = _currentTarget
            };
        }

        public void ApplyCameraState(CameraState state)
        {
            if (main == null || state == null) return;

            main.transform.position = state.Position;
            main.transform.rotation = state.Rotation;
            main.fieldOfView = state.FieldOfView;
            CurrentMode = state.Mode;
            _currentTarget = state.Target;
        }
    }
}
