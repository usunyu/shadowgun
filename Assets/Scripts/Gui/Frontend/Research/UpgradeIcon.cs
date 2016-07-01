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

public class UpgradeIcon : MonoBehaviour
{
	public enum Status
	{
		Active,
		Inactive,
		FullyUpgraded
	}

	GUIBase_Widget Parent;
	GUIBase_MultiSprite Images;
	GUIBase_Widget Active;
	GUIBase_Widget Inactive;
	Status m_Status;
	bool m_Visible;
	public Color ActiveColor;
	public Color InactiveColor;
	public Color FullyUpgradedColor;

	// -------
	void Awake()
	{
		Parent = GetComponent<GUIBase_Widget>();
		Active = GuiBaseUtils.GetChild<GUIBase_Widget>(Parent, "Active");
		Inactive = GuiBaseUtils.GetChild<GUIBase_Widget>(Parent, "Inactive");
		Images = GuiBaseUtils.GetChild<GUIBase_MultiSprite>(Parent, "Images");
		//Button.RegisterTouchDelegate(upgradeDlgts[i]);
	}

	// ------
	public void Relink(GUIBase_Widget newParent, GUIBase_Widget imagesDummy = null)
	{
		Parent.Relink(newParent);
		float xScale = newParent.GetWidth()/Parent.GetWidth();
		float yScale = newParent.GetHeight()/Parent.GetHeight();

		if (imagesDummy != null)
		{
			float xScale2 = imagesDummy.GetWidth()/Images.Widget.GetWidth();
			float yScale2 = imagesDummy.GetHeight()/Images.Widget.GetHeight();
			Images.transform.localScale = new Vector3(1/xScale*xScale2, 1/yScale*yScale2, 1);
		}

		Parent.transform.localScale = new Vector3(xScale, yScale, 1);
		if (imagesDummy != null)
		{
			Images.transform.position = imagesDummy.transform.position;
		}
	}

	// ------
	public bool IsVisible()
	{
		return m_Visible;
	}

	// ------
	public Status GetStatus()
	{
		return m_Status;
	}

	// ------
	public void SetStatus(Status status)
	{
		m_Status = status;
		RefreshVisuals();
		SetColors();
	}

	// ------
	public void SetUpgradeType(E_WeaponUpgradeID upgrade)
	{
		string name = "";
		switch (upgrade)
		{
		case E_WeaponUpgradeID.Dispersion:
			name = "Accuracy";
			break;
		case E_WeaponUpgradeID.AimingFov:
			name = "AimingFov";
			break;
		case E_WeaponUpgradeID.BulletSpeed:
			name = "BulletSpeed";
			break;
		case E_WeaponUpgradeID.ClipSize:
			name = "ClipSize";
			break;
		case E_WeaponUpgradeID.Damage:
			name = "Damage";
			break;
		case E_WeaponUpgradeID.AmmoSize:
			name = "Homing";
			break;
		case E_WeaponUpgradeID.Silencer:
			name = "Silencer";
			break;
		case E_WeaponUpgradeID.FireTime:
			name = "FireRate";
			break;
		default:
			Debug.LogWarning("Error: E_WeaponUpgradeID " + upgrade + " is not handled by the switch!");
			break;
		}

		SetColors();
		Images.State = name;
	}

	// ------
	public void Show()
	{
		Parent.Show(true, true);
		RefreshVisuals();
		m_Visible = true;
	}

	// ------
	public void Hide()
	{
		Parent.Show(false, true);
		m_Visible = false;
	}

	// ------
	void SetColors()
	{
		if (m_Status == Status.Inactive)
			Images.Color = InactiveColor;
		else if (m_Status == Status.Active)
			Images.Color = ActiveColor;
		else if (m_Status == Status.FullyUpgraded)
			Images.Color = FullyUpgradedColor;
		else
			Debug.LogWarning("Unknown enum!");
	}

	// -------
	void RefreshVisuals()
	{
		Active.Show(m_Status != Status.Inactive, true);
		Inactive.Show(m_Status == Status.Inactive, true);
	}
}
