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
using System.Collections.Generic;

public struct InAppInitRequest
{
	public string ProductId;
	public string ProductName;
	public InAppProductType ProductType;
}

public class InAppPurchaseMgr : MonoBehaviour
{
	public const int STATE_INIT = 0;
	public const int STATE_BILLING = 100;
	public const int STATE_FAILURE = 200;
	public const int STATE_SERVICE_UNAVAILABLE = 201;

	static InAppPurchaseMgr _Instance;
	InAppInventory _Inventory;

	static InAppPurchaseMgr CreateInstance(GameObject go)
	{
		return go.AddComponent<InAppPurchaseMgr>();
	}

	public static InAppPurchaseMgr Instance
	{
		get
		{
			if (_Instance == null)
			{
				GameObject go = new GameObject("InAppPurchaseMgr");
				_Instance = CreateInstance(go);
				GameObject.DontDestroyOnLoad(_Instance);

				_Instance.Inventory = new InAppInventory();
				_Instance.CurrentState = STATE_SERVICE_UNAVAILABLE;
			}

			return _Instance;
		}
	}

	public void Init(InAppInitRequest[] requestedProducts)
	{
	}

	public InAppAsyncOpResult RequestPurchaseProduct(InAppProduct product)
	{
		// TODO: implement minimum placeholder functionality (return an request which fails in few tens of seconds
		return null;
	}

	public int CurrentState { get; protected set; }

	public InAppInventory Inventory
	{
		get { return _Inventory; }
		private set { _Inventory = value; }
	}

	protected void Awake()
	{
	}

	protected void Update()
	{
	}
}
