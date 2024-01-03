using System;
using Vintagestory.API.Common;

namespace ProspectTogether.Client
{
    public class ColorWithAlphaUpdate
    {

        private readonly byte? red;
        private readonly byte? green;
        private readonly byte? blue;
        private readonly byte? alpha;

        public ColorWithAlphaUpdate(byte? r, byte? g, byte? b, byte? a)
        {
            red = r;
            green = g;
            blue = b;
            alpha = a;
        }

        public void ApplyUpdateTo(ColorWithAlpha other)
        {
            other.Red = red ?? other.Red;
            other.Green = green ?? other.Green;
            other.Blue = blue ?? other.Blue;
            other.Alpha = alpha ?? other.Alpha;
        }
    }

    public class ColorWithAlphaArgParser : ArgumentParserBase
    {
        private ColorWithAlphaUpdate value = new(null, null, null, null);

        public ColorWithAlphaArgParser(string argName, bool isMandatoryArg) : base(argName, isMandatoryArg)
        {
        }

        public override object GetValue()
        {
            return value;
        }

        public override void SetValue(object data)
        {
            value = (ColorWithAlphaUpdate)data;
        }

        public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
        {
            // TODO: In theory this is bad design, because this parser consumes a variable amount of arguments,
            // so we might 'steal' arguments from a subsequent parser, even though they are not meant for this parser. 
            // In other words, this parser should always be last.

            int argCount = args.RawArgs.Length;
            if (!(argCount == 1 || argCount == 3 || argCount == 4))
            {
                lastErrorMessage = "Invalid color format. Specify either R G B A or R G B or only A.";
                return EnumParseResult.Bad;
            }

            byte[] values = new byte[argCount];
            for (int i = 0; i < values.Length; i++)
            {
                var arg = args.RawArgs.PopInt();
                if (arg == null)
                {
                    lastErrorMessage = $"Color component {i + 1} is not a number.";
                    return EnumParseResult.Bad;
                }
                if (arg < 0 || arg > 255)
                {
                    lastErrorMessage = $"Color component {i + 1} must be in range [0-255].";
                    return EnumParseResult.Bad;
                }
                values[i] = (byte)arg.Value;
            }

            if (values.Length == 1)
                value = new ColorWithAlphaUpdate(null, null, null, values[0]);
            else if (values.Length == 3)
                value = new ColorWithAlphaUpdate(values[0], values[1], values[2], null);
            else
                value = new ColorWithAlphaUpdate(values[0], values[1], values[2], values[3]);
            return EnumParseResult.Good;
        }
    }
}
