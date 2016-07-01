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
using System.Collections.Generic;

//
public struct PerkInfo
{
	public E_PerkID id;
	public float Timer; //can be used for perk stamina or so, depends on the actual perk
	public float Recharge; //can be used for perk stamina or so, depends on the actual perk
	public float Modifier; //modifies context-based value (e.g. E_PerkID.ExtendedHealth changes agent's health at the time of spawn)

	//ciphered accessor to Modifier (I do not cipher Modifier itself because I'd have to change all Modifiers in settings after its change to property)
	float m_CipheredModifier;

	public float CipheredModifier
	{
		get { return MathUtils.CipherValue(m_CipheredModifier, Cipher1); }
		set { m_CipheredModifier = MathUtils.CipherValue(value, Cipher1); }
	}

	uint m_Cipher1;

	uint Cipher1
	{
		get { return m_Cipher1; }
		set { m_Cipher1 = value; }
	}

	// -----
	public bool IsSprint()
	{
		return (id == E_PerkID.Sprint) || (id == E_PerkID.SprintII) || (id == E_PerkID.SprintIII);
	}

	// -----
	public bool IsEmpty()
	{
		return id == E_PerkID.None;
	}

	//ctor
	public PerkInfo(E_PerkID perkId)
	{
		Cipher1 = (uint)(new System.Random(Time.frameCount).Next());
		CipheredModifier = Modifier; //cipher the value

//		Debug.Log ("PERK: id=" + id + ", Modifier=" + Modifier + ", m_CipheredModifier=" + m_CipheredModifier + ", CipheredModifier=" + CipheredModifier + ", Cipher=" + Cipher1);
	}
}

[Serializable]
public class ComponentGadgets : MonoBehaviour
{
	const float INVISIBLE_BOOST_MODIFIER_DEFAULT = 0.85f;

	class BoostInfo
	{
		public ItemSettings Settings;
		public float TimeToEnd;
		public ParticleSystem Effect;
		public float CurrentPower;
	}

	protected AgentHuman Owner;
	public Transform HandL;
	public Transform HandR;

//	private		PerkInfo					CurrentPerk;
	public PerkInfo Perk; //			{get { return CurrentPerk; } protected set { CurrentPerk = value; } }	//currently equipped Perk
	public Dictionary<E_ItemID, Item> Gadgets { get; protected set; }

	List<BoostInfo> ActiveBoosts = new List<BoostInfo>();

	AgentActionUseItem AgentActionUseItem;

	void Awake()
	{
		Owner = GetComponent<AgentHuman>();
		Owner.BlackBoard.ActionHandler += HandleAction;

		Gadgets = new Dictionary<E_ItemID, Item>();
		Perk = new PerkInfo(E_PerkID.None);
	}

	void Activate()
	{
		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Owner.NetworkView.owner);

		Item g = null;
		foreach (PPIItemData d in ppi.EquipList.Items)
		{
			if (d.ID == E_ItemID.None)
				continue;

			g = new Item(ppi, Owner, d.ID);
			Gadgets.Add(d.ID, g);
		}

		if (ppi.EquipList.Perk != E_PerkID.None)
		{
			PerkSettings settings = PerkSettingsManager.Instance.Get(ppi.EquipList.Perk);
			Perk.id = settings.ID;
			Perk.Timer = settings.Timer;
			Perk.Recharge = settings.Recharge;
			Perk.CipheredModifier = settings.Modifier; //ciphered accessor; Perk.Modifier	= settings.Modifier;

//			Debug.Log ("PERK: id=" + Perk.id + ", settings.Modifier=" + settings.Modifier + ", m_CipheredModifier=" + Perk.m_CipheredModifier + ", CipheredModifier=" + Perk.CipheredModifier + ", Cipher=" + Perk.Cipher1);

			//'HACK': this is because we don't want to change the execution order (to: 1) ComponentGadgets 2) AgentHuman)
			Owner.BlackBoard.Health = Owner.BlackBoard.RealMaxHealth;
							//need to reset the Health, because it can be modified by Perks which were not available at BlackBoard.Reset() time
		}

		if (Owner.IsOwner)
			GuiHUD.Instance.CreateGadgetInventory(ppi);

		if (Owner.debugGame)
		{
			if (Gadgets.Count > 0)
			{
				string s = PPIManager.Instance.GetPPI(Owner.NetworkView.owner).Name + " : created gadgets - ";
				foreach (KeyValuePair<E_ItemID, Item> pair in Gadgets)
					s += TextDatabase.instance[pair.Value.Settings.Name];

				if (Game.Instance.GameLog)
					Debug.Log(s);
			}
			else if (Game.Instance.GameLog)
				Debug.Log(PPIManager.Instance.GetPPI(Owner.NetworkView.owner).Name + " : created no gadgets");

			if (Game.Instance.GameLog)
				Debug.Log("Current Perk: " + Perk);
		}
	}

	void LateUpdate()
	{
		if (AgentActionUseItem != null)
		{
			if (AgentActionUseItem.Throw)
			{
				Item g = GetGadget(Owner.BlackBoard.Desires.Gadget);
				if (g == null)
				{
					AgentActionUseItem.SetFailed();
					return;
				}
				g.Use(HandL.position, Owner.BlackBoard.FireDir, Owner.BlackBoard.Desires.FireTargetPlace);
				AgentActionUseItem = null;
			}
		}

		//update Gadgets
		foreach (KeyValuePair<E_ItemID, Item> pair in Gadgets)
			pair.Value.Update();

		//update Perk
		UpdatePerk();

		UpdateBoosts();
	}

	void UpdatePerk()
	{
		if (Perk.id == E_PerkID.None)
			return;

		bool active = true;
		bool countdown = false;

		switch (Perk.id)
		{
		case E_PerkID.Sprint:
		case E_PerkID.SprintII:
		case E_PerkID.SprintIII:
			if (Owner.BlackBoard.MotionType == E_MotionType.Sprint)
				countdown = true;
			else
				active = false;
			break;
		}

		if (countdown)
			Perk.Timer = Mathf.Max(0, Perk.Timer - Time.deltaTime);

		if (active)
			return;

		PerkSettings settings = PerkSettingsManager.Instance.Get(Perk.id);

		if (Perk.Timer < settings.Timer)
			Perk.Timer = Mathf.Min(Perk.Timer + settings.Recharge*Time.deltaTime, settings.Timer);
	}

	void Deactivate()
	{
		foreach (KeyValuePair<E_ItemID, Item> pair in Gadgets)
		{
			pair.Value.Destroy();
		}

		Gadgets.Clear();

		foreach (BoostInfo boost in ActiveBoosts)
		{
			if (boost.Effect)
			{
				boost.Effect.transform.parent = null;
				boost.Effect.Stop(true);
				boost.Effect = null;
			}
		}

		ActiveBoosts.Clear();
		AgentActionUseItem = null;
	}

	public void HandleAction(AgentAction action)
	{
		if (action.IsFailed())
			return;

		if (action is AgentActionUseItem)
		{
			AgentActionUseItem = action as AgentActionUseItem;
		}
		else if (action is AgentActionAttack)
		{
			OnFire();
		}
	}

	public bool IsPerkAvailableForUse(E_PerkID id)
	{
		if (Perk.id != id)
			return false;

//		Debug.Log ("Perk: " + id + ", Timer=" + Perk.Timer);

		if (id == E_PerkID.Sprint || id == E_PerkID.SprintII || id == E_PerkID.SprintIII)
			return Perk.Timer > 0;

		return true;
	}

	public bool IsGadgetAvailableForUse(E_ItemID id)
	{
		if (Gadgets.ContainsKey(id) && Gadgets[id].IsAvailableForUse())
			return true;

		return false;
	}

	public bool IsGadgetAvailableWithBehaviour(E_ItemBehaviour behaviour)
	{
		foreach (KeyValuePair<E_ItemID, Item> pair in Gadgets)
			if (pair.Value.Settings.ItemBehaviour == behaviour && pair.Value.IsAvailableForUse())
				return true;

		return false;
	}

	public Item GetGadgetAvailableWithBehaviour(E_ItemBehaviour behaviour)
	{
		foreach (KeyValuePair<E_ItemID, Item> pair in Gadgets)
			if (pair.Value.Settings.ItemBehaviour == behaviour)
				return pair.Value;

		return null;
	}

	public Item GetGadget(E_ItemID id)
	{
		if (Gadgets.ContainsKey(id))
			return Gadgets[id];

		return null;
	}

	public void RegisterUsedGadget(E_ItemID id)
	{
		if (Gadgets.ContainsKey(id))
			Gadgets[id].RegisterUsedItem();
	}

	public void RegisterLiveGadget(E_ItemID id, GameObject g)
	{
		if (Gadgets.ContainsKey(id))
			Gadgets[id].RegisterPlacedItem(g);
	}

	public void UnRegisterLiveGadget(E_ItemID id, GameObject g)
	{
		if (Gadgets.ContainsKey(id))
			Gadgets[id].UnRegisterPlacedObject(g);
	}

	[uSuite.RPC]
	protected void AskForBoost(E_ItemID gadget)
	{
		if (uLink.Network.isServer == false)
			return;

		if (Owner.BlackBoard.DontUpdate)
			return;

		Item item = Owner.GadgetsComponent.GetGadget(gadget);

		if (item == null)
			return;

		if (!item.IsAvailableForUse())
			return;

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Owner.NetworkView.owner);
		if (ppi == null)
			return;

		ActiveBoosts.Add(new BoostInfo()
		{
			Settings = item.Settings,
			TimeToEnd = Time.timeSinceLevelLoad + item.Settings.BoostTimer,
			CurrentPower = 1
		});

		ppi.ConsumableItemUsed(gadget);

		item.BoostUsed();

		Owner.NetworkView.RPC("RcvBoost", uLink.RPCMode.Others, gadget);
	}

	[uSuite.RPC]
	protected void RcvBoost(E_ItemID gadget)
	{
		if (uLink.Network.isServer == true)
			return;

		Item item = Owner.GadgetsComponent.GetGadget(gadget);

		if (item == null)
			return;

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(Owner.NetworkView.owner);
		if (ppi == null)
			return;

		BoostInfo boost = new BoostInfo()
		{
			Settings = item.Settings,
			TimeToEnd = Time.timeSinceLevelLoad + item.Settings.BoostTimer,
			CurrentPower = 1
		};

		if (item.Settings.BoostSoundOn)
			Owner.SoundPlay(item.Settings.BoostSoundOn);

		if (item.Settings.BoostEffect)
		{
			boost.Effect = GameObject.Instantiate(item.Settings.BoostEffect) as ParticleSystem;
			Transform t = boost.Effect.transform;
			t.parent = Owner.HatTarget;
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;

			if (IsBoostActive(E_ItemBoosterBehaviour.Invisible) == false)
				boost.Effect.Play(true);
		}
		else
			boost.Effect = null;

		if (item.Settings.BoosterBehaviour == E_ItemBoosterBehaviour.Invisible)
		{
			//Owner.SetInvisibleOn(item.Settings.BoostModifier);
			boost.CurrentPower = 0.0f;
			Owner.SetInvisibleOn(boost.CurrentPower);

			StopAllCoroutines();
			StartCoroutine(Coroutine_InvisibilityStart(boost, INVISIBLE_BOOST_MODIFIER_DEFAULT));

			SetBoostEffect(false);
		}

		ActiveBoosts.Add(boost);
		ppi.ConsumableItemUsed(gadget);

		item.BoostUsed();
	}

	void UpdateBoosts()
	{
		bool setupNewBoostEffects = false;
		foreach (BoostInfo boost in ActiveBoosts)
		{
			if (boost.TimeToEnd < Time.timeSinceLevelLoad)
			{
				if (boost.Effect)
				{
					boost.Effect.transform.parent = null;
					boost.Effect.Stop(true);
					boost.Effect = null;
				}

				if (boost.Settings.BoosterBehaviour == E_ItemBoosterBehaviour.Invisible)
				{
					StopAllCoroutines();
					StartCoroutine(Coroutine_InvisibilityStop(boost));

					//Owner.SetInvisibleOff();

					//setupNewBoostEffects = true;
				}

				if (boost.Settings.BoostSoundOff)
					Owner.SoundPlay(boost.Settings.BoostSoundOff);
			}
		}

		ActiveBoosts.RemoveAll(boost => boost.TimeToEnd < Time.timeSinceLevelLoad);

		if (setupNewBoostEffects)
			SetBoostEffect(true);
	}

	public bool IsBoostActive(E_ItemBoosterBehaviour boost)
	{
		return ActiveBoosts.Find(s => s.Settings.BoosterBehaviour == boost) != null;
	}

	public float GetActiveBoostPower(E_ItemBoosterBehaviour boost)
	{
		BoostInfo item = ActiveBoosts.Find(s => s.Settings.BoosterBehaviour == boost);

		if (item == null)
			return 1;

		return item.CurrentPower;
	}

	public float GetActiveBoostModifier(E_ItemBoosterBehaviour boost)
	{
		BoostInfo item = ActiveBoosts.Find(s => s.Settings.BoosterBehaviour == boost);

		if (item == null)
			return 1;

		return item.Settings.BoostModifier;
	}

	public int GetBoostGoldReward()
	{
		int gold = 0;
		foreach (BoostInfo boost in ActiveBoosts)
		{
			gold += boost.Settings.GoldReward;
		}

		return gold;
	}

	void SetBoostEffect(bool on)
	{
		foreach (BoostInfo boost in ActiveBoosts)
		{
			if (boost.Effect)
			{
				if (on)
					boost.Effect.Play(true);
				else
					boost.Effect.Stop(true);
			}
		}
	}

	void OnFire()
	{
		if (IsBoostActive(E_ItemBoosterBehaviour.Invisible))
		{
			StopAllCoroutines();

			BoostInfo boost = ActiveBoosts.Find(s => s.Settings.BoosterBehaviour == E_ItemBoosterBehaviour.Invisible);

			StartCoroutine(Coroutine_InvisibilityDump(boost));
		}
	}

	IEnumerator Coroutine_InvisibilityStart(BoostInfo boost, float target)
	{
		do
		{
			boost.CurrentPower = Math.Min(boost.CurrentPower + Time.deltaTime*0.75f, target);

			Owner.UpdateEffectAmount(boost.CurrentPower);

			yield return new WaitForEndOfFrame();
		} while (target > boost.CurrentPower);
	}

	IEnumerator Coroutine_InvisibilityDump(BoostInfo boost)
	{
		float powerTarget = Math.Max(0.25f, boost.CurrentPower - 0.5f);

		do
		{
			boost.CurrentPower = Math.Max(boost.CurrentPower - Time.deltaTime*2.0f, powerTarget);

			Owner.UpdateEffectAmount(boost.CurrentPower);

			yield return new WaitForEndOfFrame();
		} while (boost.CurrentPower > powerTarget);

		do
		{
			boost.CurrentPower = Math.Min(boost.CurrentPower + Time.deltaTime*0.5f, INVISIBLE_BOOST_MODIFIER_DEFAULT);

			Owner.UpdateEffectAmount(boost.CurrentPower);

			yield return new WaitForEndOfFrame();
		} while (boost.CurrentPower < INVISIBLE_BOOST_MODIFIER_DEFAULT);
	}

	IEnumerator Coroutine_InvisibilityStop(BoostInfo boost)
	{
		do
		{
			boost.CurrentPower = Math.Max(boost.CurrentPower - Time.deltaTime*0.75f, 0.0f);

			Owner.UpdateEffectAmount(boost.CurrentPower);

			yield return new WaitForEndOfFrame();
		} while (boost.CurrentPower > 0.0f);

		Owner.SetInvisibleOff();

		SetBoostEffect(true);
	}
}
