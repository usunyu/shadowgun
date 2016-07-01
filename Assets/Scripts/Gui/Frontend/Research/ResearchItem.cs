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

public class ResearchItem : MonoBehaviour, IResearchItem
{
	public const int MAX_PARAMS = 4;
	public const int MAX_UPGRADES = 4;

	public E_WeaponID weaponID;
	public E_ItemID itemID;
	public E_UpgradeID upgradeID;
	public E_PerkID perkID;
	public ResearchItem[] children = null;

	public int m_GuiPageIndex { get; set; }

	IViewOwner m_Owner;
	ResearchIcon m_Icon;
	UpgradeIcon[] m_UpgradeIcons = null;
	ResearchItem m_Parent = null;
	ResearchState m_State;
	bool m_StateDirty = true;
	bool m_IsVisible = false;

	int m_ItemName = 0;
	int m_ItemDescription = 0;
	int m_Price = 0;
	bool m_IsPriceGold = false;
	GUIBase_Widget m_Image = null;
	int m_GUID;
	string m_CantBuyExplanation = "";
	int m_RequiredRank = 0;

	public const float MAX_DISPERSION = 6.8f;

	// ------
	public int GetName()
	{
		return m_ItemName;
	}

	public int GetRequiredRank()
	{
		return m_RequiredRank;
	}

	// ------
	public string GetDescription()
	{
		string text;
		text = TextDatabase.instance[m_ItemDescription];
		if (perkID != E_PerkID.None)
		{
			PerkSettings settings = PerkSettingsManager.Instance.Get(perkID);
			text = text.Replace("@1", Mathf.Round((settings.Modifier - 1.0f)*100).ToString());
			if ((perkID == E_PerkID.Sprint) || (perkID == E_PerkID.SprintII) || (perkID == E_PerkID.SprintIII))
				text = text.Replace("@2", Mathf.Round(settings.Timer).ToString());
		}

		return text;
	}

	// ------
	public GUIBase_Widget GetImage()
	{
		return m_Image;
	}

	// ------
	public int GetPrice(out bool isGold)
	{
		isGold = m_IsPriceGold;
		return m_Price;
	}

	// ------
	public int GetGUID()
	{
		return m_GUID;
	}

	// ------
	public string GetCantBuyExplanation()
	{
		return m_CantBuyExplanation;
	}

	// ------
	public bool IsDefault()
	{
		if (weaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(weaponID);
			return settings.IsDefault();
		}
		else if (itemID != E_ItemID.None)
		{
			ItemSettings settings = ItemSettingsManager.Instance.Get(itemID);
			return settings.IsDefault();
		}
		else if (upgradeID != E_UpgradeID.None)
		{
			UpgradeSettings settings = UpgradeSettingsManager.Instance.Get(upgradeID);
			return settings.IsDefault();
		}
		else if (perkID != E_PerkID.None)
		{
			PerkSettings settings = PerkSettingsManager.Instance.Get(perkID);
			return settings.IsDefault();
		}

		return false;
	}

	// ------
	public int GetNumOfParams()
	{
		if (weaponID != E_WeaponID.None)
			return 4;
		else if (itemID != E_ItemID.None)
		{
			if ((itemID == E_ItemID.SentryGun) || (itemID == E_ItemID.SentryGunII) || (itemID == E_ItemID.SentryGunRail) ||
				(itemID == E_ItemID.SentryGunRockets))
				return 4;
			else if (itemID == E_ItemID.Mine)
				return 4;
			else if ((itemID == E_ItemID.MineEMP) || (itemID == E_ItemID.MineEMPII))
				return 3;
			else if ((itemID == E_ItemID.GrenadeFrag) || (itemID == E_ItemID.GrenadeFragII))
				return 3;
			else if ((itemID == E_ItemID.GrenadeFlash) || (itemID == E_ItemID.GrenadeEMP) || (itemID == E_ItemID.GrenadeEMPII))
				return 2;
			else if ((itemID == E_ItemID.EnemyDetector) || (itemID == E_ItemID.EnemyDetectorII))
				return 2;
			else if ((itemID == E_ItemID.BoxHealth) || (itemID == E_ItemID.BoxHealthII))
				return 3;
			else if ((itemID == E_ItemID.BoxAmmo) || (itemID == E_ItemID.BoxAmmoII))
				return 2;
			else
				return 1;
		}

		return 0;
	}

	// ------
	public int GetParamName(int paramIndex)
	{
		// -----------------------------
		if (weaponID != E_WeaponID.None)
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
		if (itemID != E_ItemID.None)
		{
			if ((itemID == E_ItemID.SentryGun) || (itemID == E_ItemID.SentryGunII) || (itemID == E_ItemID.SentryGunRail) ||
				(itemID == E_ItemID.SentryGunRockets))
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
			else if (itemID == E_ItemID.Mine)
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
			else if ((itemID == E_ItemID.MineEMP) || (itemID == E_ItemID.MineEMPII))
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
			else if ((itemID == E_ItemID.GrenadeFrag) || (itemID == E_ItemID.GrenadeFragII))
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
			else if ((itemID == E_ItemID.GrenadeFlash) || (itemID == E_ItemID.GrenadeEMP) || (itemID == E_ItemID.GrenadeEMPII))
			{
				if (paramIndex == 0)
					return 0111070;
				else if (paramIndex == 1)
					return 0111060;
				else
					Error("unknown index!");
			}
			else if ((itemID == E_ItemID.BoxHealth) || (itemID == E_ItemID.BoxHealthII))
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
			else if ((itemID == E_ItemID.BoxAmmo) || (itemID == E_ItemID.BoxAmmoII))
			{
				if (paramIndex == 0)
					return 0111080;
				else if (paramIndex == 1)
					return 0111060;
				else
					Error("unknown index!");
			}
			else if ((itemID == E_ItemID.EnemyDetector) || (itemID == E_ItemID.EnemyDetectorII))
			{
				if (paramIndex == 0)
					return 0111070;
				else if (paramIndex == 1)
					return 0111060;
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

	// ------
	public bool UpgradeIsAppliedOnParam(int paramIndex)
	{
		// -----------------------------
		if (weaponID != E_WeaponID.None)
		{
			switch (paramIndex)
			{
			case 0:
				return !Mathf.Approximately(DamageModificator(), 1.0f);
			case 1:
				return !Mathf.Approximately(FireTimeModificator(), 1.0f);
			case 2:
				return !Mathf.Approximately(DispersionModificator(), 1.0f);
			case 3:
				return !Mathf.Approximately(ClipSizeModificator(), 1.0f);
			default:
				Error("unknown index!");
				break;
			}
		}
		return false;
	}

	// ------
	public string GetParamValue(int paramIndex)
	{
		// -----------------------------
		if (weaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(weaponID);
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
				return Mathf.Round(100*(MAX_DISPERSION - (settings.BaseData.Dispersion*DispersionModificator()))/MAX_DISPERSION).ToString();
			case 3:
				return Mathf.CeilToInt(settings.BaseData.MaxAmmoInClip*ClipSizeModificator()).ToString();
			default:
				Error("unknown index!");
				break;
			}
		}
		// -----------------------------
		else if (itemID != E_ItemID.None)
		{
			ItemSettings settings = ItemSettingsManager.Instance.Get(itemID);
			//----
			if ((itemID == E_ItemID.SentryGun) || (itemID == E_ItemID.SentryGunII) || (itemID == E_ItemID.SentryGunRail) ||
				(itemID == E_ItemID.SentryGunRockets))
			{
				SentryGun gun = settings.SpawnObject.GetComponent<SentryGun>();
				switch (paramIndex)
				{
				case 0:
					return gun.m_WpnSettings.m_Damage.ToString();
				case 1:
					return Mathf.Round(1.0f/gun.m_WpnSettings.m_FireRate).ToString();
				case 2:
					return Mathf.Round(100*(MAX_DISPERSION - gun.m_WpnSettings.m_Dispersion)/MAX_DISPERSION).ToString();
				case 3:
					return gun.m_WpnSettings.m_RangeMaximal.ToString();
				default:
					Error("unknown index!");
					break;
				}
			}
			else if (itemID == E_ItemID.Mine)
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
			else if ((itemID == E_ItemID.MineEMP) || (itemID == E_ItemID.MineEMPII))
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
			else if ((itemID == E_ItemID.GrenadeFrag) || (itemID == E_ItemID.GrenadeFragII))
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
			else if (itemID == E_ItemID.GrenadeFlash)
			{
				FlashBangProjectile obj = settings.SpawnObject.GetComponent<FlashBangProjectile>();

				if (paramIndex == 0)
					return obj.Radius.ToString();
				else if (paramIndex == 1)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if ((itemID == E_ItemID.GrenadeEMP) || (itemID == E_ItemID.GrenadeEMPII))
			{
				EMPGrenadeProjectile obj = settings.SpawnObject.GetComponent<EMPGrenadeProjectile>();

				if (paramIndex == 0)
					return obj.Radius.ToString();
				else if (paramIndex == 1)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if ((itemID == E_ItemID.BoxHealth) || (itemID == E_ItemID.BoxHealthII))
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
			else if ((itemID == E_ItemID.BoxAmmo) || (itemID == E_ItemID.BoxAmmoII))
			{
				AmmoKit obj = settings.SpawnObject.GetComponent<AmmoKit>();

				if (paramIndex == 0)
					return obj.Timer.ToString();
				else if (paramIndex == 1)
					return settings.Count.ToString();
				else
					Error("unknown index!");
			}
			else if ((itemID == E_ItemID.EnemyDetector) || (itemID == E_ItemID.EnemyDetectorII))
			{
				if (paramIndex == 0)
					return settings.Range.ToString();
				else if (paramIndex == 1)
					return settings.Count.ToString();
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

	// ------
	public int GetNumOfUpgrades()
	{
		int count = 0;
		if (weaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(weaponID);

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
	public int GetUpgradeName(int index)
	{
		WeaponSettings.Upgrade upgrade = GetUpgrade(index);

		if (upgrade != null)
		{
			switch (upgrade.ID)
			{
			case E_WeaponUpgradeID.Dispersion:
				return 0112040;
			case E_WeaponUpgradeID.AimingFov:
				return 0112070;
			case E_WeaponUpgradeID.BulletSpeed:
				return 0112050;
			case E_WeaponUpgradeID.ClipSize:
				return 0112030;
			case E_WeaponUpgradeID.Damage:
				return 0112020;
			case E_WeaponUpgradeID.AmmoSize:
				return 0112060;
			case E_WeaponUpgradeID.Silencer:
				return 0112080;
			case E_WeaponUpgradeID.FireTime:
				return 0112090;
			default:
				Error("unknown index!");
				break;
			}
		}
		return 0;
	}

	// ------
	public string GetUpgradeValueText(int index)
	{
		string text = ""; //TextDatabase.instance[GetUpgradeName(index)];

		WeaponSettings settings = WeaponSettingsManager.Instance.Get(weaponID);
		WeaponSettings.Upgrade upgrade = GetUpgrade(index);
		if (upgrade != null)
		{
			switch (upgrade.ID)
			{
			case E_WeaponUpgradeID.Dispersion:
				if (upgrade.Modifier <= 0)
				{
					float upgradeVal = 100*((ResearchItem.MAX_DISPERSION - settings.BaseData.Dispersion*(1 + upgrade.Modifier))/
											(ResearchItem.MAX_DISPERSION - settings.BaseData.Dispersion) - 1);
					text += "+ " + (upgradeVal >= 10 ? upgradeVal.ToString("0") : upgradeVal.ToString("0.0")) + "%";
				}
				else
					text += (upgrade.Modifier*100) + "%";
				break;
			case E_WeaponUpgradeID.BulletSpeed:
			case E_WeaponUpgradeID.ClipSize:
			case E_WeaponUpgradeID.Damage:
				if (upgrade.Modifier >= 0)
					text += "+ " + (upgrade.Modifier*100) + "%";
				else
					text += (upgrade.Modifier*100) + "%";
				break;
			case E_WeaponUpgradeID.AimingFov:
			case E_WeaponUpgradeID.FireTime:

				if (upgrade.Modifier >= 0)
					text += (-upgrade.Modifier*100) + "%";
				else
					text += "+ " + (-upgrade.Modifier*100) + "%";
				break;
			case E_WeaponUpgradeID.AmmoSize:
				text += "+ " + upgrade.Modifier;
				break;
			case E_WeaponUpgradeID.Silencer:
				break;
			default:
				Error("unknown index!");
				break;
			}
		}

		return text;
	}

	// ------
	public bool OwnsUpgrade(int index)
	{
		WeaponSettings.Upgrade upgrade = GetUpgrade(index);

		if (upgrade != null)
		{
			return ResearchSupport.Instance.GetPPI().InventoryList.OwnsWeaponUpgrade(weaponID, upgrade.ID);
		}

		return false;
	}

	// ------
	public WeaponSettings.Upgrade GetUpgrade(int index)
	{
		if (weaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(weaponID);

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

	// ------
	public ResearchState GetState()
	{
		Validate();
		return m_State;
	}

	// ------
	public void Validate()
	{
		if (m_StateDirty)
			RecomputeState();
	}

	// ------
	public void StateChanged()
	{
		foreach (ResearchItem item in children)
		{
			if (item != null)
				item.StateChanged();
			else
				Error("Children is null!");
		}
		RecomputeState();
		RefreshVisuals();
	}

	// ------
	void SetDirtyFlag()
	{
		m_StateDirty = true;
	}

	// ------
	public void SetParent(ResearchItem parent)
	{
		if (m_Parent)
			Error("Parent already assigned!");
		m_Parent = parent;

		foreach (ResearchItem item in children)
		{
			if (item && (item == m_Parent))
			{
				Error("Cyclic parentship!");
			}
		}
	}

	// ------
	// For constructing hierarchy
	public void Init()
	{
		foreach (ResearchItem item in children)
		{
			if (item)
				item.SetParent(this);
		}

		if (m_Icon == null)
			m_Icon = (weaponID != E_WeaponID.None)
									 ? ResearchSupport.Instance.GetNewResearchWeaponIcon()
									 : ResearchSupport.Instance.GetNewResearchRestIcon();

		m_Icon.Init(GetComponent<GUIBase_Widget>(), weaponID != E_WeaponID.None);
		m_Icon.SetButtonCallback(ButtonPressed);

		if (weaponID != E_WeaponID.None)
		{
			m_UpgradeIcons = new UpgradeIcon[MAX_UPGRADES];

			for (int i = 0; i < MAX_UPGRADES; i++)
			{
				m_UpgradeIcons[i] = ResearchSupport.Instance.GetNewUpgradeIcon();
			}
			m_Icon.SetUpgradeIcons(m_UpgradeIcons);
		}

		InitData();
		ResearchSupport.Instance.RegisterResearchItem(this, SetDirtyFlag);
		m_StateDirty = true;
	}

	// ------
	// Hierarchy is established - initialize visuals
	public void Show(IViewOwner owner)
	{
		m_IsVisible = true;
		m_Owner = owner;
		RecomputeState();
		RefreshVisuals();
		m_Icon.Show();
	}

	// ------
	public void Hide()
	{
		m_IsVisible = false;
		m_Owner = null;
		m_Icon.Hide();
	}

	// ------
	public bool Enabled()
	{
		if (weaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(weaponID);
			return !settings.DISABLED;
		}
		else if (itemID != E_ItemID.None)
		{
			ItemSettings settings = ItemSettingsManager.Instance.Get(itemID);
			return !settings.DISABLED;
		}
		else if (upgradeID != E_UpgradeID.None)
		{
			UpgradeSettings settings = UpgradeSettingsManager.Instance.Get(upgradeID);
			return !settings.DISABLED;
		}
		else if (perkID != E_PerkID.None)
		{
			PerkSettings settings = PerkSettingsManager.Instance.Get(perkID);
			return !settings.DISABLED;
		}
		else
		{
			Error("Unknown type!");
		}

		return false;
	}

	// ------
	void InitData()
	{
		if (weaponID != E_WeaponID.None)
		{
			WeaponSettings settings = WeaponSettingsManager.Instance.Get(weaponID);
			m_ItemName = settings.Name;
			m_ItemDescription = settings.Description;
			m_IsPriceGold = settings.GoldCost > 0;
			m_Price = m_IsPriceGold ? settings.GoldCost : settings.MoneyCost;
			m_Image = settings.ResearchWidget;
			m_GUID = settings.GUID;
		}
		else if (itemID != E_ItemID.None)
		{
			ItemSettings settings = ItemSettingsManager.Instance.Get(itemID);
			m_ItemName = settings.Name;
			m_ItemDescription = settings.Description;
			m_IsPriceGold = settings.GoldCost > 0;
			m_Price = m_IsPriceGold ? settings.GoldCost : settings.MoneyCost;
			m_Image = settings.ResearchWidget;
			m_GUID = settings.GUID;
		}
		else if (upgradeID != E_UpgradeID.None)
		{
			UpgradeSettings settings = UpgradeSettingsManager.Instance.Get(upgradeID);
			m_ItemName = settings.Name;
			m_ItemDescription = settings.Description;
			m_IsPriceGold = settings.GoldCost > 0;
			m_Price = m_IsPriceGold ? settings.GoldCost : settings.MoneyCost;
			m_Image = settings.ResearchWidget;
			m_GUID = settings.GUID;
		}
		else if (perkID != E_PerkID.None)
		{
			PerkSettings settings = PerkSettingsManager.Instance.Get(perkID);
			m_ItemName = settings.Name;
			m_ItemDescription = settings.Description;
			m_IsPriceGold = settings.GoldCost > 0;
			m_Price = m_IsPriceGold ? settings.GoldCost : settings.MoneyCost;
			m_Image = settings.ResearchWidget;
			m_GUID = settings.GUID;
		}
		else
		{
			Error("Unknown type!");
		}
	}

	// ------
	void RefreshVisuals()
	{
		if (!m_IsVisible)
			return;

		bool isGold;
		int cost = GetPrice(out isGold);

		m_Icon.SetPrice(cost, ResearchSupport.Instance.HasPlayerEnoughFunds(cost, isGold));
		m_Icon.SetName(GetName());
		m_Icon.SetImage(GetImage());

		bool fullyResearched = true;

		if (m_UpgradeIcons != null)
		{
			int maxUpgrades = GetNumOfUpgrades();
			for (int i = 0; i < maxUpgrades; i++)
			{
				bool ownsUpgrade = OwnsUpgrade(i);
				if (!ownsUpgrade)
					fullyResearched = false;
				m_UpgradeIcons[i].SetUpgradeType(GetUpgrade(i).ID);
				m_UpgradeIcons[i].SetStatus(ownsUpgrade ? UpgradeIcon.Status.Active : UpgradeIcon.Status.Inactive);
				m_UpgradeIcons[i].Show();
			}

			for (int i = 0; i < maxUpgrades; i++)
			{
				if (fullyResearched)
					m_UpgradeIcons[i].SetStatus(UpgradeIcon.Status.FullyUpgraded);
			}

			for (int i = maxUpgrades; i < MAX_UPGRADES; i++)
				m_UpgradeIcons[i].Hide();
		}

		m_Icon.SetState(GetState(), fullyResearched, m_RequiredRank);
	}

	// ------
	protected void RecomputeState()
	{
		m_StateDirty = false;
		m_CantBuyExplanation = "";
		m_RequiredRank = 0;

		if (weaponID != E_WeaponID.None)
		{
			if (ResearchSupport.Instance.GetPPI().InventoryList.ContainsWeapon(weaponID))
			{
				m_State = ResearchState.Active;
				return;
			}
			m_RequiredRank = WeaponSettingsManager.Instance.Get(weaponID).MinRank;
		}
		else if (itemID != E_ItemID.None)
		{
			foreach (PPIItemData data in ResearchSupport.Instance.GetPPI().InventoryList.Items)
			{
				//Debug.Log(data.ID);
				if (data.ID == itemID)
				{
					m_State = ResearchState.Active;
					return;
				}
			}
			m_RequiredRank = ItemSettingsManager.Instance.Get(itemID).MinRank;
		}
		else if (upgradeID != E_UpgradeID.None)
		{
			foreach (PPIUpgradeList.UpgradeData data in ResearchSupport.Instance.GetPPI().Upgrades.Upgrades)
			{
				if (data.ID == upgradeID)
				{
					m_State = ResearchState.Active;
					return;
				}
			}
			m_RequiredRank = UpgradeSettingsManager.Instance.Get(upgradeID).MinRank;
		}
		else if (perkID != E_PerkID.None)
		{
			foreach (PPIPerkData data in ResearchSupport.Instance.GetPPI().InventoryList.Perks)
			{
				if (data.ID == perkID)
				{
					m_State = ResearchState.Active;
					return;
				}
			}
			m_RequiredRank = PerkSettingsManager.Instance.Get(perkID).MinRank;
		}
		else
		{
			Error("Data are not properly set!");
		}

		bool isGold;
		int cost = GetPrice(out isGold);

		if (ResearchSupport.Instance.GetPPI().Rank < m_RequiredRank)
		{
			m_CantBuyExplanation = TextDatabase.instance[0113100];
			m_State = ResearchState.Unavailable;
			return;
		}
		else
		{
			m_RequiredRank = 0;
			if (m_Parent)
			{
				if (m_Parent.GetState() == ResearchState.Active)
				{
					if (ResearchSupport.Instance.HasPlayerEnoughFunds(cost, isGold))
						m_State = ResearchState.Available;
					else
						m_State = ResearchState.Unavailable;
					return;
				}
				else
				{
					m_CantBuyExplanation = TextDatabase.instance[0113090];
					m_State = ResearchState.Unavailable;
					return;
				}
			}
		}

		if (ResearchSupport.Instance.HasPlayerEnoughFunds(cost, isGold))
			m_State = ResearchState.Available;
		else
			m_State = ResearchState.Unavailable;
	}

	// ------
	public void ButtonPressed()
	{
		if (m_Owner != null)
		{
			GuiPopupViewResearchItem popik = m_Owner.ShowPopup("ViewResearchItem", "", "", null) as GuiPopupViewResearchItem;
			popik.SetItem(this);
		}
	}

	// ------
	float GetModificator(E_WeaponUpgradeID upgradeID, bool checkOwnership)
	{
		float modif = 1.0f;
		if (weaponID == E_WeaponID.None)
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
	void Error(string msg)
	{
		Debug.LogWarning("Error: ResearchItem: " + name + "  " + msg);
	}
}
