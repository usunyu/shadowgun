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

//"Componenta" pro zobrazeni gold a money.
//Vyzaduje pod root sprite nalinkovany sprite s gold a money iconami 'Gold_Sprite' a 'Money_Sprite' a label pro zobrazeni castky.

class GuiShopFunds
{
	GUIBase_Sprite m_RootSprite;
	GUIBase_Label m_FundsLabel;
	GUIBase_Sprite m_SpriteGold;
	GUIBase_Sprite m_SpriteMoney;
	bool m_Disabled;
	bool m_MissingFunds;
	bool m_IsGold;

	public GuiShopFunds(GUIBase_Sprite rootSprite)
	{
		m_RootSprite = rootSprite;

		m_FundsLabel = GuiBaseUtils.GetChildLabel(m_RootSprite.Widget, "Funds_Label");
		m_SpriteGold = GuiBaseUtils.GetChildSprite(m_RootSprite.Widget, "Gold_Sprite");
		m_SpriteMoney = GuiBaseUtils.GetChildSprite(m_RootSprite.Widget, "Money_Sprite");
	}

	public void SetValue(int cost, bool gold, bool plusSign = false)
	{
		m_IsGold = gold;

		string strCost = plusSign ? "+" + cost.ToString() : cost.ToString();
		m_FundsLabel.SetNewText(strCost);
		m_SpriteGold.Widget.Show(m_IsGold, false);
		m_SpriteMoney.Widget.Show(!m_IsGold, false);

		SetMissingFunds(gold && !plusSign ? ShopDataBridge.Instance.PlayerGold < cost : ShopDataBridge.Instance.PlayerMoney < cost);
	}

	public void Show(bool on)
	{
		m_RootSprite.Widget.Show(on, true);
		if (on)
		{
			m_SpriteGold.Widget.Show(m_IsGold, false);
			m_SpriteMoney.Widget.Show(!m_IsGold, false);
		}

		UpdateColors();
	}

	public void SetDisabled(bool disabled)
	{
		m_Disabled = disabled;
		UpdateColors();
	}

	public void SetMissingFunds(bool missingFunds)
	{
		m_MissingFunds = missingFunds;
		UpdateColors();
	}

	void UpdateColors()
	{
		Color color = m_Disabled ? Color.grey : Color.white;
		m_FundsLabel.Widget.Color = m_MissingFunds ? Color.red : color;
	}
};
