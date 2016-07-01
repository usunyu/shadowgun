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

/***************************************************************
 * Class Name : AnimState
 * Function   :  Base state for animation engine FSM.. 
 *				
 * Created by : Marek R.
 **************************************************************/

using UnityEngine;
using System.Collections;

public class AnimState : System.Object
{
	public enum E_AnimEvent
	{
		Loop,
	};

	protected Animation Animation;
	bool m_Finished = true;
	protected AgentHuman Owner;
	protected Transform Transform;
	protected Transform RootTransform;
	protected Transform Stomach;
	float PlayInjuryTime;

	/// <summary>
	///  Public interface
	/// </summary>
	public AnimState(Animation anims, AgentHuman owner)
	{
		Animation = anims;
		Owner = owner;
		Transform = Owner.transform;
		RootTransform = Transform.Find("root");
		Stomach = Transform.Find("pelvis/stomach");
	}

	public virtual void OnActivate(AgentAction action) // state is being activated
	{
		if (Owner.debugAnims)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " Activate " + " by " + (action != null ? action.ToString() : "nothing"));

		SetFinished(false);

		Initialize(action);
	}

	public virtual void OnDeactivate() //..............deactivated
	{
		if (Owner.debugAnims)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " DeActivate");
	}

	public virtual void Reset() //..............deactivated
	{
		if (Owner.debugAnims)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " Reset");
	}

	public virtual void Release()
	{
		SetFinished(true);
	} // finish currrent action and then finish state

	public virtual bool HandleNewAction(AgentAction action)
	{
		return false;
	} // new action is comming..

	public virtual void Update()
	{
	} // update current state

	public virtual bool IsFinished()
	{
		return m_Finished;
	}

	public virtual void SetFinished(bool finished)
	{
		m_Finished = finished;
	} // state is finished or not

	public virtual void HandleAnimationEvent(E_AnimEvent animEvent)
	{
	}

	//

	protected virtual void Initialize(AgentAction action)
	{
		//if (Owner.debugAnims) Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " Initialize " + " by " + (action != null ? action.ToString() : "nothing"));
	}

	protected virtual bool Move(Vector3 velocity, bool slide = true)
	{
		if (Owner.CharacterController == null || !Owner.CharacterController.enabled)
			return false;

		//if (Owner.CharacterController.isGrounded == false)
		if (!Owner.IsServer || (false == Owner.CharacterController.isGrounded)) // server optimizations
		{
			velocity += Physics.gravity*Time.deltaTime;
		}

		if (velocity.sqrMagnitude > Mathf.Epsilon)
			Owner.CharacterController.Move(velocity);

		return true;
	}

	protected virtual bool MoveEx(Vector3 velocity)
	{
		if (Owner.CharacterController == null)
			return false;

		Vector3 old = Transform.position;

		Transform.position += Vector3.up*Time.deltaTime;

		velocity.y -= 9*Time.deltaTime;
		CollisionFlags flags = Owner.CharacterController.Move(velocity);

		if (flags == CollisionFlags.None)
		{
			RaycastHit hit;
			if (Physics.Raycast(Transform.position, -Vector3.up, out hit, 3, (int)E_LayerType.CollisonMove) == false)
			{
				Transform.position = old;
				return false;
			}
		}

		return true;
	}

	/*
    protected bool Move(Vector3 direction, float distance)
	{
        Vector3 endPos = Transform.position + direction * distance;

        if (Owner.BlackBoard.CollideWithEnemies && Mission.Instance.GameZone)
        {
            Vector3 test = endPos + direction * 0.5f;
            if (Mission.Instance.GameZone.GetNearestAliveEnemy(test, Owner.BlackBoard.CollisionRadius, true) != null)
                return false;
        }

        if (Owner.BlackBoard.CollideWithPlayer)
        {
            Vector3 test = endPos + direction * 0.25f;
            if ((test - Player.Instance.Agent.Position).sqrMagnitude < Owner.BlackBoard.CollisionRadius * Owner.BlackBoard.CollisionRadius)
                return false;
        }

		endPos.y += 1;
		RaycastHit hit;
        if (Physics.Raycast(endPos, -Vector3.up, out hit, 3, 1 << 10) == false)
			return false;

		Transform.position = hit.point;
		return true;
     }
    /*
    protected bool MoveTo(Vector3 finalPos)
    {
        RaycastHit hit;
        if (Physics.Raycast(finalPos + Vector3.up, -Vector3.up, out hit, 5, 1 << 10) == false)
            return false;

        Transform.position = hit.point;
        return true;
    }

    protected bool MoveToCollideWithEnemy(Vector3 finalPos, Vector3 direction)
    {
        if (Owner.BlackBoard.CollideWithEnemies && Mission.Instance.GameZone)
        {//FIX_ME pridat test podle sirky enemace !!!
            Vector3 test = finalPos + direction * 0.25f;
            if (Mission.Instance.GameZone.GetNearestAliveEnemy(test, Owner.BlackBoard.CollisionRadius, true) != null)
                return false;
        }

        if (Owner.BlackBoard.CollideWithPlayer)
        {
            Vector3 test = finalPos + direction * 0.25f;

            if ((test - Player.Instance.Agent.Position).sqrMagnitude < Owner.BlackBoard.CollisionRadius * Owner.BlackBoard.CollisionRadius)
                return false;
        }

        RaycastHit hit;
        if (Physics.Raycast(finalPos + Vector3.up, -Vector3.up, out hit, 5, 1 << 10) == false)
            return false;

        Transform.position = hit.point;
        return true;
    }

    protected bool JumpTo(Vector3 finalPos, Vector3 direction, float height)
    {
        MoveToCollideWithEnemy(finalPos, direction);

        Transform.position += new Vector3(0,height,0);
        return true;
    }
    */

	protected bool IsGroundThere(Vector3 pos)
	{
		return Physics.Raycast(pos + Vector3.up, -Vector3.up, 5, (int)E_LayerType.CollisionDecal);
	}

	protected void CrossFade(string anim, float fadeInTime, PlayMode mode)
	{
		if (Owner.debugAnims)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " cross fade anim: " + anim + " in " + fadeInTime + "s.");

		if (Animation.IsPlaying(anim))
			Animation.CrossFadeQueued(anim, fadeInTime, QueueMode.PlayNow);
		else
			Animation.CrossFade(anim, fadeInTime, mode);
	}

	protected void Blend(string anim, float fadeInTime)
	{
		if (Owner.debugAnims)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " blend anim: " + anim + " in " + fadeInTime + "s.");

		Animation.Blend(anim, 1, fadeInTime);
	}

	protected void Blend(string anim, float weight, float fadeInTime)
	{
		if (Owner.debugAnims)
			Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " blend anim: " + anim + " in " + fadeInTime + "s.");

		Animation.Blend(anim, weight, fadeInTime);
	}

	protected void PlayInjuryAnimation(AgentActionInjury action)
	{
		if (PlayInjuryTime > Time.timeSinceLevelLoad)
		{
			action.SetSuccess();
			return;
		}

		string animName = Owner.AnimSet.GetInjuryAnim();
		Animation[animName].blendMode = AnimationBlendMode.Additive;
		Animation[animName].layer = 0;

		//FIX IT !!!!!!
		/*if (Owner.BlackBoard.MotionType == E_MotionType.None)
        {
            Animation[animName].RemoveMixingTransform(Stomach);
        }
        else
        {
            Animation[animName].AddMixingTransform(Stomach);
        }*/

		Blend(animName, 1, 0.1f);

		PlayInjuryTime = Time.timeSinceLevelLoad + Animation[animName].length*0.35f;

		action.SetSuccess();
	}
}
