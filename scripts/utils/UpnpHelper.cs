using Godot;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

public partial class UpnpHelper : Node
{
	private Upnp upnp;
	private GodotThread thread;
	public static bool ipv6 = false;
	public void setup() {
		upnp = new Upnp { DiscoverIpv6 = ipv6 };
		Upnp.UpnpResult code = (Upnp.UpnpResult)upnp.Discover();
		if (code == Upnp.UpnpResult.Success && upnp.GetGateway() != null && upnp.GetGateway().IsValidGateway()) {
			Upnp.UpnpResult code2 = (Upnp.UpnpResult)upnp.AddPortMapping(Gameboard.PORT);
			GD.Print("UPNP result (add):", code2.ToString());
		}
		GD.Print("UPNP result:", code.ToString());
		EmitSignal(SignalName.sigUpnpComplete, code.ToString());
	}

	public async void queryExternalIP() {
		await Task.Run(() => {GD.Print("UPNP result (IP): " + upnp.QueryExternalAddress()); EmitSignal(SignalName.sigUpnpUpdateIP, upnp.QueryExternalAddress());});
	} 

	public string getExternalIP() {
		return upnp.QueryExternalAddress();
	} 

	public async override void _ExitTree()
	{
		await Task.Run (() => {
			Upnp.UpnpResult code = (Upnp.UpnpResult)upnp.DeletePortMapping(Gameboard.PORT);
			GD.Print("UPNP result 2:", code.ToString());
			upnp.ClearDevices();
			EmitSignal(SignalName.sigUpnpDeleteMapping, code.ToString());
		});
	}

	[Signal]
	public delegate void sigUpnpCompleteEventHandler(string msg);

	[Signal]
	public delegate void sigUpnpUpdateIPEventHandler(string IP);

	[Signal]
	public delegate void sigUpnpDeleteMappingEventHandler(string msg);
}
