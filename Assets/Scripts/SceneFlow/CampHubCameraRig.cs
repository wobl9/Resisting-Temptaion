using ShatteredForge.Combat;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Camp camera: Gothic-style mouse look (yaw rotates hero, pitch tilts camera), third-person offset, atmosphere.
    /// Optional Cinemachine 3 orbital + rotation composer (free look) with soft Perlin noise when a noise profile is assigned.
    /// </summary>
    [DefaultExecutionOrder(-35)]
    public sealed class CampHubCameraRig : MonoBehaviour
    {
        [Header("Driver")]
        [Tooltip("Use Cinemachine 3 (orbital follow + rotation composer). If false, legacy manual camera path.")]
        [SerializeField] private bool useCinemachine = true;
        [Tooltip("Optional; assign e.g. a hand-held noise profile from the Cinemachine package samples. Leave empty to skip shake.")]
        [SerializeField] private NoiseSettings cinemachineSoftNoiseProfile;
        [SerializeField] [Min(0f)] private float cinemachineNoiseAmplitude = 0.22f;
        [SerializeField] [Min(0f)] private float cinemachineNoiseFrequency = 0.1f;

        [Header("Follow / orbit")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Camera targetCamera;
        [Tooltip("Offset in camera rig space: pitch then yaw applied (x side, y up, z back).")]
        [SerializeField] private Vector3 followOffsetLocal = new(0.35f, 2.15f, -5.4f);

        [Tooltip("World height above hero root: orbit sphere center and look target (same point — hero stays in screen center while orbiting).")]
        [SerializeField] [Min(0.5f)] private float lookHeight = 1.25f;
        [Tooltip("Lag of orbit pivot when the hero moves (0 = rigid follow). Orbit radius is always |followOffsetLocal|; mouse spin does not shorten distance.")]
        [SerializeField] [Min(0f)] private float pivotSmoothTime = 0.18f;
        [SerializeField] [Range(40f, 75f)] private float fieldOfView = 52f;

        [Header("Mouse look")]
        [SerializeField] private float mouseSensitivityX = 0.14f;
        [SerializeField] private float mouseSensitivityY = 0.12f;
        [SerializeField] private float pitchMin = -18f;
        [SerializeField] private float pitchMax = 42f;

        [Header("Atmosphere")]
        [SerializeField] private bool applyFogAndClearColor = true;
        [SerializeField] private Color horizonColor = new(0.11f, 0.09f, 0.13f, 1f);
        [SerializeField] private float fogStart = 12f;
        [SerializeField] private float fogEnd = 48f;

        [Header("Sky placeholder")]
        [SerializeField] private Material skyDomeMaterial;
        [SerializeField] private float skyDomeRadius = 95f;
        [SerializeField] private Vector3 skyDomeCenter = new(0f, 28f, 0f);

        private Vector3 _pivotSmoothed;
        private Vector3 _pivotVel;
        private bool _pivotInited;
        private bool _skySpawned;
        private bool _atmosphereApplied;
        private float _yawDeg;
        private float _pitchDeg;
        private bool _anglesInitialized;
        private bool _lookSuspendedFromUi;
        private CampHubCinemachineCamp _cinemachineCamp;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                targetCamera.fieldOfView = fieldOfView;
            }

            if (applyFogAndClearColor)
            {
                _atmosphereApplied = true;
                RenderSettings.fog = true;
                RenderSettings.fogColor = horizonColor;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = fogStart;
                RenderSettings.fogEndDistance = fogEnd;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = horizonColor * 1.35f;

                if (targetCamera != null)
                {
                    targetCamera.clearFlags = CameraClearFlags.SolidColor;
                    targetCamera.backgroundColor = horizonColor;
                }
            }

            EnsureSkyDome();
        }

        private void Start()
        {
            ResolveFollowTarget();
            ApplyCampMovementOnPlayer();
            InitAnglesIfNeeded();
            if (followTarget != null && !_pivotInited)
            {
                _pivotSmoothed = OrbitPivotWorld(followTarget.position);
                _pivotInited = true;
            }

            if (!_lookSuspendedFromUi)
            {
                LockCursor();
            }

            if (useCinemachine && followTarget != null && targetCamera != null)
            {
                _cinemachineCamp = CampHubCinemachineCamp.TryBuild(
                    targetCamera,
                    followTarget,
                    followOffsetLocal.magnitude,
                    lookHeight,
                    fieldOfView,
                    pitchMin,
                    pitchMax,
                    _yawDeg,
                    _pitchDeg,
                    cinemachineSoftNoiseProfile,
                    cinemachineNoiseAmplitude,
                    cinemachineNoiseFrequency,
                    transform);
                if (_cinemachineCamp == null)
                {
                    Debug.LogWarning($"{nameof(CampHubCameraRig)}: Cinemachine setup failed; check console / package install.");
                }
            }
        }

        private void OnDestroy()
        {
            _cinemachineCamp?.DestroyRig();
            if (_atmosphereApplied)
            {
                RenderSettings.fog = false;
            }
        }

        /// <summary>
        /// When UI (stub panel) is open: free cursor, suspend mouse orbit.
        /// </summary>
        public void SetLookFromUiSuppressed(bool suppressed)
        {
            _lookSuspendedFromUi = suppressed;
            if (suppressed)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                LockCursor();
            }
        }

        /// <summary>
        /// Escape when no modal UI: unlock mouse for OS / alt-tab convenience; press again or click game to lock.
        /// </summary>
        public void ToggleCursorLock()
        {
            if (_lookSuspendedFromUi)
            {
                return;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                LockCursor();
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (targetCamera == null)
            {
                return;
            }

            ResolveFollowTarget();
            if (followTarget == null)
            {
                return;
            }

            if (!_anglesInitialized)
            {
                InitAnglesIfNeeded();
            }

            if (!_lookSuspendedFromUi &&
                Cursor.lockState == CursorLockMode.Locked &&
                Mouse.current != null)
            {
                var d = Mouse.current.delta.ReadValue();
                _yawDeg += d.x * mouseSensitivityX;
                _pitchDeg -= d.y * mouseSensitivityY;
                _pitchDeg = Mathf.Clamp(_pitchDeg, pitchMin, pitchMax);
            }

            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                Cursor.lockState != CursorLockMode.Locked &&
                !_lookSuspendedFromUi)
            {
                LockCursor();
            }

            followTarget.rotation = Quaternion.Euler(0f, _yawDeg, 0f);
            var ctrl = followTarget.GetComponent<SimplePlayerController>();
            ctrl?.SyncPlanarFacingFromTransform();

            if (_cinemachineCamp != null)
            {
                _cinemachineCamp.SyncAxes(_yawDeg, _pitchDeg);
                return;
            }

            var orbit = Quaternion.Euler(_pitchDeg, _yawDeg, 0f);
            var pivotTarget = OrbitPivotWorld(followTarget.position);
            if (pivotSmoothTime > 0.0001f)
            {
                _pivotSmoothed = Vector3.SmoothDamp(_pivotSmoothed, pivotTarget, ref _pivotVel, pivotSmoothTime, Mathf.Infinity, Time.deltaTime);
            }
            else
            {
                _pivotSmoothed = pivotTarget;
            }

            // Rigid orbit around character center; look at same pivot so the hero stays in the middle of the view.
            var arm = orbit * followOffsetLocal;
            var desiredCamPos = _pivotSmoothed + arm;

            targetCamera.transform.position = desiredCamPos;

            // No rotation smoothing: Slerp lets the aim lag behind the orbit and the hero drifts off-center.
            var toLook = pivotTarget - targetCamera.transform.position;
            if (toLook.sqrMagnitude > 0.0001f)
            {
                targetCamera.transform.rotation = Quaternion.LookRotation(toLook.normalized, Vector3.up);
            }
        }

        private Vector3 OrbitPivotWorld(Vector3 heroRoot)
        {
            return heroRoot + Vector3.up * lookHeight;
        }

        private void ResolveFollowTarget()
        {
            if (followTarget != null)
            {
                return;
            }

            var player = FindFirstObjectByType<SimplePlayerController>();
            if (player != null)
            {
                followTarget = player.transform;
            }
        }

        private void InitAnglesIfNeeded()
        {
            if (_anglesInitialized || followTarget == null)
            {
                return;
            }

            _yawDeg = followTarget.eulerAngles.y;
            var flatZ = Mathf.Abs(followOffsetLocal.z);
            var y = followOffsetLocal.y;
            _pitchDeg = flatZ > 0.01f
                ? Mathf.Clamp(Mathf.Atan2(y, flatZ) * Mathf.Rad2Deg, pitchMin, pitchMax)
                : 12f;
            _anglesInitialized = true;
        }

        private void ApplyCampMovementOnPlayer()
        {
            var p = FindFirstObjectByType<SimplePlayerController>();
            p?.ConfigureForCampHub();
        }

        private void EnsureSkyDome()
        {
            if (_skySpawned || skyDomeMaterial == null)
            {
                return;
            }

            if (GameObject.Find("CampSkyDome") != null)
            {
                _skySpawned = true;
                return;
            }

            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = "CampSkyDome";
            dome.transform.SetPositionAndRotation(skyDomeCenter, Quaternion.identity);
            dome.transform.localScale = Vector3.one * (skyDomeRadius * 2f);
            Object.Destroy(dome.GetComponent<SphereCollider>());

            var rend = dome.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.sharedMaterial = skyDomeMaterial;
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }

            _skySpawned = true;
        }
    }
}
