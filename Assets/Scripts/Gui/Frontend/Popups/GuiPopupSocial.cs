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

public class GuiPopupSocial : GuiPopup
{
	// GUIPOPUP INTEFACE

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	// IDs of localized strings
	const int TextID_Reward_Caption = 01011110;
	const int TextID_Reward_Message = 01011111;
	const int TextID_Rewarded_TWMFG = 01011071;
	const int TextID_Rewarded_FBMFG = 01011091;
	const int TextID_Rewarded_FBSG = 01011081;
	const int TextID_Rewarded_FBDT = 01011101;
	const int TextID_CheckingStatus = 01011120;

	// names of buttons
	readonly static string BUTTON_CLOSE = "Close_Button";
	//private static readonly   string   BUTTON_ADCOLONY = "ButtonAdColony";
	//private static readonly   string   BUTTON_BANNER   = "ButtonBanner";
	readonly static string BUTTON_TWMFG = "ButtonTwitter";
	readonly static string BUTTON_FBMFG = "ButtonFBMFG";
	readonly static string BUTTON_FBSG = "ButtonFBSG";
	readonly static string BUTTON_FBDT = "ButtonFBDT";

	//=================================================================================================================

	static class Notification
	{
		//static private GuiPopupMessageBox m_Popup;

		public static bool Show(string caption, string message, bool showButton)
		{
			/*m_Popup = GuiBaseUtils.ShowMessageBox( caption, message, OnResult ) as GuiPopupMessageBox;
			
			if ( m_Popup != null )
			{
				m_Popup.SetButtonVisible( showButton );   return true;
			}*/

			return false;
		}

		public static void Hide()
		{
			/*if ( m_Popup != null )
			{
				m_Popup.ForceClose();
			}*/
		}

		static void OnResult(GuiPopup popup, E_PopupResultCode result)
		{
			/*if ( m_Popup == popup )
			{
				m_Popup.SetButtonVisible( true );
			}*/
		}
	}

	//=================================================================================================================

	// tracked rewarded site data
	class SiteData
	{
		public string m_URL; // URL of Facebook/Twitter site
		public string m_ID; // corresponding Facebook/Twitter ID
		public bool m_Rewarded; // already rewarder ?
		public bool m_FBSite; // Facebook site ?
		public GUIBase_Button m_Button; // corresponding button
		public string m_ButtonRewardedLabel; // rewarded-text for button-label ( e.g. "Like..." --> "Visit..." )
	}

	//=================================================================================================================

	// various Facebook/Twitter sites
	SiteData m_TwitterMFG;
	SiteData m_FacebookMFG;
	SiteData m_FacebookDT;
	SiteData m_FacebookSG;

	// currenlty "visited" Facebook/Twitter site
#if UNITY_IPHONE || UNITY_ANDROID
	SiteData m_VisitingSite;
#endif

	// logged-in to Facebook/Twitter ?
	bool m_LoggedIn;
	float m_WaitForLogin;

	// popup used as "checking status" notification
	GuiPopupMessageBox m_Popup;

	//=================================================================================================================

	protected override void OnViewInit()
	{
		// call super class
		base.OnViewShow();

		// init Facebook/Twitter site's data...
		PPIBankData bankData = PPIManager.Instance.GetLocalPPI().BankData;

		m_TwitterMFG = new SiteData();
		m_TwitterMFG.m_URL = "https://twitter.com/madfingergames";
		m_TwitterMFG.m_ID = "85328174";
		m_TwitterMFG.m_Rewarded = bankData.TwitterSites.Contains(m_TwitterMFG.m_ID);
		m_TwitterMFG.m_FBSite = false;
		m_TwitterMFG.m_Button = GuiBaseUtils.GetButton(m_ScreenLayout, BUTTON_TWMFG);
		m_TwitterMFG.m_ButtonRewardedLabel = TextDatabase.instance[TextID_Rewarded_TWMFG];

		m_FacebookMFG = new SiteData();
		m_FacebookMFG.m_URL = "http://www.facebook.com/madfingergames";
		m_FacebookMFG.m_ID = "131826663523131";
		m_FacebookMFG.m_Rewarded = bankData.FacebookSites.Contains(m_FacebookMFG.m_ID);
		m_FacebookMFG.m_FBSite = true;
		m_FacebookMFG.m_Button = GuiBaseUtils.GetButton(m_ScreenLayout, BUTTON_FBMFG);
		m_FacebookMFG.m_ButtonRewardedLabel = TextDatabase.instance[TextID_Rewarded_FBMFG];

		m_FacebookDT = new SiteData();
		m_FacebookDT.m_URL = "http://www.facebook.com/DEADTRIGGER";
		m_FacebookDT.m_ID = "202653433190538";
		m_FacebookDT.m_Rewarded = bankData.FacebookSites.Contains(m_FacebookDT.m_ID);
		m_FacebookDT.m_FBSite = true;
		m_FacebookDT.m_Button = GuiBaseUtils.GetButton(m_ScreenLayout, BUTTON_FBDT);
		m_FacebookDT.m_ButtonRewardedLabel = TextDatabase.instance[TextID_Rewarded_FBDT];

		m_FacebookSG = new SiteData();
		m_FacebookSG.m_URL = "http://www.facebook.com/Shadowgun";
		m_FacebookSG.m_ID = "302554313089127";
		m_FacebookSG.m_Rewarded = bankData.FacebookSites.Contains(m_FacebookSG.m_ID);
		m_FacebookSG.m_FBSite = true;
		m_FacebookSG.m_Button = GuiBaseUtils.GetButton(m_ScreenLayout, BUTTON_FBSG);
		m_FacebookSG.m_ButtonRewardedLabel = TextDatabase.instance[TextID_Rewarded_FBSG];
	}

	protected override void OnViewShow()
	{
		// call super class
		base.OnViewShow();

		// bind buttons
		RegisterButtonDelegate(BUTTON_CLOSE, () => { Owner.Back(); }, null);
		//RegisterButtonDelegate( BUTTON_ADCOLONY, null, OnReleaseAdColony );
		//RegisterButtonDelegate( BUTTON_BANNER,   null, OnReleaseBanner   );
		RegisterButtonDelegate(BUTTON_FBDT, null, OnReleaseFBDT);
		RegisterButtonDelegate(BUTTON_FBMFG, null, OnReleaseFBMFG);
		RegisterButtonDelegate(BUTTON_FBSG, null, OnReleaseFBSG);
		RegisterButtonDelegate(BUTTON_TWMFG, null, OnReleaseTWMFG);

#if UNITY_IPHONE || UNITY_ANDROID
		// update labels of already rewarded Facebook/Twitter sites
		m_VisitingSite = null;
#endif
		UpdateSiteButton(m_TwitterMFG);
		UpdateSiteButton(m_FacebookMFG);
		UpdateSiteButton(m_FacebookDT);
		UpdateSiteButton(m_FacebookSG);
	}

	protected override void OnViewHide()
	{
		// unbind buttons
		RegisterButtonDelegate(BUTTON_CLOSE, null, null);
		//RegisterButtonDelegate( BUTTON_ADCOLONY, null, null );
		//RegisterButtonDelegate( BUTTON_BANNER,   null, null );
		RegisterButtonDelegate(BUTTON_FBDT, null, null);
		RegisterButtonDelegate(BUTTON_FBMFG, null, null);
		RegisterButtonDelegate(BUTTON_FBSG, null, null);
		RegisterButtonDelegate(BUTTON_TWMFG, null, null);

		// call super class
		base.OnViewHide();
	}

	//=================================================================================================================

	void OnReleaseAdColony(bool inside)
	{
		if (inside)
		{
			// TODO
		}
	}

	void OnReleaseBanner(bool inside)
	{
		if (inside)
		{
			// TODO
		}
	}

	void OnReleaseFBDT(bool inside)
	{
		if (inside)
		{
			VisitSite(m_FacebookDT);
		}
	}

	void OnReleaseFBMFG(bool inside)
	{
		if (inside)
		{
			VisitSite(m_FacebookMFG);
		}
	}

	void OnReleaseFBSG(bool inside)
	{
		if (inside)
		{
			VisitSite(m_FacebookSG);
		}
	}

	void OnReleaseTWMFG(bool inside)
	{
		if (inside)
		{
			VisitSite(m_TwitterMFG);
		}
	}

	//=================================================================================================================

	//-----------------------------------------------------------------------------------------------------------------
	// Updates label of button if player was rewarded for corresponding site.
	//-----------------------------------------------------------------------------------------------------------------
	void UpdateSiteButton(SiteData site)
	{
		if ((site.m_Rewarded == true) && (site.m_Button != null) && (site.m_ButtonRewardedLabel != string.Empty))
		{
			GUIBase_Label label = GuiBaseUtils.GetChildLabel(site.m_Button.Widget, "Button_Caption_idle");

			if (label != null)
			{
				label.SetNewText(site.m_ButtonRewardedLabel);
			}

			label = GuiBaseUtils.GetChildLabel(site.m_Button.Widget, "Button_Caption_over");

			if (label != null)
			{
				label.SetNewText(site.m_ButtonRewardedLabel);
			}

			label = GuiBaseUtils.GetChildLabel(site.m_Button.Widget, "Button_Caption_disabled");

			if (label != null)
			{
				label.SetNewText(site.m_ButtonRewardedLabel);
			}
		}
	}

#if UNITY_IPHONE || UNITY_ANDROID

	//-----------------------------------------------------------------------------------------------------------------
	// Opens given Facebook/Twitter site.
	//-----------------------------------------------------------------------------------------------------------------
	void VisitSite(SiteData site)
	{
		//	Debug.Log( "CAPA" );
		//	Debug.Log( "CAPA : GuiScreenBank : VisitSite( " + site.m_URL + " )" );

		m_LoggedIn = true;
		m_WaitForLogin = 0.0f;
		m_VisitingSite = site;

		if (site.m_FBSite) // Facebook
		{
			if ((site.m_Rewarded == false) && (FacebookPlugin.Instance.CurrentUser == null))
			{
				//	Debug.Log( "CAPA : GuiScreenBank : VisitSite : Login to Facebook..." );

				m_LoggedIn = false;
				m_WaitForLogin = float.MaxValue;

				FacebookPlugin.Instance.Login("user_likes", OnFacebookLoginComplete);
			}
		}
		else // Twitter
		{
			if ((site.m_Rewarded == false) && (TwitterWrapper.IsLoggedIn() == false))
			{
				//	Debug.Log( "CAPA : GuiScreenBank : VisitSite : Login to Twitter..." );

				m_LoggedIn = false;
				m_WaitForLogin = float.MaxValue;

				TwitterUtils.LogIn(this.OnLoginResult);
			}
		}

		StartCoroutine(WaitForLogin());
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Result of login-attempt.
	//-----------------------------------------------------------------------------------------------------------------
	void OnFacebookLoginComplete(SocialPlugin.State state, string message)
	{
		OnLoginResult(state == SocialPlugin.State.SUCCESS);
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Result of login-attempt.
	//-----------------------------------------------------------------------------------------------------------------
	void OnLoginResult(bool result)
	{
		//	Debug.Log( "CAPA : GuiScreenBank : OnLoginResult( " +  result + " )" );

		m_LoggedIn = result;
		m_WaitForLogin = 0.0f;
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Waits for login-attempt and then displays given site (so application will be paused / will loose focus).
	//-----------------------------------------------------------------------------------------------------------------
	IEnumerator WaitForLogin()
	{
		//	Debug.Log( "CAPA : GuiScreenBank : WaitForLogin : Begin" );

		float timeStep = 0.25f;
		bool extraWait = m_WaitForLogin > 0.0f;

		while ((m_WaitForLogin -= timeStep) > 0.0f)
		{
			yield return new WaitForSeconds(timeStep);
		}

		if (extraWait) // HACK: after twitter-login-dialog the web-view didn't show on iOS but the game "remained" paused (was paused again)
		{
			yield return new WaitForSeconds(0.5f);
		}

		EtceteraWrapper.ShowWeb(m_VisitingSite.m_URL);

		//	Debug.Log( "CAPA : GuiScreenBank : WaitForLogin : End" );
	}

	//-----------------------------------------------------------------------------------------------------------------
	// After return from web-site back to game send Facebook/Twitter request to check if player likes/follows that site.
	// Note: Maybe better 'OnApplicationFocus' isn't called on iOS so we were forced to use 'OnApplicationPause'
	//       which is called on both platforms.
	//-----------------------------------------------------------------------------------------------------------------
	void OnApplicationPause(bool pause)
	{
		//	Debug.Log( "CAPA : GuiScreenBank : OnApplicationPause( " + Pause + " )" );

		// not unpaused
		if (pause)
			return;

		// not visiting yet unrewarded site
		if ((m_VisitingSite == null) || (m_VisitingSite.m_Rewarded == true))
			return;

		//	// waiting for result of attempt to login Twitter/Facebook...
		//	if ( m_WaitForLogin > 0.0f )
		//	{
		//		// at this point we returned to app from login-dialog but still waiting for the result -->
		//		m_WaitForLogin = Mathf.Min( 15.0f, m_WaitForLogin ); // --> "reduce" waiting time to "reasonable" value
		//		
		//	//	Debug.Log( "CAPA : GuiScreenBank : OnApplicationPause : Still Waiting( " + m_WaitForLogin + " )" );
		//	}
		//	else

		// done so check if user is liking/following corresponding site...
		if (m_LoggedIn)
		{
			m_LoggedIn = false;

			string msg = TextDatabase.instance[TextID_CheckingStatus];
			Notification.Show(string.Empty, msg, false);

			if (m_VisitingSite.m_FBSite)
			{
				//	Debug.Log( "CAPA : GuiScreenBank : OnApplicationPause : Checking 'like' status..." );

				FacebookPlugin.Instance.GetUserLike(m_VisitingSite.m_ID, this.OnUserLikeResult);
			}
			else
			{
				//	Debug.Log( "CAPA : GuiScreenBank : OnApplicationPause : Checking 'follow' status..." );

				TwitterUtils.DoesUserFollow(m_VisitingSite.m_ID, this.OnStatusCheckResult);
			}
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Process result of "Does user follows/likes this site" request.
	//-----------------------------------------------------------------------------------------------------------------

	void OnUserLikeResult(FBResult textResult)
	{
		OnStatusCheckResult(FacebookPlugin.Instance.DoesUserLike(textResult.Text));
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Process result of "Does user follows/likes this site" request.
	//-----------------------------------------------------------------------------------------------------------------
	void OnStatusCheckResult(bool result)
	{
		//	Debug.Log( "CAPA : GuiScreenBank : OnStatusCheckResult( " + Result + " )" );

		Notification.Hide();

		if (!result)
			return;

		// inform about reward...

		int reward = 5;
		string caption = TextDatabase.instance[TextID_Reward_Caption];
		string message = TextDatabase.instance[TextID_Reward_Message].Replace("%d", reward.ToString());

		Notification.Show(caption, message, true);

		// add reward...

		PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();

		if (m_VisitingSite.m_FBSite)
		{
			ppi.BankData.FacebookSites.Add(m_VisitingSite.m_ID);
		}
		else
		{
			ppi.BankData.TwitterSites.Add(m_VisitingSite.m_ID);
		}

		//	ppi.AddGold( reward );
		//	ppi.Save();

		m_VisitingSite.m_Rewarded = true;
		UpdateSiteButton(m_VisitingSite);

		m_VisitingSite = null;

		//	Debug.Log( "CAPA : GuiScreenBank : OnStatusCheckResult : End" );
	}
#else
	private void VisitSite( SiteData site ){}
	
#endif // UNITY_IPHONE || UNITY_ANDROID
}
