using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace NuimoSDK
{
    public class PairedNuimoManager
    {
        private readonly DeviceWatcher _deviceWatcher;

        public PairedNuimoManager()
        {
            _deviceWatcher =
                DeviceInformation.CreateWatcher(
                    BluetoothLEDevice.GetDeviceSelectorFromDeviceName("Nuimo"),
                    null,
                    DeviceInformationKind.AssociationEndpoint);

            _deviceWatcher.Added += _deviceWatcher_Added;
            _deviceWatcher.Removed += _deviceWatcher_Removed;
        }

        public event Action<INuimoController> NuimoFound;
        public event Action<INuimoController> NuimoLost;

        private void _deviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Debug.WriteLine("TODO: device removed");
        }

        private async void _deviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            var bleDevice = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);
            var nuimo = new NuimoBluetoothController(bleDevice) as INuimoController;
            NuimoFound?.Invoke(nuimo);
        }

        public void StartWatching()
        {
            _deviceWatcher.Start();
        }

        public void StopWatching()
        {
            _deviceWatcher.Stop();
        }

        public async Task<IEnumerable<INuimoController>> ListPairedNuimosAsync()
        {
            return await Task.WhenAll(
                (await DeviceInformation.FindAllAsync(
                    GattDeviceService.GetDeviceSelectorFromUuid(ServiceGuids.LedMatrixServiceGuid), null))
                .Select(async deviceInformation => await BluetoothLEDevice.FromIdAsync(deviceInformation.Id))
                .Select(async device => new NuimoBluetoothController(await device) as INuimoController)
            );
        }
    }
}