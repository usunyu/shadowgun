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

public class ScreenComponent_ShortStats : ScreenComponent
{
	class Table
	{
		GUIBase_Widget m_RootWidget;
		GUIBase_Label m_NameLabel;
		GUIBase_Label m_RankLabel;
		GUIBase_MultiSprite m_RankSprite;
		GUIBase_Label m_KilledMeLabel;
		GUIBase_Label m_KilledByMeLabel;
		GUIBase_Widget m_FriendIcon;

		public Table(GUIBase_Widget inRootWidget)
		{
			m_RootWidget = inRootWidget;

			GUIBase_Label[] labels = m_RootWidget.GetComponentsInChildren<GUIBase_Label>();
			GUIBase_MultiSprite[] sprites = m_RootWidget.GetComponentsInChildren<GUIBase_MultiSprite>();

			foreach (GUIBase_Label l in labels)
			{
				if (l.name == "RankNum")
					m_RankLabel = l;
				else if (l.name == "Name")
					m_NameLabel = l;
				else if (l.name == "KilledMe")
					m_KilledMeLabel = l;
				else if (l.name == "KilledByMe")
					m_KilledByMeLabel = l;
			}

			foreach (GUIBase_MultiSprite s in sprites)
			{
				if (s.name == "RankPic")
					m_RankSprite = s;
			}

			m_FriendIcon = GuiBaseUtils.GetChild<GUIBase_Widget>(m_RootWidget, "FriendIcon");
		}

		public void Update(PlayerPersistantInfo inPPI, int inKilledMe, int inKilledByMe)
		{
			if (m_RankSprite != null)
				m_RankSprite.State = "Rank_" + Mathf.Min(inPPI.Rank, m_RankSprite.Count - 1).ToString("D2");

			if (m_RankLabel != null)
				m_RankLabel.SetNewText(inPPI.Rank.ToString());

			if (m_NameLabel != null)
				m_NameLabel.SetNewText(inPPI.NameForGui);

			if (m_KilledMeLabel != null)
				m_KilledMeLabel.SetNewText(inKilledMe.ToString());

			if (m_KilledByMeLabel != null)
				m_KilledByMeLabel.SetNewText(inKilledByMe.ToString());

			if (m_FriendIcon != null)
			{
				bool isFriend = GameCloudManager.friendList.friends.FindIndex(obj => obj.PrimaryKey == inPPI.PrimaryKey) != -1;
				if (m_FriendIcon.Visible != isFriend)
				{
					m_FriendIcon.Show(isFriend, true);
				}
			}
		}

		public void Show()
		{
			if (m_RankSprite != null)
				m_RankSprite.Widget.Show(true, true);

			if (m_RankLabel != null)
				m_RankLabel.Widget.Show(true, true);

			if (m_NameLabel != null)
				m_NameLabel.Widget.Show(true, true);

			if (m_KilledMeLabel != null)
				m_KilledMeLabel.Widget.Show(true, true);

			if (m_KilledByMeLabel != null)
				m_KilledByMeLabel.Widget.Show(true, true);
		}

		public void Hide()
		{
			if (m_RankSprite != null)
				m_RankSprite.Widget.Show(false, true);

			if (m_RankLabel != null)
				m_RankLabel.Widget.Show(false, true);

			if (m_NameLabel != null)
				m_NameLabel.Widget.Show(false, true);

			if (m_KilledMeLabel != null)
				m_KilledMeLabel.Widget.Show(false, true);

			if (m_KilledByMeLabel != null)
				m_KilledByMeLabel.Widget.Show(false, true);

			if (m_FriendIcon != null)
				m_FriendIcon.Show(false, true);
		}
	};

	// =================================================================================================================

	Table m_MyBestVictim;
	Table m_MyBestKiller;

	public override string ParentName
	{
		get { return "ShortStats"; }
	}

	public override float UpdateInterval
	{
		get { return 0.5f; }
	}

	// =================================================================================================================

	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		InitTables();
		return true;
	}

	protected override void OnShow()
	{
		base.OnShow();
		UpdateTables();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		UpdateTables();
	}

	void InitTables()
	{
		GUIBase_Widget vRoot = Parent.transform.FindChildByName("Prey").GetComponent<GUIBase_Widget>();
		GUIBase_Widget kRoot = Parent.transform.FindChildByName("Nightmare").GetComponent<GUIBase_Widget>();

		if (vRoot == null)
		{
			Debug.LogError("Widget 'Prey' in '" + Parent.name + "' not found!");
			return;
		}
		if (kRoot == null)
		{
			Debug.LogError("Widget 'Nightmare' in '" + Parent.name + "' not found!");
			return;
		}

		m_MyBestVictim = new Table(vRoot);
		m_MyBestKiller = new Table(kRoot);
	}

	void UpdateTables()
	{
		PlayerPersistantInfo player = PPIManager.Instance.GetLocalPlayerPPI();

		if (player == null)
			return;

		int killedMe = 0;
		int killedByMe = 0;

		if (m_MyBestVictim != null)
		{
			PlayerPersistantInfo victim = PPILocalStats.GetBestVictim(player, ref killedByMe);

			if (victim != null)
			{
				killedMe = PPILocalStats.GetKills(victim, player);

				m_MyBestVictim.Show();
				m_MyBestVictim.Update(victim, killedMe, killedByMe);
			}
			else
			{
				m_MyBestVictim.Hide();
			}
		}

		if (m_MyBestKiller != null)
		{
			PlayerPersistantInfo killer = PPILocalStats.GetBestKiller(player, ref killedMe);

			if (killer != null)
			{
				killedByMe = PPILocalStats.GetKills(player, killer);

				m_MyBestKiller.Show();
				m_MyBestKiller.Update(killer, killedMe, killedByMe);
			}
			else
			{
				m_MyBestKiller.Hide();
			}
		}
	}
}
