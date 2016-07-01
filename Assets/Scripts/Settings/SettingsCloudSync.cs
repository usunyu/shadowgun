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
using LitJson;

public class SettingsCloudSync : MonoBehaviour
{
	enum E_State
	{
		E_STATE_INIT,
		E_STATE_LOADING_SHOP_ITEMS,
		E_STATE_FAILED,
		E_STATE_OK
	};

	static SettingsCloudSync ms_Instance;
	CloudServices.AsyncOpResult m_LoadShopItemsAsyncOp;
	E_State m_State = E_State.E_STATE_INIT;
	string m_ShopItemsJSON;
	bool m_Updated = false;

	public bool isDone
	{
		get { return m_State == E_State.E_STATE_OK || m_State == E_State.E_STATE_FAILED; }
	}

	public static SettingsCloudSync GetInstance()
	{
		if (!ms_Instance)
		{
			GameObject tmpObj = new GameObject();

			ms_Instance = tmpObj.AddComponent<SettingsCloudSync>();

			GameObject.DontDestroyOnLoad(ms_Instance);
		}

		return ms_Instance;
	}

	public static void Reset()
	{
		if (ms_Instance != null)
		{
			GameObject.DestroyImmediate(ms_Instance);

			ms_Instance = null;
		}
		GetInstance();
	}

	public bool UpdateSettingsManagersFromCloud()
	{
		if (m_State == E_State.E_STATE_OK)
		{
			if (!m_Updated)
			{
				m_Updated = true;

				return UpdateSettingsManagers();
			}
		}

		return false;
	}

	public bool KeepTrying()
	{
		return m_State != E_State.E_STATE_FAILED;
	}

	void Update()
	{
		switch (m_State)
		{
		case E_State.E_STATE_INIT:
		{
			m_LoadShopItemsAsyncOp = CloudServices.GetInstance().ProductGetParam(PPIManager.ProductID, CloudServices.PROP_ID_SHOP_ITEMS, "");

			m_State = E_State.E_STATE_LOADING_SHOP_ITEMS;
		}
			break;

		case E_State.E_STATE_LOADING_SHOP_ITEMS:
		{
			if (m_LoadShopItemsAsyncOp != null)
			{
				if (m_LoadShopItemsAsyncOp.m_Finished)
				{
					if (m_LoadShopItemsAsyncOp.m_Res)
					{
						m_ShopItemsJSON = m_LoadShopItemsAsyncOp.m_ResultDesc;

						UpdateSettingsManagers();
						//Debug.Log("Loaded shop settings from cloud");

						m_State = E_State.E_STATE_OK;
					}
					else
					{
						Debug.LogError("Unable to load shop settings from cloud");

						m_State = E_State.E_STATE_FAILED;
					}
				}
			}
			else
			{
				Debug.LogError("Unable to load shop settings from cloud");

				m_State = E_State.E_STATE_FAILED;
			}
		}
			break;
		}
	}

	bool UpdateSettingsManagers()
	{
		JsonData data = JsonMapper.ToObject(m_ShopItemsJSON);
		bool res = true;

		if (data != null && data.IsArray)
		{
			Dictionary<int, JsonData> jsonObjectsByGUID = new Dictionary<int, JsonData>();

			for (int i = 0; i < data.Count; i++)
			{
				JsonData item = data[i];
				int guid = -1;

				try
				{
					JsonData GUID = item["GUID"];

					guid = (int)GUID;
				}

				catch
				{
					continue;
				}

				MFDebugUtils.Assert(guid != -1);

				if (jsonObjectsByGUID.ContainsKey(guid))
				{
					Debug.LogError("Multiply defined shop item GUID found : " + guid);
					continue;
				}

				jsonObjectsByGUID.Add(guid, item);
			}

			res &= FundSettingsManager.Instance.UpdateFromJSONDesc(jsonObjectsByGUID);
			res &= HatSettingsManager.Instance.UpdateFromJSONDesc(jsonObjectsByGUID);
			res &= ItemSettingsManager.Instance.UpdateFromJSONDesc(jsonObjectsByGUID);
			res &= SkinSettingsManager.Instance.UpdateFromJSONDesc(jsonObjectsByGUID);
			res &= UpgradeSettingsManager.Instance.UpdateFromJSONDesc(jsonObjectsByGUID);
			res &= WeaponSettingsManager.Instance.UpdateFromJSONDesc(jsonObjectsByGUID);
			res &= TicketSettingsManager.Instance.UpdateFromJSONDesc(jsonObjectsByGUID);
			res &= AccountSettingsManager.Instance.UpdateFromJSONDesc(jsonObjectsByGUID);
			res &= BundleSettingsManager.Instance.UpdateFromJSONDesc(jsonObjectsByGUID);
		}
		else
		{
			Debug.LogError("Error parsing shop settings");
		}

		return res;
	}
};
