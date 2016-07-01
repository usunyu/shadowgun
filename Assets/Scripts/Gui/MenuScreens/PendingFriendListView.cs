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
using PendingFriendInfo = FriendList.PendingFriendInfo;

// =============================================================================================================================
// =============================================================================================================================
public class PendingFriendListView : BaseListView
{
	//	ListView<PendingFriendListView.TestClass> {

	readonly static int MESSAGE_MAX_LENGTH = 47;

/*	
	public class TestClass
	{
		public int a;
		public int b;
	}
	
	protected override TestClass 	GetNewTItem { get { return new TestClass(); } }	
*/
	internal class FriendLine
	{
		// FriendShip status ...	
		const int FS_REQUEST = 02040218; //	wants be your friend
		const int FS_PENDING = 02040219; //	friendship approval pending
		const int LOI_UNKNOWN = 02040221; //	UNKNOWN

		PendingFriendInfo m_FriendInfo;

		GUIBase_Widget m_Line;

		GUIBase_Label m_Username;
		GUIBase_Label m_Status;
		GUIBase_Label m_Added;
		GUIBase_Button m_Accept;
		GUIBase_Button m_Reject;
		GUIBase_Button m_Remove;

		GUIBase_Label m_FacebookName;
		GUIBase_Widget m_FacebookIcon;

		public Vector3 spritePos
		{
			get { return m_Line.transform.position; }
		}

		public FriendLine(GUIBase_Widget inLine)
		{
			m_Line = inLine;

			Transform trans = inLine.transform;

			m_Username = trans.GetChildComponent<GUIBase_Label>("Username");
			m_Status = trans.GetChildComponent<GUIBase_Label>("Status");
			m_Added = trans.transform.GetChildComponent<GUIBase_Label>("Added");
			m_Accept = trans.transform.GetChildComponent<GUIBase_Button>("Accept_Button");
			m_Reject = trans.transform.GetChildComponent<GUIBase_Button>("Reject_Button");
			m_Remove = trans.transform.GetChildComponent<GUIBase_Button>("Remove_Button");
			m_FacebookName = trans.GetChildComponent<GUIBase_Label>("FacebookName");
			m_FacebookIcon = trans.GetChildComponent<GUIBase_Widget>("FacebookIcon");

			m_Accept.RegisterTouchDelegate(Delegate_Accept);
			m_Reject.RegisterTouchDelegate(Delegate_Reject);
			m_Remove.RegisterTouchDelegate(Delegate_Remove);
		}

		public void Update(PendingFriendInfo inFriend)
		{
			m_FriendInfo = inFriend;

			// update GUI...

			{
				m_FacebookIcon.Show(false, false);
				m_FacebookName.Widget.Show(false, false);
				m_Username.Widget.Show(true, false);

				string username = string.IsNullOrEmpty(m_FriendInfo.Username_New) ? m_FriendInfo.PrimaryKey : m_FriendInfo.Username_New;
				string nickname = string.IsNullOrEmpty(m_FriendInfo.Nickname) ? username : m_FriendInfo.Nickname;

				m_Username.SetNewText(GuiBaseUtils.FixNameForGui(nickname));
			}

			string added = GetLastOnlineInfo(m_FriendInfo);
			m_Added.SetNewText(added);

			bool beMyFriendRequest = m_FriendInfo.IsItRequest;

			string message = string.IsNullOrEmpty(m_FriendInfo.Message) ? TextDatabase.instance[FS_REQUEST] : m_FriendInfo.Message;
			string status = beMyFriendRequest ? message : TextDatabase.instance[FS_PENDING];
			if (status.Length > MESSAGE_MAX_LENGTH)
			{
				status = status.Substring(0, MESSAGE_MAX_LENGTH - 3) + "...";
			}

			m_Status.SetNewText(status);
			m_Accept.Widget.Show((beMyFriendRequest ? true : false), true);
			m_Reject.Widget.Show((beMyFriendRequest ? true : false), true);
			m_Remove.Widget.Show((beMyFriendRequest ? false : true), true);

			//Debug.Log("show PPI " + inPPI.Name + " " + inPPI.Score.Score.ToString());
		}

		public void Show()
		{
			if (m_Line.Visible == false)
			{
				m_Line.Show(true, true);
			}
		}

		public void Hide()
		{
			m_Line.Show(false, true);
		}

		string GetLastOnlineInfo(PendingFriendInfo inFriend)
		{
			double addedDate = inFriend.AddedDate;
			return addedDate <= 0 ? TextDatabase.instance[LOI_UNKNOWN] : GuiBaseUtils.EpochToString(addedDate);
		}

		void Delegate_Accept()
		{
			GameCloudManager.friendList.AcceptFriendRequest(m_FriendInfo.PrimaryKey);
		}

		void Delegate_Reject()
		{
			GameCloudManager.friendList.RejectFriendRequest(m_FriendInfo.PrimaryKey);
		}

		void Delegate_Remove()
		{
			GameCloudManager.friendList.RemovePendingFriendRequest(m_FriendInfo.PrimaryKey);
		}
	};

	// -------------------------------------------------------------------------------------------------------------------------
	FriendLine[] m_GuiLines;
	PendingFriendInfo[] m_Friends = new PendingFriendInfo[0];
	GUIBase_List m_Table;
	GUIBase_Layout m_View;

	int m_FirstVisibleIndex = 0;
	bool isUpdateNeccesary { get; set; }

	// =========================================================================================================================
	// === public interface ====================================================================================================
	public void GUIView_Init(GUIBase_Layout inView, GUIBase_List inList)
	{
		m_View = inView;
		m_Table = GuiBaseUtils.GetControl<GUIBase_List>(m_View, "Table");

		InitGuiLines(inList);
	}

	public override void GUIView_Show()
	{
		if (m_View.Visible == true)
			return;

		isUpdateNeccesary = true;

		m_Table.OnUpdateRow += OnUpdateTableRow;

		MFGuiManager.Instance.ShowLayout(m_View, true);

		GameCloudManager.friendList.PendingFriendListChanged += OnFriendListChanged;
	}

	public override void GUIView_Hide()
	{
		if (m_View.Visible == false)
			return;

		m_Table.OnUpdateRow -= OnUpdateTableRow;

		GameCloudManager.friendList.PendingFriendListChanged -= OnFriendListChanged;

		MFGuiManager.Instance.ShowLayout(m_View, false);
	}

	public override void GUIView_Update()
	{
		if (isUpdateNeccesary == false)
			return;

		UpdateView();
		isUpdateNeccesary = false;
	}

	public override GUIBase_Widget GUIView_HitTest(ref Vector2 point)
	{
		if (m_View != null)
			return m_View.HitTest(ref point);

		return null;
	}

	public override bool GUIView_ProcessInput(ref IInputEvent evt)
	{
		if (m_View != null)
			return m_View.ProcessInput(ref evt);

		return false;
	}

	// =========================================================================================================================
	// === MonoBehaviour interface =============================================================================================

	// !!!!!! DON"T USE Standard MonoBehaviour Update functions. This view is controled from parrent object...

	// =========================================================================================================================
	// === private part ========================================================================================================
	void InitGuiLines(GUIBase_List inParent)
	{
		if (inParent.numOfLines <= 0)
		{
			Debug.LogError("inParent.numOfLines = " + inParent.numOfLines);
			return;
		}

		m_GuiLines = new FriendLine[inParent.numOfLines];
		for (int i = 0; i < inParent.numOfLines; i++)
		{
			m_GuiLines[i] = new FriendLine(inParent.GetWidgetOnLine(i));
		}

		//inParent.Widget.NotifyLayoutChanged(true, true);
	}

	void OnUpdateTableRow(GUIBase_Widget widget, int rowIndex, int itemIndex)
	{
		FriendLine row = m_GuiLines[rowIndex];

		if (itemIndex < m_Friends.Length)
		{
			row.Show();
			row.Update(m_Friends[itemIndex]);
		}
		else
		{
			row.Hide();
		}
	}

	void UpdateView()
	{
		List<PendingFriendInfo> allFriends = GameCloudManager.friendList.pendingFriends;

		m_Friends = allFriends.ToArray();
		System.Array.Sort(m_Friends, (x, y) => { return x.AddedDate.CompareTo(y.AddedDate)*-1; });

		m_Table.MaxItems = m_Friends.Length;
		m_Table.Widget.SetModify();
	}

	void OnFriendListChanged(object sender, System.EventArgs e)
	{
		isUpdateNeccesary = true;
	}

	// =========================================================================================================================
	// === internal gui delegates ==============================================================================================
	void Delegate_Prev(GUIBase_Widget inInstigator)
	{
		m_FirstVisibleIndex -= m_GuiLines.Length;
		isUpdateNeccesary = true;

		// debug code...
		if (m_FirstVisibleIndex < 0)
		{
			m_FirstVisibleIndex = 0;
			Debug.LogError("Internal error, inform alex");
		}
	}

	void Delegate_Next(GUIBase_Widget inInstigator)
	{
		m_FirstVisibleIndex += m_GuiLines.Length;
		isUpdateNeccesary = true;

		// debug code...		
		List<PendingFriendInfo> allFriends = GameCloudManager.friendList.pendingFriends;
		if ((m_FirstVisibleIndex%m_GuiLines.Length) != 0 || m_FirstVisibleIndex >= allFriends.Count)
		{
			m_FirstVisibleIndex = m_GuiLines.Length*((int)(allFriends.Count/m_GuiLines.Length));
			Debug.LogError("Internal error, inform alex");
		}
	}
}
