using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace NuimoSDK
{
    public class PairedNuimoManager
    {
        public async Task<IEnumerable<INuimoController>> ListPairedNuimosAsync()
        {
            return await Task.WhenAll(
                (await DeviceInformation.FindAllAsync(GattDeviceService.GetDeviceSelectorFromUuid(ServiceGuids.LedMatrixServiceGuid), null))
                    .Select(async deviceInformation => (await GattDeviceService.FromIdAsync(deviceInformation.Id)).Device)
                    .Select(async device => new NuimoBluetoothController(await device) as INuimoController)
            );
        }
    }
}
