using UnityEngine;
using KingdomsOfBharat.Core;

namespace KingdomsOfBharat.Vfx
{
    // Prefab-free, self-destructing one-shot particle bursts (hit sparks,
    // dust puffs, death poofs...) - matches the project's "everything
    // spawned via code, no hand-authored prefabs" convention. Callers fire
    // a burst from an existing Update()/tick rather than managing a
    // persistent emitter's lifecycle, keeping every call site a single
    // line with nothing to clean up later (stopAction: Destroy handles it).
    public static class VfxFactory
    {
        private static Shader _particleShader;

        public static void SpawnBurst(
            Vector3 position, Color color,
            float size = 0.15f, int count = 8,
            float speed = 1.5f, float lifetime = 0.4f)
        {
            var go = new GameObject("VfxBurst");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            // AddComponent<ParticleSystem>() auto-starts it (playOnAwake
            // defaults true) - editing main.duration while it's already
            // "playing" logs a warning every single call. Stop it first.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = lifetime;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var material = new Material(FindParticleShader());
            GameplayMaterial.ForceTransparent(material);
            renderer.sharedMaterial = material;

            ps.Play();
        }

        // URP's dedicated particle shaders multiply the vertex color the
        // particle system module drives (main.startColor) into the final
        // color - the plain Unlit/Lit shaders GameplayMaterial otherwise
        // uses for gameplay objects don't do that, so particles need their
        // own shader lookup rather than reusing FindUnlitShader().
        private static Shader FindParticleShader()
        {
            if (_particleShader == null)
            {
                _particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Particles/Standard Unlit")
                    ?? GameplayMaterial.FindUnlitShader();
            }

            return _particleShader;
        }
    }
}
