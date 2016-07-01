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

using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;


/**
 * This is a special ping class. It is powered by UDP protocol and requires a special pong program running on the remote host.
 * We can't really use classic ICMP ping as there are many implementation problems on various platforms. More over,
 * we can't implement it at all for WebPlayer as it does not support ICMP packets, nor external process launching.
 * 
 * The object of the class triggers couple of pings and reports min, average and max ping.
 */
class OptimisticPing
{
	static readonly IPEndPoint AnyAddressEndpoint = new IPEndPoint(IPAddress.Any, 0);

	public readonly string Host;
	int Port;
	int Timeout;

	class PingRecord
	{
		public Stopwatch Stopwatch;
		
		public PingRecord(Stopwatch stopwatch)
		{
			Stopwatch = stopwatch;
		}
	}

	Socket MainSocket = null;

	Stopwatch ProgramTime = new Stopwatch();
	int NextPingStart = 0;
	const int PingInterval = 500;
	int PingsToShoot = 3;

	const int MAX_PACKET_SIZE = 1500;
	byte[] Buffer = new byte[MAX_PACKET_SIZE];

	Dictionary<string, PingRecord> PingRecords;
	IPEndPoint PingEndpoint;
	IPEndPoint RecvEndpoint;
	
	int MinPingTime;
	int MaxPingTime;
	int PingTimeSum;
	int PingSendCount;
	int PongRcvCount;

	static void Log(string text)
	{
		//UnityEngine.Debug.Log("UDP ping: " + text);
	}

	static void LogWarning(string text)
	{
		UnityEngine.Debug.LogWarning("UDP ping: " + text);
	}
	
	public OptimisticPing(string host)
	{
		Host = host;
		Port = 848;
		Timeout = 500;

		ProgramTime.Start();

		PingRecords = new Dictionary<string, PingRecord>();
		PingEndpoint = new IPEndPoint(Resolve(Host), Port);
		RecvEndpoint = new IPEndPoint(IPAddress.Any, 0);

		MinPingTime = Timeout * 10;
		MaxPingTime = 0;
		PingTimeSum = 0;
		PingSendCount = 0;
		PongRcvCount = 0;

		Log("Pinging to " + PingEndpoint);

		try
		{
			MainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			MainSocket.Blocking = false;
			MainSocket.Bind(RecvEndpoint);
		}
		catch (SocketException e)
		{
			LogWarning("Failed to create UDP socket: " + e);
			InternalError = true;
		}
	}

	bool InternalError = false;
	public bool IsDone
	{
		get { return InternalError || PingSendCount>=PingsToShoot && PingRecords.Count==0; }
	}

	private static IPAddress Resolve(string ipOrHost)
	{
		if (string.IsNullOrEmpty(ipOrHost))
			throw new System.ArgumentException("Supplied string must not be empty", "ipOrHost");
		
		ipOrHost = ipOrHost.Trim();
		
		// is it an ip number string?
		IPAddress ipAddress = null;
		if (IPAddress.TryParse(ipOrHost, out ipAddress))
			return ipAddress;
		
		// ok must be a host name
		IPHostEntry entry;
		
		entry = Dns.GetHostEntry(ipOrHost);
		if (entry == null)
			return null;
		
		// check each entry for a valid IP address
		foreach (IPAddress ip in entry.AddressList)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork)
				ipAddress = ip;
		}
		
		return ipAddress;
	}
	
	void CheckOutdatedRecords()
	{
		foreach (KeyValuePair<string,PingRecord> pair in PingRecords)
		{
			if (pair.Value.Stopwatch.ElapsedMilliseconds > Timeout)
			{
				Log(PingEndpoint.ToString() + " timeout");
				PingRecords.Remove(pair.Key);
				return; // to remove one per frame update is good enough for now
			}
		}
	}
	
	void TriggerNewPing()
	{
		Stopwatch stopwatch = new Stopwatch();
		
		System.DateTime now = System.DateTime.UtcNow;
		string pingIdentifier = now.Second.ToString() + ":" + now.Millisecond.ToString();
		byte[] pingData = System.Text.Encoding.ASCII.GetBytes(pingIdentifier);
		
		stopwatch.Start();
		MainSocket.SendTo(pingData, PingEndpoint);
		
		PingRecords.Add(pingIdentifier, new PingRecord(stopwatch));
	}
	
	void ReadAndProcessPong()
	{
		int bytesRead = 0;
		EndPoint responseEndpoint = AnyAddressEndpoint;
		
		try
		{
			bytesRead = MainSocket.ReceiveFrom(Buffer, ref responseEndpoint);
		}
		catch (SocketException e)
		{
			if (e.ErrorCode == (int)SocketError.ConnectionReset)
			{
				LogWarning("ConnectionReset");
			}
			else
			{
				LogWarning("Exception while reading from socket: " + e.ToString());
			}
			
			return;
		}
		
		string pongIdentifier = System.Text.Encoding.ASCII.GetString(Buffer, 0, bytesRead);
		if (PingRecords.ContainsKey(pongIdentifier))
		{
			PingRecord rec = PingRecords[pongIdentifier];
			rec.Stopwatch.Stop();
			
			PingRecords.Remove(pongIdentifier);
			
			int pingTime = (int)rec.Stopwatch.ElapsedMilliseconds;
			
			// statistics
			if (pingTime < MinPingTime)
				MinPingTime = pingTime;
			if (pingTime > MaxPingTime)
				MaxPingTime = pingTime;
			PingTimeSum += pingTime;
			PongRcvCount++;
			
			Log("Received pong from " + responseEndpoint + " in " + pingTime + " ms");
		}
		else
		{
			// probably outdated pong or other rabbish
		}
	}
	
	public void Update()
	{
		if (IsDone)
			return;

		CheckOutdatedRecords();
		
		if (PingSendCount<PingsToShoot && ProgramTime.ElapsedMilliseconds>NextPingStart)
		{
			TriggerNewPing();
			PingSendCount++;
			NextPingStart += PingInterval;
		}
		
		while (MainSocket.Available > 0)
		{
			ReadAndProcessPong();
		}
	}

	public int MinValue
	{
		get	{ return PongRcvCount>0 ? MinPingTime : -1; }
	}

	public int MaxValue
	{
		get	{ return PongRcvCount>0 ? MaxPingTime : -1; }
	}

	public int AverageValue
	{
		get	{ return PongRcvCount>0 ? PingTimeSum/PongRcvCount : -1; }
	}

	public void Dispose()
	{
		if (MainSocket != null)
		{
			MainSocket.Close();
			MainSocket = null;
		}
	}
}
