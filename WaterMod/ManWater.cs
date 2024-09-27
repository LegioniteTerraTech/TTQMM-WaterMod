using System;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using FMODUnity;
using TerraTechETCUtil;
using UnityEngine.UI;
#if !STEAM
using QModManager.Utility;
#endif


namespace WaterMod
{
    internal static class ManWaterDefaults
    {
        public const float Density = 8,
            FanJetMultiplier = 1.75f,
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
            AbyssDepth = 50f,
            LavaDampenMulti = 3,
            WheelWaterForceMultiplier = 0.45f;
    }
    internal static class WaterGlobals
    {
        public static float Density = 8,
            FanJetMultiplier = 1.75f,
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
            AbyssDepth = 50f,
            LavaDampenMulti = 3,
            WheelWaterForceMultiplier = 0.45f;
    }
    /// <summary>
    /// Was WaterBuoyancy 
    ///   Note: Can make more efficent by localizing the float vector in relation to water
    /// </summary>
    internal class ManWater : MonoBehaviour, IWorldTreadmill
    {
        private const float physicsZoneSize = 65536f;//4096f

        private static FieldInfo m_Sky = typeof(ManTimeOfDay).GetField("m_Sky", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool UseStandards = true;
        public static float WaterSound = 0.75f;
        public static bool AlwaysShowTrails = false;
        public static Globals.ObjectLayer WaterLayer => waterLayer;
        private static Globals.ObjectLayer waterLayer = new Globals.ObjectLayer("Water");

        public static float Brightener = 0.65f;
        public static Color waterColor = new Color(0.125f, 0.25f, 0.65f, 0.875f); //new Color(0.125f, 0.35f, 0.65f, 0.875f);
        public static Color waterColorBright = new Color(Brightener, Brightener, Brightener, 0) * (new Color(1,1,1,1) - waterColor) + waterColor - new Color(0, 0, 0, 0.5f);
        public static Color lavaColor = new Color(0.97f, 0.41f, 0.024f, 0.9f);
        public static Color lavaColorBright = new Color(Brightener, Brightener, Brightener, 0) * (new Color(1, 1, 1, 1) - lavaColor) + lavaColor - new Color(0,0,0,0.5f);


        public static Texture2D CameraFilter;
        public static Texture2D CameraFilterLava; 
        public static Texture2D WaterTex;
        public static Texture2D LavaTex;

        public static float Height
        {
            get
            {
                if (QPatch.OceanMan)
                    return -50;
                return height;
            }
            set
            {
                UpdateHeightCalc();
                height = value;
            }
        }
        public static float height = -25f;
        public static float minimumBlockSleepHeight = height + BlockSleepHeightOffset;
        public static float BlockSleepHeightOffset = -8f;


        /// <summary>
        /// Editable
        /// </summary>
        public static float Density = 8,
            FanJetMultiplier = 1.75f,
            ResourceBuoyancyMultiplier = 1.2f,
            BulletDampener = 1E-06f,
            LaserFraction = 0.275f,
            MissileDampener = 0.012f,
            SurfaceSkinning = 0.25f,
            SubmergedTankDampening = 0.4f,//0.01f,
            SubmergedTankDampeningYAddition = 0.25f,//0f,
            SurfaceTankDampening = 0f,
            SurfaceTankDampeningYAddition = 0.5f,//0.0225f,
            RainWeightMultiplier = 0.06f,
            RainDrainMultiplier = 0.06f,
            FloodChangeClamp = 0.002f,
            AbyssDepth = 50f,
            LavaDampenMulti = 3,
            WheelWaterForceMultiplier = 0.45f;

        /// <summary>
        /// Non-Editable
        /// </summary>
        public const float SubmergedBlockDampening = 0.4f,
            SubmergedBlockDampeningYAddition = 0.4f,
            SurfaceBlockDampening = 0.2f,
            SurfaceBlockDampeningYAddition = 0.75f,
            BasicBlockFloatAssistMulti = 2.75f,
            SmallWheelWaterBuff = 2.25f,
            MinWheelMaxForce = 2.5f;

        public static float ApplyLava(float initialVal)
        {
            return 1f - ((1f - initialVal) / 3f);
        }
        public static void SetToCustom()
        {
            WaterGlobals.Density = Density;
            WaterGlobals.FanJetMultiplier = FanJetMultiplier;
            WaterGlobals.ResourceBuoyancyMultiplier = ResourceBuoyancyMultiplier;
            WaterGlobals.BulletDampener = BulletDampener;
            WaterGlobals.LaserFraction = LaserFraction;
            WaterGlobals.MissileDampener = MissileDampener;
            WaterGlobals.SurfaceSkinning = SurfaceSkinning;
            WaterGlobals.SubmergedTankDampening = SubmergedTankDampening;
            WaterGlobals.SubmergedTankDampeningYAddition = SubmergedTankDampeningYAddition;
            WaterGlobals.SurfaceTankDampening = SurfaceTankDampening;
            WaterGlobals.SurfaceTankDampeningYAddition = SurfaceTankDampeningYAddition;
            WaterGlobals.RainWeightMultiplier = RainWeightMultiplier;
            WaterGlobals.RainDrainMultiplier = RainDrainMultiplier;
            WaterGlobals.FloodChangeClamp = FloodChangeClamp;
            WaterGlobals.AbyssDepth = AbyssDepth;
            WaterGlobals.LavaDampenMulti = LavaDampenMulti;
            WaterGlobals.WheelWaterForceMultiplier = WheelWaterForceMultiplier;
        }
        public static void SetToStandard()
        {
            WaterGlobals.Density = ManWaterDefaults.Density;
            WaterGlobals.FanJetMultiplier = ManWaterDefaults.FanJetMultiplier;
            WaterGlobals.ResourceBuoyancyMultiplier = ManWaterDefaults.ResourceBuoyancyMultiplier;
            WaterGlobals.BulletDampener = ManWaterDefaults.BulletDampener;
            WaterGlobals.LaserFraction = ManWaterDefaults.LaserFraction;
            WaterGlobals.MissileDampener = ManWaterDefaults.MissileDampener;
            WaterGlobals.SurfaceSkinning = ManWaterDefaults.SurfaceSkinning;
            WaterGlobals.SubmergedTankDampening = ManWaterDefaults.SubmergedTankDampening;
            WaterGlobals.SubmergedTankDampeningYAddition = ManWaterDefaults.SubmergedTankDampeningYAddition;
            WaterGlobals.SurfaceTankDampening = ManWaterDefaults.SurfaceTankDampening;
            WaterGlobals.SurfaceTankDampeningYAddition = ManWaterDefaults.SurfaceTankDampeningYAddition;
            WaterGlobals.RainWeightMultiplier = ManWaterDefaults.RainWeightMultiplier;
            WaterGlobals.RainDrainMultiplier = ManWaterDefaults.RainDrainMultiplier;
            WaterGlobals.FloodChangeClamp = ManWaterDefaults.FloodChangeClamp;
            WaterGlobals.AbyssDepth = ManWaterDefaults.AbyssDepth;
            WaterGlobals.LavaDampenMulti = ManWaterDefaults.LavaDampenMulti;
            WaterGlobals.WheelWaterForceMultiplier = ManWaterDefaults.WheelWaterForceMultiplier;
        }

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
        private static Mesh WaterPlane;
        public static float waterAlpha = 0.725f;
        public static float waterAlphaMain = 0.75f;
        public static float waterAlphaRipple = 0.95f;
        public static float waterSpeed = 3.75f;
        public static float lavaSpeed = 0.25f;
        public static float lavaAlpha = 0.95f;


        private static void SetWaterHeight(float height)
        {
            Vector3 pos = _inst.transform.position.SetY(height - 1024f);
            _inst.transform.position = pos;
            splashCol.position = pos;
            foreach (var item in ActiveWaterTiles)
            {
                item.Value.UpdateTileHeight(height);
            }
        }
        public static void OnTileCreated(WorldTile tile)
        {
            try
            {
                if (!ActiveWaterTiles.ContainsKey(tile.Coord))
                {
                    WaterTile wTile = WaterTile.CreateWaterTile(tile.Coord);
                    ActiveWaterTiles.Add(tile.Coord, wTile);
                }
                else
                    DebugWater.Assert("waterTile at " + tile.Coord.ToString() + " already existed when attempting to spawn a new one!");
            }
            catch (Exception e)
            {
                throw new Exception("OnTileCreated failed", e);
            }
        }
        public static void OnTileDestroyed(WorldTile tile)
        {
            try
            {
                if (ActiveWaterTiles.TryGetValue(tile.Coord, out WaterTile val))
                {
                    val.Recycle();
                    ActiveWaterTiles.Remove(tile.Coord);
                }/*
            else
                DebugWater.Info("waterTile at " + tile.Coord.ToString() + " did not exist when attempting to destroy it!");
            */
            }
            catch (Exception e)
            {
                throw new Exception("OnTileDestroyed failed", e);
            }
        }
        public static void OnTilePopulated(WorldTile tile)
        {
        }
        public static void OnTileDepopulated(WorldTile tile)
        {
        }

        public static void OnDayStateChanged(Mode unused)
        {

        }

        public static void OnModeFinishedLoading(Mode unused)
        {
            try
            {
                UpdateLook();
            }
            catch (Exception e)
            {
                throw new Exception("OnModeFinishedLoading failed", e);
            }
        }


        private static Vector3 posPrev;
        private static float distDelta = ManWorld.inst.TileSize / 5;
        private static void UpdateAllTilesLOD(IntVector2 coord, Vector3 PosScene)
        {
            if (!posPrev.Approximately(PosScene, distDelta))
            {
                //DebugWater.Log("Updated Tile LODs");
                posPrev = PosScene;
                Vector3 PosVsCenter = PosScene - ManWorld.inst.TileManager.CalcTileCentreScene(coord);
                if (PosVsCenter.x > 0)
                {
                    if (PosVsCenter.y > 0)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            for (int y = -1; y <= 1; y++)
                            {
                                if (ActiveWaterTiles.TryGetValue(new IntVector2(x + coord.x, y + coord.y), out WaterTile wTile))
                                {
                                    //wTile.IsClose = x <=  1 && y <= 1 && x >= 0 && y >= 0;
                                    wTile.UpdateLook();
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            for (int y = -1; y <= 1; y++)
                            {
                                if (ActiveWaterTiles.TryGetValue(new IntVector2(x + coord.x, y + coord.y), out WaterTile wTile))
                                {
                                    //wTile.IsClose = x <= 1 && y <= 0 && y >= 0 && y >= -1;
                                    wTile.UpdateLook();
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (PosVsCenter.y > 0)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            for (int y = -1; y <= 1; y++)
                            {
                                if (ActiveWaterTiles.TryGetValue(new IntVector2(x + coord.x, y + coord.y), out WaterTile wTile))
                                {
                                    //wTile.IsClose = x <= 0 && y <= 1 && x >= -1 && y >= 0;
                                    wTile.UpdateLook();
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            for (int y = -1; y <= 1; y++)
                            {
                                if (ActiveWaterTiles.TryGetValue(new IntVector2(x + coord.x, y + coord.y), out WaterTile wTile))
                                {
                                    //wTile.IsClose = x <= 0 && y <= 0 && x >= -1 && y >= -1;
                                    wTile.UpdateLook();
                                }
                            }
                        }
                    }
                }
            }
        }
        private static void UpdateAllTiles()
        {
            foreach (var item in ActiveWaterTiles)
            {
                item.Value.UpdateLook();
            }
        }
        private static void ClearAllTiles()
        {
            foreach (var item in ActiveWaterTiles)
            {
                item.Value.Recycle();
            }
            ActiveWaterTiles.Clear();
        }
        

        public static void CreateCameraFilters()
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
            DebugWater.Log("Water Mod: new CameraFilter for water effect");

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
        }


        //setup in Initiate
        internal static Material defaultWater;
        internal static Material defaultLava;
        internal static Material FancyLava;
        private static Color waterEmissionColor = new Color(0.05f, 0.125f, 0.2f, 0.2f);
        private static Color lavaEmissionColor = new Color(1, 0.26f, 0.17f, 0.75f);
        private static List<Material> matsMain = new List<Material>();

        internal static Shader InsureGetShader(string name)
        {
            //var shader = Shader.Find("Standard");
            Shader shader = Shader.Find(name);
            //var shader = Shader.Find("Shield");
            //var shader = Shader.Find("Unlit/Transparent");
            //var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            if (shader == null)
            {
                IEnumerable<Shader> shaders = Resources.FindObjectsOfTypeAll<Shader>();
                /*
                foreach (var item in shaders)
                {
                    if (item && !item.name.NullOrEmpty())
                        DebugWater.Log(item.name);
                }
                */
                shaders = shaders.Where(s => s.name == name); ////Standard
                shader = shaders.ElementAt(0);
                if (shader == null)
                    DebugWater.Log("Water Mod: failed to get shader");
            }
            return shader;
        }
        public static void CreateTextures()
        {
            TerraTechETCUtil.DebugExtUtilities.AllowEnableDebugGUIMenu_KeypadEnter = true;

            Shader shader = InsureGetShader("Legacy Shaders/Particles/Additive");//InsureGetShader("Legacy Shaders/Transparent/Diffuse")
            //Shader shader = InsureGetShader("Standard");
            WaterTex = new Texture2D(2, 2);
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    WaterTex.SetPixel(i, j, waterColor);
            WaterTex.Apply();

            defaultWater = new Material(shader)
            {
                renderQueue = 3000,//3000
                                   //color = new Color(0.2f, 0.8f, 0.75f, 0.4f)
                mainTexture = WaterTex,
                color = waterColor,
                shaderKeywords = new string[] { },
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive,
                doubleSidedGI = true,
            };
            defaultWater.SetFloat("_Mode", 3f);
            defaultWater.SetFloat("_Metallic", 0f);//0.4f);
            defaultWater.SetFloat("_Glossiness", 0.6f);
            defaultWater.SetFloat("_GlossyReflections", 1f);
            defaultWater.SetFloat("_InvFade", 3);
            defaultWater.SetFloat("_SmoothnessTextureChannel", 0);
            //defaultWater.SetFloat("_SrcBlend", 5);
            //defaultWater.SetInt("_DstBlend", 10);
            defaultWater.SetFloat("_SrcBlend", 1);
            defaultWater.SetFloat("_DstBlend", 10);
            defaultWater.SetFloat("_UVSec", 0);
            defaultWater.SetFloat("_ZWrite", 0);
            defaultWater.SetColor("_TintColor", waterColorBright.SetAlpha(0.1f));

            defaultWater.SetOverrideTag("RenderType", "Transparent");
            //defaultWater.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            defaultWater.EnableKeyword("_ALPHABLEND_ON");
            //defaultWater.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            defaultWater.DisableKeyword("_EMISSION");
            defaultWater.SetColor("_Color", waterColor);
            defaultWater.SetColor("_EmissionColor", waterEmissionColor);
            matsMain.Add(defaultWater);

            DebugWater.Log("Water Mod: new Water plane");
            

            //shader = InsureGetShader("Legacy Shaders/Particles/Additive");
            LavaTex = new Texture2D(2, 2);
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    LavaTex.SetPixel(i, j, lavaColor);
            LavaTex.Apply();

            defaultLava = new Material(shader)
            {
                renderQueue = 3000,//3000
                mainTexture = LavaTex,
                color = lavaColor,
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.None,
            };
            defaultLava.SetFloat("_Mode", 3f);
            defaultLava.SetFloat("_Metallic", 5f);//0.5f);
            defaultLava.SetFloat("_Glossiness", 0.9f);
            defaultLava.SetInt("_SrcBlend", 5);
            defaultLava.SetInt("_DstBlend", 10);
            defaultLava.SetInt("_ZWrite", 0);
            defaultLava.SetColor("_Color", lavaColor.SetAlpha(1));
            //defaultLava.SetColor("_EmissionColor", new Color(0.97f, 0.46f, 0.1f, 0.5f));
            defaultLava.SetColor("_EmissionColor", lavaEmissionColor.SetAlpha(1));
            defaultLava.SetColor("_TintColor", lavaColorBright.SetAlpha(0.73f));

            defaultLava.DisableKeyword("_ALPHATEST_ON");
            defaultLava.EnableKeyword("_ALPHABLEND_ON");
            defaultLava.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            defaultLava.EnableKeyword("_EMISSION");
            /*
            waterLooks.Add(new WaterLook()
            {
                name = "Lava",
                material = defaultLava,
                mesh = WaterPlane
            });
            */
            matsMain.Add(defaultLava);
            DebugWater.Log("Water Mod: new Lava plane");
            waterLooks.Add(new WaterLook(0.05f, "Default", WaterPlane, defaultWater, defaultLava, 0, 128));


            // WATER FANCY (Waveless)
            //Material fancyWavelessWater = new Material(QPatch.assetBundle.LoadAllAssets<Shader>().First(s => s.name == "Shader Forge/CartoonWaterWaveless"));
            Material fancyWavelessWater = new Material(QPatch.ModHandle.GetModContainer().GetObjectFromModContainer<Shader>("Shader Forge/CartoonWaterWaveless"));
            fancyWavelessWater.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            fancyWavelessWater.SetFloat("_UseWorldCoordinates", 1f);
            fancyWavelessWater.SetFloat("_Speed", waterSpeed);
            fancyWavelessWater.SetFloat("_RippleDensity", 0.25f);
            fancyWavelessWater.SetFloat("_RippleCutoff", 3.5f);
            fancyWavelessWater.DisableKeyword("_EMISSION");
            fancyWavelessWater.SetFloat("_Cutoff", 0.3f);
            fancyWavelessWater.SetOverrideTag("RenderType", "Transparent");

            /*
            fancyWavelessWater.SetColor("_BaseColor_copy", new Color(0.1f, 0.3f, 0.56f, fancyWavelessWater.GetColor("_BaseColor_copy").a * waterAlphaMain));
            fancyWavelessWater.SetColor("_RippleColor_copy", new Color(0.18f, 0.5f, 1f, fancyWavelessWater.GetColor("_RippleColor_copy").a * waterAlphaRipple));
            */
            fancyWavelessWater.SetColor("_BaseColor_copy", waterColor.SetAlpha(fancyWavelessWater.GetColor("_BaseColor_copy").a * waterAlphaMain));
            fancyWavelessWater.SetColor("_RippleColor_copy", waterColorBright.SetAlpha(fancyWavelessWater.GetColor("_RippleColor_copy").a * waterAlphaRipple));
            fancyWavelessWater.SetColor("_EmissionColor", waterEmissionColor);
            
            fancyWavelessWater.SetFloat("_Metallic", 0.5f);
            fancyWavelessWater.SetFloat("_Glossiness", 0.9f);
            fancyWavelessWater.SetFloat("_Opacity", waterAlpha);
            matsMain.Add(fancyWavelessWater);

            Material FancyWavelessLava = new Material(fancyWavelessWater);
            FancyWavelessLava.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            //0.97f
            /*
            FancyWavelessLava.SetColor("_BaseColor_copy", new Color(0.47f, 0.3f, 0.07f, 1));
            FancyWavelessLava.SetColor("_RippleColor_copy", new Color(1f, 0.6f, 0.2f, 1));
            */
            FancyWavelessLava.SetColor("_BaseColor_copy", lavaColor.SetAlpha(1));
            FancyWavelessLava.SetColor("_RippleColor_copy", lavaColorBright.SetAlpha(1));
            FancyWavelessLava.SetColor("_EmissionColor", lavaEmissionColor);
            FancyWavelessLava.EnableKeyword("_EMISSION");
            FancyWavelessLava.SetFloat("_Speed", lavaSpeed);
            FancyWavelessLava.SetFloat("_RippleDensity", 0.075f);
            FancyWavelessLava.SetFloat("_RippleCutoff", 1.5f);
            FancyWavelessLava.SetFloat("_Opacity", lavaAlpha);
            matsMain.Add(FancyWavelessLava);


            waterLooks.Add(new WaterLook(0.05f, "Fancy (waveless)", WaterPlane, fancyWavelessWater, 
                FancyWavelessLava, 1, 256));

            // WATER FANCY (FULL)
            //Material fancyWater = new Material(QPatch.assetBundle.LoadAsset<Shader>("CartoonWater"));
            Material fancyWater = new Material(QPatch.ModHandle.GetModContainer().GetObjectFromModContainer<Shader>("Shader Forge/CartoonWater"));
            fancyWater.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            fancyWater.SetFloat("_UseWorldCoordinates", 1f);
            fancyWater.SetFloat("_Speed", waterSpeed);
            fancyWater.SetFloat("_RippleDensity", 0.25f);
            fancyWater.SetFloat("_RippleCutoff", 3.5f);
            fancyWater.SetFloat("_WaveAmplitude", 5f);
            //fancyWater.SetFloat("_Tessellation", 7.5f);
            fancyWater.SetFloat("_Tessellation", 4f);
            fancyWater.DisableKeyword("_EMISSION");
            fancyWater.SetFloat("_Cutoff", 0.3f);
            fancyWater.SetOverrideTag("RenderType", "Transparent");

            /*
            fancyWater.SetColor("_BaseColor", new Color(0.1f, 0.3f, 0.56f, fancyWater.GetColor("_BaseColor").a * waterAlphaMain));
            fancyWater.SetColor("_RippleColor", new Color(0.18f, 0.5f, 1f, fancyWater.GetColor("_RippleColor").a * waterAlphaRipple));
            */
            fancyWater.SetColor("_BaseColor", waterColor.SetAlpha(fancyWater.GetColor("_BaseColor").a * waterAlphaMain));
            fancyWater.SetColor("_RippleColor", waterColorBright.SetAlpha(fancyWater.GetColor("_RippleColor").a * waterAlphaRipple));
            fancyWater.SetColor("_EmissionColor", waterEmissionColor);

            fancyWater.SetFloat("_Metallic", 0.5f);
            fancyWater.SetFloat("_Glossiness", 0.9f);
            fancyWater.SetFloat("_Opacity", waterAlpha);
            matsMain.Add(fancyWater);

            /*
            Mesh fancyMesh = new Mesh();
            fancyMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            fancyMesh = OBJParser.MeshFromFile("ExtAssets/plane.obj", fancyMesh);
            */
            Mesh fancyMesh = QPatch.ModHandle.GetModContainer().GetMeshFromModAssetBundle("plane");

            FancyLava = new Material(fancyWater);
            FancyLava.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            /*
            FancyLava.SetColor("_BaseColor", new Color(0.47f, 0.3f, 0.07f, 1));
            FancyLava.SetColor("_RippleColor", new Color(1f, 0.6f, 0.2f, 1));
            */
            FancyLava.SetColor("_BaseColor", lavaColor.SetAlpha(1));
            FancyLava.SetColor("_RippleColor", lavaColorBright.SetAlpha(1));
            FancyLava.SetColor("_EmissionColor", lavaEmissionColor);
            FancyLava.EnableKeyword("_EMISSION");
            FancyLava.SetFloat("_Speed", lavaSpeed);
            FancyLava.SetFloat("_RippleDensity", 0.075f);
            FancyLava.SetFloat("_RippleCutoff", 1.5f);
            FancyLava.SetFloat("_WaveAmplitude", 2.25f);
            FancyLava.SetFloat("_Opacity", lavaAlpha);
            matsMain.Add(FancyLava);

            waterLooks.Add(new WaterLook(0.5f, "Fancy", fancyMesh, fancyWater, FancyLava, 1, 512));
            DebugWater.Log("Water Mod: new Water fancy plane");
            SetShaders(AlwaysShowTrails);
        }
        public static void SetShaders(bool ShowTrailsAboveAll)
        {
            foreach (var item in matsMain)
            {
                item.SetInt("_ZWrite", ShowTrailsAboveAll ? 0 : 1);
            }
        }

        internal static int blockSFXCount = 0;
        internal static bool attemptedSounds = false;
        internal static AudioInst SplashSmall;
        internal static AudioInst SplashMedium;
        internal static AudioInst SplashLarge;
        internal static AudioInst WaterNoises;
        public static void GetSounds()
        {
            if (attemptedSounds)
                return;
            attemptedSounds = true;
            //ResourcesHelper.ShowDebug = true;
            string dllDir = System.IO.Path.Combine(Assembly.GetCallingAssembly().Location, "../ExtAssets");
            ModContainer MC = QPatch.ModHandle.GetModContainer();
            SplashLarge = ResourcesHelper.FetchSound(MC, "SplashLarge.wav", dllDir);
            if (SplashLarge != null)
            {
                SplashLarge.Volume = 0.125f;
                SplashLarge.PitchVariance = 0.25f;
            }
            else
                throw new NullReferenceException("Could not fetch SplashLarge");
            SplashMedium = ResourcesHelper.FetchSound(MC, "SplashMedium.wav", dllDir);
            if (SplashMedium != null)
            {
                SplashMedium.Volume = 0.1f;
                SplashMedium.PitchVariance = 0.325f;
            }
            else
                throw new NullReferenceException("Could not fetch SplashMedium");
            SplashSmall = ResourcesHelper.FetchSound(MC, "SplashSmall.wav", dllDir);
            if (SplashSmall != null)
            {
                SplashSmall.Volume = 0.095f;
                SplashSmall.PitchVariance = 0.4f;
            }
            else
                throw new NullReferenceException("Could not fetch SplashSmall");
            WaterNoises = ResourcesHelper.FetchSound(MC, "MovingWater.wav", dllDir);
            if (WaterNoises != null)
            {
                WaterNoises.Looped = true;
            }
            else
                throw new NullReferenceException("Could not fetch WaterNoises");
        }

        private static Dictionary<IntVector2, WaterTile> ActiveWaterTiles = new Dictionary<IntVector2, WaterTile>();


        public static void Initiate()
        {
            try
            {
                ManWorldTreadmill.inst.OnBeforeWorldOriginMove.Subscribe(WorldShift);
                ManWorldTreadmill.inst.OnAfterWorldOriginMoved.Subscribe(WorldShiftEnd);
                //*
                ManWorld.inst.TileManager.TileCreatedEvent.Subscribe(OnTileCreated);
                ManWorld.inst.TileManager.TileDestroyedEvent.Subscribe(OnTileDestroyed);
                ManWorld.inst.TileManager.TileStartPopulatingEvent.Subscribe(OnTilePopulated);
                ManWorld.inst.TileManager.TileDepopulatedEvent.Subscribe(OnTileDepopulated);
                // */
                ManGameMode.inst.ModeStartEvent.Subscribe(OnModeFinishedLoading);
                foreach (var item in FindObjectsOfType<Tank>())
                {
                    WaterTank.Insure(item);
                }
                ManWorldTreadmill.inst.AddWorldSpaceObject(new WaterParticleHandler());
            }
            catch (Exception e)
            {
                DebugWater.LogException(e);
                throw new Exception("Fail on init ManWater hooks", e);
            }
            try
            {
                var tempGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
                WaterPlane = Instantiate(tempGO.GetComponent<MeshFilter>().mesh);
                Destroy(tempGO);
                CreateCameraFilters();
                CreateTextures();
                GetSounds();



                // Construct the water the techs go in
                var folder = new GameObject("WaterObject");
                folder.transform.position = Vector3.zero;

                PhysicsZone = folder;

                /*
                GameObject Surface = tempGO;
                Destroy(Surface.GetComponent<MeshCollider>());
                Transform component = Surface.transform; component.parent = folder.transform;
                //Surface.GetComponent<Renderer>().material = defaultWater;
                //Surface.GetComponent<Renderer>().material = defaultLava;
                if (QPatch.TheWaterIsLava)
                    Surface.GetComponent<Renderer>().material = defaultLava;
                else
                    Surface.GetComponent<Renderer>().material = defaultWater;

                WaterBuoyancy.surface = Surface;

                component.localScale = new Vector3(2048f, 0.075f, 2048f);
                */

                GameObject PhysicsTrigger = new GameObject("PhysicsTrigger");
                Transform PhysicsTriggerTransform = PhysicsTrigger.transform;
                //PhysicsTriggerTransform.parent = folder.transform;
                //PhysicsTriggerTransform.localScale = new Vector3(2048f, 2048f, 2048f); PhysicsTriggerTransform.localPosition = new Vector3(0f, -1024f, 0f);
                PhysicsTriggerTransform.localScale = Vector3.one; 
                PhysicsTriggerTransform.localPosition = new Vector3(0f, -1024f, 0f);
                //This is bigger to suppress that blind spot when the world does the treadmill thing.  Unknown performance impact.
                seaCol = PhysicsTrigger.AddComponent<BoxCollider>();
                seaCol.isTrigger = true;
                seaCol.size = new Vector3(physicsZoneSize, 2048f, physicsZoneSize);

                _inst = PhysicsTrigger.AddComponent<ManWater>();
                ManUpdate.inst.AddAction(ManUpdate.Type.Update, ManUpdate.Order.Last, _inst.RemoteUpdate, 1009001);
                ManUpdate.inst.AddAction(ManUpdate.Type.FixedUpdate, ManUpdate.Order.Last, _inst.RemoteFixedUpdate, 1009001);


                for (int i = 0; i < 32; i++)
                {
                    if (i != waterLayer)
                    {
                        Physics.IgnoreLayerCollision(waterLayer, i, true);
                    }
                }
                //Physics.IgnoreLayerCollision(waterLayer, LayerMask.NameToLayer("Tank"), false);


                GameObject PhysicsCollider = new GameObject("WaterCollider");
                PhysicsCollider.layer = waterLayer;
                Transform PhysicsColliderTransform = PhysicsCollider.transform;
                PhysicsColliderTransform.parent = folder.transform;
                PhysicsColliderTransform.localScale = Vector3.one;
                PhysicsColliderTransform.localPosition = new Vector3(0f, -1024f, 0f);
                //This is bigger to suppress that blind spot when the world does the treadmill thing.  Unknown performance impact.
                var boxCol = PhysicsCollider.AddComponent<BoxCollider>();
                boxCol.size = new Vector3(physicsZoneSize, 2048f, physicsZoneSize);

                splashCol = PhysicsCollider.transform;

                
                _inst.waterGUI = new GameObject().AddComponent<WaterGUI>();
                _inst.waterGUI.gameObject.SetActive(false);

                ManHUD.inst.OnExpandHUDElementEvent.Subscribe(_inst.TryFix);
                ManHUD.inst.OnShowHUDElementEvent.Subscribe(_inst.TryFix);

                DebugWater.Log("Water Mod: Ready!");

            }
            catch (Exception e)
            {
                DebugWater.LogException(e);
                throw new Exception("Fail on init ManWater", e);
            }
            try
            {
                WaterParticleHandler.Initialize();
                SurfacePool.Initiate();
            }
            catch (Exception e)
            {
                DebugWater.LogException(e);
                throw new Exception("Fail on init WaterParticleHandler or SurfacePool", e);
            }
        }
        public void TryFix(UIHUDElement ele)
        {
            try
            {
                if (ele.HudElementType == ManHUD.HUDElementType.TechLoader)
                {
                    UIHUDElement tech2 = ManHUD.inst.GetHudElement(ManHUD.HUDElementType.TechLoader);
                    //Utilities.LogGameObjectHierachy(tech2.gameObject);
                    RectTransform RT = tech2.GetComponent<RectTransform>();
                    RT.anchoredPosition = Vector2.zero;//new Vector2(-Display.main.renderingWidth / 8f, Display.main.renderingHeight / 4f); 
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                SafeInit.Save();
            }
            catch { }
        }

        public static float HeightCalc => heightCalcSet;
        public static float heightCalcSet = -50;//-25;
        public static float heightCalc = -50;//-25;

        public static float RainFlood {
            get { return rainFlood; }
            set 
            {
                UpdateHeightCalc();
                rainFlood = value;
            }
        }

        private static float rainFlood = 0f;

        public byte heartBeat;
        public static ManWater _inst;
        public static BoxCollider seaCol;
        public static Transform splashCol;
        public static GameObject PhysicsZone;
        public static bool _WeatherMod;
        private bool ShowGUI = false;
        private WaterGUI waterGUI;
        //public static GameObject surface;
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

            public void DelayedSave()
            {
                try
                {
                    SafeInit.Save();
                }
                catch { }
            }
            private void GUIWindow(int ID)
            {
                GUILayout.Label("Height: " + Height.ToString());
                int height = Mathf.RoundToInt(GUILayout.HorizontalSlider(Mathf.RoundToInt(Height / 5), -15f, 20f)) * 5;
                if (Height != height)
                {
                    CancelInvoke("DelayedSave");
                    Height = height;
                    Invoke("DelayedSave", 1f);
                }

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
            UpdateAllTiles();
            UpdateHeightCalc();
            if (!IsActive)
                ManTimeOfDayExt.RemoveState("WM");
        }



        internal bool Heart = false;

        internal static void UpdateHeightCalc()
        {
            if (IsActive)
            {
                heightCalc = Height + (rainFlood * floodHeightMultiplier);
                minimumBlockSleepHeight = Height + BlockSleepHeightOffset;
            }
            else
            {
                heightCalc = -1024f;
                minimumBlockSleepHeight = -1024f;
                heightCalcSet = -1024f;
            }
            SetWaterHeight(heightCalc);
        }

        public void OnTriggerStay(Collider collider)
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

        public void OnTriggerEnter(Collider collider)
        {
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.Ent(heartBeat);
            }
        }

        public void OnTriggerExit(Collider collider)
        {
            var wEffect = collider.GetComponentInParent<WaterEffect>();

            if (wEffect != null)
            {
                wEffect.Ext(heartBeat);
            }
        }

        bool techloaderBroken = true;
        float DamageClock = 0;
        private void RemoteFixedUpdate()
        {
            heartBeat++;    // Updates the water
            DamageClock += Time.fixedDeltaTime;
            if (Input.GetKey(KeyCode.Keypad6))
                techloaderBroken = true;
            /*
            if (Input.GetKey(KeyCode.Keypad8))
            {// you should Scrap yourself.  NOW!
                UIScreenTechLoader tech = (UIScreenTechLoader)ManUI.inst.GetScreen(ManUI.ScreenType.TechLoaderScreen);
                UITechSelector select = tech.GetComponentInChildren<UITechSelector>(true);
                DestroyImmediate(select);
            }
            if (Input.GetKey(KeyCode.Keypad9))
            {// you should Scrap yourself.  NOW!
                UIHUDElement tech2 = ManHUD.inst.GetHudElement(ManHUD.HUDElementType.TechLoader);
                DestroyImmediate(tech2);
            }*/

            /*
            if (techloaderBroken && ManGameMode.inst.GetIsInPlayableMode())
            {
                techloaderBroken = false;
                try
                {
                    ManHUD.inst.InitialiseHudElement(ManHUD.HUDElementType.TechLoader);
                    UIHUDElement tech2 = ManHUD.inst.GetHudElement(ManHUD.HUDElementType.TechLoader);
                    //Utilities.LogGameObjectHierachy(tech2.gameObject);
                    RectTransform RT = tech2.GetComponent<RectTransform>();
                    RT.anchoredPosition = Vector2.zero;//new Vector2(-Display.main.renderingWidth / 8f, Display.main.renderingHeight / 4f);
                    
                }
                catch (Exception e)
                {
                    DebugWater.LogException(e);
                }
            }
            */

            if (DamageClock >= LavaMode.DamageUpdateDelay)
            {
                LavaMode.DealPainThisFrame = true;
                DamageClock = 0;
                UpdateAllTilesLOD(WorldPosition.FromScenePosition(Singleton.playerPos).TileCoord, Singleton.playerPos);
            }
            else
                LavaMode.DealPainThisFrame = false;
            if (QPatch.SimulateProjectiles)
                WaterObj.RemoteFixedUpdateAll();
            foreach (var item in WaterTank.All)
            {
                item.RemoteFixedUpdate();
            }
        }
        private static void CompensateForTreadmill()
        {
            WaterBlock.InvertPrevForces();
            WaterTank.UpdateAllReversed();
        }

        internal static bool CameraSubmerged = false;

        static Color underWaterColor = new Color(0, 0.1704828f, 1f, 0.65f);
        static Color underLavaColor = new Color(0.97f, 0.41f, 0.024f, 0.65f);

        static Gradient underWaterSkyColors = new Gradient()
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            },

            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(underWaterColor, 0f),
                new GradientColorKey(underWaterColor, 1f),
            }
        };
        static Gradient underLavaSkyColors = new Gradient()
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

        private static Color spoopy = new Color(0.005f, 0.005f, 0.025f, 1f);
        private static float multiplierUnclamped = 0;
        private static float multiplier = 0;

        public static void SetDarkness()
        {
            var sky = m_Sky.GetValue(ManTimeOfDay.inst) as TOD_Sky;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogStartDistance = 0f;
            RenderSettings.fogEndDistance = 40f;
            sky.m_UseTerraTechBiomeData = false;
            sky.Fog.Mode = TOD_FogType.None;
            sky.Ambient.Mode = TOD_AmbientType.None;

            Color abyssColor;
            if (QPatch.TheWaterIsLava)
            {
                abyssColor = underLavaColor * multiplier;
                abyssColor.a = 1f;
                RenderSettings.fogDensity = 420f;
                RenderSettings.fogColor = abyssColor;
                RenderSettings.ambientLight = abyssColor;
                RenderSettings.ambientGroundColor = abyssColor;
                RenderSettings.ambientIntensity = 1 + multiplier;
                var keys = underLavaSkyColors.colorKeys;
                keys[0].color = keys[1].color = abyssColor;
                underLavaSkyColors.colorKeys = keys;

                sky.Day.AmbientColor = sky.Night.AmbientColor = underLavaSkyColors;
                sky.Day.FogColor = sky.Night.FogColor = sky.Day.LightColor = sky.Night.LightColor = sky.Day.SkyColor = sky.Night.SkyColor = underLavaSkyColors;
            }
            else
            {
                Color TODColorDelta = ManTimeOfDay.inst.NightTime ? new Color(0.525f, 0.525f, 0.525f, 1) : new Color(1, 1, 1, 1);
                abyssColor = underWaterColor * TODColorDelta * multiplier;
                abyssColor.a = 1f;
                float depthWatch = Mathf.Clamp01((multiplierUnclamped / 2) - (ManTimeOfDay.inst.NightTime ? 0f : 0.5f));
                Color darker = (spoopy * depthWatch) + (abyssColor * (1f - depthWatch));
                RenderSettings.fogDensity = 160f;
                RenderSettings.fogColor = darker;
                RenderSettings.ambientLight = darker;
                RenderSettings.ambientGroundColor = darker;
                RenderSettings.ambientIntensity = 1 + multiplier;
                var keys = underWaterSkyColors.colorKeys;
                keys[0].color = keys[1].color = darker;
                underWaterSkyColors.colorKeys = keys;

                sky.Day.AmbientColor = sky.Night.AmbientColor = underWaterSkyColors;
                sky.Day.FogColor = sky.Night.FogColor = sky.Day.LightColor = sky.Night.LightColor = sky.Day.SkyColor = sky.Night.SkyColor = underWaterSkyColors;
            }
        }
        public static void UpdateDarkness()
        {// INSIDE the water
            if (!CameraSubmerged)
            {
                CameraSubmerged = true;
                UpdateAllTiles();
            }
            ManTimeOfDayExt.SetState("WM", 100, SetDarkness);
        }

        private void RemoteUpdate()
        {
            if (!IsActive)
            {
                gameObject.SetActive(false);
                PhysicsZone.transform.position = Vector3.down * 2000f;
                return;
            }
            blockSFXCount = 0;
            if (ManGameMode.inst.IsCurrentModeMultiplayer())
            {
                if (!ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                {
                    heightCalcSet = NetHeightSmooth;
                }
                else
                {
                    heightCalcSet = -1000f;
                }
            }
            else
            {
                heightCalcSet = heightCalc;
            }
            float heightCalcDiff = height + (rainFlood * floodHeightMultiplier);
            if (!heightCalcDiff.Approximately(heightCalcDiff, 0.1f))
                UpdateHeightCalc();
            WaterParticleHandler.MaintainBubbles();
            try
            {
                if (Camera.main.transform.position.y < HeightCalc)
                {
                    float height;
                    if (Singleton.playerTank && CameraManager.inst.IsCurrent<TankCamera>())
                        height = Singleton.playerTank.boundsCentreWorldNoCheck.y;
                    else
                        height = Camera.main.transform.position.y;

                    multiplierUnclamped = Mathf.Approximately(WaterGlobals.AbyssDepth, 0) ? 1 : 1 
                        - ((HeightCalc - height) / WaterGlobals.AbyssDepth);
                    multiplier = Mathf.Clamp01(multiplierUnclamped);
                    UpdateDarkness();
                }
                else if (CameraSubmerged)
                {   // OUTSIDE the water
                    CameraSubmerged = false;
                    UpdateAllTiles();
                    ManTimeOfDayExt.RemoveState("WM");
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
                            try
                            {
                                SafeInit.Save();
                            }
                            catch { }
                        }
                    }

                }
                if (flag && !ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                {
                    NetHeightSmooth = NetHeightSmooth * 0.9f + NetworkHandler.ServerWaterHeight * 0.1f;
                }
                PhysicsZone.transform.position = new Vector3(Singleton.camera.transform.position.x, HeightCalc, Singleton.camera.transform.position.z);
                if (_WeatherMod && !flag)
                {
                    float dTime = Time.deltaTime;
                    float newHeight = RainFlood;
                    //newHeight += WeatherMod.RainWeight * WaterGlobals.RainWeightMultiplier * dTime;
                    newHeight *= 1f - WaterGlobals.RainDrainMultiplier * dTime;
                    RainFlood += Mathf.Clamp(newHeight - RainFlood, -WaterGlobals.FloodChangeClamp * dTime,
                        WaterGlobals.FloodChangeClamp * dTime);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        internal static bool PistonHeart = false;
        private static Vector4 WorldPosOffset = Vector3.zero;
        private static int ID = Shader.PropertyToID("_WorldPos");
        internal static void WorldShift()
        {
            try
            {
                DebugWater.Log("World beginning shift");
                PistonHeart = !PistonHeart;
                WorldMove = true;
                CompensateForTreadmill();
            }
            catch (Exception e)
            {
                throw new Exception("WorldShift failed", e);
            }
        }
        public void OnMoveWorldOrigin(IntVector3 delta)
        {
            try
            {
                WaterParticleHandler.TreadmillAllParticles(delta);
                WorldPosOffset += delta;
                foreach (var item in matsMain)
                    item.SetVector(ID, WorldPosOffset);
            }
            catch (Exception e)
            {
                throw new Exception("OnMoveWorldOrigin failed", e);
            }
        }
        internal static void WorldShiftEnd(IntVector3 vec)
        {
            try
            {
                WorldMove = false;
                foreach (var item in ActiveWaterTiles)
                {
                    item.Value.UpdateTileTreadmill(vec);
                }
                //WaterBlock.MassApplyForces();
                DebugWater.Log("World ended shift");
            }
            catch (Exception e)
            {
                throw new Exception("WorldShiftEnd failed", e);
            }
        }

        public static List<WaterLook> waterLooks = new List<WaterLook>();

        public static void UpdateLook()
        {
            try
            {
                foreach (var item in ActiveWaterTiles)
                {
                    item.Value.UpdateLook();
                }
                SurfacePool.MaxGrow = waterLooks[SelectedLook].ParticleLimits;
                WaterParticleHandler.UpdateSplash();
                WaterParticleHandler.UpdateSurface();
                WaterParticleHandler.UpdateBubbles();
            }
            catch (Exception e)
            {
                DebugWater.Log(e);
            }
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        internal struct WaterLook
        {
            public string name;
            public Mesh mesh;
            public Material material;
            public Material materialLava;
            public float scale;
            public int lowResFallback;
            public int ParticleLimits;

            internal WaterLook(float scale, string name, Mesh mesh, Material material, Material materialLava,
                int lowResFallback, int particleLim)
            {
                this.scale = scale;
                this.name = name;
                this.mesh = mesh;
                this.material = material;
                this.materialLava = materialLava;
                this.lowResFallback = lowResFallback;
                ParticleLimits = particleLim;
            }

            public override string ToString()
            {
                return this.name;
            }
        }
    }
}
