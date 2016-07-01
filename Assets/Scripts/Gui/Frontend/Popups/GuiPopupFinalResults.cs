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

public class GuiPopupFinalResults : GuiPopup
{
	static Color[] m_TeamColors = new Color[3];

#if ( !UNITY_EDITOR ) && ( UNITY_ANDROID || UNITY_IPHONE )
	private readonly int[] TextID_RankingsDM = new int[3] { 01150020, 01150021, 01150022 };
	private readonly int[] TextID_RankingsZC = new int[3] { 01150023, 01150024, 01150025 };
#endif

	class Row
	{
		int m_Index;
		bool m_LocalPlayer;
		RoundFinalResult.PlayerResult m_Player;
		GUIBase_Widget m_Root;
		GUIBase_Label m_NicknameLabel;
		GUIBase_Label m_StandLabel;
		GUIBase_Label m_ScoreLabel;
		GUIBase_Label m_KillsLabel;
		GUIBase_Label m_DeadsLabel;
		GUIBase_Label m_RankLabel;
		GUIBase_MultiSprite m_RankIcon;
		GUIBase_MultiSprite m_PlatformIcon;
		GUIBase_Widget m_FriendIcon;

		public string PrimaryKey
		{
			get { return m_Player != null ? m_Player.PrimaryKey : string.Empty; }
		}

		public Row(int index, GUIBase_Widget root, RoundFinalResult.PlayerResult player, bool localPlayer)
		{
			m_Index = index;
			m_LocalPlayer = localPlayer;
			m_Player = player;
			m_Root = root;
			m_NicknameLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Name");
			m_StandLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Stand_Enum");
			m_ScoreLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Score_Enum");
			m_KillsLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Kills_Enum");
			m_DeadsLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Deads_Enum");
			m_RankLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "RankNumber");
			m_RankIcon = GuiBaseUtils.GetChild<GUIBase_MultiSprite>(root, "RankPic");
			m_PlatformIcon = GuiBaseUtils.GetChild<GUIBase_MultiSprite>(root, "PlatformPic");
			m_FriendIcon = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "FriendIcon");

			m_Root.Show(false, true);
		}

		public void Update()
		{
			if (m_Player == null)
				return;

			m_Root.Show(true, true);

			var animation = MFGuiManager.AnimateWidget(m_Root);
			animation.Duration = 0.15f;
			animation.Scale = 1.1f;
			animation.Alpha = 0.0f;

			bool isFriend = GameCloudManager.friendList.friends.FindIndex(obj => obj.PrimaryKey == m_Player.PrimaryKey) != -1;
			m_FriendIcon.Show(isFriend, true);

			int rank = PlayerPersistantInfo.GetPlayerRankFromExperience(m_Player.Experience);

			Color color = m_TeamColors[(int)m_Player.Team];
			color.a = m_LocalPlayer == true ? 1.0f : 0.5f;
			m_Root.Color = color;

			m_NicknameLabel.SetNewText(GuiBaseUtils.FixNameForGui(m_Player.NickName));
			m_StandLabel.SetNewText(m_Index.ToString());
			m_ScoreLabel.SetNewText(m_Player.Score.ToString());
			m_KillsLabel.SetNewText(m_Player.Kills.ToString());
			m_DeadsLabel.SetNewText(m_Player.Deaths.ToString());
			m_RankLabel.SetNewText(rank.ToString());
			m_RankIcon.State = string.Format("Rank_{0}", Mathf.Min(rank, m_RankIcon.Count - 1).ToString("D2"));

			string platform;
			switch (m_Player.Platform)
			{
			case RuntimePlatform.Android:
				platform = "Plat_Andro";
				break;
			case RuntimePlatform.IPhonePlayer:
				platform = "Plat_Apple";
				break;
			case RuntimePlatform.WindowsPlayer:
				platform = "Plat_Pc";
				break;
			case RuntimePlatform.OSXPlayer:
				platform = "Plat_Mac";
				break;
			case RuntimePlatform.WindowsWebPlayer:
				platform = "Plat_Fb";
				break;
			case RuntimePlatform.OSXWebPlayer:
				platform = "Plat_Fb";
				break;
			default:
				platform = "Plat_Skull";
				break;
			}
			m_PlatformIcon.State = platform;
		}
	}

	class PreyNightmare
	{
		GUIBase_Widget m_Root;
		GUIBase_Label m_NicknameLabel;
		GUIBase_Label m_KilledByMeLabel;
		GUIBase_Label m_KilledMeLabel;
		GUIBase_Label m_RankLabel;
		GUIBase_MultiSprite m_RankIcon;
		GUIBase_Widget m_FriendIcon;

		public AudioClip CountDownSound;

		public PreyNightmare(GUIBase_Widget root)
		{
			m_Root = root;
			m_NicknameLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "Name");
			m_KilledByMeLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "KilledByMe");
			m_KilledMeLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "KilledMe");
			m_RankLabel = GuiBaseUtils.GetChild<GUIBase_Label>(root, "RankNum");
			m_RankIcon = GuiBaseUtils.GetChild<GUIBase_MultiSprite>(root, "RankPic");
			m_FriendIcon = GuiBaseUtils.GetChild<GUIBase_Widget>(root, "FriendIcon");

			m_KilledByMeLabel.SetNewText("0");
			m_KilledMeLabel.SetNewText("0");
		}

		public void Update(RoundFinalResult.PlayerResult player, RoundFinalResult.PreyNightmare data)
		{
			if (player == null)
				return;

			m_Root.Show(true, true);

			int rank = PlayerPersistantInfo.GetPlayerRankFromExperience(player.Experience);

			bool isFriend = GameCloudManager.friendList.friends.FindIndex(obj => obj.PrimaryKey == player.PrimaryKey) != -1;
			m_FriendIcon.Show(isFriend, true);

			AnimateWidget(m_NicknameLabel, GuiBaseUtils.FixNameForGui(player.NickName));
			AnimateWidget(m_KilledByMeLabel, 0, data.KilledByMe);
			AnimateWidget(m_KilledMeLabel, 0, data.KilledMe);
			m_RankLabel.SetNewText(rank.ToString());
			m_RankIcon.State = string.Format("Rank_{0}", Mathf.Min(rank, m_RankIcon.Count - 1).ToString("D2"));
		}

		void AnimateWidget(GUIBase_Label label, string text)
		{
			var animation = MFGuiManager.AnimateWidget(label, text);
			if (animation != null)
			{
				animation.Duration = 0.5f;
				animation.AudioClip = CountDownSound;
			}
		}

		void AnimateWidget(GUIBase_Label label, float source, float target, string format = null)
		{
			var animation = MFGuiManager.AnimateWidget(label, source, target);
			if (animation != null)
			{
				animation.Duration = 0.5f;
				animation.AudioClip = CountDownSound;
			}
		}
	}

	// CONFIGURATION

	[SerializeField] Color m_NoTeamColor = new Color(90.0f/255, 145.0f/255, 120.0f/255);
	[SerializeField] Color m_TeamGoodColor = new Color(30.0f/255, 181.0f/255, 255.0f/255);
	[SerializeField] Color m_TeamBadColor = new Color(255.0f/255, 30.0f/255, 51.0f/255);
	[SerializeField] int[] m_YouWonTextIds = new int[0];
	[SerializeField] int[] m_YouLoseTextIds = new int[0];
	[SerializeField] AudioClip m_ShowRowSound;
	[SerializeField] AudioClip m_CountDownSound;

	// PRIVATE MEMBERS

	string m_PrimaryKey;
	GUIBase_MultiSprite m_ZoneControl;
	GUIBase_Widget[] m_DeathMatch = new GUIBase_Widget[3];
	bool m_SkipAnimations = false;
	UserGuideAction m_UserGuideAction = new UserGuideAction_FinalResults();

	// GUIPOPUP INTERFACE

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	// PUBLIC METHODS

	public void SetData(RoundFinalResult results)
	{
		StartCoroutine(ShowResults(results));
	}

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		GUIBase_Widget[] children = Layout.GetComponentsInChildren<GUIBase_Widget>();
		foreach (var child in children)
		{
			switch (child.name)
			{
			case "ZoneControl":
				m_ZoneControl = child.GetComponent<GUIBase_MultiSprite>();
				break;
			case "DeathMatch1st":
				m_DeathMatch[0] = child;
				break;
			case "DeathMatch2nd":
				m_DeathMatch[1] = child;
				break;
			case "DeathMatch3rd":
				m_DeathMatch[2] = child;
				break;
			}
		}

		UserGuide.RegisterAction(m_UserGuideAction);
	}

	protected override void OnViewDestroy()
	{
		UserGuide.UnregisterAction(m_UserGuideAction);

		StopAllCoroutines();

		base.OnViewDestroy();
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_PrimaryKey = CloudUser.instance.primaryKey;
		m_SkipAnimations = false;

		RegisterButtonDelegate("Close_Button",
							   () =>
							   {
								   Owner.Back();
								   SendResult(E_PopupResultCode.Cancel);
							   },
							   null);
		RegisterButtonDelegate("SkipAnim_Button", () => { m_SkipAnimations = true; }, null);
	}

	protected override void OnViewHide()
	{
		StopAllCoroutines();

		RegisterButtonDelegate("Close_Button", null, null);
		RegisterButtonDelegate("SkipAnim_Button", null, null);

		RegisterButtonDelegate("FB_Button", null, null);
		RegisterButtonDelegate("TW_Button", null, null);

		base.OnViewHide();
	}

	// PRIVATE MEMBERS

	IEnumerator ShowResults(RoundFinalResult results)
	{
		results = results ?? new RoundFinalResult();
		List<RoundFinalResult.PlayerResult> players = results.PlayersScore ?? new List<RoundFinalResult.PlayerResult>();

		E_Team enemy = results.Team == E_Team.Good ? E_Team.Bad : E_Team.Good;
		E_Team winners = results.Winner ? results.Team : enemy;

		// sort players
		if (results.GameType != E_MPGameType.DeathMatch)
		{
			players.Sort((x, y) =>
						 {
							 int res = x.Team.CompareTo(y.Team);
							 if (res == 0)
							 {
								 // descending by score
								 res = y.Score.CompareTo(x.Score);
							 }
							 else
							 {
								 res = x.Team == winners ? -1 : +1;
							 }
							 return res;
						 });
		}

		m_TeamColors[(int)E_Team.None] = m_NoTeamColor;
		m_TeamColors[(int)E_Team.Good] = m_TeamGoodColor;
		m_TeamColors[(int)E_Team.Bad] = m_TeamBadColor;

		ShowResultText(results, winners);
		ShowResultImage(results, winners);

		yield return StartCoroutine(UpdateList(players, winners));

		yield return new WaitForSeconds(0.2f);

		PreyNightmare prey = new PreyNightmare(Layout.GetWidget("Prey"));
		prey.CountDownSound = m_CountDownSound;
		prey.Update(players.Find(obj => obj.PrimaryKey == results.Prey.PrimaryKey), results.Prey);

		PreyNightmare nightmare = new PreyNightmare(Layout.GetWidget("Nightmare"));
		nightmare.CountDownSound = m_CountDownSound;
		nightmare.Update(players.Find(obj => obj.PrimaryKey == results.Nightmare.PrimaryKey), results.Nightmare);

		if (m_SkipAnimations == true)
		{
			MFGuiManager.FlushAnimations();
		}

#if !UNITY_STANDALONE
		int screenShotPlace = 3;
#if FB_LOGIN_REVIEW
		screenShotPlace = 100;
#endif
		if (results.Place < screenShotPlace)
		{
			GUIBase_Widget socNetsRoot = Layout.GetWidget("SocialNetworks", false);
			if (socNetsRoot != null)
			{
				socNetsRoot.Show(true, true);

				RegisterButtonDelegate("FB_Button",
									   null,
									   (inside) =>
									   {
										   if (inside)
											   PostResults(true, results);
									   });
				RegisterButtonDelegate("TW_Button",
									   null,
									   (inside) =>
									   {
										   if (inside)
											   PostResults(false, results);
									   });
			}
		}
#endif
	}

	void ShowResultText(RoundFinalResult results, E_Team winners)
	{
		GUIBase_Label resultText = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, "ResultText_Label");
		GUIBase_Label resultShadow = GuiBaseUtils.GetControl<GUIBase_Label>(Layout, "ResultShadow_Label");
		if (results.GameType == E_MPGameType.ZoneControl)
		{
			Color color = results.Team == E_Team.Good ? m_TeamGoodColor : m_TeamBadColor;
			int[] textIds = results.Team == winners ? m_YouWonTextIds : m_YouLoseTextIds;
			int textId = textIds[Random.Range(0, textIds.Length)];

			resultText.SetNewText(textId);
			resultText.Widget.Color = color;
			resultText.Widget.Show(true, true);

			resultShadow.SetNewText(textId);
			resultShadow.Widget.Show(true, false);
		}
		else
		{
			resultText.Widget.Show(false, true);
			resultShadow.Widget.Show(false, true);
		}
	}

	void ShowResultImage(RoundFinalResult results, E_Team winners)
	{
		GUIBase_Widget widget = null;

		if (results.GameType == E_MPGameType.ZoneControl)
		{
			m_ZoneControl.State = results.Team == winners ? "Win" : "Lose";
			widget = m_ZoneControl.Widget;
		}
		else
		{
			int max = Mathf.Min(results.PlayersScore.Count, m_DeathMatch.Length);
			for (int idx = 0; idx < max; ++idx)
			{
				if (results.PlayersScore[idx].PrimaryKey == m_PrimaryKey)
				{
					widget = m_DeathMatch[idx];
					break;
				}
			}
		}

		if (widget != null)
		{
			widget.ShowImmediate(true, true);
		}
	}

	IEnumerator UpdateList(List<RoundFinalResult.PlayerResult> players, E_Team winners)
	{
		GUIBase_List list = GuiBaseUtils.GetControl<GUIBase_List>(Layout, "Table");
		GUIBase_Widget outline = GuiBaseUtils.GetChild<GUIBase_Widget>(list, "MyPlayer");
		Row[] rows = new Row[list.numOfLines];

		outline.Show(false, true);

		// sort player by score
		RoundFinalResult.PlayerResult[] score = players.ToArray();
		System.Array.Sort(score,
						  (x, y) =>
						  {
							  int res = y.Score.CompareTo(x.Score);
							  if (res == 0)
							  {
								  // descending by kills
								  res = y.Kills.CompareTo(x.Kills);
								  if (res == 0)
								  {
									  // increasing by deaths
									  res = x.Deaths.CompareTo(y.Deaths);
									  if (res == 0)
									  {
										  // increasing by names
										  res = x.NickName.CompareTo(y.NickName);
									  }
								  }
							  }
							  return res;
						  });

		// prepare rows
		E_Team team = E_Team.Last;
		for (int idx = 0; idx < list.numOfLines; ++idx)
		{
			RoundFinalResult.PlayerResult player = idx < players.Count ? players[idx] : null;

			if (player != null && player.Team != team)
			{
				team = player.Team;
			}

			int stand = player != null ? System.Array.FindIndex(score, obj => obj.PrimaryKey == player.PrimaryKey) + 1 : -1;
			rows[idx] = new Row(stand, list.GetWidgetOnLine(idx), player, player != null && player.PrimaryKey == m_PrimaryKey);
		}

		yield return new WaitForSeconds(0.2f);

		// show rows
		for (int idx = 0; idx < list.numOfLines; ++idx)
		{
			rows[idx].Update();

			do
			{
				if (m_SkipAnimations == true)
				{
					MFGuiManager.FlushAnimations();
				}

				yield return new WaitForEndOfFrame();
			} while (MFGuiManager.IsAnimating == true);

			if (rows[idx].PrimaryKey == m_PrimaryKey)
			{
				outline.transform.position = list.GetWidgetOnLine(idx).transform.position;
				outline.SetModify();
				outline.Show(true, true);
			}

			MFGuiManager.Instance.PlayOneShot(m_ShowRowSound);
		}
	}

	void PostResults(bool facebook, RoundFinalResult result)
	{
#if ( !UNITY_EDITOR ) && ( UNITY_ANDROID || UNITY_IPHONE )
		if (Layout.Visible)
		{
			StartCoroutine(PostResults_Coroutine(facebook, result));
		}
#endif
	}

#if( !UNITY_EDITOR ) && ( UNITY_ANDROID || UNITY_IPHONE )
	private IEnumerator PostResults_Coroutine(bool facebook, RoundFinalResult result)
	{
		// hide "social networks" widgets (will be hidden on posted screen-shots)...

		GUIBase_Widget socNetsRoot = Layout.GetWidget("SocialNetworks", true);
		socNetsRoot.ShowImmediate(false, true);

		// take & post screen-shot (with message)...

		yield return new WaitForEndOfFrame();

		int idx;
		int textIndex = result.Place;
#if(FB_LOGIN_REVIEW)
		textIndex = 1;
#endif

		if (result.GameType == E_MPGameType.DeathMatch) {
			idx = TextID_RankingsDM[textIndex];
		}
		else {
			idx = TextID_RankingsZC[textIndex];
		}
		
		string name = CloudUser.instance.primaryKey;
		if (FacebookPlugin.Instance.CurrentUser != null && !string.IsNullOrEmpty(FacebookPlugin.Instance.CurrentUser.Name))
		{
			name = FacebookPlugin.Instance.CurrentUser.Name;
		}

		if (facebook == true)
		{
			byte[] screenshot = takeScreenshot();
			if (screenshot != null)
			{
				yield return StartCoroutine(FacebookPlugin.Instance.Init());
				string requestedScope = "publish_actions";
				if(FacebookPlugin.Instance.IsLoggedIn() == false || FacebookPlugin.Instance.HasPermittedScope(requestedScope) == false)
				{
					yield return StartCoroutine(FacebookPlugin.Instance.Login(requestedScope));
				}
				if(FacebookPlugin.Instance.IsLoggedIn() == true && FacebookPlugin.Instance.HasPermittedScope(requestedScope) == true)
				{
					yield return StartCoroutine(FacebookPlugin.Instance.PostImage(string.Empty, screenshot));
				}
			}
			else
			{
				Debug.LogWarning("Could not capture screenshot!");
			}
		}
		else
		{
#if ( UNITY_ANDROID || UNITY_IPHONE )
			string msg = string.Format(TextDatabase.instance[idx], name, result.MapName);
			TwitterUtils.PostScreenshot(msg, OnPostResult);
#endif		
		}

		// show "social networks" widgets again...

		yield return new WaitForSeconds(1.0f);

#if ( UNITY_ANDROID || UNITY_IPHONE )
		socNetsRoot.ShowImmediate(Layout.Visible, true);
#endif	
	}

	private byte[] takeScreenshot()
	{
		// We should only read the screen after all rendering is complete yield return new WaitForEndOfFrame();
		// Create a texture the size of the screen, RGB24 format
		int width = Screen.width;
		int height = Screen.height;
		Texture2D tex = new Texture2D(width /*- 210*/, height, TextureFormat.RGB24, false);
		// Read screen contents into the texture
		tex.ReadPixels(new Rect(0, 0, width /*- 210*/, height), 0, 0);
		tex.Apply();

		// Encode texture into PNG
		byte[] bytes = tex.EncodeToPNG();
		Destroy(tex);
		return bytes;
	}

	private void OnPostResult( bool Success )
	{
	//	Debug.Log( "CAPA -- SMFinalResults_Screen :: OnPostResultFB( " + Success + " )" );
	}
#endif
}
