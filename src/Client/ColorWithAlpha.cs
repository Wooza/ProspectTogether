using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace ProspectTogether.Client
{
    public class ColorWithAlpha
    {
        public ColorWithAlpha(byte r, byte g, byte b, byte a)
        {
            Red = r;
            Green = g;
            Blue = b;
            Alpha = a;
        }

        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
        public byte Alpha { get; set; }

        [JsonIgnore]
        public int RGBA => ColorUtil.ToRgba(Alpha, Blue, Green, Red);
    }
}
