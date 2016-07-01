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

public class CameraTargetAttributes : MonoBehaviour
{
	// This script goes on any GameObject in your scene that you will track with the camera.
	// It'll help customize the camera tracking to your specific object to polish your game.

	// See the GetGoalPosition () function in CameraScrolling.js for an explanation of these variables.
	public enum E_HeightType
	{
		E_CALM,
		E_ENEMIES_ARE_FAR,
		E_ENEMIES_ARE_NEAR,
	}
	public float[] HeightOffset = {11, 9, 7};

	public E_HeightType HeightType = E_HeightType.E_CALM;

	public float DistanceModifier = 1.0f;
	public float VelocityLookAhead = 0.15f;
	public Vector2 MaxLookAhead = new Vector2(3.0f, 3.0f);
	public float HieghtSpeed = 1;

	public float CurrentHeightOffset;

	void Awake()
	{
	}

	void Start()
	{
		CurrentHeightOffset = HeightOffset[(int)E_HeightType.E_CALM];
	}

	void Update()
	{
		float optimal = HeightOffset[(int)HeightType];
		if (CurrentHeightOffset > optimal)
		{
			CurrentHeightOffset -= HieghtSpeed*Time.deltaTime;
			if (CurrentHeightOffset < optimal)
				CurrentHeightOffset = optimal;
		}
		else if (CurrentHeightOffset < optimal)
		{
			CurrentHeightOffset += HieghtSpeed*Time.deltaTime;
			if (CurrentHeightOffset > optimal)
				CurrentHeightOffset = optimal;
		}
	}

	void ReSpawn(Transform spawnTransform)
	{
		HeightType = E_HeightType.E_CALM;
		CurrentHeightOffset = HeightOffset[(int)E_HeightType.E_CALM];
	}
}
