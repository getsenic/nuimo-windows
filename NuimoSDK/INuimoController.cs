using System;
using System.Threading.Tasks;

namespace NuimoSDK
{
    public interface INuimoController
    {
        event Action<INuimoController, NuimoConnectionState> ConnectionStateChanged;
        event Action<INuimoController, string>               FirmwareVersionRead;
        event Action<INuimoController>                       LedMatrixDisplayed;
        event Action<INuimoController, int>                  BatteryPercentageChanged;
        event Action<INuimoController, NuimoGestureEvent>    GestureEventOccurred;

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
