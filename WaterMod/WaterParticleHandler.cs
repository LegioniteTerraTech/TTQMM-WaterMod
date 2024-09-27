using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using TerraTechETCUtil;

namespace WaterMod
{
    public class WaterParticleHandler : WorldSpaceObjectBase
    {
        public static bool UseAlternateSplash = false;
        public const float offsetHeightSplash = -0.1f;
        public static float offsetHeightSurface => ManWater.CameraSubmerged ? offsetHeightSurfaceMainInv : offsetHeightSurfaceMain;
        public static float offsetHeightSurfaceMain = 0.325f;
        public static float offsetHeightSurfaceMainInv = -0.35f;
        public static float BubblesOpacity = 0.35f;
        public static float BubblesStartSize = 0.10f;
        public static float BubblesMaxSize = 0.15f;

        public static Material blurredMat;
        public static Material blurredMatLava;
        public static Material spriteMaterial;
        public static Material bubbleMaterial;
        private static GameObject FXFolder;
        public static GameObject oSplash;
        public static GameObject oSurface;
        public static GameObject oBubbleStreams;
        public static ParticleSystem FXSplash;
        public static ParticleSystem FXSurface;
        public static ParticleSystem FXBubbleStreams;

        public static bool UseParticleEffects = true;

        public static ParticleSystem.MinMaxGradient WaterGradient = new ParticleSystem.MinMaxGradient(
                new Gradient()
                {
                    alphaKeys = new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
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
                    new GradientAlphaKey(1f, 0f),
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
            oBubbleStreams = new GameObject("Bubble");

            oSplash.transform.parent = FXFolder.transform;
            oSurface.transform.parent = FXFolder.transform;
            CreateSpriteMaterial();
            CreateSplash();
            CreateSurface();
            CreateBubbleStreams();
            DebugWater.Log("WaterMod: Created Water Effects");
        }
        public static ParticleSystem.Particle[] particleHandler = new ParticleSystem.Particle[500];
        public static void TreadmillParticles(IntVector3 toChange, ParticleSystem item)
        {
            ParticleSystem.Particle[] cached = particleHandler;
            if (item.particleCount > 0)
            {
                int maxParticles = item.main.maxParticles;
                if (cached.Length < maxParticles)
                {
                    Array.Resize(ref particleHandler, maxParticles);
                    cached = particleHandler;
                }
                int particles = item.GetParticles(cached);
                for (int step = 0; step < particles; step++)
                    cached[step].position = cached[step].position + toChange;
                item.SetParticles(cached, particles);
            }
        }

        public override void OnMoveWorldOrigin(IntVector3 toChange)
        {
            SurfacePool.TreadmillManagedParticles(toChange);
            TreadmillParticles(toChange, FXSplash);
            TreadmillParticles(toChange, FXBubbleStreams);
        }
        public static void TreadmillAllParticles(IntVector3 toChange)
        {
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
 
            //var materialClear = ResourcesHelper.GetMaterialFromBaseGameAllDeep("ClearPanel");
            Shader watShader = ManWater.InsureGetShader("Legacy Shaders/Particles/Alpha Blended");

            blurredMat = new Material(watShader)
            {
                color = ManWater.waterColorBright,
                shaderKeywords = new string[] {},
            };
            //var tex = new Texture2D(0, 0);
            var tex = QPatch.ModHandle.GetModContainer().GetTextureFromModAssetBundle("Splash");//tex.LoadImage(File.ReadAllBytes(Path.Combine(QPatch.assets_path, "Splash.png")));
            tex.Apply();
            blurredMat.mainTexture = tex;
            blurredMat.EnableKeyword("_ALPHATEST_ON");
            blurredMat.EnableKeyword("_ALPHABLEND_ON");
            blurredMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            blurredMat.DisableKeyword("_EMISSION");
            blurredMat.SetColor("_Color", ManWater.waterColorBright);
            blurredMat.SetColor("_EmissionColor", new Color(0.05f, 0.125f, 0.2f, 0));
            //blurredMat.SetColor("_EmissionColor", new Color(0.05f, 0.125f, 0.2f, 0.2f));

            blurredMatLava = new Material(blurredMat)
            {
                color = ManWater.lavaColorBright,
            };
            blurredMatLava.EnableKeyword("_EMISSION");
            blurredMatLava.SetColor("_Color", ManWater.lavaColorBright);
            blurredMatLava.SetColor("_EmissionColor", new Color(0.97f, 0.3f, 0.07f, 0.45f));
            Texture2D image = null;
            foreach (var item in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                if (item.name == "t_sph_Liquid_oil_01")
                    image = item;
            }
            if (image == null)
                throw new NullReferenceException("t_sph_Liquid_oil_01 null");
            bubbleMaterial = new Material(watShader)
            {
                mainTexture = image,
                color = ManWater.waterColorBright,
            };
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
            m.loop = false;

            var e = ps.emission;
            e.rateOverTime = 16f;

            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (UseAlternateSplash)
            {
                var ac2 = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f); ac2.AddKey(new Keyframe(0.5f, 1f));
                m.startSpeed = new ParticleSystem.MinMaxCurve(3f, ac2);
                var s = ps.shape;
                s.shapeType = ParticleSystemShapeType.Hemisphere;
                s.radiusThickness = 0.1f;
                s.radius = 0.2f;
                s.rotation = Vector3.right * 270f;

                m.gravityModifier = 0.8f;

                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.separateAxes = true;
                sz.x = new ParticleSystem.MinMaxCurve(0.65f, AnimationCurve.Linear(0f, 0.5f, 1f, 1f));
                sz.z = new ParticleSystem.MinMaxCurve(0.65f, AnimationCurve.Linear(0f, 0.5f, 1f, 1f));
                var ac = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f); ac.AddKey(new Keyframe(0.5f, 1f));
                sz.y = new ParticleSystem.MinMaxCurve(0.65f, ac);

                r.renderMode = ParticleSystemRenderMode.Billboard;
                r.velocityScale = 3;
            }
            else
            {
                m.startSpeed = 0f;

                var s = ps.shape;
                s.shapeType = ParticleSystemShapeType.Circle;
                s.radius = 0.2f;
                s.rotation = Vector3.right * 90f;

                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.separateAxes = true;
                sz.x = new ParticleSystem.MinMaxCurve(6f, AnimationCurve.Linear(0f, 0.5f, 1f, 1f));
                sz.z = new ParticleSystem.MinMaxCurve(6f, AnimationCurve.Linear(0f, 0.5f, 1f, 1f));
                var ac = AnimationCurve.EaseInOut(0f, 0f, 1f, 0f); ac.AddKey(new Keyframe(0.5f, 1f));
                sz.y = new ParticleSystem.MinMaxCurve(6f, ac);

                r.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            }

            var c = ps.colorOverLifetime;
            c.enabled = true;
            if (QPatch.TheWaterIsLava)
                c.color = LavaGradient;
            else
                c.color = WaterGradient;
            
            if (QPatch.TheWaterIsLava)
                r.material = blurredMatLava;
            else
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
            m.startSize = 1;
            m.startLifetime = 2.5f;
            m.playOnAwake = false; //change later
            m.maxParticles = 500;
            m.startSpeed = 0f;
            m.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;

            var e = ps.emission;
            e.rateOverTime = 0.5f;
            e.rateOverDistance = 0.5f;

            var s = ps.shape;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.radius = 0.2f;
            s.rotation = Vector3.right * 90f;

            var c = ps.colorOverLifetime;
            c.enabled = true;
            if (QPatch.TheWaterIsLava)
                c.color = LavaGradient;
            else
                c.color = WaterGradient;

            var o = ps.sizeOverLifetime;
            o.enabled = true;
            o.size = new ParticleSystem.MinMaxCurve(16f, AnimationCurve.Linear(0f, 0.05f, 1f, 1f));

            /*
            var v = ps.velocityOverLifetime;
            v.enabled = true;
            v.y = 0.25f;//-0.15f;
            */


            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            if (QPatch.TheWaterIsLava)
                r.material = blurredMatLava;
            else
                r.material = blurredMat;
            r.maxParticleSize = 20f;
            //r.sortingOrder = -1;//Make the effects appear correctly

            FXSurface = ps;
            ps.Stop();
            oSurface.AddComponent<SurfacePool.Item>();
        }
        private static void CreateBubbleStreams()
        {
            var ps = oBubbleStreams.AddComponent<ParticleSystem>();

            var m = ps.main;
            m.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = 16f;
            m.startSize = BubblesStartSize;
            m.startSize3D = false;
            m.playOnAwake = false;
            m.maxParticles = 500;
            m.startSpeed = 6f;
            m.loop = true;

            var e = ps.emission;
            e.enabled = true;
            e.rateOverTime = 16f;
            e.rateOverDistance = 2.5f;

            var s = ps.shape;
            s.enabled = true;
            //s.shapeType = ParticleSystemShapeType.Cone;
            s.shapeType = ParticleSystemShapeType.Circle;
            s.angle = 0f;
            s.radius = 70f;
            s.rotation = Vector3.right * 270f;
            s.position = Vector3.zero;

            var c = ps.colorOverLifetime;
            c.enabled = true;
            if (QPatch.TheWaterIsLava)
                c.color = new Color(0.95f, 0.7f, 0.4f, BubblesOpacity);
            else
                c.color = new Color(0.561f, 0.937f, 0.875f, BubblesOpacity);


            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.material = bubbleMaterial;
            r.maxParticleSize = BubblesMaxSize;

            FXBubbleStreams = ps;
            ps.Stop();
        }


        public static void UpdateSplash()
        {
            if (oSplash == null)
                return; // TOO EARLY
            var ps = oSplash.GetComponent<ParticleSystem>();

            var c = ps.colorOverLifetime;
            if (QPatch.TheWaterIsLava)
            {
                c.color = LavaGradient;
                blurredMat.SetColor("_EmissionColor", new Color(0.97f, 0.3f, 0.07f, 1f));
            }
            else
            {
                c.color = WaterGradient;
                blurredMat.SetColor("_EmissionColor", new Color(0.05f, 0.125f, 0.2f, 0.2f));
            }
            FXSplash = ps;
        }
        public static void UpdateSurface()
        {
            if (oSurface == null)
                return; // TOO EARLY
            var ps = oSurface.GetComponent<ParticleSystem>();

            var c = ps.colorOverLifetime;
            if (QPatch.TheWaterIsLava)
                c.color = LavaGradient;
            else
                c.color = WaterGradient;
            FXSurface = ps;
            SurfacePool.UpdateAllActiveParticlesMode();
        }
        public static void UpdateBubbles()
        {
            if (oBubbleStreams == null)
                return; // TOO EARLY
            var ps = oBubbleStreams.GetComponent<ParticleSystem>();

            var c = ps.colorOverLifetime;
            if (QPatch.TheWaterIsLava)
                c.color = new Color(0.95f, 0.7f, 0.4f, BubblesOpacity);
            else
                c.color = new Color(0.561f, 0.937f, 0.875f, BubblesOpacity);
            FXBubbleStreams = ps;
        }
        public static void MaintainBubbles()
        {
            oBubbleStreams.transform.position = Singleton.playerPos + 
                (Singleton.cameraTrans.forward * 15);
            if (ManWater.CameraSubmerged != FXBubbleStreams.isPlaying)
            {
                if (ManWater.CameraSubmerged)
                {
                    FXBubbleStreams.Play();
                    SurfacePool.UpdateAllActiveParticlesMode();
                }
                else
                {
                    FXBubbleStreams.Clear();
                    FXBubbleStreams.Stop();
                    SurfacePool.UpdateAllActiveParticlesMode();
                }
            }
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
            if (UseAlternateSplash)
                FXSplash.Emit(emitparams, UnityEngine.Random.Range(3,8));
            else
                FXSplash.Emit(emitparams, 1);
        }
        public static void ClearAllParticles()
        {
            FXSplash.Clear();
            FXSurface.Clear();
        }
    }

    public class SurfacePool
    {
        public static bool CanGrow = true;
        public static int MaxGrow = 500;
        private static List<Item> FreeList;
        public static HashSet<ParticleSystem> allActive = new HashSet<ParticleSystem>();
        public static int Count { get; private set; }
        public static int Available { get; set; }

        public static void Initiate()
        {
            Count = 0;
            Available = 0;
            FreeList = new List<Item>();
        }

        public static Item GetFromPool(float size)
        {
            Item ps;
            ParticleSystem.MainModule main;
            if (Available != 0)
            {
                Available--;
                ps = FreeList[Available];
                ps.StartUsing();
                FreeList.RemoveAt(Available);
                ps.Size = size;
                ps.UpdateMode();
                return ps;
            }
            if (Count >= MaxGrow)
            {
                return null;
            }
            ps = CreateNew(true);
            ps.Size = size;
            ps.UpdateMode();
            return ps;
        }

        public static void ReturnToPool(Item surface, bool Now = false)
        {
            ParticleSystem ps = surface.GetComponent<ParticleSystem>();
            if (Now)
                ps.Clear();
            ps.Stop();
            Available++;
            FreeList.Add(surface);
            allActive.Remove(ps);
            surface.SetDestroy();
        }
        public static void UpdateAllActiveParticlesMode()
        {
            foreach (var item in allActive)
            {
                item.GetComponent<Item>().UpdateMode();
            }
        }
        public static void UpdateAllActiveParticles()
        {
            foreach (var item in allActive)
            {
                try
                {
                    var ps = item.GetComponent<ParticleSystem>();
                    var c = ps.colorOverLifetime;
                    if (QPatch.TheWaterIsLava)
                        c.color = WaterParticleHandler.LavaGradient;
                    else
                        c.color = WaterParticleHandler.WaterGradient;
                }
                catch (Exception e)
                {
                    DebugWater.Log(e);
                }
            }
        }
        public static ParticleSystem.Particle[] particleHandler = new ParticleSystem.Particle[500];
        public static void TreadmillManagedParticles(IntVector3 toChange)
        {
            ParticleSystem.Particle[] cached = particleHandler;
            foreach (var item in allActive)
            {
                if (item.particleCount > 0)
                {
                    int maxParticles = item.main.maxParticles;
                    if (cached.Length < maxParticles)
                    {
                        Array.Resize(ref particleHandler, maxParticles);
                        cached = particleHandler;
                    }
                    int particles = item.GetParticles(cached);
                    for (int step = 0; step < particles; step++)
                        cached[step].position = cached[step].position + toChange;
                    item.SetParticles(cached, particles);
                }
            }
        }

        private static Item CreateNew(bool SetActive = false)
        {
            var s = UnityEngine.Object.Instantiate(WaterParticleHandler.oSurface);
            s.SetActive(SetActive);
            Count++;
            var i = s.GetComponent<Item>();
            i.Setup();
            return i;
        }

        public class Item : MonoBehaviour
        {
            public bool Enabled = true;
            public bool Using = true;
            public float Size = 1;
            public float Rate = 1;
            public ParticleSystem PS;

            internal void SetRate(float rate)
            {
                Rate = rate;
                var e = PS.emission;
                e.rateOverDistance = 0.015f * Rate;
            }
            public void SetDestroy()
            {
                Using = false;
                Invoke("Destroy", 2.5f);
            }

            private void Destroy()
            {
                if (!Using)
                {
                    //AllList.Remove(this);
                    gameObject.GetComponent<ParticleSystem>().Clear();
                    gameObject.SetActive(false);
                }
            }

            public void UpdateMode()
            {
                var r = GetComponent<ParticleSystemRenderer>();
                if (ManWater.CameraSubmerged)
                {
                    var m = PS.main;
                    m.startSize = UnityEngine.Random.Range(0.8f, 1.25f) * WaterParticleHandler.BubblesStartSize;
                    var e = PS.emission;
                    e.rateOverTime = 0.0005f;
                    if (Rate > 1)
                        e.rateOverDistance = 0.015f * Rate;
                    else
                        e.rateOverDistance = 0.015f + UnityEngine.Random.Range(0f,0.02f);
                    r.renderMode = ParticleSystemRenderMode.Billboard;
                    r.maxParticleSize = WaterParticleHandler.BubblesMaxSize;
                    r.material = WaterParticleHandler.bubbleMaterial;
                }
                else
                {
                    var m = PS.main;
                    m.startSize = Size;
                    var e = PS.emission;
                    e.rateOverTime = 0.5f;
                    e.rateOverDistance = 0.5f;
                    r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                    r.maxParticleSize = 200;
                    if (QPatch.TheWaterIsLava)
                        r.material = WaterParticleHandler.blurredMatLava;
                    else
                        r.material = WaterParticleHandler.blurredMat;
                    var c = PS.colorOverLifetime;
                    if (QPatch.TheWaterIsLava)
                        c.color = WaterParticleHandler.LavaGradient;
                    else
                        c.color = WaterParticleHandler.WaterGradient;
                }
            }
            public void UpdatePos(Vector3 position)
            {
                Using = true; 
                if (ManWater.WorldMove == Enabled)
                {
                    if (ManWater.WorldMove)
                    {
                        PS.SetEmissionEnabled(false);
                        var e = PS.emission;
                        e.rateOverDistance = 0;
                        Enabled = false;
                    }
                }
                if (Enabled)
                    transform.position = position + (Vector3.up * WaterParticleHandler.offsetHeightSurface);// keep it at the water level
                if (ManWater.WorldMove == Enabled)
                {
                    if (!ManWater.WorldMove)
                    {
                        UpdateMode();
                        PS.SetEmissionEnabled(true); 
                        Enabled = true;
                    }
                }
            }

            public void Setup()
            {
                if (!PS)
                    PS = gameObject.GetComponent<ParticleSystem>();
                UpdateMode();
                allActive.Add(PS);
                PS.Clear(true);
                PS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                PS.Play();
            }
            public void StartUsing()
            {
                Using = true;
                gameObject.SetActive(true);
                Setup();
            }
        }
    }
}