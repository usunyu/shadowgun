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
public class GuiPopupViewResearchItem : GuiPopup
{
	// -----
	class Param
	{
		public GUIBase_Widget Parent;
		public GUIBase_Label Name;
		public GUIBase_Label Value;
		public Vector3 OrigPos;
	}

	// -----
	class Upgrade
	{
		public GUIBase_Widget m_Parent;
		public UpgradeIcon m_UpgradeIcon;
		public GUIBase_Widget m_CostGold;
		public GUIBase_Widget m_CostMoney;
		public GUIBase_Label m_CostVal;
		public GUIBase_Label m_Name;
		public GUIBase_Button m_Button;
		public GUIBase_Widget m_Researched;
	}

	IResearchItem m_ResearchItem;
	Param[] m_Params = new Param[ResearchItem.MAX_PARAMS];
	GUIBase_Label m_Name;
	GUIBase_TextArea m_Explanation;
	GUIBase_TextArea m_Description;
	GUIBase_Button m_ResearchButton;
	GUIBase_Label m_Price;
	GUIBase_Widget m_PriceArea;
	GUIBase_Sprite m_Image;

	Upgrade[] m_Upgrades = new Upgrade[ResearchItem.MAX_UPGRADES];
	GUIBase_Widget m_UpgradeArea;

	Color m_ActiveUpgradeBtnColor = new Color(0.1f, 0.1f, 0.1f); //new Color(0.1f, 0.1f, 0.1f);
	Color m_InactiveUpgradeBtnColor = new Color(0.0f, 0.1f, 0.09f); //new Color(0.0f,0.1f,0.08f);
	Color m_UpgradedValueColor = new Color(1.0f, 0.98f, 0.38f); //new Color(0.0f,0.1f,0.08f);

	// -----
	protected void Start()
	{
		for (int i = 0; i < ResearchItem.MAX_UPGRADES; i++)
		{
			m_Upgrades[i] = new Upgrade();
			m_Upgrades[i].m_UpgradeIcon = ResearchSupport.Instance.GetNewUpgradeIcon();
		}
	}

	// -----
	protected override void OnViewInit()
	{
		base.OnViewInit();

		if (m_ScreenLayout == null)
		{
			Debug.LogError("GuiConfirmDialog<" + name + "> :: There is not any layout specified for dialog!");
			return;
		}

		GUIBase_Button.TouchDelegate[] upgradeDlgts = new GUIBase_Button.TouchDelegate[]
		{Upgrade1Touch, Upgrade2Touch, Upgrade3Touch, Upgrade4Touch};

		for (int i = 0; i < ResearchItem.MAX_PARAMS; i++)
		{
			m_Params[i] = new Param();
			m_Params[i].Parent = GuiBaseUtils.GetChild<GUIBase_Widget>(Layout, "Param" + (i + 1));
			m_Params[i].OrigPos = m_Params[i].Parent.transform.localPosition;
			m_Params[i].Name = GuiBaseUtils.GetChildLabel(m_Params[i].Parent, "ParamName");
			m_Params[i].Value = GuiBaseUtils.GetChildLabel(m_Params[i].Parent, "ParamValue");
		}

		m_UpgradeArea = GetWidget(Layout, "Upgrade_Area").GetComponent<GUIBase_Widget>();
		for (int i = 0; i < ResearchItem.MAX_UPGRADES; i++)
		{
			GUIBase_Widget parent = GuiBaseUtils.GetChild<GUIBase_Widget>(m_UpgradeArea, "Upgrade" + (i + 1));
			m_Upgrades[i].m_Parent = parent;
			m_Upgrades[i].m_UpgradeIcon.Relink(parent);
			m_Upgrades[i].m_Button = GuiBaseUtils.GetChild<GUIBase_Button>(parent, "Button");
			m_Upgrades[i].m_Button.RegisterTouchDelegate(upgradeDlgts[i]);
			m_Upgrades[i].m_CostGold = GuiBaseUtils.GetChild<GUIBase_Widget>(parent, "CostGold");
			m_Upgrades[i].m_CostMoney = GuiBaseUtils.GetChild<GUIBase_Widget>(parent, "CostMoney");
			m_Upgrades[i].m_CostVal = GuiBaseUtils.GetChild<GUIBase_Label>(parent, "CostVal");
			m_Upgrades[i].m_Name = GuiBaseUtils.GetChild<GUIBase_Label>(parent, "Name");
			m_Upgrades[i].m_Researched = GuiBaseUtils.GetChild<GUIBase_Widget>(parent, "Researched");
		}

		m_ResearchButton = PrepareButton(m_ScreenLayout, "Research_Button", null, Delegate_Research);
		PrepareButton(m_ScreenLayout, "Close_Button", null, Delegate_Close);
		PrepareButton(m_ScreenLayout,
					  "Funds_Button",
					  null,
					  (widget) =>
					  {
						  if (BuildInfo.Version.Stage != BuildInfo.Stage.Beta)
						  {
							  IViewOwner owner = Owner;
							  owner.Back();
							  owner.ShowScreen("Shop:3");
						  }
					  });

		m_Name = PrepareLabel(m_ScreenLayout, "Name");
		m_Explanation = PrepareTextArea(m_ScreenLayout, "Explanation");
		m_Description = PrepareTextArea(m_ScreenLayout, "Description");
		m_Price = PrepareLabel(m_ScreenLayout, "Price");
		m_PriceArea = GetWidget(Layout, "Price_Area").GetComponent<GUIBase_Widget>();
		m_Image = GetWidget(Layout, "Image").GetComponent<GUIBase_Sprite>();
	}

	// -----
	protected override void OnViewShow()
	{
		base.OnViewShow();
	}

	// -----
	protected override void OnViewUpdate()
	{
		if (IsVisible == false)
			return;

		base.OnViewUpdate();
	}

	// -----
	public void SetItem(IResearchItem item)
	{
		m_ResearchItem = item;
		m_Name.SetNewText(m_ResearchItem.GetName());

		m_Description.SetNewText(m_ResearchItem.GetDescription());

		bool isGold;
		// TODO : PROBABLY NOT CORRECT, FUND TYPE IS NOT PROPERLY TESTED:
		m_Price.SetNewText(m_ResearchItem.GetPrice(out isGold).ToString());
		m_Image.Widget.CopyMaterialSettings(m_ResearchItem.GetImage());

		int maxParams = item.GetNumOfParams();
		for (int i = 0; i < maxParams; i++)
		{
			m_Params[i].Name.SetNewText(item.GetParamName(i));
			m_Params[i].Value.SetNewText(item.GetParamValue(i));
			m_Params[i].Value.Widget.Color = item.UpgradeIsAppliedOnParam(i) ? m_UpgradedValueColor : Color.white;
			ShowWidget(m_Params[i].Parent, true);
		}

		for (int i = maxParams; i < ResearchItem.MAX_PARAMS; i++)
		{
			ShowWidget(m_Params[i].Parent, false);
		}

		int maxUpgrades = item.GetNumOfUpgrades();
		if (m_ResearchItem.GetState() == ResearchState.Active)
		{
			ShowWidget(m_UpgradeArea, maxUpgrades > 0);
			ShowWidget(m_PriceArea, false);
		}
		else
		{
			ShowWidget(m_UpgradeArea, false);
			ShowWidget(m_PriceArea, true);
		}

		if (m_ResearchItem.GetState() == ResearchState.Active)
		{
			for (int i = 0; i < maxUpgrades; i++)
			{
				bool ownsUpgrade = item.OwnsUpgrade(i);
				WeaponSettings.Upgrade upgrade = item.GetUpgrade(i);
				m_Upgrades[i].m_UpgradeIcon.SetUpgradeType(upgrade.ID);
				m_Upgrades[i].m_UpgradeIcon.Show();

				isGold = upgrade.GoldCost > 0;
				bool hasEnoughMoney = ResearchSupport.Instance.HasPlayerEnoughFunds(isGold ? upgrade.GoldCost : upgrade.MoneyCost, isGold);
				if (!ownsUpgrade)
				{
					m_Upgrades[i].m_Name.Widget.Color = Color.white;
					m_Upgrades[i].m_CostVal.SetNewText(isGold ? upgrade.GoldCost.ToString() : upgrade.MoneyCost.ToString());

					if (!hasEnoughMoney)
					{
						GuiBaseUtils.PendingHint = E_Hint.Money;
						m_Upgrades[i].m_Parent.Color = m_ActiveUpgradeBtnColor;
						m_Upgrades[i].m_CostVal.Widget.Color = Color.red;
						m_Upgrades[i].m_UpgradeIcon.SetStatus(UpgradeIcon.Status.Inactive);
					}
					else
					{
						m_Upgrades[i].m_Parent.Color = m_ActiveUpgradeBtnColor;
						m_Upgrades[i].m_CostVal.Widget.Color = Color.white;
						m_Upgrades[i].m_UpgradeIcon.SetStatus(UpgradeIcon.Status.Active);
					}
					ShowWidget(m_Upgrades[i].m_Researched, false);
					ShowWidget(m_Upgrades[i].m_CostGold, isGold);
					ShowWidget(m_Upgrades[i].m_CostMoney, !isGold);
				}
				else
				{
					m_Upgrades[i].m_UpgradeIcon.SetStatus(UpgradeIcon.Status.Active);
					m_Upgrades[i].m_CostVal.Widget.Color = m_UpgradedValueColor;
					m_Upgrades[i].m_Name.Widget.Color = Color.white;
					m_Upgrades[i].m_Parent.Color = m_InactiveUpgradeBtnColor;
					m_Upgrades[i].m_CostVal.SetNewText(item.GetUpgradeValueText(i));
					ShowWidget(m_Upgrades[i].m_Researched, true);
					ShowWidget(m_Upgrades[i].m_CostGold, false);
					ShowWidget(m_Upgrades[i].m_CostMoney, false);
				}
				m_Upgrades[i].m_Name.SetNewText(item.GetUpgradeName(i));
				ShowWidget(m_Upgrades[i].m_Name.Widget, true);
				ShowWidget(m_Upgrades[i].m_CostVal.Widget, true);
				m_Upgrades[i].m_Button.SetDisabled(ownsUpgrade);
				ShowWidget(m_Upgrades[i].m_Button.Widget, true);
			}
			for (int i = maxUpgrades; i < ResearchItem.MAX_UPGRADES; i++)
			{
				m_Upgrades[i].m_UpgradeIcon.Hide();
				ShowWidget(m_Upgrades[i].m_Name.Widget, false);
				ShowWidget(m_Upgrades[i].m_CostGold, false);
				ShowWidget(m_Upgrades[i].m_CostMoney, false);
				ShowWidget(m_Upgrades[i].m_CostVal.Widget, false);
				ShowWidget(m_Upgrades[i].m_Parent, false);
			}
		}
		else
		{
			for (int i = 0; i < ResearchItem.MAX_UPGRADES; i++)
			{
				m_Upgrades[i].m_UpgradeIcon.Hide();
				ShowWidget(m_Upgrades[i].m_Name.Widget, false);
				ShowWidget(m_Upgrades[i].m_CostVal.Widget, false);
				ShowWidget(m_Upgrades[i].m_CostGold, false);
				ShowWidget(m_Upgrades[i].m_CostMoney, false);
			}
		}

		bool unavailable = m_ResearchItem.GetState() == ResearchState.Unavailable;
		int cost = m_ResearchItem.GetPrice(out isGold);
		bool notEnoughMoney = !ResearchSupport.Instance.HasPlayerEnoughFunds(cost, isGold);

		if (unavailable || notEnoughMoney && (m_ResearchItem.GetState() != ResearchState.Active))
		{
			string explanation = item.GetCantBuyExplanation();
			if ((explanation == "") && notEnoughMoney)
				explanation = TextDatabase.instance[0113080];
			m_Explanation.SetNewText(explanation);
			ShowWidget(m_Explanation.Widget, true);
			ShowWidget(m_ResearchButton.Widget, false);

			m_Price.Widget.Color = notEnoughMoney ? Color.red : Color.white;

			if (notEnoughMoney)
				GuiBaseUtils.PendingHint = E_Hint.Money;
		}
		else
		{
			ShowWidget(m_Explanation.Widget, false);
			if (m_ResearchItem.GetState() == ResearchState.Active)
				ShowWidget(m_ResearchButton.Widget, false);
			else
				ShowWidget(m_ResearchButton.Widget, true);
			m_Price.Widget.Color = Color.white;
		}
		m_ResearchButton.SetDisabled(unavailable || notEnoughMoney);
	}

	// -----
	public override void SetCaption(string inCaption)
	{
	}

	// -----
	public override void SetText(string inText)
	{
	}

	// ------
	void Upgrade1Touch()
	{
		DoUpgrade(0);
	}

	// ------
	void Upgrade2Touch()
	{
		DoUpgrade(1);
	}

	// ------
	void Upgrade3Touch()
	{
		DoUpgrade(2);
	}

	// ------
	void Upgrade4Touch()
	{
		DoUpgrade(3);
	}

	// ------
	void DoUpgrade(int upgradeIndex)
	{
		//TODO: start as coroutine, so we could continu in buying after IAP is sucessfull
		StartCoroutine("DoUpgradeCoroutine", upgradeIndex);
	}

	IEnumerator DoUpgradeCoroutine(int upgradeIndex)
	{
		WeaponSettings.Upgrade upgrade = m_ResearchItem.GetUpgrade(upgradeIndex);
		if (upgrade.GoldCost > 0 && ResearchSupport.Instance.HasPlayerEnoughFunds(upgrade.GoldCost, true) == false)
		{
			ShopItemId desired = new ShopItemId((int)upgrade.ParentID, GuiShop.E_ItemType.Weapon);
			ShopItemId reqIAP = ShopDataBridge.Instance.GetIAPNeededForItem(desired, upgradeIndex);

			if (reqIAP.IsEmpty())
			{
				yield break;
			}

			bool buySucceed = true;
			GuiShopNotFundsPopup.Instance.AddFundsID = reqIAP;
			GuiPopup popup = Owner.ShowPopup("NotFundsPopup",
											 "",
											 "",
											 (inPopup, inResult) =>
											 {
												 switch (inResult)
												 {
												 case E_PopupResultCode.Cancel:
													 buySucceed = false;
													 break;
												 case E_PopupResultCode.Failed:
													 buySucceed = false;
													 break;
												 }
											 });

			//Debug.Log("Popup Visible:" + popup.IsVisible);
			while (popup.IsVisible == true)
			{
				yield return new WaitForEndOfFrame();
			}

			if (buySucceed == false)
			{
				yield break;
			}

			//Debug.Log("IAP success:" + buySucceed);
		}

		if (ResearchSupport.Instance.HasPlayerEnoughFunds(upgrade.GoldCost, true))
		{
			GuiPopupDoUpgrade popik = Owner.ShowPopup("DoUpgrade", "", "", DoUpgradeResultHandler) as GuiPopupDoUpgrade;
			popik.SetItem(m_ResearchItem, upgradeIndex);
		}
	}

	// ------
	void DoUpgradeResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult == E_PopupResultCode.Ok)
		{
			SetItem(m_ResearchItem); // refresh data after upgrade
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

	// #################################################################################################################
	// ###  Delegates  #################################################################################################
	void Delegate_Research(GUIBase_Widget inInstigator)
	{
		GuiPopupDoResearch popik = Owner.ShowPopup("DoResearch", "", "", DoResearchResultHandler) as GuiPopupDoResearch;
		popik.SetItem(m_ResearchItem);
	}

	void Delegate_Close(GUIBase_Widget inInstigator)
	{
		Owner.Back();
		SendResult(E_PopupResultCode.Cancel);
	}

	// ------
	void DoResearchResultHandler(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		SetItem(m_ResearchItem);
		if (inResult == E_PopupResultCode.Success)
		{
			//Owner.Back();
		}
	}
}
