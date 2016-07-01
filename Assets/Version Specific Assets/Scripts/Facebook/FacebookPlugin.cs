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
using System;
using System.Collections;
using LitJson;
using System.Collections.Generic;

public class FBResult
{
	public string Text = "";
}
namespace Facebook
{
	public delegate void FacebookDelegate(FBResult result);
}

public class FacebookPlugin : SocialPlugin
{
	protected static FacebookPlugin m_Instance;

	public static FacebookPlugin Instance
	{
		get
		{
			if (m_Instance == null)
			{
				GameObject go = new GameObject("FacebookPlugin");
				m_Instance = go.AddComponent<FacebookPlugin>();
				GameObject.DontDestroyOnLoad(m_Instance);
			}
			return m_Instance;
		}
	}

	public Hashtable UserInfo
	{
		get { return null; }
	}

	protected List<Person> m_Friends = new List<Person>();

	public override Person[] Friends
	{
		get { return null; }
	}

	public string[] Permissions { get; private set; }

	public delegate void OnHideUnityDelegate(bool isGameShown);

	public OnHideUnityDelegate OnHideUnityEvent;

	public override void Init(Action<State, string> initFinishedEvent)
	{
	}

	public override IEnumerator Init()
	{
		yield break;
	}

	public virtual void Login(string scope, Action<State, string> loginEvent)
	{
	}

	public override void Login(Action<State, string> loginEvent)
	{
	}

	public override IEnumerator Login()
	{
		yield break;
	}

	public virtual IEnumerator Login(string scope)
	{
		yield break;
	}

	public bool IsLoggedIn()
	{
		return false;
	}

	public bool HasPermittedScope(string permission)
	{
		return false;
	}

	void LoadUserData()
	{
	}

	void LoadPermissions()
	{
	}

	public override void Logout(Action<State, string> logoutEvent)
	{
	}

	public override IEnumerator Logout()
	{
		yield break;
	}

	void ResetToDefault()
	{
	}

	public override void PostStatus(string inMessage, Action<State, string> sendMessageEvent)
	{
	}

	public override IEnumerator PostStatus(string inMessage)
	{
		yield break;
	}

	public override void PostImage(string inMessage, byte[] image, Action<State, string> sendMessageEvent)
	{
	}

	public override IEnumerator PostImage(string inMessage, byte[] image)
	{
		yield break;
	}

	public void Feed(string image, string link, string caption, string description, Action<bool> actionSuccess)
	{
	}

	public override void LoadFriends(Action<State, string, Person[]> loadFriendsEvent)
	{
	}

	public override IEnumerator LoadFriends()
	{
		yield break;
	}

	public IEnumerator WaitForUserInfoLoad()
	{
		yield break;
	}

	public void GetUserLike(string FacebookID, Facebook.FacebookDelegate Callback)
	{
	}

	public bool DoesUserLike(string requestResult)
	{
		return false;
	}

	void UserInfoLoaded(FBResult result)
	{
	}

	void OnInitComplete()
	{
	}

	void OnHideUnity(bool isGameShown)
	{
	}

	void AuthCallback(FBResult result)
	{
	}

	void LoadUserDataCallback(FBResult response)
	{
	}

	void LoadPermissionsCallback(FBResult response)
	{
	}

	public static Person PersonFromJson(string jsonString)
	{
		return null;
	}

	public static Person PersonFromJson(JsonData jsonData)
	{
		return null;
	}

	public static string[] PermissionsFromJson(string json)
	{
		return null;
	}

	IEnumerator LoadFriendsFromJson(string json)
	{
		yield break;
	}

	IEnumerator PostStatusInternal(string inMessage, Action<State, string> sendMessageEvent)
	{
		yield break;
	}

	IEnumerator PostImageInternal(string inMessage, byte[] image, Action<State, string> sendMessageEvent)
	{
		yield break;
	}

	void LoadFriendsCallback(FBResult response)
	{
	}
}
