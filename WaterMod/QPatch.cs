using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using System.IO;
using TerraTechETCUtil;


#if !STEAM
using ModHelper.Config;
#endif

namespace WaterMod
{
    public class QPatch
    {
        public const string ModNameAssetBundle = "Water Mod + Lava";
        internal static ModDataHandle ModHandle = new ModDataHandle(ModNameAssetBundle);
        public const string ModName = "Water Mod";
        public static bool ModExists(string name)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.StartsWith(name))
                {
                    return true;
                }
            }
            return false;
        }

        public static float WaterHeight
        {
            get => ManWater.HeightCalc;
        }

        public static KeyCode key;
        public static int key_int = 111;

        //internal static AssetBundle assetBundle;
        //internal static string asm_path = Assembly.GetExecutingAssembly().Location.Replace("WaterMod.dll", "");
        //internal static string assets_path = Path.Combine(asm_path, "ExtAssets", "waterassets");
        public static Material basic;
        public static Material fancy;

        // Experimentals
        public static bool OnlyPlayerWaterTraverseSFX = false;

        /// <summary> CLIENT SETTINGS </summary>
        public static bool SimulateProjectiles = true;
        /// <summary> CLIENT SETTINGS </summary>
        public static bool EnableLooseBlocksFloat = true;
        /// <summary> CLIENT SETTINGS </summary>
        public static bool OceanMan2 = false;
        /// <summary> CLIENT SETTINGS </summary>
        public static bool DestroyTreesInWater = false;
        /// <summary> CLIENT SETTINGS </summary>
        internal static bool WantsLava = false;

        public static void ApplyClientSettings()
        {
            WaterGlobals.SimulateProjectiles = SimulateProjectiles;
            WaterGlobals.EnableLooseBlocksFloat = EnableLooseBlocksFloat;
            WaterGlobals.OceanMan2 = OceanMan2;
            WaterGlobals.DestroyTreesInWater = DestroyTreesInWater;
            WaterGlobals.WantsLava = WantsLava;
        }

        /// <summary>
        /// Only edit if absolutely nesseary! Use TheWaterIsLava instead
        /// </summary>
        public static bool theWaterIsLava = false;
        public static bool TheWaterIsLava
        {
            get
            {
                try
                {
                    if (ManGameMode.inst.IsCurrentModeMultiplayer())
                    {
                        if (ManGameMode.inst.IsCurrent<ModeDeathmatch>())
                            return false;
                        else
                            return NetworkHandler.ServerLava;
                    }
                }
                catch { }
                return theWaterIsLava;
            }
            set
            {
                theWaterIsLava = value;
            }
        }

        public static Harmony harmony;
        public static void Main()
        {
            try
            {
                LegModExt.InsurePatches();
            }
            catch (Exception e)
            {
                DebugWater.Assert("Water Mod: Error on init hooks, LegModExt ENCOUNTERED INIT ERROR");
                throw e;
            }

            //ManTechBuilder.DebugIntersections = true;// SOO USEFUL
            harmony = new Harmony("aceba1.ttmm.revived.water");
            harmony.MassPatchAllWithin(typeof(MainPatches),"Water Mod + Lava", true);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            /*
            assetBundle = AssetBundle.LoadFromFile(assets_path);
            if (assetBundle == null)
                DebugWater.Log("Water Mod: assetBundle is NULL");
            */
            //HelperGUI.Init();

            ManWater.Initiate();
            RemoveScenery.Initiate();
            LavaMode.Initiate();

            try
            {
                if (ModStatusChecker.IsConfigHelperPresent && ModStatusChecker.IsNativeOptionsPresent)
                    InitSettingsCapsulated();
                else
                    DebugWater.Log("Water Mod: Could not init hooks for ConfigHelper or NativeOptions as both are not present");
            }
            catch { DebugWater.Assert("Water Mod: Error on init hooks, was confighelper or NativeOptions absent?"); }

            if (ManWater.UpdateHeightCalc())
                ManWater.ApplyHeightCalc(ManWater.heightCalc);
            ManWater.UpdateLook();
            SurfacePool.UpdateAllActiveParticles();

            if (OceanMan2)
                ManWorldGeneratorExt.InsurePreInit();
            //TerrainOperations.BeachingMode = OceanMan2;

            WikiPageBlock.AdditionalDisplayOnUI.Subscribe(BlockBouyWikiUI);
        }

        private static void InitSettingsCapsulated()
        {
            try
            {
                SafeInit.InitHooks();
            }
            catch { }
        }

        private static bool ShowWaterStats = false;
        private static void BlockBouyWikiUI(BlockTypes blockT)
        {
            TankBlock block = ManSpawn.inst.GetBlockPrefab(blockT);
            if (block != null)
            {
                float buoyforce = WaterBlock.GetMaxBuoyancyForce(block);

                GUILayout.BeginVertical(AltUI.BoxBlack);
                if (GUILayout.Button("Water", ShowWaterStats ? AltUI.LabelBlueTitle : AltUI.LabelWhiteTitleBlueHover,
                    GUILayout.ExpandWidth(true)))
                    ShowWaterStats = !ShowWaterStats;

                if (ShowWaterStats)
                {
                    GUILayout.BeginHorizontal(AltUI.TextfieldBlackSearch);
                    GUILayout.Label("General Buoyancy: ", AltUI.LabelWhite);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label((string)AutoDataExtractor.DisplayNewtons(buoyforce), AltUI.LabelGold);
                    GUILayout.EndHorizontal();

                    float floatationForce = buoyforce - (block.m_DefaultMass * Physics.gravity.magnitude);
                    GUILayout.BeginHorizontal(AltUI.TextfieldBlackSearch);
                    GUILayout.Label("Floatation Force: ", AltUI.LabelWhite);
                    GUILayout.FlexibleSpace();
                    if (floatationForce == 0)
                        GUILayout.Label((string)AutoDataExtractor.DisplayNewtons(floatationForce), AltUI.LabelWhite);
                    else if (floatationForce > 0)
                        GUILayout.Label(AltUI.FriendlyString((string)AutoDataExtractor.DisplayNewtons(floatationForce)), AltUI.LabelWhite);
                    else
                        GUILayout.Label(AltUI.EnemyString((string)AutoDataExtractor.DisplayNewtons(floatationForce)), AltUI.LabelWhite);
                    GUILayout.EndHorizontal();

                    float floatation = buoyforce / (block.m_DefaultMass * Physics.gravity.magnitude);
                    GUILayout.BeginHorizontal(AltUI.TextfieldBlackSearch);
                    GUILayout.Label("Float vs Mass Factor: ", AltUI.LabelWhite);
                    GUILayout.FlexibleSpace();
                    if (floatation == 1f)
                        GUILayout.Label(floatation.ToString("P"), AltUI.LabelWhite);
                    else if (floatation > 1f)
                        GUILayout.Label(AltUI.FriendlyString(floatation.ToString("P")), AltUI.LabelWhite);
                    else
                        GUILayout.Label(AltUI.EnemyString(floatation.ToString("P")), AltUI.LabelWhite);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }
    }

    public class SafeInit
    {
        public static ModHelper.ModConfig _thisMod;


        public static Nuterra.NativeOptions.OptionKey GUIMenu;
        public static Nuterra.NativeOptions.OptionToggle IsWaterActive;
        public static Nuterra.NativeOptions.OptionToggle UseParticleEffects;
        public static Nuterra.NativeOptions.OptionToggle TrailPriority;
        public static Nuterra.NativeOptions.OptionRange Height;
        public static Nuterra.NativeOptions.OptionRange Sound;

        public static Nuterra.NativeOptions.OptionToggle EnableCustomSettings;
        public static Nuterra.NativeOptions.OptionToggle looseBlocksFloat;
        public static Nuterra.NativeOptions.OptionToggle oceanMan;
        public static Nuterra.NativeOptions.OptionToggle noTreesInWater;
        public static Nuterra.NativeOptions.OptionToggle makeDeath;

        public static Nuterra.NativeOptions.OptionRange Density;
        public static Nuterra.NativeOptions.OptionRange FanJetMultiplier;
        public static Nuterra.NativeOptions.OptionRange ResourceBuoyancy;
        public static Nuterra.NativeOptions.OptionRange BulletDampener;
        public static Nuterra.NativeOptions.OptionRange MissileDampener;
        public static Nuterra.NativeOptions.OptionRange LaserFraction;
        public static Nuterra.NativeOptions.OptionRange SurfaceSkinning;
        public static Nuterra.NativeOptions.OptionRange SubmergedTankDampening;
        public static Nuterra.NativeOptions.OptionRange SubmergedTankDampeningY;
        public static Nuterra.NativeOptions.OptionRange SurfaceTankDampening;
        public static Nuterra.NativeOptions.OptionRange SurfaceTankDampeningY;
        public static Nuterra.NativeOptions.OptionRange WheelForces;

        public static Nuterra.NativeOptions.OptionRange RainWeightMultiplier;
        public static Nuterra.NativeOptions.OptionRange RainDrainMultiplier;
        public static Nuterra.NativeOptions.OptionRange FloodRateClamp;
        public static Nuterra.NativeOptions.OptionRange FloodHeightMultiplier;

        public static Nuterra.NativeOptions.OptionToggle Reset;
        private static ModHelper.ModConfig thisMod;
         
        internal static void TrySetWaterHeightSlider(float value)
        {
            try
            {
                Height.Value = value;
            }
            catch { }
        }

        public static void InitHooks()
        {
            ModHelper.ModConfig thisConfig = new ModHelper.ModConfig();

            thisConfig.BindConfig<WaterParticleHandler>(null, "UseParticleEffects");

            thisMod = thisConfig;

            thisMod.BindConfig<QPatch>(null, "key_int");
            QPatch.key = (KeyCode)QPatch.key_int;
            thisMod.BindConfig<ManWater>(null, "IsActive");
            thisMod.BindConfig<ManWater>(null, "WaterSound");
            thisMod.BindConfig<ManWater>(null, "AlwaysShowTrails");
            thisMod.BindConfig<ManWater>(null, "height");
            thisMod.BindConfig<ManWater>(null, "heightOcean");
            thisMod.BindConfig<ManWater>(null, "Density");
            thisMod.BindConfig<ManWater>(null, "UseStandards");
            thisMod.BindConfig<ManWater>(null, "FanJetMultiplier");
            thisMod.BindConfig<ManWater>(null, "ResourceBuoyancyMultiplier");
            thisMod.BindConfig<ManWater>(null, "BulletDampener");
            thisMod.BindConfig<ManWater>(null, "MissileDampener");
            thisMod.BindConfig<ManWater>(null, "LaserFraction");
            thisMod.BindConfig<ManWater>(null, "SurfaceSkinning");
            thisMod.BindConfig<ManWater>(null, "SubmergedTankDampening");
            thisMod.BindConfig<ManWater>(null, "SubmergedTankDampeningYAddition");
            thisMod.BindConfig<ManWater>(null, "SurfaceTankDampening");
            thisMod.BindConfig<ManWater>(null, "SurfaceTankDampeningYAddition");
            thisMod.BindConfig<ManWater>(null, "WheelWaterForceMultiplier");
            thisMod.BindConfig<ManWater>(null, "SelectedLook");
            thisMod.BindConfig<ManWater>(null, "AbyssDepth");

            thisMod.BindConfig<QPatch>(null, "EnableLooseBlocksFloat");
            thisMod.BindConfig<QPatch>(null, "OceanMan2");
            thisMod.BindConfig<QPatch>(null, "DestroyTreesInWater");
            thisMod.BindConfig<QPatch>(null, "theWaterIsLava");

            ManWater._WeatherMod = QPatch.ModExists("TTQMM WeatherMod");
            if (ManWater._WeatherMod)
            {
                DebugWater.Log("Found WeatherMod!");
                thisMod.BindConfig<ManWater>(null, "RainWeightMultiplier");
                thisMod.BindConfig<ManWater>(null, "RainDrainMultiplier");
                thisMod.BindConfig<ManWater>(null, "FloodChangeClamp");
                thisMod.BindConfig<ManWater>(null, "floodHeightMultiplier");
            }

            _thisMod = thisMod;

            string menuNameGeneral = QPatch.ModName + " - General";
            var waterLook = new Nuterra.NativeOptions.OptionList<ManWater.WaterLook>("Water quality", menuNameGeneral, ManWater.waterLooks, ManWater.SelectedLook);
            waterLook.onValueSaved.AddListener(() =>
            {
                ManWater.SelectedLook = waterLook.SavedValue;
                ManWater.UpdateLook();
                SurfacePool.UpdateAllActiveParticles();
            });

            var waterAbyssDepth = SuperNativeOptions.OptionRangeAutoDisplay("Abyss depth", menuNameGeneral, ManWater.AbyssDepth
                , uiReturnFuncString: (float value) =>
                {
                    return "-" + Mathf.RoundToInt(value) + "m";
                });
            waterAbyssDepth.onValueSaved.AddListener(() => {
                ManWater.AbyssDepth = waterAbyssDepth.SavedValue;
            });
            GUIMenu = new Nuterra.NativeOptions.OptionKey("GUI Menu button", menuNameGeneral, QPatch.key);
            GUIMenu.onValueSaved.AddListener(() => { QPatch.key_int = (int)(QPatch.key = GUIMenu.SavedValue); ManWater._inst.Invoke("Save", 0.5f); });

            UseParticleEffects = new Nuterra.NativeOptions.OptionToggle("Particle effects Active", menuNameGeneral, WaterParticleHandler.UseParticleEffects);
            UseParticleEffects.onValueSaved.AddListener(() => {
                WaterParticleHandler.UseParticleEffects = UseParticleEffects.SavedValue;
                if (!WaterParticleHandler.UseParticleEffects)
                    WaterParticleHandler.ClearAllParticles();
            });
            TrailPriority = new Nuterra.NativeOptions.OptionToggle("Always Show Trails", menuNameGeneral, ManWater.AlwaysShowTrails);
            TrailPriority.onValueSaved.AddListener(() => {
                ManWater.AlwaysShowTrails = TrailPriority.SavedValue;
                ManWater.SetShaders(ManWater.AlwaysShowTrails);
            });
            Sound = SuperNativeOptions.OptionRangeAutoDisplay("Water Traverse Loudness", menuNameGeneral, ManWater.WaterSound,
                0f, 1f, 0.05f, (float value) =>
                {
                    return Mathf.RoundToInt(value * 100) + "%";
                });
            Sound.onValueSaved.AddListener(() => { ManWater.WaterSound = Sound.SavedValue; });



            // --------------------------------------------------------------------------
            string WaterProperties = QPatch.ModName + " - Host";
            Nuterra.NativeOptions.OptionToggle togTest = new Nuterra.NativeOptions.OptionToggle("Water Active", WaterProperties, ManWater.IsActive);
            togTest.onValueSaved.AddListener(() => {
                ManWater.IsActive = togTest.SavedValue;
                ManWater.SetState();
            });
            IsWaterActive = togTest;
            oceanMan = SuperNativeOptions.OptionToggleAutoDisplay("Ocean Mode", WaterProperties, QPatch.OceanMan2,
                (bool state) => { return "LOCKS WATER HEIGHT TO " + ManWater.heightOcean + "]"; });
            oceanMan.onValueSaved.AddListener(() =>
            {
                try
                {
                    bool RELOAD = WaterGlobals.OceanMan2 != oceanMan.SavedValue;
                    WaterGlobals.OceanMan2 = oceanMan.SavedValue;
                    if (RELOAD && ManNetwork.IsHost)
                    {
                        //TerrainOperations.BeachingMode = QPatch.OceanMan2;
                        if (WaterGlobals.OceanMan2)
                        {
                            ManWorldGeneratorExt.InsurePreInit();
                        }

                        ManWorldTileExt.RushTileLoading();
                        foreach (var item in ManWorld.inst.TileManager.IterateTiles())
                        {
                            ManWorldTileExt.HostOnly_ReloadTile(item.Coord, false);
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugWater.Log(e);
                }

            });
            Height = SuperNativeOptions.OptionRangeAutoDisplay("Height level", WaterProperties, ManWater.Height, 
                -75f, 100f, 1f, (float value) =>
                {
                    return Mathf.RoundToInt(value) + "m";
                });
            Height.onValueSaved.AddListener(() => { ManWater.Height = Height.SavedValue; });

            looseBlocksFloat = new Nuterra.NativeOptions.OptionToggle("Loose Blocks and Chunks float", WaterProperties, QPatch.EnableLooseBlocksFloat);
            looseBlocksFloat.onValueSaved.AddListener(() => { QPatch.EnableLooseBlocksFloat = looseBlocksFloat.SavedValue; });
            makeDeath = new Nuterra.NativeOptions.OptionToggle("but it's lava", WaterProperties, QPatch.theWaterIsLava);
            makeDeath.onValueSaved.AddListener(() => { 
                QPatch.WantsLava = makeDeath.SavedValue; 
                LavaMode.ThrowLavaDeathWarning(); 
            });

            EnableCustomSettings = new Nuterra.NativeOptions.OptionToggle("Use Custom Settings", WaterProperties, !ManWater.UseStandards);
            EnableCustomSettings.onValueSaved.AddListener(() => {
                if (ManWater.UseStandards == EnableCustomSettings.SavedValue)
                {
                    ManWater.UseStandards = !EnableCustomSettings.SavedValue;
                    if (ManWater.UseStandards)
                        thisMod.WriteConfigJsonFile();
                    else
                        thisMod.ReadConfigJsonFile();
                }
            });
            noTreesInWater = new Nuterra.NativeOptions.OptionToggle("Destroy <b>[!FOREVER!]</b> Submerged Trees (Single-player only)", WaterProperties, QPatch.DestroyTreesInWater);
            noTreesInWater.onValueSaved.AddListener(() => { QPatch.DestroyTreesInWater = noTreesInWater.SavedValue; });
            Density = new Nuterra.NativeOptions.OptionRange("Density", WaterProperties, ManWater.Density, -16, 16, 0.25f);
            Density.onValueSaved.AddListener(() => {
                ManWater.Density = Density.SavedValue;
            });
            FanJetMultiplier = new Nuterra.NativeOptions.OptionRange("Fan jet Multiplier", WaterProperties, ManWater.FanJetMultiplier, 0f, 4f, .05f);
            FanJetMultiplier.onValueSaved.AddListener(() => {
                ManWater.FanJetMultiplier = FanJetMultiplier.SavedValue;
            });
            ResourceBuoyancy = new Nuterra.NativeOptions.OptionRange("Resource Buoyancy", WaterProperties, ManWater.ResourceBuoyancyMultiplier, 0f, 4f, .05f);
            ResourceBuoyancy.onValueSaved.AddListener(() => {
                ManWater.ResourceBuoyancyMultiplier = ResourceBuoyancy.SavedValue;
            });
            BulletDampener = new Nuterra.NativeOptions.OptionRange("Bullet Dampening", WaterProperties, ManWater.BulletDampener, 0f, 1E-4f, 1E-8f);
            BulletDampener.onValueSaved.AddListener(() => {
                ManWater.BulletDampener = BulletDampener.SavedValue;
            });
            MissileDampener = new Nuterra.NativeOptions.OptionRange("Missile Dampening", WaterProperties, ManWater.MissileDampener, 0f, 0.1f, 0.003f);
            MissileDampener.onValueSaved.AddListener(() => {
                ManWater.MissileDampener = MissileDampener.SavedValue;
            });
            LaserFraction = new Nuterra.NativeOptions.OptionRange("Laser Slowdown", WaterProperties, ManWater.LaserFraction, 0f, 0.5f, 0.025f);
            LaserFraction.onValueSaved.AddListener(() => {
                ManWater.LaserFraction = LaserFraction.SavedValue;
            });
            SurfaceSkinning = new Nuterra.NativeOptions.OptionRange("Surface Skinning", WaterProperties, ManWater.SurfaceSkinning, -0.5f, 0.5f, 0.05f);
            SurfaceSkinning.onValueSaved.AddListener(() => {
                ManWater.SurfaceSkinning = SurfaceSkinning.SavedValue;
            });
            SubmergedTankDampening = new Nuterra.NativeOptions.OptionRange("Submerged Tank Dampening", WaterProperties, ManWater.SubmergedTankDampening, 0f, 2f, 0.05f);
            SubmergedTankDampening.onValueSaved.AddListener(() => {
                ManWater.SubmergedTankDampening = SubmergedTankDampening.SavedValue;
            });
            SubmergedTankDampeningY = new Nuterra.NativeOptions.OptionRange("Submerged Tank Dampening Y addition", WaterProperties, ManWater.SubmergedTankDampeningYAddition, -1f, 1f, 0.05f);
            SubmergedTankDampeningY.onValueSaved.AddListener(() => {
                ManWater.SubmergedTankDampeningYAddition = SubmergedTankDampeningY.SavedValue;
            });
            SurfaceTankDampening = new Nuterra.NativeOptions.OptionRange("Surface Tank Dampening", WaterProperties, ManWater.SurfaceTankDampening, 0f, 2f, 0.05f);
            SurfaceTankDampening.onValueSaved.AddListener(() => {
                ManWater.SurfaceTankDampening = SurfaceTankDampening.SavedValue;
            });
            SurfaceTankDampeningY = new Nuterra.NativeOptions.OptionRange("Surface Tank Dampening Y addition", WaterProperties, ManWater.SurfaceTankDampeningYAddition, -1f, 1f, 0.05f);
            SurfaceTankDampeningY.onValueSaved.AddListener(() => {
                ManWater.SurfaceTankDampeningYAddition = SurfaceTankDampeningY.SavedValue;
            });
            WheelForces = new Nuterra.NativeOptions.OptionRange("Surface Wheel Force Multiplier", WaterProperties, ManWater.WheelWaterForceMultiplier, 0, 4f, 0.05f);
            WheelForces.onValueSaved.AddListener(() => {
                ManWater.WheelWaterForceMultiplier = WheelForces.SavedValue;
            });


            if (ManWater._WeatherMod)
            {
                var WeatherProperties = QPatch.ModName + " - Weather mod";
                RainWeightMultiplier = new Nuterra.NativeOptions.OptionRange("Rain Weight Multiplier", WeatherProperties, ManWater.RainWeightMultiplier, 0, 0.25f, 0.01f);
                RainWeightMultiplier.onValueSaved.AddListener(() => { ManWater.RainWeightMultiplier = RainWeightMultiplier.SavedValue; });
                RainDrainMultiplier = new Nuterra.NativeOptions.OptionRange("Rain Drain Multiplier", WeatherProperties, ManWater.RainDrainMultiplier, 0, 0.25f, 0.01f);
                RainDrainMultiplier.onValueSaved.AddListener(() => { ManWater.RainDrainMultiplier = RainDrainMultiplier.SavedValue; });
                FloodRateClamp = new Nuterra.NativeOptions.OptionRange("Flood rate Clamp", WeatherProperties, ManWater.FloodChangeClamp, 0, 0.08f, 0.001f);
                FloodRateClamp.onValueSaved.AddListener(() => { ManWater.FloodChangeClamp = FloodRateClamp.SavedValue; });
                FloodHeightMultiplier = new Nuterra.NativeOptions.OptionRange("Flood Height Multiplier", WeatherProperties, ManWater.FloodHeightMultiplier, 0, 50f, 1f);
                FloodHeightMultiplier.onValueSaved.AddListener(() => { ManWater.FloodHeightMultiplier = FloodHeightMultiplier.SavedValue; });
            }
            Nuterra.NativeOptions.NativeOptionsMod.onOptionsSaved.AddListener(() =>
            {
                try
                {
                    ManWater.ApplyClientSideSettingsAndSendForHost();
                    thisMod.WriteConfigJsonFile();
                    ManWater.UpdateNetworkedWaterIfNeeded();
                }
                catch (Exception e)
                {
                    DebugWater.Log(e);
                }
            });
            ManWater.ApplyClientSideSettingsAndSendForHost();
        }

        internal static void Save()
        {
            _thisMod.WriteConfigJsonFile();
        }
    }
}
