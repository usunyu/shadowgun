//
// By using or accessing the source codes or any other information of the Game SHADOWGUN: DeadZone ("Game"),
// you ("You" or "Licensee") agree to be bound by all the terms and conditions of SHADOWGUN: DeadZone Public
// License Agreement (the "PLA") starting the day you access the "Game" under the Terms of the "PLA".
//
// You can review the most current version of the "PLA" at any time at: http://madfingergames.com/pla/deadzone
//
// If you don't agree to all the terms and conditions of the "PLA", you shouldn't, and aren't permitted
// to use or access the source codes or any other information of the "Game" supplied by MADFINGER Games, a.s.
//

using UnityEngine;
using System.Collections;
using uLink;

public class RegisterGameServerInMasterServer : UnityEngine.MonoBehaviour
{
	// This is used when this game server has registered to the master server. It will report current number fo players every 5 seconds.
	public int reportNumberOfPlayersFrequency = 5;

	// This is used if the masterserver is restated. This game server will retry registration every 2 seconds. This makes it possible to restart
	// the uLink master server without restarting any game servers.
	public float retryFrequencyForRegister = 4.0F;

	/*
	 * 
	void Start () {
		uLink.MasterServer.updateRate = reportNumberOfPlayersFrequency;
		StartCoroutine(DoRegister());
	}
	
	
	IEnumerator DoRegister()
	{
		while (true)
		{
			if (!uLink.MasterServer.isRegistered && uLink.Network.status == uLink.NetworkStatus.Connected)
			{
				Debug.Log("Now tries to registers this dedicated game server with ip " + uLink.Network.player.ipAddress + " in uLink master server at " + uLink.MasterServer.ipAddress);
				//uLink.MasterServer.gameType & gameName has been set in the script file Resources/uLinkNetworkPrefs.cs
				//uLink.MasterServer.RegisterHost(System.Net.IPAddress.Parse("192.168.1.150"));
				uLink.MasterServer.RegisterHost();

			}
			yield return new WaitForSeconds(retryFrequencyForRegister);
		}
	}

	void uLink_OnMasterServerEvent(uLink.MasterServerEvent msEvent)
	{
		Debug.Log("RegisterGameServerInMasterServer: " + msEvent);
	}
	
	*/
}
