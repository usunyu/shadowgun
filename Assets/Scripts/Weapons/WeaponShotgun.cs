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

[AddComponentMenu("Weapons/WeaponShotgun")]
public class WeaponShotgun : WeaponBase
{
	public int ProjectilesPerShot = 10;

	protected override void SpawnProjectile(Vector3 fromPos, Vector3 direction)
	{
		InitProjSettings.Agent = Owner;
		InitProjSettings.IgnoreTransform = Owner.Transform;

		for (int i = 0; i < ProjectilesPerShot; i++)
		{
			float dispersion = Owner.IsInCover ? Dispersion*0.8f : Dispersion;
			Temp.SetLookRotation(direction);
			Temp.eulerAngles = new Vector3(Temp.eulerAngles.x + Random.Range(-dispersion, dispersion),
										   Temp.eulerAngles.y + Random.Range(-dispersion, dispersion),
										   0);

			ProjectileManager.Instance.SpawnProjectile(Settings.ProjectileType, fromPos, Temp*Vector3.forward, InitProjSettings);
		}
	}

	/*// Use this for initialization
	void Start () {
	    
	}
	
	// Update is called once per frame
	void Update () {
	
	}*/
}
