using System;
using System.Linq;

namespace NuimoSDK
{
    public class NuimoLedMatrix
    {
        private const int LedCount = 81;
        private readonly char[] _ledOffCharacters = " 0".ToCharArray();

        public NuimoLedMatrix()
        {
            Leds = new bool[LedCount];
        }

        public NuimoLedMatrix(string pattern)
        {
            Leds = pattern
                .Substring(0, Math.Min(LedCount, pattern.Length))
                .PadRight(LedCount)
                .ToCharArray()
                .Select(c => !_ledOffCharacters.Contains(c))
                .ToArray();
        }

        public bool[] Leds { get; protected set; }
    }
}