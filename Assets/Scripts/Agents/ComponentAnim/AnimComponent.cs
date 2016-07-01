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

public class AnimComponent : MonoBehaviour
{
	AnimFSM FSM;
	//private Transform OwnerTransform;
	Animation Animation;
	AgentHuman Owner;

	Vector3 RootPosition;

	Transform ContactPlatfrom;
	Vector3 ContactPoint = Vector3.zero;

	public AnimState CurrentAnimState
	{
		get { return FSM != null ? FSM.CurrentAnimState : null; }
	}

	public void Awake()
	{
		Owner = GetComponent<AgentHuman>();

		Animation = GetComponent<Animation>();
		//OwnerTransform = transform;

		FSM = new AnimFSM(Animation, Owner);
		FSM.Initialize();

		enabled = false;
	}

	// Use this for initialization
	void Start()
	{
		Owner.BlackBoard.ActionHandler += HandleAction;
	}

	// Update is called once per frame
	void Update()
	{
		if (ContactPlatfrom)
		{
			Vector3 move = ContactPlatfrom.position - ContactPoint;

			ContactPlatfrom = null;

			if (move.sqrMagnitude > Mathf.Epsilon)
			{
				if (Owner.CharacterController)
				{
					Owner.CharacterController.Move(move);
				}
				else
				{
					Owner.Transform.position += move;
				}
			}
		}
//		else
//		{
//			BENY:   Zaremovano, protoze CharacterController.Move zere jak svina (profilovano na Asus Prime s T3).
//					Moving platform predpokladam v MP nepouzivame, tak to snad nebude nicemu vadit. Kdyby jo, tak 
//					by bylo lepsi najit lepsi reseni nez toto.
//			
//			// Safe code for moving with moving platforms... Uncomment if problem occur again.
//			if(Owner.CharacterController && /*Owner.IsAlive == true*/ Owner.CharacterController.enabled)
//			{
//				if( !Owner.IsInCover )
//				{
//					Owner.CharacterController.Move(Vector3.up  *0.1f);
//					Owner.CharacterController.Move(Vector3.down*0.1f);
//				}
//			}
//		}

//        // HACK :: This is another one atempt to fix falling problem on lift...
//		if(Owner.CharacterController)
//		{
//        	//Owner.CharacterController.SimpleMove(Vector3.zero);
//		}

		//Profiler.BeginSample("AnimComponent.Update() : UpdateAnimStates ");
		FSM.UpdateAnimStates();
		//Profiler.EndSample();
	}

	public void HandleAction(AgentAction action)
	{
		//if(Owner.debugAnims) Debug.Log(Time.timeSinceLevelLoad + " " +  gameObject.name  + " handle action " + action.ToString());

		if (enabled == false || action.IsFailed())
			return;

		if (action is AgentActionWeaponChange)
		{
			if (Owner.IsAlive)
				StartCoroutine(SwitchWeapon(action as AgentActionWeaponChange));
			return;
		}

/*		//THROW_RUN_2
		if (action is AgentActionUseItem)
		{
			StartCoroutine( ThrowItem(action as AgentActionUseItem) );
			return;
		}
*/
		if (FSM.DoAction(action) == false)
			action.SetFailed();
	}

	public void Activate()
	{
		//if (Owner.debugAnims) Debug.Log(gameObject.name + " activated");
		enabled = true;
		FSM.Activate();

		if (null != Owner)
		{
			// update bones/meshes to current animation frame
			Owner.SampleAnimations();
		}
	}

	public void Deactivate()
	{
		Animation.Stop();
		Animation.Rewind();
		FSM.Reset();
		enabled = false;
		ContactPlatfrom = null;

		StopAllCoroutines();

		//if (Owner.debugAnims) Debug.Log(gameObject.name + "deactivated");
	}

	public void OnTeleport()
	{
		Animation.Stop();

		FSM.Reset();

		FSM.Activate();

		//if (Owner.debugAnims) Debug.Log(gameObject.name + "OnTeleport");
	}

	void HandleAnimationEvent(AnimationEvent animEvent)
	{
		FSM.CurrentAnimState.HandleAnimationEvent((AnimState.E_AnimEvent)animEvent.intParameter);
	}

	IEnumerator SwitchWeapon(AgentActionWeaponChange action)
	{
		//change FOV if desired
		if (uLink.Network.isClient && Owner.NetworkView.isMine /* && !Owner.IsInCover/**/)
		{
			float newFOV = GameCamera.Instance.DefaultFOV;
			if (Owner.IsInCover)
				newFOV *= Owner.WeaponComponent.GetWeapon(action.NewWeapon).CoverFovModificator;
			GameCamera.Instance.SetFov(newFOV, 60);
		}

		//
		string s = Owner.AnimSet.GetWeaponAnim(E_WeaponAction.Switch);
		Animation[s].layer = 3;
		Animation[s].blendMode = AnimationBlendMode.Blend;
		Animation.CrossFade(s, 0.15f, PlayMode.StopSameLayer);

		Owner.WeaponComponent.GetCurrentWeapon().SetBusy(Animation[s].length - 0.1f);
						//- 0.1f for better feeling (player feels that he should be able to shoot sooner than the animation ends, so this gives him some reaction time)

		action.SetSuccess();

		yield return new WaitForSeconds(Animation[s].length*0.36f);

		Owner.WeaponComponent.SwitchWeapons(action.NewWeapon);
	}

/*	//THROW_RUN_2
	IEnumerator ThrowItem(AgentActionUseItem action)
    {
		//init
        Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
        Owner.BlackBoard.Desires.MeleeTriggerOn  = false;
        Owner.BlackBoard.ReactOnHits = false;
        Owner.BlackBoard.BusyAction = true;
		action.Throw = true;
		
		//play anim
		string	AnimName				= Owner.AnimSet.GetGadgetAnim(Owner.BlackBoard.Desires.Gadget);
		Animation[AnimName].layer		= 3;
		Animation[AnimName].blendMode	= AnimationBlendMode.Blend;
		Animation.CrossFade(AnimName, 0.15f, PlayMode.StopSameLayer);

		ItemSettings s = ItemSettingsManager.Instance.Get(Owner.BlackBoard.Desires.Gadget);
		
		if (s && s.ItemBehaviour == E_ItemBehaviour.Place)
			Owner.SoundPlay(Owner.UseSound);
		else
			Owner.SoundPlay(Owner.ThrowSound);

		yield return new WaitForSeconds(Animation[AnimName].length * 0.7f);
		
		action.SetSuccess();
		
        Owner.BlackBoard.ReactOnHits = true;
        Owner.BlackBoard.BusyAction = false;
    }
*/
}
