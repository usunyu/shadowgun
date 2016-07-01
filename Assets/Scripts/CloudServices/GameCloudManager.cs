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
using Queue = System.Collections.Queue;

// =============================================================================================================================
// =============================================================================================================================
public class GameCloudManager : MonoBehaviour
{
	// -- public properties...
	//public static string						productID		{ get { return "DeadTrigger"; 			} }
	public static FriendList friendList
	{
		get { return instance.m_FriendList; }
	}

	public static FacebookFriendList facebookFriendList
	{
		get { return instance.m_FacebookFriendList; }
	}

	public static CloudMailbox mailbox
	{
		get { return instance.m_CloudMailbox; }
	}

	//public static PlayerPersistantInfo		cloudPPI		{ get { return instance.m_CloudPPI; 	} }
	public static News news
	{
		get { return instance.m_News; }
	}

	public static bool isBusy
	{
		get { return instance.PendingActions.Count > 0 || instance.ActiveAction != null ? true : false; }
	}

	// -------------------------------------------------------------------------------------------------------------------------
	// private part...
	static GameCloudManager m_sInstance;

	static GameCloudManager instance
	{
		get { return GetInstance(); }
	}

	// Cloud service comunication...
	Queue<BaseCloudAction> PendingActions = new Queue<BaseCloudAction>();
	BaseCloudAction ActiveAction;

	// other members...
	FriendList m_FriendList;
	FacebookFriendList m_FacebookFriendList = null;
	CloudMailbox m_CloudMailbox;
	News m_News;

	// -------------------------------------------------------------------------------------------------------------------------
	//private PlayerPersistantInfo				m_CloudPPI;
	//private DataFileJSON						m_CloudProgress;
	//private System.DateTime					m_LastDownloadTime;
	//private const int 						SKIP_UPDATE_TIMEOUT = 1;	// one minute...

	// =========================================================================================================================
	// === public interface ====================================================================================================
	public static void AddAction(BaseCloudAction inAction)
	{
		instance.PendingActions.Enqueue(inAction);
	}

	/*public static void SetCloudPPI(PlayerPersistantInfo	inCloudPPI)
	{
		instance.m_CloudPPI = inCloudPPI;
	}*/
	/*
	public static BaseCloudAction SendPPIToCloud(string inPPIInJSON)
	{
		// check preconditions...
		if(CloudUser.instance.isUserAuthenticated == false)	{
			Debug.LogWarning("Can't send PPI to cloud. User is not authenticated.");
			return null;
		}
	
		//Debug.Log("SendPPIToCloud...");	
		BaseCloudAction action = new SetPlayerPersistantInfo(CloudUser.instance.authenticatedUserID, inPPIInJSON);
		GameCloudManager.AddAction( action );
		return action;
	}
	
	public static BaseCloudAction BackupProgressToCloud()
	{
		// check preconditions...
		if(CloudUser.instance.isUserAuthenticated == false)	{
			Debug.LogWarning("Can't send progress to cloud. User is not authenticated.");
			return null;
		}
		
		if(Game.Instance == null || Game.Instance.PlayerPersistentInfo == null)
		{
			Debug.LogWarning("Can't send progress to cloud. There isn't valid local PPI");
			return null;
		}
		
		if(CityManager.Instance == null)
		{
			Debug.LogWarning("Can't send progress to cloud. Can't access CityManager");
			return null;
		}

		//Debug.Log("Backup in progress...");
		Game.Instance.PlayerPersistentInfo.SetAccountNameifNotExist(CloudUser.instance.primaryKey);
		string ppi      = Game.Instance.PlayerPersistentInfo.GetPlayerDataAsJsonStr();
		string progress = CityManager.Instance.GetCityProgressAsJSON();
		
		DataFileJSON jsonData = new DataFileJSON("");
		jsonData.SetString( "PlayerData"  , ppi      );
		jsonData.SetString( "GameProgress", progress );
		
		BaseCloudAction action = new SetUserProductData(CloudUser.instance.authenticatedUserID, CloudServices.PROP_ID_PROGRESS, jsonData.ToString());
		GameCloudManager.instance.StartCoroutine( GameCloudManager.instance.BackupProgressToCloud_Corutine ( action, ppi, progress) );
		
		return action;		
	}
	
	public static BaseCloudAction RetrieveProgressFromCloud(bool inSkipTimeOutCheck = false)
	{
		// check preconditions...
		if(CloudUser.instance.isUserAuthenticated == false)	{
			Debug.LogWarning("Can't send PPI to cloud. User is not authenticated.");
			return null;
		}
		
		if(inSkipTimeOutCheck == false)
		{
		 	if(Mathf.Abs((float )(instance.m_LastDownloadTime - CloudDateTime.UtcNow).TotalMinutes) < SKIP_UPDATE_TIMEOUT) 
		 		return null;		// don't retrive cloud data agian...
		}
		
		//Debug.Log("Retrieve data from cloud...");
		BaseCloudAction action = new GetUserProductData(CloudUser.instance.authenticatedUserID, CloudServices.PROP_ID_PROGRESS);
		instance.StartCoroutine( instance.RetrieveProgressFromCloud_Corutine ( action) );
		
		return action;
	}
	
	public static bool CanRestoreProgressFromCloud()
	{
		if(instance.m_CloudPPI == null || instance.m_CloudProgress == null)
			return false;
		
		return true;
	}
    
	public static bool RestoreProgressFromCloud()
	{
		if(CanRestoreProgressFromCloud() == false)
		{
			Debug.LogError("RestoreProgressFromCloud, internal error !!!");
			return false;
		}
		
		Game.Instance.PlayerPersistentInfo = instance.m_CloudPPI;
		Game.Instance.PlayerPersistentInfo.Save();
		
		GameSaveLoadUtl.SaveGameData( instance.m_CloudProgress );
		
		Game.Instance.MissionResultData.Result = MissionResult.Type.NONE;
		Game.Instance.LoadMainMenu(true);
		return true;
	}
	*/

	// =========================================================================================================================
	// === MonoBehaviour interface =============================================================================================
	void Awake()
	{
		m_FriendList = gameObject.AddComponent<FriendList>();
		m_CloudMailbox = gameObject.AddComponent<CloudMailbox>();
		m_News = gameObject.AddComponent<News>();
	}

	void Update()
	{
		UpdateCloudActions();
	}

	void OnDestroy()
	{
		//m_sInstance = null;
		PendingActions.Clear();
		ActiveAction = null;
	}

	// =========================================================================================================================
	static GameCloudManager GetInstance()
	{
		if (m_sInstance == null)
		{
			GameObject go = new GameObject("GameCloudManager");
			m_sInstance = go.AddComponent<GameCloudManager>();
			GameObject.DontDestroyOnLoad(m_sInstance);
		}

		return m_sInstance;
	}

	void UpdateCloudActions()
	{
		// TODO action time out...
		if (ActiveAction != null)
		{
			try
			{
				ActiveAction.PPIManager_Update();

				if (ActiveAction.isDone == false)
					return;

				ActiveAction = null;
			}
			catch (System.Exception e)
			{
				ActiveAction = null;
				throw e;
			}
		}

		if (PendingActions.Count > 0)
		{
			ActiveAction = PendingActions.Dequeue();
		}
	}

	// =========================================================================================================================
	/*
    private IEnumerator BackupProgressToCloud_Corutine(BaseCloudAction inAction, string inPPI, string inProgress)
    {
		GameCloudManager.AddAction( inAction );    
		while( inAction.isDone == false )
			yield return new WaitForSeconds(0.2f);
		
		if(inAction.isFailed == true)
		{
			Debug.LogError("Cloud backup failed: " + inAction.failInfo);
			yield break;
		}

		//Debug.Log("Cloud backup successfuly: " + inAction.result);
		
		// force local cloud temporary structures...
		if(false == SetCloudProgressData(inPPI, inProgress))
		{
			m_CloudPPI = null;
			m_CloudProgress = null;
			m_LastDownloadTime 	= new System.DateTime();
		}
    }
    
    private IEnumerator RetrieveProgressFromCloud_Corutine(BaseCloudAction inAction)
    {
		GameCloudManager.AddAction( inAction );    
		while( inAction.isDone == false )
			yield return new WaitForSeconds(0.2f);
		
		if(inAction.isFailed == true)
		{
			Debug.LogError("Retrieve Progress From Cloud failed: " + inAction.failInfo);
			yield break;
		}

		//Debug.Log("Progress from cloud: " + inAction.result);
		
		try
		{
			DataFileJSON jsonData = new DataFileJSON(inAction.result);
			string ppi      	  = jsonData.GetString( "PlayerData"   );
			string progress 	  = jsonData.GetString( "GameProgress" );
			
			// force local cloud temporary structures...
			if(false == SetCloudProgressData(ppi, progress))
			{
				m_CloudPPI 			= null;
				m_CloudProgress 	= null;
				m_LastDownloadTime 	= new System.DateTime();
				yield break;
			}
		}
		catch (System.Exception e)
		{
			Debug.LogError("Exception during processing progress data from Cloud. \nDetails: \n" + e.Message);
			yield break;
		}
		
		// and last step...
		//RestoreProgressFromCloud();
    }
    
    private bool SetCloudProgressData(string inPPIJSON, string inProgressJSON)
    {
		PlayerPersistantInfo cloudPPI 	  = new PlayerPersistantInfo();
		DataFileJSON 		 cityProgress = new DataFileJSON();
		
		if(string.IsNullOrEmpty(inPPIJSON) == true || cloudPPI.InitPlayerDataFromStr( inPPIJSON ) == false)
		{
			Debug.LogError("JSON data consistency error. Can't recreate PPI from JSON string");
			return false;
		}
		
		if(string.IsNullOrEmpty(inProgressJSON) == true || cityProgress.InitFromString(inProgressJSON) == false)
		{
			Debug.LogError("JSON data consistency error. City progress string is not valid");
			return false;
		}
		else
		{
			cityProgress.SetString("DEVICE_SIG",SysUtils.GetUniqueDeviceID());
		}
		
		m_CloudPPI      	= cloudPPI;
		m_CloudProgress 	= cityProgress;
		m_LastDownloadTime 	= CloudDateTime.UtcNow;
		return true;
    }
    */
}
