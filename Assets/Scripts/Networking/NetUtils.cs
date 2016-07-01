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

/// <summary>
/// Supporting functionality for multiplayer/network games
/// There will be no instance of this class, all the functions must be static
///
/// What type of functions you could expect here:
/// - data quantization / dequantization support
/// - connection quality settings
/// - network statistics
/// - network version
///</summary>
[System.Serializable]
public class NetUtils : MonoBehaviour
{
	static NetUtils _Instance;

	// currently this member is protected as there is no serious reason to make it public
	protected static NetUtils Instance
	{
		get
		{
			if (_Instance == null)
			{
				GameObject go = new GameObject("NetUtils");

				_Instance = go.AddComponent<NetUtils>() as NetUtils;

				GameObject.DontDestroyOnLoad(go);
			}

			return _Instance;
		}
	}

	// don't allow to create an instance from outside of this class
	NetUtils()
	{
	}

	public enum E_ConnectionQuality
	{
		Good,
		Average,
		Bad,
		None
	}

	// ============================ connection quality ===========================

	public static E_ConnectionQuality GetConnectionQuality()
	{
		if (uLink.Network.isClient)
		{
			int ping = uLink.Network.GetAveragePing(uLink.NetworkPlayer.server);

			if (ping < 100)
			{
				return E_ConnectionQuality.Good;
			}
			else if (ping < 150)
			{
				return E_ConnectionQuality.Average;
			}
			else
			{
				return E_ConnectionQuality.Bad;
			}
		}

		return E_ConnectionQuality.None;
	}

	// =============================== data quantization support ===============================

	public static int Bitmask(int bits)
	{
		//assert( bits < 32 );

		return (1 << bits) - 1;
	}

	// find better name
	public static int MaxValueForBits(int bits)
	{
		// assert( bits < 32 );

		return (1 << bits) - 1;
	}

	public static int QuantizeAngle(float angle, int bits)
	{
		float floatMaxValue = MaxValueForBits(bits);
		return Mathf.RoundToInt(floatMaxValue*angle/360.0f);
	}

	public static float DequantizeAngle(int quantizedValue, int bits)
	{
		float maxValue = MaxValueForBits(bits);
		return quantizedValue/maxValue*359.99f;
	}

	// =============================== packet loss and latency simulation ===============================

	[System.Serializable]
	public class ConnectionQuality
	{
		public int PacketLoss_percent;
		public int MinLatency_ms;
		public int MaxLatency_ms;
	};

	public static void SetConnectionQualityEmulation(ConnectionQuality connectionParams)
	{
		int minLatency = connectionParams.MinLatency_ms < 0 ? 0 : connectionParams.MinLatency_ms;
		int maxLatency = connectionParams.MaxLatency_ms < minLatency ? minLatency : connectionParams.MaxLatency_ms;

		uLink.Network.emulation.chanceOfLoss = 0.01f*connectionParams.PacketLoss_percent;
		uLink.Network.emulation.minLatency = minLatency/1000.0f;
		uLink.Network.emulation.maxLatency = maxLatency/1000.0f;
	}

	public static void ResetConnectionQualityEmulation()
	{
		uLink.Network.emulation.chanceOfLoss = 0.0f;
		uLink.Network.emulation.minLatency = -1.0f;
		uLink.Network.emulation.maxLatency = -1.0f;
	}

	// =============================== network statistics ===============================

	static uLinkStatisticsGUI _StatisticsGUI = null;

	protected static uLinkStatisticsGUI StatisticsGUI
	{
		get
		{
			if (_StatisticsGUI == null)
			{
				_StatisticsGUI = Instance.gameObject.AddComponent<uLinkStatisticsGUI>() as uLinkStatisticsGUI;
				// The showFrameRate variable has ben removed from the API - why? What's the current status? Is the FPS automatically displayed?
				//_StatisticsGUI.showFrameRate = true;
			}

			return _StatisticsGUI;
		}
	}

	public static void DisplayNetworkStatistics()
	{
		StatisticsGUI.isEnabled = true;
	}

	public static void HideNetworkStatistics()
	{
		// it could even destroy the component
		StatisticsGUI.isEnabled = false;
	}

	// =============================== network version ===============================

	public class Version
	{
		readonly string VersionString;

		public Version(string versionString)
		{
			VersionString = versionString;
		}

		public override string ToString()
		{
			return VersionString;
		}

		public override bool Equals(object other)
		{
			Version ver = other as Version;
			if (ver != null)
			{
				return VersionString == ver.VersionString;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return VersionString.GetHashCode();
		}
	}

	public static Version CurrentVersion
	{
		get
		{
			switch (Configuration)
			{
			case ConfigurationType.Local:
				return m_Version_Local;

			case ConfigurationType.Develop:
				return m_Version_Develop;

			case ConfigurationType.Beta:
				return m_Version_Beta;

			case ConfigurationType.Final:
				return m_Version_Final;
			}

			return m_Version_Develop;
		}
	}

	// =============================== network regions ==================================

	public enum GeoRegion
	{
		// !!! Don't change order. new items on end...
		America,
		Europe,
		Asia,
		None
	};

	public static string GetRegionString(NetUtils.GeoRegion region)
	{
		switch (region)
		{
		case NetUtils.GeoRegion.America:
			return "america";
		case NetUtils.GeoRegion.Europe:
			return "europe";
		case NetUtils.GeoRegion.Asia:
			return "asia";
		default:
			return null;
		}
	}

	public static NetUtils.GeoRegion GetRegionFromString(string region)
	{
		if (string.IsNullOrEmpty(region) == true)
			return NetUtils.GeoRegion.None;

		switch (region.ToLower())
		{
		case "america":
			return NetUtils.GeoRegion.America;
		case "europe":
			return NetUtils.GeoRegion.Europe;
		case "asia":
			return NetUtils.GeoRegion.Asia;
		default:
			return NetUtils.GeoRegion.None;
		}
	}

	public static NetUtils.GeoRegion GetRegionFromGeoPoint(float inLangitude, float inLongitude)
	{
		// normalize latitude
		while (inLongitude > 180)
			inLongitude -= 360;
		while (inLongitude <= -180)
			inLongitude += 360;

		if (inLongitude < -30)
		{
			return GeoRegion.America;
		}
		else if (inLongitude < 60)
		{
			return GeoRegion.Europe;
		}
		else
		{
			return GeoRegion.Asia;
		}
	}

	public static bool GetDefaultMatchmakingServerAddress(GeoRegion region, out string ip, out int port)
	{
		switch (Configuration)
		{
		case ConfigurationType.Local:
			ip = LocalIP;
			break;

		case ConfigurationType.Develop:
			ip = m_Matchmaking_DefaultServerIP_Develop;
			break;

		default:
			port = 0;
			ip = null;
			return false;
		}

		port = DefaulMatchmakingServerPort;
		return true;
	}

	// =============================== global configuration =============================
	// those values are used by all/most of the networking systems

	public static int DefaulMatchmakingServerPort
	{
		get
		{
			switch (Configuration)
			{
			case ConfigurationType.Final:
				return m_Matchmaking_DefaultServerPort_Final;

			case ConfigurationType.Beta:
				return m_Matchmaking_DefaultServerPort_Beta;

			default:
				return m_Matchmaking_DefaultServerPort_Develop;
			}
		}
	}

	public enum ConfigurationType
	{
		Local, // Matchmaking server runs on local host - this configuration is usefull for lobby application development (programmers)
		Develop, // Matchmaking server runs on our testing server with a public IP address
		Beta, // For public beta builds
		Final // Configuration for final released game - no one beside final builds should use this configuration
	};

	// =============================== global configuration =============================
	// those values are used by all/most of the networking systems

	// By default we are using develop configuration which means that everyone connetcts to develop match-making (never to final one)
	//public static readonly ConfigurationType Configuration = ConfigurationType.Develop;
	//public static readonly ConfigurationType Configuration = ConfigurationType.Beta;
	public readonly static ConfigurationType Configuration = ConfigurationType.Final;
	//public static readonly ConfigurationType Configuration = ConfigurationType.Local;

	//WARNING: The network version is used as a key to get the IP address of the match-making server from the cloud.
	//         Whenever you change the version number make sure that there is an appropriate record written on our cloud.
	const string m_MainVersion = "2.4.9";

	static Version m_Version_Local = new Version(m_MainVersion + ".local");
	static Version m_Version_Develop = new Version(m_MainVersion + ".dev");
	static Version m_Version_Beta = new Version(m_MainVersion + ".beta");
	static Version m_Version_Final = new Version(m_MainVersion);

	public readonly static string LocalIP = "127.0.0.1";
	static string m_Matchmaking_DefaultServerIP_Develop = "159.253.143.198"; // Amsterdam #1  (Developer lobby server)
	//private static string m_Matchmaking_DefaultServerIP_Develop = "192.168.1.176"; 		// Amsterdam #1  (Developer lobby server)
	//private static string m_Matchmaking_DefaultServerIP_Develop = "192.168.1.151"; 		// Amsterdam #1  (Developer lobby server)

	// ============================ game server configuration ===========================

	public static int ServerPortMin = 8101;
	public static int ServerPortMax = 8999;

	// =============================== Lobby configuration ===============================
	// this a default port configuration - used by all involved sides (lobby itself, game server and clients)
	// The default values use to be used for testing purposes only. Normally the client application reads the lobby IP/port from 
	// cloud settings and the match-making (lobby) server gets it from command-line

	static int m_Matchmaking_DefaultServerPort_Develop = 30226;
	static int m_Matchmaking_DefaultServerPort_Beta = 14499;
	static int m_Matchmaking_DefaultServerPort_Final = 30226;

	public static int LobbyMaximumConnections = 15000;

	// =============================== Zone configuration ===============================
	// this configuration is used by Lobby application only

	public readonly static int DefaultZoneServerPort = 15001;
}
