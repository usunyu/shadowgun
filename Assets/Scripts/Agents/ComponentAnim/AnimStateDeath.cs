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

public class AnimStateDeath : AnimState
{
	Vector3 StartPosition;
	Vector3 FinalPosition;
	Quaternion FinalRotation;
	Quaternion StartRotation;

	float RotationProgress;
	float MoveTime;
	float CurrentMoveTime;

	AgentActionDeath Action = null;

	public AnimStateDeath(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);

		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;

		//Time.timeScale = .1f;
	}

	public override void Update()
	{
	}

	public override void Reset()
	{
		Action.SetSuccess();
//		SetFinished(true);
	}

	public override void Release()
	{
//		SetFinished(true);
	}

	public override bool HandleNewAction(AgentAction action)
	{
//		Debug.Log ("DIE: AnimStateDeath.HandleNewAction(), agent=" + Owner.name + ", action.Type=" + action.Type + ", time=" + Time.timeSinceLevelLoad);

		//FAIL any action which comes during death.
		//This is to fix the ocassional 'stand up' after death.
		action.SetFailed();
		return true;

/*		//
		if (action is AgentActionDeath)
		{
			action.SetFailed();
			return true;
		}
		
		return false;
*/
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionDeath;

		if (!Owner.RagdollRoot || uLink.Network.isServer)
		{
//			Debug.Log ("DIE: AnimStateDeath.InitializeAnimation(), agent=" + Owner.name + ", time=" + Time.timeSinceLevelLoad);

			InitializeAnimation();
		}
		else
		{
//			Debug.Log ("DIE: AnimStateDeath.InitializeRagdoll(), agent=" + Owner.name + ", time=" + Time.timeSinceLevelLoad);

			InitializeRagdoll();
		}
	}

	//Initialize ragdoll animation
	void InitializeAnimation()
	{
		// play owner anims
		string animName = Owner.AnimSet.GetDeathAnim();

		CrossFade(animName, 0.1f, PlayMode.StopAll);

		// Debug.Log(Action.AnimName + " " + EndOfStateTime );
		Owner.BlackBoard.MotionType = E_MotionType.None;

		//Owner.Invoke("SpawnBlood", Animation[animName].length);
		Owner.BlackBoard.MotionType = E_MotionType.Death;

		Owner.DisableCollisions();
	}

	//Initialize ragdoll Death
	void InitializeRagdoll()
	{
		Animation.Stop();

//		Owner.Dissolve( 2.5f );								//Fadeout() is called by Die() in AgentHuman

		//disable collision with player
		Owner.ToggleCollisions(false, true);

		//enable ragdoll physics
		Owner.EnableRagdoll(true);

		//physical impulse
		Vector3 impulse;

		impulse = Action.Impulse + (Vector3.up*Action.Impulse.magnitude*0.5f);

		// Limit the impulse. We really do not want the the dead bodies to fly dozens of meters.
		const float maxRagdolImpusle = 5000.0f;
		if (impulse.magnitude > maxRagdolImpusle)
		{
			impulse = impulse.normalized * maxRagdolImpusle;
		}

		Owner.RigidBodyForce.AddForce(impulse, ForceMode.Force);

		Owner.BlackBoard.MotionType = E_MotionType.Death;

		//Debug.Log ("AnimStateDeath, Action.Impulse=" + Action.Impulse.ToString("F5") + ", Total Impulse=" + impulse.ToString("F5") + ", impulse.magnitude=" + impulse.magnitude);
	}
}
