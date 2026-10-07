// VFX.cs - Procedural particle visual effects: pickups, impacts, landing, power-ups, speed lines, death, and environmental.
using System.Collections.Generic;
using UnityEngine;

namespace JungleDash
{
    public class VFX : MonoBehaviour
    {
        public static VFX Instance { get; private set; }

        ParticleSystem coinSparkles;
        ParticleSystem gemSparkles;
        ParticleSystem hitSparks;
        ParticleSystem landDust;
        ParticleSystem shieldBreakBurst;
        ParticleSystem doubleJumpBurst;
        ParticleSystem deathExplosion;
        ParticleSystem speedLines;
        ParticleSystem flyTrail;
        ParticleSystem ambientFireflies;
        ParticleSystem coinTrail;

        void Awake()
        {
            if (Instance == null) Instance = this;
            BuildSystems();
        }

        void BuildSystems()
        {
            // Core gameplay VFX
            coinSparkles = CreateParticleSystem("CoinBurst", GameConfig.GoldColor, 28, 0.5f, 4.5f, 0.24f);
            gemSparkles = CreateParticleSystem("GemBurst", GameConfig.GemColor, 40, 0.65f, 6f, 0.28f);
            hitSparks = CreateParticleSystem("HitSparks", new Color(1f, 0.4f, 0.15f), 45, 0.55f, 7f, 0.38f);
            landDust = CreateParticleSystem("LandDust", new Color(0.7f, 0.65f, 0.55f, 0.5f), 20, 0.45f, 3f, 0.35f);
            shieldBreakBurst = CreateParticleSystem("ShieldBreak", new Color(0.3f, 0.75f, 1f), 50, 0.75f, 8f, 0.38f);
            
            // New effects
            doubleJumpBurst = CreateParticleSystem("DoubleJump", new Color(0.95f, 0.95f, 1f), 22, 0.35f, 5f, 0.18f);
            deathExplosion = CreateParticleSystem("DeathExplosion", new Color(1f, 0.25f, 0.15f), 60, 0.8f, 5.5f, 0.45f);
            coinTrail = CreateParticleSystem("CoinTrail", GameConfig.GoldColor, 16, 0.3f, 2f, 0.12f);
            
            // Speed lines (stretched along Z)
            speedLines = CreateSpeedLines();
            
            // Flying trail
            flyTrail = CreateFlyTrail();
            
            // Ambient fireflies
            ambientFireflies = CreateAmbientFireflies();
        }

        ParticleSystem CreateParticleSystem(string name, Color color, int maxParticles, float lifetime, float speed, float size)
        {
            GameObject go = new GameObject("VFX_" + name);
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.3f;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var colOverLifetime = ps.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.Lerp(color, Color.white, 0.3f), 0.3f), new GradientColorKey(color, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.5f);
            curve.AddKey(0.15f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Material m = Mats.Get(MatKind.Emissive, color, null, 1, 1, 0.5f, 2f);
            renderer.sharedMaterial = m;

            return ps;
        }

        ParticleSystem CreateSpeedLines()
        {
            GameObject go = new GameObject("VFX_SpeedLines");
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.3f;
            main.startSpeed = -30f;
            main.startSize3D = true;
            main.startSizeX = 0.04f;
            main.startSizeY = 0.04f;
            main.startSizeZ = 2.5f;
            main.startColor = new Color(1f, 1f, 1f, 0.3f);
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(8f, 4f, 0.1f);
            shape.position = new Vector3(0f, 2f, 12f);

            var colOverLifetime = ps.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.4f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Mats.Get(MatKind.Emissive, Color.white, null, 1, 1, 0.5f, 0.5f);
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 8f;

            ps.Stop();
            return ps;
        }

        ParticleSystem CreateFlyTrail()
        {
            GameObject go = new GameObject("VFX_FlyTrail");
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.6f;
            main.startSpeed = 1.5f;
            main.startSize = 0.35f;
            main.startColor = new Color(0.82f, 0.5f, 1f, 0.5f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 30f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.6f;

            var colOverLifetime = ps.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            Color flyCol = new Color(0.82f, 0.5f, 1f);
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(flyCol, 0f), new GradientColorKey(Color.Lerp(flyCol, Color.white, 0.5f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Mats.Get(MatKind.Emissive, flyCol, null, 1, 1, 0.5f, 1.5f);

            ps.Stop();
            return ps;
        }

        ParticleSystem CreateAmbientFireflies()
        {
            GameObject go = new GameObject("VFX_Fireflies");
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.startLifetime = 3f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new Color(0.95f, 0.92f, 0.4f, 0.7f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 5f, 20f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.3f;

            var colOverLifetime = ps.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            Color fireflyCol = new Color(0.95f, 0.92f, 0.4f);
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(fireflyCol, 0f), new GradientColorKey(fireflyCol, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Mats.Get(MatKind.Emissive, fireflyCol, null, 1, 1, 0.5f, 2.5f);

            return ps;
        }

        // ----------- Public API -----------

        public void PlayCoinPickup(Vector3 pos)
        {
            if (coinSparkles != null)
            {
                coinSparkles.transform.position = pos;
                coinSparkles.Emit(14);
            }
        }

        public void PlayGemPickup(Vector3 pos)
        {
            if (gemSparkles != null)
            {
                gemSparkles.transform.position = pos;
                gemSparkles.Emit(24);
            }
        }

        public void PlayObstacleHit(Vector3 pos)
        {
            if (hitSparks != null)
            {
                hitSparks.transform.position = pos;
                hitSparks.Emit(32);
            }
        }

        public void PlayLandingDust(Vector3 pos)
        {
            if (landDust != null)
            {
                landDust.transform.position = pos;
                landDust.Emit(12);
            }
        }

        public void PlayShieldBreak(Vector3 pos)
        {
            if (shieldBreakBurst != null)
            {
                shieldBreakBurst.transform.position = pos;
                shieldBreakBurst.Emit(36);
            }
        }

        public void PlayDoubleJumpBurst(Vector3 pos)
        {
            if (doubleJumpBurst != null)
            {
                doubleJumpBurst.transform.position = pos;
                doubleJumpBurst.Emit(16);
            }
        }

        public void PlayDeathExplosion(Vector3 pos)
        {
            if (deathExplosion != null)
            {
                deathExplosion.transform.position = pos;
                deathExplosion.Emit(45);
            }
        }

        public void PlayCoinTrail(Vector3 pos)
        {
            if (coinTrail != null)
            {
                coinTrail.transform.position = pos;
                coinTrail.Emit(6);
            }
        }

        public void SetSpeedLines(bool on, Vector3 playerPos)
        {
            if (speedLines == null) return;
            speedLines.transform.position = playerPos;
            if (on && !speedLines.isPlaying) speedLines.Play();
            else if (!on && speedLines.isPlaying) speedLines.Stop();
        }

        public void SetFlyTrail(bool on, Vector3 playerPos)
        {
            if (flyTrail == null) return;
            flyTrail.transform.position = playerPos + Vector3.up * 0.3f;
            if (on && !flyTrail.isPlaying) flyTrail.Play();
            else if (!on && flyTrail.isPlaying) flyTrail.Stop();
        }

        public void UpdateAmbientFireflies(Vector3 playerPos)
        {
            if (ambientFireflies != null)
            {
                ambientFireflies.transform.position = playerPos + new Vector3(0f, 2f, 8f);
            }
        }
    }
}
