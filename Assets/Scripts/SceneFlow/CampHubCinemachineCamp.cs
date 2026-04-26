using Unity.Cinemachine;
using UnityEngine;

namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Runtime wiring for a Cinemachine 3 "free look" style camp camera (orbital follow + rotation composer).
    /// Mouse axes are driven from <see cref="CampHubCameraRig"/> so execution order stays aligned with <see cref="Combat.SimplePlayerController"/>.
    /// </summary>
    internal sealed class CampHubCinemachineCamp
    {
        private readonly GameObject _root;
        private readonly CinemachineOrbitalFollow _orbit;

        private CampHubCinemachineCamp(GameObject root, CinemachineOrbitalFollow orbit)
        {
            _root = root;
            _orbit = orbit;
        }

        public static CampHubCinemachineCamp TryBuild(
            Camera unityCamera,
            Transform follow,
            float orbitRadius,
            float lookHeightLocalY,
            float fieldOfView,
            float pitchMin,
            float pitchMax,
            float initialYawDeg,
            float initialPitchDeg,
            NoiseSettings softNoiseProfile,
            float noiseAmplitude,
            float noiseFrequency,
            Transform hierarchyParent)
        {
            if (unityCamera == null || follow == null)
            {
                return null;
            }

            var brain = unityCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = unityCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            var root = new GameObject("CampHub_Cinemachine");

            var vcam = root.AddComponent<CinemachineCamera>();
            vcam.Priority = 20;
            vcam.Follow = follow;
            vcam.LookAt = follow;
            var lens = vcam.Lens;
            lens.FieldOfView = fieldOfView;
            vcam.Lens = lens;

            var orbit = root.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = Mathf.Max(0.5f, orbitRadius);
            orbit.TargetOffset = new Vector3(0f, lookHeightLocalY, 0f);

            var h = orbit.HorizontalAxis;
            h.Range = new Vector2(-180f, 180f);
            h.Wrap = true;
            h.Value = initialYawDeg;
            var hRec = h.Recentering;
            hRec.Enabled = false;
            h.Recentering = hRec;
            orbit.HorizontalAxis = h;

            var v = orbit.VerticalAxis;
            v.Range = new Vector2(pitchMin, pitchMax);
            v.Value = initialPitchDeg;
            var vRec = v.Recentering;
            vRec.Enabled = false;
            v.Recentering = vRec;
            orbit.VerticalAxis = v;

            var rad = orbit.RadialAxis;
            rad.Center = 1f;
            rad.Range = new Vector2(1f, 1f);
            rad.Value = 1f;
            var radRec = rad.Recentering;
            radRec.Enabled = false;
            rad.Recentering = radRec;
            orbit.RadialAxis = rad;

            var composer = root.AddComponent<CinemachineRotationComposer>();
            composer.TargetOffset = new Vector3(0f, lookHeightLocalY, 0f);
            composer.Damping = Vector2.zero;
            composer.CenterOnActivate = true;

            if (softNoiseProfile != null)
            {
                var perlin = root.AddComponent<CinemachineBasicMultiChannelPerlin>();
                perlin.NoiseProfile = softNoiseProfile;
                perlin.AmplitudeGain = noiseAmplitude;
                perlin.FrequencyGain = noiseFrequency;
            }

            if (hierarchyParent != null)
            {
                root.transform.SetParent(hierarchyParent, false);
            }

            return new CampHubCinemachineCamp(root, orbit);
        }

        public void SyncAxes(float yawDeg, float pitchDeg)
        {
            var h = _orbit.HorizontalAxis;
            h.Value = yawDeg;
            _orbit.HorizontalAxis = h;

            var v = _orbit.VerticalAxis;
            v.Value = pitchDeg;
            _orbit.VerticalAxis = v;
        }

        public void DestroyRig()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
            }
        }
    }
}
