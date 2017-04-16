using System;
using System.Threading.Tasks;

namespace NuimoSDK
{
    public interface INuimoController
    {
        string Identifier { get; }
        NuimoConnectionState ConnectionState { get; }
        float MatrixBrightness { get; set; }
        TimeSpan ThrottlePeriod { get; set; }
        event Action<INuimoController, NuimoConnectionState> ConnectionStateChanged;
        event Action<INuimoController, string> FirmwareVersionRead;
        event Action<INuimoController> LedMatrixDisplayed;
        event Action<INuimoController, int> BatteryPercentageChanged;
        event Action<INuimoController, NuimoGestureEvent> GestureEventOccurred;
        event Action<INuimoController, NuimoGestureEvent> ThrottledGestureEventOccurred;

        Task<bool> ConnectAsync();
        Task<bool> DisconnectAsync();

        void DisplayLedMatrixAsync(NuimoLedMatrix matrix, double displayInterval = 2.0,
            NuimoLedMatrixWriteOptions options = NuimoLedMatrixWriteOptions.None);
    }

    public enum NuimoConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting
    }

    [Flags]
    public enum NuimoLedMatrixWriteOptions
    {
        None = 0,
        WithFadeTransition = 1,
        WithoutWriteResponse = 2
    }
}