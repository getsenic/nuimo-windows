using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace NuimoSDK
{
    public class NuimoBluetoothController : INuimoController
    {
        private readonly string _deviceId;
        private BluetoothLEDevice _bluetoothLeDevice;

        private Dictionary<Guid, GattDeviceService> _gattServicesForGuid =
            new Dictionary<Guid, GattDeviceService>();
        private readonly Dictionary<Guid, GattCharacteristic> _gattCharacteristicsForGuid =
            new Dictionary<Guid, GattCharacteristic>();

        private readonly object _gattCharacteristicsLock = new object();
        private NuimoConnectionState _connectionState = NuimoConnectionState.Disconnected;
        private bool _isThrottling;
        private int _throttledValue;
        private Timer _throttleTimer;

        public NuimoBluetoothController(string deviceId)
        {
            _deviceId = deviceId;
        }

        public event Action<INuimoController, NuimoConnectionState> ConnectionStateChanged;
        public event Action<INuimoController, string> FirmwareVersionRead;
        public event Action<INuimoController> LedMatrixDisplayed;
        public event Action<INuimoController, int> BatteryPercentageChanged;
        public event Action<INuimoController, NuimoGestureEvent> GestureEventOccurred;
        public event Action<INuimoController, NuimoGestureEvent> ThrottledGestureEventOccurred;

        public string Identifier => _bluetoothLeDevice.DeviceId.Substring(14, 12);
        public float MatrixBrightness { get; set; } = 1.0f;
        public TimeSpan ThrottlePeriod { get; set; } = TimeSpan.FromSeconds(0.7);

        public NuimoConnectionState ConnectionState
        {
            get => _connectionState;
            set
            {
                _connectionState = value;
                ConnectionStateChanged?.Invoke(this, ConnectionState);
            }
        }

        public async Task<bool> ConnectAsync()
        {
            if (ConnectionState == NuimoConnectionState.Connected ||
                ConnectionState == NuimoConnectionState.Connecting) return false;
            return await InternalConnectAsync();
        }

        private async void OnConnectionStateChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected) await InternalDisconnectAsync();
        }

        private async Task<bool> InternalConnectAsync()
        {
            ConnectionState = NuimoConnectionState.Connecting;
            var isConnected = false;
            await Task.Run(async () =>
            {
                _bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(_deviceId);
                _bluetoothLeDevice.ConnectionStatusChanged += OnConnectionStateChanged;

                var accessStatus = await _bluetoothLeDevice.RequestAccessAsync();
                if (accessStatus != DeviceAccessStatus.Allowed)
                    return;

                GattDeviceServicesResult result = await _bluetoothLeDevice.GetGattServicesAsync();
                if (result.Status == GattCommunicationStatus.Success)
                {
                    var services = result.Services;
                    _gattServicesForGuid = services.ToDictionary(service => service.Uuid);
                }

                lock (_gattCharacteristicsLock)
                {
                    
                    AddCharacteristics(ServiceGuids.SensorsServiceGuid);
                    AddCharacteristics(ServiceGuids.LedMatrixServiceGuid);
                    AddCharacteristics(ServiceGuids.BatteryServiceGuid);
                    AddCharacteristics(ServiceGuids.DeviceInformationServiceGuid);

                    isConnected = EstablishConnection() && _bluetoothLeDevice.ConnectionStatus ==
                                  BluetoothConnectionStatus.Connected;
                }
            });

            if (isConnected) ConnectionState = NuimoConnectionState.Connected;
            else await InternalDisconnectAsync();
            return isConnected;
        }

        private async void AddCharacteristics(Guid serviceGuid)
        {
            var service = _gattServicesForGuid[serviceGuid];
            if (service == null)
                return;

            var session = service.Session;
            session.MaintainConnection = true;

            GattCharacteristicsResult result = await service.GetCharacteristicsAsync();
            if (result.Status == GattCommunicationStatus.Success)
            {
                var characteristics = result.Characteristics;
                characteristics.ToList()
                    .ForEach(characteristic => 
                        _gattCharacteristicsForGuid.Add(characteristic.Uuid, characteristic));
            }
        }

        private bool EstablishConnection()
        {
            return SubscribeForCharacteristicNotifications() && ReadFirmwareVersion() && ReadBatteryLevel();
        }

        private bool SubscribeForCharacteristicNotifications()
        {
            var isConnected = true;
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
            var cancellationToken = cancellationTokenSource.Token;
            cancellationToken.Register(() => isConnected = false);

            foreach (var characteristic in CharacteristicsGuids.NotificationCharacteristicGuids
                .Select(guid => _gattCharacteristicsForGuid[guid])
                .TakeWhile(characteristic => isConnected))
            {
                characteristic.SetNotify(true, cancellationToken);
                characteristic.ValueChanged += BluetoothGattCallback;
            }
            return isConnected;
        }

        private void BluetoothGattCallback(GattCharacteristic sender, GattValueChangedEventArgs changedValue)
        {
            if (sender.Uuid.Equals(CharacteristicsGuids.BatteryCharacteristicGuid))
            {
                BatteryPercentageChanged?.Invoke(this, changedValue.CharacteristicValue.ToArray()[0]);
                return;
            }

            NuimoGestureEvent nuimoGestureEvent;
            switch (sender.Uuid.ToString())
            {
                case CharacteristicsGuids.ButtonCharacteristicGuidString:
                    nuimoGestureEvent = changedValue.ToButtonEvent();
                    break;
                case CharacteristicsGuids.SwipeCharacteristicGuidString:
                    nuimoGestureEvent = changedValue.ToSwipeEvent();
                    break;
                case CharacteristicsGuids.RotationCharacteristicGuidString:
                    nuimoGestureEvent = changedValue.ToRotationEvent();
                    break;
                case CharacteristicsGuids.FlyCharacteristicGuidString:
                    nuimoGestureEvent = changedValue.ToFlyEvent();
                    break;
                default:
                    nuimoGestureEvent = null;
                    break;
            }

            if (nuimoGestureEvent != null)
            {
                GestureEventOccurred?.Invoke(this, nuimoGestureEvent);
                ThrottleGestureEvent(nuimoGestureEvent);
            }
        }

        private void ThrottleGestureEvent(NuimoGestureEvent gestureEvent)
        {
            switch (gestureEvent.Gesture)
            {
                case NuimoGesture.Rotate:
                    if (!_isThrottling)
                    {
                        _isThrottling = true;
                        _throttleTimer = new Timer(ThrottleTimeout, null, (int) ThrottlePeriod.TotalMilliseconds,
                            Timeout.Infinite);
                    }
                    _throttledValue += gestureEvent.Value;
                    break;
                default:
                    ThrottledGestureEventOccurred?.Invoke(this, gestureEvent);
                    break;
            }
        }

        private void ThrottleTimeout(object state)
        {
            var throttledEvent = new NuimoGestureEvent(NuimoGesture.Rotate, _throttledValue);
            ThrottledGestureEventOccurred?.Invoke(this, throttledEvent);
            _isThrottling = false;
            _throttledValue = 0;
        }

        private bool ReadFirmwareVersion()
        {
            return ReadCharacteristicValue(CharacteristicsGuids.FirmwareVersionGuid, bytes =>
                FirmwareVersionRead?.Invoke(this, Encoding.GetEncoding("ASCII").GetString(bytes, 0, bytes.Length))
            );
        }

        private bool ReadBatteryLevel()
        {
            return ReadCharacteristicValue(CharacteristicsGuids.BatteryCharacteristicGuid, bytes =>
                BatteryPercentageChanged?.Invoke(this, bytes[0])
            );
        }

        private bool ReadCharacteristicValue(Guid characteristicGuid, Action<byte[]> onValueRead)
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
            var cancellationToken = cancellationTokenSource.Token;
            var readResult = _gattCharacteristicsForGuid[characteristicGuid].ReadValueAsync().AsTask(cancellationToken);
            readResult.GetAwaiter().OnCompleted(() => onValueRead(readResult.Result.Value.ToArray()));
            try
            {
                readResult.Wait(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                /*no need to handle*/
            }
            return !readResult.IsCanceled;
        }

        public async void DisplayLedMatrixAsync(NuimoLedMatrix matrix, double displayInterval = 2.0,
            NuimoLedMatrixWriteOptions options = NuimoLedMatrixWriteOptions.None)
        {
            if (ConnectionState != NuimoConnectionState.Connected) return;

            var withFadeTransition = options.HasFlag(NuimoLedMatrixWriteOptions.WithFadeTransition);
            var writeWithoutResponse = options.HasFlag(NuimoLedMatrixWriteOptions.WithoutWriteResponse);

            var byteArray = new byte[13];
            matrix.GattBytes().CopyTo(byteArray, 0);
            byteArray[10] |= Convert.ToByte(withFadeTransition ? 1 << 4 : 0);
            byteArray[11] = Convert.ToByte(Math.Max(0, Math.Min(255, MatrixBrightness * 255)));
            byteArray[12] = Convert.ToByte(Math.Max(0, Math.Min(255, displayInterval * 10)));

            if (writeWithoutResponse)
            {
#pragma warning disable CS4014
                // Because this call is not awaited, execution of the current method continues before the call is completed
                // ReSharper disable once InconsistentlySynchronizedField
                _gattCharacteristicsForGuid[CharacteristicsGuids.LedMatrixCharacteristicGuid]
                    .WriteValueAsync(byteArray.AsBuffer(), GattWriteOption.WriteWithoutResponse);
#pragma warning restore CS4014
            }
            else
            {
                // ReSharper disable once InconsistentlySynchronizedField
                var gattWriteResponse =
                    await _gattCharacteristicsForGuid[CharacteristicsGuids.LedMatrixCharacteristicGuid]
                        .WriteValueAsync(byteArray.AsBuffer(), GattWriteOption.WriteWithResponse);
                if (gattWriteResponse == GattCommunicationStatus.Success) LedMatrixDisplayed?.Invoke(this);
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            if (ConnectionState == NuimoConnectionState.Disconnected ||
                ConnectionState == NuimoConnectionState.Disconnecting) return false;
            await InternalDisconnectAsync();
            return true;
        }

        private async Task InternalDisconnectAsync()
        {
            ConnectionState = NuimoConnectionState.Disconnecting;

            await Task.Run(() =>
            {
                lock (_gattCharacteristicsLock)
                {
                    var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
                    var cancellationToken = cancellationTokenSource.Token;

                    foreach (var characteristic in _gattCharacteristicsForGuid.Values)
                        characteristic.SetNotify(false, cancellationToken);
                    UnsubscribeFromCharacteristicNotifications();
                }
            });
            _bluetoothLeDevice.Dispose();
            ConnectionState = NuimoConnectionState.Disconnected;
        }

        private void UnsubscribeFromCharacteristicNotifications()
        {
            lock (_gattCharacteristicsLock)
            {
                foreach (var characteristic in _gattCharacteristicsForGuid.Values)
                    characteristic.ValueChanged -= BluetoothGattCallback;
                _gattCharacteristicsForGuid.Clear();
            }
        }
    }

    internal static class GattCharacteristicExtension
    {
        public static void SetNotify(this GattCharacteristic characteristic, bool enabled,
            CancellationToken cancellationToken)
        {
            characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(enabled
                    ? GattClientCharacteristicConfigurationDescriptorValue.Notify
                    : GattClientCharacteristicConfigurationDescriptorValue.None)
                .AsTask(cancellationToken);
        }
    }

    internal static class GattValueChangedEventArgsExtension
    {
        public static NuimoGestureEvent ToButtonEvent(this GattValueChangedEventArgs changedValue)
        {
            var value = changedValue.CharacteristicValue.ToArray()[0];
            return new NuimoGestureEvent(value == 1 ? NuimoGesture.ButtonPress : NuimoGesture.ButtonRelease, value);
        }

        public static NuimoGestureEvent ToSwipeEvent(this GattValueChangedEventArgs changedValue)
        {
            var value = changedValue.CharacteristicValue.ToArray()[0];
            switch (value)
            {
                case 0: return new NuimoGestureEvent(NuimoGesture.SwipeLeft, value);
                case 1: return new NuimoGestureEvent(NuimoGesture.SwipeRight, value);
                case 2: return new NuimoGestureEvent(NuimoGesture.SwipeUp, value);
                case 3: return new NuimoGestureEvent(NuimoGesture.SwipeDown, value);
                default: return null;
            }
        }

        public static NuimoGestureEvent ToRotationEvent(this GattValueChangedEventArgs changedValue)
        {
            return new NuimoGestureEvent(NuimoGesture.Rotate,
                BitConverter.ToInt16(changedValue.CharacteristicValue.ToArray(), 0));
        }

        public static NuimoGestureEvent ToFlyEvent(this GattValueChangedEventArgs changedValue)
        {
            var value = changedValue.CharacteristicValue.ToArray()[0];
            switch (value)
            {
                case 0: return new NuimoGestureEvent(NuimoGesture.FlyLeft, value);
                case 1: return new NuimoGestureEvent(NuimoGesture.FlyRight, value);
                case 2: return new NuimoGestureEvent(NuimoGesture.FlyTowards, value);
                case 3: return new NuimoGestureEvent(NuimoGesture.FlyBackwards, value);
                case 4:
                    return new NuimoGestureEvent(NuimoGesture.FlyUpDown, changedValue.CharacteristicValue.ToArray()[1]);
                default: return null;
            }
        }
    }

    internal static class NuimoLedMatrixExtension
    {
        public static byte[] GattBytes(this NuimoLedMatrix matrix)
        {
            return matrix.Leds
                .Chunk(8)
                .Select(chunk => chunk
                    .Select((led, i) => Convert.ToByte(led ? 1 << i : 0))
                    .Aggregate((a, b) => Convert.ToByte(a + b)))
                .ToArray();
        }

        public static IEnumerable<IEnumerable<bool>> Chunk(this bool[] matrix, int chunkSize)
        {
            for (var i = 0; i < matrix.Length / chunkSize + 1; i++)
                yield return matrix.Skip(i * chunkSize).Take(chunkSize);
        }
    }

    internal static class ServiceGuids
    {
        internal static readonly Guid BatteryServiceGuid = new Guid("0000180f-0000-1000-8000-00805f9b34fb");
        internal static readonly Guid DeviceInformationServiceGuid = new Guid("0000180A-0000-1000-8000-00805F9B34FB");
        internal static readonly Guid SensorsServiceGuid = new Guid("f29b1525-cb19-40f3-be5c-7241ecb82fd2");
        internal static readonly Guid LedMatrixServiceGuid = new Guid("f29b1523-cb19-40f3-be5c-7241ecb82fd1");
    }

    internal static class CharacteristicsGuids
    {
        internal const string ButtonCharacteristicGuidString = "f29b1529-cb19-40f3-be5c-7241ecb82fd2";
        internal const string RotationCharacteristicGuidString = "f29b1528-cb19-40f3-be5c-7241ecb82fd2";
        internal const string SwipeCharacteristicGuidString = "f29b1527-cb19-40f3-be5c-7241ecb82fd2";
        internal const string FlyCharacteristicGuidString = "f29b1526-cb19-40f3-be5c-7241ecb82fd2";
        internal static readonly Guid BatteryCharacteristicGuid = new Guid("00002a19-0000-1000-8000-00805f9b34fb");

        internal static readonly Guid DeviceInformationCharacteristicGuid =
            new Guid("00002A29-0000-1000-8000-00805F9B34FB");

        internal static readonly Guid FirmwareVersionGuid = new Guid("00002a26-0000-1000-8000-00805f9b34fb");
        internal static readonly Guid LedMatrixCharacteristicGuid = new Guid("f29b1524-cb19-40f3-be5c-7241ecb82fd1");

        private static readonly Guid ButtonCharacteristicGuid = new Guid("f29b1529-cb19-40f3-be5c-7241ecb82fd2");
        private static readonly Guid RotationCharacteristicGuid = new Guid("f29b1528-cb19-40f3-be5c-7241ecb82fd2");
        private static readonly Guid SwipeCharacteristicGuid = new Guid("f29b1527-cb19-40f3-be5c-7241ecb82fd2");
        private static readonly Guid FlyCharacteristicGuid = new Guid("f29b1526-cb19-40f3-be5c-7241ecb82fd2");

        internal static readonly Guid[] GestureCharacteristicGuids =
        {
            ButtonCharacteristicGuid,
            SwipeCharacteristicGuid,
            RotationCharacteristicGuid,
            FlyCharacteristicGuid
        };

        internal static readonly Guid[] NotificationCharacteristicGuids =
        {
            ButtonCharacteristicGuid,
            SwipeCharacteristicGuid,
            RotationCharacteristicGuid,
            FlyCharacteristicGuid,
            BatteryCharacteristicGuid
        };
    }
}