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
using System.Collections.Generic;
using JsonMapper = LitJson.JsonMapper;

public class Chat
{
	public readonly static string CHANNEL_ALL = "chat/channel/all";

	public interface IListener
	{
		void ReceiveMessage(string channel, Chat.Message message);
	}

	public struct Message
	{
		public string Id { get; private set; }
		public double Date { get; private set; }
		public string PrimaryKey { get; private set; }
		public string Nickname { get; private set; }
		public int Rank { get; private set; }
		public string Text;

		public static Message Create(string primaryKey, string nickname, int rank, string text)
		{
			return new Chat.Message()
			{
				Id = System.Guid.NewGuid().ToString(),
				Date = GuiBaseUtils.DateToEpoch(CloudDateTime.UtcNow),
				PrimaryKey = primaryKey,
				Nickname = nickname,
				Rank = rank,
				Text = text
			};
		}

		public override string ToString()
		{
			return string.Format("Chat.Message(Id={0}, PrimaryKey={1}, Nickname={2}, Text={3})", Id, PrimaryKey, Nickname, Text);
		}
	}

	class Listeners : List<IListener>
	{
	}
	class Channels : Dictionary<string, Listeners>
	{
	}

	// PRIVATE MEMBERS

	static Chat Instance
	{
		get { return LobbyClient.Chat; }
	}

	Channels m_Channels = new Channels();

	// PUBLIC METHODS

	/** Register listener to listen for all channels
	 */

	public static bool Register(IListener listener)
	{
		if (Instance == null)
			return false;

		Instance.RegisterImpl(CHANNEL_ALL, listener);

		return true;
	}

	/** Register listener to listen for specific channel only
	 */

	public static bool Register(string channel, IListener listener)
	{
		if (Instance == null)
			return false;

		Instance.RegisterImpl(channel, listener);

		return true;
	}

	/** Unregister listener from listening of all channels
	 */

	public static void Unregister(IListener listener)
	{
		if (Instance == null)
			return;

		Instance.UnregisterImpl(CHANNEL_ALL, listener);
	}

	/** Unregister listener from listening of one specific channel
	 */

	public static void Unregister(string channel, IListener listener)
	{
		if (Instance == null)
			return;

		Instance.UnregisterImpl(channel, listener);
	}

	public static void SendMessage(string channel, Chat.Message message)
	{
		// Chat does not work in this version. The required underlying technology was released.
	}

	internal void ReceiveMessage(string channel, string json)
	{
		try
		{
			Chat.Message message = JsonMapper.ToObject<Chat.Message>(json);
			message.Text = SwearWords.Filter(message.Text, true);

			ForwardMessage(channel, channel, message);
			ForwardMessage(CHANNEL_ALL, channel, message);
		}
		catch
		{
			if (Debug.isDebugBuild == true)
			{
				Debug.LogWarning("Chat.ReceiveMessage() :: json of invalid format received!");
				Debug.LogWarning("  channel = " + channel);
				Debug.LogWarning("  json = " + json);
			}
		}
	}

	// PRIVATE METHODS

	void RegisterImpl(string channel, IListener listener)
	{
		if (string.IsNullOrEmpty(channel) == true)
			return;

		if (m_Channels.ContainsKey(channel) == false)
		{
			m_Channels[channel] = new Listeners();
		}

		Listeners listeners = m_Channels[channel];
		if (listeners.Contains(listener) == true)
			return;

		listeners.Add(listener);
	}

	void UnregisterImpl(string channel, IListener listener)
	{
		if (string.IsNullOrEmpty(channel) == true)
			return;
		if (m_Channels.ContainsKey(channel) == false)
			return;

		Listeners listeners = m_Channels[channel];
		listeners.Remove(listener);
	}

	void ForwardMessage(string channel, string messageChannel, Chat.Message message)
	{
		if (m_Channels.ContainsKey(channel) == false)
			return;

		Listeners listeners = new Listeners();
		listeners.AddRange(m_Channels[channel]);

		bool cleanup = false;
		foreach (var listener in listeners)
		{
			if (listener != null)
			{
				listener.ReceiveMessage(messageChannel, message);
			}
			else
			{
				cleanup = true;
			}
		}

		if (cleanup == true)
		{
			m_Channels[channel].RemoveAll((obj) => { return obj == null ? true : false; });
		}
	}
}
