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

public class FNVHash
{
	public static int CalcFNVHash(string str)
	{
		uint hval = 0x811C9DC5;
		char[] chars = str.ToCharArray();

		foreach (char curr in chars)
		{
			hval += (hval << 1) + (hval << 4) + (hval << 7) + (hval << 8) + (hval << 24);

			hval ^= curr;
		}

		return (int)hval;
	}

	public static int CalcModFNVHash(string str)
	{
		char[] chars = str.ToCharArray();
		uint hash = 2166136261u;

		foreach (char curr in chars)
		{
			hash = (hash ^ curr)*16777619u;
		}

		hash += hash << 13;
		hash ^= hash >> 7;
		hash += hash << 3;
		hash ^= hash >> 17;
		hash += hash << 5;

		return (int)hash;
	}
};
