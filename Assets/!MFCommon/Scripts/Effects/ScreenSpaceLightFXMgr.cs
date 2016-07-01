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

#define DISABLE_SCREENSPACE_VERTEX_GRID_FX

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Effects/Screen space light FX mgr")]
public class ScreenSpaceLightFXMgr : MonoBehaviour
{
	public bool m_EnableEffects = true;

	/*
	void FakeInit()
	{
		//LRUCache<int,S_CacheRec>	m_GlowsVisCache = new LRUCache<int,S_CacheRec>(128);
		MFScreenSpaceVertexGridFX fx = new MFScreenSpaceVertexGridFX();
		fx = null;
	}
	*/

	void Awake()
	{
#if  DEADZONE_DEDICATEDSERVER
		m_EnableEffects = false;
#endif // DEADZONE_DEDICATEDSERVER
	}

	void LateUpdate()
	{
		PostFXTracking.ScreenspaceLightFXEffectActive = false;

		if (MFScreenSpaceVertexGridFX.Instance)
		{
			if (m_EnableEffects)
			{
				MFScreenSpaceVertexGridFX.Instance.InternalUpdate();

				if (MFScreenSpaceVertexGridFX.Instance.AnyEffectActive())
				{
					MFScreenSpaceVertexGridFX.Instance.enabled = true;
				}
				else
				{
					MFScreenSpaceVertexGridFX.Instance.enabled = false;
				}
			}
			else
			{
				MFScreenSpaceVertexGridFX.Instance.enabled = false;
			}
		}
	}
}
