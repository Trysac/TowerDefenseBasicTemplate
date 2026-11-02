using UnityEngine;

namespace SaucerSiege.Game
{
    /// <summary>Particle bursts and shockwave rings made in code.</summary>
    public static class Effects
    {
        /// <summary>A one-shot puff of particles that cleans itself up.</summary>
        public static void Burst(Vector3 position, Material material, Color colorA, Color colorB, int count,
            float speed, float size, float lifetime, float gravity = 0f)
        {
            var host = new GameObject("Burst");
            host.transform.position = position;
            var system = host.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.45f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            host.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            system.Play();
        }

        /// <summary>A flat ring on the ground that grows to <paramref name="radius"/> and fades.</summary>
        public static void Shockwave(Vector3 position, float radius, Color color, Mesh ring, Material overlay, float duration = 0.35f)
        {
            Transform wave = Shapes.Part("Shockwave", null, ring, overlay, position, Vector3.zero, Vector3.one * 0.1f, shadows: false);
            wave.gameObject.AddComponent<Fader>().Begin(radius, color, duration);
        }

        /// <summary>Grows and fades a shockwave, then removes it.</summary>
        sealed class Fader : MonoBehaviour
        {
            static MaterialPropertyBlock block;
            Renderer target;
            float radius;
            Color color;
            float duration;
            float age;

            public void Begin(float size, Color tint, float seconds)
            {
                target = GetComponent<Renderer>();
                radius = size;
                color = tint;
                duration = seconds;
                Update();
            }

            void Update()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / duration);
                transform.localScale = Vector3.one * Mathf.Lerp(0.15f, radius, 1f - (1f - t) * (1f - t));
                Color tint = color;
                tint.a *= 1f - t;
                block ??= new MaterialPropertyBlock();
                Shapes.Tint(target, block, "_TintColor", tint);
                if (t >= 1f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
