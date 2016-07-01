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

// !!!  NEMAZAT POLOZKY, PRIDAVAT POUZE NA KONEC SEZNAMU (pred MAX_ID) !!! - reflectuje se na CLOUD jako INT
public enum E_PlatformShop
{
	None,

	StandaloneOSX,
	StandaloneWin,
	iOS,
	Android,
	Facebook,

	MAX_ID // add any new IDs prior to this one!
}

public interface ISettings
{
	int GetGUID();
	string GetSettingsClass();
	string GetIdAsStr();
	bool IsDefault();

	bool SkipZeroCostWarning();
}

public class SettingsBase : MonoBehaviour, ISettings
{
	public bool DISABLED = false;
	public const string GUID_CALC_PREFIX = Game.PrimaryProductID;

	[OnlineShopItemSystemProperty] public int GUID;

	[SerializeField] bool m_DefaultItem; // this is editor helper only. If TRUE, export process will skip warning about zero cost of item

	public virtual bool SkipZeroCostWarning()
	{
		return m_DefaultItem;
	}

	public virtual int GetGUID()
	{
		return GUID;
	}

	public virtual string GetSettingsClass()
	{
		return "";
	}

	public virtual string GetIdAsStr()
	{
		return "";
	}

	public virtual bool IsDefault()
	{
		return false;
	} // true, pokud ma byt zarazen do DefaultPPI

	internal int CalcGUIDFromID()
	{
		return Mathf.Abs(FNVHash.CalcModFNVHash(GUID_CALC_PREFIX + GetSettingsClass() + GetIdAsStr()));
	}

	public virtual void OnGenerateGuid()
	{
		GUID = CalcGUIDFromID();
	}

	public virtual List<ISettings> GetChildSettings()
	{
		return new List<ISettings>();
	}
}

// -----------------
public class Settings<Key> : SettingsBase
{
	// --------------------------------------------------------------------------------------------
	// P U B L I C    P A R T
	// --------------------------------------------------------------------------------------------

	[OnlineShopItemSystemProperty] public Key ID;

	public int Name;
	public int Description;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public int Experience;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public bool AvailableInShop = true;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public List<E_PlatformShop> ExcludeFromShop; // list of platforms from which are settings excluded

	[OnlineShopItemProperty] public bool AvailableAsIAPOnly = false;

	[OnlineShopItemProperty] public bool PremiumOnly = false;

	[OnlineShopItemProperty] // Please don't change names of members tagged with 'OnlineShopItemProperty' as it breaks online shopping service
	public bool Consumable = false; // items for golds only

	[OnlineShopItemProperty] public bool BundleOnly = false;

	[OnlineShopItemProperty] public int MinRank = 0;

	[OnlineShopItemProperty] public int EquipGroup = 0;

	public Transform ShopWidgetPrefab;
	public Transform HudWidgetPrefab;
	public Transform ResearchWidgetPrefab;
	public Transform ScrollerWidgetPrefab;

	public GUIBase_Widget ShopWidget
	{
		get
		{
			if (ShopWidgetPrefab)
				return ShopWidgetPrefab.GetComponent<GUIBase_Widget>();
			else
				return null;
		}
	}

	public GUIBase_Widget HudWidget
	{
		get
		{
			if (HudWidgetPrefab)
				return HudWidgetPrefab.GetComponent<GUIBase_Widget>();
			else
				return null;
		}
	}

	public GUIBase_Widget ResearchWidget
	{
		get
		{
			if (ResearchWidgetPrefab)
				return ResearchWidgetPrefab.GetComponent<GUIBase_Widget>();
			else
				return null;
		}
	}

	public GUIBase_Widget ScrollerWidget
	{
		get
		{
			if (ScrollerWidgetPrefab)
				return ScrollerWidgetPrefab.GetComponent<GUIBase_Widget>();
			else
				return null;
		}
	}

	[OnlineShopItemProperty] public int SaleInPercent = 0;
										//pro IAP fundy (ie. gold) pouze zobrazuje slevu. Pro skiny, haty, itemy, bundly a non IAP fundy (ie. money) snizi realnou cenu o x procent. 

	public bool NewInShop;
	public bool RareItem;

	public override string GetIdAsStr()
	{
		return ID.ToString();
	}

	public bool AvailableInShopOnCurrentPlatform
	{
		get
		{
			if (!AvailableInShop)
				return false;

			if (ExcludeFromShop == null)
				return true;

			E_PlatformShop currentShop = GetCurrentPlatformShop();

			if (currentShop == E_PlatformShop.None || currentShop == E_PlatformShop.MAX_ID)
				return true;

			// settings are excluded from the shop
			if (ExcludeFromShop.Contains(currentShop))
				return false;
			else
				return true;
		}
	}

	E_PlatformShop GetCurrentPlatformShop()
	{
#if UNITY_IPHONE
		return E_PlatformShop.iOS;
#elif UNITY_ANDROID
		return E_PlatformShop.Android;
#else
		return E_PlatformShop.None;
#endif
	}
}
