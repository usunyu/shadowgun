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
using System.Collections.Generic;

[ExecuteInEditMode]
[AddComponentMenu("Effects/Screen space glow")]
public class ScreenSpaceGlowEmitter : MonoBehaviour
{
	public enum GlowType
	{
		Point,
		Spot
	}

	public GlowType m_GlowType;
	public Color m_Color = new Color(1, 1, 1, 1);
	public float m_Intensity = 1;
	public float m_MaxVisDist = 30;
	public float m_ConeAngle = 30;
	public float m_DirIntensityFallof = 2;
	public int m_InstanceID = -1;
	public LayerMask m_ColLayerMask = -1;
	public static HashSet<ScreenSpaceGlowEmitter> ms_Instances = new HashSet<ScreenSpaceGlowEmitter>();
	static int ms_InstCnt = 0;

	void Awake()
	{
		m_InstanceID = ms_InstCnt++;
		ms_Instances.Add(this);
	}

	void OnDestroy()
	{
		ms_Instances.Remove(this);
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(transform.position, 0.25f);
	}
}
