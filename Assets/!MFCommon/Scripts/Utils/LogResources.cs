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

public class LogResources : MonoBehaviour
{
	// Use this for initialization
	void Start()
	{
		Invoke("Log", 1);
	}

	// Update is called once per frame
	void Update()
	{
	}

	void Log()
	{
		Object[] resources = Resources.FindObjectsOfTypeAll(typeof (Texture));

		Debug.Log("number of textures " + resources.Length);
		for (int i = 0; i < resources.Length; i++)
		{
			Debug.Log(resources[i].name + " " + (resources[i] as Texture).width + "x" + (resources[i] as Texture).height);
		}

		Object[] objects = Resources.FindObjectsOfTypeAll(typeof (Object));

		Debug.Log("number of objects " + resources.Length);
		for (int i = 0; i < resources.Length; i++)
		{
			Debug.Log(objects[i].name);
		}
	}
}
