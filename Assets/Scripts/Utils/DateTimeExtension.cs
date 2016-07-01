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
using System.Globalization;
using LitJson;
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;
using GeoRegion = NetUtils.GeoRegion;

public static class CloudDateTime
{
	struct Data
	{
		public DateTime CloudSyncTime;
		public DateTime ClientSyncTime;

		public TimeSpan Offset
		{
			get { return CloudSyncTime - ClientSyncTime; }
		}
	}
	static Data m_Data;

	public static DateTime UtcNow
	{
		get { return DateTime.UtcNow + m_Data.Offset; }
	}

	public static void Load(string json)
	{
		DateTime clientTime = DateTime.UtcNow;
		DateTime cloudTime = clientTime;
		try
		{
			JsonData data = JsonMapper.ToObject(json);

			DateTime date;
			if (data.HasValue("time") == true && DateTime.TryParse((string)data["time"], out date) == true)
			{
				cloudTime = date;
			}
		}
		catch
		{
		}
		finally
		{
			m_Data.CloudSyncTime = cloudTime;
			m_Data.ClientSyncTime = clientTime;
		}
	}
}

public static class DateTimeExtension
{
	public static string ToLongRegionalString(this DateTime date)
	{
		GeoRegion region = CloudUser.instance.region;

		int pattern = 8000012;
		switch (TextDatabase.GetLanguage())
		{
		case SystemLanguage.English:
			pattern = region == GeoRegion.Europe ? 8000000 : 8000001;
			break;
		case SystemLanguage.German:
			pattern = 8000002;
			break;
		case SystemLanguage.French:
			pattern = region == GeoRegion.Europe ? 8000003 : 8000004;
			break;
		case SystemLanguage.Italian:
			pattern = 8000005;
			break;
		case SystemLanguage.Spanish:
			pattern = region == GeoRegion.Europe ? 8000006 : 8000007;
			break;
		case SystemLanguage.Russian:
			pattern = 8000008;
			break;
		case SystemLanguage.Japanese:
			pattern = 8000009;
			break;
		case SystemLanguage.Chinese:
			pattern = 8000010;
			break;
		case SystemLanguage.Korean:
			pattern = 8000011;
			break;
		default:
			break;
		}

		return date.ToString(TextDatabase.instance[pattern]);
	}

	public static string ToShortRegionalString(this DateTime date)
	{
		GeoRegion region = CloudUser.instance.region;

		int pattern = 8000112;
		switch (TextDatabase.GetLanguage())
		{
		case SystemLanguage.English:
			pattern = region == GeoRegion.Europe ? 8000100 : 8000101;
			break;
		case SystemLanguage.German:
			pattern = 8000102;
			break;
		case SystemLanguage.French:
			pattern = region == GeoRegion.Europe ? 8000103 : 8000104;
			break;
		case SystemLanguage.Italian:
			pattern = 8000105;
			break;
		case SystemLanguage.Spanish:
			pattern = region == GeoRegion.Europe ? 8000106 : 8000107;
			break;
		case SystemLanguage.Russian:
			pattern = 8000108;
			break;
		case SystemLanguage.Japanese:
			pattern = 8000109;
			break;
		case SystemLanguage.Chinese:
			pattern = 8000110;
			break;
		case SystemLanguage.Korean:
			pattern = 8000111;
			break;
		default:
			break;
		}

		return date.ToString(TextDatabase.instance[pattern]);
	}
}
