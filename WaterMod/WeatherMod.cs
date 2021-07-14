using TTQMM_WeatherMod;

namespace WaterMod
{
    class WeatherMod
    {
        public static float RainWeight { get => (RainMaker.isRaining ? RainMaker.RainWeight : 0f); }
    }
}
