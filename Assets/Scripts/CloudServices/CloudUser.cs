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
using Queue = System.Collections.Queue;
using LitJson;
using PremiumAccountDesc = CloudServices.PremiumAccountDesc;
using GeoRegion = NetUtils.GeoRegion;

// TODO ::
//		- Remove singleton behaviour. this class will be part of GameCloudmanager.
//		
// =============================================================================================================================
// =============================================================================================================================
public class CloudUser : MonoBehaviour
{
	public const int MIN_ACCOUNT_NAME_LENGTH = 6; // we have to limit it.
	public const int MAX_ACCOUNT_NAME_LENGTH = 18; // we have to limit it because we don't limit text size in widgets.
	public const int MIN_PASSWORD_LENGTH = 8; // we have to limit it.	

#if UNITY_EDITOR
	public const string NICKNAME_KEY = "CloudUser.Editor.NickName";
	public const string USERNAME_KEY = "CloudUser.Editor.UserName";
	public const string PRIMARY_KEY_KEY = "CloudUser.Editor.PrimaryKey";
	public const string PASSWORD_HASH_KEY = "CloudUser.Editor.PasswordHash";
	public const string PASSWORD_LENGTH_KEY = "CloudUser.Editor.PasswordLength";
	public const string AUTO_LOGIN_KEY = "CloudUser.Editor.AutoLogin";
#else
	public const string 				NICKNAME_KEY			= "CloudUser.NickName";
	public const string 				USERNAME_KEY 		 	= "CloudUser.UserName";
	public const string 				PRIMARY_KEY_KEY 	 	= "CloudUser.PrimaryKey";
	public const string 				PASSWORD_HASH_KEY 	 	= "CloudUser.PasswordHash";
	public const string 				PASSWORD_LENGTH_KEY   	= "CloudUser.PasswordLength";
	public const string 				AUTO_LOGIN_KEY   		= "CloudUser.AutoLogin";
#endif

	// -- public properties...
	public static CloudUser instance
	{
		get { return GetInstance(); }
	}

	public string nickName
	{
		get { return string.IsNullOrEmpty(m_NickName) ? m_UserName : m_NickName; }
		set { m_NickName = value; }
	}

	public string userName_TODO
	{
		get { return m_UserName; }
	}

	public string primaryKey
	{
		get { return m_PrimaryKey; }
	}

	public string passwordHash
	{
		get { return m_PasswordHash; }
	}

	public bool autoLogin
	{
		get { return m_AutoLogin; }
	}

	public bool skipAutoLogin
	{
		get { return m_SkipAutoLogin; }
	}

	public GeoRegion region
	{
		get { return GetActualRegion(); }
		set { SetCustomRegion(value); }
	}

	public E_UserAcctKind userAccountKind
	{
		get { return m_UserAcctKind; }
	}

	public bool receiveNews
	{
		get { return m_ReceiveNews; }
		set { m_ReceiveNews = value; }
	}

	public string currentCountry
	{
		get { return m_CountryCode; }
	}

	public bool isUserAuthenticated
	{
		get { return authenticationStatus == E_AuthenticationStatus.Ok; }
	}

	public bool authenticationDataPresent
	{
		get { return (m_PrimaryKey != null && m_UserName != null && m_PasswordHash != null && m_PasswordLength > 0); }
	}

	public UnigueUserID authenticatedUserID
	{
		get { return m_AuthenticatedUserID; }
		set { m_AuthenticatedUserID = value; }
	}

	public delegate void AuthenticationEventHandler(bool state);
	static AuthenticationEventHandler m_AuthenticationChanged;

	public static event AuthenticationEventHandler authenticationChanged
	{
		add
		{
			if (value != null)
			{
				m_AuthenticationChanged -= value; // just to be sure we don't have any doubles
				m_AuthenticationChanged += value;
				value(instance.isUserAuthenticated); // call delegate when registering so it recieves current state
			}
		}
		remove { m_AuthenticationChanged -= value; }
	}

	public bool isPremiumAccountActive
	{
		get { return GetPremiumAccountEndDateTime() > CloudDateTime.UtcNow ? true : false; }
	}

	public PremiumAccountDesc[] availablePremiumAccounts
	{
		get { return m_AvailableAccts; }
	}

	public delegate void PremiumAcctEventHandler(bool state);
	static PremiumAcctEventHandler m_PremiumAcctChanged;

	public static event PremiumAcctEventHandler premiumAcctChanged
	{
		add
		{
			if (value != null)
			{
				m_PremiumAcctChanged -= value; // just to be sure we don't have any doubles
				m_PremiumAcctChanged += value;
				value(instance.isPremiumAccountActive); // call delegate when registering so it recieves current state
			}
		}
		remove { m_PremiumAcctChanged -= value; }
	}

// -------------------------------------------------------------------------------------------------------------------------
	// private part...
	static CloudUser ms_Instance;
//	private static string				productID				{ get { return GameCloudManager.productID;	} }	
	static string productID
	{
		get { return PPIManager.ProductID; }
	}

	// User data used for authentication...
	string m_NickName = null;
	string m_UserName = null;
	string m_PrimaryKey = null;
	string m_PasswordHash = null;
	int m_PasswordLength = 8;
	bool m_AutoLogin = false;
	bool m_SkipAutoLogin = false;

	// Other user settings (valid after authentication)...
	string m_CountryCode = null;
	GeoRegion m_RealRegion = GeoRegion.America;
	GeoRegion m_CustomRegion = GeoRegion.None;
	bool m_ReceiveNews = true;
	E_UserAcctKind m_UserAcctKind = E_UserAcctKind.Normal;
	PremiumAccountDesc[] m_AvailableAccts = null;

	bool m_AuthenticationDataLoaded; // data was stored in PlayerPrefs and loaded...

	// -- Local User Authentication...	
	UnigueUserID m_AuthenticatedUserID;

	// Cloud service comunication...		
	public enum E_AuthenticationStatus
	{
		None,
		InProgress,
		RetrievingPPI,
		RetryIAP,
		Ok,
		Failed
	};
	E_AuthenticationStatus m_AuthenticationStatus = E_AuthenticationStatus.None;
	string m_LastAuthenticationFailReason;

	public E_AuthenticationStatus authenticationStatus
	{
		get { return m_AuthenticationStatus; }
	}

	public string lastAuthenticationFail
	{
		get { return m_LastAuthenticationFailReason; }
	}

	// =========================================================================================================================
	// === MonoBehaviour interface =============================================================================================
	void OnDestroy()
	{
		ms_Instance = null;
	}

#if !UNITY_EDITOR
	void OnApplicationFocus(bool state)
	{
		// When the application is switched to the fullscreen mode (web player on Mac OS X)
		// this method is called with focus == false.
		// Focus must be set to true.
		if (Application.platform == RuntimePlatform.OSXWebPlayer)
		{
			if (Screen.fullScreen)
			{
				state = true;
			}
		}

#if DEADZONE_CLIENT
		if (state == true && isUserAuthenticated == true)
		{
			CheckCloudDateTime();
		}
#endif
	}
#endif

	// =========================================================================================================================
	// === public interface ====================================================================================================

	// =========================================================================================================================		

	#region --- internal ...    

	static CloudUser GetInstance()
	{
		if (ms_Instance == null)
		{
			GameObject go = new GameObject("CloudUser");
			ms_Instance = go.AddComponent<CloudUser>();
			GameObject.DontDestroyOnLoad(ms_Instance);

			ms_Instance.LoadAuthenticationData();
		}

		return ms_Instance;
	}

	#endregion

	// =========================================================================================================================		

	#region --- Create new user...    

	public UsernameAlreadyExists CheckIfUserNameExist(string inUserName) //TODO: PRIMARY KEY - nemelo by se tu pouzvat primaryKey?
	{
		UsernameAlreadyExists action = new UsernameAlreadyExists(inUserName);
		GameCloudManager.AddAction(action);
		return action;
	}

	public Coroutine CreateNewUser(string inUserName,
								   string inPasswordHash,
								   string inNickName,
								   string inEmail,
								   bool inIWantNews,
								   E_UserAcctKind inKind,
								   System.Action<bool> callback)
	{
		E_AppProvider appProvider = E_AppProvider.Madfinger;

		// reset authentication status. New player is creating...
		SetAuthenticationStatus(E_AuthenticationStatus.None);

		return StartCoroutine(CreateNewUser_Coroutine(inUserName, inPasswordHash, inNickName, inEmail, inIWantNews, inKind, appProvider, callback));
	}

	IEnumerator CreateNewUser_Coroutine(string inUserName,
										string inPasswordHash,
										string inNickName,
										string inEmail,
										bool inIWantNews,
										E_UserAcctKind inKind,
										E_AppProvider inAppProvider,
										System.Action<bool> callback)
	{
		// create user
		{
			_CreateNewUser action = new _CreateNewUser(inUserName, inPasswordHash, productID);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			if (action.isFailed == true)
			{
				callback(false);
				yield break;
			}
		}

		// get primary key
		UnigueUserID userID;
		{
			UserGetPrimaryKey action = new UserGetPrimaryKey(inUserName);
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			userID = new UnigueUserID(action.primaryKey, inPasswordHash, productID);
		}

		// set user data
		{
			Dictionary<string, string> userData = new Dictionary<string, string>();

			userData.Add(CloudServices.PROP_ID_NICK_NAME, inNickName);
			userData.Add(CloudServices.PROP_ID_EMAIL, inEmail);
			userData.Add(CloudServices.PROP_ID_I_WANT_NEWS, inIWantNews.ToString());
			userData.Add(CloudServices.PROP_ID_ACCT_KIND, inKind.ToString());

			List<BaseCloudAction> actions = new List<BaseCloudAction>();

			actions.Add(new SetUserDataList(userID, userData));

			if (inAppProvider != E_AppProvider.Madfinger)
			{
				actions.Add(new SetUserProductData(userID, CloudServices.PROP_ID_APP_PROVIDER, inAppProvider.ToString()));
			}

			CloudActionSerial action = new CloudActionSerial(null, BaseCloudAction.NoTimeOut, actions.ToArray());
			GameCloudManager.AddAction(action);

			while (action.isDone == false)
				yield return new WaitForEndOfFrame();
		}

		callback(true);
	}

	public UpdateMFAccountAndFetchPPI UpdateUser(string inPasswordHash, string inNickName, string inEmail, bool inIWantNews, GeoRegion inRegion)
	{
		UpdateMFAccountAndFetchPPI action = new UpdateMFAccountAndFetchPPI(m_AuthenticatedUserID,
																		   inNickName,
																		   inEmail,
																		   inIWantNews,
																		   NetUtils.GetRegionString(inRegion));
		GameCloudManager.AddAction(action);

		return action;
	}

	#endregion

	// =========================================================================================================================		

	#region --- Authentication routines...

	public bool CanAutoAuthenticate()
	{
		return (m_SkipAutoLogin == false &&
				m_AutoLogin == true &&
				isUserAuthenticated == false &&
				authenticationDataPresent == true &&
				Application.internetReachability != NetworkReachability.NotReachable);
	}

	public void AuthenticateLocalUser()
	{
		if (authenticationStatus == E_AuthenticationStatus.InProgress)
		{
			// authentication in proggres, so wait for finish ...
			return;
		}

		if (authenticationStatus == E_AuthenticationStatus.Ok)
		{
			// skip other authentication requests...
			return;
		}

		if (authenticationDataPresent == false)
		{
			// we don't have valid user data so we can't authenticate local user...
			SetAuthenticationStatus(E_AuthenticationStatus.Failed, "Authentication data are missing"); // TODO TextDatabase
			return;
		}

		StartCoroutine(AuthenticateUser_Corutine());
	}

	public void LogoutLocalUser()
	{
		SetAuthenticationStatus(E_AuthenticationStatus.None);

		m_SkipAutoLogin = true;

		// -------------------------------------------------------------------------
#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
		if(FacebookPlugin.Instance.IsLoggedIn() == true)
		{
			FacebookPlugin.Instance.Logout(null);
		}
#endif

		// -------------------------------------------------------------------------
		// stop premium acct coroutine
		CheckPremiumAcct(false);

		// -------------------------------------------------------------------------
		// raise authentication changed event
		OnAuthenticationChanged(false);

		// -------------------------------------------------------------------------
		// save settings
		Game.Settings.Save();
		Game.Settings = UserSettings.Empty;
	}

	void OnAuthenticationChanged(bool state)
	{
		// make a temporary copy of the event to avoid possibility of 
		// a race condition if the last subscriber unsubscribes 
		// immediately after the null check and before the event is raised
		AuthenticationEventHandler handler = null;
		lock (this)
		{
			handler = m_AuthenticationChanged;
		}

		// raise event
		if (handler != null)
		{
			handler(state);
		}

		if (state == true)
		{
			CheckCloudDateTime();
		}
	}

	IEnumerator AuthenticateUser_Corutine()
	{
		UnigueUserID userID = new UnigueUserID(primaryKey, passwordHash, productID);
		string deviceID = SysUtils.GetUniqueDeviceID();
		string facebookID = string.Empty;

		SetAuthenticationStatus(E_AuthenticationStatus.InProgress);

// capa
		// -------------------------------------------------------------------------
		// retrieve facebook id...
		if (FacebookPlugin.Instance.CurrentUser != null)
		{
			facebookID = FacebookPlugin.Instance.CurrentUser.ID;
		}

		// -------------------------------------------------------------------------
		// authenticate user...
// capa
//		SetAuthenticationStatus(E_AuthenticationStatus.InProgress);
		{
			AuthenticateUser action = new AuthenticateUser(userID, deviceID, facebookID);
			GameCloudManager.AddAction(action);

			// wait for async action...
			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			// process action result...			
			if (action.isFailed == true)
			{
				SetAuthenticationStatus(E_AuthenticationStatus.Failed, action.failInfo);
				yield break;
			}

			authenticatedUserID = userID;

			// this will be overwritten later ProcessAuthenticationData()
			m_UserAcctKind = E_UserAcctKind.Normal;

			// rip data from sub-actions
			string err;
			if (ProcessAuthenticationData(action.actions, out err) == false)
			{
				SetAuthenticationStatus(E_AuthenticationStatus.Failed, err);
				yield break;
			}
		}

#if UNITY_IPHONE || TEST_IOS_VENDOR_ID
				// pair guest account with vendorID
		if (m_UserAcctKind == E_UserAcctKind.Guest)
		{
			yield return StartCoroutine(PairGuestAccountWithVendorID_Coroutine(userID));
		}
#endif

		// -------------------------------------------------------------------------
		// Retrive player persistent info...
		SetAuthenticationStatus(E_AuthenticationStatus.RetrievingPPI);
		{
			BaseCloudAction action = new FetchPlayerPersistantInfo(userID);
			GameCloudManager.AddAction(action);

			// wait for async action...
			while (action.isDone == false)
				yield return new WaitForEndOfFrame();

			// process action result...
			if (action.isFailed == true)
			{
				SetAuthenticationStatus(E_AuthenticationStatus.Failed, "Can't retrive Player Data");
				yield break;
			}

			//Debug.Log("Authentication process succeful");
		}

		// -------------------------------------------------------------------------
		SetAuthenticationStatus(E_AuthenticationStatus.Ok);

		// -------------------------------------------------------------------------
		// raise authentication changed event
		OnAuthenticationChanged(true);

		// -------------------------------------------------------------------------
		// raise premium acct changed event
		CheckPremiumAcct(true);
	}

#if UNITY_IPHONE || TEST_IOS_VENDOR_ID
	IEnumerator PairGuestAccountWithVendorID_Coroutine(UnigueUserID userID)
	{
		List<BaseCloudAction> actions = new List<BaseCloudAction>();
		
		// it can happen that iOSVendorID is null
		// so we need to try it again
		int guard = 100;
		string vendorID = string.Empty;
		while (guard > 0)
		{
			vendorID = MFNativeUtils.IOS.VendorId;
			if (string.IsNullOrEmpty(vendorID) == false)
				break;
			
			yield return new WaitForEndOfFrame();
			
			guard -= 1;
		}
		if (string.IsNullOrEmpty(vendorID) == false)
		{
			actions.Add(new SetiOSVendorId(userID, vendorID));
		}
		
		// it can happen that iOSAdvertisingID is null
		// so we need to try it again
		guard = 100;
		string advertID = string.Empty;
		while (guard > 0)
		{
			advertID = MFNativeUtils.IOS.AdvertisingId;
			if (string.IsNullOrEmpty(advertID) == false)
				break;
			
			yield return new WaitForEndOfFrame();
			
			guard -= 1;
		}
		if (string.IsNullOrEmpty(advertID) == false)
		{
			actions.Add(new SetiOSAdvertisingId(userID, advertID));
		}
		
		//Debug.Log(">>>> AUTHENTICATE :: vendorID="+vendorID+", advertID="+advertID);
		
		if (actions.Count > 0)
		{
			CloudActionSerial action = new CloudActionSerial(null, BaseCloudAction.NoTimeOut, actions.ToArray());
			GameCloudManager.AddAction(action);
			
			while (action.isDone == false)
				yield return new WaitForEndOfFrame();
		}
	}
#endif

	bool ProcessAuthenticationData(BaseCloudAction[] actions, out string err)
	{
		err = string.Empty;

		foreach (BaseCloudAction action in actions)
		{
			// -------------------------------------------------------------------------
			// Get actual region info...
			if (action is GetUserRegionInfo)
			{
				GetUserRegionInfo data = (GetUserRegionInfo)action;
				m_RealRegion = data.region;
				m_CountryCode = data.countryCode;
			}
			// -------------------------------------------------------------------------
			// Get custom region info...
			else if (action is GetCustomRegionInfo)
			{
				if (action.isSucceeded == true)
				{
					GetCustomRegionInfo data = (GetCustomRegionInfo)action;
					m_CustomRegion = data.region;
				}
				else if (action.isFailed == true)
				{
					SetUserProductData setAction = new SetUserProductData(m_AuthenticatedUserID, CloudServices.PROP_ID_CUSTOM_REGION, "none");
					GameCloudManager.AddAction(setAction);
				}
			}
			// -------------------------------------------------------------------------
			// Get available premium accounts...
			else if (action is GetAvailablePremiumAccounts)
			{
				GetAvailablePremiumAccounts data = (GetAvailablePremiumAccounts)action;
				m_AvailableAccts = data.accounts;
				if (m_AvailableAccts == null)
				{
					// do not break, this isn't critical...
					Debug.LogWarning("Can't retrive list of all available premium accounts!");
				}
			}
			else if (action is GetUserData)
			{
				if (action.isSucceeded == true)
				{
					GetUserData data = (GetUserData)action;
					// -------------------------------------------------------------------------
					// Get nick name from cloud...
					if (data.dataID == CloudServices.PROP_ID_NICK_NAME)
					{
						string nickName = data.result;
						if (string.IsNullOrEmpty(nickName) == false)
						{
							m_NickName = GuiBaseUtils.FixNickname(nickName, m_UserName);
						}
					}
					// -------------------------------------------------------------------------
					// Get 'i want news' from cloud...
					else if (data.dataID == CloudServices.PROP_ID_I_WANT_NEWS)
					{
						bool receiveNews;
						if (bool.TryParse(action.result, out receiveNews) == true)
						{
							m_ReceiveNews = receiveNews;
						}
					}
					// -------------------------------------------------------------------------
					// Get account kind from cloud...
					else if (data.dataID == CloudServices.PROP_ID_ACCT_KIND)
					{
						try
						{
							m_UserAcctKind = (E_UserAcctKind)System.Enum.Parse(typeof (E_UserAcctKind), data.result, false);
						}
						catch
						{
						}
					}
				}
			}
		}

		return true;
	}

	void SetAuthenticationStatus(E_AuthenticationStatus status, string msg = null)
	{
		if (m_AuthenticationStatus == status)
			return;

		m_AuthenticationStatus = status;

		switch (m_AuthenticationStatus)
		{
		case E_AuthenticationStatus.None:
			m_LastAuthenticationFailReason = string.Empty;
			break;
		case E_AuthenticationStatus.Failed:
			m_LastAuthenticationFailReason = msg;

			#if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)
			if(FacebookPlugin.Instance.IsLoggedIn() == true)
			{
				FacebookPlugin.Instance.Logout(null);
			}
			#endif

			if (Debug.isDebugBuild == true || Application.isEditor == true)
			{
				string[] lines = new string[]
				{
					"Authentication failed:",
					msg ?? string.Empty
				};
				Debug.LogWarning(string.Join(System.Environment.NewLine, lines));
			}
			else
			{
				Debug.LogWarning("Authentication failed");
			}
			break;
		case E_AuthenticationStatus.Ok:
			if (Debug.isDebugBuild == true || Application.isEditor == true)
			{
				Debug.Log("Authentication succeful");
			}
			break;
		default:
			break;
		}
	}

	#endregion

	// =========================================================================================================================		

	#region --- Save load login (authentication) data...

	public bool GetLoginData(ref string outNickName,
							 ref string outUserName,
							 ref string outPaswordHash,
							 ref int outPasswordLength,
							 ref bool outRemeberLoginData,
							 ref bool outAutoLogin)
	{
		if (authenticationDataPresent == false)
			return false;

		outNickName = m_NickName;
		outUserName = m_UserName;
		outPaswordHash = m_PasswordHash;
		outPasswordLength = m_PasswordLength;
		outRemeberLoginData = m_AuthenticationDataLoaded;
		outAutoLogin = m_AutoLogin;
		return true;
	}

	public void SetLoginData(string inPrimaryKey,
							 string inNickName,
							 string inUserName,
							 string inPaswordHash,
							 int inPasswordLength,
							 bool inRemeberThem,
							 bool inAutoLogin)
	{
		m_PrimaryKey = inPrimaryKey;
		m_NickName = string.IsNullOrEmpty(inNickName) ? null : inNickName;
		m_UserName = string.IsNullOrEmpty(inUserName) ? null : inUserName.ToLower();
		m_PasswordHash = string.IsNullOrEmpty(inPaswordHash) ? null : inPaswordHash;
		m_PasswordLength = inPasswordLength;
		m_AutoLogin = inAutoLogin;

		m_AuthenticatedUserID = new UnigueUserID(primaryKey, passwordHash, productID);

		m_AuthenticationDataLoaded = inRemeberThem;
		if (inRemeberThem == true)
		{
			PlayerPrefs.SetString(NICKNAME_KEY, m_NickName);
			PlayerPrefs.SetString(USERNAME_KEY, m_UserName);
			PlayerPrefs.SetString(PRIMARY_KEY_KEY, m_PrimaryKey);
			PlayerPrefs.SetString(PASSWORD_HASH_KEY, m_PasswordHash);
			PlayerPrefs.SetInt(PASSWORD_LENGTH_KEY, m_PasswordLength);
			PlayerPrefs.SetInt(AUTO_LOGIN_KEY, m_AutoLogin ? 1 : 0);
			PlayerPrefs.Save();
		}
		else
		{
			// delete, if user don't want to store this values...
			PlayerPrefs.DeleteKey(NICKNAME_KEY);
			PlayerPrefs.DeleteKey(USERNAME_KEY);
			PlayerPrefs.DeleteKey(PRIMARY_KEY_KEY);
			PlayerPrefs.DeleteKey(PASSWORD_HASH_KEY);
			PlayerPrefs.DeleteKey(PASSWORD_LENGTH_KEY);
			PlayerPrefs.DeleteKey(AUTO_LOGIN_KEY);
			PlayerPrefs.Save();
		}
	}

	public void LoadAuthenticationData()
	{
		m_NickName = PlayerPrefs.GetString(NICKNAME_KEY, null);
		m_UserName = PlayerPrefs.GetString(USERNAME_KEY, null);
		m_PrimaryKey = PlayerPrefs.GetString(PRIMARY_KEY_KEY, null);
		m_PasswordHash = PlayerPrefs.GetString(PASSWORD_HASH_KEY, null);
		m_PasswordLength = PlayerPrefs.GetInt(PASSWORD_LENGTH_KEY, 0);
		m_AutoLogin = (PlayerPrefs.GetInt(AUTO_LOGIN_KEY, 0) != 0 ? true : false);

		if (string.IsNullOrEmpty(m_UserName) == false)
		{
			m_UserName = m_UserName.ToLower();
		}

		if (string.IsNullOrEmpty(m_PrimaryKey) == true)
		{
			m_PrimaryKey = m_UserName;
		}

		m_AuthenticationDataLoaded = authenticationDataPresent;

		if (m_AuthenticationDataLoaded == false)
		{
			// try to load old auth data...
			LoadAuthenticationData_OLD();
		}

		if (m_AuthenticationDataLoaded == true)
		{
			m_AuthenticatedUserID = new UnigueUserID(primaryKey, passwordHash, productID);
		}
	}

	void LoadAuthenticationData_OLD()
	{
		m_NickName = PlayerPrefs.GetString("MFNetworkUser.NickName", null);
		m_UserName = PlayerPrefs.GetString("MFNetworkUser.UserName", null);
		m_PrimaryKey = PlayerPrefs.GetString("MFNetworkUser.UserName", null);
		m_PasswordHash = PlayerPrefs.GetString("MFNetworkUser.PasswordHash", null);
		m_PasswordLength = PlayerPrefs.GetInt("MFNetworkUser.PasswordLength", 0);

		m_AuthenticationDataLoaded = authenticationDataPresent;

		if (m_AuthenticationDataLoaded == true)
		{
			// save them...
			SetLoginData(m_PrimaryKey, m_NickName, m_UserName, m_PasswordHash, m_PasswordLength, true, true);
		}
	}

	#endregion

	// =========================================================================================================================		

	#region --- Premium account...

	public DateTime GetPremiumAccountEndDateTime()
	{
		PlayerPersistantInfo player = PPIManager.Instance.GetLocalPPI();
		return player != null ? player.GetPremiumAccountEndDateTime() : CloudDateTime.UtcNow;
	}

	void OnPremiumAcctChanged(bool state)
	{
		// make a temporary copy of the event to avoid possibility of 
		// a race condition if the last subscriber unsubscribes 
		// immediately after the null check and before the event is raised
		PremiumAcctEventHandler handler = null;
		lock (this)
		{
			handler = m_PremiumAcctChanged;
		}

		// raise event
		if (handler != null)
		{
			handler(state);
		}
	}

	void CheckPremiumAcct(bool state)
	{
		if (state == true)
		{
			OnPremiumAcctChanged(isPremiumAccountActive);
			StartCoroutine("CheckPremiumAcct_Coroutine");
		}
		else
		{
			StopCoroutine("CheckPremiumAcct_Coroutine");
			OnPremiumAcctChanged(false);
		}
	}

	IEnumerator CheckPremiumAcct_Coroutine()
	{
		bool lastState = isPremiumAccountActive;
		bool state = lastState;

		while (true)
		{
			yield return new WaitForSeconds(1.0f);

			state = isPremiumAccountActive;
			if (lastState != state)
			{
				lastState = state;
				OnPremiumAcctChanged(state);
			}
		}
	}

	void CheckCloudDateTime()
	{
		GetCloudDateTime action = new GetCloudDateTime(authenticatedUserID);
		GameCloudManager.AddAction(action);
	}

	#endregion

	// =========================================================================================================================		

	#region --- Custom region...

	public GeoRegion GetActualRegion()
	{
		return m_CustomRegion != GeoRegion.None ? m_CustomRegion : m_RealRegion;
	}

	public GeoRegion GetRealRegion()
	{
		return m_RealRegion;
	}

	public GeoRegion GetCustomRegion()
	{
		return m_CustomRegion;
	}

	public void SetCustomRegion(GeoRegion region)
	{
		m_CustomRegion = region;
	}

	#endregion
}
