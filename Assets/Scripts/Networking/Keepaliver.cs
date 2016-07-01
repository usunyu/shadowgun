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
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;

public class Keepaliver : MonoBehaviour
{
	public string m_IpAddress = "127.0.0.1";
	public int m_Port = 22022;
	public float m_Period = 5.0f;

	void ProcessCommandline()
	{
		string[] arguments = System.Environment.GetCommandLineArgs();

		foreach (string str in arguments)
		{
			if (str.StartsWith("-aliveport="))
			{
				string[] param = str.Split('=');
				m_Port = System.Convert.ToInt32(param[1]);
			}
			else if (str.StartsWith("-aliveperiod="))
			{
				string[] param = str.Split('=');
				m_Period = System.Convert.ToInt32(param[1]);
			}
		}
	}

	void Awake()
	{
		// Originally we use to initiate on Start() but we need to be really aggressive here: the sooner the better
		ProcessCommandline();

		IPEndPoint targetEndpoint = null;
		Socket sendSocket = null;

		try
		{
			targetEndpoint = new IPEndPoint(IPAddress.Parse(m_IpAddress), m_Port);
		}
		catch (Exception e)
		{
			Log("Failed to create endpoint for keepalive sender: " + e.ToString());
			return;
		}

		try
		{
			sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		}
		catch (Exception e)
		{
			Log("Failed to create the send socket for keepaliver sender: " + e.ToString());
			return;
		}

		// Send the initial info right now to register to the keepaliver server asap. We need to eliminate
		// the risk that the application will freeze during initation of further components (which was really
		// happening in the past - this modification is in fact a bug fix of a real problem)
		BeepServer(sendSocket, targetEndpoint);

		StartCoroutine(BeepServerRoutine(sendSocket, targetEndpoint, m_Period));

		Log("KeepAliver activated: server address=" + m_IpAddress + ":" + m_Port + " period=" + m_Period);
	}

	void BeepServer(Socket sendSocket, IPEndPoint targetEndpoint)
	{
		System.Int32 pid = System.Diagnostics.Process.GetCurrentProcess().Id;
		sendSocket.SendTo(System.BitConverter.GetBytes(pid), targetEndpoint);
	}

	IEnumerator BeepServerRoutine(Socket sendSocket, IPEndPoint targetEndpoint, float period)
	{
		while (true)
		{
			yield return new WaitForSeconds(period);

			BeepServer(sendSocket, targetEndpoint);
		}
	}

	protected static void Log(string msg)
	{
		Debug.Log(msg);
	}
}
