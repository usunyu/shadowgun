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

#if CLOUD_TOOL_BUILD || UNITY_EDITOR
#define USING_MULTI_CLOUD_CONFIG
#endif // CLOUD_TOOL_BUILD || UNITY_EDITOR

using UnityEngine;

public enum MFGCloudService
{
	Community,
	Localhost
};

static class CloudConfiguration
{
	static MFGCloudService m_CurrentCloudService = MFGCloudService.Community;

	const string SERVICE_URL_BASE_COMMUNITY = "madfingerdeadzone-dev.appspot.com";
	const string SERVICE_URL_BASE_LOCAL = "localhost:8888";

	public static string DedicatedServerPasswordHash = "8A109A67ED4242AB678EF1C812A1690F46FD6CF7";

	public static bool ChangeCurrentCloudService(MFGCloudService cloud)
	{
#if USING_MULTI_CLOUD_CONFIG

		m_CurrentCloudService = cloud;
		return true;

#else

		Debug.LogError( "ChangeCurrentCloudService: service is not enabled." );
		return false;

#endif
	}

	public static string GetURLBase(MFGCloudService service)
	{
		switch (service)
		{
		case MFGCloudService.Community:
			return SERVICE_URL_BASE_COMMUNITY;

		case MFGCloudService.Localhost:
			return SERVICE_URL_BASE_LOCAL;
		}

		Debug.LogError("CloudServices.GetCurrentURLBase() : unsupported cloud service - " + m_CurrentCloudService);

		return string.Empty;
	}

	public static string GetCurrentURLBase()
	{
		return GetURLBase(GetCurrentCloudService());
	}

	public static MFGCloudService GetCurrentCloudService()
	{
		return m_CurrentCloudService;
	}

	/// <summary>
	/// Returns true for the retail or important service, false for develop, testing and local services
	/// </summary>
	public static bool IsServiceCritical(MFGCloudService service)
	{
		return service == MFGCloudService.Community;
	}

	public static string Salt
	{
		get { return "A98A8AC4A3DE7CB0E507A918B45443B4"; }	
	}


	public static string PublicRSAKeyAsXML
	{
		get { return "<RSAKeyValue><Modulus>AKYUcNITS8sthKN+wt6Bzlzcy/BZ4VViWugQK0m+W5Kvz1nzwrXowGRSywAzenHojQwF4vJ3Vp1mFmgPbKxh058=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>"; }
	}
}
