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

public class AnimStateIdle : AnimState
{
	float TimeToFinishWeaponAction;
	AgentAction WeaponAction;

	float TimeToFinishRotateAction;
	AgentActionRotate RotateAction;

	string AnimNameUp;
	string AnimNameDown;
	string AnimNameBase;

	float BlendUp;
	float BlendDown;
	//float PrevBlendUp;
	//float PrevBlendDown;

	float BlendUniTarget;
	float BlendUniCurrent;

	bool InstantBlend = true;

	public AnimStateIdle(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void OnActivate(AgentAction action)
	{
//		Debug.Log ("AnimStateIdle.OnActivate(), time=" + Time.timeSinceLevelLoad);

		base.OnActivate(action);
	}

	public override void OnDeactivate()
	{
		if (WeaponAction != null)
		{
			WeaponAction.SetSuccess();
			WeaponAction = null;
		}

		if (RotateAction != null)
		{
			RotateAction.SetSuccess();
			RotateAction = null;
		}

		base.OnDeactivate();
	}

	public override void Reset()
	{
		if (Owner.BlackBoard.AimAnimationsEnabled)
		{
			Animation[AnimNameUp].weight = 0;
			Animation[AnimNameDown].weight = 0;
			Animation.Stop(AnimNameUp);
			Animation.Stop(AnimNameDown);
		}

		if (WeaponAction != null)
		{
			WeaponAction.SetSuccess();
			WeaponAction = null;
		}

		if (RotateAction != null)
		{
			RotateAction.SetSuccess();
			RotateAction = null;
		}

		InstantBlend = true;

		base.Reset();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		//if (m_Human.PlayerProperty != null)
		//if(Owner.debugAnims) Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " - action " + action.ToString());
		if (action is AgentActionIdle)
		{
			action.SetFailed();
			return true;
		}
		else if (action is AgentActionAttack)
		{
			if (null != Owner.AnimSet)
			{
				string s = Owner.AnimSet.GetWeaponAnim(E_WeaponAction.Fire);

				if (null != s)
				{
					AnimationState state = Animation[s];

					if (null != state)
					{
						TimeToFinishWeaponAction = Time.timeSinceLevelLoad + state.length*0.5f;

						state.layer = 2;
						state.blendMode = AnimationBlendMode.Additive;

						if (Animation.IsPlaying(s))
						{
							//Debug.Log(Time.timeSinceLevelLoad + " " + s + " rewind " + Animation[s].length + " " + Animation[s].time);
							state.time = 0;
						}
						else
						{
							//Debug.Log(Time.timeSinceLevelLoad + " " + s + " fade " + Animation[s].length + " " + Animation[s].time);
							Blend(s, 0.05f);
						}
					}
				}
			}

			if (WeaponAction != null)
			{
				WeaponAction.SetSuccess();
			}

			WeaponAction = action;

			return true;
		}
		else if (action is AgentActionInjury)
		{
			PlayInjuryAnimation(action as AgentActionInjury);
			return true;
		}
		else if (action is AgentActionReload)
		{
			if (null != Owner.AnimSet)
			{
				string s = Owner.AnimSet.GetWeaponAnim(E_WeaponAction.Reload);

				if (null != s)
				{
					AnimationState state = Animation[s];

					if (null != state)
					{
						state.layer = 2;
						state.blendMode = AnimationBlendMode.Blend;

						Blend(s, 0.2f);

						TimeToFinishWeaponAction = Time.timeSinceLevelLoad + state.length - 0.3f;
					}
				}

				action.SetSuccess();

				WeaponAction = action;

				//			PrevBlendUp *= 0.25f;		//this is to minimize the quick blend to aim after reload
				//			PrevBlendDown *= 0.25f;
			}

			return true;
		}
		else if (action is AgentActionRotate)
		{
			RotateAction = action as AgentActionRotate;

			if (null != Owner.AnimSet)
			{
				string s = Owner.AnimSet.GetRotateAnim(RotateAction.Rotation);

				if (s != null && Animation.IsPlaying(s) == false)
				{
					/*				if ( Animation[s] == null )
					{
						Debug.Log ("Animation.Length=" + Animation.GetClipCount() + ", agent=" + Owner.name);
						
						foreach ( AnimationClip clip in Animation )
							Debug.Log ("clip=" + clip.name );
					}
	*/
					AnimationState state = Animation[s];

					if (null != state)
					{
						state.blendMode = AnimationBlendMode.Additive;

						state.layer = 1;

						TimeToFinishRotateAction = Time.timeSinceLevelLoad + state.length + 0.3f;

						Blend(s, 0.1f);
					}
				}
			}
		}
		return false;
	}

	public override void Update()
	{
		if (WeaponAction != null && TimeToFinishWeaponAction < Time.timeSinceLevelLoad)
		{
			WeaponAction.SetSuccess();
			WeaponAction = null;
			//Debug.Log(Owner.AnimSet.GetIdleAnim(Owner.BlackBoard.WeaponSelected, Owner.BlackBoard.WeaponState).ToString());
			//PlayIdleAnim();
			CrossFade(Owner.AnimSet.GetIdleAnim(), 0.4f, PlayMode.StopSameLayer);
		}

		if (RotateAction != null && TimeToFinishRotateAction < Time.timeSinceLevelLoad)
		{
			RotateAction.SetSuccess();
			RotateAction = null;
		}

		//fall down if not grounded
		if (Owner.IsInCover == false)
			Move(Vector3.zero, false);

		PlayIdleAnim();

		if (Owner.BlackBoard.AimAnimationsEnabled && Owner.IsInCover == false)
		{
			UpdateBlendValues();
			UpdateBlendedAnims();
		}
	}

	void PlayIdleAnim(bool bInstant = false)
	{
		AnimNameBase = Owner.AnimSet.GetIdleAnim();

		if (Animation.IsPlaying(AnimNameBase) == false)
		{
			if (Owner.IsInCover)
				CrossFade(AnimNameBase, bInstant ? 0 : 0.2f, PlayMode.StopSameLayer);
			else
				CrossFade(AnimNameBase, bInstant ? 0 : 0.25f, PlayMode.StopSameLayer);
		}

		Owner.SetDominantAnimName(AnimNameBase);
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;

		if (WeaponAction == null)
			PlayIdleAnim(InstantBlend);

		InstantBlend = false;

		if (action != null)
			action.SetSuccess();

		if (Owner.BlackBoard.AimAnimationsEnabled && Owner.IsInCover == false)
		{
			AnimNameUp = Owner.AnimSet.GetAimAnim(E_AimDirection.Up, E_CoverPose.None, E_CoverDirection.Unknown);
			AnimNameDown = Owner.AnimSet.GetAimAnim(E_AimDirection.Down, E_CoverPose.None, E_CoverDirection.Unknown);

			Animation[AnimNameUp].wrapMode = WrapMode.ClampForever;
			Animation[AnimNameDown].wrapMode = WrapMode.ClampForever;

			Animation[AnimNameUp].blendMode = AnimationBlendMode.Additive;
			Animation[AnimNameUp].layer = 1;

			Animation[AnimNameDown].blendMode = AnimationBlendMode.Additive;
			Animation[AnimNameDown].layer = 1;

			//Debug.Log("anim up " + AnimNameUp + " down " + AnimNameDown);

			UpdateBlendValues();

			Animation[AnimNameUp].time = 0.0333f;
			Animation[AnimNameDown].time = 0.0333f;

			Animation[AnimNameUp].weight = BlendUp;
			Animation[AnimNameDown].weight = BlendDown;

			Animation.Blend(AnimNameUp, BlendUp, 0);
			Animation.Blend(AnimNameDown, BlendDown, 0);
		}
	}

	void UpdateBlendValues()
	{
		if (Owner.IsInCover)
		{
			BlendUp = 0;
			BlendDown = 0;
			//PrevBlendUp = 0;
			//PrevBlendDown = 0;
			return;
		}

		if (WeaponAction != null && WeaponAction is AgentActionReload) //this is to avoid mixing-in additive AimU/AimD while reloading
		{
			BlendUp = 0;
			BlendDown = 0;
			BlendUniTarget = 0;

			//Debug.Log ("PrevBlendUp=" + PrevBlendUp + ", PrevBlendDown=" + PrevBlendDown);
		}
		else
		{
			Quaternion r = Owner.BlackBoard.Desires.Rotation;
			r.SetLookRotation(Owner.BlackBoard.FireDir);
			// more precize version
			//r.SetLookRotation( ( Owner.BlackBoard.Desires.FireTargetPlace - Owner.ChestPosition ).normalized );
			//Vector3 bestAngles = Owner.BlackBoard.Desires.Rotation.eulerAngles;
			Vector3 bestAngles = r.eulerAngles;
			Vector3 currentAngles = Owner.Transform.rotation.eulerAngles;

			Vector3 diff = (bestAngles - currentAngles);

			if (diff.x > 180)
				diff.x -= 360;
			else if (diff.x < -180)
				diff.x += 360;

			float blendUp = diff.x > 0 ? 0 : -diff.x;
			float blendDown = diff.x < 0 ? 0 : diff.x;

			//Debug.Log(diff + " " + blendUp + " " + blendDown);

			BlendUp = Mathf.Min(1, blendUp/70.0f); // *Animation[AnimNameBase].weight;
			BlendDown = Mathf.Min(1, blendDown/50.0f); // *Animation[AnimNameBase].weight;

			// > 0 means down
			// < 0 means up
			BlendUniTarget = diff.x > 0 ? Mathf.Min(1, diff.x/50.0f) : Mathf.Max(-1, diff.x/70.0f);
		}
	}

	void UpdateUpDownAnims(string AnimToPlay, string AnimToStop, float Weight)
	{
		if (!Animation.IsPlaying(AnimToPlay))
		{
			Animation.Play(AnimToPlay);
		}

		if (Animation.IsPlaying(AnimToStop))
		{
			Animation.Stop(AnimToStop);
		}

		Animation[AnimToPlay].weight = Weight;
	}

	void UpdateBlendedAnims()
	{
		BlendUniCurrent = Mathf.Lerp(BlendUniCurrent, BlendUniTarget, Time.deltaTime*10.0f);

		if (BlendUniCurrent > 0)
		{
			UpdateUpDownAnims(AnimNameDown, AnimNameUp, BlendUniCurrent); // down
		}
		else
		{
			UpdateUpDownAnims(AnimNameUp, AnimNameDown, -BlendUniCurrent); // up
		}

		//float	weight;
		//bool	playing;

/*		Animation[AnimNameUp].weight = Mathf.Lerp(PrevBlendUp, BlendUp, Time.deltaTime * speed);
		Animation[AnimNameDown].weight = Mathf.Lerp(PrevBlendDown, BlendDown, Time.deltaTime * speed);
		
		PrevBlendUp 	= BlendUp;
		PrevBlendDown 	= BlendDown;
*/

		/*
		//up
		if (WeaponAction != null && WeaponAction is AgentActionReload)	//this is to avoid mixing-in additive AimU/AimD while reloading
		{
			weight = 0;
		}
		else
		{
			if( PrevBlendDown == 0 )
			{
				//weight		= BlendUp > Mathf.Epsilon ? Mathf.Lerp(PrevBlendUp, BlendUp, Time.deltaTime * speed) : 0;
				weight		= Mathf.Lerp(PrevBlendUp, BlendUp, Time.deltaTime * speed);
				if( Mathf.Abs( weight - BlendUp ) < 0.01f )
				{
					weight = BlendUp;
				}
				PrevBlendUp	= weight;
			}
			else
			{
				weight = 0;
			}
		}	
		
		playing	= Animation.IsPlaying(AnimNameUp);
		if (weight > Mathf.Epsilon)
		{
			if (!playing)
				//Animation.Blend(AnimNameUp, BlendUp, Mathf.Epsilon);
				Animation.Play(AnimNameUp);//, BlendUp, Mathf.Epsilon);
			//else
				Animation[AnimNameUp].weight = weight;
		}
		else if (playing)
		{
			Animation.Stop(AnimNameUp);
		}
		
		//down
		if (WeaponAction != null && WeaponAction is AgentActionReload)	//this is to avoid mixing-in additive AimU/AimD while reloading
		{
			weight = 0;
		}
		else
		{
			if( PrevBlendUp == 0 )
			{
				//weight			= BlendDown > Mathf.Epsilon ? Mathf.Lerp(PrevBlendDown, BlendDown, Time.deltaTime * speed) : 0;
				weight			= Mathf.Lerp(PrevBlendDown, BlendDown, Time.deltaTime * speed);
				
				if( Mathf.Abs( weight - BlendDown ) < 0.01f )
				{
					weight = BlendDown;
				}
				PrevBlendDown	= weight;
			}
			else
			{
				weight = 0;
			}
		}	
		
		playing	= Animation.IsPlaying(AnimNameDown);
		if (weight > Mathf.Epsilon)
		{
			if (!playing)
				//Animation.Blend(AnimNameDown, BlendDown, Mathf.Epsilon);
				Animation.Play(AnimNameDown);
			//else
				Animation[AnimNameDown].weight = weight;
		}
		else if (playing)
		{
			Animation.Stop(AnimNameDown);
		}

//		Debug.Log ("Update: BlendUp=" + BlendUp + ", PrevBlendUp=" + PrevBlendUp + ", BlendDown=" + BlendDown + ", PrevBlendDown=" + PrevBlendDown);
		
//		if (Owner.IsPlayer)
//			Debug.Log("Idle Blend: " + " up " + Animation[AnimNameUp].weight + " down " + Animation[AnimNameDown].weight);
		*/
	}
}
