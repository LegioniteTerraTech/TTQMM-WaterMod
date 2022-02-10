// https://github.com/fuqunaga/RapidGUI

using System;
using System.Reflection;
using HarmonyLib;
using ModHelper.Config;
using UnityEngine;
using Nuterra.NativeOptions;
using System.IO;

namespace WaterMod
{
    public class QPatch
    {
        const string ModName = "Water Mod";
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
            get => WaterBuoyancy.HeightCalc;
        }

        public static ModConfig _thisMod;

        public static KeyCode key;
        public static int key_int = 111;

        internal static AssetBundle assetBundle;
        internal static string asm_path = Assembly.GetExecutingAssembly().Location.Replace("WaterMod.dll", "");
        internal static string assets_path = Path.Combine(asm_path, "Assets");
        public static Material basic;
        public static Material fancy;

        // Experimentals
        public static bool HeightBasedFloat = true;

        public static bool EnableLooseBlocksFloat = true;
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
            var harmony = new Harmony("aceba1.ttmm.revived.water");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            assetBundle = AssetBundle.LoadFromFile(Path.Combine(assets_path, "waterassets"));

            ModConfig thisMod = new ModConfig();

            thisMod.BindConfig<WaterParticleHandler>(null, "UseParticleEffects");

            WaterBuoyancy.Initiate();
            RemoveScenery.Initiate();
            LavaMode.Initiate();

            thisMod.BindConfig<QPatch>(null, "key_int");
            key = (KeyCode)key_int;
            thisMod.BindConfig<WaterBuoyancy>(null, "IsActive");
            thisMod.BindConfig<WaterBuoyancy>(null, "height");
            thisMod.BindConfig<WaterBuoyancy>(null, "Density");
            thisMod.BindConfig<WaterBuoyancy>(null, "FanJetMultiplier");
            thisMod.BindConfig<WaterBuoyancy>(null, "ResourceBuoyancyMultiplier");
            thisMod.BindConfig<WaterBuoyancy>(null, "BulletDampener");
            thisMod.BindConfig<WaterBuoyancy>(null, "MissileDampener");
            thisMod.BindConfig<WaterBuoyancy>(null, "LaserFraction");
            thisMod.BindConfig<WaterBuoyancy>(null, "SurfaceSkinning");
            thisMod.BindConfig<WaterBuoyancy>(null, "SubmergedTankDampening");
            thisMod.BindConfig<WaterBuoyancy>(null, "SubmergedTankDampeningYAddition");
            thisMod.BindConfig<WaterBuoyancy>(null, "SurfaceTankDampening");
            thisMod.BindConfig<WaterBuoyancy>(null, "SurfaceTankDampeningYAddition");
            thisMod.BindConfig<WaterBuoyancy>(null, "SelectedLook");
            thisMod.BindConfig<WaterBuoyancy>(null, "AbyssDepth");

            thisMod.BindConfig<QPatch>(null, "EnableLooseBlocksFloat");
            thisMod.BindConfig<QPatch>(null, "DestroyTreesInWater");
            thisMod.BindConfig<QPatch>(null, "theWaterIsLava");

            WaterBuoyancy._WeatherMod = ModExists("TTQMM WeatherMod");
            if (WaterBuoyancy._WeatherMod)
            {
                Debug.Log("Found WeatherMod!");
                thisMod.BindConfig<WaterBuoyancy>(null, "RainWeightMultiplier");
                thisMod.BindConfig<WaterBuoyancy>(null, "RainDrainMultiplier");
                thisMod.BindConfig<WaterBuoyancy>(null, "FloodChangeClamp");
                thisMod.BindConfig<WaterBuoyancy>(null, "floodHeightMultiplier");
            }

            _thisMod = thisMod;


            GUIMenu = new OptionKey("GUI Menu button", ModName, key);
            GUIMenu.onValueSaved.AddListener(() => { key_int = (int)(key = GUIMenu.SavedValue); WaterBuoyancy._inst.Invoke("Save", 0.5f); });

            IsWaterActive = new OptionToggle("Water Active", ModName, WaterBuoyancy.IsActive);
            IsWaterActive.onValueSaved.AddListener(() => { WaterBuoyancy.IsActive = IsWaterActive.SavedValue; WaterBuoyancy.SetState(); });
            UseParticleEffects = new OptionToggle("Particle effects Active", ModName, WaterParticleHandler.UseParticleEffects);
            UseParticleEffects.onValueSaved.AddListener(() => { WaterParticleHandler.UseParticleEffects = UseParticleEffects.SavedValue; });
            Height = new OptionRange("Height level", ModName, WaterBuoyancy.Height, -75f, 100f, 1f);
            Height.onValueSaved.AddListener(() => { WaterBuoyancy.Height = Height.SavedValue; });

            looseBlocksFloat = new OptionToggle("Loose Blocks and Chunks float", ModName, EnableLooseBlocksFloat);
            looseBlocksFloat.onValueSaved.AddListener(() => { EnableLooseBlocksFloat = looseBlocksFloat.SavedValue; });
            noTreesInWater = new OptionToggle("Destroy <b>[!FOREVER!]</b> Submerged Trees", ModName, DestroyTreesInWater);
            noTreesInWater.onValueSaved.AddListener(() => { DestroyTreesInWater = noTreesInWater.SavedValue; });
            makeDeath = new OptionToggle("but it's lava", ModName, theWaterIsLava);
            makeDeath.onValueSaved.AddListener(() => { WantsLava = makeDeath.SavedValue; LavaMode.ThrowLavaDeathWarning(); });


            var WaterProperties = ModName + " - Water properties";
            Density = new OptionRange("Density", WaterProperties, WaterBuoyancy.Density, -16, 16, 0.25f);
            Density.onValueSaved.AddListener(() => { WaterBuoyancy.Density = Density.SavedValue; });
            FanJetMultiplier = new OptionRange("Fan jet Multiplier", WaterProperties, WaterBuoyancy.FanJetMultiplier, 0f, 4f, .05f);
            FanJetMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.FanJetMultiplier = FanJetMultiplier.SavedValue; });
            ResourceBuoyancy = new OptionRange("Resource Buoyancy", WaterProperties, WaterBuoyancy.ResourceBuoyancyMultiplier, 0f, 4f, .05f);
            ResourceBuoyancy.onValueSaved.AddListener(() => { WaterBuoyancy.ResourceBuoyancyMultiplier = ResourceBuoyancy.SavedValue; });
            BulletDampener = new OptionRange("Bullet Dampening", WaterProperties, WaterBuoyancy.BulletDampener, 0f, 1E-4f, 1E-8f);
            BulletDampener.onValueSaved.AddListener(() => { WaterBuoyancy.BulletDampener = BulletDampener.SavedValue; });
            MissileDampener = new OptionRange("Missile Dampening", WaterProperties, WaterBuoyancy.MissileDampener, 0f, 0.1f, 0.003f);
            MissileDampener.onValueSaved.AddListener(() => { WaterBuoyancy.MissileDampener = MissileDampener.SavedValue; });
            LaserFraction = new OptionRange("Laser Slowdown", WaterProperties, WaterBuoyancy.LaserFraction, 0f, 0.5f, 0.025f);
            LaserFraction.onValueSaved.AddListener(() => { WaterBuoyancy.LaserFraction = LaserFraction.SavedValue; });
            SurfaceSkinning = new OptionRange("Surface Skinning", WaterProperties, WaterBuoyancy.SurfaceSkinning, -0.5f, 0.5f, 0.05f);
            SurfaceSkinning.onValueSaved.AddListener(() => { WaterBuoyancy.SurfaceSkinning = SurfaceSkinning.SavedValue; });
            SubmergedTankDampening = new OptionRange("Submerged Tank Dampening", WaterProperties, WaterBuoyancy.SubmergedTankDampening, 0f, 2f, 0.05f);
            SubmergedTankDampening.onValueSaved.AddListener(() => { WaterBuoyancy.SubmergedTankDampening = SubmergedTankDampening.SavedValue; });
            SubmergedTankDampeningY = new OptionRange("Submerged Tank Dampening Y addition", WaterProperties, WaterBuoyancy.SubmergedTankDampeningYAddition, -1f, 1f, 0.05f);
            SubmergedTankDampeningY.onValueSaved.AddListener(() => { WaterBuoyancy.SubmergedTankDampeningYAddition = SubmergedTankDampeningY.SavedValue; });
            SurfaceTankDampening = new OptionRange("Surface Tank Dampening", WaterProperties, WaterBuoyancy.SurfaceTankDampening, 0f, 2f, 0.05f);
            SurfaceTankDampening.onValueSaved.AddListener(() => { WaterBuoyancy.SurfaceTankDampening = SurfaceTankDampening.SavedValue; });
            SurfaceTankDampeningY = new OptionRange("Surface Tank Dampening Y addition", WaterProperties, WaterBuoyancy.SurfaceTankDampeningYAddition, -1f, 1f, 0.05f);
            SurfaceTankDampeningY.onValueSaved.AddListener(() => { WaterBuoyancy.SurfaceTankDampeningYAddition = SurfaceTankDampeningY.SavedValue; });

            var WaterLook = ModName + " - Water look";
            var waterLook = new OptionList<WaterBuoyancy.WaterLook>("Water look", WaterLook, WaterBuoyancy.waterLooks, WaterBuoyancy.SelectedLook);
            waterLook.onValueSaved.AddListener(() =>
            {
                WaterBuoyancy.UpdateLook(waterLook.Selected);
                WaterBuoyancy.SelectedLook = waterLook.SavedValue;
            });

            var waterAbyssDepth = new OptionRange("Abyss depth", WaterLook, WaterBuoyancy.AbyssDepth);
            waterAbyssDepth.onValueSaved.AddListener(() => { WaterBuoyancy.AbyssDepth = waterAbyssDepth.SavedValue; });

            if (WaterBuoyancy._WeatherMod)
            {
                var WeatherProperties = ModName + " - Weather mod";
                RainWeightMultiplier = new OptionRange("Rain Weight Multiplier", WeatherProperties, WaterBuoyancy.RainWeightMultiplier, 0, 0.25f, 0.01f);
                RainWeightMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.RainWeightMultiplier = RainWeightMultiplier.SavedValue; });
                RainDrainMultiplier = new OptionRange("Rain Drain Multiplier", WeatherProperties, WaterBuoyancy.RainDrainMultiplier, 0, 0.25f, 0.01f);
                RainDrainMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.RainDrainMultiplier = RainDrainMultiplier.SavedValue; });
                FloodRateClamp = new OptionRange("Flood rate Clamp", WeatherProperties, WaterBuoyancy.FloodChangeClamp, 0, 0.08f, 0.001f);
                FloodRateClamp.onValueSaved.AddListener(() => { WaterBuoyancy.FloodChangeClamp = FloodRateClamp.SavedValue; });
                FloodHeightMultiplier = new OptionRange("Flood Height Multiplier", WeatherProperties, WaterBuoyancy.FloodHeightMultiplier, 0, 50f, 1f);
                FloodHeightMultiplier.onValueSaved.AddListener(() => { WaterBuoyancy.FloodHeightMultiplier = FloodHeightMultiplier.SavedValue; });
            }
            WaterBuoyancy.UpdateHeightCalc();
        }
        public static OptionKey GUIMenu;
        public static OptionToggle IsWaterActive;
        public static OptionToggle UseParticleEffects;
        public static OptionRange Height;

        public static OptionToggle looseBlocksFloat;
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

        public static OptionRange RainWeightMultiplier;
        public static OptionRange RainDrainMultiplier;
        public static OptionRange FloodRateClamp;
        public static OptionRange FloodHeightMultiplier;

        public static OptionToggle Reset;
    }

    internal class Patches
    {
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnPool")]
        private class PatchBlock
        {
            private static void Postfix(TankBlock __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterBlock>();
                wEffect.TankBlock = __instance;
                if (__instance.BlockCategory == BlockCategories.Flight)
                {
                    var component = __instance.GetComponentInChildren<FanJet>();
                    if (component != null)
                    {
                        wEffect.isFanJet = true;
                        wEffect.componentEffect = component;
                        wEffect.initVelocity = new Vector3(component.force, component.backForce, 0f);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnRecycle")]
        private class TankBlockRecycle
        {
            private static void Postfix(TankBlock __instance)
            {
                try
                {
                    __instance.gameObject.GetComponent<WaterBlock>().TryRemoveSurface();
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(Tank))]
        [HarmonyPatch("OnPool")]
        private class PatchTank
        {
            private static void Postfix(Tank __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterTank>();
                wEffect.Subscribe(__instance);
            }
        }

        [HarmonyPatch(typeof(Projectile), "OnPool")]
        private class PatchProjectileSpawn
        {
            private static void Postfix(Projectile __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterObj>();
                wEffect.effectBase = __instance;

                if (__instance is MissileProjectile missile)
                {
                    wEffect.effectType = EffectTypes.MissileProjectile;
                }
                else if (__instance is LaserProjectile laser)
                {
                    wEffect.effectType = EffectTypes.LaserProjectile;
                }
                else
                {
                    wEffect.effectType = EffectTypes.NormalProjectile;
                }
                wEffect._rbody = __instance.rbody;
            }
        }

        //[HarmonyPatch(typeof(MissileProjectile), "Fire")]
        //private class PatchMissile
        //{
        //    private static void Prefix(MissileProjectile __instance)
        //    {
        //        var wEffect = __instance.gameObject.GetComponent<WaterBuoyancy.WaterObj>();
        //        if (wEffect == null)
        //        {
        //            wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
        //        }
        //        else if (wEffect.effectType >= WaterBuoyancy.EffectTypes.MissileProjectile)
        //        {
        //            return;
        //        }

        //        wEffect.effectBase = __instance;
        //        wEffect.effectType = WaterBuoyancy.EffectTypes.MissileProjectile;
        //        wEffect.GetRBody();
        //    }
        //}

        //[HarmonyPatch(typeof(Projectile), "Fire")]
        //private class PatchProjectile
        //{
        //    private static void Prefix(Projectile __instance)
        //    {
        //        var wEffect = __instance.GetComponent<WaterBuoyancy.WaterObj>();
        //        if (wEffect == null)
        //        {
        //            wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
        //        }
        //        else
        //        {
        //            return;
        //        }

        //        wEffect.effectBase = __instance;
        //        wEffect.effectType = WaterBuoyancy.EffectTypes.NormalProjectile;
        //        wEffect.GetRBody();
        //    }
        //}

        //[HarmonyPatch(typeof(LaserProjectile), "Fire")]
        //private class PatchLaser
        //{
        //    private static void Prefix(LaserProjectile __instance)
        //    {
        //        var wEffect = __instance.GetComponent<WaterBuoyancy.WaterObj>();
        //        if (wEffect == null)
        //        {
        //            wEffect = __instance.gameObject.AddComponent<WaterBuoyancy.WaterObj>();
        //        }
        //        else if (wEffect.effectType >= WaterBuoyancy.EffectTypes.LaserProjectile)
        //        {
        //            return;
        //        }

        //        wEffect.effectBase = __instance;
        //        wEffect.effectType = WaterBuoyancy.EffectTypes.LaserProjectile;
        //        wEffect.GetRBody();
        //    }
        //}

        [HarmonyPatch(typeof(ResourcePickup))]
        [HarmonyPatch("OnPool")]
        private class PatchResource
        {
            private static void Postfix(ResourcePickup __instance)
            {
                var wEffect = __instance.gameObject.AddComponent<WaterObj>();
                wEffect.effectBase = __instance;
                wEffect.effectType = EffectTypes.ResourceChunk;
                wEffect.GetRBody();
            }
        }

        
        [HarmonyPatch(typeof(TileManager))]
        [HarmonyPatch("Init")]
        private class PatchTiles
        {
            private static void Postfix(TileManager __instance)
            {
                RemoveScenery.Sub();
            }
        }
    }
}