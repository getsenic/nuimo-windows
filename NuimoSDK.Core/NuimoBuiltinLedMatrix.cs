namespace NuimoSDK
{
    public class NuimoBuiltinLedMatrix : NuimoLedMatrix
    {
        /// <summary>
        /// Use this constructor to call one of the built-in led matrices
        /// </summary>
        /// <param name="builtinMatrixIdentifier"></param>
        public NuimoBuiltinLedMatrix(int builtinMatrixIdentifier)
        {
            Leds = new bool[81];
            var n = builtinMatrixIdentifier;
            var i = 0;
            while (n > 0)
            {
                Leds[i] = (n % 2 > 0);
                n = n / 2;
                i += 1;
            }
        }

        public static NuimoLedMatrix Busy => new NuimoBuiltinLedMatrix(1);
    }
}