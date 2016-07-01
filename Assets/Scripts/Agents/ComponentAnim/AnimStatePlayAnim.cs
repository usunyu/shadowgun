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

public class AnimStatePlayAnim : AnimState
{
	AgentAction Action = null;

	float EndOfStateTime;
	string AnimName = null;

	public AnimStatePlayAnim(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);

		Owner.BlackBoard.Desires.WeaponTriggerOn = false;
		Owner.BlackBoard.Desires.WeaponTriggerUp = false;
		Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
		Owner.BlackBoard.Desires.MeleeTriggerOn = false;

		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;
	}

	public override void OnDeactivate()
	{
		Animation[AnimName].layer = 0;
		Action.SetSuccess();
		Action = null;
		base.OnDeactivate();
	}

	public override void Reset()
	{
		Animation.Stop(AnimName);

		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	public override void Update()
	{
		if (EndOfStateTime <= Time.timeSinceLevelLoad)
			Release();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		if (action is AgentActionPlayAnim)
		{
			if (Action != null)
				action.SetFailed();
		}
		return false;
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action;

		if (Action is AgentActionPlayAnim)
		{
			AnimName = (Action as AgentActionPlayAnim).AnimName;
			Animation[AnimName].layer = 5;
		}
		else if (Action is AgentActionPlayIdleAnim)
		{
			AnimName = Owner.AnimSet.GetIdleActionAnim();
		}

		if (AnimName == null)
		{
			Action.SetFailed();
			Action = null;
			Release();
			return;
		}

		// play owner anims
		CrossFade(AnimName, 0.1f, PlayMode.StopAll);
		//Animation.Play(animName, AnimationPlayMode.Stop);

		//end of state
		if (Animation[AnimName].wrapMode == WrapMode.Loop)
			EndOfStateTime = 100000 + Time.timeSinceLevelLoad;
		else
			EndOfStateTime = Animation[AnimName].length + Time.timeSinceLevelLoad - 0.3f;

		// Debug.Log(Action.AnimName + " " + EndOfStateTime );
	}

	public override void HandleAnimationEvent(E_AnimEvent animEvent)
	{
		/*Debug.Log("HandleAnimationEvent " + animEvent);
        Animation[AnimName].time = 0;
        EndOfStateTime = (Animation[AnimName].length * 0.9f) + Time.timeSinceLevelLoad;*/
	}
}
