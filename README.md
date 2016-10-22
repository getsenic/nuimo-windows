# Nuimo SDK for Universal Windows Apps

The Nuimo controller is an intuitive controller for your computer and connected smart devices. The Nuimo Windows SDK helps you to easily integrate your Windows Universal apps with Nuimo controllers.

## Installation

#### Windows project requirements

Your Windows Universal project must target Windows 10. Earlier Windows versions do not support Windows Universal apps.

Make sure to add Bluetooth capability to your `Package.appxmanifest` and manually pair your Nuimo (Windows -> Settings -> Devices -> Bluetooth).

#### NuGet package for the Nuimo library

The NuimoSDK for Universal Windows Apps is [available via the NuGet package manager](https://www.nuget.org/packages/NuimoSDK/). Verify that you have NuGet package manager installed in Visual Studio (Tools -> Extensions and Updates; Installed). In order to install the Nuimo library in your Universal Windows project you just need to right-click on the solution of your project, select "Manage NuGet packages" and search for NuimoSDK. Now you can simply install it.

## Usage

#### Basic usage

The Nuimo library makes it very easy to connect your Windows Universal apps with Nuimo controllers. Remember that you have to pair your Nuimo manually (for further information see below: A ready to checkout Windows Universal demo app). It only takes three steps and a very few lines of code to list paired Nuimos and receive gesture events:

1. Create a `PairedNuimoManager` and call `ListPairedNuimos()`. This will return a `IEnumerable<INuimoController>`.

2. In the next step you can establish a Bluetooth connection to a `NuimoController` by calling `ConnectAsync()`.

3. Subscribe to the events of the `NuimoController` in order to be notified.

The following code example demonstrates how to list paired Nuimos, connect a Nuimo and receive gesture events from your Nuimo.

#### Example code

```C#
using NuimoSDK;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

class Demo
{
	private readonly PairedNuimoManager _pairedNuimoManager = new PairedNuimoManager();
	private INuimoController _nuimoController;

	private async Task GetPairedNuimos()
	{
		var nuimoControllers = await _pairedNuimoManager.ListPairedNuimosAsync();
		_nuimoController = nuimoControllers.ElementAt(0);
	}

	private async Task Connect()
	{
		var isConnected = await _nuimoController.ConnectAsync();
	}

	private void AddDelegates()
	{
		_nuimoController.GestureEventOccurred     += OnNuimoGestureEvent;
		_nuimoController.FirmwareVersionRead      += OnFirmwareVersion;
		_nuimoController.ConnectionStateChanged   += OnConnectionState;
		_nuimoController.BatteryPercentageChanged += OnBatteryPercentage;
		_nuimoController.LedMatrixDisplayed       += OnLedMatrixDisplayed;
	}

	private void OnNuimoGestureEvent(INuimoController sender, NuimoGestureEvent nuimoGestureEvent)
	{
		Debug.WriteLine("Event: " + nuimoGestureEvent.Gesture + ", " + nuimoGestureEvent.Value);
	}

	private void OnFirmwareVersion(INuimoController sender, string firmwareVersion)
	{
		Debug.WriteLine(firmwareVersion);
	}

	private void OnConnectionState(INuimoController sender, NuimoConnectionState nuimoConnectionState)
	{
		Debug.WriteLine("Connection state: " + nuimoConnectionState);
	}

	private void OnBatteryPercentage(INuimoController sender, int batteryPercentage)
	{
		Debug.WriteLine("Battery percentage: " + batteryPercentage);
	}

	private void OnLedMatrixDisplayed(INuimoController sender)
	{
		Debug.WriteLine("LED matrix displayed");
	}

	private void SendMatrix()
	{
		var displayInterval = 5.0;
		var matrixString =
			"         " +
			"         " +
			" ..   .. " +
			"   . .   " +
			"    .    " +
			"   . .   " +
			" ..   .. " +
			"         " +
			"         ";
		_nuimoController?.DisplayLedMatrixAsync(new NuimoLedMatrix(matrixString), displayInterval, NuimoLedMatrixWriteOptions.WithFadeTransition);
	}
}
```

#### A ready to checkout Windows Universal demo app

We've provided a ready to checkout Universal Windows app that demonstrates listing paired Nuimos, connecting and receiving events from your Nuimo controllers. It also provides a simple UI for creating a matrix and send it to your Nuimo. Simply clone the [Nuimo Windows demo repository](https://github.com/getsenic/nuimo-windows-demo) and open it in Visual Studio. If your IDE shows errors, just build the project. Make sure to add Bluetooth capability to your `Package.appxmanifest` and manually pair your Nuimo (Windows -> Settings -> Devices -> Bluetooth).

## Contact & Support

Have questions or suggestions? Drop us a mail at developers@senic.com or create an issue. We'll be happy to hear from you.

## License

The nuimo-windows source code is available under the MIT License.
