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

public static class Player
{
	public static ComponentPlayerLocal LocalInstance { get; private set; }

	public static Dictionary<uLink.NetworkPlayer, ComponentPlayer> Players { get; private set; }

	static Player()
	{
		Players = new Dictionary<uLink.NetworkPlayer, ComponentPlayer>();
	}

	public static void Register(uLink.NetworkPlayer nplayer, ComponentPlayer player)
	{
		if (player is ComponentPlayerLocal)
		{
			LocalInstance = player as ComponentPlayerLocal;
		}

		//  Debug.Log(Time.timeSinceLevelLoad + " Register - " + nplayer.id);

		if (Players.ContainsKey(nplayer))
			Players[nplayer] = player;
		else
			Players.Add(nplayer, player);
	}

	public static void UnRegister(uLink.NetworkPlayer nplayer, ComponentPlayer player)
	{
		if (Players.ContainsKey(nplayer) == false)
		{
			Debug.LogWarning(Time.timeSinceLevelLoad + "unregister - cannot find " + nplayer);
			foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Players)
				Debug.Log(pair.Key + " " + pair.Value.name);
			return;
		}

		if (Players[nplayer] != player)
		{
			Debug.LogWarning(Time.timeSinceLevelLoad + "unregister - different instance is assigned to " + nplayer + " already");
			return;
		}

		ComponentPlayerLocal local = player as ComponentPlayerLocal;
		if (local == LocalInstance)
		{
			LocalInstance = null;
		}

		//    Debug.Log(Time.timeSinceLevelLoad + " Uregister - " + nplayer.id);

		Players.Remove(nplayer);
	}

	public static ComponentPlayer GetPlayer(uLink.NetworkPlayer nplayer)
	{
		if (Players.ContainsKey(nplayer) == false)
		{
			//Debug.Log("Player::GetPlayer - error" + nplayer.id );

			foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Players)
				Debug.Log(pair.Key + " " + pair.Value.name);

			return null;
		}

		return Players[nplayer];
	}

	public static uLink.NetworkPlayer GetNetworkPlayer(AgentHuman Human)
	{
		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			if (pair.Value.Owner == Human)
			{
				return pair.Key;
			}
		}

		return uLink.NetworkPlayer.unassigned;
	}

	public static void Clear()
	{
		LocalInstance = null;
		Players.Clear();
	}
}

public class ComponentPlayer : uLink.MonoBehaviour
{
	[System.Serializable]
	public class SoundInfo
	{
		public AudioClip[] TakeAmmo = new AudioClip[0];
		public AudioClip Roll = new AudioClip();
	}

	public SoundInfo SoundSetup = new SoundInfo();

	public AudioClip SoundTakeAmmo
	{
		get
		{
			if (SoundSetup.TakeAmmo.Length == 0)
				return null;
			return SoundSetup.TakeAmmo[Random.Range(0, SoundSetup.TakeAmmo.Length)];
		}
	}

	public AudioClip SoundRoll
	{
		get { return SoundSetup.Roll; }
	}

	float HeightStand = 2.0f + 0.4f; // CAPA-HACK: make it bigger to solve problem with ray-cast vs character-controller !!!
	float HeightCover = 1.3f + 0.2f;

	// disabled - we will use full-sized capsule for collision testing during roll
	//float HeightRoll = 1.3f;

	public AgentHuman Owner { get; private set; }
	public bool InUseMode { get; protected set; }

	AgentActionCoverLeave CoverLeaveAction;
	AgentActionRoll AgentActionRoll;

	public bool IsLeavingCover
	{
		get { return CoverLeaveAction != null && CoverLeaveAction.IsActive(); }
	}

	public bool IsRolling
	{
		get { return AgentActionRoll != null && AgentActionRoll.IsActive(); }
	}

	protected const float SPEED_TEST_DELAY = 10.0f;

	protected virtual void Awake()
	{
		Owner = GetComponent<AgentHuman>();
	}

	protected virtual void OnDestroy()
	{
		SoundSetup = null;
		Destroy(gameObject);
	}

	// Use this for initialization
	protected virtual void Start()
	{
		Owner.BlackBoard.IsPlayer = true;
		Owner.BlackBoard.ActionHandler += HandleAction;

		//Debug.Log(name + " start ", this);
	}

	protected virtual void Activate()
	{
		AgentActionRoll = null;
		CoverLeaveAction = null;

		RestoreWalkingCharacterCapsule();
	}

	protected virtual void Deactivate()
	{
		//Debug.Log(name + " Deactivate ", this);

		InUseMode = false;
		AgentActionRoll = null;
		CoverLeaveAction = null;
	}

	protected virtual void Update()
	{
		if (AgentActionRoll != null && AgentActionRoll.IsActive() == false)
		{
			RestoreWalkingCharacterCapsule();

			AgentActionRoll = null;
		}

		if (CoverLeaveAction != null && CoverLeaveAction.IsActive() == false)
		{
			CoverLeaveAction = null;
		}
	}

	public void HandleAction(AgentAction a)
	{
		if (a is AgentActionRoll)
		{
			// disabled - we will use full-sized capsule for collision testing during roll
			// SetHeightOfCharacterCapsule( HeightRoll );

			AgentActionRoll = a as AgentActionRoll;

			if (SoundRoll)
				Owner.SoundPlay(SoundRoll);
		}
		else if (a is AgentActionCoverEnter)
		{
			if ((a as AgentActionCoverEnter).Cover.IsStandAllowed)
				SetHeightOfCharacterCapsule(HeightStand);
			else
				SetHeightOfCharacterCapsule(HeightCover);
			//Owner.BlackBoard.Desires.Rotation = (a as AgentActionCoverEnter).Cover.Transform.rotation;
		}
		else if (a is AgentActionCoverLeave)
		{
			RestoreWalkingCharacterCapsule();

			//Owner.BlackBoard.Desires.Rotation.SetLookRotation((a as AgentActionCoverLeave).FinalViewDirection);

			CoverLeaveAction = (AgentActionCoverLeave)a;
		}
		else if (a is AgentActionCoverFire)
		{
			if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Middle)
			{
				SetHeightOfCharacterCapsule(HeightStand);
			}
			else if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Left)
			{
				Owner.CharacterController.center = new Vector3(-0.5f, Owner.CharacterController.height*0.5f, 0);
			}
			else
			{
				Owner.CharacterController.center = new Vector3(+0.5f, Owner.CharacterController.height*0.5f, 0);
			}
		}
		else if (a is AgentActionCoverFireCancel)
		{
			if (Owner.BlackBoard.CoverPose == E_CoverPose.Crouch)
				SetHeightOfCharacterCapsule(HeightCover);
			else
				SetHeightOfCharacterCapsule(HeightStand);
		}
	}

	public void HandleBadUse()
	{
	}

	public virtual void StopMove(bool stop)
	{
		Owner.Stop(stop);

		Owner.WorldState.SetWSProperty(E_PropKey.AtTargetPos, true);
		Owner.WorldState.SetWSProperty(E_PropKey.KillTarget, false);
	}

	public virtual bool CanChangeWeapon()
	{
		if (Owner.BlackBoard.Stop || Owner.BlackBoard.BusyAction || Owner.WeaponComponent.GetCurrentWeapon().IsBusy())
			return false;
		else
			return true;
	}

	protected void ClipRotation()
	{
		if (Owner.BlackBoard.Cover)
		{
			if (IsLeavingCover)
			{
				//beny: this was causing a camera position glitch when leaving cover, so let's try to keep it where it was and let the animation control the CameraTargetPos
				//Owner.BlackBoard.Desires.Rotation.SetLookRotation( CoverLeaveAction.FinalViewDirection );
			}
			else
			{
				Vector3 coverAngles = Owner.BlackBoard.Cover.Transform.rotation.eulerAngles;

				Quaternion inverse1 = new Quaternion(-Owner.BlackBoard.Cover.Transform.rotation.x,
													 -Owner.BlackBoard.Cover.Transform.rotation.y,
													 -Owner.BlackBoard.Cover.Transform.rotation.z,
													 Owner.BlackBoard.Cover.Transform.rotation.w);
				Quaternion localC = (inverse1*Owner.BlackBoard.Cover.Transform.rotation);

				Quaternion inverse2 = new Quaternion(-Owner.Transform.rotation.x,
													 -Owner.Transform.rotation.y,
													 -Owner.Transform.rotation.z,
													 Owner.Transform.rotation.w);
				Quaternion localP = (inverse2*Owner.BlackBoard.Desires.Rotation);

				Vector3 ownerAngles = Owner.BlackBoard.Desires.Rotation.eulerAngles;

				Vector3 diff = Quaternion.FromToRotation(localP*Vector3.forward, localC*Vector3.forward).eulerAngles;

				if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Right)
				{
					if (diff.x > 180 && diff.x < 360)
					{
						if (360 - diff.x > Owner.BlackBoard.CoverSetup.RightMaxDown)
							ownerAngles.x = coverAngles.x + Owner.BlackBoard.CoverSetup.RightMaxDown;
					}
					else
					{
						if (diff.x > Owner.BlackBoard.CoverSetup.RightMaxUp)
							ownerAngles.x = coverAngles.x - Owner.BlackBoard.CoverSetup.RightMaxUp;
					}
					if (diff.y > 180 && diff.y < 360)
					{
						if (360 - diff.y > Owner.BlackBoard.CoverSetup.RightMaxRight)
							ownerAngles.y = coverAngles.y + Owner.BlackBoard.CoverSetup.RightMaxRight;
					}
					else
					{
						if (diff.y > Owner.BlackBoard.CoverSetup.RightMaxLeft)
							ownerAngles.y = coverAngles.y - Owner.BlackBoard.CoverSetup.RightMaxLeft;
					}
				}
				else if (Owner.BlackBoard.CoverPosition == E_CoverDirection.Left)
				{
					if (diff.x > 180 && diff.x < 360)
					{
						if (360 - diff.x > Owner.BlackBoard.CoverSetup.LeftMaxDown)
							ownerAngles.x = coverAngles.x + Owner.BlackBoard.CoverSetup.LeftMaxDown;
					}
					else
					{
						if (diff.x > Owner.BlackBoard.CoverSetup.LeftMaxUp)
							ownerAngles.x = coverAngles.x - Owner.BlackBoard.CoverSetup.LeftMaxUp;
					}
					if (diff.y > 180 && diff.y < 360)
					{
						if (360 - diff.y > Owner.BlackBoard.CoverSetup.LeftMaxRight)
							ownerAngles.y = coverAngles.y + Owner.BlackBoard.CoverSetup.LeftMaxRight;
					}
					else
					{
						if (diff.y > Owner.BlackBoard.CoverSetup.LeftMaxLeft)
							ownerAngles.y = coverAngles.y - Owner.BlackBoard.CoverSetup.LeftMaxLeft;
					}
				}
				else
				{
					if (diff.x > 180 && diff.x < 360)
					{
						if (360 - diff.x > Owner.BlackBoard.CoverSetup.CenterMaxDown)
							ownerAngles.x = coverAngles.x + Owner.BlackBoard.CoverSetup.CenterMaxDown;
					}
					else
					{
						if (diff.x > Owner.BlackBoard.CoverSetup.CenterMaxUp)
							ownerAngles.x = coverAngles.x - Owner.BlackBoard.CoverSetup.CenterMaxUp;
					}
					if (diff.y > 180 && diff.y < 360)
					{
						if (360 - diff.y > Owner.BlackBoard.CoverSetup.CenterMaxRight)
							ownerAngles.y = coverAngles.y + Owner.BlackBoard.CoverSetup.CenterMaxRight;
					}
					else
					{
						if (diff.y > Owner.BlackBoard.CoverSetup.CenterMaxLeft)
							ownerAngles.y = coverAngles.y - Owner.BlackBoard.CoverSetup.CenterMaxLeft;
					}
				}

				Owner.BlackBoard.Desires.Rotation.eulerAngles = ownerAngles;
			}
		}
		else
		{
			Vector3 angles = Owner.BlackBoard.Desires.Rotation.eulerAngles;

			if (angles.x >= 180)
				angles.x -= 360;

			if (angles.x < -60)
				angles.x = -60;
			else if (angles.x > 50)
				angles.x = 50;

			Owner.BlackBoard.Desires.Rotation.eulerAngles = angles;
			// Debug.Log(angles);
		}
	}

	void SetHeightOfCharacterCapsule(float height)
	{
		Owner.CharacterController.height = height;
		Owner.CharacterController.center = new Vector3(0, height*0.5f, 0);
	}

	void RestoreWalkingCharacterCapsule()
	{
		Owner.CharacterController.height = HeightStand;
		Owner.CharacterController.center = new Vector3(0, HeightStand*0.5f, 0);
	}
}
