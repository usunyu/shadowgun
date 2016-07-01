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


#if !DEADZONE_CLIENT

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerConnectionStatistics : MonoBehaviour
{
	internal class Record
	{
		internal readonly PlayerPersistantInfo		ppi;
		internal readonly uLink.NetworkPlayer		networkPlayer;
		
		internal float					connectedTimeStamp;
		internal float					pingSum;
		internal int					pingCount;
		
		internal Record( uLink.NetworkPlayer player )
		{
			networkPlayer = player;
			ppi = PPIManager.Instance.GetPPI( player );
			
			if ( ppi == null )
			{
				throw new System.InvalidOperationException( "Cannot find related PPI record" );
			}
		}
		
		internal int averagePing
		{
			get{ return (pingCount == 0) ? 0 : Mathf.RoundToInt( pingSum / pingCount ); }
		}
	}
	
	private List<Record> allRecords = new List<Record>();
	private float lastLogReportTime;
	private float lastPingUpdateTime;
	
	public float pingUpdateInterval;
	public float numberOfPingsToCalcAverage = 5;
	public float logInterval = 30.0f;
	
	void Start()
	{
		lastLogReportTime = lastPingUpdateTime = Time.realtimeSinceStartup;
		
		//TODO That value can change at runtime. Maybe we should update it over time.
		pingUpdateInterval = uLink.Network.config.timeBetweenPings;
		
		Server.Instance.OnPlayerConnected += NewPlayerConnected;
		Server.Instance.OnPlayerDisconnected += PlayerDisconnected;
	}
	
	void Update()
	{
		float timeNow = Time.realtimeSinceStartup;
		
		if ( lastPingUpdateTime + pingUpdateInterval < timeNow )
		{
			UpdatePings();
			
			lastPingUpdateTime += pingUpdateInterval;
		}
		
		
		if ( lastLogReportTime + logInterval < timeNow )
		{
			LogStatistics();
			
			lastLogReportTime += logInterval;
		}
	}
	
	void NewPlayerConnected( uLink.NetworkPlayer player, int joinRequestId )
	{
		Record	rec = new Record( player );
		rec.connectedTimeStamp = Time.realtimeSinceStartup;
		
		allRecords.Add( rec );
	}
	
	void PlayerDisconnected( uLink.NetworkPlayer player )
	{
		allRecords.RemoveAll( e => e.networkPlayer == player );
	}
	
	void UpdatePings()
	{
		foreach( Record rec in allRecords )
		{
			if ( rec.pingCount < numberOfPingsToCalcAverage )
			{
				rec.pingCount ++;
				rec.pingSum += rec.networkPlayer.lastPing;
			}
			else
			{
				rec.pingSum -= (rec.pingSum / rec.pingCount);
				rec.pingSum += rec.networkPlayer.lastPing;
			}
		}
	}
	
	const string StatisticsFormat = " {0,-20}: {1,4} {2,10} {3,8} {4,10} {5,8} {6,6} {7,6}\n";
	string GetPlayerStatistics( Record rec )
	{
		uLink.NetworkStatistics	netStats = rec.networkPlayer.statistics;
		
		string res = string.Format( StatisticsFormat,
									rec.ppi.Name,
									rec.averagePing,
									netStats.bytesReceived,
									netStats.packetsReceived,
									netStats.bytesSent,
									netStats.packetsSent,
									netStats.messagesResent,
									netStats.messagesUnsent
									);
		
		return res;
	}
	
	void LogStatistics()
	{
		if ( allRecords.Count > 0 )
		{
			string report = "\n " + System.DateTime.Now.ToString() + "\n";
			report += string.Format( StatisticsFormat, "Name", "rtt", "b-rx", "p-rx", "b-tx", "p-tx", "msg-r", "msg-u" );
			
			foreach( Record rec in allRecords )
			{
				report += GetPlayerStatistics( rec );
			}
			
			Debug.Log( report );
		}
	}
}

#endif
