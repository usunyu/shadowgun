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

[AddComponentMenu("Weapons/AmmoBox")]
public class AmmoBox : GameZoneControledObject
{
	public int Ammo;
	public E_WeaponID ForWeaponType;

	public float RespawnTime = 30;

	public GameObject GameObject { get; private set; }

	public Transform Transform { get; private set; }

	bool Scale;
	float CurrentTime;

	List<GameObject> Childs = new List<GameObject>();

	public bool IsActive { get; private set; }

	protected override void Awake()
	{
		base.Awake();

		Transform = transform;
		GameObject = gameObject;

		for (int i = 0; i < Transform.childCount; i++)
			Childs.Add(Transform.GetChild(i).gameObject);

		IsActive = true;
	}

	void Start()
	{
		if (uLink.Network.isServer == false)
			NetworkView.RPC("Sync", uLink.RPCMode.Server);
	}

	void OnDestroy()
	{
		Childs.Clear();
		CancelInvoke();
	}

	[uSuite.RPC]
	public override void Reset()
	{
		CancelInvoke();

		IsActive = true;

		foreach (GameObject g in Childs)
			g.SetActive(true);

		if (uLink.Network.isServer)
			NetworkView.RPC("Reset", uLink.RPCMode.Others);
	}

	[uSuite.RPC]
	public void Disable()
	{
		IsActive = false;

		foreach (GameObject g in Childs)
			g.SetActive(false);

		if (uLink.Network.isServer)
		{
			NetworkView.RPC("Disable", uLink.RPCMode.Others);
			Invoke("Enable", RespawnTime);
		}
	}

	[uSuite.RPC]
	public void Enable()
	{
		IsActive = true;

		foreach (GameObject g in Childs)
			g.SetActive(true);

		Transform.localScale = Vector3.zero;
		Transform.rotation = Quaternion.identity;
		Scale = true;
		CurrentTime = 0;

		if (uLink.Network.isServer)
			NetworkView.RPC("Enable", uLink.RPCMode.Others);
	}

	void Update()
	{
		if (Scale)
		{
			CurrentTime += Time.deltaTime;
			if (CurrentTime >= 1)
			{
				CurrentTime = 1;
				Scale = false;
			}

			float scale = Mathfx.Hermite(0, 1, CurrentTime);

			Transform.localScale = new Vector3(scale, scale, scale);
		}
	}

	[uSuite.RPC]
	protected virtual void Sync(uLink.NetworkMessageInfo info)
	{
		if (IsActive == false)
			NetworkView.RPC("InitOnClient", info.sender);
	}

	[uSuite.RPC]
	protected virtual void InitOnClient()
	{
		IsActive = false;

		foreach (GameObject g in Childs)
			g.SetActive(false);
	}
}
