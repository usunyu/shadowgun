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
using DateTime = System.DateTime;
using Exception = System.Exception;
using System.Runtime.Serialization;
using LitJson;

// =====================================================================================================================
// =====================================================================================================================
public class MFUnknownMesageTypeException : UnityException
{
	public MFUnknownMesageTypeException() : base("Unknown message")
	{
	}

	public MFUnknownMesageTypeException(string message) : base(message)
	{
	}

	public MFUnknownMesageTypeException(string message, Exception innerException) : base(message, innerException)
	{
	}

	protected MFUnknownMesageTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class CloudMailbox : MonoBehaviour
{
	public enum E_Mailbox
	{
		Unknown = -1,
		Global,
		Product
	};

	// =================================================================================================================
	public class BaseMessage
	{
		public string msgType
		{
			get { return GetType().Name; }
		}

		public virtual bool isValid
		{
			get { return true; }
		} // TODO ::

		public virtual bool isSpecialMessage
		{
			get { return false; }
		} // if it is special message, it will not be shown to user

		public E_Mailbox m_Mailbox; // is this message from/for global user or product mailbox?
		public string m_Sender; // the sender of this message.
		public string m_Message; // The content of message.
		public string m_RAWMessage; // raw (full) message. usually JSON string, used for debug.
		public DateTime m_SendTime; // time when message was sent.

		public bool m_IsRead; // user read this message already

		// .............................................................................................................
		// Constructors..
		public BaseMessage()
		{
		}

		public BaseMessage(string inRawString, E_Mailbox inMailbox, JsonData inJsonData = null)
		{
			m_Mailbox = inMailbox;
			m_RAWMessage = inRawString;

			if (inJsonData != null)
			{
				if (inJsonData.HasValue("m_Sender"))
				{
					m_Sender = (string)inJsonData["m_Sender"];
				}

				if (inJsonData.HasValue("m_Message"))
				{
					m_Message = (string)inJsonData["m_Message"];
				}
				//m_SendTime      = JsonMapper.ToObject<DateTime>( inJsonData["m_SendTime"].ToString() );

				if (inJsonData.HasValue("m_SendTime"))
				{
					m_SendTime = DateTime.Parse(inJsonData["m_SendTime"].ToString());
				}
			}
		}
	}

	// =================================================================================================================
	public class SystemCommand : BaseMessage
	{
		public override bool isSpecialMessage
		{
			get { return true; }
		} // this is special message, it will not be shown to user

		public string m_TargetSystem;

		// .............................................................................................................
		// Constructors..
		public SystemCommand()
		{
		}

		public SystemCommand(string inRawString, E_Mailbox inMailbox, JsonData inJsonData = null)
						: base(inRawString, inMailbox, inJsonData)
		{
			m_TargetSystem = (string)inJsonData["m_TargetSystem"];
		}
	}

	// =================================================================================================================
	public class FriendRequest : SystemCommand
	{
		public string m_Username; // the sender of this message.
		public string m_NickName; // the sender of this message.
		public string m_ConfirmCommand;

		// .............................................................................................................
		// Constructors..
		public FriendRequest()
		{
		}

		public FriendRequest(string inRawString, E_Mailbox inMailbox, JsonData inJsonData = null)
						: base(inRawString, inMailbox, inJsonData)
		{
			try
			{
				m_Username = (string)inJsonData["m_Username"];
			}
			catch
			{
				m_Username = m_Sender;
			}

			try
			{
				m_NickName = (string)inJsonData["m_NickName"];
			}
			catch
			{
				m_NickName = m_Sender;
			}

			m_ConfirmCommand = (string)inJsonData[CloudServices.RESPONSE_PROP_ID_SERVER_CMD];
		}
	}

	// =================================================================================================================
	public class FriendRequestReject : SystemCommand
	{
		public string m_NickName; // the sender of this message.

		// .............................................................................................................
		// Constructors..
		public FriendRequestReject()
		{
		}

		public FriendRequestReject(string inRawString, E_Mailbox inMailbox, JsonData inJsonData = null)
						: base(inRawString, inMailbox, inJsonData)
		{
			try
			{
				m_NickName = (string)inJsonData["m_NickName"];
			}
			catch
			{
			}
		}
	}

	// =================================================================================================================
	public class FriendMessage : BaseMessage
	{
		public string m_NickName; // the sender of this message.

		// .............................................................................................................
		// Constructors..
		public FriendMessage()
		{
		}

		public FriendMessage(string inRawString, E_Mailbox inMailbox, JsonData inJsonData = null)
						: base(inRawString, inMailbox, inJsonData)
		{
			try
			{
				m_NickName = (string)inJsonData["m_NickName"];
			}
			catch
			{
			}
		}
	}

	// =================================================================================================================
	public class SystemMessage : BaseMessage
	{
		// .............................................................................................................
		// Constructors..
		public SystemMessage()
		{
		}

		public SystemMessage(string inRawString, E_Mailbox inMailbox, JsonData inJsonData = null)
						: base(inRawString, inMailbox, inJsonData)
		{
		}
	}

	// =================================================================================================================
	public class NewsMessage : SystemCommand
	{
		public string m_Subject;
		public string m_HeadLine;
		public DateTime m_ExpirationTime;

		// .............................................................................................................
		// Constructors..
		public NewsMessage()
		{
		}

		public NewsMessage(string inRawString, E_Mailbox inMailbox, JsonData inJsonData = null)
						: base(inRawString, inMailbox, inJsonData)
		{
			m_Subject = (string)inJsonData["m_Subject"];
			m_HeadLine = (string)inJsonData["m_HeadLine"];
			//m_ExpirationTime = JsonMapper.ToObject<DateTime>( inJsonData["m_ExpirationTime"].ToString() );
			m_ExpirationTime = DateTime.Parse(inJsonData["m_ExpirationTime"].ToString());
		}
	}

	// =================================================================================================================
	public class ResetResearch : SystemCommand
	{
		// .............................................................................................................
		// Constructors..
		public ResetResearch()
		{
		}

		public ResetResearch(string inRawString, E_Mailbox inMailbox, JsonData inJsonData = null)
						: base(inRawString, inMailbox, inJsonData)
		{
		}
	}

	// =================================================================================================================

	// -----------------------------------------------------------------------------------------------------------------
	List<BaseMessage> m_Inbox = new List<BaseMessage>();
	List<BaseMessage> m_Outbox = new List<BaseMessage>();
	int m_LastMessageIndexFromProductInbox = 0;
	string m_PrimaryKey;

	bool m_MessageFetchInProggres = false;

	// -----------------------------------------------------------------------------------------------------------------
	const int SKIP_UPDATE_TIMEOUT = 5;
	System.DateTime m_LastSyncTime;

	// -----------------------------------------------------------------------------------------------------------------
	public delegate void MailboxEventHandler(BaseMessage[] messages);
	static MailboxEventHandler m_InboxChanged;

	public static event MailboxEventHandler inboxChanged
	{
		add
		{
			if (value != null)
			{
				m_InboxChanged -= value; // just to be sure we don't have any doubles
				m_InboxChanged += value;
				value(GameCloudManager.mailbox.m_Inbox.ToArray()); // call delegate when registering so it recieves current state
			}
		}
		remove { m_InboxChanged -= value; }
	}

	// =================================================================================================================
	// === public interface ============================================================================================
	public void FetchMessages(bool inSkipTimeOutCheck = false)
	{
		// ...................................
		// check preconditions...
		if (m_MessageFetchInProggres == true)
			return;

		if (CloudUser.instance.isUserAuthenticated == false)
		{
			Debug.LogError("sender is not authenticated, this message can't be send");
			return;
		}

		if (inSkipTimeOutCheck == false)
		{
			if (Mathf.Abs((float)(m_LastSyncTime - CloudDateTime.UtcNow).TotalMinutes) < SKIP_UPDATE_TIMEOUT)
			{
				return; // don't update mailbox from cloud
			}
		}

		// ...................................
		// OK we can fetch new messages...
		m_LastSyncTime = CloudDateTime.UtcNow;
		m_MessageFetchInProggres = true;

		StartCoroutine(FetchAllMessages_Corutine());
	}

	public void SendMessage(string inRecipient, string inMessageBody, E_Mailbox inMailbox = E_Mailbox.Global)
	{
		// ...................................
		// check preconditions...	
		if (string.IsNullOrEmpty(inRecipient) == true)
		{
			Debug.LogError("Invalid recipient " + inRecipient);
			return;
		}

		if (string.IsNullOrEmpty(inMessageBody) == true)
		{
			Debug.LogError("Invalid message " + inMessageBody);
			return;
		}

		if (CloudUser.instance.isUserAuthenticated == false)
		{
			Debug.LogError("sender is not authenticated, this message can't be send");
			return;
		}

		// ...................................
		// create message object...
		BaseMessage msg = new BaseMessage();
		msg.m_Mailbox = inMailbox;
		msg.m_Sender = CloudUser.instance.primaryKey;
		msg.m_Message = inMessageBody;

		// and send it.
		SendMessage(inRecipient, msg);
	}

	public void SendMessage(string inRecipient, BaseMessage inMessage)
	{
		// ...................................
		// check preconditions...
		if (string.IsNullOrEmpty(inRecipient) == true)
		{
			Debug.LogError("Invalid recipient " + inRecipient);
			return;
		}

		if (inMessage == null || inMessage.isValid == false)
		{
			Debug.LogError("Invalid message " + inMessage);
			return;
		}

		// ...................................
		inMessage.m_SendTime = CloudDateTime.UtcNow;

		// ...................................
		// convert message to JSON string...
		string message = JsonMapper.ToJson(inMessage);

		// ...................................
		// create send message action and send it via GameCloudManager
		//Debug.Log("SendMessage: " + message);
		if (inMessage is FriendRequest)
		{
			SendMessage action = new SendFriendRequestMessage(CloudUser.instance.authenticatedUserID, inRecipient, message);
			GameCloudManager.AddAction(action);
		}
		else
		{
			bool globalMailbox = (inMessage.m_Mailbox == E_Mailbox.Global);
			SendMessage action = new SendMessage(CloudUser.instance.authenticatedUserID, inRecipient, message, globalMailbox);
			GameCloudManager.AddAction(action);
		}

		// ...................................
		// Save out going message into outbox...
		m_Outbox.Add(inMessage);
		Save();
	}

	public void RemoveMessage(BaseMessage message)
	{
		if (message == null)
			return;

		int idx = m_Inbox.FindIndex(obj => obj.m_RAWMessage == message.m_RAWMessage);
		if (idx == -1)
			return;

		m_Inbox.RemoveAt(idx);
		Save();

		OnInboxChanged();
	}

	public void SetMessageReadState(BaseMessage message, bool state)
	{
		if (message == null)
			return;

		BaseMessage msg = m_Inbox.Find(obj => obj.m_RAWMessage == message.m_RAWMessage);
		if (msg == null)
			return;
		if (msg.m_IsRead == state)
			return;

		msg.m_IsRead = state;
		Save();

		OnInboxChanged();
	}

	void OnUserAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			m_PrimaryKey = CloudUser.instance.primaryKey;
			Load();
		}
		else
		{
			m_PrimaryKey = string.Empty;
			m_LastSyncTime = new System.DateTime();
		}
	}

	// =================================================================================================================
	// === MonoBehaviour interface =====================================================================================
	void Awake()
	{
		CloudUser.authenticationChanged += OnUserAuthenticationChanged;
	}

	void OnDestroy()
	{
		CloudUser.authenticationChanged -= OnUserAuthenticationChanged;

		m_Inbox = null;
		m_Outbox = null;
	}

	// =================================================================================================================
	// === internal ====================================================================================================
	IEnumerator FetchAllMessages_Corutine()
	{
		//Debug.LogWarning("FetchAllMessages_Corutine");

		// fetch messages from global product inbox...
		yield return StartCoroutine(FetchProductMessages_Corutine());

		// fetch messages from global user inbox...
		yield return StartCoroutine(FetchMessages_Corutine(true));

		// fetch messages from product user inbox...
		yield return StartCoroutine(FetchMessages_Corutine(false));

		m_MessageFetchInProggres = false;
	}

	IEnumerator FetchProductMessages_Corutine(string inMailboxName = null)
	{
		// construct special user needed for accessing global per product inbox...
		UnigueUserID product_user = new UnigueUserID("", "", PPIManager.ProductID);

		// Create action for fetching messages from global product inbox...
		BaseCloudAction action = new GetProductGlobalMessages(product_user, m_LastMessageIndexFromProductInbox, inMailboxName);
		GameCloudManager.AddAction(action);

		// wait for action finish...
		while (action.isDone == false)
			yield return new WaitForSeconds(0.2f);

		// end with error if action was not succesfully...
		if (action.isFailed == true)
		{
			Debug.LogError("Can't obtain messages " + action.result);
			yield break;
		}

		// try to process returned result and save Inbox if there are new messages...
		int lastMessageIndex = ProcessMessages(action.result, true);
		if (m_LastMessageIndexFromProductInbox < lastMessageIndex)
		{
			m_LastMessageIndexFromProductInbox = lastMessageIndex;
			Save();

			OnInboxChanged();
		}
	}

	IEnumerator FetchMessages_Corutine(bool inGlobalInbox, bool inRemoveMessagesFromServer = true)
	{
		BaseCloudAction action = new GetMessagesFromInbox(CloudUser.instance.authenticatedUserID, inGlobalInbox);
		GameCloudManager.AddAction(action);

		// wait for authentication...
		while (action.isDone == false)
			yield return new WaitForSeconds(0.2f);

		int lastMessageIndex = -1;
		if (action.isFailed == true)
		{
			Debug.LogError("Can't obtain messages " + action.result);
		}
		else
		{
			//Debug.Log("messages are here " + action.result);
			lastMessageIndex = ProcessMessages(action.result, inGlobalInbox);
		}

		Save();

		if (lastMessageIndex > 0)
		{
			OnInboxChanged();
		}

		if (lastMessageIndex <= 0 || inRemoveMessagesFromServer == false)
			yield break;

		// --- remove processed messages from inbox..
		action = new RemoveMessagesFromInbox(CloudUser.instance.authenticatedUserID, inGlobalInbox, lastMessageIndex);
		GameCloudManager.AddAction(action);

		// wait for authentication...
		while (action.isDone == false)
			yield return new WaitForSeconds(0.2f);

		if (action.isFailed == true)
		{
			Debug.LogError("messages wasn't removed correctly " + action.result);
		}
		else
		{
			//Debug.Log("messages removed " + action.result);
		}
	}

	int ProcessMessages(string inRawMessageFromCloud, bool inGlobalInbox)
	{
		JsonData responseData = JsonMapper.ToObject(inRawMessageFromCloud);

		string[] messages = JsonMapper.ToObject<string[]>((string)responseData["messages"]);
		int lastMsgIdx = (int)responseData[CloudServices.RESPONSE_PROP_ID_LAST_MSG_IDX];

		foreach (string curr in messages)
		{
			BaseMessage message = ProcessMessage(curr, inGlobalInbox);
			if (message != null)
			{
				m_Inbox.Add(message);
			}
		}

		return lastMsgIdx;
	}

	BaseMessage ProcessMessage(string inRawMessage, bool inGlobalInbox)
	{
		//Debug.Log( inRawMessage );

		try
		{
			// Construct message from raw string...
			BaseMessage message = ConstructMessage(inRawMessage, (inGlobalInbox ? E_Mailbox.Global : E_Mailbox.Product));

			// If this message is System command deliver it into specified target system...
			if (message is SystemCommand)
			{
				SystemCommand sys_command = (SystemCommand)message;
				switch (sys_command.m_TargetSystem)
				{
				case "Game.FriendList":
					GameCloudManager.friendList.ProcessMessage(message);
					break;
				case "Game.News":
					GameCloudManager.news.ProcessMessage(message as NewsMessage);
					break;
				case "Game.ResetResearch":
					UserGuideAction_ResetResearch.NotifyUser = true;
					//Debug.LogWarning(" ### Game.ResetResearch ### ");
					break;
				case "Game.AccountExtended":
					Debug.LogWarning(" ### Game.AccountExtended ### ");
					break;
				default:
					Debug.LogError("Unknown target system " + ((SystemCommand)message).m_TargetSystem);
					break;
				}
			}
			else if (message is FriendMessage)
			{
				if (GameCloudManager.friendList.friends.Find(obj => obj.PrimaryKey == message.m_Sender) == null)
				{
					// ignore message from non-friend user
					return null;
				}
			}

			return message;
		}
		catch
		{
			Debug.LogWarning("Error during message processing. See callstack for more info.");

			//Construct simple message in this case...
			return new BaseMessage(inRawMessage, (inGlobalInbox ? E_Mailbox.Global : E_Mailbox.Product), null);
		}
	}

	static BaseMessage ConstructMessage(string inRawMessage, E_Mailbox inInboxType)
	{
		JsonData msgData = null;

		// try to process input string and check if it is in JSON format...
		try
		{
			msgData = JsonMapper.ToObject(inRawMessage);
			if (inInboxType == E_Mailbox.Unknown)
			{
				if (msgData.HasValue("m_Mailbox") == true)
				{
					inInboxType = (E_Mailbox)(int)msgData["m_Mailbox"];
				}
			}
		}
		catch
		{
			Debug.LogWarning("Message is not a JSON object");
		}

		//Check for old legacy messages. This code is here only for back compatibility...
		if (msgData == null || msgData.HasValue("msgType") == false)
		{
			Debug.Log("Old Message:\n" + inRawMessage);
			return new BaseMessage(inRawMessage, inInboxType, null);
		}

		// Construct and init message by msg type...
		switch (msgData["msgType"].ToString())
		{
		case "BaseMessage":
			return new BaseMessage(inRawMessage, inInboxType, msgData);
		case "FriendRequest":
			return new FriendRequest(inRawMessage, inInboxType, msgData);
		case "FriendRequestReject":
			return new FriendRequestReject(inRawMessage, inInboxType, msgData);
		case "FriendMessage":
			return new FriendMessage(inRawMessage, inInboxType, msgData);
		case "NewsMessage":
			return new NewsMessage(inRawMessage, inInboxType, msgData);
		case "ResetResearch":
			return new ResetResearch(inRawMessage, inInboxType, msgData);
		case "SystemMessage":
			return new SystemMessage(inRawMessage, inInboxType, msgData);
		default:
			throw new MFUnknownMesageTypeException();
		}
	}

	// =================================================================================================================

	#region --- internal ...    

	void Save()
	{
		DictionaryFile file = GetFile();
		if (file == null)
			return;

		SaveInbox(file);
		SaveOutbox(file);

		file.Save();
	}

	void SaveInbox(DictionaryFile file)
	{
		string messages = ConvertToString(m_Inbox);

		file.SetString("inbox", messages);
		file.SetInt("lastMessageIndexFromProductInbox", m_LastMessageIndexFromProductInbox);
	}

	void SaveOutbox(DictionaryFile file)
	{
		string messages = ConvertToString(m_Outbox);
		file.SetString("outbox", messages);
	}

	void removeOldestMessage(ref List<BaseMessage> messageList)
	{
		int oldestMessageIndex = -1;
		DateTime oldestMessageTime = DateTime.MaxValue;
		for (int i = 0; i < messageList.Count; i++)
		{
			if (messageList[i].m_SendTime < oldestMessageTime)
			{
				oldestMessageTime = messageList[i].m_SendTime;
				oldestMessageIndex = i;
			}
		}
		if (oldestMessageIndex != -1)
		{
			messageList.RemoveAt(oldestMessageIndex);
		}
	}

	void Load()
	{
		DictionaryFile file = GetFile();
		if (file == null)
			return;

		file.Load();

		LoadInbox(file);
		LoadOutbox(file);
	}

	void LoadInbox(DictionaryFile file)
	{
		bool resave = false;

		string messages = "";
		if (file.HasKey("inbox") == true)
		{
			messages = file.GetString("inbox", "");
		}
		else
		{
			string rootKey = "Player[" + m_PrimaryKey + "].CloudMailbox";
			messages = PlayerPrefs.GetString(rootKey + ".Inbox", "");

			PlayerPrefs.DeleteKey(rootKey + ".Inbox");

			resave = true;
		}

		m_LastMessageIndexFromProductInbox = file.GetInt("lastMessageIndexFromProductInbox", 0);

		//System.IO.File.WriteAllText("Inbox.json", messages);

		m_Inbox = ReconstructFromString(messages);
		m_Inbox = m_Inbox ?? new List<BaseMessage>();
		m_Inbox = CleanUpMailBox(m_Inbox);

		if (resave == true)
		{
			Save();
		}

		OnInboxChanged();
	}

	void LoadOutbox(DictionaryFile file)
	{
		bool resave = false;

		string messages = "";
		if (file.HasKey("outbox") == true)
		{
			messages = file.GetString("outbox", "");
		}
		else
		{
			string rootKey = "Player[" + m_PrimaryKey + "].CloudMailbox";
			messages = PlayerPrefs.GetString(rootKey + ".Outbox", "");

			PlayerPrefs.DeleteKey(rootKey + ".Outbox");

			resave = true;
		}

		//System.IO.File.WriteAllText("Outbox.json", messages);

		m_Outbox = ReconstructFromString(messages);
		m_Outbox = m_Outbox ?? new List<BaseMessage>();

		if (resave == true)
		{
			Save();
		}
	}

	void OnInboxChanged()
	{
		// make a temporary copy of the event to avoid possibility of 
		// a race condition if the last subscriber unsubscribes 
		// immediately after the null check and before the event is raised
		MailboxEventHandler handler = null;
		lock (this)
		{
			handler = m_InboxChanged;
		}

		// raise event
		if (handler != null)
		{
			handler(m_Inbox.ToArray());
		}
	}

	#endregion

	// =================================================================================================================

	#region --- some utilities used for converting mailbox <--> string  ...

	static string ConvertToString(List<BaseMessage> inMailBox)
	{
		//
		// Save inbox as json list of messages in string format.
		// We can't use simple string messages = JsonMapper.ToJson( m_Inbox );
		// becouse JsonMapper used in load is not able to automaticaly create proper sub-class from source string
		//

		List<string> _tmp = new List<string>();
		foreach (BaseMessage m in inMailBox)
			_tmp.Add(JsonMapper.ToJson(m));

		return JsonMapper.ToJson(_tmp);
	}

	static List<BaseMessage> ReconstructFromString(string inMessage)
	{
		//
		// Try if inMessage is in new format, and if so return result as mailbox
		//
		List<BaseMessage> new_mb = ReconstructFromString_NewFormat(inMessage);
		if (new_mb != null)
			return new_mb;

		//
		// Now we try to restore from old format
		//
		List<BaseMessage> tmp_mb = null;
		try
		{
			tmp_mb = JsonMapper.ToObject<List<BaseMessage>>(inMessage);
		}
		catch
		{
			Debug.LogWarning("OLD: Incorrect format of mailbox messages");
			return null;
		}

		if (tmp_mb != null)
		{
			new_mb = new List<BaseMessage>();
			foreach (BaseMessage tmp_msg in tmp_mb)
			{
				//
				// if this message doesn't have raw message, we are not able to reconstruct it
				// so we will add it and continue
				//
				if (string.IsNullOrEmpty(tmp_msg.m_RAWMessage))
				{
					new_mb.Add(tmp_msg);
					continue;
				}

				ReconstructMessage(tmp_msg.m_RAWMessage, ref new_mb);
			}
		}

		return new_mb;
	}

	static List<BaseMessage> ReconstructFromString_NewFormat(string inMessage)
	{
		List<string> tmp_mb_strings = null;

		try
		{
			tmp_mb_strings = JsonMapper.ToObject<List<string>>(inMessage);
		}
		catch
		{
			Debug.LogWarning("NEW: Incorrect format of mailbox messages");
			return null;
		}

		List<BaseMessage> new_mb = null;
		if (tmp_mb_strings != null)
		{
			new_mb = new List<BaseMessage>();

			foreach (string raw_msg in tmp_mb_strings)
				ReconstructMessage(raw_msg, ref new_mb);
		}

		return new_mb;
	}

	static void ReconstructMessage(string inRAWMessage, ref List<BaseMessage> inTargetMailBox)
	{
		try
		{
			JsonData data = string.IsNullOrEmpty(inRAWMessage) == false ? JsonMapper.ToObject(inRAWMessage) : null;

			//
			// Unwind original raw message.
			//
			if (string.IsNullOrEmpty(inRAWMessage) == false)
			{
				// Unwind RawData
				JsonData x = JsonMapper.ToObject(inRAWMessage);

				while (x.HasValue("m_RAWMessage") && x["m_RAWMessage"] != null)
				{
					inRAWMessage = (string)x["m_RAWMessage"];
					x = JsonMapper.ToObject(inRAWMessage);
				}
			}

			//
			// if message with same raw message already exist in new inTargetMailBox ignore it
			//
			if (inTargetMailBox.FindIndex(x => x.m_RAWMessage == inRAWMessage) >= 0)
				return;

			//
			// try to reconstruct message from raw string
			//
			BaseMessage message = ConstructMessage(inRAWMessage, E_Mailbox.Unknown);

			if (message != null && data.HasValue("m_IsRead"))
			{
				message.m_IsRead = (bool)data["m_IsRead"];
			}

			inTargetMailBox.Add(message);
		}
		catch
		{
			Debug.LogWarning("Error during message processing. See callstack for more info. Message content: " + inRAWMessage ?? "<null>");
		}
	}

	static List<BaseMessage> CleanUpMailBox(List<BaseMessage> inMailBox)
	{
		List<BaseMessage> new_mb = new List<BaseMessage>();

		foreach (BaseMessage message in inMailBox)
		{
			FriendRequest f_req = message as FriendRequest;
			if (f_req != null)
			{
				//
				// Remove friend reguest message if friend is already in friend list
				//
				if (GameCloudManager.friendList.friends.FindIndex(f => f.PrimaryKey == f_req.m_Sender) > -1)
					continue;

				//
				// Remove too old friend requests
				//
				if ((CloudDateTime.UtcNow - f_req.m_SendTime).TotalDays > 45)
					continue;
			}

			//
			// Message was not filtered so add it.
			//
			new_mb.Add(message);
		}

		return new_mb;
	}

	DictionaryFile GetFile()
	{
		DictionaryFile file = null;

		if (string.IsNullOrEmpty(m_PrimaryKey) == false)
		{
			string filename = string.Format("users/{0}/.mailbox", GuiBaseUtils.GetCleanName(m_PrimaryKey));
			file = new DictionaryFile(filename);
		}

		return file;
	}

	#endregion

	// =================================================================================================================
	// === debug =======================================================================================================
	/*
	private void Debug_GenerateRandomFriends(bool inActive)
	{
		if(inActive == true)
		{
			for( int i = 0; i < 10; i++)
			{
				FriendInfo friend = new FriendInfo();
				friend.m_Name 		= DebugUtils.GetRandomString(Random.Range(5,12));
				friend.m_Level 		= Random.Range(1,20);
				friend.m_Missions	= Random.Range(0,200);
				friend.m_LastOnline	= MiscUtils.RandomValue( new string[] {"unknown", "yesterday", "tomorrow"});
				friend.m_Status		= E_FriendRelationShipStatus.Accepted;
				m_Friends.Add(friend);
			}
		}
		else
		{
			for( int i = 0; i < 4; i++)
			{
				FriendInfo friend = new FriendInfo();
				friend.m_Name 		= DebugUtils.GetRandomString(Random.Range(5,12));
				friend.m_Level 		= 0;
				friend.m_Missions	= 0;
				friend.m_LastOnline	= "";
				do 
				{			
					friend.m_Status		= MiscUtils.RandomEnum<E_FriendRelationShipStatus>();
				}
				while(friend.m_Status == E_FriendRelationShipStatus.Accepted);
				
				m_PendingFriends.Add(friend);
			}
		}
	}
	
	 */
}
