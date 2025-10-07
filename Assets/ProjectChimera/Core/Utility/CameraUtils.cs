using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Utility
{
    /// <summary>
    /// A static utility class containing advanced mathematical functions for camera operations.
    /// This helps keep the main camera controller focused on state and input management.
    /// </summary>
    public static class CameraUtils
    {
        /// <summary>
        /// Clamps an angle to the given range, handling wrapping around 360 degrees.
        /// </summary>
        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }

        /// <summary>
        /// Calculates a new camera position for orbiting around a target.
        /// </summary>
        public static Vector3 CalculateOrbitPosition(Vector3 targetPosition, float distance, float yaw, float pitch)
        {
            float yawRad = yaw * Mathf.Deg2Rad;
            float pitchRad = pitch * Mathf.Deg2Rad;

            Vector3 sphericalOffset = new Vector3(
                distance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
                distance * Mathf.Sin(pitchRad),
                distance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
            );

            return targetPosition + sphericalOffset;
        }

        /// <summary>
        /// Smoothly interpolates a value using an AnimationCurve or a default SmoothStep.
        /// </summary>
        public static float CalculateEasedInterpolation(float t, AnimationCurve curve = null)
        {
            if (curve != null)
            {
                return curve.Evaluate(t);
            }
            return Mathf.SmoothStep(0f, 1f, t);
        }

        /// <summary>
        /// Applies camera bounds with a soft-edge resistance zone for more natural movement.
        /// </summary>
        public static Vector3 ApplySoftBounds(Vector3 position, Vector3 boundsMin, Vector3 boundsMax, float softZone = 0.8f)
        {
            Vector3 center = (boundsMin + boundsMax) * 0.5f;
            Vector3 size = boundsMax - boundsMin;
            Vector3 softSize = size * softZone;

            Vector3 offset = position - center;

            // Apply soft clamping - resistance increases near bounds
            for (int i = 0; i < 3; i++)
            {
                float halfSoftSize = softSize[i] * 0.5f;
                float halfHardSize = size[i] * 0.5f;

                if (Mathf.Abs(offset[i]) > halfSoftSize)
                {
                    float excess = Mathf.Abs(offset[i]) - halfSoftSize;
                    float resistance = excess / (halfHardSize - halfSoftSize);
                    resistance = Mathf.Clamp01(resistance);

                    float damping = 1f - (resistance * resistance); // Quadratic resistance
                    offset[i] = Mathf.Sign(offset[i]) * (halfSoftSize + excess * damping);

                    // Hard clamp at absolute bounds
                    offset[i] = Mathf.Clamp(offset[i], -halfHardSize, halfHardSize);
                }
            }

            return center + offset;
        }
    }
}
