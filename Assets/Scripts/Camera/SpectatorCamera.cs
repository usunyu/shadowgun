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

// Spectator cameras

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof (Camera))]
[AddComponentMenu("Multiplayer/Spectator camera")]
public class SpectatorCamera : MonoBehaviour
{
	public enum Mode
	{
		None,
		Free,
		FollowPlayer
	}

	Camera mCamera = null;

	static List<SpectatorCamera> mSpectatorCameras = new List<SpectatorCamera>();
	static SpectatorCamera mCurrentSpectatorCamera = null;
	static Camera mPreviousCamera = null;
	static Mode mCurrentMode = Mode.None;

	// follow player
	static AgentHuman mLastHumanSpectator = null;
	static string mLastHumanSpectatorName = string.Empty;
	static float mTimeFromhumanSpectatorActivation = 0;
	static bool mAutoSwitch = true;
	static bool mSwitchToPlayer = false;

	public static float SpectatorCameraTimerMin = 5;
	public static float SpectatorCameraTimerMax = 10;

	void Awake()
	{
		mCamera = GetComponent<Camera>();

		Activate(false);

		mCurrentMode = Mode.None;

		mSpectatorCameras.Add(this);
	}

	void OnDestroy()
	{
		mSpectatorCameras.Remove(this);
	}

	public static void SetSpectatorMode(Mode NewMode)
	{
		if (mCurrentMode == NewMode)
		{
			return;
		}

		mCurrentMode = NewMode;

		switch (mCurrentMode)
		{
		case Mode.Free:

			RestorePreviousCamera();

			if (mSpectatorCameras.Count > 0)
			{
				mCurrentSpectatorCamera = mSpectatorCameras[Random.Range(0, mSpectatorCameras.Count)];

				if (null != mCurrentSpectatorCamera)
				{
					mPreviousCamera = Camera.main;

					mCurrentSpectatorCamera.Activate(true);

					if (null != mPreviousCamera)
					{
						mPreviousCamera.enabled = false;

						SetupAudioListeners(mPreviousCamera.gameObject, false);
					}
				}
			}

			break;

		case Mode.FollowPlayer:

			RestorePreviousCamera();

			break;

		default:

			RestorePreviousCamera();

			mAutoSwitch = true;

			break;
		}
	}

	void Activate(bool On)
	{
		enabled = On;

		if (null != mCamera)
		{
			// activated camera will be enabled during next update preventing some glitches
			if (!On)
			{
				mCamera.enabled = On;
			}

			if (null != gameObject)
			{
				Animation[] anims = gameObject.GetComponents<Animation>();

				foreach (Animation Anim in anims)
				{
					Anim.enabled = On;

					if (On)
						Anim.Sample();
				}

				SetupAudioListeners(gameObject, On);
			}
		}
	}

	void Update()
	{
		// delayed camera activation
		if (null != mCamera && !mCamera.enabled)
		{
			mCamera.enabled = true;
		}
	}

	static void RestorePreviousCamera()
	{
		if (null != mPreviousCamera)
		{
			mPreviousCamera.enabled = true;

			SetupAudioListeners(mPreviousCamera.gameObject, true);

			if (null != mCurrentSpectatorCamera)
			{
				mCurrentSpectatorCamera.Activate(false);

				mCurrentSpectatorCamera = null;
			}

			mPreviousCamera = null;
		}
	}

	static void SetupAudioListeners(GameObject Obj, bool On)
	{
		AudioListener[] listeners = Obj.GetComponents<AudioListener>();

		foreach (AudioListener Listener in listeners)
		{
			Listener.enabled = On;
		}
	}

	// camera can follow a player in case there is one
	public static bool Spectator_CanFollowPlayer()
	{
		return Player.Players.Count > 0;
	}

	public static AgentHuman SelectNextAgentToFollow()
	{
		if (mSwitchToPlayer)
		{
			// we already selected next player, using NEXT/PREV buttons
			mSwitchToPlayer = false;

			return mLastHumanSpectator;
		}

		AgentHuman SelectedAgent = null;

		List<AgentHuman> list = new List<AgentHuman>();

		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			list.Add(pair.Value.Owner);
		}

		// to prevent of selection of the same human
		if (null != mLastHumanSpectator && list.Count > 1)
		{
			list.Remove(mLastHumanSpectator);
		}

		// we prefer alive human
		foreach (AgentHuman Human in list)
		{
			if (Human.IsAlive)
			{
				for (int i = list.Count - 1; i >= 0; i--)
				{
					if (!list[i].IsAlive)
					{
						list.RemoveAt(i);
					}
				}
				break;
			}
		}

		if (list.Count > 0)
		{
			SelectedAgent = list[Random.Range(0, list.Count)];
		}

		SetHumanSpectator(SelectedAgent);

		return SelectedAgent;
	}

	static void SetHumanSpectator(AgentHuman Agent)
	{
		if (null != mLastHumanSpectator)
		{
			mLastHumanSpectator.BlackBoard.ActionHandler -= SpectatorActionHandler;
		}

		mLastHumanSpectator = Agent;

		mTimeFromhumanSpectatorActivation = Random.Range(SpectatorCameraTimerMin, SpectatorCameraTimerMax);

		if (null != mLastHumanSpectator)
		{
			mLastHumanSpectator.BlackBoard.ActionHandler += SpectatorActionHandler;
			mLastHumanSpectatorName = string.Empty;

			uLink.NetworkPlayer networkPlayer = Player.GetNetworkPlayer(mLastHumanSpectator);

			if (uLink.NetworkPlayer.unassigned != networkPlayer)
			{
				mLastHumanSpectatorName = Game.GetPlayerName(networkPlayer);
			}
		}
	}

	public static bool UpdateFollowPlayer()
	{
		if (mAutoSwitch)
		{
			mTimeFromhumanSpectatorActivation -= Time.deltaTime;

			if (mTimeFromhumanSpectatorActivation <= 0)
			{
				return false;
			}
		}
		else
		{
			return !mSwitchToPlayer;
		}

		return true;
	}

	public static void SpectatorActionHandler(AgentAction action)
	{
		if (action.IsFailed())
		{
			return;
		}

		if (action is AgentActionDeath)
		{
			if (mAutoSwitch)
			{
				mTimeFromhumanSpectatorActivation = Mathf.Min(mTimeFromhumanSpectatorActivation, 1.5f);
			}
			else
			{
				mAutoSwitch = true;

				mTimeFromhumanSpectatorActivation = 1.5f;
			}
		}
		else if (action is AgentActionAttack)
		{
			mTimeFromhumanSpectatorActivation = Mathf.Max(mTimeFromhumanSpectatorActivation, 1.5f);
		}
		else if (action is AgentActionInjury)
		{
			mTimeFromhumanSpectatorActivation = Mathf.Max(mTimeFromhumanSpectatorActivation, 2.5f);
		}
		else if (action is AgentActionMelee)
		{
			mTimeFromhumanSpectatorActivation = Mathf.Max(mTimeFromhumanSpectatorActivation, 1.75f);
		}
		else if (action is AgentActionKnockdown)
		{
			mTimeFromhumanSpectatorActivation = Mathf.Max(mTimeFromhumanSpectatorActivation, 1.75f);
		}
	}

	public static string GetSpectatedPlayerName()
	{
		if (null != mLastHumanSpectator)
		{
			return mLastHumanSpectatorName;
		}

		return "";
	}

	public static void PrevPlayer()
	{
		mAutoSwitch = false;

		mSwitchToPlayer = true;

		AgentHuman Prev = null;

		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			if (pair.Value.Owner == mLastHumanSpectator)
			{
				if (null != Prev)
				{
					SetHumanSpectator(Prev);
					return;
				}

				continue;
			}
			Prev = pair.Value.Owner;
		}

		if (null != Prev)
		{
			SetHumanSpectator(Prev);
		}
	}

	public static void NextPlayer()
	{
		mAutoSwitch = false;
		mSwitchToPlayer = true;

		AgentHuman Next = mLastHumanSpectator;

		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			if (null == Next)
			{
				SetHumanSpectator(pair.Value.Owner);
				return;
			}

			if (pair.Value.Owner == mLastHumanSpectator)
			{
				Next = null;
			}
		}

		foreach (KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players)
		{
			SetHumanSpectator(pair.Value.Owner);
			break;
		}
	}
}
