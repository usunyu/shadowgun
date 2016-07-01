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

public class GuiGameMessages
{
	public class Message
	{
		public Client.E_MessageType Type;
		public float CaptionScale;
		public float TextScale;
		public float ShowTime;
		public string Caption;
		public string Text;
	}
/**/

	// ---------
	class CombatText
	{
		GUIBase_Widget m_Parent;
		GUIBase_Label m_Caption;
		GUIBase_Label m_Text;
		GUIBase_Widget m_BackGround;
		GUIBase_Widget m_Icon1;
		//GUIBase_Widget	m_Icon2;
		//GUIBase_Widget	m_Icon3;

		float m_CurrentAlpha;
		float m_Speed = 2;
		float m_Progress;
		float m_Delay = 0;

		Queue<Message> MessageQueue = new Queue<Message>();
		Message CurrentMessage = null;

		public bool IsVisible
		{
			get { return CurrentMessage != null || MessageQueue.Count > 0; }
		}

		public float Progress
		{
			get { return m_Progress; }
		}

		~CombatText()
		{
			m_Parent = null;
			m_Caption = null;
			m_Text = null;
			CurrentMessage = null;
			MessageQueue.Clear();
		}

		// -----
		public CombatText(GUIBase_Layout layout)
		{
			m_Parent = layout.GetWidget("NotifyInfo");
			m_Caption = layout.GetWidget("Caption").GetComponent<GUIBase_Label>();
			m_Text = layout.GetWidget("Text").GetComponent<GUIBase_Label>();

			m_BackGround = layout.GetWidget("Bg");
			m_Icon1 = layout.GetWidget("NotifyIcon");
			//m_Icon2 = layout.GetWidget("IconL");
			//m_Icon3 = layout.GetWidget("IconR");

			Hide();
		}

		// -----
		public void Enable(bool enable)
		{
			if (enable && IsVisible)
			{
				m_Parent.Show(true, true);
			}
			else if (enable == false && IsVisible)
				m_Parent.Show(false, true);
		}

		// -----

		public void Add(Client.E_MessageType message, string text)
		{
			switch (message)
			{
			case Client.E_MessageType.Kill:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500200],
					Text = text,
					CaptionScale = 1.2f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.KillAssist:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500201],
					Text = text,
					CaptionScale = 0.9f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.HeadShot:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0503006],
					Text = text,
					CaptionScale = 0.9f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.Turret:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500207],
					Text = text,
					CaptionScale = 0.55f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.Spider:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500208],
					Text = text,
					CaptionScale = 0.7f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.ZoneNeutral:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500203],
					Text = text,
					CaptionScale = 0.6f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.ZoneControl:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500204],
					Text = text,
					CaptionScale = 0.8f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.ZoneDefended:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500202],
					Text = text,
					CaptionScale = 0.7f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.ZoneAttacked:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500209],
					Text = text,
					CaptionScale = 0.65f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.Rank:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500901],
					Text = text,
					CaptionScale = 1.2f,
					TextScale = 0.7f,
					ShowTime = 5.0f
				});
				break;
			case Client.E_MessageType.Unlock:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500902],
					Text = text,
					CaptionScale = 0.9f,
					TextScale = 0.7f,
					ShowTime = 4.0f
				});
				break;
			case Client.E_MessageType.Win:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500903],
					Text = text,
					CaptionScale = 1.2f,
					TextScale = 0.7f,
					ShowTime = 5.0f
				});
				break;
			case Client.E_MessageType.Lost:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500904],
					Text = text,
					CaptionScale = 1.2f,
					TextScale = 0.7f,
					ShowTime = 5.0f
				});
				break;
			case Client.E_MessageType.Ammo:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500205],
					Text = text,
					CaptionScale = 0.7f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.Heal:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500206],
					Text = text,
					CaptionScale = 0.9f,
					TextScale = 0.7f,
					ShowTime = 3.0f
				});
				break;
			case Client.E_MessageType.ExclusiveKill:
				MessageQueue.Enqueue(new Message()
				{
					Type = message,
					Caption = TextDatabase.instance[0500210],
					Text = text,
					CaptionScale = 0.8f,
					TextScale = 1.0f,
					ShowTime = 4.0f
				});
				break;
			default:
				Debug.Log("Unknow type for client Combat Text " + message);
				break;
			}
		}

		// -----
		public void Update()
		{
			if (IsVisible == false || m_Delay > Time.timeSinceLevelLoad)
				return;

			if (CurrentMessage == null)
			{
				CurrentMessage = MessageQueue.Dequeue();
				ShowNewMessage();
			}

			UpdateCurrentMessage();
		}

		void UpdateCurrentMessage()
		{
			if (CurrentMessage == null)
				return;

			m_Progress = Mathf.Min(m_Progress + Time.deltaTime*m_Speed, CurrentMessage.ShowTime);

			float alpha;

			if (m_Progress < 0.5f)
				alpha = Mathfx.Sinerp(0, 1, m_Progress*2);
			else if (m_Progress > (CurrentMessage.ShowTime - 0.5f))
				alpha = 1 - Mathfx.Sinerp(0, 1, (m_Progress - (CurrentMessage.ShowTime - 0.5f))*2);
			else
				alpha = 1.0f;

			m_Parent.FadeAlpha = alpha;
			m_Caption.Widget.FadeAlpha = alpha;
			m_Text.Widget.FadeAlpha = alpha;
			m_BackGround.FadeAlpha = alpha;
			m_Icon1.FadeAlpha = alpha;
			//m_Icon2.FadeAlpha = alpha;
			//m_Icon3.FadeAlpha = alpha;

			if (alpha <= 0)
				Hide();
		}

		void ShowNewMessage()
		{
			m_Parent.Show(true, true);
			m_Caption.SetNewText(CurrentMessage.Caption);
			m_Caption.Widget.transform.localScale = new Vector3(CurrentMessage.CaptionScale, CurrentMessage.CaptionScale, CurrentMessage.CaptionScale);
			m_Text.SetNewText(CurrentMessage.Text);
			m_Text.Widget.transform.localScale = new Vector3(CurrentMessage.TextScale, CurrentMessage.TextScale, CurrentMessage.TextScale);

			m_Progress = 0;

			if (CurrentMessage.Type == Client.E_MessageType.ExclusiveKill)
				Client.Instance.PlaySoundCombatMessageGold();
			else
				Client.Instance.PlaySoundCombatMessage();
		}

		// -----
		void Hide()
		{
			m_Parent.Show(false, true);
			m_Delay = Time.timeSinceLevelLoad + 0.2f;
			CurrentMessage = null;
		}
	}

	CombatText CombatInfo;

	string s_PivotMainName = "MainHUD";
	string s_LayoutMainName = "HUD_Layout";
	string s_Parent = "CombatInfo";

	// ---------------------------------------------------------------------------------------------------------------------------------
	// 						P U B L I C      P A R T
	// ---------------------------------------------------------------------------------------------------------------------------------
	// ---------
	public void Init()
	{
		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		GUIBase_Layout layout = pivot.GetLayout(s_LayoutMainName);

		layout.GetWidget(s_Parent).GetComponent<GUIBase_Widget>();
		CombatInfo = new CombatText(layout);
	}

	public void Destroy()
	{
		CombatInfo = null;
	}

	// ---------
	public void Update()
	{
		CombatInfo.Update();
	}

	// -------
	public void AddNewMessage(Client.E_MessageType type, string text)
	{
		CombatInfo.Add(type, text);
	}
}
