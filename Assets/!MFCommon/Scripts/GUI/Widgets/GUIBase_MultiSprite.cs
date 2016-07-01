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
using System.Collections.Generic;

[AddComponentMenu("GUI/Widgets/MultiSprite")]
public class GUIBase_MultiSprite : GUIBase_Callback
{
	[System.Serializable]
	class StateInfo
	{
		public string Name;
		public Rect UVCoords;
		public Color Color;

		public override string ToString()
		{
			return Name;
		}
	}

	// PRIVATE MEMBERS

	[SerializeField] string m_State;
	[SerializeField] [HideInInspector] StateInfo[] m_States = new StateInfo[0];

	Color m_Color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	bool m_Dirty = true;

	// PUBLIC MEMBERS

	public readonly static string DefaultState = "default";

	// GETTERS / SETTERS

	public GUIBase_Widget Widget { get; private set; }

	public Color Color
	{
		get { return m_Color; }
		set { SetColor(value); }
	}

	public string State
	{
		get { return m_State; }
		set { SetState(value); }
	}

	public int Count
	{
		get { return m_States.Length; }
	}

	public string[] States
	{
		get
		{
			string[] keys = new string[m_States.Length];
			for (int idx = 0; idx < m_States.Length; ++idx)
			{
				keys[idx] = m_States[idx].Name;
			}
			return keys;
		}
	}

	// PUBLIC METHODS

	public bool Contains(string state)
	{
		for (int idx = 0; idx < m_States.Length; ++idx)
		{
			if (m_States[idx].Name == state)
				return true;
		}
		return false;
	}

	// this method is used by build process to cache states
	public void PrepareStates()
	{
		GUIBase_Sprite[] sprites = GetComponentsInChildren<GUIBase_Sprite>();
		if (sprites.Length == 0 && m_States.Length > 1)
			return;

		m_States = new StateInfo[sprites.Length + 1];

		// add current widget's UVs as default ones
		GUIBase_Widget thisWidget = Widget ?? GetComponent<GUIBase_Widget>();
		m_States[0] = new StateInfo()
		{
			Name = DefaultState,
			UVCoords = new Rect(thisWidget.m_InTexPos.x, thisWidget.m_InTexPos.y, thisWidget.m_InTexSize.x, thisWidget.m_InTexSize.y),
			Color = thisWidget.Color
		};

		// read uv coords and destroy all game objects
		for (int idx = 0; idx < sprites.Length; ++idx)
		{
			GUIBase_Sprite sprite = sprites[idx];

			GUIBase_Widget widget = sprite.GetComponent<GUIBase_Widget>();
			if (widget != null)
			{
				m_States[idx + 1] = new StateInfo()
				{
					Name = widget.name,
					UVCoords = new Rect(widget.m_InTexPos.x, widget.m_InTexPos.y, widget.m_InTexSize.x, widget.m_InTexSize.y),
					Color = widget.Color
				};
			}

			Object.DestroyImmediate(sprite.gameObject);
		}
	}

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		Widget = GetComponent<GUIBase_Widget>();
		if (Widget == null)
		{
			Debug.LogWarning("There is NOT any widget specified for MultiSprite '" + MFDebugUtils.GetFullName(gameObject) + "'!");
			return;
		}
		Widget.CreateMainSprite = true;
	}

	void Start()
	{
		Widget.RegisterCallback(this, (int)E_CallbackType.E_CT_NONE);
		Widget.RegisterUpdateDelegate(OnWidgetUpdate);

		// get all linked sprites
		// we call this method just to be sure we have valid data prepared
		PrepareStates();
	}

	// PRIVATE METHODS

	void SetColor(Color color)
	{
		if (m_Color == color)
			return;

		m_Color = color;
		m_Dirty = true;

		Widget.SetModify();
	}

	void SetState(string state)
	{
		if (state.Length == 0)
			state = DefaultState;

		if (m_State == state)
			return;
		if (Contains(state) == false)
			return;

		m_State = state;
		m_Dirty = true;

		Widget.SetModify();
	}

	void OnWidgetUpdate()
	{
		if (m_Dirty == false)
			return;

		string state = m_State.Length == 0 ? DefaultState : m_State;
		for (int idx = 0; idx < m_States.Length; ++idx)
		{
			StateInfo info = m_States[idx];
			if (info.Name != state)
				continue;

			Rect uvCoords = info.UVCoords;
			Widget.SetTextureCoords(0, uvCoords.x, uvCoords.y, uvCoords.width, uvCoords.height);
			Widget.Color = info.Color*m_Color;

			break;
		}

		m_Dirty = false;
	}
}
