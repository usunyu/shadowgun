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

public class BuildInfo : MonoBehaviour
{
	static BuildInfo m_Instance;

	public static BuildInfo Instance
	{
		get
		{
			if (m_Instance == null)
			{
				GameObject go = GameObject.Find("/BuildInfo");
				m_Instance = go != null ? go.GetComponent<BuildInfo>() : null;
			}
			return m_Instance;
		}
	}

	public enum Stage
	{
		Release,
		Beta,
		Alpha,
		Development
	}

	public static string ToString(Stage stage)
	{
		switch (stage)
		{
		case Stage.Development:
			return "d";
		case Stage.Alpha:
			return "a";
		case Stage.Beta:
			return "b";
		case Stage.Release:
			return "";
		default:
			throw new System.IndexOutOfRangeException();
		}
	}

	public static Stage FromString(string stage)
	{
		switch (stage)
		{
		case "d":
			return Stage.Development;
		case "a":
			return Stage.Alpha;
		case "b":
			return Stage.Beta;
		default:
			return Stage.Release;
		}
	}

	[System.Serializable]
	public class VersionInfo
	{
		public int Major;
		public int Minor;
		public int Build;
		public int Revision;
		public int Code;
		public Stage Stage;

		public override string ToString()
		{
			return string.Format("{0}.{1}.{2}{3}.{4}", Major, Minor, Build, BuildInfo.ToString(Stage), Revision);
		}

		public string Version()
		{
			return string.Format("{0}.{1}.{2}", Major, Minor, Build);
		}
	}

	[System.Serializable]
	public class DateInfo
	{
		public int Year;
		public int Month;
		public int Day;
		public int Hour;
		public int Minute;
		public int Second;

		public override string ToString()
		{
			return new System.DateTime(Year, Month, Day, Hour, Minute, Second, System.DateTimeKind.Utc).ToString();
		}
	}

	[SerializeField] VersionInfo versionInfo;
	[SerializeField] DateInfo dateInfo;

	public static VersionInfo Version
	{
		get { return Instance != null ? Instance.versionInfo : new VersionInfo(); }
	}

	public static DateInfo Date
	{
		get { return Instance != null ? Instance.dateInfo : new DateInfo(); }
	}

	public static bool DrawVersionInfo = true;

#if UNITY_EDITOR
	public void Init(VersionInfo versionInfo, DateInfo dateInfo)
	{
		this.versionInfo = versionInfo;
		this.dateInfo = dateInfo;

		//Debug.Log(">>>> BuildInfo.Init() :: version="+Version+", date="+Date);
	}
#else
	private static bool infoPrinted = false;
	
	public void Awake()
	{
		//Debug.Log(">>>> BuildInfo.Awake()");
		
		// we should pring build and system info just for the first time
		if (infoPrinted == false)
		{
			Print();
			infoPrinted = true;
		}
	}
#endif

	public void Start()
	{
		if (Instance != this)
		{
			GameObject.Destroy(this);
		}
	}

#if UNITY_EDITOR
	void OnGUI()
	{
		if (Debug.isDebugBuild == true && DrawVersionInfo == true)
		{
			GUIContent content = new GUIContent("v" + Version);
			GUIStyle styleShadow = new GUIStyle(GUIStyle.none);
			GUIStyle styleWhite = new GUIStyle(GUIStyle.none);

			styleShadow.normal.textColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
			styleWhite.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

			Vector2 size = styleWhite.CalcSize(content);

			GUI.Label(new Rect(16, Screen.height - size.y - 9, size.x, size.y), content, styleShadow);
			GUI.Label(new Rect(15, Screen.height - size.y - 10, size.x, size.y), content, styleWhite);
		}
	}
#endif

	public string FormatBuildInfo()
	{
		string[] lines =
		{
			"BuildInfo :",
			"   BuildInfo.Version : " + Version,
#if UNITY_ANDROID
			"   BuildInfo.Code    : " + Version.Code,
#endif
			"   BuildInfo.Date    : " + Date
		};
		return string.Join(System.Environment.NewLine, lines);
	}

	public string FormatSystemInfo()
	{
		string[] lines =
		{
			"SystemInfo :",
			"   SystemInfo.operatingSystem        : " + SystemInfo.operatingSystem,
			"   SystemInfo.processorType          : " + SystemInfo.processorType,
			"   SystemInfo.processorCount         : " + SystemInfo.processorCount,
			"   SystemInfo.systemMemorySize       : " + SystemInfo.systemMemorySize,
			"   SystemInfo.graphicsMemorySize     : " + SystemInfo.graphicsMemorySize,
			"   SystemInfo.graphicsDeviceName     : " + SystemInfo.graphicsDeviceName,
			"   SystemInfo.graphicsDeviceVendor   : " + SystemInfo.graphicsDeviceVendor,
			"   SystemInfo.graphicsDeviceID       : " + SystemInfo.graphicsDeviceID,
			"   SystemInfo.graphicsDeviceVendorID : " + SystemInfo.graphicsDeviceVendorID,
			"   SystemInfo.graphicsDeviceVersion  : " + SystemInfo.graphicsDeviceVersion,
			"   SystemInfo.graphicsShaderLevel    : " + SystemInfo.graphicsShaderLevel,
			"   SystemInfo.supportsShadows        : " + SystemInfo.supportsShadows,
			"   SystemInfo.supportsRenderTextures : " + SystemInfo.supportsRenderTextures,
			"   SystemInfo.supportsImageEffects   : " + SystemInfo.supportsImageEffects,
#if UNITY_STANDALONE_WIN
			"   SystemInfo.deviceUniqueIdentifier : not queried on windows platform due to performance problem with SystemInfo.deviceUniqueIdentifier", // + SystemInfo.deviceUniqueIdentifier,
#elif UNITY_IPHONE
			"   SystemInfo.deviceUniqueIdentifier : N/A", // We can't access UDID on iOS due to new Apple restrictions -> rewrite this to get UIDevice.identifierForVendor
#else
			"   SystemInfo.deviceUniqueIdentifier : " + SystemInfo.deviceUniqueIdentifier,
#endif
			"   SystemInfo.deviceName             : " + SystemInfo.deviceName,
			"   SystemInfo.deviceModel            : " + SystemInfo.deviceModel
		};
		return string.Join(System.Environment.NewLine, lines);
	}

	public void Print()
	{
		{
			// BuildInfo
			string[] lines =
			{
				"----------------------------------------------------------------------",
				FormatBuildInfo()
			};
			print(string.Join(System.Environment.NewLine, lines));
		}

		{
			// SystemInfo
			string[] lines =
			{
				FormatSystemInfo(),
				"----------------------------------------------------------------------",
			};
			print(string.Join(System.Environment.NewLine, lines));
		}
	}
}
