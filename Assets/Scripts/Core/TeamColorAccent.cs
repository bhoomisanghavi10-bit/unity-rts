using UnityEngine;

namespace KingdomsOfBharat.Core
{
    // Wave 5 item 29: attaches a small flat-colored cloth banner/pennant
    // accent to a unit or building, colored by FactionId (see TeamColor) -
    // the AoE convention of a player-color accent layered on top of civ
    // identity. No painted accent-region mask exists anywhere in this
    // project's models/shaders (confirmed by direct investigation before
    // planning this item) - real per-pixel masking would need new
    // texture/shader authoring, out of scope for a code-only session. This
    // uses discrete decorative geometry instead (a hand-built quad mesh,
    // same idiom ProceduralBuildingFactory.BuildPyramidMesh/RallyPoint's
    // own flag cylinder already use), which needs zero new art.
    public static class TeamColorAccent
    {
        private const string AccentName = "TeamColorAccent";
        private static readonly Color PoleColor = new Color(0.32f, 0.22f, 0.14f);

        // Buildings: a hanging banner near one roof corner, sized off the
        // building's own final world bounds (already computed generically
        // by BuildingModelFactory.AlignBaseToGround for all 15 building
        // types) - no per-building-type special-casing needed. Parented
        // under the "Visual" child (destroyed/rebuilt wholesale by
        // BuildingModelFactory.Refresh on every Age-up re-skin), so the
        // banner is automatically cleaned up and re-created alongside it -
        // no separate cleanup path required.
        public static void AttachToBuilding(Transform visualRoot, Bounds worldBounds, FactionId faction)
        {
            float height = Mathf.Clamp(worldBounds.size.y * 0.3f, 0.5f, 1.6f);
            float width = Mathf.Clamp(worldBounds.size.x * 0.12f, 0.25f, 0.6f);

            GameObject banner = BuildFlagQuad(AccentName, width, height, TeamColor.For(faction));
            banner.transform.position = new Vector3(
                worldBounds.center.x + worldBounds.extents.x * 0.55f,
                worldBounds.max.y,
                worldBounds.max.z - width * 0.5f);
            banner.transform.SetParent(visualRoot, worldPositionStays: true);
            CompensateParentScale(banner.transform);
        }

        // Boats: same idea as buildings, near the mast/bow.
        public static void AttachToBoat(Transform visualRoot, Bounds worldBounds, FactionId faction)
        {
            float height = Mathf.Clamp(worldBounds.size.y * 0.4f, 0.3f, 0.9f);
            float width = Mathf.Clamp(worldBounds.size.x * 0.25f, 0.15f, 0.35f);

            GameObject banner = BuildFlagQuad(AccentName, width, height, TeamColor.For(faction));
            banner.transform.position = new Vector3(
                worldBounds.center.x,
                worldBounds.max.y,
                worldBounds.center.z);
            banner.transform.SetParent(visualRoot, worldPositionStays: true);
            CompensateParentScale(banner.transform);
        }

        // Humanoid/generic-rig units: a small pole+pennant mounted behind
        // the unit. Sockets to the Humanoid Avatar's Spine bone when one
        // exists (every HumanModelFactory-spawned unit); falls back to
        // parenting directly under the model root otherwise, which covers
        // the 2 War Elephant factories (a generic, non-Humanoid rig -
        // ElephantAnimationDriver, not AnimationDriver).
        public static void AttachToHumanoid(GameObject model, FactionId faction)
        {
            Transform socket = null;
            if (model.TryGetComponent(out Animator animator) && animator.isHuman)
            {
                socket = animator.GetBoneTransform(HumanBodyBones.Spine);
            }
            if (socket == null)
            {
                socket = model.transform;
            }

            var wrapper = new GameObject(AccentName);
            wrapper.transform.SetParent(socket, false);
            wrapper.transform.localPosition = new Vector3(0f, 0.15f, -0.15f);
            wrapper.transform.localRotation = Quaternion.identity;
            wrapper.transform.localScale = Vector3.one;
            // Some rigs (e.g. the sourced villager body's bone chain) bake
            // a tiny non-1 scale onto the socket bone itself - without
            // this, the pennant would render at that same tiny fraction of
            // its intended size instead of a fixed world-space size.
            CompensateParentScale(wrapper.transform);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            // DestroyImmediate, not Destroy: this runs from HumanModelFactory.Spawn,
            // which EditMode tests (EntitySpawnerTests etc.) call directly outside
            // Play mode - Destroy() logs an Editor-only error there.
            Object.DestroyImmediate(pole.GetComponent<Collider>());
            pole.transform.SetParent(wrapper.transform, false);
            pole.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            pole.transform.localScale = new Vector3(0.02f, 0.15f, 0.02f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = GameplayMaterial.CreateOpaque(PoleColor);

            GameObject flag = BuildFlagQuad("Flag", 0.16f, 0.12f, TeamColor.For(faction));
            flag.transform.SetParent(wrapper.transform, false);
            flag.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        }

        // Unity's Transform.SetParent(parent, worldPositionStays: true)
        // preserves world scale across a re-parent onto a UNIFORMLY scaled
        // parent, but silently fails to for a non-uniformly scaled one
        // (can't cleanly decompose the shear back into TRS) - a building's
        // squashed-Y procedural visual left its banner squashed the same
        // way even with worldPositionStays:true. Forces world scale back
        // to exactly 1 unconditionally (an absolute set, not a multiply on
        // top of whatever SetParent already did) so this is correct either
        // way, idempotent regardless of the parent's own scale shape -
        // every caller's accent GameObject has no other explicit scale of
        // its own before this runs.
        private static void CompensateParentScale(Transform child)
        {
            if (child.parent == null)
            {
                return;
            }

            Vector3 parentScale = child.parent.lossyScale;
            child.localScale = new Vector3(
                Mathf.Approximately(parentScale.x, 0f) ? 1f : 1f / parentScale.x,
                Mathf.Approximately(parentScale.y, 0f) ? 1f : 1f / parentScale.y,
                Mathf.Approximately(parentScale.z, 0f) ? 1f : 1f / parentScale.z);
        }

        // Hand-built double-sided quad, hanging down from its pivot (top
        // edge at localPosition, bottom edge at localPosition.y - height) -
        // same "small mesh, don't trust triangle winding, disable culling
        // instead" idiom as ProceduralBuildingFactory.BuildPyramidMesh/
        // CreateDoubleSidedMaterial.
        private static GameObject BuildFlagQuad(string name, float width, float height, Color color)
        {
            float halfWidth = width * 0.5f;

            Vector3[] vertices =
            {
                new Vector3(-halfWidth, 0f, 0f),
                new Vector3(halfWidth, 0f, 0f),
                new Vector3(halfWidth, -height, 0f),
                new Vector3(-halfWidth, -height, 0f),
            };

            int[] triangles = { 0, 1, 2, 0, 2, 3 };

            var mesh = new Mesh { name = "TeamColorFlagQuad" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().mesh = mesh;

            Material material = GameplayMaterial.CreateOpaque(color);
            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }
            go.AddComponent<MeshRenderer>().sharedMaterial = material;

            return go;
        }
    }
}
