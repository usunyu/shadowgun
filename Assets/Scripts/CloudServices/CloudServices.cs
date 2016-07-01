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

//
// TODO:
//
// - Make all operations accessing user custom data safe from perspective of authorization
// - Add http traffic encryption
//

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using LitJson;

public enum E_HttpResultCode
{
	None = 0,
	// 2xx success
	Ok = 200,
	AlreadyProcessed = 208,
	// 4xx client error
	BadRequest = 400,
	Unauthorized = 401,
	Forbidden = 403,
	NotFound = 404,
	RequestTimeout = 408,
	Conflict = 409,
	ExpectationFailed = 417,
	ParameterNotUnderstood = 451,
	// 5xx server error
	InternalServerError = 500,
	ServiceUnavailable = 503,
}

[AddComponentMenu("Cloud services/CloudServices")]
public class CloudServices : MonoBehaviour
{
	const string URL_CURRENT_GAME = "/sgdz";

	// 
	// Set to true to enable outgoing traffic encryption
	//
	public bool m_EnableEncryption = true;

	//
	// Set to true if you wish to handle Apple StoreKit requests on sandbox server
	//

	public bool m_EnableStoreKitSandbox = true;
	//
	// NOTE:
	//
	// Don't change PROP_ID_XXX constants. If you have to do it, fix cloud service servlet constants
	// accordingly.
	//
	public const string PROP_ID_DEFAULT_PLAYER_DATA = "_DefaultPlayerData";
	public const string PROP_ID_PLAYER_DATA = "_PlayerData";
	public const string PROP_ID_SHOP_ITEMS = "_ShopItems";
	public const string PROP_ID_GAME_SETTINGS = "_GameSettings";
	public const string PROP_ID_PREMIUM_ACC_DESC = "_PremiumAccountsDesc";
	public const string PROP_ID_FRIENDS = "_Friends";
	public const string PROP_ID_PROGRESS = "_Progress";
	public const string PROP_ID_SWEAR_WORDS = "_SwearWords";
	public const string PROP_ID_EMAIL = "Email";
	public const string PROP_ID_I_WANT_NEWS = "IWantNews";
	public const string PROP_ID_ACCT_KIND = "AccountKind";
	public const string PROP_ID_APP_PROVIDER = "AppProvider";
	public const string PROP_ID_NICK_NAME = "NickName";
	public const string PROP_ID_USERNAME = "Username";
	public const string PROP_ID_PASSWORD = "pw";
	public const string PROP_ID_CUSTOM_REGION = "CustomRegion";
	public const string PROP_ID_DESC = "desc";
	public const string PROP_ID_CODE = "code";

	public const string SECTION_ID_PARAMS = "Params";

	public const string RESP_OK = "ok";
	public const string RESP_ALREADY_PROCESSED = "alreadyproc";
	public const string RESP_NOT_FOUND = "notfound";
	public const string RESP_INVALID_USERID = "invalidusr";
	public const string RESP_DB_ERROR = "dberror";
	public const string RESP_INVALID_PARAMS = "invalidparams";
	public const string RESP_INVALID_REQUEST = "invalidreq";
	public const string RESP_GENERAL_ERROR = "error";
	public const string RESP_OPERATION_REFUSED = "refused";

	public const string RESPONSE_PROP_ID_MESSAGES = "messages";
	public const string RESPONSE_PROP_ID_LAST_MSG_IDX = "lastMsgIdx";
	public const string RESPONSE_PROP_ID_SERVER_CMD = "_respCmd";

	public const string RESPONSE_HTTP_RESULT = "httpResult";

	public const string LINK_ID_TYPE_DEVICE = "DeviceID";
	public const string LINK_ID_TYPE_FACEBOOK = "FacebookID";
	public const string LINK_ID_TYPE_IOSVENDOR = "iOSVendorID";
	public const string LINK_ID_TYPE_IOSADVERTISING = "iOSAdvertisingID";

	public const string CONFIG_IP = "IP";
	public const string CONFIG_GAME_TYPES = "GameTypes";
	public const string CONFIG_SHOP_ID_TABLE = "ShopIDTable";
	public const string CONFIG_DAILY_REWARDS = "DailyRewards";
	public const string CONFIG_GENERIC = "";

	public const string PARAM_ID_USERID = "userid";
	public const string PARAM_ID_CMD = "cmd";
	public const string PARAM_ID_DATA = "data";
	public const string PARAM_ID_SIGNATURE = "sig";
	public const string PARAM_ID_PRODUCT_ID = "prodId";
	public const string PARAM_ID_APP_STORE_TRANSACTION_ID = "appStoreTransId";
	public const string PARAM_ID_PARAM = "param";
	public const string PARAM_ID_PARAM2 = "param2";
	public const string PARAM_ID_PASSWORD = "pw";
	public const string PARAM_ID_GUID = "id";
	public const string PARAM_ID_KEY = "key";
	public const string PARAM_ID_VAL = "val";
	public const string PARAM_ID_REPROVISION = "force";
	public const string PARAM_ID_EMAIL = "email";
	public const string PARAM_ID_UTC_OFFSET = "utcOffset";
	public const string PARAM_ID_APP_VER_MAJOR = "aVerMaj";
	public const string PARAM_ID_APP_VER_MINOR = "aVerMin";
	public const string PARAM_ID_APP_VER_BUILD = "aVerBld";
	public const string PARAM_ID_ID = "id";
	public const string PARAM_ID_VERSION = "ver";

	const string CMD_ID_CREATE_PRODUCT = "createProduct";
	const string CMD_ID_SET_PRODUCT_DATA = "setProductData";
	const string CMD_ID_GET_PRODUCT_DATA = "getProductData";
	const string CMD_ID_GET_USER_TRANSACTIONS = "getUserTransactions";
	const string CMD_ID_CREATE_LEADERBOARD = "createLeaderboard";
	const string CMD_ID_LEADERBOARD_SET_SCORES = "leaderboardSetScores";
	const string CMD_ID_LEADERBOARD_GET_RANKS = "leaderboardGetRanks";
	const string CMD_ID_LEADERBOARD_GET_RANKS_AND_SCORES = "leaderboardGetRanksAndScores";
	const string CMD_ID_LEADERBOARD_QUERY = "leaderboardQuery";
	const string CMD_ID_ADD_USR_PRODUCT_DATA = "addUsrProduct";
	const string CMD_ID_VALIDATE_IOS_RECEIPT = "validateIOSReceipt";
	const string CMD_ID_PROCESS_IAP_IOS = "processInAppPurchIOS";
	const string CMD_ID_PROCESS_IAP_IOS_VERSION = "processInAppPurchIOSVersion";
	const string CMD_ID_PROCESS_IAP_GOOGLE = "processInAppPurchGoogle";
	const string CMD_ID_PROCESS_IAP_GOOGLE_VERSION = "processInAppPurchGoogleVersion";
	const string CMD_ID_PROCESS_IAP_FACEBOOK = "processInAppPurchFacebook";
	const string CMD_ID_IAP_FACEBOOK_PAYMENT_INFO = "inAppPurchFacebookPaymentInfo";
	const string CMD_ID_REGISTER_FOR_PUSH_NOTIFICATIONS = "registerForPushNotifications";
	const string CMD_ID_GET_REGION_INFO = "getRegionInfo";
	const string CMD_ID_USER_SET_PER_PRODUCT_DATA = "setUsrPerProductData";
	const string CMD_ID_USER_GET_PER_PRODUCT_DATA = "getUsrPerProductData";
	const string CMD_ID_SET_USER_DATA = "setUsrData";
	const string CMD_ID_GET_USER_DATA = "getUsrData";
	const string CMD_ID_GET_PUBLIC_USER_DATA = "getPublicUserData";
	const string CMD_ID_GET_USER_DATA_LIST = "getUsrDataList";
	const string CMD_ID_GET_DATA_ON_USER_LIST = "adminGetUserDataOnUserList";
	const string CMD_ID_BUY_BUILTIN_ITEM = "buyBuiltInItem";
	const string CMD_ID_REFUND_BUILTIN_ITEM = "refundBuiltInItem";
	const string CMD_ID_EQUIP_ITEM = "equipItem";
	const string CMD_ID_UNEQUIP_ITEM = "unEquipItem";
	const string CMD_ID_MODIFY_ITEM = "modifyItem";
	const string CMD_ID_SET_DATA_SECTION = "setDataSection";
	const string CMD_ID_UPDATE_DATA_SECTION = "updateDataSection";
	const string CMD_ID_CREATE_USER = "createUser";
	const string CMD_ID_REMOVE_USER = "removeUser";
	const string CMD_ID_CREATE_PUSH_NOTIFICATION = "createPushNotification";
	const string CMD_ID_ADMIN_BAN_DEVICE = "addDeviceBan";
	const string CMD_ID_ADMIN_UNBAN_DEVICE = "unbanDevice";
	const string CMD_ID_ADMIN_IS_DEVICE_BANNED = "isDeviceBanned";
	const string CMD_ID_DELETE_LEADERBOARD_CONTENT = "deleteLeaderboardContent";
	const string CMD_ID_USER_EXISTS = "userExists";
	const string CMD_ID_BUY_ITEM = "buyItem";
	const string CMD_ID_TRANSACTION_PROCESSED = "transProcessed";
	const string CMD_ID_VALIDATE_USER_ACCOUNT = "validateAccount";
	const string CMD_ID_QUERY_FRIENDS_INFO = "getFriendsInfo";
	const string CMD_ID_REQUEST_ADD_FRIEND = "reqAddFriend";
	const string CMD_ID_REQUEST_DEL_FRIEND = "reqDelFriend";
	const string CMD_ID_PICKUP_INBOX_MESSAGES = "fetchInboxMsgs";
	const string CMD_ID_INBOX_ADD_MSG = "inboxAddMsg";
	const string CMD_ID_INBOX_REMOVE_MESSAGES = "inboxRemoveMsgs";
	const string CMD_ID_REQUEST_RESET_PASSWORD = "reqResetPw";
	const string CMD_ID_REQUEST_RESET_PASSWORD_WITH_EMAIL = "reqResetPwEmail";
	const string CMD_ID_BUY_PREMIUM_ACCOUNT = "buyPremiumAccount";
	const string CMD_ID_SLOTMACHINE_SPIN = "slotmachineSpin";
	const string CMD_ID_GET_DAILY_REWARDS = "getDailyRewards";
	const string CMD_ID_GET_CLOUD_DATE_TIME = "getCloudDateTime";
	const string CMD_ID_GET_SLOTMACHINE_JACKPOT = "getSMJackpot";
	const string CMD_ID_QUERY_USERS_BY_FIELD = "queryUsersByField";
	const string CMD_ID_GET_CONFIG = "getConfig";
	const string CMD_ID_SET_CONFIG = "setConfig";
	const string CMD_ID_ADD_CONFIG = "addConfig";
	const string CMD_ID_LINK_ID_WITH_USER = "linkIDWithUser";
	const string CMD_ID_GET_USER_LINKED_WITH_ID = "getUserLinkedWithID";
	const string CMD_ID_GET_USERS_LINKED_WITH_ID = "getLinkedUsers";
	const string CMD_ID_BATCH_COMMAND = "batchCommand";
	const string CMD_ID_GET_ENTITY_FIELD = "getEntityField";
	const string CMD_ID_APP_LICENSING_OUYA = "appLicensingOuya";
	const string CMD_ID_GET_LEADERBOARD_USERS_COUNT = "getLBoardUsersCount";

	const int NUM_DB_UPDATE_RETRIES = 5;
	const float DB_UPDATE_RETRY_WAIT_MS = 500;
	const float PASSWORD_UPDATE_PERIOD_SEC = 30;
	const float ASYNC_OP_CHAIN_WAIT_TIMEOUT_SEC = 0.1f;

	//
	// This value must match cloud service settings (Test2Servlet.java EnableOutgoingDataEncryption)
	//
	const bool IncommingTrafficEncrypted = false;

	// Don't change names of enum values - the name is converted to the string and sent to the cloud application
	public enum IAPGoogleVersion
	{
		APIV3MFGInternalV2, // for Google API 3 plus new internal version of cloud application ( JSON results, result codes review )
	}

	// Don't change names of enum values - the name is converted to the string and sent to the cloud application
	public enum IAPVersion
	{
		InternalV2, // new internal version of cloud application ( JSON results, result codes review )
	}

	public class AsyncOpResult
	{
		public delegate void AsyncOpResDelegate(AsyncOpResult res);

		public bool m_Res = false;
		public bool m_Finished = false;
		public string m_ResultDesc;
		public string m_DbgId;
		public object m_UserData;
		internal string m_Password;

		// this shouldn't be used directly to prevent fast-speed replies problems from the cloud
		protected List<AsyncOpResDelegate> m_Listeners = new List<AsyncOpResDelegate>();

		// TODO : currently there can be set one listener only using constructor
		internal AsyncOpResult(string password, AsyncOpResDelegate listener, object userData = null)
		{
			m_Password = password;

			if (listener != null)
			{
				m_Listeners.Add(listener);
			}

			if (null != userData)
			{
				m_UserData = userData;
			}
		}

		public void Finished()
		{
//			Debug.Log("Async op: '" + m_DbgId + "' finished with result : " + m_Res + "(" + m_ResultDesc + ")"); 

			m_Finished = true;

			foreach (AsyncOpResDelegate curr in m_Listeners)
			{
				curr(this);
			}
		}
	}

	public class AsyncOpResultChain
	{
		public delegate void AsyncOpResChainDelegate(AsyncOpResultChain res);

		public List<AsyncOpResult> m_PendingOps = new List<AsyncOpResult>();
		public bool m_Finished = false;
		AsyncOpResChainDelegate m_Listener;

		internal AsyncOpResultChain(AsyncOpResChainDelegate listener)
		{
			m_Listener = listener;
		}

		public void Finished()
		{
			m_Finished = true;

			if (m_Listener != null)
			{
				m_Listener(this);
			}
		}
	}

	public struct S_ModItemInfo
	{
		public int m_GUID;
		public string m_Key;
		public string m_Val;

		public S_ModItemInfo(int guid, string key, string val)
		{
			m_GUID = guid;
			m_Key = key;
			m_Val = val;
		}
	};

	public struct S_LeaderBoardScoreInfo
	{
		public int m_Score;
		public string m_UserName;

		public S_LeaderBoardScoreInfo(int score, string userName)
		{
			m_Score = score;
			m_UserName = userName;
		}
	};

	//
	// NOTE: don't remove / rename members of this class as it serves as schema for
	// cloud service
	//

	[System.Serializable]
	public class PremiumAccountDesc
	{
		public string m_Id;
		public int m_DurationInMinutes;
		public int m_PriceGold;

		// Value from interval <0,1>. Final price is calculated as finalPrice = PriceGold * DiscountMultiplier
		public float m_DiscountMultiplier;
	};

	// Symmetric encryption password size. Don't change this value to retain decryption compatibility with cloud service.
	const int SYMMETRIC_ENC_PW_SIZE = 24;

	// Symmetric encryption Initial Vector size. Don't change this value to retain decryption compatibility with cloud service.
	const int SYMMETRIC_ENC_IV_SIZE = 8;

	RSACryptoServiceProvider m_RSA;
	TripleDESCryptoServiceProvider m_SymmetricEnc;
	ICryptoTransform m_SymmetricEncEncryptor;
	RNGCryptoServiceProvider m_RndGen = new RNGCryptoServiceProvider();

	// Randomly generated password for symmetric encryption
	byte[] m_SymmetricEncPassword = new byte[SYMMETRIC_ENC_PW_SIZE];

	// Randomly generated initial vector for symmetric encryption
	byte[] m_SymmetricEncIV = new byte[SYMMETRIC_ENC_IV_SIZE];

	// Symmetric encryption password encrypted by cloud service public key, encoded in Base64. This is the password
	// passed to cloud service together with each request (can be only decoded by server).	
	string m_SymmetricEncPasswordBase64;
	string m_SymmetricEncIVBase64;

	float m_LastPasswordChangeTime = -1;

	// You can use this value to control number of failures introduced for debugging purposes.
	// Valid values are from interval <0,1>
	float m_DbgIntroducedFailureRate = -1;

	static CloudServices ms_Instance;

	void OnDestroy()
	{
		ms_Instance = null;
	}

	public static CloudServices GetInstance()
	{
		if (ms_Instance == null)
		{
			GameObject go = new GameObject("CloudServices");
			ms_Instance = go.AddComponent<CloudServices>();

			ms_Instance.InitEncryption();

			GameObject.DontDestroyOnLoad(ms_Instance);
		}

		return ms_Instance;
	}

	public static string GetCurrentURLBase()
	{
		return CloudConfiguration.GetCurrentURLBase();
	}

	public static MFGCloudService GetCurrentCloudService()
	{
		return CloudConfiguration.GetCurrentCloudService();
	}

	string GetURLCurrentGame()
	{
		return "http://" + GetCurrentURLBase() + URL_CURRENT_GAME;
	}

	void InitEncryption()
	{
		m_RSA = new RSACryptoServiceProvider();
		m_RSA.FromXmlString(CloudConfiguration.PublicRSAKeyAsXML);

		RefreshSymmetricEncParams();
	}

	void RefreshSymmetricEncParams()
	{
		MFDebugUtils.Assert(m_RndGen != null);

		//
		// generate random password for symmetric encryption
		//

		m_RndGen.GetBytes(m_SymmetricEncPassword);
		m_RndGen.GetBytes(m_SymmetricEncIV);

		m_SymmetricEnc = new TripleDESCryptoServiceProvider();

		m_SymmetricEnc.Key = m_SymmetricEncPassword;
		m_SymmetricEnc.IV = m_SymmetricEncIV;
		m_SymmetricEnc.Mode = CipherMode.CBC;

		m_SymmetricEncEncryptor = m_SymmetricEnc.CreateEncryptor();

		MFDebugUtils.Assert(m_SymmetricEncEncryptor != null);

		m_SymmetricEncPasswordBase64 = Convert.ToBase64String(AsymmetricEncrypt(m_SymmetricEncPassword));
		m_SymmetricEncIVBase64 = Convert.ToBase64String(m_SymmetricEncIV);

//		Debug.Log(m_SymmetricEncEncryptor.CanReuseTransform);
//		Debug.Log(m_SymmetricEncPasswordBase64);
//		Debug.Log(m_SymmetricEncIVBase64);
//		Debug.Log("Changed symmetric enc password");
	}

	//
	// Creates hashed version from specified plain text password
	//

	public static string CalcPasswordHash(string password)
	{
		byte[] output = CalcSHA1Hash(password + CloudConfiguration.Salt);

		return BitConverter.ToString(output).Replace("-", "");
	}

	static byte[] CalcSHA1Hash(string str)
	{
		SHA1 sha = new SHA1CryptoServiceProvider();
		byte[] input = Encoding.UTF8.GetBytes(str);

		return sha.ComputeHash(input);
	}

	public static ulong CalcHash64(string str)
	{
		byte[] data = CalcSHA1Hash(str);

		MFDebugUtils.Assert(data.Length >= 16);

		return BitConverter.ToUInt64(data, 0) ^ BitConverter.ToUInt64(data, 8);
	}

	public static E_HttpResultCode ParseResultCode(string result, E_HttpResultCode defaultCode)
	{
		switch (result)
		{
		// 2xx success
		case RESP_OK:
			return E_HttpResultCode.Ok;
		case RESP_ALREADY_PROCESSED:
			return E_HttpResultCode.AlreadyProcessed;
		// 4xx client error
		case RESP_INVALID_REQUEST:
			return E_HttpResultCode.BadRequest;
		case RESP_INVALID_USERID:
			return E_HttpResultCode.Unauthorized;
		case "unauthorized":
			return E_HttpResultCode.Unauthorized;
		case RESP_NOT_FOUND:
			return E_HttpResultCode.NotFound;
		case RESP_OPERATION_REFUSED:
			return E_HttpResultCode.Forbidden;
		case "forbidden":
			return E_HttpResultCode.Forbidden;
		case "conflict":
			return E_HttpResultCode.Conflict;
		case "expectfailed":
			return E_HttpResultCode.ExpectationFailed;
		case RESP_INVALID_PARAMS:
			return E_HttpResultCode.ParameterNotUnderstood;
		// 5xx server error
		case RESP_GENERAL_ERROR:
			return E_HttpResultCode.InternalServerError;
		case "interror":
			return E_HttpResultCode.InternalServerError;
		case RESP_DB_ERROR:
			return E_HttpResultCode.ServiceUnavailable;
		case "unavailable":
			return E_HttpResultCode.ServiceUnavailable;
		// default
		default:
			return defaultCode;
		}
	}

	public class ParamsFormatter
	{
		StringBuilder sb;
		JsonWriter writer;
		public string cmdName { get; private set; }

		public ParamsFormatter(string inCmdName)
		{
			sb = new StringBuilder();
			writer = new JsonWriter(sb);

			writer.WriteObjectStart();

			cmdName = inCmdName;

			AddField(PARAM_ID_CMD, cmdName);
		}

		public void AddField(string id, string val)
		{
			writer.WritePropertyName(id);
			writer.Write(val);
		}

		public override string ToString()
		{
			writer.WriteObjectEnd();
			return sb.ToString();
		}
	}

	//
	// Creates new user context on cloud service
	//
	// TODO:
	// 
	// We need some security mechanism to avoid the possibility of users
	// to create new player contexts at their will (this could lead to DOS attacks)
	//

	public AsyncOpResult CreateUser(string uniqueUserID,
									string productID,
									string passwordHash,
									bool usePrimaryKey,
									AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(CreateUser(uniqueUserID, productID, passwordHash, usePrimaryKey, res));

		return res;
	}

	public AsyncOpResult RemoveUser(string userId, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_REMOVE_USER,
			pw = passwordHash,
			userid = userId
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	public AsyncOpResult AdminCreatePushNotification(string productId,
													 string title,
													 string text,
													 string passwordHash,
													 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_CREATE_PUSH_NOTIFICATION,
			prodId = productId,
			param = title,
			param2 = text,
			pw = passwordHash
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	public AsyncOpResult AdminBanDevice(string deviceID, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_ADMIN_BAN_DEVICE,
			param = deviceID,
			pw = passwordHash
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	public AsyncOpResult AdminUnbanDevice(string deviceID, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_ADMIN_UNBAN_DEVICE,
			param = deviceID,
			pw = passwordHash
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	public AsyncOpResult AdminIsDeviceBanned(string deviceID, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_ADMIN_IS_DEVICE_BANNED,
			param = deviceID,
			pw = passwordHash
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	// ------------------------------------------------------------------------
	public AsyncOpResult AdminGetCrackLogEntries(string user, string adminPasswordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "getCrackLogEntries",
			userid = user,
			pw = adminPasswordHash
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult(adminPasswordHash, listener));
	}

	public AsyncOpResult AdminCalcHash64(string textToHash, string adminPasswordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "adminCalcHash64",
			param = textToHash,
			pw = adminPasswordHash
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult(adminPasswordHash, listener));
	}

	public AsyncOpResult AdminKontagentTest(string userId,
											string productId,
											string adminPasswordHash,
											AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "adminKontagentTest",
			prodId = productId,
			pw = adminPasswordHash,
			userid = userId
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult(adminPasswordHash, listener));
	}

	public AsyncOpResult AdminDeleteLeaderboardContent(string productId,
													   string leaderboard,
													   string passwordHash,
													   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_DELETE_LEADERBOARD_CONTENT,
			prodId = productId,
			param = leaderboard,
			pw = passwordHash
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	public AsyncOpResult UserNameExists(string userId, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatUserNameExists(userId), new AsyncOpResult("", listener));
	}

	public static object FormatUserNameExists(string userId)
	{
		return new
		{
			cmd = CMD_ID_USER_EXISTS,
			userid = userId
		};
	}

	public AsyncOpResult UserExists(string userId, string productId, string password, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatUserExists(userId, productId, password), new AsyncOpResult(password, listener));
	}

	public static object FormatUserExists(string userId, string productId, string password)
	{
		bool pwd = !string.IsNullOrEmpty(password);
		bool prodId = !string.IsNullOrEmpty(productId);

		/*
		List<object> formated = new List<object>();
		
		formated.Add( new { cmd = CMD_ID_USER_EXISTS, userid = userId } );
		
		if( pwd )
		{
			formated.Add( new { pw = password } );
		}
		
		if( prodId )
		{
			formated.Add( new { prodId = productId } );
		}
		
		Debug.LogWarning( JsonMapper.ToJson( formated.ToArray() ) );
		*/

		if (pwd && prodId)
			return new {cmd = CMD_ID_USER_EXISTS, userid = userId, pw = password, prodId = productId};
		else if (pwd)
			return new {cmd = CMD_ID_USER_EXISTS, userid = userId, pw = password};
		else if (prodId)
			return new {cmd = CMD_ID_USER_EXISTS, userid = userId, prodId = productId};
		else
			return new {cmd = CMD_ID_USER_EXISTS, userid = userId};
	}

	public AsyncOpResult GetMyRegionInfo(AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatGetMyRegionInfo(), new AsyncOpResult("", listener));
	}

	public static object FormatGetMyRegionInfo()
	{
		return new
		{
			cmd = CMD_ID_GET_REGION_INFO
		};
	}

	public AsyncOpResult ValidateUserAccount(string userId, string productId, string password, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessWebRequestObject(FormatValidateUserAccount(userId, productId, password), new AsyncOpResult(password, listener));
	}

	public static object FormatValidateUserAccount(string userId, string productId, string password)
	{
		return new
		{
			cmd = CMD_ID_VALIDATE_USER_ACCOUNT,
			prodId = productId,
			pw = password,
			userid = userId,
			aVerMaj = BuildInfo.Version.Major.ToString(),
			aVerMin = BuildInfo.Version.Minor.ToString(),
			aVerBld = BuildInfo.Version.Build.ToString()
		};
	}

	public AsyncOpResult GetUserLinkedWithID(string id, string idType, string accountType, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult("", listener);

		StartCoroutine(GetUserLinkedWithID(id, idType, accountType, res));

		return res;
	}

	public static object FormatGetUserLinkedWithID(string id, string idType, string accountType)
	{
		return new
		{
			cmd = CMD_ID_GET_USER_LINKED_WITH_ID,
			data = id,
			param = idType,
			param2 = accountType
		};
	}

	public AsyncOpResult GetUsersLinkedWithID(string id, string idType, string accountType, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatGetUsersLinkedWithID(id, idType, accountType), new AsyncOpResult(null, listener));
	}

	public static object FormatGetUsersLinkedWithID(string id, string idType, string accountType)
	{
		return new
		{
			cmd = CMD_ID_GET_USERS_LINKED_WITH_ID,
			data = id,
			param = idType,
			param2 = accountType,
		};
	}

	public AsyncOpResult GetIOSVendorIDInfo(string id, string idType, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatGetIOSVendorIDInfo(id, idType), new AsyncOpResult(null, listener));
	}

	public static object FormatGetIOSVendorIDInfo(string id, string idType)
	{
		return new
		{
			cmd = "iOSVendorIDInfo",
			param = idType,
			param2 = id
		};
	}

	public AsyncOpResult BatchCommand(string stringJSON, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult("", listener);

		StartCoroutine(BatchCommand(stringJSON, res));

		return res;
	}

	public AsyncOpResult UserSetPerProductData(string userId,
											   string productId,
											   string key,
											   string val,
											   string passwordHash,
											   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(UserSetProductSpecificData(userId, productId, key, val, passwordHash, res));

		return res;
	}

	public AsyncOpResult UserGetPerProductData(string userId,
											   string productId,
											   string key,
											   string passwordHash,
											   AsyncOpResult.AsyncOpResDelegate listener = null,
											   object userData = null)
	{
		return ProcessGetWebRequestObject(FormatUserGetPerProductData(userId, productId, key, passwordHash),
										  new AsyncOpResult(passwordHash, listener, userData));
	}

	public static object FormatUserGetPerProductData(string userId, string productId, string paramId, string password)
	{
		return new
		{
			cmd = CMD_ID_USER_GET_PER_PRODUCT_DATA,
			userid = userId,
			prodId = productId,
			param = paramId,
			pw = password
		};
	}

	public AsyncOpResult UserSetPerProductDataSection(string userId,
													  string productId,
													  string key,
													  string sectionId,
													  string val,
													  string passwordHash,
													  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(UserSetPerProductDataSection(userId, productId, key, sectionId, val, passwordHash, res));

		return res;
	}

	public AsyncOpResult UserUpdatePerProductDataSection(string userId,
														 string productId,
														 string key,
														 string sectionId,
														 string val,
														 string passwordHash,
														 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(UserUpdatePerProductDataSection(userId, productId, key, sectionId, val, passwordHash, res));

		return res;
	}

	public AsyncOpResult UserSetData(string userId,
									 string key,
									 string val,
									 string passwordHash,
									 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(SetUserData(userId, key, val, passwordHash, res));

		return res;
	}

	public AsyncOpResult UserGetData(string userId, string key, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatUserGetData(userId, key, passwordHash), new AsyncOpResult(passwordHash, listener));
	}

	public static object FormatUserGetData(string userId, string fieldId, string password)
	{
		return new
		{
			cmd = CMD_ID_GET_USER_DATA,
			userid = userId,
			param = fieldId,
			pw = password
		};
	}

	public AsyncOpResult UserGetPublicData(string userId, string fieldId, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatUserGetPublicData(userId, fieldId), new AsyncOpResult(null, listener));
	}

	public static object FormatUserGetPublicData(string userId, string fieldId)
	{
		return new
		{
			cmd = CMD_ID_GET_PUBLIC_USER_DATA,
			userid = userId,
			param = fieldId,
		};
	}

	public AsyncOpResult UserGetDataList(string userId, string[] keys, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatUserGetDataList(userId, keys, passwordHash), new AsyncOpResult(passwordHash, listener));
	}

	public static object FormatUserGetDataList(string userId, string[] fieldIds, string password)
	{
		string jsonList = JsonMapper.ToJson(fieldIds);

		return new
		{
			cmd = CMD_ID_GET_USER_DATA_LIST,
			userid = userId,
			param = jsonList,
			pw = password
		};
	}

	public AsyncOpResult AdminGetDataOnUserList(string[] users,
												string[] keys,
												string passwordHash,
												AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		string userList = JsonMapper.ToJson(users);
		string keyList = JsonMapper.ToJson(keys);

		object httpData = new
		{
			cmd = CMD_ID_GET_DATA_ON_USER_LIST,
			param = userList,
			param2 = keyList,
			pw = passwordHash
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	public AsyncOpResult BuyItem(string userId,
								 string productId,
								 int itemID,
								 string passwordHash,
								 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(BuyItem(userId, productId, itemID, passwordHash, res));

		return res;
	}

	public AsyncOpResult RefundItem(string userId,
									string productId,
									int itemID,
									string passwordHash,
									AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);
		int[] itemsIDs = new int[1];

		itemsIDs[0] = itemID;

		StartCoroutine(RefundItem(userId, productId, itemsIDs, passwordHash, res));

		return res;
	}

	public AsyncOpResult RefundItems(string userId,
									 string productId,
									 int[] itemsIDs,
									 string passwordHash,
									 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(RefundItem(userId, productId, itemsIDs, passwordHash, res));

		return res;
	}

	public AsyncOpResult EquipItem(string userId,
								   string productId,
								   int itemID,
								   int slotIdx,
								   string passwordHash,
								   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(EquipItem(userId, productId, itemID, slotIdx, passwordHash, res));

		return res;
	}

	public AsyncOpResult UnEquipItem(string userId,
									 string productId,
									 int itemID,
									 int slotIdx,
									 string passwordHash,
									 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(UnEquipItem(userId, productId, itemID, slotIdx, passwordHash, res));

		return res;
	}

//	public AsyncOpResult UpdatePlayerDataSection(string userId,string productId,string passwordHash,string sectionId,string sectionData)
//	{
//		return new AsyncOpResult();
//	}

	//
	// This operation modifies settings of specified item. It is only available for 'master'
	// account (ie. dedicated server)
	//
	public AsyncOpResult ModifyItem(string userId,
									string productId,
									int itemID,
									string key,
									string val,
									string passwordHash,
									AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		S_ModItemInfo[] items = new S_ModItemInfo[1] {new S_ModItemInfo(itemID, key, val)};

		StartCoroutine(ModifyItems(userId, productId, items, passwordHash, res));

		return res;
	}

	public AsyncOpResult ModifyItems(string userId,
									 string productId,
									 S_ModItemInfo[] items,
									 string passwordHash,
									 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(ModifyItems(userId, productId, items, passwordHash, res));

		return res;
	}

	//
	// Creates new product info record. This function fails, if product already exists.
	//

	public AsyncOpResult CreateProduct(string productID, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(CreateProduct(productID, passwordHash, res));

		return res;
	}

	//
	// Sets specified product <key,value> param
	//

	public AsyncOpResult ProductSetParam(string productID,
										 string paramId,
										 string paramVal,
										 string passwordHash,
										 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(ProductSetParam(productID, paramId, paramVal, passwordHash, res));

		return res;
	}

	//
	// Retrieves specified product param
	//

	public AsyncOpResult ProductGetParam(string productID,
										 string paramId,
										 string passwordHash,
										 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatProductGetParam(productID, paramId, passwordHash), new AsyncOpResult(passwordHash, listener));
	}

	public static object FormatProductGetParam(string productID, string paramId, string password)
	{
		return new
		{
			cmd = CMD_ID_GET_PRODUCT_DATA,
			prodId = productID,
			pw = password,
			param = paramId
		};
	}

	public AsyncOpResult GetUserTransactions(string productID,
											 string userId,
											 string passwordHash,
											 int maxResults,
											 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatGetUserTransactions(productID, userId, passwordHash, maxResults),
										  new AsyncOpResult(passwordHash, listener));
	}

	public static object FormatGetUserTransactions(string productID, string userId, string passwordHash, int maxResults)
	{
		return new
		{
			cmd = CMD_ID_GET_USER_TRANSACTIONS,
			prodId = productID,
			userid = userId,
			pw = passwordHash,
			param = maxResults
		};
	}

	public AsyncOpResult QueryFriendsInfo(string userId,
										  string productId,
										  string friendsListJSON,
										  string passwordHash,
										  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(QueryFriendsInfo(userId, productId, friendsListJSON, passwordHash, res));

		return res;
	}

	public AsyncOpResult RequestAddFriend(string userId,
										  string friendUserId,
										  string infoMessage,
										  string passwordHash,
										  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(RequestAddFriend(userId, friendUserId, infoMessage, passwordHash, res));

		return res;
	}

	public AsyncOpResult RequestDelFriend(string userId,
										  string friendUserId,
										  string passwordHash,
										  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(RequestDelFriend(userId, friendUserId, passwordHash, res));

		return res;
	}

	public AsyncOpResult FetchInboxMessages(string userId,
											string productId,
											string mailboxName,
											string passwordHash,
											int startMsgIdx = 0,
											AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(FetchInboxMessages(startMsgIdx, userId, productId, mailboxName, passwordHash, res));

		return res;
	}

	public AsyncOpResult FetchProductInboxMessages(string productId,
												   string mailboxName,
												   string passwordHash,
												   int startMsgIdx = 0,
												   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(FetchInboxMessages(startMsgIdx, string.Empty, productId, mailboxName, passwordHash, res));

		return res;
	}

	public AsyncOpResult InboxRemoveMessages(string userId,
											 string productId,
											 int lastMessageIdx,
											 string passwordHash,
											 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(InboxRemoveMessages(userId, productId, lastMessageIdx, passwordHash, res));

		return res;
	}

	public AsyncOpResult ProductInboxRemoveMessages(string productId,
													int lastMessageIdx,
													string passwordHash,
													AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(InboxRemoveMessages(string.Empty, productId, lastMessageIdx, passwordHash, res));

		return res;
	}

	public AsyncOpResult InboxAddMsg(string userId,
									 string targetUserId,
									 string productId,
									 string msg,
									 string passwordHash,
									 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(InboxAddMsg(userId, targetUserId, productId, msg, passwordHash, res));

		return res;
	}

	public AsyncOpResult ProductInboxAddMsg(string productId, string msg, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(InboxAddMsg(string.Empty, string.Empty, productId, msg, passwordHash, res));

		return res;
	}

	public AsyncOpResult ProcessResponseCmd(string cmdData, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult("", listener);

		StartCoroutine(ProcessResponseCmd(cmdData, res));

		return res;
	}

	public AsyncOpResult RequestResetPassword(string userId,
											  string msgBody,
											  string msgSubject,
											  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult("", listener);

		StartCoroutine(RequestResetPassword(userId, msgBody, msgSubject, res));

		return res;
	}

	public AsyncOpResult RequestResetPasswordWithEmail(string userId,
													   string userEmail,
													   string msgBody,
													   string msgSubject,
													   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult("", listener);

		StartCoroutine(RequestResetPasswordWithEmail(userId, userEmail, msgBody, msgSubject, res));

		return res;
	}

	public AsyncOpResult VerifyStoreKitReceipt(string receiptData, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult("", listener);

		StartCoroutine(VerifyStoreKitReceipt(receiptData, res));

		return res;
	}

	public AsyncOpResult CreateLeaderboard(string productID,
										   string leaderboardID,
										   string passwordHash,
										   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(CreateLeaderboard(productID, leaderboardID, passwordHash, res));

		return res;
	}

	public AsyncOpResult LeaderboardSetScores(string productID,
											  string leaderboardID,
											  string passwordHash,
											  S_LeaderBoardScoreInfo[] scores,
											  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(LeaderboardSetScores(productID, leaderboardID, passwordHash, scores, res));

		return res;
	}

	public AsyncOpResult LeaderboardGetRanks(string userID,
											 string productID,
											 string leaderboardID,
											 string passwordHash,
											 String[] userNames,
											 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(LeaderboardGetRanks(userID, productID, leaderboardID, passwordHash, userNames, res));

		return res;
	}

	public AsyncOpResult LeaderboardGetRanksAndScores(string userID,
													  string productID,
													  string leaderboardID,
													  string passwordHash,
													  String[] userNames,
													  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(LeaderboardGetRanksAndScores(userID, productID, leaderboardID, passwordHash, userNames, res));

		return res;
	}

	public AsyncOpResult LeaderboardQuery(string userID,
										  string productID,
										  string leaderboardID,
										  string passwordHash,
										  int startRank,
										  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(LeaderboardQuery(userID, productID, leaderboardID, passwordHash, startRank, res));

		return res;
	}

	public AsyncOpResult LeaderboardQueryAdmin(string productID,
											   string leaderboardID,
											   string passwordHash,
											   int startRank,
											   int count,
											   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(LeaderboardQueryAdmin(productID, leaderboardID, passwordHash, startRank, count, res));

		return res;
	}

	public AsyncOpResultChain WaitForAsyncOpResults(AsyncOpResultChain.AsyncOpResChainDelegate listener, params AsyncOpResult[] list)
	{
		AsyncOpResultChain res = new AsyncOpResultChain(listener);

		foreach (AsyncOpResult curr in list)
		{
			res.m_PendingOps.Add(curr);
		}

		StartCoroutine(WaitForAsyncOpResults(res));

		return res;
	}

	public AsyncOpResult BuyPremiumAccount(string userID,
										   string productID,
										   string accountTypeID,
										   string passwordHash,
										   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(BuyPremiumAccount(userID, productID, accountTypeID, passwordHash, res));

		return res;
	}

	public AsyncOpResult ProcessInAppPurchaseIOS(string userID,
												 string productID,
												 string appStoreTransactionID,
												 string receiptData,
												 string passwordHash,
												 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(ProcessInAppPurchaseIOS(userID, productID, appStoreTransactionID, receiptData, passwordHash, res));

		return res;
	}

	public AsyncOpResult ProcessInAppPurchaseIOSVersion(IAPVersion version,
														string userID,
														string productID,
														string appStoreTransactionID,
														string receiptData,
														string passwordHash,
														AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(ProcessInAppPurchaseIOSVersion(version, userID, productID, appStoreTransactionID, receiptData, passwordHash, res));

		return res;
	}

	public AsyncOpResult ProcessInAppPurchaseGoogle(string userID,
													string productID,
													string receiptData,
													string passwordHash,
													AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(ProcessInAppPurchaseGoogle(userID, productID, receiptData, passwordHash, res));

		return res;
	}

	public AsyncOpResult ProcessInAppPurchaseGoogleVersion(IAPGoogleVersion version,
														   string userID,
														   string productID,
														   string receiptData,
														   string passwordHash,
														   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(ProcessInAppPurchaseGoogleVersion(version, userID, productID, receiptData, passwordHash, res));

		return res;
	}

	public AsyncOpResult ProcessInAppPurchaseFacebook(string productId,
													  string userID,
													  string requestId,
													  string fbUser,
													  string passwordHash,
													  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_PROCESS_IAP_FACEBOOK,
			userid = userID,
			pw = passwordHash,
			prodId = productId,
			param = requestId,
			param2 = fbUser
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	public AsyncOpResult InAppPurchaseFacebookPaymentInfo(string productId,
														  string userID,
														  string passwordHash,
														  string fbUserId,
														  string requestId,
														  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_IAP_FACEBOOK_PAYMENT_INFO,
			prodId = productId,
			userid = userID,
			pw = passwordHash,
			param = fbUserId,
			param2 = requestId
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult("", listener));
	}

	public AsyncOpResult SlotMachineSpin(string userID, string productID, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(SlotMachineSpin(userID, productID, passwordHash, res));

		return res;
	}

	public AsyncOpResult GetDailyRewards(string userID, string productID, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatGetDailyRewards(userID, productID, passwordHash), new AsyncOpResult(passwordHash, listener));
	}

	public static object FormatGetDailyRewards(string userId, string productID, string password)
	{
		int utcOffset = 0;
		try
		{
			TimeSpan ts = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Today);
			utcOffset = ts.Hours;
		}
		catch (Exception)
		{
			utcOffset = 0;
		}

		return new
		{
			cmd = CMD_ID_GET_DAILY_REWARDS,
			userid = userId,
			prodId = productID,
			pw = password,
			utcOffset = utcOffset.ToString() // Note: we can use it without worries - client is sending just UTC, not time ( player cannot cheat )
		};
	}

	public AsyncOpResult GetCloudDateTime(string userID, string passwordHash, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_GET_CLOUD_DATE_TIME,
			userid = userID,
			pw = passwordHash
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	public AsyncOpResult GetSlotMachineJackpot(string productID, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatGetSlotMachineJackpot(productID), new AsyncOpResult("", listener));
	}

	public static object FormatGetSlotMachineJackpot(string productID)
	{
		return new
		{
			cmd = CMD_ID_GET_SLOTMACHINE_JACKPOT,
			prodId = productID
		};
	}

	public AsyncOpResult QueryUsersByField(string fieldID,
										   string desiredValue,
										   int maxResults,
										   bool useSubstringAlso,
										   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult("", listener);

		StartCoroutine(QueryUsersByField(fieldID, desiredValue, maxResults, useSubstringAlso, res));

		return res;
	}

	public AsyncOpResult GetConfig(string productID, string kind, string version, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult("", listener);

		StartCoroutine(GetConfig(productID, kind, version, res));

		return res;
	}

	public static object FormatGetConfig(string productID, string kind, string version)
	{
		return new
		{
			cmd = CMD_ID_GET_CONFIG,
			prodId = productID,
			param = version,
			param2 = kind
		};
	}

	public AsyncOpResult SetConfig(string productID,
								   string kind,
								   string version,
								   string val,
								   string passwordHash,
								   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(SetConfig(productID, kind, version, val, passwordHash, res));

		return res;
	}

	public AsyncOpResult AddConfig(string productID,
								   string kind,
								   string version,
								   string passwordHash,
								   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		AsyncOpResult res = new AsyncOpResult(passwordHash, listener);

		StartCoroutine(AddConfig(productID, kind, version, passwordHash, res));

		return res;
	}

	public AsyncOpResult GetAppLicenseOuya(string productId, string licenseRequest, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_APP_LICENSING_OUYA,
			prodId = productId,
			data = Convert.ToBase64String(Encoding.UTF8.GetBytes(licenseRequest))
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult("", listener));
	}

	public AsyncOpResult GetLeaderboardUsersCount(string productId,
												  string leaderboardId,
												  string userId,
												  string password,
												  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_GET_LEADERBOARD_USERS_COUNT,
			userid = userId,
			pw = password,
			prodId = productId,
			param = leaderboardId
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult(password, listener));
	}

	public AsyncOpResult LinkIDWithUser(string userId,
										string passwordHash,
										string idType,
										string id,
										bool rewriteCurrent,
										AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessWebRequestObject(FormatLinkIDWithUser(userId, passwordHash, idType, id, rewriteCurrent),
									   new AsyncOpResult(passwordHash, listener));
	}

	public static object FormatLinkIDWithUser(string userId, string passwordHash, string idType, string id, bool rewriteCurrent)
	{
		if (string.IsNullOrEmpty(id) == true)
			return null;

		return new
		{
			cmd = CMD_ID_LINK_ID_WITH_USER,
			userid = userId,
			pw = passwordHash,
			param = id,
			param2 = idType,
			data = rewriteCurrent.ToString()
		};
	}

	// desiredFieldsJson - json list of pairs key/value
	public AsyncOpResult SetUserDataList(string userId,
										 string passwordHash,
										 string prodId,
										 string desiredFieldsJson,
										 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessWebRequestObject(FormatSetUserDataList(userId, passwordHash, prodId, desiredFieldsJson),
									   new AsyncOpResult(passwordHash, listener));
	}

	public static object FormatSetUserDataList(string userId, string passwordHash, string prodId, string desiredFieldsJson)
	{
		return new
		{
			cmd = "setUsrDataList",
			userid = userId,
			pw = passwordHash,
			prodId = prodId,
			param = desiredFieldsJson,
		};
	}

	public AsyncOpResult RegisterForPushNotifications(string userID,
													  string productID,
													  string provider,
													  string registrationID,
													  bool register,
													  string passwordHash,
													  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_REGISTER_FOR_PUSH_NOTIFICATIONS,
			userid = userID,
			prodId = productID,
			param = provider,
			param2 = registrationID,
			data = register ? "register" : "unregister",
			pw = passwordHash
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(passwordHash, listener));
	}

	public AsyncOpResult GetEntityFieldAdmin(string kind,
											 string name,
											 string field,
											 string password,
											 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = CMD_ID_GET_ENTITY_FIELD,
			pw = password,
			param = kind,
			param2 = name,
			data = field
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult(password, listener));
	}

	//
	// CLAN SUPPORT 
	//

	public AsyncOpResult CLAN_Create(string product, string adminPwdHash, string clanName, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "clanCreate",
			prodId = product,
			pw = adminPwdHash,
			param = clanName
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(adminPwdHash, listener));
	}

	public AsyncOpResult CLAN_Remove(string product, string adminPwdHash, string clanName, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "clanRemove",
			prodId = product,
			pw = adminPwdHash,
			param = clanName
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(adminPwdHash, listener));
	}

	public AsyncOpResult CLAN_AdminAdd(string product,
									   string adminPwdHash,
									   string clanName,
									   string clanAdminName,
									   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "clanAdminAdd",
			prodId = product,
			pw = adminPwdHash,
			param = clanName,
			param2 = clanAdminName
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(adminPwdHash, listener));
	}

	public AsyncOpResult CLAN_AdminRemove(string product,
										  string adminPwdHash,
										  string clanName,
										  string clanAdminName,
										  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "clanAdminRemove",
			prodId = product,
			pw = adminPwdHash,
			param = clanName,
			param2 = clanAdminName
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(adminPwdHash, listener));
	}

	public AsyncOpResult CLAN_AdminLogin(string product,
										 string adminPwdHash,
										 string clanName,
										 string clanAdminName,
										 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "clanAdminLogin",
			prodId = product,
			userid = clanAdminName,
			pw = adminPwdHash,
			param = clanName,
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(adminPwdHash, listener));
	}

	public AsyncOpResult CLAN_MembersAdd(string product,
										 string clanAdminName,
										 string clanAdminPwdHash,
										 string clanName,
										 string clanMembersJSON,
										 AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "clanMembersAdd",
			prodId = product,
			userid = clanAdminName,
			pw = clanAdminPwdHash,
			param = clanName,
			param2 = clanMembersJSON
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(clanAdminPwdHash, listener));
	}

	public AsyncOpResult CLAN_MemberRemove(string product,
										   string clanAdminName,
										   string clanAdminPwdHash,
										   string clanName,
										   string clanMemberName,
										   AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "clanMemberRemove",
			prodId = product,
			userid = clanAdminName,
			pw = clanAdminPwdHash,
			param = clanName,
			param2 = clanMemberName
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult(clanAdminPwdHash, listener));
	}

	public AsyncOpResult CLAN_GetStats(string product, string clanName, int page, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "clanGetStats",
			prodId = product,
			param = clanName,
			param2 = page,
		};

		return ProcessGetWebRequestObject(httpData, new AsyncOpResult("", listener));
	}

	public AsyncOpResult GuestMigrateSGDZ(string guestName,
										  int money,
										  string newEmail,
										  string newPasswordHash,
										  AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "guestMigrateSGDZ",
			userid = guestName,
			pw = newPasswordHash,
			param2 = money.ToString(),
			email = newEmail
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult("", listener));
	}

	object FormatUserGetPrimaryKey(string userId)
	{
		return new
		{
			cmd = "userGetPrimaryKey",
			userid = userId
		};
	}

	// request returns username given by this request or entity key for user
	// ( this will be used as 'username' parameter for all other requests )
	// passwordHash can be empty string for guest accounts
	public AsyncOpResult UserGetPrimaryKey(string userId, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		return ProcessGetWebRequestObject(FormatUserGetPrimaryKey(userId), new AsyncOpResult("", listener));
	}

	IEnumerator UserGetPrimaryKey(string userId, string password, AsyncOpResult result)
	{
		ParamsFormatter httpData = new ParamsFormatter("userGetPrimaryKey");

		httpData.AddField(PARAM_ID_USERID, userId);
		httpData.AddField(PARAM_ID_PASSWORD, password);

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	// ------------------------------------------------------------------------
	// userId is primaryKey ( normal username or primary key for already migrated user )
	public AsyncOpResult UserRequestMigrate(string userId, string passwordHash, string email, AsyncOpResult.AsyncOpResDelegate listener = null)
	{
		object httpData = new
		{
			cmd = "userRequestMigrate",
			userid = userId,
			pw = passwordHash,
			param = email
		};

		return ProcessWebRequestObject(httpData, new AsyncOpResult("", listener));
	}

	IEnumerator WaitForAsyncOpResults(AsyncOpResultChain chain)
	{
		bool done = false;

		while (!done)
		{
			bool allFinished = true;

			foreach (AsyncOpResult curr in chain.m_PendingOps)
			{
				allFinished &= curr.m_Finished;

				if (!allFinished)
				{
					break;
				}
			}

			if (allFinished)
			{
				done = true;
			}
			else
			{
				yield return new WaitForSeconds(ASYNC_OP_CHAIN_WAIT_TIMEOUT_SEC);
			}
		}

		chain.Finished();

		yield return 0;
	}

	IEnumerator CreateLeaderboard(string productID,
								  string leaderboardID,
								  string password,
								  AsyncOpResult result,
								  int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_CREATE_LEADERBOARD);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_PARAM, leaderboardID);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "CreateLeaderboard"));
	}

	IEnumerator LeaderboardSetScores(string productID,
									 string leaderboardID,
									 string password,
									 S_LeaderBoardScoreInfo[] scores,
									 AsyncOpResult result,
									 int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_LEADERBOARD_SET_SCORES);

		string scoresAsJSON = JsonMapper.ToJson(scores);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_PARAM, leaderboardID);
		httpData.AddField(PARAM_ID_PARAM2, scoresAsJSON);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "LeaderboardSetScores"));
	}

	IEnumerator LeaderboardGetRanks(string userID,
									string productID,
									string leaderboardID,
									string password,
									string[] userNames,
									AsyncOpResult result,
									int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_LEADERBOARD_GET_RANKS);

		string userNamesAsJSON = JsonMapper.ToJson(userNames);

		httpData.AddField(PARAM_ID_USERID, userID);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_PARAM, leaderboardID);
		httpData.AddField(PARAM_ID_PARAM2, userNamesAsJSON);

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	IEnumerator LeaderboardGetRanksAndScores(string userID,
											 string productID,
											 string leaderboardID,
											 string password,
											 string[] userNames,
											 AsyncOpResult result,
											 int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_LEADERBOARD_GET_RANKS_AND_SCORES);

		string userNamesAsJSON = JsonMapper.ToJson(userNames);

		httpData.AddField(PARAM_ID_USERID, userID);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_PARAM, leaderboardID);
		httpData.AddField(PARAM_ID_PARAM2, userNamesAsJSON);

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	IEnumerator LeaderboardQuery(string userID,
								 string productID,
								 string leaderboardID,
								 string password,
								 int startRank,
								 AsyncOpResult result,
								 int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_LEADERBOARD_QUERY);

		httpData.AddField(PARAM_ID_USERID, userID);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_PARAM, leaderboardID);
		httpData.AddField(PARAM_ID_PARAM2, startRank.ToString());

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	IEnumerator LeaderboardQueryAdmin(string productID,
									  string leaderboardID,
									  string password,
									  int startRank,
									  int count,
									  AsyncOpResult result,
									  int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_LEADERBOARD_QUERY);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_PARAM, leaderboardID);
		httpData.AddField(PARAM_ID_PARAM2, startRank.ToString());
		httpData.AddField(PARAM_ID_DATA, count.ToString());

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	IEnumerator CreateProduct(string productID, string password, AsyncOpResult result, int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_CREATE_PRODUCT);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PASSWORD, password);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "CreateProduct"));
	}

	IEnumerator ProductSetParam(string productID,
								string paramId,
								string paramVal,
								string password,
								AsyncOpResult result,
								int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_SET_PRODUCT_DATA);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PARAM, paramId);
		httpData.AddField(PARAM_ID_DATA, paramVal);
		httpData.AddField(PARAM_ID_PASSWORD, password);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "ProductSetParam"));
	}

	IEnumerator SetUserData(string userId,
							string fieldId,
							string data,
							string password,
							AsyncOpResult result,
							int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_SET_USER_DATA);

		httpData.AddField(PARAM_ID_DATA, data);
		httpData.AddField(PARAM_ID_PARAM, fieldId);
		httpData.AddField(PARAM_ID_USERID, userId);
		httpData.AddField(PARAM_ID_PASSWORD, password);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "SetUserData"));
	}

	IEnumerator CreateUser(string userId,
						   string productId,
						   string password,
						   bool usePrimaryKey,
						   AsyncOpResult result,
						   int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		AsyncOpResult tmpResult = UserExists(userId, productId, password);

		tmpResult.m_DbgId = "UserExists";

		while (!tmpResult.m_Finished)
		{
			yield return new WaitForSeconds(0.2f);
		}

		if (tmpResult.m_Res && tmpResult.m_ResultDesc == RESP_NOT_FOUND)
		{
			tmpResult.m_DbgId = "CreateUser";

			yield return StartCoroutine(CreateUser(userId, password, tmpResult, usePrimaryKey));

			if (tmpResult.m_Res && (tmpResult.m_ResultDesc == RESP_OK || tmpResult.m_ResultDesc == RESP_ALREADY_PROCESSED))
			{
				if (false == usePrimaryKey)
				{
					tmpResult.m_DbgId = "AddUserProductData";

					yield return StartCoroutine(UserAddProductData(userId, productId, password, tmpResult));
				}
				else
				{
					tmpResult.m_DbgId = "userGetPrimaryKey";

					yield return StartCoroutine(UserGetPrimaryKey(userId, password, tmpResult));

					if (tmpResult.m_Res)
					{
						string primaryKey = null;

						try
						{
							JsonData data = JsonMapper.ToObject(tmpResult.m_ResultDesc);

							if (200 == (int)data["code"])
							{
								primaryKey = (string)data["data"]["primaryKey"];
							}
						}
						catch (Exception e)
						{
							Debug.LogError("unexpected result format : " + tmpResult.m_ResultDesc + ", exception : " + e.ToString());
							primaryKey = null;
						}

						if (null != primaryKey)
						{
							tmpResult.m_DbgId = "AddUserProductData";
							yield return StartCoroutine(UserAddProductData(primaryKey, productId, password, tmpResult));
						}
					}
				}
			}
		}
		else if (tmpResult.m_Res && tmpResult.m_ResultDesc == RESP_OK)
		{
			tmpResult.m_Res = false;
		}

		result.m_Res = tmpResult.m_Res;
		result.m_ResultDesc = tmpResult.m_ResultDesc;
		result.m_DbgId = tmpResult.m_DbgId;
		result.m_Finished = tmpResult.m_Finished;

		result.Finished();
	}

	IEnumerator CreateUser(string userId, string password, AsyncOpResult result, bool usePrimaryKey, int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_CREATE_USER);
		System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
		long secretCode = CloudSecretCode.Compute(enc.GetBytes(password));

		httpData.AddField(PARAM_ID_USERID, userId);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_PARAM, secretCode.ToString());
		httpData.AddField(PARAM_ID_PARAM2, usePrimaryKey.ToString());

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "CreateUser"));
	}

	IEnumerator UserAddProductData(string userId,
								   string productId,
								   string password,
								   AsyncOpResult result,
								   int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_ADD_USR_PRODUCT_DATA);

		httpData.AddField(PARAM_ID_USERID, userId);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_PASSWORD, password);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "UserAddProductData"));
	}

	IEnumerator UserSetProductSpecificData(string userId,
										   string productId,
										   string key,
										   string val,
										   string password,
										   AsyncOpResult result,
										   int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_USER_SET_PER_PRODUCT_DATA);

		httpData.AddField(PARAM_ID_USERID, userId);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_PARAM, key);
		httpData.AddField(PARAM_ID_DATA, val);
		httpData.AddField(PARAM_ID_PASSWORD, password);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "UserSetProductSpecificData"));
	}

	IEnumerator UserSetPerProductDataSection(string userId,
											 string productId,
											 string key,
											 string sectionId,
											 string data,
											 string password,
											 AsyncOpResult result,
											 int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_SET_DATA_SECTION);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_PARAM, key);
		httpData.AddField(PARAM_ID_PARAM2, sectionId);
		httpData.AddField(PARAM_ID_DATA, data);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "UserSetPerProductDataSection"));
	}

	IEnumerator UserUpdatePerProductDataSection(string userId,
												string productId,
												string key,
												string sectionId,
												string data,
												string password,
												AsyncOpResult result,
												int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_UPDATE_DATA_SECTION);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_PARAM, key);
		httpData.AddField(PARAM_ID_PARAM2, sectionId);
		httpData.AddField(PARAM_ID_DATA, data);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "UserUpdatePerProductDataSection"));
	}

	IEnumerator BuyItem(string userId,
						string productId,
						int itemID,
						string password,
						AsyncOpResult result,
						int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_BUY_BUILTIN_ITEM);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_DATA, itemID.ToString());
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "BuyItem"));
	}

	IEnumerator RefundItem(string userId,
						   string productId,
						   int[] itemsIDs,
						   string password,
						   AsyncOpResult result,
						   int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_REFUND_BUILTIN_ITEM);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_DATA, JsonMapper.ToJson(itemsIDs));
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "RefundItem"));
	}

	IEnumerator EquipItem(string userId,
						  string productId,
						  int itemID,
						  int slotIdx,
						  string password,
						  AsyncOpResult result,
						  int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_EQUIP_ITEM);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_DATA, itemID.ToString());
		httpData.AddField(PARAM_ID_PARAM2, slotIdx.ToString());
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "EquipItem"));
	}

	IEnumerator UnEquipItem(string userId,
							string productId,
							int itemID,
							int slotIdx,
							string password,
							AsyncOpResult result,
							int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_UNEQUIP_ITEM);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_DATA, itemID.ToString());
		httpData.AddField(PARAM_ID_PARAM2, slotIdx.ToString());
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "UnEquipItem"));
	}

	IEnumerator ModifyItem(string userId,
						   string productId,
						   int itemID,
						   string key,
						   string val,
						   string password,
						   AsyncOpResult result,
						   int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_MODIFY_ITEM);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_PARAM, key);
		httpData.AddField(PARAM_ID_PARAM2, itemID.ToString());
		httpData.AddField(PARAM_ID_DATA, val);
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "ModifyItem"));
	}

	IEnumerator QueryFriendsInfo(string userId,
								 string productId,
								 string friendsListJSON,
								 string passwordHash,
								 AsyncOpResult result,
								 int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_QUERY_FRIENDS_INFO);

		httpData.AddField(PARAM_ID_USERID, userId);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_PARAM, friendsListJSON);
		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	IEnumerator RequestAddFriend(string userId,
								 string friendUserId,
								 string infoMessage,
								 string passwordHash,
								 AsyncOpResult result,
								 int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_REQUEST_ADD_FRIEND);

		httpData.AddField(PARAM_ID_PARAM, friendUserId);
		httpData.AddField(PARAM_ID_PARAM2, infoMessage);
		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "RequestAddFriend"));
	}

	IEnumerator RequestDelFriend(string userId,
								 string friendUserId,
								 string passwordHash,
								 AsyncOpResult result,
								 int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_REQUEST_DEL_FRIEND);

		httpData.AddField(PARAM_ID_PARAM, friendUserId);
		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "RequestDelFriend"));
	}

	IEnumerator ModifyItems(string userId,
							string productId,
							S_ModItemInfo[] items,
							string password,
							AsyncOpResult result,
							int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_MODIFY_ITEM);
		StringBuilder sb = new StringBuilder();
		JsonWriter writer = new JsonWriter(sb);

		writer.WriteArrayStart();

		foreach (S_ModItemInfo curr in items)
		{
			writer.WriteObjectStart();

			writer.WritePropertyName("id");
			writer.Write(curr.m_GUID.ToString());

			writer.WritePropertyName("key");
			writer.Write(curr.m_Key);

			writer.WritePropertyName("val");
			writer.Write(curr.m_Val);

			writer.WriteObjectEnd();
		}

		writer.WriteArrayEnd();

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_DATA, sb.ToString());
		httpData.AddField(PARAM_ID_PASSWORD, password);
		httpData.AddField(PARAM_ID_USERID, userId);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "ModifyItems"));
	}

	IEnumerator InboxAddMsg(string userId,
							string targetUserId,
							string productId,
							string msg,
							string passwordHash,
							AsyncOpResult result,
							int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_INBOX_ADD_MSG);

		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);
		httpData.AddField(PARAM_ID_PARAM2, msg);

		if (string.IsNullOrEmpty(userId) == false)
		{
			httpData.AddField(PARAM_ID_USERID, userId);
		}

		if (string.IsNullOrEmpty(targetUserId) == false)
		{
			httpData.AddField(PARAM_ID_PARAM, targetUserId);
		}

		if (string.IsNullOrEmpty(productId) == false)
		{
			httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		}

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "InboxAddMsg"));
	}

	IEnumerator FetchInboxMessages(int startMsgIdx,
								   string userId,
								   string productId,
								   string mailboxName,
								   string passwordHash,
								   AsyncOpResult result,
								   int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_PICKUP_INBOX_MESSAGES);

		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);
		httpData.AddField(PARAM_ID_PARAM, startMsgIdx.ToString());

		if (string.IsNullOrEmpty(userId) == false)
		{
			httpData.AddField(PARAM_ID_USERID, userId);
		}

		if (string.IsNullOrEmpty(productId) == false)
		{
			httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		}

		if (!string.IsNullOrEmpty(mailboxName))
		{
			httpData.AddField(PARAM_ID_DATA, mailboxName);
		}

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	IEnumerator InboxRemoveMessages(string userId,
									string productId,
									int lastMessageIdx,
									string passwordHash,
									AsyncOpResult result,
									int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_INBOX_REMOVE_MESSAGES);

		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);
		httpData.AddField(PARAM_ID_PARAM, lastMessageIdx.ToString());

		if (string.IsNullOrEmpty(userId) == false)
		{
			httpData.AddField(PARAM_ID_USERID, userId);
		}

		if (string.IsNullOrEmpty(productId) == false)
		{
			httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		}

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "InboxRemoveMessages"));
	}

	IEnumerator RequestResetPassword(string userId,
									 string message,
									 string subjectMsg,
									 AsyncOpResult result,
									 int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_REQUEST_RESET_PASSWORD);
		string usrIdSHA = Convert.ToBase64String(CalcSHA1Hash(userId));
		long secretCode = CloudSecretCode.Compute(Encoding.UTF8.GetBytes(usrIdSHA));

		httpData.AddField(PARAM_ID_USERID, userId);
		httpData.AddField(PARAM_ID_PARAM, secretCode.ToString());
		httpData.AddField(PARAM_ID_PARAM2, message);
		httpData.AddField(PARAM_ID_DATA, subjectMsg);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "RequestResetPassword"));
	}

	IEnumerator RequestResetPasswordWithEmail(string userId,
											  string userEmail,
											  string message,
											  string subjectMsg,
											  AsyncOpResult result,
											  int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_REQUEST_RESET_PASSWORD_WITH_EMAIL);
		string usrIdSHA = Convert.ToBase64String(CalcSHA1Hash(userId));
		long secretCode = CloudSecretCode.Compute(Encoding.UTF8.GetBytes(usrIdSHA));

		httpData.AddField(PARAM_ID_USERID, userId);
		httpData.AddField(PARAM_ID_PARAM, secretCode.ToString());
		httpData.AddField(PARAM_ID_PARAM2, message);
		httpData.AddField(PARAM_ID_DATA, subjectMsg);
		httpData.AddField(PARAM_ID_EMAIL, userEmail);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "RequestResetPasswordWithEmail"));
	}

	IEnumerator VerifyStoreKitReceipt(string receiptData, AsyncOpResult result, int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_VALIDATE_IOS_RECEIPT);
		string dataHash = Convert.ToBase64String(CalcSHA1Hash(receiptData));
		long secretCode = CloudSecretCode.Compute(Encoding.UTF8.GetBytes(dataHash));

		httpData.AddField(PARAM_ID_PARAM, receiptData);
		httpData.AddField(PARAM_ID_PARAM2, secretCode.ToString());

		if (m_EnableStoreKitSandbox)
		{
			httpData.AddField(PARAM_ID_DATA, "useSandbox");
		}

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "VerifyStoreKitReceipt"));
	}

	ParamsFormatter PrepareHttpDataForIAPIOS(string cmdName,
											 string userID,
											 string productID,
											 string appStoreTransactionID,
											 string receiptData,
											 string passwordHash)
	{
		ParamsFormatter httpData = new ParamsFormatter(cmdName);
		string dataHash = Convert.ToBase64String(CalcSHA1Hash(receiptData));
		long secretCode = CloudSecretCode.Compute(Encoding.UTF8.GetBytes(dataHash));

		httpData.AddField(PARAM_ID_PARAM, receiptData);
		httpData.AddField(PARAM_ID_PARAM2, secretCode.ToString());
		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);
		httpData.AddField(PARAM_ID_USERID, userID);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_APP_STORE_TRANSACTION_ID, appStoreTransactionID);

		if (m_EnableStoreKitSandbox)
		{
			httpData.AddField(PARAM_ID_DATA, "useSandbox");
		}

		return httpData;
	}

	IEnumerator ProcessInAppPurchaseIOS(string userID,
										string productID,
										string appStoreTransactionID,
										string receiptData,
										string passwordHash,
										AsyncOpResult res,
										int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = PrepareHttpDataForIAPIOS(CMD_ID_PROCESS_IAP_IOS,
															userID,
															productID,
															appStoreTransactionID,
															receiptData,
															passwordHash);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), res, numRetries, "ProcessInAppPurchaseIOS"));
	}

	// new internal version ( JSON results )
	IEnumerator ProcessInAppPurchaseIOSVersion(IAPVersion version,
											   string userID,
											   string productID,
											   string appStoreTransactionID,
											   string receiptData,
											   string passwordHash,
											   AsyncOpResult res,
											   int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = PrepareHttpDataForIAPIOS(CMD_ID_PROCESS_IAP_IOS_VERSION,
															userID,
															productID,
															appStoreTransactionID,
															receiptData,
															passwordHash);

		httpData.AddField(PARAM_ID_VERSION, version.ToString());

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), res, numRetries, "ProcessInAppPurchaseIOSVersion"));
	}

	ParamsFormatter PrepareHttpDataForIAPGoogle(string cmdName, string userID, string productID, string receiptData, string passwordHash)
	{
		ParamsFormatter httpData = new ParamsFormatter(cmdName);

		string dataHash = Convert.ToBase64String(CalcSHA1Hash(receiptData));
		long secretCode = CloudSecretCode.Compute(Encoding.UTF8.GetBytes(dataHash));

		httpData.AddField(PARAM_ID_PARAM, receiptData);
		httpData.AddField(PARAM_ID_PARAM2, secretCode.ToString());
		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);
		httpData.AddField(PARAM_ID_USERID, userID);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);

		return httpData;
	}

	IEnumerator ProcessInAppPurchaseGoogle(string userID,
										   string productID,
										   string receiptData,
										   string passwordHash,
										   AsyncOpResult res,
										   int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = PrepareHttpDataForIAPGoogle(CMD_ID_PROCESS_IAP_GOOGLE, userID, productID, receiptData, passwordHash);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), res, numRetries, "ProcessInAppPurchaseGoogle"));
	}

	IEnumerator ProcessInAppPurchaseGoogleVersion(IAPGoogleVersion version,
												  string userID,
												  string productID,
												  string receiptData,
												  string passwordHash,
												  AsyncOpResult res,
												  int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = PrepareHttpDataForIAPGoogle(CMD_ID_PROCESS_IAP_GOOGLE_VERSION, userID, productID, receiptData, passwordHash);

		httpData.AddField(PARAM_ID_VERSION, version.ToString());

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), res, numRetries, "ProcessInAppPurchaseGoogleVersion"));
	}

	IEnumerator BuyPremiumAccount(string userID,
								  string productID,
								  string accountTypeID,
								  string passwordHash,
								  AsyncOpResult result,
								  int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_BUY_PREMIUM_ACCOUNT);

		httpData.AddField(PARAM_ID_USERID, userID);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PARAM, accountTypeID);
		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "BuyPremiumAccount"));
	}

	IEnumerator SlotMachineSpin(string userID,
								string productID,
								string passwordHash,
								AsyncOpResult result,
								int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_SLOTMACHINE_SPIN);

		httpData.AddField(PARAM_ID_USERID, userID);
		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PASSWORD, passwordHash);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "SlotMachineSpin"));
	}

	// try to keep maxResults at low values
	IEnumerator QueryUsersByField(string fieldID, string desiredValue, int maxResults, bool useSubstringAlso, AsyncOpResult result)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_QUERY_USERS_BY_FIELD);

		httpData.AddField(PARAM_ID_PARAM, fieldID);
		httpData.AddField(PARAM_ID_PARAM2, desiredValue);
		httpData.AddField(PARAM_ID_VAL, maxResults.ToString());
		httpData.AddField(PARAM_ID_DATA, useSubstringAlso.ToString());

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	IEnumerator GetConfig(string productID, string kind, string version, AsyncOpResult result)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_GET_CONFIG);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productID);
		httpData.AddField(PARAM_ID_PARAM, version);
		httpData.AddField(PARAM_ID_PARAM2, kind);

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	IEnumerator SetConfig(string productId,
						  string kind,
						  string version,
						  string val,
						  string password,
						  AsyncOpResult result,
						  int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_SET_CONFIG);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_PARAM, version);
		httpData.AddField(PARAM_ID_PARAM2, kind);
		httpData.AddField(PARAM_ID_DATA, val);
		httpData.AddField(PARAM_ID_PASSWORD, password);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "SetConfig"));
	}

	IEnumerator AddConfig(string productId,
						  string kind,
						  string version,
						  string password,
						  AsyncOpResult result,
						  int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_ADD_CONFIG);

		httpData.AddField(PARAM_ID_PRODUCT_ID, productId);
		httpData.AddField(PARAM_ID_PARAM, version);
		httpData.AddField(PARAM_ID_PARAM2, kind);
		httpData.AddField(PARAM_ID_PASSWORD, password);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "AddConfig"));
	}

	IEnumerator GetUserLinkedWithID(string id, string idType, string accountType, AsyncOpResult result)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_GET_USER_LINKED_WITH_ID);

		httpData.AddField(PARAM_ID_DATA, id);
		httpData.AddField(PARAM_ID_PARAM, idType);
		httpData.AddField(PARAM_ID_PARAM2, accountType);

		yield return StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));
	}

	IEnumerator BatchCommand(string stringJSON, AsyncOpResult result, int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		ParamsFormatter httpData = new ParamsFormatter(CMD_ID_BATCH_COMMAND);

		httpData.AddField(PARAM_ID_PARAM, stringJSON);

		yield return StartCoroutine(ProcessWebRequest(CreateWWWForm(httpData.ToString()), result, numRetries, "BatchCommand"));
	}

	IEnumerator ProcessResponseCmd(string cmdData, AsyncOpResult result, int numRetries = NUM_DB_UPDATE_RETRIES)
	{
		WWWForm form = CreateWWWForm(cmdData, true);

		form.AddField("ipw", "");

		yield return StartCoroutine(ProcessWebRequest(form, result, numRetries));
	}

	// TODO : remove
	string BuildURLFromParams(ParamsFormatter httpData)
	{
		string result = GetURLCurrentGame();

		if (null != httpData.cmdName)
		{
			result += "/" + httpData.cmdName;
		}

		return result + "?" + PARAM_ID_PARAM + "=" + EncodeURLParam(httpData.ToString());
	}

	string BuildURLFromParams(object httpData)
	{
		string result = GetURLCurrentGame();

		/*
		if( httpData.GetType().IsArray() )
		{
			// array params support
			// convert from array into object with properties
		}
		*/

		System.Reflection.PropertyInfo cmdProp = httpData.GetType().GetProperty(PARAM_ID_CMD);

		if (null != cmdProp)
		{
			result += "/" + (string)(cmdProp.GetValue(httpData, null));
		}

		return result + "?" + PARAM_ID_PARAM + "=" + EncodeURLParam(JsonMapper.ToJson(httpData));
	}

	AsyncOpResult ProcessGetWebRequestObject(object httpData, AsyncOpResult result)
	{
		StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData), result));

		return result;
	}

	/*AsyncOpResult ProcessGetWebRequestParams(ParamsFormatter httpData, string cmd, AsyncOpResult result)
	{
		StartCoroutine(ProcessGetWebRequest(BuildURLFromParams(httpData, cmd), result));
		
		return result;
	}*/

	IEnumerator ProcessGetWebRequest(string url, AsyncOpResult result)
	{
//		Debug.Log("WWW get: " + url);

		if (UnityEngine.Random.Range(0.0f, 1.0f) < m_DbgIntroducedFailureRate)
		{
			yield return new WaitForSeconds(UnityEngine.Random.Range(0.25f, 1.0f));

			result.m_ResultDesc = "dbgfailure";
			result.m_Res = false;
			result.Finished();

			yield return 0;
		}
		else
		{
			WWW httpReq = new WWW(url);

			yield return httpReq;

			result.m_Res = httpReq.error == null;

			if (result.m_Res)
			{
				result.m_ResultDesc = httpReq.text;
			}
			else
			{
				result.m_ResultDesc = httpReq.error;
				Debug.LogWarning(httpReq.error);
			}

			result.Finished();
		}

		yield return 0;
	}

	WWWForm CreateWWWFormObject(object obj)
	{
		return CreateWWWForm(JsonMapper.ToJson(obj));
	}

	WWWForm CreateWWWForm(string data, bool forceDisableEncryption = false)
	{
//		Debug.Log("CreateWWWForm data: " + data);

		WWWForm form = new WWWForm();

		if (m_EnableEncryption && !forceDisableEncryption)
		{
			if (!m_SymmetricEncEncryptor.CanReuseTransform)
			{
				m_SymmetricEncEncryptor = m_SymmetricEnc.CreateEncryptor();
			}

			form.AddField(PARAM_ID_PARAM, EncryptStr(data));
			form.AddField("_pw", m_SymmetricEncPasswordBase64);
			form.AddField("_iv", m_SymmetricEncIVBase64);
		}
		else
		{
			form.AddField(PARAM_ID_PARAM, data);
		}

		return form;
	}

	AsyncOpResult ProcessWebRequestObject(object httpData, AsyncOpResult result)
	{
		string cmdURL = null;

		System.Reflection.PropertyInfo cmdProp = httpData.GetType().GetProperty(PARAM_ID_CMD);

		if (null != cmdProp)
		{
			cmdURL = (string)(cmdProp.GetValue(httpData, null));
		}

		StartCoroutine(ProcessWebRequest(CreateWWWFormObject(httpData), result, NUM_DB_UPDATE_RETRIES, cmdURL));

		return result;
	}

	IEnumerator ProcessWebRequest(WWWForm httpData, AsyncOpResult result, int numRetries, string cmdURL = null)
	{
		float currRetryTimeout = DB_UPDATE_RETRY_WAIT_MS;

		if (UnityEngine.Random.Range(0.0f, 1.0f) < m_DbgIntroducedFailureRate)
		{
			yield return new WaitForSeconds(UnityEngine.Random.Range(0.25f, 1.0f));

			result.m_ResultDesc = "dbgfailure";
			result.m_Res = false;
			result.Finished();

			yield return 0;
		}
		else
		{
			string servletURL = GetURLCurrentGame();

			if (null != cmdURL)
			{
				servletURL += "/" + cmdURL;
			}

			for (int i = 0; i < numRetries; i++)
			{
				WWW httpPutReq = new WWW(servletURL, httpData);

				yield return httpPutReq;

				result.m_Res = httpPutReq.error == null;

				if (result.m_Res)
				{
					result.m_ResultDesc = httpPutReq.text;
				}
				else
				{
					Debug.LogWarning(httpPutReq.error);
					result.m_ResultDesc = httpPutReq.error;
				}

				if (result.m_ResultDesc != RESP_DB_ERROR && result.m_ResultDesc != RESP_GENERAL_ERROR)
				{
					bool finish = true;

					try
					{
						JsonData http = JsonMapper.ToObject(result.m_ResultDesc);

						if (null != http && http.HasValue(RESPONSE_HTTP_RESULT))
						{
							JsonData httpResult = http[RESPONSE_HTTP_RESULT];

							if (null != httpResult && httpResult.HasValue(PROP_ID_DESC))
							{
								string resultDesc = (string)httpResult[PROP_ID_DESC];

								if (resultDesc == RESP_DB_ERROR || resultDesc == RESP_GENERAL_ERROR)
								{
									finish = false;
								}
							}
						}
					}
					catch
					{
					}

					if (finish)
					{
						break;
					}
				}

				yield return new WaitForSeconds(currRetryTimeout/1000);

				currRetryTimeout *= 2;
			}

			result.Finished();
		}

		yield return 0;
	}

	byte[] AsymmetricEncrypt(byte[] input)
	{
		MFDebugUtils.Assert(m_RSA != null);

		return m_RSA.Encrypt(input, false);
	}

	string EncodeURLParam(string str)
	{
		if (m_EnableEncryption)
		{
			if (!m_SymmetricEncEncryptor.CanReuseTransform)
			{
				m_SymmetricEncEncryptor = m_SymmetricEnc.CreateEncryptor();
			}

			return WWW.EscapeURL(EncryptStr(str)) + "&_pw=" + WWW.EscapeURL(m_SymmetricEncPasswordBase64) + "&_iv=" +
				   WWW.EscapeURL(m_SymmetricEncIVBase64);
		}
		else
		{
			return WWW.EscapeURL(str);
		}
	}

	string EncryptStr(string str)
	{
		byte[] data = ASCIIEncoding.ASCII.GetBytes(str);

		return Convert.ToBase64String(m_SymmetricEncEncryptor.TransformFinalBlock(data, 0, data.Length));
	}

	void Update()
	{
		float currRealTime = Time.realtimeSinceStartup;

		if (m_LastPasswordChangeTime < 0)
		{
			m_LastPasswordChangeTime = currRealTime;
		}

		if ((currRealTime - m_LastPasswordChangeTime) >= PASSWORD_UPDATE_PERIOD_SEC)
		{
			RefreshSymmetricEncParams();

			m_LastPasswordChangeTime = currRealTime;
		}
	}

	[System.Serializable]
	public class LobbyConfigWrapper
	{
		[System.Serializable]
		public class IPConfig
		{
			public string Name;
			public string IP;
			public int Port;
		}
		
		public string m_Version = "0.5.8.beta";
		public IPConfig[] m_Config;
	}
}
