using System;
using System.Threading.Tasks;

namespace NuimoSDK
{
    public interface INuimoController
    {
        event Action<NuimoConnectionState> ConnectionStateChanged;
        event Action<string>               FirmwareVersionRead;
        event Action                       LedMatrixDisplayed;
        event Action<int>                  BatteryPercentageChanged;
        event Action<NuimoGestureEvent>    GestureEventOccurred;

        string                             Identifier       { get;}
        NuimoConnectionState               ConnectionState  { get;}
        float                              MatrixBrightness { get; set; }

        Task<bool> ConnectAsync();
        Task<bool> DisconnectAsync();

        void DisplayLedMatrixAsync(NuimoLedMatrix matrix, double displayInterval = 2.0, int options = 0);
    }

    public enum NuimoConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Disconnecting,
    }

    public enum NuimoLedMatrixWriteOption
    {
        WithFadeTransition   = 1,
        WithoutWriteResponse = 2
    }
}
