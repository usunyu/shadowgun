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

public class PPIBankData
{
	// last "major" the player reached and was prompt to post it on Facebook/Twitter
	public int LastMajorRank = -1;

	// list of Facebook-site-IDs the player "likes" and was already rewarded for it
	public List<string> FacebookSites = new List<string>();

	// list of Twitter-site-IDs the player "follows" and was already rewarded for it
	public List<string> TwitterSites = new List<string>();
}
