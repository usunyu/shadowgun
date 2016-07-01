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

public class CamExplosionFXMgr : MonoBehaviour
{
	public static CamExplosionFXMgr Instance;

	bool m_PrevDbgBtnState = false;

	void Awake()
	{
		if (!Instance)
		{
			Instance = this;
		}
	}

	void OnDestroy()
	{
		Instance = null;
	}

	void LateUpdate()
	{
//		DbgEmitFX();		
		DisableCamFX();
	}

	public void Reset()
	{
		if (MFExplosionPostFX.Instance != null)
		{
			MFExplosionPostFX.Instance.Reset();
		}
	}

	public static void PreloadResources()
	{
		if (!MFExplosionPostFX.Instance)
		{
			Camera.main.gameObject.AddComponent<MFExplosionPostFX>();
			MFDebugUtils.Assert(MFExplosionPostFX.Instance);
		}
	}

	void CreateCamFXInstance()
	{
		if (!MFExplosionPostFX.Instance)
		{
			Camera.main.gameObject.AddComponent<MFExplosionPostFX>();
			MFDebugUtils.Assert(MFExplosionPostFX.Instance);
		}

		MFExplosionPostFX.Instance.enabled = true;
	}

	void DisableCamFX()
	{
		//
		// disable CamFX if no effect is running
		//

		if (MFExplosionPostFX.Instance && MFExplosionPostFX.Instance.enabled)
		{
			if (MFExplosionPostFX.Instance.NumActiveEffects() == 0)
			{
				//Debug.Log("Disabling camera explosion postFX");
				MFExplosionPostFX.Instance.enabled = false;
			}
		}
	}

	void DbgEmitFX()
	{
		bool dbgBtnState = Input.GetButton("Fire1");

		if (dbgBtnState && !m_PrevDbgBtnState)
		{
			DbgEmitGrenadeExplWave();
		}

		m_PrevDbgBtnState = dbgBtnState;
	}

	void DbgEmitGrenadeExplWave()
	{
		if (!Camera.main)
		{
			return;
		}

		CreateCamFXInstance();

		Vector2 pos;

		pos.x = Random.Range(0.0f, 1.0f);
		pos.y = Random.Range(0.0f, 1.0f);

		MFExplosionPostFX.S_WaveParams waveParams;

		waveParams.m_Amplitude = 0.3f;
		waveParams.m_Duration = 1.5f;
		waveParams.m_Freq = 20;
		waveParams.m_Speed = 1.5f;
		waveParams.m_Radius = 1;
		waveParams.m_Delay = 0;

		MFExplosionPostFX.Instance.EmitGrenadeExplosionWave(pos, waveParams);
	}

	public void SpawnExplosionWaveFX(Vector3 worldPos, MFExplosionPostFX.S_WaveParams waveParams)
	{
		waveParams.m_Delay = 0;

		InternalSpawnExplosionWaveFX(worldPos, waveParams);
	}

	public void SpawnExplosionWaveFX(Vector3 worldPos, MFExplosionPostFX.S_WaveParams waveParams, float inDelay)
	{
		waveParams.m_Delay = inDelay;

		InternalSpawnExplosionWaveFX(worldPos, waveParams);
	}

	bool InternalSpawnExplosionWaveFX(Vector3 worldPos, MFExplosionPostFX.S_WaveParams waveParams)
	{
#if UNITY_EDITOR
		return true;
#else
		if (!Camera.main)
		{
			return false;
		}

		Vector3 posInView 	= Camera.main.worldToCameraMatrix.MultiplyPoint(worldPos);
							
		if (posInView.z > 0)
		{
			if (waveParams.m_Radius < 0.75f)
			{
                return false;
			}
			
			posInView.z = -posInView.z;
		}
		
		Vector3 worldPosFixed = Camera.main.cameraToWorldMatrix.MultiplyPoint(posInView);		
		Vector3 pos = Camera.main.WorldToViewportPoint(worldPosFixed);		
		Vector2 spos;
		
		CreateCamFXInstance();
						
		spos.x = pos.x;
		spos.y = pos.y;
				
		MFExplosionPostFX.Instance.EmitGrenadeExplosionWave(spos,waveParams);
		
		return true;
#endif
	}
}
