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

public class HudComponentWeaponsInventory : HudComponent
{
	static string s_ParentName = "Weapons";
	public static string s_WeaponButtonName = "CurrentWeapon";
	static string s_AmmoClipName = "Ammo_Mag";
	static string s_AmmoWeaponName = "Ammo_Rest";
	static string s_MainIconName = "CurrentWeaponIcon";
	static string s_WeaponLabelName = "Weapon_Label";
	static string s_IconName = "WeaponIcon";
	static string[] s_InventoryItemName = new string[] {"Weapon1", "Weapon2", "Weapon3"};

	E_WeaponID CurrentWeapon;
	GUIBase_Widget Parent;
	GUIBase_Widget MainWeapon;
	GUIBase_Number AmmoClip;
	GUIBase_Number AmmoWeapon;
	GUIBase_Widget WeaponIcon;
	Color WeaponIconOrigColor;
	GUIBase_Label WeaponLabel;
	GUIBase_Layout Layout;

	public class Item
	{
		public GUIBase_Button Button;
		public GUIBase_Widget Sprite;
		public Color OrigSpriteColor;
		//public GUIBase_Number Ammo;
	}
	Item[] InventoryItem = new Item[s_InventoryItemName.Length];

	public List<E_WeaponID> Weapons { get; private set; }

	// ------
	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot("MainHUD");
		Layout = pivot.GetLayout("HUD_Layout");
		Weapons = new List<E_WeaponID>();

		// ------
		Parent = Layout.GetWidget(s_ParentName, false);
		MainWeapon = Layout.GetWidget(s_WeaponButtonName, false);
		AmmoClip = GuiBaseUtils.GetChildNumber(MainWeapon, s_AmmoClipName);
		AmmoWeapon = GuiBaseUtils.GetChildNumber(MainWeapon, s_AmmoWeaponName);
		WeaponLabel = GuiBaseUtils.GetChildLabel(MainWeapon, s_WeaponLabelName);
		GUIBase_Sprite s = GuiBaseUtils.GetChildSprite(MainWeapon, s_MainIconName);
		WeaponIcon = s.Widget;
		WeaponIconOrigColor = Color.white; // WeaponIcon.Color;

		// ------
		for (int i = 0; i < s_InventoryItemName.Length; ++i)
		{
			int weaponIdx = i;

			InventoryItem[i] = new Item();
			InventoryItem[i].Button = GuiBaseUtils.RegisterButtonDelegate(Layout, s_InventoryItemName[i], () => { SelectWeapon(weaponIdx); }, null);
			InventoryItem[i].Sprite = GuiBaseUtils.GetChildSprite(InventoryItem[i].Button.Widget, s_IconName).Widget;
			InventoryItem[i].OrigSpriteColor = Color.white; //InventoryItem[i].Sprite.Color;
			//InventoryItem[i].Ammo = GuiBaseUtils.GetChildNumber(InventoryItem[i].Button,s_AmmoName);
		}
		GuiBaseUtils.RegisterButtonDelegate(Layout, "CycleWeapon", () => { CycleWeapons(); }, null);
		CurrentWeapon = E_WeaponID.None;
		//Hide();

		return true;
	}

	// ------
	protected override void OnDestroy()
	{
		Parent = null;
		MainWeapon = null;
		AmmoClip = null;
		AmmoWeapon = null;

		base.OnDestroy();
	}

	// ------
	protected override void OnHide()
	{
		//      Debug.Log("HIIIIIIIIIIIIIIDE");
		Parent.Show(false, true);

		base.OnHide();
	}

	// ------
	protected override void OnShow()
	{
		base.OnShow();

//        Debug.Log("SHOOOOOW");
		Parent.Show(true, true);
		SetCurrentWeapon();
	}

	// ------
	protected override void OnLateUpdate()
	{
		base.OnLateUpdate();
		if (!LocalPlayer)
			return;

		if (CurrentWeapon != LocalPlayer.Owner.WeaponComponent.CurrentWeapon)
			SetCurrentWeapon();

		UpdateAmmoDisplay();
		UpdatePlayerWeapons();
	}

	void ResetWeaponsList()
	{
		E_WeaponID[] ids = {E_WeaponID.None, E_WeaponID.None, E_WeaponID.None, E_WeaponID.None};
		Weapons.Clear();
		Weapons.AddRange(ids);
	}

	// --------
	public void RegisterWeaponsInInventory()
	{
		ResetWeaponsList();

		foreach (PPIWeaponData data in PPIManager.Instance.GetLocalPlayerPPI().EquipList.Weapons)
		{
			if (data.ID == E_WeaponID.None)
				continue;

			Weapons[data.EquipSlotIdx] = data.ID;
		}

		for (int idx = 0; idx < InventoryItem.Length; idx++)
		{
			if (idx < Weapons.Count && Weapons[idx] != E_WeaponID.None)
			{
				WeaponSettings settings = WeaponSettingsManager.Instance.Get(Weapons[idx]);
				InventoryItem[idx].Sprite.CopyMaterialSettings(settings.HudWidget);
			}
		}

		SetCurrentWeapon();
		UpdatePlayerWeapons();
	}

	// --------
	public E_WeaponID GetWeaponOnIndex(int index)
	{
		if (Weapons.Count == 0 || Weapons.Count <= index)
			return E_WeaponID.None;

		return Weapons[index];
	}

	// ------
	public void SetCurrentWeapon()
	{
		if (LocalPlayer == null || LocalPlayer.Owner == null || LocalPlayer.Owner.WeaponComponent == null)
			return;

		E_WeaponID w = LocalPlayer.Owner.WeaponComponent.CurrentWeapon;

		if (w == E_WeaponID.None || CurrentWeapon == w)
			return;

		CurrentWeapon = w;

		WeaponSettings s = WeaponSettingsManager.Instance.Get(w);
		WeaponLabel.SetNewText(s.Name);
		WeaponIcon.CopyMaterialSettings(s.HudWidget);
		UpdateAmmoDisplay();
		UpdatePlayerWeapons();
	}

	// ------
	void UpdateAmmoDisplay()
	{
		if (LocalPlayer == null || LocalPlayer.Owner == null || LocalPlayer.Owner.WeaponComponent == null)
			return;

		if (LocalPlayer.Owner.WeaponComponent.CurrentWeapon == E_WeaponID.None)
			return;

		//Debug.Log( s_WeaponType[idx] );
		WeaponBase w = LocalPlayer.Owner.WeaponComponent.GetCurrentWeapon();

		//pokud je ammo pod critickou hranici, zacni blikat s ammem (a zobraz ho cervene)
		//bool critical = (w.ClipAmmo > 0) && (w.IsCriticalAmmo);
		if (AmmoClip)
		{
			AmmoClip.SetNumber(w.ClipAmmo, 999);

			int ammo = w.WeaponAmmo + w.ClipAmmo;
			if (ammo == 0)
				WeaponIcon.Color = Color.red;
			else
				WeaponIcon.Color = WeaponIconOrigColor;
			/*if (critical)
            {
                AmmoClip.Widget.Show(m_WeaponButton[idx].Widget.IsVisible() && m_AmmoBlink, true);
                //a.m_ClipRed.SetNumber(w.ClipAmmo, 99);
                //a.m_ClipRed.Widget.Show(m_WeaponButton[idx].Widget.IsVisible() && !m_AmmoBlink, true);
            }
            else
            {
                AmmoClip.Widget.Show(m_WeaponButton[idx].Widget.IsVisible(), true);
                //a.m_ClipRed.Widget.Show(false, true);
            }
            /**/
		}

		if (AmmoWeapon != null)
			AmmoWeapon.SetNumber(w.WeaponAmmo, 999);
	}

	// --------
	void UpdatePlayerWeapons()
	{
		if (!LocalPlayer)
			return;

		for (int idx = 0; idx < InventoryItem.Length; idx++)
		{
			if (idx < Weapons.Count && Weapons[idx] != E_WeaponID.None)
			{
				// WeaponSettings settings = WeaponSettingsManager.Instance.Get(Weapons[idx]);
				WeaponBase w = LocalPlayer.Owner.WeaponComponent.GetWeapon(Weapons[idx]);
				int ammo = w.WeaponAmmo + w.ClipAmmo;

				//InventoryItem[idx].Ammo.SetNumber(ammo, 999);
				if (ammo > 0)
				{
					InventoryItem[idx].Button.SetDisabled(false);
					InventoryItem[idx].Sprite.Color = InventoryItem[idx].OrigSpriteColor;
				}
				else
				{
					InventoryItem[idx].Sprite.Color = Color.red;
					InventoryItem[idx].Button.SetDisabled(true);
				}

				if (Weapons[idx] == LocalPlayer.Owner.WeaponComponent.CurrentWeapon)
					InventoryItem[idx].Button.ForceHighlight(true);
				else
					InventoryItem[idx].Button.ForceHighlight(false);
				InventoryItem[idx].Button.Widget.Show(true, true);
			}
			else
			{
				InventoryItem[idx].Button.Widget.Show(false, true);
			}
		}
	}

	// ------
	public void UpdateControlsPosition()
	{
		MainWeapon.transform.position = GuiOptions.WeaponButton.Positon;
	}

	// --------
	void SelectWeapon(int idx)
	{
		if (!LocalPlayer.CanChangeWeapon())
			return;

		if (Weapons[idx] != LocalPlayer.Owner.WeaponComponent.CurrentWeapon)
			LocalPlayer.Controls.ChangeWeaponDelegate(Weapons[idx]);
	}

	// --------
	void CycleWeapons()
	{
		if (!LocalPlayer.CanChangeWeapon())
			return;

		int idx = Weapons.FindIndex(obj => obj == CurrentWeapon);
		int last = Weapons.Count - 1;
		int ammo = 0;
		do
		{
			idx += 1;
			idx = idx == last ? 0 : idx;

			var weapon = LocalPlayer.Owner.WeaponComponent.GetWeapon(Weapons[idx]);

			ammo = weapon != null ? weapon.WeaponAmmo + weapon.ClipAmmo : 0;
		} while (Weapons[idx] != CurrentWeapon && ammo == 0);

		SelectWeapon(idx);
	}
}
