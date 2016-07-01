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

// ------
public class ResearchSupport : MonoBehaviour
{
	public static ResearchSupport Instance = null;
	public delegate void ResearchItemChangedDlgt();

	public GameObject m_ResearchIconWeaponPrefab;
	public GameObject m_ResearchIconRestPrefab;
	public GameObject m_UpgradeIconPrefab;
	public GameObject m_ResetTreePrefab;

	ResearchItemChangedDlgt m_ResearchItemChangedDlgt;
	List<ResearchItem> m_ResearchItems = new List<ResearchItem>();

	// -----
	void Awake()
	{
		Instance = this;
		/*m_ResearchIconWeaponPrefab 	= Resources.Load("Gui/ResearchIconWeapon") as GameObject;
		m_ResearchIconRestPrefab 	= Resources.Load("Gui/ResearchIconRest") as GameObject;
		m_UpgradeIconPrefab  		= Resources.Load("Gui/UpgradeIcon") as GameObject;
		m_ResetTreePrefab  	 		= Resources.Load("Gui/ResearchResetTree") as GameObject;
		/**/
	}

	// ------
	public ResearchItem[] GetItems()
	{
		return m_ResearchItems.ToArray();
	}

	// ------
	public PlayerPersistantInfo GetPPI()
	{
		return PPIManager.Instance.GetLocalPPI();
	}

	// ------
	public bool HasPlayerEnoughFunds(int price, bool isGold)
	{
		if (isGold)
			return GetPPI().Gold >= price;
		else
			return GetPPI().Money >= price;
	}

	// -----
	public void AnyResearchItemChanged()
	{
		m_ResearchItemChangedDlgt();
	}

	// -----
	public void AddAllConnectedItemGUIDs(ResearchItem item, List<int> insertAllItemsHere)
	{
		int upgrades = item.GetNumOfUpgrades();
		if (upgrades > 0)
		{
			for (int i = 0; i < upgrades; i++)
			{
				if (item.OwnsUpgrade(i))
					insertAllItemsHere.Add(item.GetUpgrade(i).GetGUID());
			}
		}
		if (!item.IsDefault())
			insertAllItemsHere.Add(item.GetGUID());
		//Debug.Log("Added: "+TextDatabase.instance[item.GetName()]);
	}

	// -----
	public int GetPriceBasedOnGuid(int guid)
	{
		WeaponSettings[] wSettings = WeaponSettingsManager.Instance.GetAll();
		foreach (WeaponSettings setting in wSettings)
		{
			if (setting.GetGUID() == guid)
				return setting.MoneyCost;
			foreach (WeaponSettings.Upgrade upg in setting.Upgrades)
			{
				if (upg.GetGUID() == guid)
					return upg.MoneyCost;
			}
		}

		ItemSettings[] iSettings = ItemSettingsManager.Instance.GetAll();
		foreach (ItemSettings setting in iSettings)
		{
			if (setting.GetGUID() == guid)
				return setting.MoneyCost;
		}

		PerkSettings[] pSettings = PerkSettingsManager.Instance.GetAll();
		foreach (PerkSettings setting in pSettings)
		{
			if (setting.GetGUID() == guid)
				return setting.MoneyCost;
		}

		UpgradeSettings[] uSettings = UpgradeSettingsManager.Instance.GetAll();
		foreach (UpgradeSettings setting in uSettings)
		{
			if (setting.GetGUID() == guid)
				return setting.MoneyCost;
		}

		return 0;
	}

	// -----
	public void RegisterResearchItem(ResearchItem item, ResearchItemChangedDlgt dlgt)
	{
		m_ResearchItems.Add(item);
		m_ResearchItemChangedDlgt += dlgt;
	}

	// -----
	public ResearchIcon GetNewResearchWeaponIcon()
	{
		GameObject prefab = Instantiate(m_ResearchIconWeaponPrefab) as GameObject;
		return prefab.GetComponent<ResearchIcon>();
	}

	// -----
	public ResearchIcon GetNewResearchRestIcon()
	{
		GameObject prefab = Instantiate(m_ResearchIconRestPrefab) as GameObject;
		return prefab.GetComponent<ResearchIcon>();
	}

	// -----
	public UpgradeIcon GetNewUpgradeIcon()
	{
		GameObject prefab = Instantiate(m_UpgradeIconPrefab) as GameObject;
		return prefab.GetComponent<UpgradeIcon>();
	}

	// -----
	public GUIBase_Button GetNewResetTreeButton()
	{
		GameObject prefab = Instantiate(m_ResetTreePrefab) as GameObject;
		return prefab.GetComponent<GUIBase_Button>();
	}
}
