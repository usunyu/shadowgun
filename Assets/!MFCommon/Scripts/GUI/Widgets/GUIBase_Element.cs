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

[AddComponentMenu("")]
public abstract class GUIBase_Element : MonoBehaviour
{
	// PRIVATE MEMBERS

	GUIBase_Element m_Parent = null;

	bool m_IsVisible = false;
	bool m_IsEnabled = true;
	bool m_IsDirty = true;

	[SerializeField] float m_FadeAlpha = 1.0f;

	// GETTERS / SETTERS

	public MFGuiManager GuiManager
	{
		get { return MFGuiManager.Instance; }
	}

	public GUIBase_Element Parent
	{
		get { return m_Parent; }
	}

	public bool IsDirty
	{
		get { return m_IsDirty; }
	}

	public bool InputEnabled
	{
		get { return m_IsEnabled; }
		set { m_IsEnabled = value; }
	}

	public float FadeAlpha
	{
		get { return m_FadeAlpha; }
		set { SetFadeAlpha(value, false); }
	}

	public bool Visible
	{
		get { return m_IsVisible; }
		set
		{
			if (m_IsVisible != value)
			{
				m_IsVisible = value;
				m_IsDirty = true;

				System.Type type = this.GetType();
				System.Reflection.MethodInfo method = type.GetMethod(m_IsVisible ? "OnElementVisible" : "OnElementHidden");
				if (method != null)
				{
					method.Invoke(this, null);
				}
			}
		}
	}

	// DIRTY HACK for composite controls as buttons is
	public bool DisallowShowRecursive { get; set; }

	// ABSTRACT INTERFACE

	protected virtual void OnGUIUpdate(float parentAlpha)
	{
	}

	protected virtual void OnLayoutChanged()
	{
	}

	// PUBLIC METHODS

	public bool IsVisible(bool recursive = false)
	{
		if (recursive == true && m_Parent != null)
			return m_IsVisible && m_Parent.IsVisible(true);
		return m_IsVisible;
	}

	public bool IsEnabled(bool recursive = false)
	{
		if (recursive == true && m_Parent != null)
			return m_IsEnabled && m_Parent.IsEnabled(true);
		return m_IsEnabled;
	}

	public void Show(bool state, bool recursive)
	{
		if (GuiManager == null)
			return;

		//Debug.Log("GUIBase_Element<"+this.GetFullName('.')+">.Show("+state+", "+recursive+")");

		// DIRTY HACK for composite controls as buttons is
		if (state == true && DisallowShowRecursive == true)
		{
			recursive = false;
		}

		GuiManager.Show(this, state, recursive);
	}

	public bool Relink(GUIBase_Element element)
	{
		if (element == null)
			return false;
		if (m_Parent == element)
			return true;

		// notify children and parents about change
		NotifyLayoutChanged(true, true);

		// store transformations for later use
		Transform trans = transform;
		Vector3 position = trans.localPosition;
		Vector3 scale = trans.localScale;
		Quaternion rotation = trans.localRotation;

		// change parent in scene tree
		trans.parent = element.transform;

		// get new ui parent
		m_Parent = element;

		// re-store transformations
		trans.localPosition = position;
		trans.localScale = scale;
		trans.localRotation = rotation;

		// notify new parens about change
		NotifyLayoutChanged(false, true);

		// done
		return true;
	}

	public void SetFadeAlpha(float value, bool propagateDownward)
	{
		value = Mathf.Clamp(value, 0.0f, 1.0f);
		if (m_FadeAlpha == value)
			return;

		m_FadeAlpha = value;

		if (propagateDownward == true)
		{
			GUIBase_Widget[] children = GetComponentsInChildren<GUIBase_Widget>();
			foreach (var child in children)
			{
				child.m_FadeAlpha = value;
				child.m_IsDirty = true;
			}
		}

		SetModify(true);
	}

	public void SetModify(bool propagateDownward = false, bool propagateUpward = true)
	{
		m_IsDirty = true;

		// propagate dirty flag up to the parent
		if (propagateUpward == true && m_Parent != null)
		{
			m_Parent.SetModify();
		}

		// propagate dirty flag down to children
		if (propagateDownward == true)
		{
			GUIBase_Element[] children = GetComponentsInChildren<GUIBase_Element>();
			foreach (var child in children)
			{
				if (child == this)
					continue;

				if (child.Visible == false)
					continue;

				child.SetModify(false, false);
			}
		}
	}

	public float GetFadeAlpha(bool recursive)
	{
		return recursive == true ? m_FadeAlpha*GetParentFadeAlpha() : m_FadeAlpha;
	}

	public float GetParentFadeAlpha()
	{
		return m_Parent != null ? m_Parent.GetFadeAlpha(true) : 1.0f;
	}

	public void GUIUpdate(float parentAlpha)
	{
		if (IsVisible() == false)
			return;

		OnGUIUpdate(parentAlpha);
		m_IsDirty = false;
	}

	public Rect GetBBox()
	{
		Rect bbox = new Rect();
		GUIBase_Widget[] children = GetComponentsInChildren<GUIBase_Widget>();
		foreach (var child in children)
		{
			Rect rect = child.GetRectInScreenCoords();
			if (bbox.width == 0 || bbox.height == 0)
			{
				bbox = rect;
			}
			else
			{
				bbox = bbox.Union(rect);
			}
		}
		return bbox;
	}

	// MONOBEHAVIOUR INTERFACE

	protected void Start()
	{
		// deduce parent
		Transform parentTransform = transform.parent;
		m_Parent = parentTransform ? parentTransform.GetComponent<GUIBase_Element>() : null;

		// inform sub-classes that we started
		System.Type type = this.GetType();
		System.Reflection.MethodInfo method = type.GetMethod("OnElementStart");
		if (method != null)
		{
			method.Invoke(this, null);
		}
	}

	// PRIVATE METHODS

	protected void NotifyLayoutChanged(bool propagateDownward, bool propagateUpward)
	{
		OnLayoutChanged();

		if (propagateUpward == true && Parent != null)
		{
			Parent.NotifyLayoutChanged(false, true);
		}

		if (propagateDownward == true)
		{
			GUIBase_Element[] children = GetComponentsInChildren<GUIBase_Element>();
			foreach (var child in children)
			{
				if (child != this)
				{
					child.NotifyLayoutChanged(false, false);
				}
			}
		}
	}
}
