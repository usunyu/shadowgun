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
using System.Collections.Generic;
using System;

public class WebUtils
{
	public static Dictionary<string, string> GetQueryParams()
	{
		string src = Application.srcValue;

		if (src == ""
			|| src == null
			|| src.IndexOf("?") == -1
			|| src.IndexOf("?") == src.Length - 1)
			return new Dictionary<string, string>();

		src = src.Substring(src.IndexOf("?") + 1);

		return ParseQueryString(src);
	}

	public static Dictionary<string, string> ParseQueryString(string query)
	{
		Dictionary<string, string> urlParams = new Dictionary<string, string>();

		if (query == null || query.Length == 0)
			return urlParams;

		if (query.Contains("&") == false || query.Contains("=") == false)
			return urlParams;

		string[] paramList = query.Split('&');

		for (int i = 0; i < paramList.Length; i++)
		{
			string[] temp = paramList[i].Split('=');

			string key = WWW.UnEscapeURL(temp[0]);
			string val = WWW.UnEscapeURL(temp[1]);

			urlParams.Add(key, val);
		}

		return urlParams;
	}

	public static string QueryString(Dictionary<string, string> urlParams)
	{
		string parameters = "";
		bool first = true;

		foreach (KeyValuePair<string, string> kvp in urlParams)
		{
			parameters += (first ? "?" : "&") + WWW.EscapeURL(kvp.Key) + "=" + WWW.EscapeURL(kvp.Value);
			first = false;
		}

		return parameters;
	}

	public static string GetFilename()
	{
		string src = Application.srcValue;

		if (src.IndexOf("?") >= 0)
			return src.Substring(0, src.IndexOf("?"));

		return src;
	}

	public static string GetDomain()
	{
		if (Application.isEditor)
			return "http://localhost:8080";

		string url = Application.absoluteURL;

		string protocol = url.Substring(0, url.IndexOf("://") + 3);

		url = url.Substring(url.IndexOf("://") + 3);

		string domain = url.Substring(0, url.IndexOf("/"));

		return protocol + domain;
	}
}
