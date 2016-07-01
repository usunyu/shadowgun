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

[AddComponentMenu("Utils/SineMover")]
public class SineMover : MonoBehaviour
{
	public float m_SineAmplitudeX = 0;
	public float m_SineFreqX = 1;

	public float m_SineAmplitudeY = 0;
	public float m_SineFreqY = 1;

	public float m_SineAmplitudeZ = 0;
	public float m_SineFreqZ = 1;

	Transform m_Transform;
	Vector3 m_BasePos = Vector3.zero;

	void Start()
	{
		m_Transform = transform;

		if (m_Transform != null)
		{
			m_BasePos = m_Transform.position;
		}
	}

	void LateUpdate()
	{
		if (m_Transform)
		{
			Vector3 pos = Vector3.zero;

			pos.x = m_SineAmplitudeX*Mathf.Sin(Time.time*m_SineFreqX);
			pos.y = m_SineAmplitudeY*Mathf.Sin(Time.time*m_SineFreqY);
			pos.z = m_SineAmplitudeZ*Mathf.Sin(Time.time*m_SineFreqZ);

			m_Transform.position = m_BasePos + pos;
		}
	}
}
