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

public static class ChartBoost
{
	// Starts up ChartBoost and records an app install
	public static void init(string appId, string appSignature)
	{
	}

	// Caches an interstitial. Location is optional. Pass in null if you do not want to specify the location.
	public static void cacheInterstitial(string location)
	{
	}

	// Checks for a cached an interstitial. Location is optional. Pass in null if you do not want to specify the location.
	public static bool hasCachedInterstitial(string location)
	{
		return false;
	}

	// Shows an interstitial. Location is optional. Pass in null if you do not want to specify the location.
	public static void showInterstitial(string location)
	{
	}

	// Caches the more apps screen
	public static void cacheMoreApps()
	{
	}

	// Shows the more apps screen
	public static void showMoreApps()
	{
	}

	// Checks to see if the more apps screen is cached
	public static bool hasCachedMoreApps()
	{
		return false;
	}
}
