using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KingdomsOfBharat.Editor
{
    /// <summary>
    /// Builds a Unity Humanoid Avatar for a glTF-imported biped rig at Editor
    /// time and saves the result as a runtime-spawnable prefab. glTFast does
    /// not auto-build a Humanoid Avatar the way Unity's native FBX
    /// ModelImporter can (its own Animator imports with isHuman=false,
    /// avatar=null) - the Playables-based AnimationDriver this project uses
    /// needs a real Humanoid Avatar to retarget the shared human clip
    /// library (see HumanAnimationSet) onto a new mesh, so this fills that
    /// gap with a hand-authored HumanDescription, built directly from the
    /// model's own actual transform hierarchy (never hand-transcribed - a
    /// typo'd bone position/rotation would silently distort the rig).
    /// </summary>
    public static class HumanoidGltfRigImporter
    {
        // Meshy AI's biped rig exports use this Mixamo-style naming
        // convention (confirmed by inspecting the Female Villager import) -
        // reusable as-is for any similarly-named biped (e.g. a future male
        // villager import), as long as the bone names match.
        private static readonly (string boneName, HumanBodyBones bone)[] MixamoBoneMap =
        {
            ("Hips", HumanBodyBones.Hips),
            ("Spine02", HumanBodyBones.Spine),
            ("Spine01", HumanBodyBones.Chest),
            ("Spine", HumanBodyBones.UpperChest),
            ("neck", HumanBodyBones.Neck),
            ("Head", HumanBodyBones.Head),
            ("LeftShoulder", HumanBodyBones.LeftShoulder),
            ("LeftArm", HumanBodyBones.LeftUpperArm),
            ("LeftForeArm", HumanBodyBones.LeftLowerArm),
            ("LeftHand", HumanBodyBones.LeftHand),
            ("RightShoulder", HumanBodyBones.RightShoulder),
            ("RightArm", HumanBodyBones.RightUpperArm),
            ("RightForeArm", HumanBodyBones.RightLowerArm),
            ("RightHand", HumanBodyBones.RightHand),
            ("LeftUpLeg", HumanBodyBones.LeftUpperLeg),
            ("LeftLeg", HumanBodyBones.LeftLowerLeg),
            ("LeftFoot", HumanBodyBones.LeftFoot),
            ("LeftToeBase", HumanBodyBones.LeftToes),
            ("RightUpLeg", HumanBodyBones.RightUpperLeg),
            ("RightLeg", HumanBodyBones.RightLowerLeg),
            ("RightFoot", HumanBodyBones.RightFoot),
            ("RightToeBase", HumanBodyBones.RightToes),
        };

        /// <summary>
        /// glbAssetPath: the imported .glb under Assets/Resources/...
        /// prefabDestPath: where to save the final spawnable prefab (must
        /// also be under a Resources folder for HumanModelFactory's
        /// Resources.Load convention).
        /// targetHeight: world-space height (measured via renderer bounds)
        /// to scale the model to - pass the project's existing worker-height
        /// convention (1.902692, the shared Human Character Dummy's own
        /// measured height) so a swapped-in model doesn't look mismatched
        /// next to buildings/other units.
        /// </summary>
        public static string BuildAndSavePrefab(string glbAssetPath, string prefabDestPath, float targetHeight)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(glbAssetPath);
            if (sourcePrefab == null)
            {
                return $"ERROR: no prefab asset at {glbAssetPath}";
            }

            GameObject instance = Object.Instantiate(sourcePrefab);
            try
            {
                Animator animator = instance.GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    return "ERROR: imported model has no Animator component";
                }

                Avatar avatar = BuildAvatar(animator.gameObject);
                if (!avatar.isValid || !avatar.isHuman)
                {
                    return $"ERROR: built Avatar invalid (isValid={avatar.isValid}, isHuman={avatar.isHuman})";
                }

                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(prefabDestPath)));
                string avatarAssetPath = Path.ChangeExtension(prefabDestPath, null) + "_Avatar.asset";
                AssetDatabase.CreateAsset(avatar, avatarAssetPath);

                animator.avatar = avatar;
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;

                // Measure BEFORE scaling (bounds already reflect whatever
                // baked scale the source hierarchy carries, e.g. the
                // Armature node's own 0.01 - see HumanoidGltfRigImporter's
                // own class doc / the session plan for why this can't be
                // guessed).
                float measuredHeight = MeasureHeight(instance);
                if (measuredHeight <= 0f)
                {
                    return "ERROR: no renderer bounds found to measure height";
                }

                float correction = targetHeight / measuredHeight;
                instance.transform.localScale *= correction;

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabDestPath, out bool success);
                if (!success)
                {
                    return "ERROR: PrefabUtility.SaveAsPrefabAsset failed";
                }

                float finalHeight = MeasureHeight(savedPrefab) ;
                return $"OK: saved {prefabDestPath}, avatar isHuman={avatar.isHuman}, "
                    + $"measuredHeight(pre-scale)={measuredHeight:F4}, scaleCorrection={correction:F4}, "
                    + $"avatarAsset={avatarAssetPath}";
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static float MeasureHeight(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return 0f;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.size.y;
        }

        private static Avatar BuildAvatar(GameObject root)
        {
            HumanDescription description = new HumanDescription
            {
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f,
                feetSpacing = 0f,
                hasTranslationDoF = false,
            };

            List<HumanBone> humanBones = new List<HumanBone>();
            foreach ((string boneName, HumanBodyBones bone) in MixamoBoneMap)
            {
                Transform t = FindDeep(root.transform, boneName);
                if (t == null)
                {
                    continue;
                }

                humanBones.Add(new HumanBone
                {
                    humanName = HumanTrait.BoneName[(int)bone],
                    boneName = boneName,
                    limit = new HumanLimit { useDefaultValues = true },
                });
            }
            description.human = humanBones.ToArray();

            List<SkeletonBone> skeletonBones = new List<SkeletonBone>();
            CollectSkeleton(root.transform, skeletonBones);
            description.skeleton = skeletonBones.ToArray();

            Avatar avatar = AvatarBuilder.BuildHumanAvatar(root, description);
            avatar.name = root.name + "_Avatar";
            return avatar;
        }

        private static void CollectSkeleton(Transform t, List<SkeletonBone> list)
        {
            list.Add(new SkeletonBone
            {
                name = t.name,
                position = t.localPosition,
                rotation = t.localRotation,
                scale = t.localScale,
            });

            for (int i = 0; i < t.childCount; i++)
            {
                CollectSkeleton(t.GetChild(i), list);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
