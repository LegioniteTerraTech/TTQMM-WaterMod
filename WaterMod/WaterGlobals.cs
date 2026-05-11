using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            LavaDampenMulti = 3,
            WheelWaterForceMultiplier = 0.45f;
    }
    internal static class WaterGlobals
    {
        /// <summary> SERVER SETTINGS </summary>
        public static bool SimulateProjectiles = true;
        /// <summary> SERVER SETTINGS </summary>
        public static bool EnableLooseBlocksFloat = true;
        /// <summary> SERVER SETTINGS </summary>
        public static bool OceanMan2 = false;
        /// <summary> SERVER SETTINGS </summary>
        public static bool DestroyTreesInWater = false;

        /// <summary>
        /// The global settings for the Water Mod.
        /// <para>For the custom values the player can set, see <see cref="ManWater"/></para>
        /// </summary>
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
            LavaDampenMulti = 3,
            WheelWaterForceMultiplier = 0.45f;
    }
}
