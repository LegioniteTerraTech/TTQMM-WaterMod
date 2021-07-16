using TTQMM_WeatherMod;

namespace WaterMod
{
    class WeatherMod
    {
        public static float RainWeight 
        {
            get 
            {
                float val;
                try 
                {
                    val = RainMaker.isRaining ? RainMaker.RainWeight : 0f;
                }
                catch 
                {
                    val = 0; 
                }
                return val;
             } 
        }
    }
}
