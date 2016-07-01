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

[System.Serializable]
public class WeaponConfiguration
{
	// Data definition ...
	[System.Serializable]
	public class WeaponProperty
	{
		public uint m_MinRank;
		public uint m_Cost;
	}
	[System.Serializable]
	public class DamageProperty : WeaponProperty
	{
		public float m_Damage;
	}
	[System.Serializable]
	public class PrecisionProperty : WeaponProperty
	{
		public float m_Precision;
	}
	[System.Serializable]
	public class AmmoClipProperty : WeaponProperty
	{
		public int m_MaxInClip;
		public int m_MaxTotal;
	}
	[System.Serializable]
	public class FireRateProperty : WeaponProperty
	{
		public float m_FireRate;
	}

	[System.Serializable]
	public class WeaponUpgrades<T>
	{
		public T[] m_Upgrades;
		public uint m_CurrentUpgrade = 0;
	}
	[System.Serializable]
	public class Damage : WeaponUpgrades<DamageProperty>
	{
	};
	[System.Serializable]
	public class Precision : WeaponUpgrades<PrecisionProperty>
	{
	};
	[System.Serializable]
	public class AmmoCount : WeaponUpgrades<AmmoClipProperty>
	{
	};
	[System.Serializable]
	public class FireRate : WeaponUpgrades<FireRateProperty>
	{
	};

	public string m_Name; // This name is visible and used only in editor.
	public GameObject m_Prefab;

	public Damage m_DamageSetup;
	public Precision m_PrecisionSetup;
	public AmmoCount m_AmmoCountSetup;
	public FireRate m_FireRateSetup;
}

enum E_MPWeapons
{
	SMG,
	ShotGun,
	GrenadeLauncher,
	RocketLauncher,
	PulseRifle,
	SniperRifle,
	End,
}

[System.Serializable]
class PlayerMPWeaponLevels
{
	[System.Serializable]
	public struct WeaponLevel
	{
		public uint Damage;
		public uint Precision;
		public uint AmmoClip;
		public uint FireRate;
	}

	[SerializeField] WeaponLevel[] PlayerSkils = new WeaponLevel[(int)E_MPWeapons.End];

	public WeaponLevel GetWeaponLevel(E_MPWeapons inWeaponClassID)
	{
		return PlayerSkils[(int)inWeaponClassID];
	}

	public void SetDamageLevel(E_MPWeapons inWeaponClassID, uint inValue)
	{
		PlayerSkils[(int)inWeaponClassID].Damage = inValue;
	}

	public void SetPrecisionLevel(E_MPWeapons inWeaponClassID, uint inValue)
	{
		PlayerSkils[(int)inWeaponClassID].Precision = inValue;
	}

	public void SetAmmoClipLevel(E_MPWeapons inWeaponClassID, uint inValue)
	{
		PlayerSkils[(int)inWeaponClassID].AmmoClip = inValue;
	}

	public void SetFireRateLevel(E_MPWeapons inWeaponClassID, uint inValue)
	{
		PlayerSkils[(int)inWeaponClassID].FireRate = inValue;
	}
}

// this file is here only for store weapoms comfiguration for ShadowGun MP.
[ExecuteInEditMode]
public class MPWeaponsRegister : MonoBehaviour
{
	public WeaponConfiguration[] m_WeaponConfiguration = new WeaponConfiguration[(int)E_MPWeapons.End];

	void Awake()
	{
		m_WeaponConfiguration[(int)E_MPWeapons.SMG].m_Name = "SMG";
		m_WeaponConfiguration[(int)E_MPWeapons.ShotGun].m_Name = "ShotGun";
		m_WeaponConfiguration[(int)E_MPWeapons.GrenadeLauncher].m_Name = "GrenadeLauncher";
		m_WeaponConfiguration[(int)E_MPWeapons.RocketLauncher].m_Name = "RocketLauncher";
		m_WeaponConfiguration[(int)E_MPWeapons.PulseRifle].m_Name = "PulseRifle";
		m_WeaponConfiguration[(int)E_MPWeapons.SniperRifle].m_Name = "SniperRifle-XX";
	}
}
