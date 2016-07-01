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

public class FacebookFriendList : MonoBehaviour
{
	public class FacebookFriend
	{
		public string[] PrimaryKeys { get; private set; }
		public SocialPlugin.Person Person { get; private set; }

		public FacebookFriend(string[] primaryKeys, SocialPlugin.Person person)
		{
			PrimaryKeys = primaryKeys;
			Person = person;
		}
	}

	FacebookFriend[] m_Friends = null;

	public FacebookFriend[] Friends
	{
		get { return m_Friends; }
	}

	int m_MaxFriendsInOneBatch = 20;
	bool m_IsLoading = false;

	void Start()
	{
		GameCloudManager.friendList.PendingFriendListChanged += OnFriendListChanged;
	}

	void Destroy()
	{
		GameCloudManager.friendList.PendingFriendListChanged -= OnFriendListChanged;
	}

	public void Load()
	{
#if !UNITY_EDITOR
		if (m_IsLoading)
			return;
		StartCoroutine(LoadInternal());
#endif
	}

	IEnumerator LoadInternal()
	{
		m_IsLoading = true;
		m_Friends = null;
		List<FacebookFriend> friends = new List<FacebookFriend>();

		if (FacebookPlugin.Instance.CurrentUser == null)
		{
			Debug.LogError("FacebookFriendList: Facebook user is not logged in.");
			m_IsLoading = false;
			yield break;
		}

		yield return StartCoroutine(FacebookPlugin.Instance.LoadFriends());

		SocialPlugin.Person[] friendsPlayingDeadZone = FacebookPlugin.Instance.Friends;

		if (friendsPlayingDeadZone == null || friendsPlayingDeadZone.Length <= 0)
		{
			m_IsLoading = false;
			yield break;
		}

		int friendsProcessed = 0;
		while (friendsProcessed < friendsPlayingDeadZone.Length)
		{
			int friendsInThisBatch = (friendsProcessed + m_MaxFriendsInOneBatch < friendsPlayingDeadZone.Length)
													 ? m_MaxFriendsInOneBatch
													 : friendsPlayingDeadZone.Length - friendsProcessed;
			BatchCommandAction[] actions = new BatchCommandAction[friendsInThisBatch];
			for (int i = 0; i < friendsInThisBatch; i++)
				actions[i] = new GetPrimaryKeysLinkedWithID(friendsPlayingDeadZone[friendsProcessed + i].ID,
															CloudServices.LINK_ID_TYPE_FACEBOOK,
															E_UserAcctKind.Any,
															BaseCloudAction.NoTimeOut,
															false);

			BatchCommand findFriendsPrimaryKeys = new BatchCommand(actions);
			GameCloudManager.AddAction(findFriendsPrimaryKeys);

			while (findFriendsPrimaryKeys.isDone == false)
				yield return new WaitForEndOfFrame();

			if (findFriendsPrimaryKeys.isSucceeded)
			{
				for (int i = 0; i < friendsInThisBatch; i++)
				{
					if (findFriendsPrimaryKeys.actions[i].isSucceeded)
					{
						GetPrimaryKeysLinkedWithID usersLinkedWithID = (GetPrimaryKeysLinkedWithID)findFriendsPrimaryKeys.actions[i];
						if (usersLinkedWithID.AllPrimaryKeys != null && usersLinkedWithID.AllPrimaryKeys.Length > 0)
							friends.Add(new FacebookFriend(usersLinkedWithID.AllPrimaryKeys, friendsPlayingDeadZone[friendsProcessed + i]));
					}
				}
			}

			friendsProcessed += m_MaxFriendsInOneBatch;
		}

		if (friends.Count > 0)
			m_Friends = friends.ToArray();
		m_IsLoading = false;
	}

	public IEnumerator WaitForLoading()
	{
		while (m_IsLoading)
			yield return new WaitForEndOfFrame();
	}

	public bool IsFacebookFriend(string primaryKey)
	{
		if (m_Friends == null || m_Friends.Length == 0)
			return false;

		foreach (FacebookFriend friend in m_Friends)
		{
			if (System.Array.Exists(friend.PrimaryKeys, friendPrimaryKey => friendPrimaryKey == primaryKey))
				return true;
		}
		return false;
	}

	public SocialPlugin.Person GetFacebookFriend(string primaryKey)
	{
		if (m_Friends == null || m_Friends.Length == 0)
			return null;

		foreach (FacebookFriend friend in m_Friends)
		{
			if (System.Array.Exists(friend.PrimaryKeys, friendPrimaryKey => friendPrimaryKey == primaryKey))
				return friend.Person;
		}
		return null;
	}

	void OnFriendListChanged(object sender, System.EventArgs e)
	{
		StartCoroutine(AcceptAllPendingFacebookFriends());
	}

	IEnumerator AcceptAllPendingFacebookFriends()
	{
		while (m_IsLoading)
			yield return new WaitForEndOfFrame();

		List<string> primaryKeysToAccept = new List<string>();
		foreach (FriendList.PendingFriendInfo pendingFriend in GameCloudManager.friendList.pendingFriends)
		{
			if (pendingFriend.IsItRequest && IsFacebookFriend(pendingFriend.PrimaryKey))
				primaryKeysToAccept.Add(pendingFriend.PrimaryKey);
		}

		foreach (string primaryKey in primaryKeysToAccept)
		{
			GameCloudManager.friendList.AcceptFriendRequest(primaryKey);
		}
	}

	public void AddAllFacebookFriends(System.Action<int> doneCallback)
	{
		StartCoroutine(AddAllFacebookFriendsInternal(doneCallback));
	}

	IEnumerator AddAllFacebookFriendsInternal(System.Action<int> doneCallback)
	{
		yield return StartCoroutine(FacebookPlugin.Instance.Init());
		string requiredPermission = "user_friends";
		if (FacebookPlugin.Instance.IsLoggedIn() == false || FacebookPlugin.Instance.HasPermittedScope(requiredPermission) == false)
		{
			yield return StartCoroutine(FacebookPlugin.Instance.Login(requiredPermission));
		}
		if (FacebookPlugin.Instance.IsLoggedIn() == true && FacebookPlugin.Instance.HasPermittedScope(requiredPermission) == true &&
			m_IsLoading == false)
		{
			yield return StartCoroutine(LoadInternal());
		}

		while (m_IsLoading)
			yield return new WaitForEndOfFrame();

		if (m_Friends == null || m_Friends.Length == 0 || FacebookPlugin.Instance.IsLoggedIn() == false ||
			FacebookPlugin.Instance.HasPermittedScope(requiredPermission) == false)
		{
			if (doneCallback != null)
				doneCallback(0);
			yield break;
		}

		int friendsAdded = 0;
		foreach (FacebookFriend friend in m_Friends)
		{
			if (friend.PrimaryKeys == null || friend.PrimaryKeys.Length == 0)
				continue;

			foreach (string primaryKey in 	friend.PrimaryKeys)
			{
				if (primaryKey != CloudUser.instance.primaryKey &&
					GameCloudManager.friendList.AddNewFriend(primaryKey,
															 null,
															 null,
															 string.Format(TextDatabase.instance[02040236], CloudUser.instance.nickName)))
					friendsAdded++;
			}
		}
		if (doneCallback != null)
			doneCallback(friendsAdded);
	}
}
