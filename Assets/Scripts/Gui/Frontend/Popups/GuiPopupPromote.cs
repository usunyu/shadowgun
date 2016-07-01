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

public class GuiPopupPromote : GuiPopup
{
	readonly static string CLOSE_BUTTON = "Close_Button";
	readonly static string SKIPANIM_BUTTON = "SkipAnim_Button";
	readonly static string PLAYERRANKPIC = "PlayerRankPic";
	readonly static string POINTS_LABEL = "Points_Label";
	readonly static string FACEBOOK_BUTTON = "Facebook_Button";
	readonly static string TWITTER_BUTTON = "Twitter_Button";
#if ( !UNITY_EDITOR ) && (UNITY_ANDROID || UNITY_IPHONE )
	private static readonly string WOULDPOSTIT_NOTIFY = "WouldPostIt_Notify";
#endif

	// CONFIGURATION

	[SerializeField] float m_CountDownDuration = 0.5f;
	[SerializeField] AudioClip m_CountDownSound;

	// PRIVATE METHODS

	GUIBase_Button m_CloseButton;
	GUIBase_Button m_FacebookButton;
	GUIBase_Button m_TwitterButton;

	// PUBLIC METHODS

	public void SetData(int rank, int points)
	{
		//bool isMajorRank = PlayerPersistantInfo.IsMajorRank(rank);

		GUIBase_MultiSprite sprite = GuiBaseUtils.GetControl<GUIBase_MultiSprite>(Layout, PLAYERRANKPIC);
		sprite.State = string.Format("Rank_{0}", Mathf.Min(rank, sprite.Count - 1).ToString("D2"));

		GUIBase_Label label = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, POINTS_LABEL);
		var animation = MFGuiManager.AnimateWidget(label, 0, points);
		if (animation != null)
		{
			animation.Duration = m_CountDownDuration;
			animation.AudioClip = m_CountDownSound;
		}

#if ( !UNITY_EDITOR ) && (UNITY_ANDROID || UNITY_IPHONE)
				// set visibility of info text
		Layout.GetWidget(WOULDPOSTIT_NOTIFY).Show(true, true);
		string postMessage = string.Format(TextDatabase.instance[01150011], CloudUser.instance.nickName, rank);
		// register delegate for facebook button
		m_FacebookButton.Widget.Show(true, true);
		m_FacebookButton.RegisterTouchDelegate(() =>
		{
			m_FacebookButton.SetDisabled(true);
			StartCoroutine(sendMessage(rank.ToString()));
		});

		m_TwitterButton.Widget.Show(true, true);
		m_TwitterButton.RegisterTouchDelegate(() =>
		                                      {
			m_TwitterButton.SetDisabled(true);
			TwitterUtils.PostMessage(postMessage, (success) =>
			                         {
				if (m_TwitterButton.Widget.Visible == true)
				{
					m_TwitterButton.SetDisabled(!success);
				}
			});
		});
#else
		m_FacebookButton.Widget.Show(false, true);
		m_TwitterButton.Widget.Show(false, true);
#endif
	}

	// GUIPOPUP INTERFACE

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();
		m_CloseButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, CLOSE_BUTTON);
		m_FacebookButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, FACEBOOK_BUTTON);
		m_TwitterButton = GuiBaseUtils.GetControl<GUIBase_Button>(Layout, TWITTER_BUTTON);
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		// register delegate for close button
		m_CloseButton.RegisterTouchDelegate(() =>
											{
												Owner.Back();
												SendResult(E_PopupResultCode.Cancel);
											});
		RegisterButtonDelegate(SKIPANIM_BUTTON, () => { MFGuiManager.FlushAnimations(); }, null);

		// enable social buttons
		m_FacebookButton.SetDisabled(false);
		m_TwitterButton.SetDisabled(false);
	}

	protected override void OnViewHide()
	{
		// stop promote sound
		if (Layout.GetComponent<AudioSource>() != null && Layout.GetComponent<AudioSource>().isPlaying == true)
		{
			Layout.GetComponent<AudioSource>().Stop();
		}

		// unregister all delegates
		m_CloseButton.RegisterTouchDelegate(null);
		m_FacebookButton.RegisterTouchDelegate(null);
		m_TwitterButton.RegisterTouchDelegate(null);

		RegisterButtonDelegate(SKIPANIM_BUTTON, null, null);

		// done
		base.OnViewHide();
	}

#if ( !UNITY_EDITOR ) &&  (UNITY_ANDROID || UNITY_IPHONE)
	private IEnumerator sendMessage(string rank)
	{
		yield return StartCoroutine(FacebookPlugin.Instance.Init());
		if(FacebookPlugin.Instance.IsLoggedIn() == false)
		{
			yield return StartCoroutine(FacebookPlugin.Instance.Login());
		}

		string picture 		= "cds.r3r6g5y7.hwcdn.net/rankicons/rank_" + rank + ".png";
		string link 		= "https://www.facebook.com/Shadowgun";
		string caption 		= " ";
		string description 	= TextDatabase.instance[01150006];

		FacebookPlugin.Instance.Feed(picture,link, caption, description, success => {
			if(success == false)
			{
				Debug.Log("Unable to send message to facebook: " + FacebookPlugin.Instance.PluginState + " " + FacebookPlugin.Instance.LastError);
			}
		});

		if (m_FacebookButton.Widget.Visible == true)
		{
			m_FacebookButton.SetDisabled(true);
		}
	}
#endif
}
