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

public class AnimStateTeleport : AnimState
{
	AgentActionTeleport Action;

	public AnimStateTeleport(Animation anims, AgentHuman owner)
					: base(anims, owner)
	{
	}

	public override void Release()
	{
		//if (m_Human.PlayerProperty != null)
		//Debug.Log(Time.timeSinceLevelLoad + " " + this.ToString() + " - release");

		SetFinished(true);
	}

	public override void OnActivate(AgentAction action)
	{
		base.OnActivate(action);
	}

	public override void OnDeactivate()
	{
		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;

		Action.SetSuccess();
		Action = null;

		base.OnDeactivate();
	}

	public override void Reset()
	{
		Action.SetSuccess();
		Action = null;

		base.Reset();
	}

	public override bool HandleNewAction(AgentAction action)
	{
		return false;
	}

	public override void Update()
	{
	}

	protected override void Initialize(AgentAction action)
	{
		base.Initialize(action);

		Action = action as AgentActionTeleport;

		Owner.BlackBoard.MotionType = E_MotionType.None;
		Owner.BlackBoard.MoveDir = Vector3.zero;
		Owner.BlackBoard.Speed = 0;

		Owner.StartCoroutine(Teleport());
	}

	IEnumerator Teleport()
	{
		// Time.timeScale = 0.2f;

		Owner.Stop(true);
		Owner.BlackBoard.Invulnerable = true;
		Owner.BlackBoard.ReactOnHits = false;

		Owner.SensorsComponent.DeactivateAllSensors();

		// Debug.Log(Time.timeSinceLevelLoad + " starting teleport");

		string s = Owner.AnimSet.GetTeleportAnim(AnimSet.E_TeleportAnim.In);
		CrossFade(s, 0.2f, PlayMode.StopAll);

		yield return new WaitForSeconds(Animation[s].length - 2);

		Owner.TeleportFadeOut();

		Owner.SoundPlay(Owner.TeleportSound);

		//Debug.Log(Time.timeSinceLevelLoad + " starting fade out");

		//  SpawnTeleportFX();

		yield return new WaitForSeconds(0.7f);

		//Debug.Log(Time.timeSinceLevelLoad + " agent teleport");

		Owner.Teleport(Action.Destination - Vector3.up*1000, Action.Rotation);

		yield return new WaitForSeconds(Random.Range(1.1f, 3.5f));

		Owner.Teleport(Action.Destination, Action.Rotation);

		Owner.TeleportFadeIn();
		Owner.SoundPlay(Owner.TeleportSound);

		//Debug.Log(Time.timeSinceLevelLoad + " fade out");

		s = Owner.AnimSet.GetTeleportAnim(AnimSet.E_TeleportAnim.Out);

		CrossFade(s, 0.0f, PlayMode.StopAll);

		yield return new WaitForSeconds(1);

		//Debug.Log(Time.timeSinceLevelLoad + " end of teleport");

		Owner.SensorsComponent.ActivateAllSensors();

		Owner.BlackBoard.Invulnerable = false;
		Owner.BlackBoard.ReactOnHits = true;
		Owner.Stop(false);

		//   Time.timeScale = 1;
		Release();
	}

	void SpawnTeleportFX()
	{
		MFExplosionPostFX.S_WaveParams waveParams;

		Vector3 pos = Owner.transform.position;
		float wrldDist = (pos - Camera.main.transform.position).magnitude;
		float att = Mathf.Min(wrldDist/30, 1);

		waveParams.m_Amplitude = 1;
		waveParams.m_Duration = 1.5f;
		waveParams.m_Freq = 10;
		waveParams.m_Speed = 1.3f;
		waveParams.m_Radius = Mathf.Lerp(0.025f, 0.2f, att);
		waveParams.m_Delay = 0.0f;

		CamExplosionFXMgr.Instance.SpawnExplosionWaveFX(pos, waveParams);
	}
}
