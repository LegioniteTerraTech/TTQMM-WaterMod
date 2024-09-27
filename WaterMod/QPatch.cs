using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Nuterra.NativeOptions;
using System.IO;
using TerraTechETCUtil;

#if !STEAM
using ModHelper.Config;
#else
using ModHelper;
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
        public static bool SimulateProjectiles = true;
        public static bool HeightBasedFloat = true;
        public static bool OnlyPlayerWaterTraverseSFX = false;

        public static bool EnableLooseBlocksFloat = true;
        public static bool OceanMan = false;
        public static bool DestroyTreesInWater = false;
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

        public static void Main()
        {
            //ManTechBuilder.DebugIntersections = true;// SOO USEFUL
            var harmony = new Harmony("aceba1.ttmm.revived.water");
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
                SafeInit.InitHooks();
            }
            catch { DebugWater.Log("Water Mod: Error on init hooks, was confighelper or NativeOptions absent?"); }
            TerrainOperations.BeachingMode = OceanMan;

            ManWater.UpdateHeightCalc();
            ManWater.UpdateLook();
            SurfacePool.UpdateAllActiveParticles();

            if (OceanMan)
                WorldTerraformer.InsurePreInit();
        }
    }

    public class SafeInit
    {
        public static ModConfig _thisMod;


        public static OptionKey GUIMenu;
        public static OptionToggle IsWaterActive;
        public static OptionToggle UseParticleEffects;
        public static OptionToggle TrailPriority;
        public static OptionRange Height;
        public static OptionRange Sound;

        public static OptionToggle EnableCustomSettings;
        public static OptionToggle looseBlocksFloat;
        public static OptionToggle oceanMan;
        public static OptionToggle noTreesInWater;
        public static OptionToggle makeDeath;

        public static OptionRange Density;
        public static OptionRange FanJetMultiplier;
        public static OptionRange ResourceBuoyancy;
        public static OptionRange BulletDampener;
        public static OptionRange MissileDampener;
        public static OptionRange LaserFraction;
        public static OptionRange SurfaceSkinning;
        public static OptionRange SubmergedTankDampening;
        public static OptionRange SubmergedTankDampeningY;
        public static OptionRange SurfaceTankDampening;
        public static OptionRange SurfaceTankDampeningY;
        public static OptionRange WheelForces;

        public static OptionRange RainWeightMultiplier;
        public static OptionRange RainDrainMultiplier;
        public static OptionRange FloodRateClamp;
        public static OptionRange FloodHeightMultiplier;

        public static OptionToggle Reset;
        private static ModConfig thisMod;

        public static void InitHooks()
        {
            thisMod = new ModConfig();

            thisMod.BindConfig<WaterParticleHandler>(null, "UseParticleEffects");


            thisMod.BindConfig<QPatch>(null, "key_int");
            QPatch.key = (KeyCode)QPatch.key_int;
            thisMod.BindConfig<ManWater>(null, "IsActive");
            thisMod.BindConfig<ManWater>(null, "WaterSound");
            thisMod.BindConfig<ManWater>(null, "AlwaysShowTrails");
            thisMod.BindConfig<ManWater>(null, "height");
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
            thisMod.BindConfig<QPatch>(null, "OceanMan");
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

            IsWaterActive = new OptionToggle("Water Active", QPatch.ModName, ManWater.IsActive);
            IsWaterActive.onValueSaved.AddListener(() => {
                ManWater.IsActive = IsWaterActive.SavedValue;
                ManWater.SetState();
            });
            oceanMan = new OptionToggle("Ocean Mode [WIP]", QPatch.ModName, QPatch.OceanMan);
            oceanMan.onValueSaved.AddListener(() =>
            {
                try
                {
                    bool RELOAD = QPatch.OceanMan != oceanMan.SavedValue;
                    QPatch.OceanMan = oceanMan.SavedValue;
                    if (RELOAD)
                    {
                        if (QPatch.OceanMan)
                            WorldTerraformer.InsurePreInit();
                        foreach (var item in ManWorld.inst.TileManager.IterateTiles())
                        {
                            ManWorldTileExt.ReloadTile(item.Coord);
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugWater.Log(e);
                }

            });

            EnableCustomSettings = new OptionToggle("Use Custom Settings", QPatch.ModName, !ManWater.UseStandards);
            EnableCustomSettings.onValueSaved.AddListener(() => {
                if (ManWater.UseStandards == EnableCustomSettings.SavedValue)
                {
                    ManWater.UseStandards = !EnableCustomSettings.SavedValue;
                    if (ManWater.UseStandards)
                    {
                        thisMod.WriteConfigJsonFile();
                        ManWater.SetToStandard();
                    }
                    else
                    {
                        thisMod.ReadConfigJsonFile();
                    }
                }
            });
            GUIMenu = new OptionKey("GUI Menu button", QPatch.ModName, QPatch.key);
            GUIMenu.onValueSaved.AddListener(() => { QPatch.key_int = (int)(QPatch.key = GUIMenu.SavedValue); ManWater._inst.Invoke("Save", 0.5f); });

            UseParticleEffects = new OptionToggle("Particle effects Active", QPatch.ModName, WaterParticleHandler.UseParticleEffects);
            UseParticleEffects.onValueSaved.AddListener(() => {
                WaterParticleHandler.UseParticleEffects = UseParticleEffects.SavedValue;
                if (!WaterParticleHandler.UseParticleEffects)
                    WaterParticleHandler.ClearAllParticles();
            });
            TrailPriority = new OptionToggle("Always Show Trails", QPatch.ModName, ManWater.AlwaysShowTrails);
            TrailPriority.onValueSaved.AddListener(() => {
                ManWater.AlwaysShowTrails = TrailPriority.SavedValue;
                ManWater.SetShaders(ManWater.AlwaysShowTrails);
            });
            Height = new OptionRange("Height level", QPatch.ModName, ManWater.Height, -75f, 100f, 1f);
            Height.onValueSaved.AddListener(() => { ManWater.Height = Height.SavedValue; });

            Sound = new OptionRange("Water Traverse Loudness", QPatch.ModName, ManWater.WaterSound, 0f, 1f, 0.05f);
            Sound.onValueSaved.AddListener(() => { ManWater.WaterSound = Sound.SavedValue; });

            looseBlocksFloat = new OptionToggle("Loose Blocks and Chunks float", QPatch.ModName, QPatch.EnableLooseBlocksFloat);
            looseBlocksFloat.onValueSaved.AddListener(() => { QPatch.EnableLooseBlocksFloat = looseBlocksFloat.SavedValue; });
            noTreesInWater = new OptionToggle("Destroy <b>[!FOREVER!]</b> Submerged Trees", QPatch.ModName, QPatch.DestroyTreesInWater);
            noTreesInWater.onValueSaved.AddListener(() => { QPatch.DestroyTreesInWater = noTreesInWater.SavedValue; });
            makeDeath = new OptionToggle("but it's lava", QPatch.ModName, QPatch.theWaterIsLava);
            makeDeath.onValueSaved.AddListener(() => { QPatch.WantsLava = makeDeath.SavedValue; LavaMode.ThrowLavaDeathWarning(); });


            var WaterProperties = QPatch.ModName + " - Single Player Settings";
            Density = new OptionRange("Density", WaterProperties, ManWater.Density, -16, 16, 0.25f);
            Density.onValueSaved.AddListener(() => {
                ManWater.Density = Density.SavedValue;
                if (!ManWater.UseStandards)
                    ManWater.SetToCustom();
                WaterBlock.cachedFloatVal = WaterGlobals.Density * 5f;
            });
            FanJetMultiplier = new OptionRange("Fan jet Multiplier", WaterProperties, ManWater.FanJetMultiplier, 0f, 4f, .05f);
            FanJetMultiplier.onValueSaved.AddListener(() => {
                ManWater.FanJetMultiplier = FanJetMultiplier.SavedValue;
            });
            ResourceBuoyancy = new OptionRange("Resource Buoyancy", WaterProperties, ManWater.ResourceBuoyancyMultiplier, 0f, 4f, .05f);
            ResourceBuoyancy.onValueSaved.AddListener(() => {
                ManWater.ResourceBuoyancyMultiplier = ResourceBuoyancy.SavedValue;
            });
            BulletDampener = new OptionRange("Bullet Dampening", WaterProperties, ManWater.BulletDampener, 0f, 1E-4f, 1E-8f);
            BulletDampener.onValueSaved.AddListener(() => {
                ManWater.BulletDampener = BulletDampener.SavedValue;
            });
            MissileDampener = new OptionRange("Missile Dampening", WaterProperties, ManWater.MissileDampener, 0f, 0.1f, 0.003f);
            MissileDampener.onValueSaved.AddListener(() => {
                ManWater.MissileDampener = MissileDampener.SavedValue;
            });
            LaserFraction = new OptionRange("Laser Slowdown", WaterProperties, ManWater.LaserFraction, 0f, 0.5f, 0.025f);
            LaserFraction.onValueSaved.AddListener(() => {
                ManWater.LaserFraction = LaserFraction.SavedValue;
            });
            SurfaceSkinning = new OptionRange("Surface Skinning", WaterProperties, ManWater.SurfaceSkinning, -0.5f, 0.5f, 0.05f);
            SurfaceSkinning.onValueSaved.AddListener(() => {
                ManWater.SurfaceSkinning = SurfaceSkinning.SavedValue;
            });
            SubmergedTankDampening = new OptionRange("Submerged Tank Dampening", WaterProperties, ManWater.SubmergedTankDampening, 0f, 2f, 0.05f);
            SubmergedTankDampening.onValueSaved.AddListener(() => {
                ManWater.SubmergedTankDampening = SubmergedTankDampening.SavedValue;
            });
            SubmergedTankDampeningY = new OptionRange("Submerged Tank Dampening Y addition", WaterProperties, ManWater.SubmergedTankDampeningYAddition, -1f, 1f, 0.05f);
            SubmergedTankDampeningY.onValueSaved.AddListener(() => {
                ManWater.SubmergedTankDampeningYAddition = SubmergedTankDampeningY.SavedValue;
            });
            SurfaceTankDampening = new OptionRange("Surface Tank Dampening", WaterProperties, ManWater.SurfaceTankDampening, 0f, 2f, 0.05f);
            SurfaceTankDampening.onValueSaved.AddListener(() => {
                ManWater.SurfaceTankDampening = SurfaceTankDampening.SavedValue;
            });
            SurfaceTankDampeningY = new OptionRange("Surface Tank Dampening Y addition", WaterProperties, ManWater.SurfaceTankDampeningYAddition, -1f, 1f, 0.05f);
            SurfaceTankDampeningY.onValueSaved.AddListener(() => {
                ManWater.SurfaceTankDampeningYAddition = SurfaceTankDampeningY.SavedValue;
            });
            WheelForces = new OptionRange("Surface Wheel Force Multiplier", WaterProperties, ManWater.WheelWaterForceMultiplier, 0, 4f, 0.05f);
            WheelForces.onValueSaved.AddListener(() => {
                ManWater.WheelWaterForceMultiplier = WheelForces.SavedValue;
            });

            var WaterLook = QPatch.ModName + " - Water look";
            var waterLook = new OptionList<ManWater.WaterLook>("Water look", WaterLook, ManWater.waterLooks, ManWater.SelectedLook);
            waterLook.onValueSaved.AddListener(() =>
            {
                ManWater.SelectedLook = waterLook.SavedValue;
                ManWater.UpdateLook();
                SurfacePool.UpdateAllActiveParticles();
            });

            var waterAbyssDepth = new OptionRange("Abyss depth", WaterLook, ManWater.AbyssDepth);
            waterAbyssDepth.onValueSaved.AddListener(() => {
                ManWater.AbyssDepth = waterAbyssDepth.SavedValue;
            });

            if (ManWater._WeatherMod)
            {
                var WeatherProperties = QPatch.ModName + " - Weather mod";
                RainWeightMultiplier = new OptionRange("Rain Weight Multiplier", WeatherProperties, ManWater.RainWeightMultiplier, 0, 0.25f, 0.01f);
                RainWeightMultiplier.onValueSaved.AddListener(() => { ManWater.RainWeightMultiplier = RainWeightMultiplier.SavedValue; });
                RainDrainMultiplier = new OptionRange("Rain Drain Multiplier", WeatherProperties, ManWater.RainDrainMultiplier, 0, 0.25f, 0.01f);
                RainDrainMultiplier.onValueSaved.AddListener(() => { ManWater.RainDrainMultiplier = RainDrainMultiplier.SavedValue; });
                FloodRateClamp = new OptionRange("Flood rate Clamp", WeatherProperties, ManWater.FloodChangeClamp, 0, 0.08f, 0.001f);
                FloodRateClamp.onValueSaved.AddListener(() => { ManWater.FloodChangeClamp = FloodRateClamp.SavedValue; });
                FloodHeightMultiplier = new OptionRange("Flood Height Multiplier", WeatherProperties, ManWater.FloodHeightMultiplier, 0, 50f, 1f);
                FloodHeightMultiplier.onValueSaved.AddListener(() => { ManWater.FloodHeightMultiplier = FloodHeightMultiplier.SavedValue; });
            }
            NativeOptionsMod.onOptionsSaved.AddListener(() =>
            {
                try
                {
                    if (ManWater.UseStandards)
                        ManWater.SetToStandard();
                    else
                        ManWater.SetToCustom();
                    thisMod.WriteConfigJsonFile();
                }
                catch (Exception e)
                {
                    DebugWater.Log(e);
                }
            });
            if (ManWater.UseStandards)
                ManWater.SetToStandard();
            else
                ManWater.SetToCustom();
            WaterBlock.cachedFloatVal = WaterGlobals.Density * 5f;
        }

        public static void Save()
        {
            _thisMod.WriteConfigJsonFile();
        }
    }
}
