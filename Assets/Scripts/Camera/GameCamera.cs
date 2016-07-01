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

//#define CAMERA_DEBUG_DRAW

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameCamera : MonoBehaviour
{
	public enum E_State
	{
		None,
		Player,
		Spectator_Free,
		Spectator_FollowPlayer
	}

	public static GameCamera Instance;

	E_State State;
	CameraBehaviour CameraBehaviour;

	Animation Animation;
	Animation ParentAnimation;
	Transform CameraTransform;
	public Camera MainCamera;

	float DisabledTime = 0;

	public float DefaultFOV { get; private set; }
	float ChangeFOVSpeed;
	Collider IgnoredCollider;
	float DesiredFov;

	public float DesiredCameraFov
	{
		get { return DesiredFov; }
	}

	public Vector3 CameraForward
	{
		get { return CameraTransform.forward; }
	}

	public Vector3 CameraRight
	{
		get { return CameraTransform.right; }
	}

	public Vector3 CameraUp
	{
		get { return CameraTransform.up; }
	}

	public Vector3 CameraPosition
	{
		get { return CameraTransform.position; }
	}

	float PrevDist; //distance between camera position and its target position in previous frame
	Vector3 PrevPos;
	Quaternion PrevRot;

	//
	void Awake()
	{
		Instance = this;
		MainCamera = Camera.main;
		Animation = MainCamera.GetComponent<Animation>();
		ParentAnimation = GetComponent<Animation>();
		CameraTransform = transform;

		if (Screen.width == 1024 && Screen.height == 768)
			MainCamera.fieldOfView = 70;
		else
			MainCamera.fieldOfView = 55;

		DesiredFov = DefaultFOV = MainCamera.fieldOfView;
	}

	void Start()
	{
		DisabledTime = 0;

		ChangeStateInternal(uLink.Network.isServer ? E_State.Player : E_State.Spectator_Free);
	}

	void UpdateCamera()
	{
		if (State == E_State.Spectator_Free)
		{
			// disable camera effects
			BloodManagerSetNormalizedHealth();

			return;
		}

		if (State == E_State.Spectator_FollowPlayer)
		{
			if (SpectatorCamera.Spectator_CanFollowPlayer())
			{
				if (null == CameraBehaviour)
				{
					AgentHuman Owner = SpectatorCamera.SelectNextAgentToFollow();

					if (null != Owner)
					{
						CameraBehaviour = Owner.GetComponent<CameraBehaviour>();
						IgnoredCollider = Owner.CharacterController;

						if (CameraBehaviour)
						{
							Transform desiredTransform = CameraBehaviour.GetDesiredCameraTransform();

							if (desiredTransform)
							{
								PrevDist = (desiredTransform.position - CameraBehaviour.CameraOrigin.position).magnitude;
							}
						}
					}
				}
				else
				{
					// already watching one
					if (!SpectatorCamera.UpdateFollowPlayer())
					{
						GameCamera.ChangeMode(GameCamera.E_State.Spectator_FollowPlayer);
					}
				}
			}
			else
			{
				ChangeStateInternal(E_State.Spectator_Free);
			}
		}

		if (DisabledTime >= Time.timeSinceLevelLoad)
			return;

		if (Time.deltaTime == 0 || !CameraBehaviour)
			return;

		//Collide camera
//		CollideCamera();
		CollideCamera5();
	}

	//collide camera and place it to the best position
	//Jednoduse nastav kameru 10 cm pred kolizni bod. Zadne interpolace ani serepeticky. Samozrejme prudce meni polohu v momente kolize.
	void CollideCamera()
	{
		Transform desiredTransform = CameraBehaviour.GetDesiredCameraTransform();

		//   Debug.Log(" distance " + (CameraTransform.position - desiredTransform.position).magnitude);
		if (desiredTransform)
		{
			const float radius = 0.12f;
			Vector3 FinalPos;
			Vector3 dir = desiredTransform.position - CameraBehaviour.CameraOrigin.position;
			LayerMask mask = ~(ObjectLayerMask.Ragdoll | ObjectLayerMask.IgnoreRayCast);
			RaycastHit[] hits = Physics.SphereCastAll(CameraBehaviour.CameraOrigin.position, radius, dir.normalized, dir.magnitude, mask);

			//sort by distance
			if (hits.Length > 1)
				System.Array.Sort(hits, CollisionUtils.CompareHits);

			//CameraTransform.position = desiredTransform.position;
			FinalPos = desiredTransform.position;
			MainCamera.nearClipPlane = 0.2f; //was 0.3f;

			foreach (RaycastHit hit in hits)
			{
				if (hit.collider.gameObject.layer == InteractionObject.UseLayer)
					continue;

				if (hit.collider.isTrigger)
					continue;

				if (hit.collider == IgnoredCollider)
					continue;

//				CameraTransform.position = hit.point + hit.normal * 0.3f;
				FinalPos = hit.point + hit.normal*0.1f;
								//was 0.3f; changed to 0.1f to help fix the camera-player intersection (when player was running backwards against wall)
				MainCamera.nearClipPlane = 0.2f; //0.02f;		//was 0.05f; the new value needs testing on devices (check z-fight)

/*				Debug.DrawLine(desiredTransform.position, CameraTransform.position, Color.white, 10);
				DebugDraw.Diamond(Color.green, 0.03f, desiredTransform.position);
				DebugDraw.Diamond(Color.red,   0.03f, hit.point);
				DebugDraw.Diamond(Color.yellow, 0.03f, FinalPos);
				DebugDraw.Diamond(Color.magenta, 0.03f, CameraBehaviour.CameraOrigin.position);
				
				Vector3 pos = CameraBehaviour.CameraOrigin.position + dir.normalized * (hit.distance);
				DebugDraw.Sphere(Color.gray, radius, pos);
				DebugDraw.Diamond(Color.blue, 0.03f, pos);
*/
				break; //need to care just about the nearest valid hit
			}

//			CameraTransform.position = Vector3.Lerp(CameraTransform.position, FinalPos, 15 * Time.deltaTime);
			CameraTransform.position = FinalPos;

			//CameraTransform.rotation = Quaternion.Lerp(CameraTransform.rotation, desiredTransform.rotation, 15.0f * Time.deltaTime);
			CameraTransform.rotation = desiredTransform.rotation;

			UpdateLocalPlayerInstance();
		}
	}

	//collide camera and place it to the best position
	void CollideCamera5()
	{
		//
#if CAMERA_DEBUG_DRAW			
		Color col;
#endif

		Transform desiredTransform = CameraBehaviour.GetDesiredCameraTransform();
		if (desiredTransform)
		{
			Vector3 desiredPosition = desiredTransform.position;
			Quaternion desiredRotation = desiredTransform.rotation;

			float dif = (desiredPosition - PrevPos).sqrMagnitude;

#if !CAMERA_DEBUG_DRAW //disable the optimization when Debug Draw is on
			//exit if the camera didn't move or rotate
			if (dif < 0.0001f &&
				Mathf.Approximately(PrevRot.x, desiredRotation.x) &&
				Mathf.Approximately(PrevRot.y, desiredRotation.y) &&
				Mathf.Approximately(PrevRot.z, desiredRotation.z) &&
				Mathf.Approximately(PrevRot.w, desiredRotation.w))
				return;
#endif

			//blend the transform (this is useful especially when the CameraState changes or when the camera moves very fast)
			if (dif > 0.1f)
			{
				desiredPosition = Vector3.Lerp(PrevPos, desiredPosition, Time.deltaTime*20);
				desiredRotation = Quaternion.Slerp(PrevRot, desiredRotation, Time.deltaTime*20);
			}

			PrevPos = desiredPosition;
			PrevRot = desiredRotation;

			Vector3 FinalPos;

			//pokud je jiny CameraState nez CameraState3RD, tak muzeme nasledujici testy preskocit a jen nastavit CameraTransform.position a CameraTransform.rotation a zavolat UpdateLocalPlayerInstance();
//			if ( (CameraBehaviour.State is CameraState3RD) || (CameraBehaviour.State is CameraStateCover) || (CameraBehaviour.State is CameraStateDeath) )
			{
				LayerMask mask = ObjectLayerMask.Default | ObjectLayerMask.PhysicsMetal;
								//~( ObjectLayerMask.Ragdoll | ObjectLayerMask.IgnoreRayCast | ObjectLayerMask.PhysicBody );
				const float minNear = 0.02f; //0.05f still penetrates walls while rolling
				const float minRadius = 0.12f; //0.1f
				const float maxRadius = 0.3f; //with 0.5f the camera collides too often when player runs close to obstacles
				bool IsRolling = CameraBehaviour.Owner.PlayerComponent.IsRolling;
				bool IsAlive = CameraBehaviour.Owner.IsAlive;
				float radius = IsRolling ? minRadius : maxRadius; //in dodge, use minRadius for collisions

				//for testing collision between Head and TargetPos ("CameraTargetDir" node)
				Vector3 HeadPos = CameraBehaviour.Owner.EyePosition;
				Vector3 TargetPos = CameraBehaviour.CameraOrigin.position;
				Vector3 headDir = TargetPos - HeadPos;

				//shift the HeadPos to not allow the camera to move into player's head
				HeadPos += headDir*0.5f; //middle point between head and target
				headDir = TargetPos - HeadPos;

				float headLength = headDir.magnitude;
				RaycastHit headHit;

#if CAMERA_DEBUG_DRAW			
				DebugDraw.Line(Color.magenta, HeadPos, TargetPos + headDir.normalized * radius);		//c
				DebugDraw.Diamond(Color.magenta, 0.03f, TargetPos);
				DebugDraw.Diamond(Color.green, 0.03f, desiredPosition);
#endif

				//check whether the TargetPos is behind collision or not (it is controlled by animation and usually is outside the player's capsule collider)
				//this happens when players 'shoulder' (i.e. the "CameraTargetDir") gets behind a wall
				if (Physics.Raycast(HeadPos, headDir, out headHit, headLength + radius, mask))
				{
					float m = Mathf.Max(0, headHit.distance - radius);
					TargetPos = HeadPos + headDir.normalized*m; //move the TargetPos along the line to a safe place

					if (headHit.distance < radius)
						radius = Mathf.Max(minRadius, headHit.distance - 0.001f); //change the radius based on the collision distance

#if CAMERA_DEBUG_DRAW			
					DebugDraw.Diamond(Color.cyan, 0.03f, headHit.point);
					DebugDraw.Diamond(Color.white, 0.03f, TargetPos);		//new TargetPos
					
					DebugDraw.Line(Color.green, headHit.point, headHit.point + headHit.normal * (radius + 0.01f) );	//b
					DebugDraw.Line(Color.yellow, headHit.point + headHit.normal * (radius + 0.01f), TargetPos);
#endif
				}

				//
				Vector3 dir = desiredPosition - TargetPos;
				Vector3 dirN = dir.normalized;
				float length = dir.magnitude;
				float q, dist;
				const float safeDist = maxRadius + 0.1f; //shift the capsule in front of the TargetPos a little

				//do the sphere (capsule) collision with scene
				RaycastHit[] hits = Physics.SphereCastAll(TargetPos - dirN*safeDist, radius, dirN, length + safeDist, mask);

				//sort by distance
				if (hits.Length > 1)
					System.Array.Sort(hits, CollisionUtils.CompareHits);

				//			FinalPos 		= desiredPosition;
				dist = length; //default position at the desiredPosition if we will not collide
				MainCamera.nearClipPlane = Mathf.Max(0.2f, radius*0.5f); //radius * 0.5f;				//was 0.3f;

				foreach (RaycastHit hit in hits)
				{
					if (hit.collider.gameObject.layer == InteractionObject.UseLayer)
						continue;

					if (hit.collider.isTrigger)
						continue;

					if (hit.collider == IgnoredCollider)
						continue;

					//				FinalPos = hit.point + hit.normal * minRadius;		//was 0.3f; changed to 0.1f to help fix the camera-player intersection (when player was running backwards against wall)
					dist = Mathf.Min(length, hit.distance + radius - safeDist - minRadius);

					if (dist < 0) //can become < 0 due to safeDist
						dist = 0;

					//				FinalPos = TargetPos + dirN * q;
					MainCamera.nearClipPlane = (IsRolling || !IsAlive) ? minNear : 0.2f;
									//0.02f;							//was 0.05f; the new value needs testing on devices (check z-fight)

#if CAMERA_DEBUG_DRAW			
					col = Color.grey; col.a = 0.5f;
					Debug.DrawLine(desiredPosition, TargetPos, col, 0.5f);
					DebugDraw.Diamond(Color.red, 0.03f, hit.point);
					
					col = Color.gray; col.a = 0.5f;
					DebugDraw.Capsule(col, radius, TargetPos - dirN * safeDist, TargetPos + dirN * length);
#endif

					break; //need to care just about the nearest valid hit
				}

				//
				float ratio = (Mathf.Abs(PrevDist - dist)/length) + (1 - (radius - minRadius)/maxRadius);

				if (PrevDist < dist) //we're returning back - do it slower
					ratio *= 0.5f;

				float speed = Mathf.Clamp(ratio, 0.2f, 1.0f)*25;
								//change the blend speed based on the distance between actual and computed distance (position)

				q = Mathf.Lerp(PrevDist, dist, Time.deltaTime*speed); //smoothly blend between previous and current position

				//			if ( Mathf.Abs(PrevDist - dist) > 0.01f )
				//				Debug.Log ("radius=" + radius + ", dist=" + dist + ", PrevDist=" + PrevDist + ", ratio=" + ratio + ", q=" + q + ", speed=" + speed);

				PrevDist = q;

#if CAMERA_DEBUG_DRAW			
				DebugDraw.Diamond(Color.yellow, 0.03f, TargetPos + dirN * dist);		//where we need to be (we're interpolating to this pos)
#endif

				//set Final Position
				FinalPos = TargetPos + dirN*q;
			}
//			else
//			{
//				FinalPos = desiredPosition;
//			}

			//set Final Position
			CameraTransform.position = FinalPos;

			//CameraTransform.rotation = Quaternion.Lerp(CameraTransform.rotation, desiredRotation, 15.0f * Time.deltaTime);
			CameraTransform.rotation = desiredRotation;

			//
#if CAMERA_DEBUG_DRAW			
			col = Color.yellow; col.a = 0.5f;
			DebugDraw.Diamond(col, 0.03f, FinalPos);
#endif

			UpdateLocalPlayerInstance();
		}
	}

	void UpdateLocalPlayerInstance()
	{
		if (null != Player.LocalInstance)
		{
			Player.LocalInstance.CameraPosition = CameraTransform.position;
			Player.LocalInstance.CameraDirection = CameraTransform.rotation*Vector3.forward;
		}
	}

	// Update is called once per frame
	void LateUpdate()
	{
		UpdateCamera();
	}

	void Update()
	{
		//Debug.Log(Time.timeSinceLevelLoad + " forward " + CameraTransform.forward  + " " + CameraTransform.position);

		if (MainCamera.fieldOfView < DesiredFov)
		{
			MainCamera.fieldOfView = Mathf.Min(DesiredFov, MainCamera.fieldOfView + ChangeFOVSpeed*Time.deltaTime);
		}
		else if (MainCamera.fieldOfView > DesiredFov)
		{
			MainCamera.fieldOfView = Mathf.Max(DesiredFov, MainCamera.fieldOfView - ChangeFOVSpeed*Time.deltaTime);
		}
	}

	void ChangeStateInternal(E_State state)
	{
		switch (state)
		{
		case E_State.Spectator_Free:

			CameraBehaviour = null;
			IgnoredCollider = null;

			BloodManagerSetNormalizedHealth();

			// prepare spectator cameras
			SpectatorCamera.SetSpectatorMode(SpectatorCamera.Mode.Free);

			BloodManagerSetNormalizedHealth();

			break;

		case E_State.Player:

			if (null != Player.LocalInstance)
			{
				CameraBehaviour = Player.LocalInstance.GetComponent<CameraBehaviour>();
				IgnoredCollider = Player.LocalInstance.Owner.CharacterController;

				if (CameraBehaviour)
				{
					Transform desiredTransform = CameraBehaviour.GetDesiredCameraTransform();
					if (desiredTransform)
						PrevDist = (desiredTransform.position - CameraBehaviour.CameraOrigin.position).magnitude;
				}
			}

			SpectatorCamera.SetSpectatorMode(SpectatorCamera.Mode.None);

			break;

		case E_State.Spectator_FollowPlayer:

			CameraBehaviour = null;
			IgnoredCollider = null;

			SpectatorCamera.SetSpectatorMode(SpectatorCamera.Mode.FollowPlayer);

			break;
		}

		State = state;

		// immediate camera update to prevent camera glitches
		UpdateCamera();
	}

	public void PlayCameraAnim(string animName, bool overrideBehaviour, bool fade)
	{
		if (ParentAnimation[animName] == null)
			return;

		if (overrideBehaviour)
		{
			StartCoroutine(FadeInOutAndCameraPlay(animName));
		}
		else
		{
			ParentAnimation[animName].blendMode = AnimationBlendMode.Blend;
			ParentAnimation.CrossFade(animName, 0.5f);

			if (overrideBehaviour)
				DisabledTime = Time.timeSinceLevelLoad + ParentAnimation[animName].length;
		}
	}

	public void ComboShake(int comboLevel)
	{
		string[] animations = {"shakeCombo1", "shakeCombo2", "shakeCombo3"};

		if (Animation[animations[comboLevel]] == null)
			return;

		Animation[animations[comboLevel]].blendMode = AnimationBlendMode.Blend;
		Animation.Play(animations[comboLevel]);
	}

	public void BigInjuryShake()
	{
		if (Animation["shakeInjury"] == null)
			return;

		Animation["shakeInjury"].blendMode = AnimationBlendMode.Blend;
		Animation.Play("shakeInjury");
	}

	public void Reset(float newFOV = 0, float speed = 0)
	{
		if (newFOV == 0)
		{
			MainCamera.fieldOfView = DefaultFOV;
			DesiredFov = DefaultFOV;
		}
		else
		{
			SetFov(newFOV, speed);
		}
	}

	public void Activate(Vector3 pos, Vector3 lookAt)
	{
		//Debug.Log(pos);
		DisabledTime = 0;

		CameraTransform.position = pos;
		CameraTransform.LookAt(lookAt);
	}

	public void SetFov(float fov, float speed)
	{
		if (!Mathf.Approximately(DesiredFov, fov))
		{
			//Debug.Log ("SetFov, fov=" + fov + ", speed=" + speed);
			DesiredFov = fov;
			ChangeFOVSpeed = speed; // Mathf.Abs(DesiredFov - MainCamera.fieldOfView) / time;
		}
	}

	public void SetDefaultFov(float speed)
	{
		SetFov(DefaultFOV, speed);
	}

	public float GetCurrentFov()
	{
		return MainCamera.fieldOfView;
	}

	public float GetFovRatio()
	{
		return DefaultFOV/MainCamera.fieldOfView;
	}

	public float GetFovRatioExp()
	{
		float ratio = MainCamera.fieldOfView/DefaultFOV;

		return ratio*ratio;
	}

	IEnumerator FadeInOutAndCameraPlay(string animName)
	{
		MFGuiFader.FadeIn(0.1f);
		yield return new WaitForSeconds(0.2f);

		ParentAnimation[animName].blendMode = AnimationBlendMode.Blend;
		ParentAnimation.CrossFade(animName, 0.5f);

		DisabledTime = Time.timeSinceLevelLoad + ParentAnimation[animName].length;

		StartCoroutine(FadeInOutAndCameraPlayEnd(ParentAnimation[animName].length - 0.5f));

		yield return new WaitForSeconds(0.1f);

		MFGuiFader.FadeOut();
	}

	IEnumerator FadeInOutAndCameraPlayEnd(float delay)
	{
		yield return new WaitForSeconds(delay);

		MFGuiFader.FadeIn(0.3f);
		yield return new WaitForSeconds(0.5f);
		MFGuiFader.FadeOut();
	}

	// disable camera effects (Blood)
	void BloodManagerSetNormalizedHealth()
	{
		if (BloodFXManager.Instance)
		{
			BloodFXManager.Instance.SetHealthNormalized(1.0f);
		}
	}

	// change camera mode 
	public static void ChangeMode(E_State State)
	{
		if (null != Instance)
		{
			Instance.ChangeStateInternal(State);
		}
	}

	// @return current camera mode
	public static E_State GetCurrentMode()
	{
		if (null != Instance)
		{
			return Instance.State;
		}

		return E_State.None;
	}
}
