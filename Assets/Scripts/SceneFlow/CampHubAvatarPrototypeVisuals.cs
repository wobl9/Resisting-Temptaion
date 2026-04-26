using UnityEngine;
using Object = UnityEngine.Object;

namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Camp-only readable hero: torso/head, warm chest plate toward +Z (forward), darker pack on back, light arm sway + bob.
    /// </summary>
    public sealed class CampHubAvatarPrototypeVisuals : MonoBehaviour
    {
        [SerializeField] private float breathHz = 2.15f;
        [SerializeField] private float breathAmplitude = 0.03f;
        [SerializeField] private float armSwingHz = 2.65f;
        [SerializeField] private float armSwingDegrees = 14f;

        private Transform _bobRoot;
        private Transform _armL;
        private Transform _armR;
        private bool _built;

        public static void EnsureOn(GameObject playerRoot)
        {
            if (playerRoot == null)
            {
                return;
            }

            if (playerRoot.GetComponent<CampHubAvatarPrototypeVisuals>() == null)
            {
                playerRoot.AddComponent<CampHubAvatarPrototypeVisuals>();
            }
        }

        private void Awake()
        {
            BuildIfNeeded();
        }

        private void LateUpdate()
        {
            if (_bobRoot == null)
            {
                return;
            }

            var t = Time.time;
            var bob = Mathf.Sin(t * breathHz * Mathf.PI * 2f) * breathAmplitude;
            _bobRoot.localPosition = new Vector3(0f, bob, 0f);

            var s = Mathf.Sin(t * armSwingHz * Mathf.PI * 2f);
            if (_armL != null)
            {
                _armL.localRotation = Quaternion.Euler(s * armSwingDegrees, 0f, 22f);
            }

            if (_armR != null)
            {
                _armR.localRotation = Quaternion.Euler(-s * armSwingDegrees, 0f, -22f);
            }
        }

        private void BuildIfNeeded()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            var root = transform;

            var omf = root.GetComponent<MeshFilter>();
            if (omf != null)
            {
                Object.Destroy(omf);
            }

            var omr = root.GetComponent<MeshRenderer>();
            if (omr != null)
            {
                Object.Destroy(omr);
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var ch = root.GetChild(i);
                if (ch.name == "AvatarVisual" || ch.name == "CampProtoAvatar")
                {
                    Object.Destroy(ch.gameObject);
                }
            }

            var rig = new GameObject("CampProtoAvatar");
            rig.transform.SetParent(root, false);
            rig.transform.localPosition = Vector3.zero;
            rig.transform.localRotation = Quaternion.identity;
            rig.transform.localScale = Vector3.one;
            _bobRoot = rig.transform;

            AddPrim(rig.transform, PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.78f, 1f, 0.78f), Quaternion.identity, CampHubProtoMaterials.ClothHero);
            AddPrim(rig.transform, PrimitiveType.Sphere, new Vector3(0f, 1.52f, 0f), new Vector3(0.36f, 0.36f, 0.36f), Quaternion.identity, CampHubProtoMaterials.Skin);
            AddPrim(rig.transform, PrimitiveType.Cube, new Vector3(0f, 1.02f, 0.26f), new Vector3(0.5f, 0.34f, 0.1f), Quaternion.identity, CampHubProtoMaterials.AccentFront);
            AddPrim(rig.transform, PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.24f), new Vector3(0.52f, 0.4f, 0.12f), Quaternion.identity, CampHubProtoMaterials.MetalDark);

            var shoulder = new GameObject("Shoulders").transform;
            shoulder.SetParent(rig.transform, false);
            shoulder.localPosition = new Vector3(0f, 1.22f, 0f);
            shoulder.localRotation = Quaternion.identity;
            shoulder.localScale = Vector3.one;

            _armL = CreateArm(shoulder, true);
            _armR = CreateArm(shoulder, false);
        }

        private static Transform CreateArm(Transform shoulder, bool left)
        {
            var side = left ? -1f : 1f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(shoulder, false);
            go.transform.localPosition = new Vector3(side * 0.4f, -0.06f, 0f);
            go.transform.localScale = new Vector3(0.1f, 0.48f, 0.1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, side * 72f);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = CampHubProtoMaterials.ClothHero;
            return go.transform;
        }

        private static void AddPrim(
            Transform parent,
            PrimitiveType type,
            Vector3 localPos,
            Vector3 localScale,
            Quaternion localRot,
            Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.transform.localRotation = localRot;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
