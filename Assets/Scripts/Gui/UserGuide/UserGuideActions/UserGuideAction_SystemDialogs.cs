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

public abstract class UserGuideAction_SystemDialogs<T> : UserGuideActionWithPopup<T>
				where T : GuiPopup
{
	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (GuiFrontendMain.IsVisible == false)
			return false;
		if (GuideData.Menu.IsAnyPopupVisible() == true)
			return false;
		return true;
	}
}

public class UserGuideAction_BanMessage : UserGuideAction_SystemDialogs<GuiPopupBanInfo>
{
	// C-TOR

	public UserGuideAction_BanMessage()
	{
		Priority = (int)E_UserGuidePriority.BanMessage;
		AllowRepeatedExecution = true;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
		BanInfo baninfo = ppi != null ? ppi.Ban : default(BanInfo);
		System.DateTime date = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc) +
							   System.TimeSpan.FromMilliseconds(baninfo.EndTime);

		if (date < CloudDateTime.UtcNow)
			return false;

		ShowPopup().SetData(date.ToLocalTime(), baninfo.Message);

		// don't allow user to go through
		// there is only one way out
		// and it's logout ...
		return true;
	}

	protected override void OnPopupHides(E_PopupResultCode result)
	{
		CloudUser.instance.LogoutLocalUser();
		GuiFrontendMain.ShowLoginMenu();

		base.OnPopupHides(result);
	}
}

public class UserGuideAction_Welcome : UserGuideAction_SystemDialogs<GuiPopupWelcomeScreen>
{
	// C-TOR

	public UserGuideAction_Welcome()
	{
		Priority = (int)E_UserGuidePriority.Welcome;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		string key = ConstructLegacyKey("WelcomeScreen");
		if (string.IsNullOrEmpty(key) == true)
			return false;

		if (PlayerPrefs.GetInt(ConstructLegacyKey("WelcomeScreen"), 0) != 0)
			return false;
		PlayerPrefs.SetInt(ConstructLegacyKey("WelcomeScreen"), 1);

		// display popup
		ShowPopup();

		// done
		return true;
	}

	protected override void OnPopupHides(E_PopupResultCode result)
	{
		GuideData.ShowOffers = false;

		base.OnPopupHides(result);
	}

	// PRIVATE METHODS

	protected string ConstructLegacyKey(string key)
	{
		if (GuideData == null)
			return null;
		return string.Format("{0}.{1}.{2}", GuideData.PrimaryKey, typeof (UserGuide).Name, key);
	}
}

public class UserGuideAction_NewFeatures : UserGuideAction_SystemDialogs<GuiPopupNewFeaturesDialog>
{
	// C-TOR

	public UserGuideAction_NewFeatures()
	{
		Priority = (int)E_UserGuidePriority.NewFeatures;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		if (PlayerPrefs.GetInt(GuideData.PrimaryKey + ".LastDisplayedFeatures", 0) == GuiPopupNewFeaturesDialog.FEATURES_VERSION)
			return false;
		PlayerPrefs.SetInt(GuideData.PrimaryKey + ".LastDisplayedFeatures", GuiPopupNewFeaturesDialog.FEATURES_VERSION);

		// display popup
		ShowPopup("NewFeaturesDialog");

		// done
		return true;
	}

	protected override void OnPopupHides(E_PopupResultCode result)
	{
		GuideData.ShowOffers = false;

		base.OnPopupHides(result);
	}
}

public class UserGuideAction_ResetResearch : UserGuideAction_SystemDialogs<GuiPopupBetaInfoBox>
{
	// PUBLIC MEMBERS

	public static bool NotifyUser = false;

	// C-TOR

	public UserGuideAction_ResetResearch()
	{
		Priority = (int)E_UserGuidePriority.ResetResearch;
	}

	// USERGUIDEACTION INTERFACE

	protected override bool OnExecute()
	{
		if (base.OnExecute() == false)
			return false;

		if (NotifyUser == false)
			return false;

		// display popup
		ShowPopup();

		// done
		return true;
	}

	protected override void OnPopupHides(E_PopupResultCode result)
	{
		NotifyUser = false;

		base.OnPopupHides(result);
	}
}
