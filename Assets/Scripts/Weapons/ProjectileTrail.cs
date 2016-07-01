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

public class ProjectileTrail : MonoBehaviour
{
	public float m_BeamNoFadeDuration = 1.0f;
	public float m_BeamFadeDuration = 2.0f;

	float m_InitTimer;
	LineRenderer m_LineRenderer;

	Vector3 m_TrailInitPos;
	int m_VertexCount;

	Vector4 m_Color;
	public Material m_Material;

	// Use this for initialization
	void Awake()
	{
		m_LineRenderer = GetComponent<LineRenderer>();
	}

	// Update is called once per frame
	void Update()
	{
		if (Time.timeSinceLevelLoad - m_InitTimer > m_BeamNoFadeDuration && m_BeamFadeDuration > 0)
		{
			float relTime = Time.timeSinceLevelLoad - m_InitTimer - m_BeamNoFadeDuration;
			float alpha = 1 - relTime/m_BeamFadeDuration;

			m_Color.w = alpha;
			m_LineRenderer.material.SetVector("_TintColor", m_Color);
		}
	}

	public void InitTrail(Vector3 inPos)
	{
		m_InitTimer = Time.timeSinceLevelLoad;
		m_TrailInitPos = inPos;

		//m_LineRenderer.SetColors(m_StartColor, m_EndColor);
		if (m_LineRenderer != null)
		{
			m_VertexCount = 2;
			m_LineRenderer.useWorldSpace = true;
			m_LineRenderer.SetVertexCount(m_VertexCount);
			m_LineRenderer.SetPosition(0, m_TrailInitPos);
			m_LineRenderer.SetPosition(1, m_TrailInitPos);

			if (m_Material != null)
			{
				m_Material = m_LineRenderer.material;
				m_Color = m_Material.GetVector("_TintColor");
			}
		}
	}

	public void AddTrailPos(Vector3 inPos)
	{
		if (m_LineRenderer == null)
			return;

		m_LineRenderer.SetPosition(m_VertexCount - 1, inPos);
		m_VertexCount++;
		m_LineRenderer.SetVertexCount(m_VertexCount);
		m_LineRenderer.SetPosition(m_VertexCount - 1, inPos);
	}

	// Update is called once per frame
	public void UpdateTrailPos(Vector3 inPos)
	{
		if (m_LineRenderer == null)
			return;

		m_LineRenderer.SetPosition(m_VertexCount - 1, inPos);
	}

	public bool IsVisible()
	{
		return ((Time.timeSinceLevelLoad - m_InitTimer) < (m_BeamNoFadeDuration + m_BeamFadeDuration));
	}
}
