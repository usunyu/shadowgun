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
using System.Collections.Generic;

[System.Serializable]
public abstract class AnimSet : MonoBehaviour
{
	protected class Randomizer
	{
		List<string> List1 = new List<string>();
		List<string> List2 = new List<string>();

		List<string> Current;

		public Randomizer()
		{
			Current = List1;
		}

		public void Add(string s)
		{
			List1.Add(s);
		}

		public string Get()
		{
//			if (Current.Count == 0)		//we prefer the exception to happen here, rather than somewhere else
//				return "";

			int r = Random.Range(0, Current.Count);
			string s = Current[r];

			if (Current.Count == 1) // posledni zustane v listu, aby se nam zase nahodou nevybral
			{
// a vymenime (pokud je celkovy pocet polozek vetsi nez 1)
				if (List2.Count > 0)
				{
					if (Current == List1)
						Current = List2;
					else
						Current = List1;
				}
			}
			else
			{
				//vyndame...
				Current.RemoveAt(r);

				// dame do druheho lisu..
				if (Current == List1)
					List2.Add(s);
				else
					List1.Add(s);
			}
			return s;
		}
	}

	public abstract string GetIdleAnim();

	public abstract string GetIdleActionAnim();

	public abstract string GetMoveAnim();

	public abstract string GetRotateAnim(E_RotationType rotationType);

	public abstract string GetDodgeAnim(E_StrafeDirection dir);

	public abstract string GetBlockAnim(E_BlockState block);

	public abstract string GetKnockdownAnim(E_KnockdownState knockdownState);

	public abstract string GetWeaponAnim(E_WeaponAction action);
	public abstract float GetWeaponAnimTime(E_WeaponType type, E_WeaponAction action);

	public abstract string GetInjuryAnim();

	public abstract string GetInjuryCritAnim();

	public abstract string GetDeathAnim();

	public abstract string GetTeleportAnim(E_TeleportAnim type);

	public enum E_CoverAnim
	{
		Enter,
		Leave,
		AimStart,
		AimLoop,
		AimEnd,
		Fire,
		JumpOver,
		JumpUp,
		LeaveLeft,
		LeaveRight,
	}

	public enum E_TeleportAnim
	{
		In,
		Out,
	}

	public abstract string GetCoverAnim(E_CoverAnim type, E_CoverPose pose, E_CoverDirection direction);

	public abstract string GetAimAnim(E_AimDirection direction, E_CoverPose pose, E_CoverDirection position);

	public abstract string GetRollAnim(E_Direction direction);

	public abstract string GetGadgetAnim(E_ItemID gadget, E_CoverPose pos, E_CoverDirection direction);

	public abstract string GetMeleeAnim(E_MeleeType type);
}
