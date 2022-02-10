using System;
using System.Reflection;
using QModManager.Utility;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;


namespace WaterMod
{
    internal class WaterBuoyancy : MonoBehaviour
    {
        private static FieldInfo m_Sky = typeof(ManTimeOfDay).GetField("m_Sky", BindingFlags.NonPublic | BindingFlags.Instance);
        public static Texture2D CameraFilter;
        public static Texture2D CameraFilterLava;

        public static float Height
        {
            get { return height; }
            set
            {
                UpdateHeightCalc();
                height = value;
            }
        }
        public static float height = -25f;


        public static float FanJetMultiplier = 1.75f,
            ResourceBuoyancyMultiplier = 1.2f,
            BulletDampener = 1E-06f,
            LaserFraction = 0.275f,
            MissileDampener = 0.012f,
            SurfaceSkinning = 0.25f,
            SubmergedTankDampening = 0.4f,
            SubmergedTankDampeningYAddition = 0f,
            SurfaceTankDampening = 0f,
            SurfaceTankDampeningYAddition = 1f,
            RainWeightMultiplier = 0.06f,
            RainDrainMultiplier = 0.06f,
            FloodChangeClamp = 0.002f,
            AbyssDepth = 50f;

        public static float FloodHeightMultiplier
        {
            get { return floodHeightMultiplier; }
            set
            {
                UpdateHeightCalc();
                floodHeightMultiplier = value;
            }
        }
        public static float floodHeightMultiplier = 15f;

        public static int SelectedLook = 0;

        private static float NetHeightSmooth = 0f;

        public void Save()
        {
            QPatch._thisMod.WriteConfigJsonFile();
        }

        public static float HeightCalc
        {
            get
            {
                if (ManGameMode.inst.IsCurrentModeMultiplayer())
                {
                    if (!ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                    {
                        return NetHeightSmooth;
                    }
                    else
                    {
                        return -1000f;
                    }
                }
                else
                {
                    return heightCalc;
                }
            }
        }
        public static float heightCalc = -25;

        public static float RainFlood {
            get { return rainFlood; }
            set 
            {
                UpdateHeightCalc();
                rainFlood = value;
            }
        }

        private static float rainFlood = 0f;

        public static float Density = 8;
        public byte heartBeat;
        public static WaterBuoyancy _inst;
        public static GameObject folder;
        public static bool _WeatherMod;
        private bool ShowGUI = false;
        private WaterGUI waterGUI;
        public static GameObject surface;
        internal static bool WorldMove = false;

        internal class WaterGUI : MonoBehaviour
        {
            private Rect Window = new Rect(0, 0, 140, 75);

            private void OnGUI()
            {
                try
                {
                    Window = GUI.Window(29587115, Window, GUIWindow, "Water Settings");
                }
                catch { }
            }

            private void GUIWindow(int ID)
            {
                GUILayout.Label("Height: " + Height.ToString());
                Height = Mathf.RoundToInt(GUILayout.HorizontalSlider(Height / 5, -15f, 20f)) * 5;

                try
                {
                    if (ManNetwork.inst.IsMultiplayer() && ManNetwork.IsHost)
                    {
                        if (NetworkHandler.ServerWaterHeight != Height)
                        {
                            NetworkHandler.ServerWaterHeight = Height;
                            //ManNetwork.inst.SendToAllClients(WaterChange, new WaterChangeMessage() { Height = ServerWaterHeight }, ManNetwork.inst.MyPlayer.netId);
                            //Console.WriteLine("Sent new water height, changed to " + ServerWaterHeight.ToString());
                        }
                        if (NetworkHandler.ServerLava != QPatch.theWaterIsLava)
                            NetworkHandler.ServerLava = QPatch.theWaterIsLava;
                    }
                }
                catch { }
                GUI.DragWindow();
            }
        }

        public static bool IsActive = true;

        public static void SetState()
        {
            _inst.gameObject.SetActive(IsActive);
        }



        internal bool Heart = false;

        internal static void UpdateHeightCalc()
        {
            heightCalc = height + (rainFlood * floodHeightMultiplier);
        }

        private void OnTriggerStay(Collider collider)
        {
            if (Heart != PistonHeart)
            {
                Heart = PistonHeart;
                return;
            }
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.Stay(heartBeat);
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.Ent(heartBeat);
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.Ext(heartBeat);
            }
        }

        int DamageClock = 0;
        private void FixedUpdate()
        {
            heartBeat++;    // Updates the water
            DamageClock++;
            if (DamageClock >= LavaMode.DamageUpdateDelay)
            {
                LavaMode.DealPainThisFrame = true;
                DamageClock = 0;
            }
            else
                LavaMode.DealPainThisFrame = false;
        }
        private static void CompensateForTreadmill()
        {
            WaterBlock.InvertPrevForces();
            WaterTank.UpdateAllReversed();
        }

        bool CameraSubmerged = false;
        FogMode fM = RenderSettings.fogMode;
        TOD_FogType todFt;
        TOD_AmbientType todAt;
        float fDens = RenderSettings.fogDensity;
        Color fogColor = RenderSettings.fogColor;
        Color ambientLight = RenderSettings.ambientLight;

        static Color underwaterColor = new Color(0, 0.2404828f, 1f, 0.5f);
        static Color underLavaColor = new Color(0.97f, 0.41f, 0.024f, 0.5f);

        Gradient dayFogColors;
        Gradient nightFogColors;
        Gradient dayLightColors;
        Gradient nightLightColors;
        Gradient daySkyColors;
        Gradient nightSkyColors;
        Gradient underWaterSkyColors = new Gradient()
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            },

            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(underwaterColor, 0f),
                new GradientColorKey(underwaterColor, 1f),
            }
        };
        Gradient underLavaSkyColors = new Gradient()
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            },

            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(underLavaColor, 0f),
                new GradientColorKey(underLavaColor, 1f),
            }
        };


        private void Update()
        {
            if (!IsActive)
            {
                gameObject.SetActive(false);
                folder.transform.position = Vector3.down * 2000f;
                return;
            }
            try
            {
                var sky = m_Sky.GetValue(ManTimeOfDay.inst) as TOD_Sky;
                if (Camera.main.transform.position.y < HeightCalc)
                {
                    if (!CameraSubmerged)
                    {
                        CameraSubmerged = true;
                        fogColor = RenderSettings.fogColor;
                        ambientLight = RenderSettings.ambientLight;

                        RenderSettings.fogDensity = 5f;
                        RenderSettings.fog = true;
                        RenderSettings.fogMode = FogMode.Linear;
                        RenderSettings.fogStartDistance = 0f;
                        RenderSettings.fogEndDistance = 40f;

                        todFt = sky.Fog.Mode;
                        todAt = sky.Ambient.Mode;
                        dayFogColors = sky.Day.FogColor;
                        nightFogColors = sky.Night.FogColor;
                        daySkyColors = sky.Day.SkyColor;
                        nightSkyColors = sky.Night.SkyColor;
                        dayLightColors = sky.Day.LightColor;
                        nightLightColors = sky.Night.LightColor;
                    }
                }
                else if (CameraSubmerged)
                {
                    CameraSubmerged = false;
                    RenderSettings.fogMode = fM;
                    RenderSettings.fogDensity = fDens;
                    sky.Fog.Mode = todFt;

                    sky.Ambient.Mode = todAt;
                    RenderSettings.fogColor = fogColor;
                    RenderSettings.ambientLight = ambientLight;

                    sky.Day.FogColor = dayFogColors;
                    sky.Night.FogColor = nightFogColors;
                    sky.Day.SkyColor = daySkyColors;
                    sky.Night.SkyColor = nightSkyColors;
                    sky.Day.LightColor = dayLightColors;
                    sky.Night.LightColor = nightLightColors;
                    sky.m_UseTerraTechBiomeData = true;
                }

                if (CameraSubmerged)
                {
                    sky.m_UseTerraTechBiomeData = false;
                    sky.Fog.Mode = TOD_FogType.None;
                    sky.Ambient.Mode = TOD_AmbientType.None;

                    var multiplier = Mathf.Approximately(AbyssDepth, 0) ? 1 : 1 - (Mathf.Max(HeightCalc - Camera.main.transform.position.y, 0) / AbyssDepth);
                    Color abyssColor;
                    if (QPatch.TheWaterIsLava)
                    {
                        abyssColor = underwaterColor * multiplier;
                        abyssColor.a = 1f;
                        RenderSettings.fogColor = abyssColor;
                        RenderSettings.ambientLight = abyssColor;
                        RenderSettings.ambientGroundColor = abyssColor;
                        RenderSettings.ambientIntensity = 1 - multiplier;
                        underLavaSkyColors.colorKeys[0].color = underLavaSkyColors.colorKeys[1].color = abyssColor;

                        sky.Day.FogColor = sky.Night.FogColor = sky.Day.LightColor = sky.Night.LightColor = sky.Day.SkyColor = sky.Night.SkyColor = underLavaSkyColors;
                    }
                    else
                    {
                        abyssColor = underwaterColor * multiplier;
                        abyssColor.a = 1f;
                        RenderSettings.fogColor = abyssColor;
                        RenderSettings.ambientLight = abyssColor;
                        RenderSettings.ambientGroundColor = abyssColor;
                        RenderSettings.ambientIntensity = 1 - multiplier;
                        underWaterSkyColors.colorKeys[0].color = underWaterSkyColors.colorKeys[1].color = abyssColor;

                        sky.Day.FogColor = sky.Night.FogColor = sky.Day.LightColor = sky.Night.LightColor = sky.Day.SkyColor = sky.Night.SkyColor = underWaterSkyColors;
                    }
                }

                ManNetwork mp = ManNetwork.inst;
                bool flag = false;
                if (mp != null && mp.IsMultiplayer())
                {
                    flag = true;
                }

                if (Input.GetKeyDown(QPatch.key) && !Input.GetKey(KeyCode.LeftShift))
                {
                    if (flag)
                    {
                        try
                        {
                            if (!ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                            {
                                if (ManNetwork.IsHost)
                                {
                                    ShowGUI = !ShowGUI;
                                    waterGUI.gameObject.SetActive(ShowGUI);
                                }
                                else
                                {
                                    Console.WriteLine("Tried to change water, but is a client!");
                                }
                            }
                            else
                            {
                                ManUI.inst.ShowErrorPopup("You cannot use water in this gamemode");
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        ShowGUI = !ShowGUI;
                        waterGUI.gameObject.SetActive(ShowGUI);
                        if (!ShowGUI)
                        {
                            QPatch._thisMod.WriteConfigJsonFile();
                        }
                    }

                }
                if (flag && !ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                {
                    NetHeightSmooth = NetHeightSmooth * 0.9f + NetworkHandler.ServerWaterHeight * 0.1f;
                }
                folder.transform.position = new Vector3(Singleton.camera.transform.position.x, HeightCalc, Singleton.camera.transform.position.z);
                if (_WeatherMod && !flag)
                {
                    float dTime = Time.deltaTime;
                    float newHeight = RainFlood;
                    //newHeight += WeatherMod.RainWeight * RainWeightMultiplier * dTime;
                    newHeight *= 1f - RainDrainMultiplier * dTime;
                    RainFlood += Mathf.Clamp(newHeight - RainFlood, -FloodChangeClamp * dTime, FloodChangeClamp * dTime);
                }
            }
            catch { }
        }

        internal static bool PistonHeart = false;

        internal static void WorldShift()
        {
            Debug.Log("World beginning shift");
            PistonHeart = !PistonHeart;
            WorldMove = true;
            CompensateForTreadmill();
        }
        internal static void WorldShiftEnd(IntVector3 vec)
        {
            WorldMove = false;
            SurfacePool.TreadmillAllParticles(vec);
            //WaterBlock.MassApplyForces();
            Debug.Log("World ended shift");
        }

        public static List<WaterLook> waterLooks = new List<WaterLook>();


        public static void Initiate()
        {
            ManWorldTreadmill.inst.OnBeforeWorldOriginMove.Subscribe(WorldShift);
            ManWorldTreadmill.inst.OnAfterWorldOriginMoved.Subscribe(WorldShiftEnd);
            try
            {
                // WATER BASIC
                CameraFilter = new Texture2D(32, 32);
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        CameraFilter.SetPixel(i, j, new Color(
                            0.75f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.015f,
                            0.8f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.01f,
                            0.9f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.02f,
                            0.28f));
                    }
                }
                CameraFilter.Apply();

                var tempGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Mesh plane = Instantiate(tempGO.GetComponent<MeshFilter>().mesh);

                var shader = Shader.Find("Standard");
                //var shader = Shader.Find("Shield");
                //var shader = Shader.Find("Unlit/Transparent");
                //var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
                if (!shader)
                {
                    IEnumerable<Shader> shaders = Resources.FindObjectsOfTypeAll<Shader>();
                    shaders = shaders.Where(s => s.name == "Standard"); ////Standard
                    shader = shaders.ElementAt(1);
                }
                var defaultWater = new Material(shader)
                {
                    renderQueue = 3000,//3000
                    //color = new Color(0.2f, 0.8f, 0.75f, 0.4f)
                    color = new Color(0.2f, 0.8f, 0.75f, 0.325f)
                };
                defaultWater.SetFloat("_Mode", 2f);
                defaultWater.SetFloat("_Metallic", 0.4f);
                defaultWater.SetFloat("_Glossiness", 0.6f);
                defaultWater.SetInt("_SrcBlend", 5);
                defaultWater.SetInt("_DstBlend", 10);
                defaultWater.SetInt("_ZWrite", 0);
                defaultWater.DisableKeyword("_ALPHATEST_ON");
                defaultWater.EnableKeyword("_ALPHABLEND_ON");
                defaultWater.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                defaultWater.EnableKeyword("_EMISSION");
                defaultWater.SetColor("_Color", new Color(0.2f, 0.8f, 0.75f, 0.325f));
                defaultWater.SetColor("_EmissionColor", new Color(0.05f, 0.125f, 0.2f, 1f));

                waterLooks.Add(new WaterLook()
                {
                    name = "Default",
                    material = defaultWater,
                    mesh = plane
                });

                // LAVA BASIC
                CameraFilterLava = new Texture2D(32, 32);
                for (int i = 0; i < 32; i++)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        CameraFilterLava.SetPixel(i, j, new Color(
                            0.97f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.015f,
                            0.41f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.01f,
                            0.024f - (Mathf.Abs(i - 16f) + Mathf.Abs(j - 16f)) * 0.005f,
                            1f));//0.28f
                    }
                }
                CameraFilterLava.Apply();

                defaultLava = new Material(shader)
                {
                    renderQueue = 3000,//3000
                    color = new Color(0.97f, 0.41f, 0.024f, 0.9f)
                    
                };
                defaultLava.SetFloat("_Mode", 2f);
                defaultLava.SetFloat("_Metallic", 0.5f);
                defaultLava.SetFloat("_Glossiness", 0.9f);
                defaultLava.SetInt("_SrcBlend", 5);
                defaultLava.SetInt("_DstBlend", 10);
                defaultLava.SetInt("_ZWrite", 0);
                defaultLava.SetColor("_Color", new Color(1f, 0.5f, 0.2f, 0.79f));
                //defaultLava.SetColor("_EmissionColor", new Color(0.97f, 0.46f, 0.1f, 0.5f));
                defaultLava.SetColor("_EmissionColor", new Color(0.97f, 0.3f, 0.07f, 0.5f));
 
                defaultLava.DisableKeyword("_ALPHATEST_ON");
                defaultLava.EnableKeyword("_ALPHABLEND_ON");
                defaultLava.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                defaultLava.EnableKeyword("_EMISSION");
                /*
                waterLooks.Add(new WaterLook()
                {
                    name = "Lava",
                    material = defaultLava,
                    mesh = plane
                });
                */

                // WATER FANCY (Waveless)
                Material fancyWavelessWater = new Material(QPatch.assetBundle.LoadAllAssets<Shader>().First(s => s.name == "Shader Forge/CartoonWaterWaveless"));
                fancyWavelessWater.SetFloat("_UseWorldCoordinates", 1f);
                fancyWavelessWater.SetFloat("_RippleDensity", 0.25f);
                fancyWavelessWater.SetFloat("_RippleCutoff", 3.5f);

                waterLooks.Add(new WaterLook()
                {
                    name = "Fancy (waveless)",
                    material = fancyWavelessWater,
                    mesh = plane
                });

                // WATER FANCY (FULL)
                Material fancyWater = new Material(QPatch.assetBundle.LoadAsset<Shader>("CartoonWater"));
                fancyWater.SetFloat("_UseWorldCoordinates", 1f);
                fancyWater.SetFloat("_RippleDensity", 0.25f);
                fancyWater.SetFloat("_RippleCutoff", 3.5f);
                fancyWater.SetFloat("_WaveAmplitude", 5f);
                //fancyWater.SetFloat("_Tessellation", 7.5f);
                fancyWater.SetFloat("_Tessellation", 4f);

                Mesh fancyMesh = new Mesh();
                fancyMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                fancyMesh = OBJParser.MeshFromFile("Assets/plane.obj", fancyMesh);

                waterLooks.Add(new WaterLook()
                {
                    name = "Fancy",
                    material = fancyWater,
                    mesh = fancyMesh
                });


                // Construct the water the techs go in
                var folder = new GameObject("WaterObject");
                folder.transform.position = Vector3.zero;

                WaterBuoyancy.folder = folder;

                GameObject Surface = tempGO;
                Destroy(Surface.GetComponent<MeshCollider>());
                Transform component = Surface.transform; component.parent = folder.transform;
                //Surface.GetComponent<Renderer>().material = defaultWater;
                //Surface.GetComponent<Renderer>().material = defaultLava;
                if (QPatch.TheWaterIsLava)
                    Surface.GetComponent<Renderer>().material = defaultLava;
                else
                    Surface.GetComponent<Renderer>().material = defaultWater;

                //Let's make Renderer do it's job
                //  We will re-prioritize it's graphics to work like intended
                Surface.GetComponent<Renderer>().sortingOrder = -1;//Make the water appear correctly
                //Surface.GetComponent<Renderer>().allowOcclusionWhenDynamic = false;//
                Surface.GetComponent<Renderer>().receiveShadows = true;//
                //Surface.GetComponent<Renderer>().sortingLayerID = 0;//
                //Surface.GetComponent<Renderer>().sortingLayerName = "Default";//
                //Surface.GetComponent<Renderer>().rendererPriority = 0;//
                //Surface.GetComponent<Renderer>().renderingLayerMask = 1;//

                WaterBuoyancy.surface = Surface;

                component.localScale = new Vector3(2048f, 0.075f, 2048f);

                GameObject PhysicsTrigger = new GameObject("PhysicsTrigger");
                Transform PhysicsTriggerTransform = PhysicsTrigger.transform; PhysicsTriggerTransform.parent = folder.transform;
                //PhysicsTriggerTransform.localScale = new Vector3(2048f, 2048f, 2048f); PhysicsTriggerTransform.localPosition = new Vector3(0f, -1024f, 0f);
                PhysicsTriggerTransform.localScale = new Vector3(4096f, 2048f, 4096f); PhysicsTriggerTransform.localPosition = new Vector3(0f, -1024f, 0f);
                //This is bigger to suppress that blind spot when the world does the treadmill thing.  Unknown performance impact.
                PhysicsTrigger.AddComponent<BoxCollider>().isTrigger = true;

                _inst = PhysicsTrigger.AddComponent<WaterBuoyancy>();

                int waterlayer = LayerMask.NameToLayer("Water");
                for (int i = 0; i < 32; i++)
                {
                    if (i != waterlayer)
                    {
                        Physics.IgnoreLayerCollision(waterlayer, i, true);
                    }
                }

                GameObject PhysicsCollider = new GameObject("WaterCollider");
                PhysicsCollider.layer = waterlayer;
                Transform PhysicsColliderTransform = PhysicsCollider.transform; PhysicsColliderTransform.parent = folder.transform;
                PhysicsColliderTransform.localScale = new Vector3(4096f, 2048f, 4096f); PhysicsColliderTransform.localPosition = new Vector3(0f, -1024f, 0f);
                //This is bigger to suppress that blind spot when the world does the treadmill thing.  Unknown performance impact.
                PhysicsCollider.AddComponent<BoxCollider>();
                _inst.waterGUI = new GameObject().AddComponent<WaterGUI>();
                _inst.waterGUI.gameObject.SetActive(false);

                UpdateLook();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            try
            {
                WaterParticleHandler.Initialize();
                SurfacePool.Initiate();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        //setup in Initiate
        static Material defaultLava;

        public static void UpdateLook(WaterLook waterLook)
        {
            if (QPatch.TheWaterIsLava)
            {
                //var lavaLook = waterLooks.Find(delegate (WaterLook look) { return look.name == "Default"; });
                if (waterLook.name == "Fancy")
                {
                    Material lavaLook = new Material(waterLook.material);

                    lavaLook.SetColor("_BaseColor", new Color(0.97f, 0.3f, 0.07f, lavaLook.GetColor("_BaseColor").a));
                    lavaLook.SetColor("_RippleColor", new Color(1f, 0.6f, 0.2f, lavaLook.GetColor("_RippleColor").a));
                    lavaLook.SetColor("_EmissionColor", new Color(0.97f, 0.46f, 0.1f, 0.5f));
                    lavaLook.EnableKeyword("_EMISSION");

                    surface.GetComponent<Renderer>().material = lavaLook;
                }
                else // Fancywaveless refuses to change for some reason
                    surface.GetComponent<Renderer>().material = defaultLava;
                surface.GetComponent<MeshFilter>().mesh = waterLook.mesh;
            }
            else
            {
                surface.GetComponent<Renderer>().material = waterLook.material;
                surface.GetComponent<MeshFilter>().mesh = waterLook.mesh;
            }
            WaterParticleHandler.UpdateSplash();
            WaterParticleHandler.UpdateSurface();
        }
        public static void UpdateLook()
        {
            if (QPatch.TheWaterIsLava)
            {
                //var lavaLook = waterLooks.Find(delegate (WaterLook look) { return look.name == "Default"; });
                if (waterLooks[SelectedLook].name == "Fancy")
                {
                    Material lavaLook = new Material(waterLooks[SelectedLook].material);

                    lavaLook.SetColor("_BaseColor", new Color(0.97f, 0.3f, 0.07f, lavaLook.GetColor("_BaseColor").a));
                    lavaLook.SetColor("_RippleColor", new Color(1f, 0.6f, 0.2f, lavaLook.GetColor("_RippleColor").a));
                    lavaLook.SetColor("_EmissionColor", new Color(0.97f, 0.46f, 0.1f, 0.5f));
                    lavaLook.EnableKeyword("_EMISSION");

                    surface.GetComponent<Renderer>().material = lavaLook;
                }
                else // Fancywaveless refuses to change for some reason
                    surface.GetComponent<Renderer>().material = defaultLava;
                surface.GetComponent<MeshFilter>().mesh = waterLooks[SelectedLook].mesh;
            }
            else
            {
                surface.GetComponent<Renderer>().material = waterLooks[SelectedLook].material;
                surface.GetComponent<MeshFilter>().mesh = waterLooks[SelectedLook].mesh;
            }
            WaterParticleHandler.UpdateSplash();
            WaterParticleHandler.UpdateSurface();
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public struct WaterLook
        {
            public string name;
            public Mesh mesh;
            public Material material;

            public override string ToString()
            {
                return this.name;
            }
        }
    }
}
