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
using System.Text;
using System.Security.Cryptography;

namespace utils
{
	public class CryptoUtils
	{
		static SHA1CryptoServiceProvider ms_SHA1Provider = new SHA1CryptoServiceProvider();
		static MD5CryptoServiceProvider ms_MD5Provider = new MD5CryptoServiceProvider();

		public static string CalcSHA1Hash(string input)
		{
			byte[] data = Encoding.UTF8.GetBytes(input);
			byte[] hash = ms_SHA1Provider.ComputeHash(data);

			return BitConverter.ToString(hash).Replace("-", "");
		}

		public static string CalcMD5Hash(string input)
		{
			byte[] data = Encoding.UTF8.GetBytes(input);
			byte[] hash = ms_MD5Provider.ComputeHash(data);

			return BitConverter.ToString(hash).Replace("-", "");
		}

		public static byte[] CalcMD5HashAsBytes(string input)
		{
			byte[] data = Encoding.UTF8.GetBytes(input);
			byte[] hash = ms_MD5Provider.ComputeHash(data);

			return hash;
		}
	}
}
