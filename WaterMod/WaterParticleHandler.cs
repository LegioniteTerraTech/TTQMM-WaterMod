﻿using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace WaterMod
{
    public class WaterParticleHandler
    {
        public static Material blurredMat;
        public static Material filledMat;
        public static Material spriteMaterial;
        private static GameObject FXFolder;
        public static GameObject oSplash;
        public static GameObject oSurface;
        public static ParticleSystem FXSplash;
        public static ParticleSystem FXSurface;

        public static bool UseParticleEffects = true;

        public static ParticleSystem.MinMaxGradient WaterGradient = new ParticleSystem.MinMaxGradient(
                new Gradient()
                {
                    alphaKeys = new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.3f, 0.25f),
                    new GradientAlphaKey(0f, 1f)
                    },
                    colorKeys = new GradientColorKey[] {
                    new GradientColorKey(new Color(0.561f, 0.937f, 0.875f), 0.5f),
                    new GradientColorKey(new Color(0f, 0.69f, 1f), 1f)
                    },
                    mode = GradientMode.Blend
                });
        public static ParticleSystem.MinMaxGradient LavaGradient = new ParticleSystem.MinMaxGradient(
                new Gradient()
                {
                    alphaKeys = new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.3f, 0.25f),
                    new GradientAlphaKey(0f, 1f)
                    },
                    colorKeys = new GradientColorKey[] {
                    new GradientColorKey(new Color(0.95f, 0.7f, 0.4f), 0.5f),
                    new GradientColorKey(new Color(0.69f, 0.05f, 0.05f), 1f)
                    },
                    mode = GradientMode.Blend
                });

        public static void Initialize()
        {
            FXFolder = new GameObject("WaterModFX");

            oSplash = new GameObject("Splash");
            oSurface = new GameObject("Surface");

            oSplash.transform.parent = FXFolder.transform;
            oSurface.transform.parent = FXFolder.transform;
            CreateSpriteMaterial();
            CreateSplash();
            CreateSurface();
            Debug.Log("WaterMod: Created Water Effects");
        }

        private static void CreateSpriteMaterial()
        {
            Material material = null;
            Material[] search = Resources.FindObjectsOfTypeAll<Material>();
            for (int i = 0; i < search.Length; i++)
            {
                if (search[i].name.StartsWith("Default-Particle"))
                {
                    material = search[i];
                    break;
                }
            }

            spriteMaterial = new Material(material);

            blurredMat = new Material(material);
            var tex = new Texture2D(0, 0);
            tex.LoadImage(File.ReadAllBytes(Path.Combine(QPatch.assets_path, "Splash.png")));
            tex.Apply();
            blurredMat.mainTexture = tex;
            blurredMat.EnableKeyword("_EMISSION");
            if (!QPatch.TheWaterIsLava)
                blurredMat.SetColor("_EmissionColor", new Color(0.05f, 0.125f, 0.2f, 1f));
            else
                blurredMat.SetColor("_EmissionColor", new Color(0.97f, 0.3f, 0.07f, 0.5f));
        }

        private static void CreateSplash()
        {
            var ps = oSplash.AddComponent<ParticleSystem>();

            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = .8f;
            m.startSize3D = true;
            m.playOnAwake = false;
            m.maxParticles = 500;
            m.startSpeed = 0f;
            m.loop = false;

            var e = ps.emission;
            e.rateOverTime = 16f;

            var s = ps.shape;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius = 0.2f;
            s.rotation = Vector3.right * 90f;

            var c = ps.colorOverLifetime;
            c.enabled = true;
            if (!QPatch.TheWaterIsLava)
                c.color = WaterGradient;
            else
                c.color = LavaGradient;

            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.separateAxes = true;
            sz.x = new ParticleSystem.MinMaxCurve(6f, AnimationCurve.Linear(0f, 0.5f, 1f, 1f));
            sz.z = new ParticleSystem.MinMaxCurve(6f, AnimationCurve.Linear(0f, 0.5f, 1f, 1f));
            var ac = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f); ac.AddKey(new Keyframe(0.5f, 1f));
            sz.y = new ParticleSystem.MinMaxCurve(6f, ac);

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            r.material = blurredMat;
            r.maxParticleSize = 20f;

            FXSplash = ps;
            ps.Stop();
        }

        private static void CreateSurface()
        {
            var ps = oSurface.AddComponent<ParticleSystem>();

            var m = ps.main;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = 2.5f;
            m.playOnAwake = false; //change later
            m.maxParticles = 500;
            m.startSpeed = 0f;
            m.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;

            var e = ps.emission;
            e.rateOverTime = .5f;
            e.rateOverDistance = 0.5f;

            var s = ps.shape;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius = 0.2f;
            s.rotation = Vector3.right * 90f;

            var c = ps.colorOverLifetime;
            c.enabled = true;
            if (!QPatch.TheWaterIsLava)
                c.color = WaterGradient;
            else
                c.color = LavaGradient;

            var o = ps.sizeOverLifetime;
            o.enabled = true;
            o.size = new ParticleSystem.MinMaxCurve(16f, AnimationCurve.Linear(0f, 0.05f, 1f, 1f));

            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.y = 0.25f;

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            r.material = blurredMat;
            r.maxParticleSize = 20f;

            FXSurface = ps;
            ps.Stop();
            oSurface.AddComponent<SurfacePool.Item>();
        }
        public static void UpdateSplash()
        {
            var ps = oSplash.GetComponent<ParticleSystem>();

            var c = ps.colorOverLifetime;
            if (!QPatch.TheWaterIsLava)
            {
                c.color = WaterGradient;
                blurredMat.SetColor("_EmissionColor", new Color(0.05f, 0.125f, 0.2f, 1f));
            }
            else
            {
                c.color = LavaGradient;
                blurredMat.SetColor("_EmissionColor", new Color(0.97f, 0.3f, 0.07f, 0.5f));
            }
        }
        public static void UpdateSurface()
        {
            var ps = oSurface.GetComponent<ParticleSystem>();

            var c = ps.colorOverLifetime;
            if (!QPatch.TheWaterIsLava)
                c.color = WaterGradient;
            else
                c.color = LavaGradient;
        }

        public static void SplashAtPos(Vector3 pos, float Speed, float radius)
        {
            if (!UseParticleEffects)
                return;
            float sp = Mathf.Clamp(Mathf.Abs(Speed) * 0.25f, 0.1f, 8f);
            float sqp = Mathf.Sqrt(sp);
            var emitparams = new ParticleSystem.EmitParams
            {
                position = pos,
                startLifetime = 0.1f + sqp * 0.4f,
                startSize3D = new Vector3(sqp + radius, sp, 1f)
            };
            FXSplash.Emit(emitparams, 1);
        }
    }

    public class SurfacePool
    {
        public static bool CanGrow = true;
        public static int MaxGrow = 500;
        private static List<Item> FreeList;
        public static int Count { get; private set; }
        public static int Available { get; set; }

        public static void Initiate()
        {
            Count = 0;
            Available = 0;
            FreeList = new List<Item>();
        }

        public static Item GetFromPool()
        { 
            if (Available != 0)
            {
                Available--;
                Item ps = FreeList[Available];
                ps.StartUsing();
                FreeList.RemoveAt(Available);
                return ps;
            }
            if (Count >= MaxGrow)
            {
                return null;
            }
            Item ps2 = CreateNew(true);
            ps2.GetComponent<ParticleSystem>().Play();
            return ps2;
        }

        public static void ReturnToPool(Item surface)
        {
            surface.GetComponent<ParticleSystem>().Stop();
            SurfacePool.Available++;
            SurfacePool.FreeList.Add(surface);
            surface.SetDestroy();
        }

        private static Item CreateNew(bool SetActive = false)
        {
            var s = GameObject.Instantiate(WaterParticleHandler.oSurface);
            s.SetActive(SetActive);
            Count++;
            return s.GetComponent<Item>();
        }

        public class Item : MonoBehaviour
        {
            public bool Using = true;

            public void SetDestroy()
            {
                Using = false;
                Invoke("Destroy", 2.5f);
            }

            private void Destroy()
            {
                if (!Using)
                    gameObject.SetActive(false);
            }

            public void UpdatePos(Vector3 position)
            {
                Using = true;
                transform.position = position;
            }

            public void StartUsing()
            {
                Using = true;
                gameObject.SetActive(true);
                gameObject.GetComponent<ParticleSystem>().Play();
            }
        }
    }
}