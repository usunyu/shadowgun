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

public class ResearchIcon : MonoBehaviour
{
	public class LockedByRank
	{
		GUIBase_Sprite m_Image;
		GUIBase_Label m_Text;
		// ------
		public LockedByRank(GUIBase_Sprite image, GUIBase_Label text)
		{
			m_Image = image;
			m_Text = text;
		}

		// -----		
		public void Activate(int rank)
		{
			m_Text.SetNewText(string.Format("{0} {1}", TextDatabase.instance[1160003], rank.ToString()));
			m_Image.Widget.Show(true, true);
			m_Text.Widget.Show(true, true);
		}

		// ------
		public void Deactivate()
		{
			m_Image.Widget.Show(false, true);
			m_Text.Widget.Show(false, true);
		}
	}

	public Color AvailablePriceColor;
	public Color UnavailablePriceColor;

	public Color ActiveNameColor;
	public Color AvailableNameColor;

	public Color ResearchedIconColor;
	public Color FullyResearchedIconColor;

	public Color UpgradeStarActiveColor;
	public Color UpgradeStarInactiveColor;

	GUIBase_Label m_Price;
	GUIBase_Label m_ItemName;
	GUIBase_Sprite m_Image;
	//GUIBase_MultiSprite	m_Background;
	GUIBase_Sprite m_Enabled;
	GUIBase_Sprite m_Disabled;
	GUIBase_Sprite m_Finished;
	GUIBase_Sprite m_FullyUpgraded;
	GUIBase_Sprite m_Researched;
	GUIBase_Widget m_Base;
	GUIBase_Widget m_UpgradeParent = null;
	GUIBase_Widget[] m_Upgrades = new GUIBase_Widget[ResearchItem.MAX_UPGRADES];
	GUIBase_Widget[] m_Stars = new GUIBase_Widget[ResearchItem.MAX_UPGRADES];
	//GUIBase_Widget		m_FullyUpgraded;
	UpgradeIcon[] m_UpgradeIcons = null;
	bool m_PriceAvailable;
	bool m_UpgradesAvailable = false;
	LockedByRank m_LockedByRank;

	// ------
	public void Init(GUIBase_Widget newParent, bool useUpgrades)
	{
		m_Base = GetComponent<GUIBase_Widget>();

		m_Price = GuiBaseUtils.GetChildLabel(m_Base, "Price");
		m_ItemName = GuiBaseUtils.GetChildLabel(m_Base, "Name");
		m_Image = GuiBaseUtils.GetChildSprite(m_Base, "Image");
		m_Enabled = GuiBaseUtils.GetChild<GUIBase_Sprite>(m_Base, "Enabled");
		m_Disabled = GuiBaseUtils.GetChild<GUIBase_Sprite>(m_Base, "Disabled");
		m_Finished = GuiBaseUtils.GetChild<GUIBase_Sprite>(m_Base, "Finished");
		m_Researched = GuiBaseUtils.GetChild<GUIBase_Sprite>(m_Base, "Researched");

		m_LockedByRank = new LockedByRank(GuiBaseUtils.GetChild<GUIBase_Sprite>(m_Base, "LockedByRank"),
										  GuiBaseUtils.GetChild<GUIBase_Label>(m_Base, "LockedByRankText"));

		if (useUpgrades)
		{
			m_FullyUpgraded = GuiBaseUtils.GetChild<GUIBase_Sprite>(m_Base, "FullyUpgraded");
			m_UpgradeParent = GuiBaseUtils.GetChild<GUIBase_Widget>(m_Base, "Upgrades");
			for (int i = 0; i < ResearchItem.MAX_UPGRADES; i++)
			{
				m_Upgrades[i] = GuiBaseUtils.GetChild<GUIBase_Widget>(m_UpgradeParent, "Upgrade" + (i + 1));
				m_Stars[i] = GuiBaseUtils.GetChild<GUIBase_Widget>(m_UpgradeParent, "Star" + (i + 1));
			}
			//m_FullyUpgraded = GuiBaseUtils.GetChild<GUIBase_Widget>(m_UpgradeParent, "FullyUpgraded");
		}
		else
			m_FullyUpgraded = null;
		m_Base.Relink(newParent);
	}

	// ------
	public void SetUpgradeIcons(UpgradeIcon[] upgradeIcons)
	{
		/*
		int i = 0;
		foreach (UpgradeIcon upgIcon in upgradeIcons)
		{
			if (upgIcon != null)
			{	
				upgIcon.Relink(m_Upgrades[i]);
				++i;
			}
		}
		*/

		m_UpgradeIcons = upgradeIcons;
		m_UpgradesAvailable = true;
		m_UpgradeParent.Show(true, true);
	}

	// ------
	public void SetButtonCallback(GUIBase_Button.TouchDelegate callback)
	{
		gameObject.GetComponentInChildren<GUIBase_Button>().RegisterTouchDelegate(callback);
	}

	// ------
	public void SetImage(GUIBase_Widget image)
	{
		m_Image.Widget.CopyMaterialSettings(image);
	}

	// ------
	public void SetPrice(int price, bool available)
	{
		m_PriceAvailable = available;
		m_Price.SetNewText(price.ToString());
	}

	// ------
	public void SetName(int nameTextId)
	{
		m_ItemName.SetNewText(nameTextId);
	}

	// ------
	public void SetState(ResearchState state, bool fullyUpgraded, int requiredRank)
	{
		m_Base.Show(true, true);

		switch (state)
		{
		// ----	
		case ResearchState.Active:
			m_LockedByRank.Deactivate();
			m_ItemName.Widget.SetColor(ActiveNameColor);
			m_Price.Widget.Show(false, true);
			m_Researched.Widget.Show(true, true);
			m_Researched.Widget.Color = fullyUpgraded ? FullyResearchedIconColor : ResearchedIconColor;
			m_Finished.Widget.Color = fullyUpgraded ? FullyResearchedIconColor : ResearchedIconColor;
			m_Enabled.Widget.Show(false, true);
			m_Disabled.Widget.Show(false, true);
			if (fullyUpgraded && m_FullyUpgraded)
			{
				m_Finished.Widget.Show(false, true);
				m_FullyUpgraded.Widget.Show(true, true);
			}
			else
			{
				m_Finished.Widget.Show(true, true);
				if (m_FullyUpgraded)
					m_FullyUpgraded.Widget.Show(false, true);
			}
			break;
		// ----	
		case ResearchState.Available:
			m_LockedByRank.Deactivate();
			m_Price.Widget.SetColor(m_PriceAvailable ? AvailablePriceColor : UnavailablePriceColor);
			m_ItemName.Widget.SetColor(AvailableNameColor);
			m_Price.Widget.Show(true, true);
			m_Finished.Widget.Show(false, true);
			m_Researched.Widget.Show(false, true);
			m_Enabled.Widget.Show(true, true);
			m_Disabled.Widget.Show(false, true);
			if (m_FullyUpgraded)
				m_FullyUpgraded.Widget.Show(false, true);
			break;
		// ----	
		case ResearchState.Unavailable:
			if (requiredRank > 0)
				m_LockedByRank.Activate(requiredRank);
			else
				m_LockedByRank.Deactivate();
			m_Price.Widget.SetColor(m_PriceAvailable ? AvailablePriceColor : UnavailablePriceColor);
			m_ItemName.Widget.SetColor(AvailableNameColor);
			m_Price.Widget.Show(true, true);
			m_Finished.Widget.Show(false, true);
			m_Researched.Widget.Show(false, true);
			m_Enabled.Widget.Show(false, true);
			m_Disabled.Widget.Show(true, true);
			if (m_FullyUpgraded)
				m_FullyUpgraded.Widget.Show(false, true);
			break;
		// ----	
		default:
			Debug.LogWarning("Unknown enum!");
			break;
		}

		if (m_UpgradeParent)
		{
			m_UpgradeParent.Show(m_UpgradesAvailable, true);
			int i = 0;
			foreach (UpgradeIcon upgIcon in m_UpgradeIcons)
			{
				if (upgIcon.IsVisible())
				{
					if (upgIcon.GetStatus() == UpgradeIcon.Status.Inactive)
						m_Stars[i].Color = UpgradeStarInactiveColor;
					else
						m_Stars[i].Color = UpgradeStarActiveColor;
					m_Stars[i].Show(state == ResearchState.Active, true);
					++i;
				}
			}

			for (int j = i; j < ResearchItem.MAX_UPGRADES; j++)
				m_Stars[j].Show(false, true);
		}
	}

	// ------
	public void Show()
	{
		//GUIBase_Widget widget = GetComponent<GUIBase_Widget>();
		//widget.ShowImmediate( true, true );
	}

	// ------
	public void Hide()
	{
		//GUIBase_Widget widget = GetComponent<GUIBase_Widget>();
		//widget.ShowImmediate( false, true );
	}
}
