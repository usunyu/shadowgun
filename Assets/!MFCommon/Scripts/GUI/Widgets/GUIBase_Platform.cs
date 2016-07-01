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

[AddComponentMenu("GUI/Hierarchy/Platform")]
public class GUIBase_Platform : MonoBehaviour
{
	public int m_Width = 320;
	public int m_Height = 480;

	public enum E_SpecialAnimIdx
	{
		E_SAI_INANIM = 0,
		E_SAI_OUTANIM = 1,
		//...

		E_SAI_FIRSTCUSTOM = 10, // from this index can start user with his own indexing
		E_SAI_BUTTONANIM = 11,
	}

	// delegate called from GUIUpdate
	public delegate void AnimFinishedDelegate(int animIdx);

	//
	// Private data
	//

	// List of currently running animations
	struct S_AnimDscr
	{
		public Animation m_Animation;
		public float m_StartTime;
		public float m_Length;
		public int m_CustomIdx;
		public AnimFinishedDelegate m_AnimFinishedDelegate;
		public GUIBase_Widget m_Widget; // je vyplneny, jen pokud je animace pustena primo na widgetu
	};

	// array of S_AnimDscr
	ArrayList m_PlayingAnims;

	// array of animations to be removed (indexes to m_PlayingAnims)
	ArrayList m_AnimsToRemove;

	bool m_IsInitialized;

	[HideInInspector] public GUIBase_Pivot[] Pivots;

	[HideInInspector] public GUIBase_Layout[] Layouts;

	//---------------------------------------------------------
	void Awake()
	{
		if (Game.Instance != null && Game.Instance.AppType == Game.E_AppType.DedicatedServer)
		{
			gameObject.SetActive(false);
		}
	}

	//---------------------------------------------------------
	void Start()
	{
		m_IsInitialized = false;

		MFGuiManager guiManager = MFGuiManager.Instance;
		if (guiManager)
		{
			//
			// Platformu nemuzeme registrovat v Awake, protoze v Awake vznika GUIManager.Instance
			//
			guiManager.RegisterPlatform(this);

			m_PlayingAnims = new ArrayList();
			m_AnimsToRemove = new ArrayList();
		}
		else
		{
			gameObject.SetActive(false);
			Debug.LogWarning("GuiManager prefab is not present!");
		}
	}

	void OnDestroy()
	{
		MFGuiManager guiManager = MFGuiManager.Instance;
		if (guiManager)
		{
			guiManager.UnregisterPlatform(this);
		}
	}

	// HACK - je potreba jen na zjisteni, jestli uz se UIcko updatuje. To znamena, ze vsechny komponenty uz by mely byt dohledatelne ze hry
	public bool IsInitialized()
	{
		return m_IsInitialized;
	}

	//---------------------------------------------------------
	public void Update()
	{
		m_IsInitialized = true;

		ProcessAnimations();
	}

	//---------------------------------------------------------
	public void PlayAnim(Animation animation,
						 GUIBase_Widget widget,
						 GUIBase_Platform.AnimFinishedDelegate finishDelegate = null,
						 int customIdx = -1)
	{
		S_AnimDscr animDscr = new S_AnimDscr();

		animDscr.m_Animation = animation;
		animDscr.m_StartTime = Time.realtimeSinceStartup;
		//animDscr.m_StartTime			= Time.time;
		animDscr.m_Length = animation.clip.length;
		animDscr.m_CustomIdx = customIdx;
		animDscr.m_AnimFinishedDelegate = finishDelegate;
		animDscr.m_Widget = widget;

		animDscr.m_Animation.wrapMode = animDscr.m_Animation.clip.wrapMode;

		int idx = m_PlayingAnims.Count;

		m_PlayingAnims.Add(animDscr);

		ProcessAnim(animDscr, 0.0f, idx);

		//Debug.Log("anim "+ animDscr.m_Animation.name +", Start time = "+ animDscr.m_StartTime +", Length = "+ animDscr.m_Length);
	}

	//---------------------------------------------------------
	public void StopAnim(Animation animation)
	{
		if (animation)
		{
			animation.Stop();
			animation.Sample();

			for (int idx = 0; idx < m_PlayingAnims.Count; ++idx)
			{
				S_AnimDscr anim = (S_AnimDscr)m_PlayingAnims[idx];

				if (anim.m_Animation == animation)
				{
					ProcessAnim(anim, 1, idx);
					//Debug.Log("Remove anim");
					if (anim.m_Widget)
					{
						anim.m_Widget.SetModify();
					}
					m_PlayingAnims.RemoveAt(idx);
					return;
				}
			}
		}
	}

	//---------------------------------------------------------
	void ProcessAnimations()
	{
		// Process playing animations
		if (m_PlayingAnims != null && m_PlayingAnims.Count > 0)
		{
			float currTime = Time.realtimeSinceStartup;
			//float	currTime = Time.time;

			for (int idx = 0; idx < m_PlayingAnims.Count; ++idx)
			{
				S_AnimDscr anim = (S_AnimDscr)m_PlayingAnims[idx];
				float deltaTime = currTime - anim.m_StartTime;

				ProcessAnim(anim, deltaTime, idx);
			}
		}

		// Process animations to remove

		if (m_AnimsToRemove != null && m_AnimsToRemove.Count > 0)
		{
			for (int idx = m_AnimsToRemove.Count - 1; idx >= 0; --idx)
			{
				S_AnimDscr anim = (S_AnimDscr)m_PlayingAnims[(int)m_AnimsToRemove[idx]];

				// signalize remove of some special animation
				if (anim.m_CustomIdx != -1)
				{
					AnimationRemoved(anim.m_CustomIdx, anim.m_AnimFinishedDelegate);
				}

				m_PlayingAnims.RemoveAt((int)m_AnimsToRemove[idx]);
			}

			m_AnimsToRemove.RemoveRange(0, m_AnimsToRemove.Count);
		}
	}

	//---------------------------------------------------------
	void ProcessAnim(S_AnimDscr anim, float deltaTime, int idx)
	{
		anim.m_Animation.Play();

		foreach (AnimationState state in anim.m_Animation)
		{
			state.enabled = true;
			state.time = deltaTime;
		}

		anim.m_Animation.Sample();

		foreach (AnimationState state in anim.m_Animation)
		{
			state.enabled = false;
		}

		//Debug.Log("Delta Time = " + deltaTime);

		//anim.m_Animation.Stop();
		//anim.m_Animation.Sample();

		if ((anim.m_Animation.wrapMode == WrapMode.Once || anim.m_Animation.wrapMode == WrapMode.Default) && (deltaTime > anim.m_Length))
		{
			//Debug.Log("anim.m_Animation = " + anim.m_Animation.name + ", Remove time = "+ Time.realtimeSinceStartup);

			m_AnimsToRemove.Add(idx);
		}

		// Signalize modification to widget

		if (anim.m_Widget)
		{
			anim.m_Widget.SetModify();
		}
	}

	//---------------------------------------------------------
	void AnimationRemoved(int customIdx, GUIBase_Platform.AnimFinishedDelegate finishDelegate)
	{
		if (finishDelegate != null)
		{
			finishDelegate(customIdx);
		}
	}
}
