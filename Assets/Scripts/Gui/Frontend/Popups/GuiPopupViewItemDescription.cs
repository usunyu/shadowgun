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

// =====================================================================================================================
// =====================================================================================================================

public class PreviewItem
{
	string m_ItemDescription;
	int m_NumberOfParams = 0;
	E_WeaponID m_WeaponID;
	E_ItemID m_ItemID;

	public PreviewItem(E_WeaponID weaponID)
	{
		if (weaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(weaponID);
			m_ItemDescription = TextDatabase.instance[settings.Description];
			m_NumberOfParams = 4;
			m_WeaponID = weaponID;
		}
	}

	public PreviewItem(E_ItemID itemID)
	{
		if (itemID != E_ItemID.None)
		{
			ItemSettings settings = ItemSettingsManager.Instance.Get(itemID);
			m_ItemDescription = TextDatabase.instance[settings.Description];
			m_ItemID = itemID;

			if ((itemID == E_ItemID.SentryGun) || (itemID == E_ItemID.SentryGunII) || (itemID == E_ItemID.SentryGunRail) ||
				(itemID == E_ItemID.SentryGunRockets))
				m_NumberOfParams = 4;
			else if (itemID == E_ItemID.Mine)
				m_NumberOfParams = 3;
			else if ((itemID == E_ItemID.MineEMP) || (itemID == E_ItemID.MineEMPII) || (itemID == E_ItemID.GrenadeFrag) ||
					 (itemID == E_ItemID.GrenadeFragII) || (itemID == E_ItemID.BoxHealth) || (itemID == E_ItemID.BoxHealthII))
				m_NumberOfParams = 2;
			else if ((itemID == E_ItemID.GrenadeFlash) || (itemID == E_ItemID.GrenadeEMP) || (itemID == E_ItemID.GrenadeEMPII) ||
					 (itemID == E_ItemID.EnemyDetector) || (itemID == E_ItemID.EnemyDetectorII) || (itemID == E_ItemID.BoxAmmo) ||
					 (itemID == E_ItemID.BoxAmmoII))
				m_NumberOfParams = 1;
			else if (itemID == E_ItemID.BoosterAccuracy || itemID == E_ItemID.BoosterArmor || itemID == E_ItemID.BoosterSpeed ||
					 itemID == E_ItemID.BoosterDamage)
				m_NumberOfParams = 2;
			else
				m_NumberOfParams = 1;
		}
	}

	public PreviewItem(E_UpgradeID upgradeID)
	{
		if (upgradeID != E_UpgradeID.None)
		{
			UpgradeSettings settings = UpgradeSettingsManager.Instance.Get(upgradeID);
			m_ItemDescription = TextDatabase.instance[settings.Description];
		}
	}

	public PreviewItem(E_PerkID perkID)
	{
		if (perkID != E_PerkID.None)
		{
			PerkSettings settings = PerkSettingsManager.Instance.Get(perkID);
			m_ItemDescription = TextDatabase.instance[settings.Description];

			m_ItemDescription = m_ItemDescription.Replace("@1", Mathf.Round((settings.Modifier - 1.0f)*100).ToString());
			if ((perkID == E_PerkID.Sprint) || (perkID == E_PerkID.SprintII) || (perkID == E_PerkID.SprintIII))
				m_ItemDescription = m_ItemDescription.Replace("@2", Mathf.Round(settings.Timer).ToString());
		}
	}

	// ------
	public string GetDescription()
	{
		return m_ItemDescription;
	}

	public int GetNumOfParams()
	{
		return m_NumberOfParams;
	}

	// ------
	public int GetParamName(int paramIndex)
	{
		// -----------------------------
		if (m_WeaponID != E_WeaponID.None)
		{
			switch (paramIndex)
			{
			case 0:
				return 0111020;
			case 1:
				return 0111030;
			case 2:
				return 0111040;
			case 3:
				return 0111050;
			default:
				Error("unknown index!");
				break;
			}
		}
		// -----------------------------
		if (m_ItemID != E_ItemID.None)
		{
			if ((m_ItemID == E_ItemID.SentryGun) || (m_ItemID == E_ItemID.SentryGunII) || (m_ItemID == E_ItemID.SentryGunRail) ||
				(m_ItemID == E_ItemID.SentryGunRockets))
			{
				if (paramIndex == 0)
					return 0111020;
				else if (paramIndex == 1)
					return 0111030;
				else if (paramIndex == 2)
					return 0111040;
				else if (paramIndex == 3)
					return 0111070;
				else
					Error("unknown index!");
			}
			else if (m_ItemID == E_ItemID.Mine)
			{
				if (paramIndex == 0)
					return 0111020;
				else if (paramIndex == 1)
					return 0111070;
				else if (paramIndex == 2)
					return 0111090;
				else if (paramIndex == 3)
					return 0111060;
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.MineEMP) || (m_ItemID == E_ItemID.MineEMPII))
			{
				if (paramIndex == 0)
					return 0111070;
				else if (paramIndex == 1)
					return 0111090;
				else if (paramIndex == 2)
					return 0111060;
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.GrenadeFrag) || (m_ItemID == E_ItemID.GrenadeFragII))
			{
				if (paramIndex == 0)
					return 0111020;
				else if (paramIndex == 1)
					return 0111070;
				else if (paramIndex == 2)
					return 0111060;
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.GrenadeFlash) || (m_ItemID == E_ItemID.GrenadeEMP) || (m_ItemID == E_ItemID.GrenadeEMPII))
			{
				if (paramIndex == 0)
					return 0111070;
				else if (paramIndex == 1)
					return 0111060;
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.BoxHealth) || (m_ItemID == E_ItemID.BoxHealthII))
			{
				if (paramIndex == 0)
					return 0111100;
				else if (paramIndex == 1)
					return 0111080;
				else if (paramIndex == 2)
					return 0111060;
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.BoxAmmo) || (m_ItemID == E_ItemID.BoxAmmoII))
			{
				if (paramIndex == 0)
					return 0111080;
				else if (paramIndex == 1)
					return 0111060;
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.EnemyDetector) || (m_ItemID == E_ItemID.EnemyDetectorII))
			{
				if (paramIndex == 0)
					return 0111070;
				else if (paramIndex == 1)
					return 0111060;
				else
					Error("unknown index!");
			}
			else if (m_ItemID == E_ItemID.BoosterAccuracy)
			{
				if (paramIndex == 0)
					return 00111080; ///duration
				else if (paramIndex == 1)
					return 00207078; //boost
				else
					Error("unknown index!");
			}
			else if (m_ItemID == E_ItemID.BoosterSpeed)
			{
				if (paramIndex == 0)
					return 00111080; ///duration
				else if (paramIndex == 1)
					return 00207072;
				else
					Error("unknown index!");
			}
			else if (m_ItemID == E_ItemID.BoosterDamage)
			{
				if (paramIndex == 0)
					return 00111080; ///duration
				else if (paramIndex == 1)
					return 00207074; //boost
				else
					Error("unknown index!");
			}
			else if (m_ItemID == E_ItemID.BoosterArmor)
			{
				if (paramIndex == 0)
					return 00111080; ///duration
				else if (paramIndex == 1)
					return 00111020; //boost
				else
					Error("unknown index!");
			}
			else if (m_ItemID == E_ItemID.BoosterInvicible)
			{
				if (paramIndex == 0)
					return 00111080; ///duration
				else
					Error("unknown index!");
			}
			else /**/
			{
				if (paramIndex == 0)
					return 0111060;
				else
					Error("unknown index!");
			}
		}
		return 0;
	}

	public string GetParamValue(int paramIndex)
	{
		// -----------------------------
		if (m_WeaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(m_WeaponID);
			switch (paramIndex)
			{
			case 0:
				float damage = Mathf.Round(10*settings.BaseData.Damage*DamageModificator())*0.1f;
				if (settings.WeaponType == E_WeaponType.Shotgun)
				{
					WeaponShotgun shotgun = settings.Model.GetComponent<WeaponShotgun>();
					return shotgun.ProjectilesPerShot.ToString() + "x " + damage.ToString();
				}
				else
					return damage.ToString();
			case 1:
				return (Mathf.Round(10.0f/(settings.BaseData.FireTime*FireTimeModificator()))*0.1f).ToString();
			case 2:
				return
								Mathf.Round(100*(ResearchItem.MAX_DISPERSION - (settings.BaseData.Dispersion*DispersionModificator()))/ResearchItem.MAX_DISPERSION)
									 .ToString();
			case 3:
				return Mathf.CeilToInt(settings.BaseData.MaxAmmoInClip*ClipSizeModificator()).ToString();
			default:
				Error("unknown index!");
				break;
			}
		}
		// -----------------------------
		else if (m_ItemID != E_ItemID.None)
		{
			ItemSettings settings = ItemSettingsManager.Instance.Get(m_ItemID);
			//----
			if ((m_ItemID == E_ItemID.SentryGun) || (m_ItemID == E_ItemID.SentryGunII) || (m_ItemID == E_ItemID.SentryGunRail) ||
				(m_ItemID == E_ItemID.SentryGunRockets))
			{
				SentryGun gun = settings.SpawnObject.GetComponent<SentryGun>();
				switch (paramIndex)
				{
				case 0:
					return gun.m_WpnSettings.m_Damage.ToString();
				case 1:
					return Mathf.Round(1.0f/gun.m_WpnSettings.m_FireRate).ToString();
				case 2:
					return Mathf.Round(100*(ResearchItem.MAX_DISPERSION - gun.m_WpnSettings.m_Dispersion)/ResearchItem.MAX_DISPERSION).ToString();
				case 3:
					return gun.m_WpnSettings.m_RangeMaximal.ToString();
				default:
					Error("unknown index!");
					break;
				}
			}
			else if (m_ItemID == E_ItemID.Mine)
			{
				Mine obj = settings.SpawnObject.GetComponent<Mine>();
				if (paramIndex == 0)
					return obj.m_MaxDamage.ToString();
				else if (paramIndex == 1)
					return obj.m_DamageRadius.ToString();
				else if (paramIndex == 2)
					return obj.m_DetectionDistance.ToString();
				else if (paramIndex == 3)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.MineEMP) || (m_ItemID == E_ItemID.MineEMPII))
			{
				Mine obj = settings.SpawnObject.GetComponent<Mine>();
				if (paramIndex == 0)
					return obj.m_DamageRadius.ToString();
				else if (paramIndex == 1)
					return obj.m_DetectionDistance.ToString();
				else if (paramIndex == 2)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.GrenadeFrag) || (m_ItemID == E_ItemID.GrenadeFragII))
			{
				GrenadeFragProjectile obj = settings.SpawnObject.GetComponent<GrenadeFragProjectile>();

				if (paramIndex == 0)
					return obj.Damage.ToString();
				else if (paramIndex == 1)
					return obj.Radius.ToString();
				else if (paramIndex == 2)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if (m_ItemID == E_ItemID.GrenadeFlash)
			{
				FlashBangProjectile obj = settings.SpawnObject.GetComponent<FlashBangProjectile>();

				if (paramIndex == 0)
					return obj.Radius.ToString();
				else if (paramIndex == 1)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.GrenadeEMP) || (m_ItemID == E_ItemID.GrenadeEMPII))
			{
				EMPGrenadeProjectile obj = settings.SpawnObject.GetComponent<EMPGrenadeProjectile>();

				if (paramIndex == 0)
					return obj.Radius.ToString();
				else if (paramIndex == 1)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.BoxHealth) || (m_ItemID == E_ItemID.BoxHealthII))
			{
				MedKit obj = settings.SpawnObject.GetComponent<MedKit>();

				if (paramIndex == 0)
					return obj.HealRate.ToString();
				if (paramIndex == 1)
					return obj.Timer.ToString();
				else if (paramIndex == 2)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.BoxAmmo) || (m_ItemID == E_ItemID.BoxAmmoII))
			{
				AmmoKit obj = settings.SpawnObject.GetComponent<AmmoKit>();

				if (paramIndex == 0)
					return obj.Timer.ToString();
				else if (paramIndex == 1)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if ((m_ItemID == E_ItemID.EnemyDetector) || (m_ItemID == E_ItemID.EnemyDetectorII))
			{
				if (paramIndex == 0)
					return settings.Range.ToString();
				else if (paramIndex == 1)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if (m_ItemID == E_ItemID.BoosterAccuracy || m_ItemID == E_ItemID.BoosterSpeed || m_ItemID == E_ItemID.BoosterDamage ||
					 m_ItemID == E_ItemID.BoosterInvicible || m_ItemID == E_ItemID.BoosterArmor)
			{
				if (paramIndex == 0)
					return settings.BoostTimer.ToString();
				else if (paramIndex == 1)
				{
					int boostMod = Mathf.CeilToInt(settings.BoostModifier*100);
					if (boostMod > 1.0f)
						return (boostMod - 100).ToString() + "%";
					else
						return "+" + boostMod.ToString() + "%";
				}
				else
					Error("unknown index!");
			}
			else /**/
			{
				switch (paramIndex)
				{
				case 0:
					return settings.Count.ToString();
				default:
					Error("unknown index!");
					break;
				}
			}
		}

		return "";
	}

	float GetModificator(E_WeaponUpgradeID upgradeID, bool checkOwnership)
	{
		float modif = 1.0f;
		if (m_WeaponID == E_WeaponID.None)
			return 0;

		int upgradeNum = GetNumOfUpgrades();
		for (int i = 0; i < upgradeNum; i++)
		{
			WeaponSettings.Upgrade upgrade = GetUpgrade(i);
			if ((upgrade.ID == upgradeID) && (!checkOwnership || OwnsUpgrade(i)))
			{
				modif += upgrade.Modifier;
			}
		}
		return modif;
	}

	// ------
	float DamageModificator(bool checkOwnership = true)
	{
		return GetModificator(E_WeaponUpgradeID.Damage, checkOwnership);
	}

	// ------
	float DispersionModificator(bool checkOwnership = true)
	{
		return GetModificator(E_WeaponUpgradeID.Dispersion, checkOwnership);
	}

	// ------
	float FireTimeModificator(bool checkOwnership = true)
	{
		return GetModificator(E_WeaponUpgradeID.FireTime, checkOwnership);
	}

	// ------
	float ClipSizeModificator(bool checkOwnership = true)
	{
		return GetModificator(E_WeaponUpgradeID.ClipSize, checkOwnership);
	}

	// ------
	int GetNumOfUpgrades()
	{
		int count = 0;
		if (m_WeaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(m_WeaponID);

			foreach (WeaponSettings.Upgrade upg in settings.Upgrades)
			{
				if (!upg.Disabled)
					++count;
			}
			return count;
		}
		return count;
	}

	// ------
	public WeaponSettings.Upgrade GetUpgrade(int index)
	{
		if (m_WeaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(m_WeaponID);

			int index2 = 0;
			for (int i = 0; i < settings.Upgrades.Count; i++)
			{
				if (!settings.Upgrades[i].Disabled)
				{
					if (index == index2)
						return settings.Upgrades[i];
					else
						++index2;
				}
			}
		}

		return null;
	}

	public bool OwnsUpgrade(int index)
	{
		WeaponSettings.Upgrade upgrade = GetUpgrade(index);

		if (upgrade != null)
		{
			return ResearchSupport.Instance.GetPPI().InventoryList.OwnsWeaponUpgrade(m_WeaponID, upgrade.ID);
		}

		return false;
	}

	// ------
	void Error(string msg)
	{
		Debug.LogWarning("Error: VieItemDescription: " + msg);
	}
}

public class GuiPopupViewItemDescription
{
	// -----
	class Param
	{
		public GUIBase_Widget Parent;
		public GUIBase_Label Name;
		public GUIBase_Label Value;
		public Vector3 OrigPos;
	}

	PreviewItem m_ResearchItem;
	Param[] m_Params = new Param[ResearchItem.MAX_PARAMS];
	GUIBase_TextArea m_Description;
	GUIBase_TextArea m_Key;
	bool m_ShowOnRight;

	GUIBase_Layout m_Layout;

	public string Description
	{
		get { return m_Description.text; }
		set { m_Description.SetNewText(value); }
	}

	public string Key
	{
		get { return m_Key.text; }
		set { m_Key.SetNewText(value); }
	}

	// -----
	public void Init(GUIBase_Layout layout)
	{
		m_Layout = layout;

		for (int i = 0; i < ResearchItem.MAX_PARAMS; i++)
		{
			m_Params[i] = new Param();
			m_Params[i].Parent = GuiBaseUtils.GetChild<GUIBase_Widget>(m_Layout, "Param" + (i + 1));
			m_Params[i].OrigPos = m_Params[i].Parent.transform.localPosition;
			m_Params[i].Name = GuiBaseUtils.GetChildLabel(m_Params[i].Parent, "ParamName");
			m_Params[i].Value = GuiBaseUtils.GetChildLabel(m_Params[i].Parent, "ParamValue");
		}
		m_Description = GuiBaseUtils.GetChild<GUIBase_TextArea>(m_Layout, "Description");
		m_Key = GuiBaseUtils.GetChild<GUIBase_TextArea>(m_Layout, "Key");
	}

	public void Show(bool show)
	{
		bool mouseOnLeft = Input.mousePosition.x < (m_ShowOnRight ? Screen.width/5*3 : Screen.width/5*2);
		if (!m_ShowOnRight && mouseOnLeft)
		{
			Vector3 position = m_Layout.transform.position;
			position.x = Screen.width - m_Layout.transform.Find("Background").transform.position.x*2;
			m_Layout.transform.position = position;
			m_ShowOnRight = true;
			m_Layout.SetModify(true, false);
		}
		else if (m_ShowOnRight && !mouseOnLeft)
		{
			m_Layout.transform.position = new Vector3(0, m_Layout.transform.position.y, m_Layout.transform.position.z);
			m_ShowOnRight = false;
			m_Layout.SetModify(true, false);
		}
		m_Layout.Show(show);
	}

	// -----
	public void SetItem(PreviewItem item)
	{
		m_ResearchItem = item;

		m_Description.SetNewText(m_ResearchItem.GetDescription());

		int maxParams = item.GetNumOfParams();
		for (int i = 0; i < maxParams; i++)
		{
			m_Params[i].Name.Uppercase = true;
			m_Params[i].Name.SetNewText(item.GetParamName(i));
			m_Params[i].Value.SetNewText(item.GetParamValue(i));
			m_Params[i].Value.Widget.Color = Color.white;
			ShowWidget(m_Params[i].Parent, true);
		}

		for (int i = maxParams; i < ResearchItem.MAX_PARAMS; i++)
		{
			ShowWidget(m_Params[i].Parent, false);
		}
	}

	// ------
	void ShowWidget(GUIBase_Widget widget, bool state)
	{
		if (widget != null && widget.Visible != state)
		{
			widget.ShowImmediate(state, true);
		}
	}
}
