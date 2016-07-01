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

public class GuiShopPageBoostItems : GuiShopPageBase
{
	GUIBase_Label m_CountLabel;
	GUIBase_Label m_AddLabel;
	GUIBase_Label m_BoostModLabel;
	GUIBase_Label m_DurationLabel;

	// GUIVIEW INTERFACE
	protected override void OnViewInit()
	{
		base.OnViewInit();

		m_CountLabel = GuiBaseUtils.PrepareLabel(m_ScreenLayout, "CountInfo_Label");
		m_AddLabel = GuiBaseUtils.PrepareLabel(m_ScreenLayout, "AddInfo_Label");
		m_BoostModLabel = GuiBaseUtils.PrepareLabel(m_ScreenLayout, "BoostFactor_Label");
		m_DurationLabel = GuiBaseUtils.PrepareLabel(m_ScreenLayout, "BoostDuration_Label");
	}

	protected override void OnViewReset()
	{
		base.OnViewReset();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();
	}

	protected override void OnViewHide()
	{
		base.OnViewHide();
	}

	// SPOLECNY ITERFACE PRO VSECHNY PAGE
	public override void OnItemChange(ShopItemId itemId, bool forceUpdateView)
	{
		base.OnItemChange(itemId, forceUpdateView);

		ShopItemInfo itemInf = ShopDataBridge.Instance.GetItemInfo(itemId);

		string strYouHave = TextDatabase.instance[02900050];
		strYouHave = strYouHave.Replace("%i1", itemInf.OwnedCount.ToString());
		m_CountLabel.SetNewText(strYouHave);
		m_CountLabel.Widget.Show(itemInf.Consumable, true);

		string strAddCount = TextDatabase.instance[02900051];
		strAddCount = strAddCount.Replace("%i1", itemInf.AddCount.ToString());
		m_AddLabel.SetNewText(strAddCount);
		m_AddLabel.Widget.Show(itemInf.Consumable, true);

		string strDuration = TextDatabase.instance[02900052];
		strDuration = strDuration.Replace("%i1", itemInf.BoostDuration.ToString());
		m_DurationLabel.SetNewText(strDuration);
		m_DurationLabel.Widget.Show(itemInf.Consumable && itemInf.BoostDuration > 0, true);

		// hack na zobrazeni boost midifikatoru jen pro nektere itemy
		//pokud je boost 0 nebo male cislo (napr invisibilita) tak to ignorujeme.
		//pokud je vetsi jak 1 tak to znamena jen jiny typ zapisu, takze pak odecitame 100%.
		string strBoostModif = TextDatabase.instance[02900053];
		int boostMod = Mathf.CeilToInt(itemInf.BoostModifier*100);
		if (itemInf.BoostModifier > 1.0f)
			boostMod -= 100;

		strBoostModif = strBoostModif.Replace("%i1", boostMod.ToString());
		m_BoostModLabel.SetNewText(strBoostModif);
		m_BoostModLabel.Widget.Show(itemInf.Consumable && itemInf.BoostModifier >= 0.25f, true);
	}

	public override List<ShopItemId> GetItems()
	{
		return ShopDataBridge.Instance.GetBoostItems();
	}
}
