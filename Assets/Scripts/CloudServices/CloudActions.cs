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

//#define REPORT_MALFORMED_HTTP_REQUEST_RESULT

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using LitJson;

// =====================================================================================================================
// =====================================================================================================================
public class UnigueUserID
{
	public string primaryKey { get; private set; }
	public string passwordHash { get; private set; }
	public string productID { get; private set; }

	public UnigueUserID(string inPrimaryKey, string inPasswordHash, string inProductID)
	{
		primaryKey = inPrimaryKey.ToLower();
		passwordHash = inPasswordHash;
		productID = inProductID;
	}
}

// =====================================================================================================================
// =====================================================================================================================
public abstract class BaseCloudAction
{
	public enum E_Status
	{
		Pending,
		InProggres,
		Success,
		Failed
	};

	public const float DefaultTimeOut = 30.0f;
	public const float NoTimeOut = -1.0f;

	public bool isDone
	{
		get { return (m_Status == E_Status.Failed || m_Status == E_Status.Success); }
	}

	public bool isFailed
	{
		get { return (m_Status == E_Status.Failed); }
	}

	public bool isSucceeded
	{
		get { return (m_Status == E_Status.Success); }
	}

	public string failInfo { get; protected set; }
	public string result { get; protected set; }
	public E_HttpResultCode resultCode { get; protected set; }

	public float timeOut { get; private set; }
	public float createTime { get; private set; }
	public float activationTime { get; private set; }

	public float lifeTime
	{
		get { return (Time.realtimeSinceStartup - createTime); }
	}

	public float activeTime
	{
		get { return m_Status == E_Status.Pending ? 0 : (Time.realtimeSinceStartup - activationTime); }
	}

	protected UnigueUserID userID { get; private set; }

	protected E_Status status
	{
		get { return m_Status; }
		set { SetStatus(value); }
	}

	E_Status m_Status;

	public BaseCloudAction(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
	{
		userID = inUserID;

		timeOut = inTimeOut;

		m_Status = E_Status.Pending;
		failInfo = string.Empty;
		createTime = Time.realtimeSinceStartup;
	}

	// Don't call this method. This is called from PPIManager.
	internal abstract E_Status PPIManager_Update();

	protected void SetStatus(E_Status inStatus)
	{
		if (inStatus == m_Status)
			return;

		if (inStatus == E_Status.InProggres)
			activationTime = Time.realtimeSinceStartup;

		m_Status = inStatus;
	}
}

// =====================================================================================================================
// =====================================================================================================================
public abstract class DefaultCloudAction : BaseCloudAction
{
	public virtual bool parseResultDescForResultCode
	{
		get { return true; }
	}

	CloudServices.AsyncOpResult m_AsyncOp;

	public DefaultCloudAction(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
	}

	internal override E_Status PPIManager_Update()
	{
		try
		{
			DefaultUpdateMethod();

			OnUpdate();

			switch (status)
			{
			case BaseCloudAction.E_Status.Success:
				OnSuccess();
				break;
			case BaseCloudAction.E_Status.Failed:
				OnFailed();
				break;
			default:
				break;
			}

			return status;
		}
		catch (System.Exception e)
		{
			status = BaseCloudAction.E_Status.Failed;
			throw e;
		}
	}

	protected void DefaultUpdateMethod()
	{
		if (status == BaseCloudAction.E_Status.Pending)
		{
			m_AsyncOp = GetCloudAsyncOp();
			SetStatus(m_AsyncOp != null ? BaseCloudAction.E_Status.InProggres : BaseCloudAction.E_Status.Failed);
		}

		if (status == BaseCloudAction.E_Status.InProggres)
		{
			if (m_AsyncOp.m_Finished == true)
			{
				if (m_AsyncOp.m_Res == false)
				{
					// request failed.
					failInfo = m_AsyncOp.m_ResultDesc;
					resultCode = CloudServices.ParseResultCode(m_AsyncOp.m_ResultDesc, E_HttpResultCode.BadRequest);
					SetStatus(BaseCloudAction.E_Status.Failed);
				}
				else
				{
					string resultDesc = string.Empty;

					try
					{
						if (string.IsNullOrEmpty(m_AsyncOp.m_ResultDesc) == false && m_AsyncOp.m_ResultDesc[0] == '{')
						{
							JsonData httpData = JsonMapper.ToObject(m_AsyncOp.m_ResultDesc);
							JsonData httpResult = httpData.HasValue(CloudServices.RESPONSE_HTTP_RESULT) ? httpData[CloudServices.RESPONSE_HTTP_RESULT] : null;
							if (httpResult != null)
							{
								resultDesc = httpResult.HasValue(CloudServices.PROP_ID_DESC) ? (string)httpResult[CloudServices.PROP_ID_DESC] : resultDesc;
								resultCode = httpResult.HasValue(CloudServices.PROP_ID_CODE)
															 ? (E_HttpResultCode)(int)httpResult[CloudServices.PROP_ID_CODE]
															 : E_HttpResultCode.None;
							}
#if UNITY_EDITOR && REPORT_MALFORMED_HTTP_REQUEST_RESULT
							else
							{
								Debug.LogWarning(string.Format("{0}.DefaultUpdateMethod() :: HTTP request result not found!\n\t{1}", GetType().Name, m_AsyncOp.m_ResultDesc));
							}
#endif
						}
					}
					catch
					{
					}
					finally
					{
						if (resultCode == E_HttpResultCode.None)
						{
							resultCode = parseResultDescForResultCode == false
														 ? E_HttpResultCode.Ok
														 : CloudServices.ParseResultCode(m_AsyncOp.m_ResultDesc, E_HttpResultCode.Ok);
						}
						resultDesc = string.IsNullOrEmpty(resultDesc) == false ? resultDesc : m_AsyncOp.m_ResultDesc;
					}

					if (resultCode < E_HttpResultCode.BadRequest)
					{
						// request succeeded.
						result = m_AsyncOp.m_ResultDesc;
						SetStatus(BaseCloudAction.E_Status.Success);
					}
					else
					{
						// request succeeded
						// but the result contains an error code
						// so the whole request failed for us.
						failInfo = resultDesc;
						SetStatus(BaseCloudAction.E_Status.Failed);
					}
				}
			}
			else if (timeOut > 0 && activeTime > timeOut)
			{
				// request timeout expired. action failed.
				failInfo = "Action timeout expired!";
				resultCode = E_HttpResultCode.RequestTimeout;
				SetStatus(BaseCloudAction.E_Status.Failed);
			}
		}
	}

	protected abstract CloudServices.AsyncOpResult GetCloudAsyncOp();

	protected virtual void OnUpdate()
	{
	}

	protected virtual void OnSuccess()
	{
	}

	protected virtual void OnFailed()
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class CloudActionSerial : BaseCloudAction
{
	List<BaseCloudAction> m_Actions;

	public CloudActionSerial(UnigueUserID inUserID, float inTimeOut = NoTimeOut, params BaseCloudAction[] inActions)
					: base(inUserID, inTimeOut)
	{
		m_Actions = new List<BaseCloudAction>(inActions);
		if (m_Actions == null || m_Actions.Count <= 0)
		{
			SetStatus(BaseCloudAction.E_Status.Success);
		}
	}

	internal override E_Status PPIManager_Update()
	{
		if (status == BaseCloudAction.E_Status.Pending)
		{
			SetStatus(BaseCloudAction.E_Status.InProggres);
		}
		else if (status == BaseCloudAction.E_Status.Failed || status == BaseCloudAction.E_Status.Success)
		{
			return status;
		}

		BaseCloudAction activeAction = m_Actions[0];
		E_Status childActionStatus = activeAction.PPIManager_Update();

		if (childActionStatus == BaseCloudAction.E_Status.Failed)
		{
			m_Actions = null; // remove referencies...
			failInfo = activeAction.failInfo;
			resultCode = activeAction.resultCode;
			SetStatus(BaseCloudAction.E_Status.Failed);
			OnFailed();
		}
		else if (childActionStatus == BaseCloudAction.E_Status.Success)
		{
			m_Actions.Remove(activeAction);
			if (m_Actions == null || m_Actions.Count <= 0)
			{
				resultCode = E_HttpResultCode.Ok;
				SetStatus(BaseCloudAction.E_Status.Success);
				OnSuccess();
			}
		}
		else if (timeOut > 0 && activeTime > timeOut)
		{
			// request timeout expired. action failed.
			failInfo = "Action timeout expired!";
			resultCode = E_HttpResultCode.RequestTimeout;
			SetStatus(BaseCloudAction.E_Status.Failed);

			OnFailed();
		}

		return status;
	}

	protected virtual void OnSuccess()
	{
	}

	protected virtual void OnFailed()
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class UserGetPrimaryKey : DefaultCloudAction
{
	public string username { get; private set; }
	public string primaryKey { get; private set; }

	public UserGetPrimaryKey(string inUserID, float inTimeOut = NoTimeOut)
					: base(null, inTimeOut)
	{
		username = inUserID;
		primaryKey = inUserID;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserGetPrimaryKey(username);
	}

	protected override void OnSuccess()
	{
		try
		{
			JsonData data = JsonMapper.ToObject(result)["data"];
			primaryKey = (string)data["primaryKey"];
		}
		catch
		{
			SetStatus(BaseCloudAction.E_Status.Failed);
			failInfo = "no primaryKey received";
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class UserRequestMigrate : DefaultCloudAction
{
	public string primaryKey { get; private set; }
	public string passwordHash { get; private set; }
	public string email { get; private set; }

	public UserRequestMigrate(string inPrimaryKey, string inPasswordHash, string inEmail, float inTimeOut = NoTimeOut)
					: base(null, inTimeOut)
	{
		primaryKey = inPrimaryKey;
		passwordHash = inPasswordHash;
		email = inEmail;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserRequestMigrate(primaryKey, passwordHash, email);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class TranslateShopProductIds : DefaultCloudAction
{
	public enum Shop
	{
		AppleAppStoreiOS,
		AppleAppStoreOSX,
		GooglePlay,
		LiveGamer,
		MFLive,
		WildTangent,
		Facebook
	}

	public class ProductIdTranslation
	{
		public string IngameItemID;
		public string ShopItemID;
		public string PriceToDisplay;
	}

	public ProductIdTranslation[] TranslatedProductIds { get; private set; }
	Shop m_Shop;

	public TranslateShopProductIds(UnigueUserID inUserID, Shop shop, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		m_Shop = shop;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().GetConfig(userID.productID, CloudServices.CONFIG_SHOP_ID_TABLE, m_Shop.ToString());
	}

	protected override void OnSuccess()
	{
		TranslatedProductIds = JsonMapper.ToObject<ProductIdTranslation[]>(result);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public abstract class ProcessInAppPurchaseAction : DefaultCloudAction
{
	public string transactionId { get; private set; }

	public ProcessInAppPurchaseAction(UnigueUserID inUserID, string inTransactionId, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		transactionId = inTransactionId;
	}

	protected override void OnSuccess()
	{
		string resultDesc = string.Empty;

		try
		{
			if (!string.IsNullOrEmpty(result))
			{
				JsonData httpData = JsonMapper.ToObject(result);

				resultDesc = (string)httpData[CloudServices.RESPONSE_HTTP_RESULT][CloudServices.PROP_ID_DESC];
			}
		}
#if REPORT_MALFORMED_HTTP_REQUEST_RESULT
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
#else
		catch
		{
		}
#endif
		finally
		{
			result = !string.IsNullOrEmpty(resultDesc) ? resultDesc : CloudServices.RESP_GENERAL_ERROR;
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class ProcessInAppPurchaseGooglePlay : ProcessInAppPurchaseAction
{
	string m_PurchaseData;
	string m_PurchaseSignature;

	public ProcessInAppPurchaseGooglePlay(UnigueUserID inUserID,
										  string inTransactionId,
										  string inPurchaseData,
										  string inPurchaseSignature,
										  float inTimeOut = NoTimeOut)
					: base(inUserID, inTransactionId, inTimeOut)
	{
		m_PurchaseData = inPurchaseData;
		m_PurchaseSignature = inPurchaseSignature;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		JsonWriter writer = new JsonWriter(sb);

		writer.WriteObjectStart();
		writer.WritePropertyName("signedData");
		writer.Write(m_PurchaseData);
		writer.WritePropertyName("signature");
		writer.Write(m_PurchaseSignature);
		writer.WriteObjectEnd();

		string receiptData = sb.ToString();

		return CloudServices.GetInstance()
							.ProcessInAppPurchaseGoogleVersion(CloudServices.IAPGoogleVersion.APIV3MFGInternalV2,
															   userID.primaryKey,
															   userID.productID,
															   receiptData,
															   userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class ProcessInAppPurchaseAppleAppStore : ProcessInAppPurchaseAction
{
	string m_Receipt;
	string m_AppStoreTransactionId;

	public ProcessInAppPurchaseAppleAppStore(UnigueUserID inUserID,
											 string inTransactionId,
											 string inAppStoreTransactionId,
											 string inReceipt,
											 float inTimeOut = NoTimeOut)
					: base(inUserID, inTransactionId, inTimeOut)
	{
		m_Receipt = inReceipt;
		m_AppStoreTransactionId = inAppStoreTransactionId;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance()
							.ProcessInAppPurchaseIOSVersion(CloudServices.IAPVersion.InternalV2,
															userID.primaryKey,
															userID.productID,
															m_AppStoreTransactionId,
															m_Receipt,
															userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================

public enum E_CloudProcessInAppPurchaseResult
{
	SuccessAndRemove,
	FailAndRemove,
	FailAndRetry
}

public class ProcessInAppPurchaseFacebook : ProcessInAppPurchaseAction
{
	string m_FBUser;

	public ProcessInAppPurchaseFacebook(UnigueUserID inUserID, string requestId, string fbUser, float inTimeOut = NoTimeOut)
					: base(inUserID, requestId, inTimeOut)
	{
		m_FBUser = fbUser;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		/*
		Debug.Log( "ProcessInAppPurchaseFacebook GetCloudAsyncOp\n" +
			"userID.productID: " + userID.productID + "\n" +
			"userID.userName: " + userID.userName + "\n" +
			"transactionId: " + transactionId + "\n" +
			"m_FBUser: " + m_FBUser + "\n" +
			"userID.passwordHash: " + userID.passwordHash );
			*/

		return CloudServices.GetInstance()
							.ProcessInAppPurchaseFacebook(userID.productID, userID.primaryKey, transactionId, m_FBUser, userID.passwordHash);
	}

	public E_CloudProcessInAppPurchaseResult CloudPurchaseResult
	{
		get
		{
			//Debug.Log( "ProcessInAppPurchaseFacebook -> CloudPurchaseResult result: " + result );

			if (result.Equals(CloudServices.RESP_OK))
				return E_CloudProcessInAppPurchaseResult.SuccessAndRemove;

			else if (result.Equals(CloudServices.RESP_OPERATION_REFUSED))
				return E_CloudProcessInAppPurchaseResult.FailAndRemove;

			else
				return E_CloudProcessInAppPurchaseResult.FailAndRetry;
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class InAppFacebookGetPaymentInfo : DefaultCloudAction
{
	string m_FbUser;
	string m_RequestId;

	public string FBPaymentInfoJSON { get; private set; }

	public InAppFacebookGetPaymentInfo(UnigueUserID inUserID, string fbUser, string requestId, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		m_FbUser = fbUser;
		m_RequestId = requestId;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance()
							.InAppPurchaseFacebookPaymentInfo(userID.productID, userID.primaryKey, userID.passwordHash, m_FbUser, m_RequestId);
	}

	protected override void OnSuccess()
	{
		JsonData jsonData = JsonMapper.ToObject(result);
		string paymentInfo = jsonData.HasValue("paymentInfo") ? (string)jsonData["paymentInfo"] : @"{ ""data"": [] }";

		FBPaymentInfoJSON = paymentInfo;
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SetUserData : DefaultCloudAction
{
	public string dataID { get; private set; }
	public string dataValue { get; private set; }

	public SetUserData(UnigueUserID inUserID, string inDataID, string inDataValue, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		dataID = inDataID;
		dataValue = inDataValue;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserSetData(userID.primaryKey, dataID, dataValue, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SetUserDataList : DefaultCloudAction
{
	string desiredJson;

	public SetUserDataList(UnigueUserID inUserID, Dictionary<string, string> desiredFields, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		desiredJson = JsonMapper.ToJson(desiredFields);
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().SetUserDataList(userID.primaryKey, userID.passwordHash, userID.productID, desiredJson);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetUserData : BatchCommandAction
{
	public string dataID { get; private set; }

	public GetUserData(UnigueUserID inUserID, string inDataID, float inTimeOut = NoTimeOut, bool inBreakAllOnError = false)
					: base(inUserID, inTimeOut, inBreakAllOnError)
	{
		dataID = inDataID;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserGetData(userID.primaryKey, dataID, userID.passwordHash);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatUserGetData(userID.primaryKey, dataID, userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		if (result == CloudServices.RESP_NOT_FOUND)
		{
			SetStatus(BaseCloudAction.E_Status.Failed);
			failInfo = result;
		}
	}

	public override string ToString()
	{
		return string.Format("{0}({1})", GetType().Name, dataID);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetPublicUserData : BatchCommandAction
{
	public string dataID { get; private set; }
	public string primaryKey { get; private set; }

	public GetPublicUserData(string inPrimaryKey, string inDataID, float inTimeOut = NoTimeOut, bool inBreakAllOnError = false)
					: base(null, inTimeOut, inBreakAllOnError)
	{
		dataID = inDataID;
		primaryKey = inPrimaryKey;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserGetPublicData(primaryKey, dataID);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatUserGetPublicData(primaryKey, dataID);
	}

	protected override void OnSuccess()
	{
		var jsonResult = JsonMapper.ToObject(result);

		if (jsonResult.HasValue(dataID))
		{
			result = (string)jsonResult[dataID];
		}
		else
		{
			SetStatus(BaseCloudAction.E_Status.Failed);
			failInfo = result;
		}
	}

	public override string ToString()
	{
		return string.Format("{0}({1})", GetType().Name, dataID);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetUserDataList : DefaultCloudAction
{
	public string[] dataID { get; private set; }

	public GetUserDataList(UnigueUserID inUserID, string[] inDataID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		dataID = inDataID;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserGetDataList(userID.primaryKey, dataID, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class LinkIDWithUser : BatchCommandAction
{
	public string idType { get; private set; }
	public string id { get; private set; }
	public bool rewriteCurrent { get; private set; }

	public LinkIDWithUser(UnigueUserID inUserID, string inIdType, string inId, bool inRewriteCurrent = false, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		idType = inIdType;
		id = inId;
		rewriteCurrent = inRewriteCurrent;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		if (string.IsNullOrEmpty(id) == true)
			return null;
		return CloudServices.GetInstance().LinkIDWithUser(userID.primaryKey, userID.passwordHash, idType, id, rewriteCurrent);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatLinkIDWithUser(userID.primaryKey, userID.passwordHash, idType, id, rewriteCurrent);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SetFacebookId : LinkIDWithUser
{
	public SetFacebookId(UnigueUserID inUserID, string inFacebookId, float inTimeOut = NoTimeOut)
					: base(inUserID, CloudServices.LINK_ID_TYPE_FACEBOOK, inFacebookId, false, inTimeOut)
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SetDeviceId : LinkIDWithUser
{
	public SetDeviceId(UnigueUserID inUserID, string inDeviceId, float inTimeOut = NoTimeOut)
					: base(inUserID, CloudServices.LINK_ID_TYPE_DEVICE, inDeviceId, false, inTimeOut)
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SetiOSVendorId : LinkIDWithUser
{
	public SetiOSVendorId(UnigueUserID inUserID, string iniOSVendorID, float inTimeOut = NoTimeOut)
					: base(inUserID, CloudServices.LINK_ID_TYPE_IOSVENDOR, iniOSVendorID, true, inTimeOut)
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SetiOSAdvertisingId : LinkIDWithUser
{
	public SetiOSAdvertisingId(UnigueUserID inUserID, string iniOSAdvertisingID, float inTimeOut = NoTimeOut)
					: base(inUserID, CloudServices.LINK_ID_TYPE_IOSADVERTISING, iniOSAdvertisingID, true, inTimeOut)
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class UsernameAlreadyExists : BatchCommandAction
{
	public override bool parseResultDescForResultCode
	{
		get { return false; }
	}

	public string userName { get; private set; }
	public bool userExist { get; private set; }

	public UsernameAlreadyExists(string inUserName, float inTimeOut = NoTimeOut)
					: base(null, inTimeOut)
	{
		userName = inUserName.ToLower();
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserNameExists(userName);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatUserNameExists(userName);
	}

	protected override void OnSuccess()
	{
		userExist = (result == CloudServices.RESP_OK);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class UsernamesAlreadyExist : BatchCommand
{
	public struct Pair
	{
		public string Username;
		public bool Exists;
	}

	public Pair[] usernames { get; private set; }

	public UsernamesAlreadyExist(string[] inUsernames, float inTimeOut = NoTimeOut)
					: base(null, inTimeOut)
	{
		inUsernames = inUsernames ?? new string[0];

		var temp = new UsernameAlreadyExists[inUsernames.Length];
		var pairs = new Pair[inUsernames.Length];
		for (int idx = 0; idx < inUsernames.Length; ++idx)
		{
			temp[idx] = new UsernameAlreadyExists(inUsernames[idx]);
			pairs[idx] = new Pair()
			{
				Username = inUsernames[idx],
				Exists = false
			};
		}
		actions = temp;
		usernames = pairs;
	}

	protected override void OnSuccess()
	{
		for (int idx = 0; idx < usernames.Length; ++idx)
		{
			UsernameAlreadyExists action = (UsernameAlreadyExists)actions[idx];
			if (action.isSucceeded == true && action.userExist == true)
			{
				Pair pair = usernames[idx];
				pair.Exists = true;
				usernames[idx] = pair;
			}
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
// Don't use this class directly...
// Use CreateNewUserWithInitData instead.
public class _CreateNewUser : DefaultCloudAction
{
	public string username { get; private set; }
	public string passwordHash { get; private set; }
	public string productID { get; private set; }

	public _CreateNewUser(string inUsername, string inPasswordHash, string inProductID, float inTimeOut = NoTimeOut)
					: base(null, inTimeOut)
	{
		username = inUsername;
		passwordHash = inPasswordHash;
		productID = inProductID;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().CreateUser(username, productID, passwordHash, true);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class CreateNewMFAccount : CloudActionSerial
{
	public string username { get; private set; }
	public string passwordHash { get; private set; }
	public string productID { get; private set; }
	public string email { get; private set; }
	public string iWantNews { get; private set; }
	public string kind { get; private set; }

	public CreateNewMFAccount(string inUsername,
							  string inPasswordHash,
							  string inProductID,
							  string inNickName,
							  string inEmail,
							  bool iniWantNews,
							  E_UserAcctKind inKind,
							  E_AppProvider inAppProvider,
							  float inTimeOut = NoTimeOut)
					: base(null, inTimeOut, CreateActions(inUsername, inPasswordHash, inProductID, inNickName, inEmail, iniWantNews, inKind, inAppProvider)
									)
	{
		username = inUsername;
		passwordHash = inPasswordHash;
		productID = inProductID;
		email = inEmail;
		iWantNews = iniWantNews.ToString();
		kind = inKind.ToString();
	}

	static BaseCloudAction[] CreateActions(string inUsername,
										   string inPasswordHash,
										   string inProductID,
										   string inNickName,
										   string inEmail,
										   bool iniWantNews,
										   E_UserAcctKind inKind,
										   E_AppProvider inAppProvider)
	{
		Dictionary<string, string> userData = new Dictionary<string, string>();

		userData.Add(CloudServices.PROP_ID_NICK_NAME, inNickName);
		userData.Add(CloudServices.PROP_ID_EMAIL, inEmail);
		userData.Add(CloudServices.PROP_ID_I_WANT_NEWS, iniWantNews.ToString());
		userData.Add(CloudServices.PROP_ID_ACCT_KIND, inKind.ToString());

		List<BaseCloudAction> actions = new List<BaseCloudAction>(new BaseCloudAction[]
		{
			new _CreateNewUser(inUsername, inPasswordHash, inProductID),
			//new SetUserDataList(inUserID, userData) //TODO: PRIMARY KEY - vyresit poslani na cloud
			//new SetUserData   (inUserID, CloudServices.PROP_ID_NICK_NAME,   inNickName),
			//new SetUserData   (inUserID, CloudServices.PROP_ID_EMAIL,       inEmail),
			//new SetUserData   (inUserID, CloudServices.PROP_ID_I_WANT_NEWS, iniWantNews.ToString()),
			//new SetUserData   (inUserID, CloudServices.PROP_ID_ACCT_KIND,   inKind.ToString())
		});

		if (inAppProvider != E_AppProvider.Madfinger)
		{
			//actions.Add(new SetUserProductData(inUserID, CloudServices.PROP_ID_APP_PROVIDER, inAppProvider.ToString())); //TODO: PRIMARY KEY - vyresit poslani na cloud
		}

		return actions.ToArray();
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class UpdateMFAccountAndFetchPPI : CloudActionSerial
{
	public UpdateMFAccountAndFetchPPI(UnigueUserID inUserID,
									  string inNickName,
									  string inEmail,
									  bool iniWantNews,
									  string inCustomRegion,
									  float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut, CreateActions(inUserID, inNickName, inEmail, iniWantNews, inCustomRegion))
	{
	}

	static BaseCloudAction[] CreateActions(UnigueUserID inUserID, string inNickName, string inEmail, bool iniWantNews, string inCustomRegion)
	{
		Dictionary<string, string> userData = new Dictionary<string, string>();

		userData.Add(CloudServices.PROP_ID_I_WANT_NEWS, iniWantNews.ToString());
		userData.Add(CloudServices.PROP_ID_NICK_NAME, inNickName);
		userData.Add(CloudServices.PROP_ID_PASSWORD, inUserID.passwordHash);

		List<BaseCloudAction> actions = new List<BaseCloudAction>(new BaseCloudAction[]
		{
			//new SetUserData(inUserID, CloudServices.PROP_ID_I_WANT_NEWS,          iniWantNews.ToString()),
			//new SetUserData(inUserID, CloudServices.PROP_ID_NICK_NAME,            inNickName),
			////new SetUserData(inUserID, CloudServices.PROP_ID_EMAIL,              inEmail),
			new SetUserDataList(inUserID, userData),
			new SetUserProductData(inUserID, CloudServices.PROP_ID_CUSTOM_REGION, inCustomRegion),
			//new SetUserData(inUserID, CloudServices.PROP_ID_PASSWORD,             inPasswordHash),
			new FetchPlayerPersistantInfo(new UnigueUserID(inUserID.primaryKey, inUserID.passwordHash, inUserID.productID))
		});
		return actions.ToArray();
	}
}

// =====================================================================================================================
// =====================================================================================================================
class _UserExist : BatchCommandAction
{
	public _UserExist(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserExists(userID.primaryKey, "", userID.passwordHash);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatUserExists(userID.primaryKey, "", userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		if (result != CloudServices.RESP_OK)
		{
			SetStatus(BaseCloudAction.E_Status.Failed);
			failInfo = result;
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
class _VaidateUserData : BatchCommandAction
{
	public _VaidateUserData(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().ValidateUserAccount(userID.primaryKey, userID.productID, userID.passwordHash);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatValidateUserAccount(userID.primaryKey, userID.productID, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class AuthenticateUser : BatchCommand
{
	public string deviceID { get; private set; }
	public string facebookID { get; private set; }

	public AuthenticateUser(UnigueUserID inUserID, string inDeviceID, string inFacebookID, float inTimeOut = NoTimeOut)
					: base(inUserID,
						   inTimeOut,
						   new BaseCloudAction[]
						   {
							   new _UserExist(inUserID),
							   new _VaidateUserData(inUserID),
							   new SetDeviceId(inUserID, inDeviceID),
							   new SetFacebookId(inUserID, inFacebookID),
							   new GetUserRegionInfo(inUserID),
							   new GetCustomRegionInfo(inUserID),
							   new GetUserData(inUserID, CloudServices.PROP_ID_NICK_NAME),
							   new GetUserData(inUserID, CloudServices.PROP_ID_I_WANT_NEWS),
							   new GetUserData(inUserID, CloudServices.PROP_ID_ACCT_KIND),
							   new GetAvailablePremiumAccounts(inUserID),
							   new GetGameInfoSettings(inUserID)
						   })
	{
		deviceID = inDeviceID;
		facebookID = inFacebookID;
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetIOSVendorIDInfo : BatchCommandAction
{
	public string iOSVendorID { get; private set; }
	public string idType { get; private set; }
	public string primaryKey { get; private set; }
	public string deviceID { get; private set; }

	public GetIOSVendorIDInfo(string iOSVendorID, string idType, float inTimeOut = NoTimeOut)
					: base(null, inTimeOut)
	{
		this.iOSVendorID = iOSVendorID;
		this.idType = idType;

		primaryKey = string.Empty;
		deviceID = string.Empty;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().GetIOSVendorIDInfo(iOSVendorID, idType);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatGetIOSVendorIDInfo(iOSVendorID, idType);
	}

	protected override void OnSuccess()
	{
		try
		{
			JsonData data = JsonMapper.ToObject(result);
			string[] devices = data.HasValue("deviceId") ? JsonMapper.ToObject<string[]>((string)data["deviceId"]) : null;

			primaryKey = data.HasValue("user") ? (string)data["user"] : string.Empty;
			deviceID = devices != null ? devices[devices.Length - 1] : string.Empty;
		}
		catch
		{
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetPrimaryKeyLinkedWithID : BatchCommandAction
{
	public E_UserAcctKind acctKind { get; private set; }
	public string idType { get; private set; }
	public string id { get; private set; }
	public string primaryKey { get; private set; }

	public bool isPrimaryKeyForSHDZ
	{
		get
		{
			if (acctKind != E_UserAcctKind.Guest)
				return true;
			if (string.IsNullOrEmpty(primaryKey) == true)
				return false;
			return primaryKey.StartsWith("guest") || primaryKey.StartsWith("uid_guest");
		}
	}

	public GetPrimaryKeyLinkedWithID(E_UserAcctKind inAcctKind,
									 string inID,
									 string idType,
									 float inTimeOut = NoTimeOut,
									 bool inBreakAllOnError = false)
					: base(null, inTimeOut, inBreakAllOnError)
	{
		acctKind = inAcctKind;
		this.idType = idType;
		id = inID;
		primaryKey = null;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		string accountType = acctKind.ToString();

		if (string.IsNullOrEmpty(accountType) == false && string.IsNullOrEmpty(idType) == false)
		{
			return CloudServices.GetInstance().GetUserLinkedWithID(id, idType, accountType);
		}

		return null;
	}

	protected override object GetBatchObject()
	{
		string accountType = acctKind.ToString();

		if (string.IsNullOrEmpty(accountType) == false && string.IsNullOrEmpty(idType) == false)
		{
			return CloudServices.FormatGetUserLinkedWithID(id, idType, accountType);
		}

		return null;
	}

	protected override void OnSuccess()
	{
		try
		{
			JsonData data = JsonMapper.ToObject(result);
			if (data.HasValue(CloudServices.PARAM_ID_USERID) == true)
			{
				primaryKey = data[CloudServices.PARAM_ID_USERID].ToString();
			}
			else
			{
				SetStatus(BaseCloudAction.E_Status.Failed);
				failInfo = "no primaryKey received";
			}
		}
		catch
		{
			SetStatus(BaseCloudAction.E_Status.Failed);
			failInfo = "invalid data format";
		}
	}
}

public class GetPrimaryKeysLinkedWithID : BatchCommandAction
{
	public string Id { get; private set; }
	public string IdType { get; private set; }
	public E_UserAcctKind AccountKind { get; private set; }
	public string MatchedPrimaryKeys { get; private set; }
	public string[] AllPrimaryKeys { get; private set; }

	public GetPrimaryKeysLinkedWithID(string inId,
									  string inIdType,
									  E_UserAcctKind inAccountKind,
									  float inTimeOut = NoTimeOut,
									  bool inBreakAllOnError = true)
					: base(null, inTimeOut, inBreakAllOnError)
	{
		Id = inId;
		IdType = inIdType;
		AccountKind = inAccountKind;
		MatchedPrimaryKeys = string.Empty;
		AllPrimaryKeys = new string[0];
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().GetUsersLinkedWithID(Id, IdType, AccountKind.ToString());
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatGetUsersLinkedWithID(Id, IdType, AccountKind.ToString());
	}

	protected override void OnSuccess()
	{
		try
		{
			var jsonResult = JsonMapper.ToObject(result);

			if (jsonResult.HasValue("result"))
			{
				jsonResult = jsonResult["result"];

				if (jsonResult.HasValue("userid") == true)
				{
					MatchedPrimaryKeys = (string)jsonResult["userid"];
				}

				if (jsonResult.HasValue("users") == true)
				{
					AllPrimaryKeys = JsonMapper.ToObject<string[]>(jsonResult["users"].ToJson());
				}
			}
		}
		catch
		{
			SetStatus(BaseCloudAction.E_Status.Failed);
			failInfo = "invalid data format";
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetUserRegionInfo : BatchCommandAction
{
	public NetUtils.GeoRegion region { get; private set; }
	public string countryCode { get; private set; }

	public GetUserRegionInfo(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		region = NetUtils.GeoRegion.None;
		countryCode = string.Empty;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().GetMyRegionInfo();
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatGetMyRegionInfo();
	}

	protected override void OnSuccess()
	{
		try
		{
			JsonData regionInfo = JsonMapper.ToObject(result);

			string[] lon_lat_pos = regionInfo["pos"].ToString().Split(',');
			//Debug.Log("lon_lat_pos " + lon_lat_pos[0] + " - " + lon_lat_pos[1]);

			region = NetUtils.GetRegionFromGeoPoint(float.Parse(lon_lat_pos[0]), float.Parse(lon_lat_pos[1]));

			// initialize current country code...
			countryCode = regionInfo["country"].ToString();
		}
		catch
		{
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetAvailablePremiumAccounts : BatchCommandAction
{
	public CloudServices.PremiumAccountDesc[] accounts { get; private set; }

	public GetAvailablePremiumAccounts(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().ProductGetParam(userID.productID, CloudServices.PROP_ID_PREMIUM_ACC_DESC, userID.passwordHash);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatProductGetParam(userID.productID, CloudServices.PROP_ID_PREMIUM_ACC_DESC, userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		try
		{
			accounts = JsonMapper.ToObject<CloudServices.PremiumAccountDesc[]>(result);
		}
		catch
		{
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class UpdateSwearWords : DefaultCloudAction
{
	public UpdateSwearWords(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().ProductGetParam(userID.productID, CloudServices.PROP_ID_SWEAR_WORDS, userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		try
		{
			string[] dictionary = null;

			JsonData data = JsonMapper.ToObject(result);
			if (data.IsArray == true)
			{
				dictionary = new string[data.Count];
				for (int idx = 0; idx < data.Count; ++idx)
				{
					dictionary[idx] = data[idx].ToString();
				}
			}

			SwearWords.Dictionary = dictionary;
		}
		catch
		{
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetCustomRegionInfo : BatchCommandAction
{
	public NetUtils.GeoRegion region { get; private set; }

	public GetCustomRegionInfo(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut, false)
	{
		region = NetUtils.GeoRegion.None;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance()
							.UserGetPerProductData(userID.primaryKey, userID.productID, CloudServices.PROP_ID_CUSTOM_REGION, userID.passwordHash);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatUserGetPerProductData(userID.primaryKey,
														 userID.productID,
														 CloudServices.PROP_ID_CUSTOM_REGION,
														 userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		region = NetUtils.GetRegionFromString((string)result);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SetUserProductData : DefaultCloudAction
{
	public string dataID { get; private set; }
	public string dataValue { get; private set; }

	public SetUserProductData(UnigueUserID inUserID, string inDataID, string inDataValue, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		dataID = inDataID;
		dataValue = inDataValue;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserSetPerProductData(userID.primaryKey, userID.productID, dataID, dataValue, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetUserProductData : DefaultCloudAction
{
	public string dataID { get; private set; }

	public GetUserProductData(UnigueUserID inUserID, string inDataID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		dataID = inDataID;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().UserGetPerProductData(userID.primaryKey, userID.productID, dataID, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetPlayerPersistantInfo : GetUserProductData
{
	public GetPlayerPersistantInfo(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, CloudServices.PROP_ID_PLAYER_DATA, inTimeOut)
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SetPlayerPersistantInfo : SetUserProductData
{
	public SetPlayerPersistantInfo(UnigueUserID inUserID, string inPPInfo, float inTimeOut = NoTimeOut)
					: base(inUserID, CloudServices.PROP_ID_PLAYER_DATA, inPPInfo, inTimeOut)
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class FetchPlayerPersistantInfo : DefaultCloudAction
{
	public FetchPlayerPersistantInfo(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance()
							.UserGetPerProductData(userID.primaryKey, userID.productID, CloudServices.PROP_ID_PLAYER_DATA, userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		PlayerPersistantInfo PPIFromCloud = new PlayerPersistantInfo();
		if (PPIFromCloud.InitPlayerDataFromStr(result))
		{
			PPIManager.Instance.SetPPIFromCloud(PPIFromCloud);
		}
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class QueryFriendsInfo : DefaultCloudAction
{
	public string friends { get; private set; }

	public QueryFriendsInfo(UnigueUserID inUserID, string inFriends, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		friends = inFriends;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		//public AsyncOpResult QueryFriendsInfo(string userId,string productId,string friendsListJSON,string passwordHash)	
		return CloudServices.GetInstance().QueryFriendsInfo(userID.primaryKey, userID.productID, friends, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================

public class ShopBuyAction : DefaultCloudAction
{
	int m_ItemID;

	public ShopBuyAction(UnigueUserID inUserID, int itemID)
					: base(inUserID)
	{
		m_ItemID = itemID;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().BuyItem(userID.primaryKey, userID.productID, m_ItemID, userID.passwordHash);
	}
};

public class BuyAndFetchPPI : CloudActionSerial
{
	//public delegate void OpDoneDelegate(bool success);
	//private OpDoneDelegate m_OnDoneDelegate;
	BaseCloudAction m_PPIAction;

	public BuyAndFetchPPI(UnigueUserID inUserID, int itemID, /*OpDoneDelegate onDone,*/ float inTimeOut = NoTimeOut)
					: base(inUserID,
						   inTimeOut,
						   new BaseCloudAction[]
						   {
							   new ShopBuyAction(inUserID, itemID),
							   new FetchPlayerPersistantInfo(inUserID)
						   })
	{
		//m_OnDoneDelegate = onDone;
	}

	protected override void OnSuccess()
	{
		//m_OnDoneDelegate(true);
	}

	protected override void OnFailed()
	{
		//m_OnDoneDelegate(false);
	}
}

// ------
class ResearchRefundAction : DefaultCloudAction
{
	int[] m_ItemIDs;

	public ResearchRefundAction(UnigueUserID inUserID, int[] itemIDs) : base(inUserID)
	{
		m_ItemIDs = itemIDs;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		CloudServices.AsyncOpResult result = null;
		result = CloudServices.GetInstance().RefundItems(userID.primaryKey, userID.productID, m_ItemIDs, userID.passwordHash);
		/*foreach (int itemID in m_ItemIDs) // until we have version for refunding all items at once
		{	
			result = CloudServices.GetInstance().RefundItem(userID.userName, userID.productID, itemID, userID.passwordHash);
		}/**/
		return result;
	}
};

// ------
public class RefundItems : CloudActionSerial
{
	//public delegate void OpDoneDelegate(bool success);
	//private OpDoneDelegate m_OnDoneDelegate;
	BaseCloudAction m_PPIAction;

	public RefundItems(UnigueUserID inUserID, int[] itemIDs, float inTimeOut = NoTimeOut)
					: base(inUserID,
						   inTimeOut,
						   new BaseCloudAction[]
						   {
							   new ResearchRefundAction(inUserID, itemIDs)
						   })
	{
		//m_OnDoneDelegate = onDone;
	}

	protected override void OnSuccess()
	{
		//m_OnDoneDelegate(true);
	}

	protected override void OnFailed()
	{
		//m_OnDoneDelegate(false);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class _BuyPremiumAccount : DefaultCloudAction
{
	string m_AccountTypeID;

	public _BuyPremiumAccount(UnigueUserID inUserID, string inAccountTypeID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		m_AccountTypeID = inAccountTypeID;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().BuyPremiumAccount(userID.primaryKey, userID.productID, m_AccountTypeID, userID.passwordHash);
	}
}

public class BuyPremiumAccountAndFetchPPI : CloudActionSerial
{
	public BuyPremiumAccountAndFetchPPI(UnigueUserID inUserID, string inAccountTypeID, float inTimeOut = NoTimeOut)
					: base(inUserID,
						   inTimeOut,
						   new BaseCloudAction[]
						   {
							   new _BuyPremiumAccount(inUserID, inAccountTypeID, inTimeOut),
							   new FetchPlayerPersistantInfo(inUserID)
						   })
	{
	}
}

// =====================================================================================================================
// =====================================================================================================================

public class SlotEquipAction : DefaultCloudAction
{
	int m_ItemID;
	int m_SlotIdx;
	bool m_Equip; //false pro unequip

	public SlotEquipAction(UnigueUserID inUserID, int itemID, int slotIdx, bool equip)
					: base(inUserID)
	{
		m_ItemID = itemID;
		m_SlotIdx = slotIdx;
		m_Equip = equip;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		if (m_Equip)
			return CloudServices.GetInstance().EquipItem(userID.primaryKey, userID.productID, m_ItemID, m_SlotIdx, userID.passwordHash);
		else
			return CloudServices.GetInstance().UnEquipItem(userID.primaryKey, userID.productID, m_ItemID, m_SlotIdx, userID.passwordHash);
	}
};

// =====================================================================================================================
// =====================================================================================================================
public class GetProductGlobalMessages : DefaultCloudAction
{
	public int firstMessageIndex { get; private set; }
	public string mailboxName { get; private set; }

	public GetProductGlobalMessages(UnigueUserID inUserID, int inFirstMessageIndex, string inMailboxName, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		firstMessageIndex = inFirstMessageIndex;
		mailboxName = inMailboxName;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().FetchInboxMessages(null, userID.productID, mailboxName, userID.passwordHash, firstMessageIndex);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetMessagesFromInbox : DefaultCloudAction
{
	public bool globalInbox { get; private set; }

	public GetMessagesFromInbox(UnigueUserID inUserID, bool inGlobalInbox, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		globalInbox = inGlobalInbox;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		string productId = globalInbox == true ? null : userID.productID;
		return CloudServices.GetInstance().FetchInboxMessages(userID.primaryKey, productId, null, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class RemoveMessagesFromInbox : DefaultCloudAction
{
	public bool globalInbox { get; private set; }
	public int lastMsgIndex { get; private set; }

	public RemoveMessagesFromInbox(UnigueUserID inUserID, bool inGlobalInbox, int inLastMsgIndex, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		globalInbox = inGlobalInbox;
		lastMsgIndex = inLastMsgIndex;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		string productId = globalInbox == true ? null : userID.productID;
		return CloudServices.GetInstance().InboxRemoveMessages(userID.primaryKey, productId, lastMsgIndex, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SendMessage : DefaultCloudAction
{
	public string recipient { get; private set; }
	public string message { get; private set; }
	public bool globalInbox { get; private set; }

	public SendMessage(UnigueUserID inUserID, string inRecipient, string inMessage, bool inGlobalInbox, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		recipient = inRecipient;
		message = inMessage;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		string productId = globalInbox == true ? null : userID.productID;
		return CloudServices.GetInstance().InboxAddMsg(userID.primaryKey, recipient, productId, message, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SendFriendRequestMessage : SendMessage
{
	public SendFriendRequestMessage(UnigueUserID inUserID, string inRecipient, string inMessage, float inTimeOut = NoTimeOut)
					: base(inUserID, inRecipient, inMessage, true, inTimeOut)
	{
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().RequestAddFriend(userID.primaryKey, recipient, message, userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		LobbyClient.SendMessageToPlayer(recipient, FriendList.REQUEST_ID, FriendList.MESSAGE_ADD);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class CancelFriendship : DefaultCloudAction
{
	public string friend { get; private set; }

	public CancelFriendship(UnigueUserID inUserID, string inFriendName, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		friend = inFriendName;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().RequestDelFriend(userID.primaryKey, friend, userID.passwordHash);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SendCloudCommand : DefaultCloudAction
{
	public string command { get; private set; }

	public SendCloudCommand(UnigueUserID inUserID, string inCommand, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		command = inCommand;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().ProcessResponseCmd(command);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SendAcceptFriendCommand : SendCloudCommand
{
	public string friend { get; private set; }

	public SendAcceptFriendCommand(UnigueUserID inUserID, string inFriendName, string inCommand, float inTimeOut = NoTimeOut)
					: base(inUserID, inCommand, inTimeOut)
	{
		friend = inFriendName;
	}

	protected override void OnSuccess()
	{
		LobbyClient.SendMessageToPlayer(friend, FriendList.REQUEST_ID, FriendList.MESSAGE_ACCEPT);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class EquipAndFetchPPI : CloudActionSerial
{
	public delegate void EquipDoneDelegate(ShopItemId selItem, int slotIndex, bool success);
	EquipDoneDelegate m_EquipDoneDelegate;
	BaseCloudAction m_PPIAction;
	int m_SlotIndex;
	ShopItemId m_Item;

	public EquipAndFetchPPI(UnigueUserID inUserID,
							int itemID,
							int slotIdx,
							ShopItemId inItem,
							EquipDoneDelegate onDone,
							float inTimeOut = NoTimeOut)
					: base(inUserID,
						   inTimeOut,
						   new BaseCloudAction[]
						   {
							   new SlotEquipAction(inUserID, itemID, slotIdx, inItem.IsEmpty() == false),
							   new FetchPlayerPersistantInfo(inUserID)
						   })
	{
		m_Item = inItem;
		m_SlotIndex = slotIdx;
		m_EquipDoneDelegate = onDone;
	}

	protected override void OnSuccess()
	{
		m_EquipDoneDelegate(m_Item, m_SlotIndex, true);
	}

	protected override void OnFailed()
	{
		m_EquipDoneDelegate(m_Item, m_SlotIndex, false);
	}
}

// =====================================================================================================================
// =====================================================================================================================

public class SwitchAndFetchPPI : CloudActionSerial
{
	public delegate void SwitchDoneDelegate(ShopItemId item1, int slotIndex1, ShopItemId item2, int slotIndex2, bool success);
	SwitchDoneDelegate m_SwitchDoneDelegate;
	//private BaseCloudAction m_PPIAction;
	int m_SlotIndex1;
	int m_SlotIndex2;
	ShopItemId m_Item1;
	ShopItemId m_Item2;

	public SwitchAndFetchPPI(UnigueUserID inUserID,
							 int itemID1,
							 int slotIdx1,
							 ShopItemId inItem1,
							 int itemID2,
							 int slotIdx2,
							 ShopItemId inItem2,
							 SwitchDoneDelegate onDone,
							 float inTimeOut = NoTimeOut)
					: base(inUserID,
						   inTimeOut,
						   new BaseCloudAction[]
						   {
							   new SlotEquipAction(inUserID, itemID1, slotIdx1, !inItem1.IsEmpty()),
							   new SlotEquipAction(inUserID, itemID2, slotIdx2, !inItem2.IsEmpty()),
							   new FetchPlayerPersistantInfo(inUserID)
						   })
	{
		m_SwitchDoneDelegate = onDone;
		m_SlotIndex1 = slotIdx1;
		m_SlotIndex2 = slotIdx2;
		m_Item1 = inItem1;
		m_Item2 = inItem2;
	}

	protected override void OnSuccess()
	{
		m_SwitchDoneDelegate(m_Item1, m_SlotIndex1, m_Item2, m_SlotIndex2, true);
	}

	protected override void OnFailed()
	{
		m_SwitchDoneDelegate(m_Item1, m_SlotIndex1, m_Item2, m_SlotIndex2, false);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class ForgotPassword : DefaultCloudAction
{
	public string primaryKey { get; private set; }

	public ForgotPassword(string inPrimaryKey, float inTimeOut = NoTimeOut)
					: base(null, inTimeOut)
	{
		primaryKey = inPrimaryKey.ToLower();
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().RequestResetPassword(primaryKey, TextDatabase.instance[0103045], TextDatabase.instance[0103046]);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class ForgotPasswordWithEmail : DefaultCloudAction
{
	public string primaryKey { get; private set; }
	public string userEmail { get; private set; }

	public ForgotPasswordWithEmail(string inPrimaryKey, string inUserEmail, float inTimeOut = NoTimeOut)
					: base(null, inTimeOut)
	{
		primaryKey = inPrimaryKey.ToLower();
		userEmail = inUserEmail.ToLower();
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance()
							.RequestResetPasswordWithEmail(primaryKey, userEmail, TextDatabase.instance[0103045], TextDatabase.instance[0103046]);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class SlotMachineSpin : DefaultCloudAction
{
	public int Reward { get; private set; }

	// column one uses indexes 0-2, c2 indexes 3-5, c3 indexes 6-8
	public E_SlotMachineSymbol[] Matrix { get; private set; }

	public SlotMachineSpin(UnigueUserID inUserID)
					: base(inUserID)
	{
		Matrix = new E_SlotMachineSymbol[9];
		Reward = 0;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().SlotMachineSpin(userID.primaryKey, userID.productID, userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		DefaultMatrix();

		if (result != CloudServices.RESP_OPERATION_REFUSED)
		{
			ParseResult(result);
		}
#if UNITY_EDITOR
		else
		{
			Debug.Log("SlotMachineSpin : not enough gold");
		}
#endif
	}

	protected override void OnFailed()
	{
		DefaultMatrix();
	}

	void ParseResult(string result)
	{
		JsonData[] jsonData = JsonMapper.ToObject<JsonData[]>(result);

		foreach (JsonData json in jsonData)
		{
			try
			{
				Reward = System.Convert.ToInt32(json["reward"].ToString());

				string matrix = json["matrix"].ToString();

				BuildMatrix(matrix);

#if UNITY_EDITOR
				Debug.Log("SlotMachineSpin : " + Reward + " golds ( " + " matrix : " + matrix + " )");
#endif
			}
			catch
			{
				DefaultMatrix();

#if UNITY_EDITOR
				Debug.LogWarning("SlotMachineSpin : invalid JSON data");
#endif
			}
		}
	}

	void DefaultMatrix()
	{
		BuildMatrix("JGFFCGEGA");
	}

	void BuildMatrix(string matrix)
	{
		matrix = matrix.ToUpper();

		for (int i = 0; i < 9; i++)
		{
			Matrix[i] = ConvertSymbol(matrix[i]);
		}
	}

	E_SlotMachineSymbol ConvertSymbol(char symbol)
	{
		if (symbol >= 'A' && symbol <= 'H')
		{
			int index = 'H' - symbol;

			return (E_SlotMachineSymbol)index;
		}
		else if (symbol == 'J')
		{
			return E_SlotMachineSymbol.Jackpot;
		}

		return E_SlotMachineSymbol.None;
	}
};

// =====================================================================================================================
// =====================================================================================================================
public class GetDailyRewards : BatchCommandAction
{
	public bool HasInstant { get; private set; }
	public bool HasConditional { get; private set; }

	public bool HasReward
	{
		get { return HasInstant || HasConditional; }
	}

	public GetDailyRewards(UnigueUserID inUserID)
					: base(inUserID)
	{
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().GetDailyRewards(userID.primaryKey, userID.productID, userID.passwordHash);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatGetDailyRewards(userID.primaryKey, userID.productID, userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		JsonData data = JsonMapper.ToObject(result);

		HasInstant = data.HasValue("Instant") ? (bool)data["Instant"] : false;
		HasConditional = data.HasValue("Conditional") ? (bool)data["Conditional"] : false;
	}
};

// =====================================================================================================================
// =====================================================================================================================
public class GetSlotmachineJackpot : DefaultCloudAction
{
	public GetSlotmachineJackpot(UnigueUserID inUserID)
					: base(inUserID)
	{
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().GetSlotMachineJackpot(userID.productID);
	}
}

// =====================================================================================================================
// =====================================================================================================================
public class GetConfig : BatchCommandAction
{
	public string Version { get; private set; }
	public string Kind { get; private set; }

	public GetConfig(UnigueUserID inUserID, string kind, string version)
					: base(inUserID)
	{
		Kind = kind;
		Version = version;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().GetConfig(userID.productID, Kind, Version);
	}

	protected override object GetBatchObject()
	{
		return CloudServices.FormatGetConfig(userID.productID, Kind, Version);
	}
};

// =====================================================================================================================
// =====================================================================================================================
public class GetCloudDateTime : BatchCommandAction
{
	public GetCloudDateTime(UnigueUserID inUserID)
					: base(inUserID)
	{
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().GetCloudDateTime(userID.primaryKey, userID.passwordHash);
	}

	protected override object GetBatchObject()
	{
		return null;
	}

	protected override void OnSuccess()
	{
		CloudDateTime.Load(result);
	}
};

// =====================================================================================================================
// =====================================================================================================================
public class GetIPConfig : GetConfig
{
	public GetIPConfig(UnigueUserID inUserID, string version)
					: base(inUserID, CloudServices.CONFIG_IP, version)
	{
	}

	protected override void OnFailed()
	{
		// This action is considered as successfull if the cloud server is unable to find record. In such case the result is an empty string.
		if (failInfo.StartsWith("400 "))
		{
			failInfo = null;
			result = "";
			SetStatus(BaseCloudAction.E_Status.Success);
		}
	}
};

// =====================================================================================================================
// =====================================================================================================================
public class GetGameInfoSettings : GetConfig
{
	public GetGameInfoSettings(UnigueUserID inUserID, string version = null)
					: base(inUserID, CloudServices.CONFIG_GAME_TYPES, string.IsNullOrEmpty(version) ? GameInfoSettings.DEFAULT_GAME_TYPES_ID : version)
	{
	}

	protected override void OnSuccess()
	{
		GameInfoSettings.Load(result);
	}
};

// =====================================================================================================================
// =====================================================================================================================
public class QueryUsersByField : DefaultCloudAction
{
	string m_FieldID;
	string m_Value;
	int m_MaxCount;

	// try to keep maxResults at low values
	public QueryUsersByField(UnigueUserID inUserID, string fieldID, string desiredValue, int maxResults = 1)
					: base(inUserID)
	{
		m_FieldID = fieldID;
		m_Value = desiredValue;
		m_MaxCount = maxResults;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().QueryUsersByField(m_FieldID, m_Value, m_MaxCount, false);
	}
};

// =====================================================================================================================
// =====================================================================================================================
public class RegisterForPushNotifications : DefaultCloudAction
{
	public enum Provider
	{
		Apple,
		Google
	}

	public const string RESULT_REGISTERED = "registered";
	public const string RESULT_UNREGISTERED = "unregistered";

	public RegisterForPushNotifications(UnigueUserID inUserID,
										Provider provider,
										string registrationId,
										bool register,
										float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
		this.provider = provider;
		this.registrationId = registrationId;
		this.register = register;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance()
							.RegisterForPushNotifications(userID.primaryKey, userID.productID, provider.ToString(), registrationId, register, userID.passwordHash);
	}

	protected override void OnSuccess()
	{
		result = string.Empty;

		if (resultCode >= E_HttpResultCode.Ok && resultCode < E_HttpResultCode.BadRequest)
		{
			result = register ? RESULT_REGISTERED : RESULT_UNREGISTERED;
		}
	}

	public Provider provider { get; private set; }
	public string registrationId { get; private set; }
	public bool register { get; private set; }
}

public class GetAppLicense : DefaultCloudAction
{
	public enum Platform
	{
		OUYA
	}

	public GetAppLicense(string inProductId, Platform inPlatform, string inLicenseRequest, float inTimeOut = NoTimeOut)
					: base(null, inTimeOut)
	{
		mProductId = inProductId;
		mPlatform = inPlatform;
		mLicenseRequest = inLicenseRequest;
		License = string.Empty;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		switch (mPlatform)
		{
		case Platform.OUYA:
			return CloudServices.GetInstance().GetAppLicenseOuya(mProductId, mLicenseRequest);
		default:
			return null;
		}
	}

	protected override void OnSuccess()
	{
		var responseJSON = JsonMapper.ToObject(result);

		License = responseJSON.HasValue("application_license") ? (string)responseJSON["application_license"] : string.Empty;
	}

	public string License { get; private set; }

	string mProductId;
	Platform mPlatform;
	string mLicenseRequest;
}

// =====================================================================================================================
// =====================================================================================================================
public interface IBatchAction
{
	bool BatchBreakAllOnError { get; }
	object BatchObject { get; }
	void SetBatchResult(BaseCloudAction.E_Status inStatus, E_HttpResultCode inResponseCode, string inResponse);
}
public abstract class BatchCommandAction : DefaultCloudAction, IBatchAction
{
	public bool breakAllOnError { get; private set; }

	public BatchCommandAction(UnigueUserID inUserID, float inTimeOut = NoTimeOut, bool inBreakAllOnError = true)
					: base(inUserID, inTimeOut)
	{
		breakAllOnError = inBreakAllOnError;
	}

	protected abstract object GetBatchObject();

	#region IBatchAction implementation

	bool IBatchAction.BatchBreakAllOnError
	{
		get { return breakAllOnError; }
	}

	object IBatchAction.BatchObject
	{
		get { return GetBatchObject(); }
	}

	void IBatchAction.SetBatchResult(E_Status inStatus, E_HttpResultCode inResponseCode, string inResponse)
	{
		resultCode = inResponseCode;

		SetStatus(inStatus);

		switch (status)
		{
		case BaseCloudAction.E_Status.Failed:
			if (string.IsNullOrEmpty(inResponse) == false)
			{
				failInfo = inResponse;
			}
			OnFailed();
			break;
		case BaseCloudAction.E_Status.Success:
			if (string.IsNullOrEmpty(inResponse) == false)
			{
				result = inResponse;
			}
			OnSuccess();
			break;
		default:
			break;
		}
	}

	#endregion
}

// =====================================================================================================================
// =====================================================================================================================
public class BatchCommand : DefaultCloudAction
{
	BaseCloudAction[] m_Actions;
	IBatchAction[] m_Pending;
	object[] m_Data;

	public BaseCloudAction[] actions
	{
		get { return m_Actions; }
		protected set { PrepareActions(value); }
	}

	// @see CloudServiceTester - BatchCommand usage
	protected BatchCommand(UnigueUserID inUserID, float inTimeOut = NoTimeOut)
					: base(inUserID, inTimeOut)
	{
	}

	public BatchCommand(BatchCommandAction[] inActions) : this(null, NoTimeOut, inActions)
	{
	}

	public BatchCommand(UnigueUserID inUserID, params BaseCloudAction[] inActions) : this(inUserID, NoTimeOut, inActions)
	{
	}

	public BatchCommand(UnigueUserID inUserID, float inTimeOut, params BaseCloudAction[] inActions)
					: base(inUserID, inTimeOut)
	{
		actions = inActions;
	}

	protected override CloudServices.AsyncOpResult GetCloudAsyncOp()
	{
		return CloudServices.GetInstance().BatchCommand(JsonMapper.ToJson(m_Data));
	}

	protected override void OnUpdate()
	{
		if (status != BaseCloudAction.E_Status.Success)
		{
			PropagateStatus(status);
		}
		else
		{
			try
			{
				JsonData data = JsonMapper.ToObject(result);

				if (data.IsArray == true)
				{
					for (int idx = 0; idx < data.Count; ++idx)
					{
						IBatchAction action = m_Pending[idx];
						JsonData child = data[idx];
						var responseCode = (E_HttpResultCode)(int)child["responseCode"];
						string response = (string)child["response"];
						E_Status actionStatus = responseCode >= E_HttpResultCode.BadRequest ? E_Status.Failed : E_Status.Success;

						bool actionBreakAllOnError = true;
						if (action != null)
						{
							action.SetBatchResult(actionStatus, responseCode, response);
							actionBreakAllOnError = action.BatchBreakAllOnError;
						}

						if (actionStatus == E_Status.Failed && actionBreakAllOnError == true)
						{
							string err = action != null ? string.Format("{0}: {1}", action, response) : response;
							failInfo = string.IsNullOrEmpty(failInfo) ? err : string.Format("{0}{1}{2}", failInfo, System.Environment.NewLine, err);
							SetStatus(E_Status.Failed);
						}
					}
				}
			}
			catch
			{
				Debug.LogError("BatchCommand.GetCloudAsyncOp() :: Invalid data format in the request response!");

				SetStatus(E_Status.Failed);
			}
		}
	}

	void PrepareActions(BaseCloudAction[] inActions)
	{
		m_Actions = inActions ?? new BaseCloudAction[0];
		if (m_Actions.Length <= 0)
		{
			SetStatus(BaseCloudAction.E_Status.Success);
			return;
		}

		var pending = new List<IBatchAction>();
		var data = new List<object>();
		int count = m_Actions.Length;

		for (int idx = 0; idx < count; ++idx)
		{
			IBatchAction action = m_Actions[idx] as IBatchAction;
			if (action == null)
			{
				Debug.LogError("BatchCommand :: Cloud action '" + m_Actions[idx].GetType().Name + "' doesn't implement IBatchAction interface!");
				SetStatus(BaseCloudAction.E_Status.Failed);
				return;
			}

			object obj = action.BatchObject;
			if (obj != null)
			{
				JsonData json = JsonMapper.ToObject(JsonMapper.ToJson(obj));

				json["_breakAllOnError"] = action.BatchBreakAllOnError;

				pending.Add(action);
				data.Add(json);
			}
			else
			{
				action.SetBatchResult(BaseCloudAction.E_Status.Failed, 0, null);
			}
		}

		m_Pending = pending.ToArray();
		m_Data = data.ToArray();
	}

	void PropagateStatus(E_Status status)
	{
		if (m_Pending == null)
			return;

		foreach (IBatchAction action in m_Pending)
		{
			if (action != null)
			{
				action.SetBatchResult(status, 0, null);
			}
		}
	}
};

// =====================================================================================================================
// =====================================================================================================================
