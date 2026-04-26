using UnityEngine;
using Object = UnityEngine.Object;

namespace ShatteredForge.SceneFlow
{
    /// <summary>
    /// Replaces flat stub cubes with readable primitive compositions (shop arch, forge, alchemy tower, dungeon gate).
    /// Primitives keep their colliders so <see cref="UnityEngine.CharacterController"/> cannot walk through walls.
    /// </summary>
    internal static class CampHubLandmarkDresser
    {
        private const string BuiltRootName = "CampHubBuiltVisuals";

        public static void Dress(Transform shopAnchor, Transform forgeAnchor, Transform alchemyAnchor, Transform dungeonAnchor)
        {
            var campCenter = Vector3.zero;
            if (shopAnchor != null)
            {
                BuildShop(shopAnchor, campCenter);
            }

            if (forgeAnchor != null)
            {
                BuildForge(forgeAnchor, campCenter);
            }

            if (alchemyAnchor != null)
            {
                BuildAlchemy(alchemyAnchor, campCenter);
            }

            if (dungeonAnchor != null)
            {
                BuildDungeonGate(dungeonAnchor, campCenter);
            }
        }

        private static void PrepareAnchor(Transform anchor, Vector3 campCenterXZ)
        {
            anchor.localScale = Vector3.one;
            var p = anchor.position;
            var target = new Vector3(campCenterXZ.x, p.y, campCenterXZ.z);
            var d = target - p;
            d.y = 0f;
            if (d.sqrMagnitude < 0.04f)
            {
                d = Vector3.forward;
            }

            anchor.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            StripStubPrimitives(anchor);
        }

        private static void StripStubPrimitives(Transform anchor)
        {
            var go = anchor.gameObject;
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null)
            {
                Object.Destroy(mf);
            }

            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Object.Destroy(mr);
            }

            var bx = go.GetComponent<BoxCollider>();
            if (bx != null)
            {
                Object.Destroy(bx);
            }

            var existing = anchor.Find(BuiltRootName);
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
            }
        }

        private static Transform CreateBuiltRoot(Transform anchor)
        {
            var go = new GameObject(BuiltRootName);
            go.transform.SetParent(anchor, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private static void Prim(
            PrimitiveType type,
            Transform parent,
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
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = false;
            }

            var meshCol = go.GetComponent<MeshCollider>();
            if (meshCol != null)
            {
                meshCol.convex = false;
            }

            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void BuildShop(Transform anchor, Vector3 camp)
        {
            PrepareAnchor(anchor, camp);
            var root = CreateBuiltRoot(anchor);

            Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.06f, 0.4f), new Vector3(4.2f, 0.12f, 3.4f), Quaternion.identity, CampHubProtoMaterials.StoneWarm);

            Prim(PrimitiveType.Cube, root, new Vector3(-1.35f, 1.15f, 0f), new Vector3(0.5f, 2.35f, 0.65f), Quaternion.identity, CampHubProtoMaterials.StoneWarm);
            Prim(PrimitiveType.Cube, root, new Vector3(1.35f, 1.15f, 0f), new Vector3(0.5f, 2.35f, 0.65f), Quaternion.identity, CampHubProtoMaterials.StoneWarm);

            Prim(PrimitiveType.Cube, root, new Vector3(0f, 2.28f, 0f), new Vector3(3.35f, 0.38f, 0.62f), Quaternion.identity, CampHubProtoMaterials.Wood);

            Prim(PrimitiveType.Cube, root, new Vector3(0f, 2.52f, 0f), new Vector3(2.6f, 0.22f, 0.55f), Quaternion.Euler(8f, 0f, 0f), CampHubProtoMaterials.TrimGold);

            Prim(PrimitiveType.Cube, root, new Vector3(0f, 1.05f, 0.55f), new Vector3(2.2f, 1.85f, 0.08f), Quaternion.identity, CampHubProtoMaterials.ClothTeal);

            Prim(PrimitiveType.Cylinder, root, new Vector3(-1.55f, 2.35f, 0f), new Vector3(0.35f, 0.12f, 0.35f), Quaternion.Euler(90f, 0f, 0f), CampHubProtoMaterials.TrimGold);
            Prim(PrimitiveType.Cylinder, root, new Vector3(1.55f, 2.35f, 0f), new Vector3(0.35f, 0.12f, 0.35f), Quaternion.Euler(90f, 0f, 0f), CampHubProtoMaterials.TrimGold);
        }

        private static void BuildForge(Transform anchor, Vector3 camp)
        {
            PrepareAnchor(anchor, camp);
            var root = CreateBuiltRoot(anchor);

            Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.85f, 0f), new Vector3(2.9f, 1.7f, 2.4f), Quaternion.identity, CampHubProtoMaterials.StoneDark);

            Prim(PrimitiveType.Cube, root, new Vector3(0.85f, 2.05f, -0.35f), new Vector3(0.55f, 1.35f, 0.55f), Quaternion.identity, CampHubProtoMaterials.MetalRust);

            Prim(PrimitiveType.Cube, root, new Vector3(-0.35f, 0.55f, 1.05f), new Vector3(1.1f, 0.35f, 0.75f), Quaternion.identity, CampHubProtoMaterials.MetalDark);

            Prim(PrimitiveType.Sphere, root, new Vector3(-0.2f, 0.62f, 1.12f), new Vector3(0.45f, 0.25f, 0.35f), Quaternion.identity, CampHubProtoMaterials.Ember);

            Prim(PrimitiveType.Cube, root, new Vector3(0.5f, 0.42f, 0.95f), new Vector3(0.55f, 0.18f, 0.45f), Quaternion.identity, CampHubProtoMaterials.MetalDark);
            Prim(PrimitiveType.Cube, root, new Vector3(0.5f, 0.58f, 0.95f), new Vector3(0.35f, 0.12f, 0.35f), Quaternion.identity, CampHubProtoMaterials.MetalRust);

            Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 2.55f, -0.2f), new Vector3(0.28f, 0.2f, 0.28f), Quaternion.identity, CampHubProtoMaterials.MetalDark);
        }

        private static void BuildAlchemy(Transform anchor, Vector3 camp)
        {
            PrepareAnchor(anchor, camp);
            var root = CreateBuiltRoot(anchor);

            Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 1f, 0f), new Vector3(1.55f, 2f, 1.55f), Quaternion.identity, CampHubProtoMaterials.StoneWarm);

            Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 2.15f, 0f), new Vector3(1.2f, 0.95f, 1.2f), Quaternion.identity, CampHubProtoMaterials.GlassGreen);

            Prim(PrimitiveType.Sphere, root, new Vector3(0f, 2.85f, 0f), new Vector3(1.35f, 0.55f, 1.35f), Quaternion.identity, CampHubProtoMaterials.GlassPurple);

            Prim(PrimitiveType.Cylinder, root, new Vector3(0.95f, 0.55f, 0.35f), new Vector3(0.22f, 0.55f, 0.22f), Quaternion.identity, CampHubProtoMaterials.GlassGreen);
            Prim(PrimitiveType.Cylinder, root, new Vector3(-0.85f, 0.48f, -0.45f), new Vector3(0.18f, 0.45f, 0.18f), Quaternion.identity, CampHubProtoMaterials.GlassPurple);

            Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.08f, 0.5f), new Vector3(1.8f, 0.12f, 1.2f), Quaternion.identity, CampHubProtoMaterials.Wood);
        }

        private static void BuildDungeonGate(Transform anchor, Vector3 camp)
        {
            PrepareAnchor(anchor, camp);
            var root = CreateBuiltRoot(anchor);

            for (var i = 0; i < 4; i++)
            {
                var z = -0.35f - i * 0.42f;
                Prim(PrimitiveType.Cube, root, new Vector3(0f, 0.12f + i * 0.11f, z), new Vector3(3.2f - i * 0.15f, 0.22f, 0.55f), Quaternion.identity, CampHubProtoMaterials.StoneDark);
            }

            Prim(PrimitiveType.Cube, root, new Vector3(-1.15f, 1.05f, 0.2f), new Vector3(0.45f, 2.1f, 0.55f), Quaternion.identity, CampHubProtoMaterials.StoneDark);
            Prim(PrimitiveType.Cube, root, new Vector3(1.15f, 1.05f, 0.2f), new Vector3(0.45f, 2.1f, 0.55f), Quaternion.identity, CampHubProtoMaterials.StoneDark);

            Prim(PrimitiveType.Cube, root, new Vector3(0f, 1.95f, 0.2f), new Vector3(2.45f, 0.4f, 0.5f), Quaternion.identity, CampHubProtoMaterials.StoneDark);

            Prim(PrimitiveType.Cube, root, new Vector3(0f, 1f, 0.35f), new Vector3(1.35f, 1.85f, 0.35f), Quaternion.identity, CampHubProtoMaterials.VoidDark);

            Prim(PrimitiveType.Cylinder, root, new Vector3(0f, 0.15f, 1.05f), new Vector3(0.5f, 0.06f, 0.5f), Quaternion.Euler(90f, 0f, 0f), CampHubProtoMaterials.TrimGold);
        }
    }
}
