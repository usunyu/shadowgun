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

[ExecuteInEditMode]
[RequireComponent(typeof (GUIBase_Label))]
public class GUIBase_LabelBoundaries : MonoBehaviour
{
	// CONFIGURATION

	[SerializeField] int m_Width;
	[SerializeField] int m_Height;

	// PRIVATE MEMBERS

	GUIBase_Label m_Label;
	Vector3 m_Position;
	Vector3 m_LossyScale;
	Rect m_Boundaries;
#if UNITY_EDITOR
	int m_OldWidth;
	int m_OldHeight;

	// PUBLIC MEMBERS

	public int Width
	{
		get { return m_Width; }
	}

	public int Height
	{
		get { return m_Height; }
	}
#endif

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		m_Label = GetComponent<GUIBase_Label>();

		GUIBase_Widget widget = GetComponent<GUIBase_Widget>();
		if (widget != null)
		{
			widget.RegisterUpdateDelegate(RegenerateBoundaries);

#if UNITY_EDITOR
			if (m_Width <= 0)
			{
				m_Width = Mathf.RoundToInt(widget.GetWidth());
			}
			if (m_Height <= 0)
			{
				m_Height = Mathf.RoundToInt(widget.GetHeight());
			}
#endif
		}
	}

#if UNITY_EDITOR
	void Update()
	{
		if (Application.isPlaying == true)
			return;

		GUIBase_Label label = m_Label;
		if (label == null)
		{
			label = GetComponent<GUIBase_Label>();
		}
		if (label == null)
			return;

		bool sizeChanged = false;
		if (m_Width != m_OldWidth || m_Height != m_OldHeight)
		{
			sizeChanged = true;
			m_OldWidth = m_Width;
			m_OldHeight = m_Height;
		}

		if (sizeChanged == false)
			return;

		Transform trans = transform;
		Vector3 position = trans.position;
		Vector3 lossyScale = trans.lossyScale;

		RegenerateBoundaries(label, position, lossyScale);

		label.GenerateRunTimeData();
	}
#endif

	void RegenerateBoundaries()
	{
		Transform trans = transform;
		Vector3 position = trans.position;
		Vector3 lossyScale = trans.lossyScale;

		if (m_Position == position &&
			m_LossyScale == lossyScale)
			return;

		RegenerateBoundaries(m_Label, position, lossyScale);
	}

	void RegenerateBoundaries(GUIBase_Label label, Vector3 position, Vector3 lossyScale)
	{
		if (label == null)
			return;

		Vector3 topLeft = label.GetLeftUpPos(position, m_Width, m_Height, lossyScale);
		float width = m_Width*lossyScale.x;
		float height = m_Height*lossyScale.y;

		m_Position = position;
		m_LossyScale = lossyScale;

		m_Boundaries.x = topLeft.x;
		m_Boundaries.y = topLeft.y;
		m_Boundaries.width = width;
		m_Boundaries.height = height;

		label.Boundaries = m_Boundaries;
	}
}
