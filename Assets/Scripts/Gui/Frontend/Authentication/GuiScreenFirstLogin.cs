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

public class GuiScreenFirstLogin : GuiScreen
{
	readonly static string CUSTOM_BUTTON = "Custom_Button";
#if !UNITY_STANDALONE
	readonly static string FACEBOOK_BUTTON = "Facebook_Button";
#endif
	readonly static string INSTANT_BUTTON = "Instant_Button";

	readonly static int MESSAGE_CAPTION = 02040016;
	readonly static int MESSAGE_WAIT = 02040017;
	readonly static int MESSAGE_FAILED = 02040013;
	readonly static int MESSAGE_NEW_ACCT = 02040018;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
	readonly static int MESSAGE_UNSUPPORTED = 0103037;
#endif

	// PRIVATE MEMBERS

	GuiPopupMessageBox m_MessageBox;

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();
#if UNITY_STANDALONE
		m_ScreenLayout 		    = GetLayout("Login", "MMFirstLoginNoFcb");
#else
		m_ScreenLayout = GetLayout("Login", "MMFirstLogin");
#endif
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

#if UNITY_STANDALONE && !UNITY_EDITOR
				//this beats all of UNITY_STANDALONEs here, in the end - this screen should not be seen on PC/MAC
		Owner.ShowScreen("Login");
		return;
#endif

#pragma warning disable 162

		RegisterButtonDelegate(INSTANT_BUTTON, () => { StartCoroutine(LoginAsGuest_Coroutine()); }, null);

#if !UNITY_STANDALONE
		RegisterButtonDelegate(FACEBOOK_BUTTON, () => { StartCoroutine(LoginWithFacebook_Coroutine()); }, null);
#endif

		RegisterButtonDelegate(CUSTOM_BUTTON, () => { Owner.ShowScreen("Login"); }, null);

#pragma warning restore 162
	}

	protected override void OnViewHide()
	{
		StopAllCoroutines();

		RegisterButtonDelegate(INSTANT_BUTTON, null, null);
#if !UNITY_STANDALONE
		RegisterButtonDelegate(FACEBOOK_BUTTON, null, null);
#endif
		RegisterButtonDelegate(CUSTOM_BUTTON, null, null);

		base.OnViewHide();
	}

	// PRIVATE METHODS

	IEnumerator LoginAsGuest_Coroutine()
	{
		ShowMessage(MESSAGE_WAIT, false);
		yield return new WaitForSeconds(0.1f);

#if TEST_IOS_VENDOR_ID
		string   userid = string.Empty;
#else
		string userid = SysUtils.GetUniqueDeviceID();
#endif

		string vendorID = string.Empty;
#if UNITY_IPHONE || TEST_IOS_VENDOR_ID
		char deviceType = 'i';

		// it can happen that vendotID is null
		// so we need to try it again
		while (string.IsNullOrEmpty(vendorID) == true)
		{
			vendorID = MFNativeUtils.IOS.VendorId;

			yield return new WaitForEndOfFrame();
		}

		// should we ask the user to migrate his account?
		if (string.IsNullOrEmpty(userid) == true)
		{
			string storedUsername   = PlayerPrefs.GetString(CloudUser.USERNAME_KEY,      string.Empty);
			string storedPrimaryKey = PlayerPrefs.GetString(CloudUser.PRIMARY_KEY_KEY,   string.Empty);
			string storedPassword   = PlayerPrefs.GetString(CloudUser.PASSWORD_HASH_KEY, string.Empty);

			if (string.IsNullOrEmpty(storedPrimaryKey) == true)
			{
				storedPrimaryKey = storedUsername;
			}

			if (string.IsNullOrEmpty(storedUsername) == false && storedUsername.StartsWith("guest") == true)
			{
				string id     = string.IsNullOrEmpty(userid) ? vendorID : userid;
				string idtype = string.IsNullOrEmpty(userid) ? CloudServices.LINK_ID_TYPE_IOSVENDOR : CloudServices.LINK_ID_TYPE_DEVICE;

				//Debug.Log(">>>> ID="+id+", IDType="+idtype);

				GetPrimaryKeyLinkedWithID action = new GetPrimaryKeyLinkedWithID(E_UserAcctKind.Guest, id, idtype);
				GameCloudManager.AddAction(action);

				while (action.isDone == false)
					yield return new WaitForEndOfFrame();

				//Debug.Log(">>>> action.isSucceeded="+action.isSucceeded+", action.primaryKey="+action.primaryKey+", storedPrimaryKey="+storedPrimaryKey);

				if (action.isSucceeded == true && action.isPrimaryKeyForSHDZ == true && action.primaryKey != storedPrimaryKey)
				{
					HideMessage();

					bool migrate = false;
					GuiPopupMigrateGuest migratePopup = (GuiPopupMigrateGuest)Owner.ShowPopup("MigrateGuest", null, null, (inPopup, inResult) => {
						migrate = inResult == E_PopupResultCode.Ok;
					});
					migratePopup.Usage      = GuiPopupMigrateGuest.E_Usage.QuickPlay;
					migratePopup.PrimaryKey = storedPrimaryKey;
					migratePopup.Password   = storedPassword;

					while (migratePopup.IsVisible == true)
						yield return new WaitForEndOfFrame();

					if (migrate == true)
					{
						yield break;
					}
				}
			}
		}
#else
		char deviceType = 'a';
#endif

		string username = null;
		string nickname = null;
		string email = "";
		string password = null;

		if (string.IsNullOrEmpty(userid) == false)
		{
			username = string.Format("guest{0}{1}", userid.ToLower(), deviceType);
			password = string.Format("pwd{0}!", userid);
		}
		else
		{
			password = string.Format("pwd{0}!", string.Format("VID{0}", utils.CryptoUtils.CalcSHA1Hash(vendorID)));
		}

		List<string> usernames = new List<string>();
		usernames.AddRange(GuiBaseUtils.GenerateUsernames("guest", false));
		if (string.IsNullOrEmpty(username) == false)
		{
			usernames.Add(username);
		}

		//Debug.Log(">>>> GUEST :: username="+username+", nickname="+nickname+", email="+email+", password="+password);

		yield return
						StartCoroutine(Login_Coroutine(E_UserAcctKind.Guest, userid, vendorID, username, usernames.ToArray(), nickname, password, email));
	}

	IEnumerator LoginWithFacebook_Coroutine()
	{
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
		ShowMessage(MESSAGE_WAIT, false);
		yield return new WaitForSeconds(0.1f);

		// try login with facebook if needed
		if (FacebookPlugin.Instance.CurrentUser == null)
		{
			yield return StartCoroutine(FacebookPlugin.Instance.Init());
			yield return StartCoroutine(FacebookPlugin.Instance.Login("email"));
		}
		if (FacebookPlugin.Instance.CurrentUser == null)
		{
			ShowMessage(MESSAGE_FAILED, true);
			yield break;
		}

		// get user info
		yield return StartCoroutine(FacebookPlugin.Instance.WaitForUserInfoLoad());
		Hashtable data = FacebookPlugin.Instance.UserInfo;

		if (data == null)
		{
			yield return StartCoroutine(FacebookPlugin.Instance.Logout());
			ShowMessage(MESSAGE_FAILED, true);
			yield break;
		}
		else
		{
			//Debug.Log(">>>> username=" + (data.ContainsKey("username") ? data["username"].ToString() : "---"));

			string    userid = data.ContainsKey("id") ? data["id"].ToString().ToLower() : null;
			string   olduser = string.Format("fb{0}", userid);
			string  username = data.ContainsKey("username") ? data["username"].ToString().ToLower().Replace(".", "") : null;
			string  fullname = data.ContainsKey("name") ? data["name"].ToString() : null;
			string firstname = data.ContainsKey("first_name") ? data["first_name"].ToString() : null;
			string  lastname = data.ContainsKey("last_name") ? data["last_name"].ToString() : null;
			string  nickname = fullname;
			string     email = data.ContainsKey("email") ? data["email"].ToString() : null;
			string  password = string.Format("pwd{0}!", userid);

			if (string.IsNullOrEmpty(userid))
			{
				yield return StartCoroutine(FacebookPlugin.Instance.Logout());
				ShowMessage(MESSAGE_FAILED, true);
				yield break;
			}

			List<string> usernames = new List<string>();
			if (string.IsNullOrEmpty(username) == false)
			{
				usernames.Add(username.RemoveDiacritics());
			}
			usernames.AddRange(GuiBaseUtils.GenerateUsernames(fullname, true));
			usernames.AddRange(GuiBaseUtils.GenerateUsernames(firstname, true));
			usernames.AddRange(GuiBaseUtils.GenerateUsernames(lastname, true));
			if (string.IsNullOrEmpty(email) == false && email[0] != '"')
			{
				string temp = email.Substring(0, email.IndexOf("@"));
				usernames.AddRange(GuiBaseUtils.GenerateUsernames(temp, true));
			}
			usernames.Add(olduser);

			//Debug.Log(">>>> FB :: username="+username+", nickname="+nickname+", email="+email+", password="+password);

			yield return StartCoroutine(Login_Coroutine(E_UserAcctKind.Facebook, userid, null, olduser, usernames.ToArray(), nickname, password, email));
		}
#else
		ShowMessage(MESSAGE_UNSUPPORTED, true);
		yield break;
#endif
	}

	IEnumerator Login_Coroutine(E_UserAcctKind kind,
								string userid,
								string vendorID,
								string legacyUsername,
								string[] usernames,
								string nickname,
								string password,
								string email)
	{
		if (string.IsNullOrEmpty(userid) == true && string.IsNullOrEmpty(vendorID) == true)
		{
			ShowMessage(MESSAGE_FAILED, true);
			yield break;
		}

		string pwdhash = CloudServices.CalcPasswordHash(password);
		string username = null;
		string primaryKey = null;

		ShowMessage(MESSAGE_WAIT, false);
		yield return new WaitForSeconds(0.1f);

#if UNITY_IPHONE
				// fail when there is invalid guest account based on invalid MAC address generated
		if (legacyUsername == "guest6024a02ea5577bcb6442a70d0bfa791935839db3i")
		{
			Debug.LogWarning("It's not possible to deduce valid guest account. Login failed!");
			ShowMessage(MESSAGE_FAILED, true);
			yield break;
		}
#endif

		// check if username exists
		bool legacyUsernameExists = false;
		if (string.IsNullOrEmpty(legacyUsername) == false)
		{
			// NOTE: this check is obsolete but we need to do it
			//       to support legacy accounts
			UsernameAlreadyExists usernameExists = CloudUser.instance.CheckIfUserNameExist(legacyUsername);
			while (usernameExists.isDone == false)
			{
				yield return new WaitForEndOfFrame();
			}

			if (usernameExists.isFailed == true)
			{
				ShowMessage(MESSAGE_FAILED, true);
				yield break;
			}

			legacyUsernameExists = usernameExists.userExist;
		}

		if (legacyUsernameExists == true)
		{
			username = legacyUsername;

			// obtain primaryKey using legacyUsername
			UserGetPrimaryKey action = new UserGetPrimaryKey(legacyUsername);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			if (action.isSucceeded == true)
			{
				primaryKey = action.primaryKey;
			}
		}
		else
		{
			// obtain primaryKey using userid
			string idType = kind == E_UserAcctKind.Guest ? CloudServices.LINK_ID_TYPE_DEVICE : CloudServices.LINK_ID_TYPE_FACEBOOK;

			GetPrimaryKeyLinkedWithID action = new GetPrimaryKeyLinkedWithID(kind, userid, idType);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			if (action.isSucceeded == true && action.isPrimaryKeyForSHDZ == true)
			{
				primaryKey = action.primaryKey;
			}
		}

#if UNITY_IPHONE || TEST_IOS_VENDOR_ID
		if (kind == E_UserAcctKind.Guest && string.IsNullOrEmpty(primaryKey) == true)
		{
			// obtain username using userid
			GetPrimaryKeyLinkedWithID action = new GetPrimaryKeyLinkedWithID(kind, vendorID, CloudServices.LINK_ID_TYPE_IOSVENDOR);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			//Debug.Log(">>>> action.isSucceeded="+action.isSucceeded+", action.primaryKey="+action.primaryKey);

			if (action.isSucceeded == true && action.isPrimaryKeyForSHDZ == true)
			{
				primaryKey = action.primaryKey;
			}
		}
#endif

		// create new account if needed
		if (string.IsNullOrEmpty(primaryKey) == true)
		{
			ShowMessage(MESSAGE_NEW_ACCT, false);

			// check available names
			UsernamesAlreadyExist checkAvailableNames = new UsernamesAlreadyExist(usernames);
			GameCloudManager.AddAction(checkAvailableNames);
			while (checkAvailableNames.isDone == false)
			{
				yield return new WaitForEndOfFrame();
			}

			List<string> availableNames = new List<string>();
			foreach (var pair in checkAvailableNames.usernames)
			{
				if (pair.Exists == false)
				{
					availableNames.Add(pair.Username);
				}
			}

			if (availableNames.Count == 0)
			{
				ShowMessage(MESSAGE_FAILED, true);
				yield break;
			}

			username = availableNames[0];
			nickname = string.IsNullOrEmpty(nickname) == true ? username : nickname.RemoveDiacritics();

			//Debug.Log(string.Join(System.Environment.NewLine, availableNames.ToArray()));
			//Debug.Log(">>>> "+kind+" :: username="+username+", nickname="+nickname+", email="+email+", password="+password);

			// create new user
			bool success = false;
			yield return CloudUser.instance.CreateNewUser(username, pwdhash, nickname, email, false, kind, (result) => { success = result; });

			if (success == false)
			{
				ShowMessage(MESSAGE_FAILED, true);
				yield break;
			}

			// get primary key
			UserGetPrimaryKey action = new UserGetPrimaryKey(username);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			primaryKey = action.primaryKey;
		}
		else if (string.IsNullOrEmpty(username) == true)
		{
			GetUserData action = new GetUserData(new UnigueUserID(primaryKey, pwdhash, Game.PrimaryProductID), CloudServices.PROP_ID_USERNAME);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			username = action.isSucceeded ? action.result : primaryKey;
		}

		//Debug.Log(">>>> LOGIN :: primaryKey="+primaryKey+", username="+username+", nickname="+nickname+", email="+email+", password="+password);

		// login user
		{
			HideMessage();

			CloudUser.instance.SetLoginData(primaryKey, nickname, username, pwdhash, password.Length, true, true);

			Owner.ShowPopup("Authentication",
							TextDatabase.instance[02040016],
							"",
							(inPopup, inResult) =>
							{
								if (inResult == E_PopupResultCode.Success)
								{
									Owner.Exit();
								}
							});
		}
	}

	void ShowMessage(int textId, bool canClose)
	{
		if (m_MessageBox == null || m_MessageBox.IsVisible == false)
		{
			m_MessageBox = Owner.ShowPopup("MessageBox", "", "") as GuiPopupMessageBox;
		}

		if (m_MessageBox != null)
		{
			m_MessageBox.SetCaption(TextDatabase.instance[MESSAGE_CAPTION]);
			m_MessageBox.SetText(TextDatabase.instance[textId]);
			m_MessageBox.SetButtonVisible(canClose);
		}
	}

	void HideMessage()
	{
		if (m_MessageBox != null)
		{
			m_MessageBox.ForceClose();
			m_MessageBox = null;
		}
	}
}
