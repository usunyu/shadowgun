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

//Popup tooltip k predmetum ve scrollbaru.
#if false
public class GuiShopInfoPopup
{
	GUIBase_Layout 	m_WeaponLayout;
	GUIBase_Layout 	m_ItemLayout;
	GUIBase_Pivot 	m_WeaponPositionPivot;
	GUIBase_Pivot 	m_ItemPositionPivot;
	
	GUIBase_Label 	m_Name_Label;
	
	public void GuiInit()
	{
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot ("ShopPopups");
		m_WeaponLayout = pivot.GetLayout ("InfoWeapon_Layout");
		m_ItemLayout = pivot.GetLayout ("InfoItem_Layout");
		m_WeaponPositionPivot = MFGuiManager.Instance.GetPivot ("WeaponPosition_Pivot");
		m_ItemPositionPivot = MFGuiManager.Instance.GetPivot ("ItemPosition_Pivot");
		
		
		//m_Name_Label = GuiBaseUtils.PrepareLabel(m_Layout, "Name_Label");
	}
	
	public void Show(ShopItemId item, float desiredPos)
	{
		//o kolik muzeme maximalne posunout pivot. ted bereme z editoru, slo by dopocitat ze sirky bacground widgetu.
		const float localPosLimit = 555f;  
		Vector2 pos = new Vector2( Mathf.Clamp(desiredPos, -localPosLimit, localPosLimit), 0); //todo: dodelat i pro vertical scroller

		ShopItemInfo inf = ShopDataBridge.Instance.GetItemInfo(item);

		
		if(item.ItemType == GuiShop.E_ItemType.Weapon)
			ShowWeaponInfo(pos, inf);
		else if(item.ItemType == GuiShop.E_ItemType.Item)
			ShowItemInfo(pos, inf);
		
	}
	
	void ShowWeaponInfo(Vector2 pos, ShopItemInfo inf)
	{
		m_WeaponPositionPivot.transform.localPosition = pos;
		MFGuiManager.Instance.ShowLayout(m_WeaponLayout, true);
		//m_Name_Label.SetNewText(inf.NameText);
	}

	void ShowItemInfo(Vector2 pos, ShopItemInfo inf)
	{
		m_ItemPositionPivot.transform.localPosition = pos;
		MFGuiManager.Instance.ShowLayout(m_ItemLayout, true);
		//m_Name_Label.SetNewText(inf.NameText);
	}
	
	public void Hide()
	{
		MFGuiManager.Instance.ShowLayout(m_ItemLayout, false);
		MFGuiManager.Instance.ShowLayout(m_WeaponLayout, false);
	}
}
#endif
