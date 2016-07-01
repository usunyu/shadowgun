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

public class CamRelativePos : MonoBehaviour
{
	public Vector3 m_Offset = new Vector3(0, 0, 0);
	public bool m_HorizontalOnly = false;
	Transform m_OwnerTransform;

	void Awake()
	{
		m_OwnerTransform = transform;
	}

	void LateUpdate()
	{
		if (m_OwnerTransform && Camera.main)
		{
			Vector3 newPos = Camera.main.transform.position + m_Offset;

			if (m_HorizontalOnly)
			{
				newPos.y = m_OwnerTransform.position.y;
			}

			m_OwnerTransform.position = newPos;
		}
	}
};
