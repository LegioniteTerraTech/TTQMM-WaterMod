using System;
using System.Reflection;
using HarmonyLib;
using TerraTechETCUtil;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using WaterMod.PatchBatch;


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
        /// <summary> SERVER & CLIENT SETTINGS </summary>
        internal static bool WantsLava = false;


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


        public static Harmony harmony = new Harmony("aceba1.ttmm.revived.water");
        public static bool IsOptionsAvailable = false;
        public static bool IsBiomeInjectorPresent = false;

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
            IsOptionsAvailable = ModStatusChecker.IsModOptionsAvailable();
            IsBiomeInjectorPresent = ModStatusChecker.LookForMod("NuterraWorld");

            try
            {
                SafeSaves.ManSafeSaves.RegisterSaveSystem(Assembly.GetExecutingAssembly(), OnSave, OnLoad);
            }
            catch (Exception e)
            {
                DebugWater.Assert("Water Mod: Error on init hooks to SafeSaves");
                throw e;
            }

            //ManTechBuilder.DebugIntersections = true;// SOO USEFUL
            harmony.MassPatchAllWithin(typeof(MainPatches), ModNameAssetBundle, true);
            harmony.MassPatchAllWithin(typeof(GlobalPatches), ModNameAssetBundle, true);
            harmony.MassPatchAllWithin(typeof(VisualPatches), ModNameAssetBundle, true);
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
                if (IsOptionsAvailable)
                    InitSettingsCapsulated();
                else
                    DebugWater.Log("Water Mod: Could not init hooks for ConfigHelper or NativeOptions as both are not present");
            }
            catch { DebugWater.Assert("Water Mod: Error on init hooks, was ConfigHelper or NativeOptions absent?"); }

            if (ManWater.UpdateHeightCalc())
                ManWater.ApplyHeightCalc(ManWater.heightCalc);
            ManWater.UpdateLook();
            SurfacePool.UpdateAllActiveParticles();

            if (OceanMan2)
                ManWorldGeneratorExt.InsurePreInit();
            //TerrainOperations.BeachingMode = OceanMan2;

            WikiPageBlock.AdditionalDisplayOnUI.Subscribe(BlockBouyWikiUI);
        }
        public static void OnSave(bool beforeSaving)
        {
        }
        public static void OnLoad(bool beforeLoading)
        {
            if (beforeLoading)
                ManWaterSaveData.inst.HeightSave = ManWater.height;
        }

        public static void Deinit()
        {
            harmony.MassUnPatchAllWithin(typeof(MainPatches), ModNameAssetBundle, true);
            harmony.MassUnPatchAllWithin(typeof(GlobalPatches), ModNameAssetBundle, true);
            harmony.MassUnPatchAllWithin(typeof(VisualPatches), ModNameAssetBundle, true);
            harmony.UnpatchAll(harmony.Id);
            try
            {
                SafeSaves.ManSafeSaves.UnregisterSaveSystem(Assembly.GetExecutingAssembly(), OnSave, OnLoad);
            }
            catch (Exception e)
            {
                DebugWater.Assert("Water Mod: Error on init hooks to SafeSaves");
                throw e;
            }
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
        public static Nuterra.NativeOptions.OptionToggle UseDynamicDepthFog;
        public static Nuterra.NativeOptions.OptionToggle UseParticleEffects;
        public static Nuterra.NativeOptions.OptionToggle TrailPriority;
        public static Nuterra.NativeOptions.OptionRange SoundSplash;
        public static Nuterra.NativeOptions.OptionRange SoundTraverse;
        public static Nuterra.NativeOptions.OptionRange Depth;


        public static Nuterra.NativeOptions.OptionToggle IsWaterActive;
        public static Nuterra.NativeOptions.OptionRange Height;
        public static Nuterra.NativeOptions.OptionRange HeightSave;
        public static Nuterra.NativeOptions.OptionToggle oceanMan;
        public static Nuterra.NativeOptions.OptionToggle looseBlocksFloat;
        public static Nuterra.NativeOptions.OptionToggle noTreesInWater;
        public static Nuterra.NativeOptions.OptionToggle makeDeath;

        public static Nuterra.NativeOptions.OptionToggle EnableCustomSettings;
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
            _thisMod = new ModHelper.ModConfig();
            var thisMod = _thisMod;

            thisMod.BindConfig<WaterParticleHandler>(null, "UseParticleEffects");


            thisMod.BindConfig<QPatch>(null, "key_int");
            QPatch.key = (KeyCode)QPatch.key_int;
            thisMod.BindConfig<ManWater>(null, "IsActive");
            thisMod.BindConfig<ManWater>(null, "WaterSound");
            thisMod.BindConfig<ManWater>(null, "WaterSoundSplash");
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
            thisMod.BindConfig<ManWater>(null, "UseDynamicFog");

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

            string menuGeneral = QPatch.ModName + " - General";
            GUIMenu = new Nuterra.NativeOptions.OptionKey("GUI Menu button", menuGeneral, QPatch.key);
            GUIMenu.onValueSaved.AddListener(() => { QPatch.key_int = (int)(QPatch.key = GUIMenu.SavedValue); ManWater._inst.Invoke("Save", 0.5f); });

            var waterLook = new Nuterra.NativeOptions.OptionList<ManWater.WaterLook>("Water quality", menuGeneral, ManWater.waterLooks, ManWater.SelectedLook);
            waterLook.onValueSaved.AddListener(() =>
            {
                ManWater.SelectedLook = waterLook.SavedValue;
                ManWater.UpdateLook();
                SurfacePool.UpdateAllActiveParticles();
            });

            Depth = SuperNativeOptions.OptionRangeAutoDisplay("Abyss depth", menuGeneral, ManWater.AbyssDepth
                , uiReturnFuncString: (float value) =>
                {
                    return "-" + Mathf.RoundToInt(value) + "m";
                });
            Depth.onValueSaved.AddListener(() => {
                ManWater.AbyssDepth = Depth.SavedValue;
            });
            UseDynamicDepthFog = new Nuterra.NativeOptions.OptionToggle("Use dynamic depth fog effect", menuGeneral, ManWater.UseDynamicFog);
            UseDynamicDepthFog.onValueSaved.AddListener(() => {
                ManWater.UseDynamicFog = UseDynamicDepthFog.SavedValue;
                CameraManager.inst.SetDrawDist01(CameraManager.inst.DrawDist01);
            });

            UseParticleEffects = new Nuterra.NativeOptions.OptionToggle("Particle effects Active", menuGeneral, WaterParticleHandler.UseParticleEffects);
            UseParticleEffects.onValueSaved.AddListener(() => {
                WaterParticleHandler.UseParticleEffects = UseParticleEffects.SavedValue;
                if (!WaterParticleHandler.UseParticleEffects)
                    WaterParticleHandler.ClearAllParticles();
            });

            TrailPriority = new Nuterra.NativeOptions.OptionToggle("Always Show Trails", menuGeneral, ManWater.AlwaysShowTrails);
            TrailPriority.onValueSaved.AddListener(() => {
                ManWater.AlwaysShowTrails = TrailPriority.SavedValue;
                ManWater.SetShaders(ManWater.AlwaysShowTrails);
            });
            SoundSplash = SuperNativeOptions.OptionRangeAutoDisplay("Water Splash Loudness [Impacted by SFX Settings too]", menuGeneral, ManWater.WaterSoundSplash,
                0f, 1f, 0.05f, (float value) =>
                {
                    return Mathf.RoundToInt(value * 100) + "%";
                });
            SoundSplash.onValueSaved.AddListener(() => { ManWater.WaterSoundSplash = SoundSplash.SavedValue; });
            SoundTraverse = SuperNativeOptions.OptionRangeAutoDisplay("Water Traverse Loudness [Impacted by SFX Settings too]", menuGeneral, ManWater.WaterSound,
                0f, 1f, 0.05f, (float value) =>
                {
                    return Mathf.RoundToInt(value * 100) + "%";
                });
            SoundTraverse.onValueSaved.AddListener(() => { ManWater.WaterSound = SoundTraverse.SavedValue; });



            // --------------------------------------------------------------------------
            string WaterProperties = QPatch.ModName + " - Host";
            Nuterra.NativeOptions.OptionToggle togTest = new Nuterra.NativeOptions.OptionToggle("Water Active", WaterProperties, ManWater.IsActive);
            togTest.onValueSaved.AddListener(() => {
                ManWater.IsActive = togTest.SavedValue;
            });
            IsWaterActive = togTest;
            oceanMan = SuperNativeOptions.OptionToggleAutoDisplay("Ocean Mode", WaterProperties, QPatch.OceanMan2,
                (bool state) => { return "LOCKS WATER HEIGHT TO " + ManWater.heightOcean + "m]"; });
            oceanMan.onValueSaved.AddListener(() =>
            {
                QPatch.OceanMan2 = oceanMan.SavedValue;
            });
            Height = SuperNativeOptions.OptionRangeAutoDisplay("Default Sea Height level", WaterProperties, ManWater.height, 
                -75f, 100f, 1f, (float value) =>
                {
                    return Mathf.RoundToInt(value) + "m";
                });
            Height.onValueSaved.AddListener(() => { ManWater.height = Height.SavedValue; });
            HeightSave = SuperNativeOptions.OptionRangeAutoDisplay("Saved Sea Height level", WaterProperties, ManWaterSaveData.inst.HeightSave,
                -75f, 100f, 1f, (float value) =>
                {
                    return Mathf.RoundToInt(value) + "m";
                });
            HeightSave.onValueSaved.AddListener(() => { ManWaterSaveData.inst.HeightSave = HeightSave.SavedValue; });

            looseBlocksFloat = new Nuterra.NativeOptions.OptionToggle("Loose Blocks and Chunks float", WaterProperties, QPatch.EnableLooseBlocksFloat);
            looseBlocksFloat.onValueSaved.AddListener(() => { QPatch.EnableLooseBlocksFloat = looseBlocksFloat.SavedValue; });
            makeDeath = new Nuterra.NativeOptions.OptionToggle("but it's lava", WaterProperties, QPatch.theWaterIsLava);
            makeDeath.onValueSaved.AddListener(() => { 
                QPatch.WantsLava = makeDeath.SavedValue;
                LavaMode.ThrowLavaDeathWarningIfNeeded();
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
            Density = SuperNativeOptions.OptionRangeAutoDisplay("Density", WaterProperties, 
                ManWater.Density, -16, 16, 0.25f, (val) => {
                    return (val / ManWaterDefaults.Density).ToString("F");
                });
            Density.onValueSaved.AddListener(() => {
                ManWater.Density = Density.SavedValue;
            });
            FanJetMultiplier = SuperNativeOptions.OptionRangeAutoDisplay("Fan jet Multiplier", WaterProperties, 
                ManWater.FanJetMultiplier, 0f, 4f, .05f, (val) => {
                    return (val / ManWaterDefaults.FanJetMultiplier).ToString("F");
                });
            FanJetMultiplier.onValueSaved.AddListener(() => {
                ManWater.FanJetMultiplier = FanJetMultiplier.SavedValue;
            });
            ResourceBuoyancy = SuperNativeOptions.OptionRangeAutoDisplay("Resource Buoyancy", WaterProperties, 
                ManWater.ResourceBuoyancyMultiplier, 0f, 4f, .05f, (val) => {
                    return (val / ManWaterDefaults.ResourceBuoyancyMultiplier).ToString("F");
                });
            ResourceBuoyancy.onValueSaved.AddListener(() => {
                ManWater.ResourceBuoyancyMultiplier = ResourceBuoyancy.SavedValue;
            });
            BulletDampener = SuperNativeOptions.OptionRangeAutoDisplay("Bullet Dampening", WaterProperties, 
                ManWater.BulletDampener, 0f, 1E-4f, 1E-8f, (val) => {
                    return (val / ManWaterDefaults.BulletDampener).ToString("F");
                });
            BulletDampener.onValueSaved.AddListener(() => {
                ManWater.BulletDampener = BulletDampener.SavedValue;
            });
            MissileDampener = SuperNativeOptions.OptionRangeAutoDisplay("Missile Dampening", WaterProperties, 
                ManWater.MissileDampener, 0f, 0.1f, 0.003f, (val) => {
                    return (val / ManWaterDefaults.MissileDampener).ToString("F");
                });
            MissileDampener.onValueSaved.AddListener(() => {
                ManWater.MissileDampener = MissileDampener.SavedValue;
            });
            LaserFraction = SuperNativeOptions.OptionRangeAutoDisplay("Laser Slowdown", WaterProperties, 
                ManWater.LaserFraction, 0f, 0.5f, 0.025f, (val) => {
                    return (val / ManWaterDefaults.LaserFraction).ToString("F");
                });
            LaserFraction.onValueSaved.AddListener(() => {
                ManWater.LaserFraction = LaserFraction.SavedValue;
            });
            SurfaceSkinning = SuperNativeOptions.OptionRangeAutoDisplay("Surface Skinning", WaterProperties, 
                ManWater.SurfaceSkinning, -0.5f, 0.5f, 0.05f, (val) => {
                    return (val / ManWaterDefaults.SurfaceSkinning).ToString("F");
                });
            SurfaceSkinning.onValueSaved.AddListener(() => {
                ManWater.SurfaceSkinning = SurfaceSkinning.SavedValue;
            });
            SubmergedTankDampening = SuperNativeOptions.OptionRangeAutoDisplay("Submerged Tank Dampening", WaterProperties, 
                ManWater.SubmergedTankDampening, 0f, 2f, 0.05f, (val) => {
                    return val.ToString("F");
                });
            SubmergedTankDampening.onValueSaved.AddListener(() => {
                ManWater.SubmergedTankDampening = SubmergedTankDampening.SavedValue;
            });
            SubmergedTankDampeningY = SuperNativeOptions.OptionRangeAutoDisplay("Submerged Tank Dampening Y addition", WaterProperties, 
                ManWater.SubmergedTankDampeningYAddition, -1f, 1f, 0.05f, (val) => {
                    return val.ToString("F");
                });
            SubmergedTankDampeningY.onValueSaved.AddListener(() => {
                ManWater.SubmergedTankDampeningYAddition = SubmergedTankDampeningY.SavedValue;
            });
            SurfaceTankDampening = SuperNativeOptions.OptionRangeAutoDisplay("Surface Tank Dampening", WaterProperties, 
                ManWater.SurfaceTankDampening, 0f, 2f, 0.05f, (val) => {
                    return val.ToString("F");
                });
            SurfaceTankDampening.onValueSaved.AddListener(() => {
                ManWater.SurfaceTankDampening = SurfaceTankDampening.SavedValue;
            });
            SurfaceTankDampeningY = SuperNativeOptions.OptionRangeAutoDisplay("Surface Tank Dampening Y addition", WaterProperties, 
                ManWater.SurfaceTankDampeningYAddition, -1f, 1f, 0.05f, (val) => {
                    return val.ToString("F");
                });
            SurfaceTankDampeningY.onValueSaved.AddListener(() => {
                ManWater.SurfaceTankDampeningYAddition = SurfaceTankDampeningY.SavedValue;
            });
            WheelForces = SuperNativeOptions.OptionRangeAutoDisplay("Surface Wheel Force Multiplier", WaterProperties, 
                ManWater.WheelWaterForceMultiplier, 0, 4f, 0.05f, (val) => {
                    return (val / ManWaterDefaults.WheelWaterForceMultiplier).ToString("F");
                });
            WheelForces.onValueSaved.AddListener(() => {
                ManWater.WheelWaterForceMultiplier = WheelForces.SavedValue;
            });


            if (ManWater._WeatherMod)
            {
                var WeatherProperties = QPatch.ModName + " - Weather mod";
                RainWeightMultiplier = SuperNativeOptions.OptionRangeAutoDisplay("Rain Weight Multiplier", WeatherProperties, 
                    ManWater.RainWeightMultiplier, 0, 0.25f, 0.01f, (val) => {
                        return (val / ManWaterDefaults.RainWeightMultiplier).ToString("F");
                    });
                RainWeightMultiplier.onValueSaved.AddListener(() => { ManWater.RainWeightMultiplier = RainWeightMultiplier.SavedValue; });
                RainDrainMultiplier = SuperNativeOptions.OptionRangeAutoDisplay("Rain Drain Multiplier", WeatherProperties, 
                    ManWater.RainDrainMultiplier, 0, 0.25f, 0.01f, (val) => {
                        return (val / ManWaterDefaults.RainDrainMultiplier).ToString("F");
                    });
                RainDrainMultiplier.onValueSaved.AddListener(() => { ManWater.RainDrainMultiplier = RainDrainMultiplier.SavedValue; });
                FloodRateClamp = SuperNativeOptions.OptionRangeAutoDisplay("Flood rate Clamp", WeatherProperties, 
                    ManWater.FloodChangeClamp, 0, 0.08f, 0.001f, (val) => {
                        return (val / ManWaterDefaults.FloodChangeClamp).ToString("F");
                    });
                FloodRateClamp.onValueSaved.AddListener(() => { ManWater.FloodChangeClamp = FloodRateClamp.SavedValue; });
                FloodHeightMultiplier = SuperNativeOptions.OptionRangeAutoDisplay("Flood Height Multiplier", WeatherProperties, 
                    ManWater.FloodHeightMultiplier, 0, 50f, 1f, (val) => {
                        return val.ToString("0") + "x";
                    });
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
