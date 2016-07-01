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

public class BaseListView : MonoBehaviour
{
	public virtual void GUIView_Show()
	{
	}

	public virtual void GUIView_Hide()
	{
	}

	public virtual void GUIView_Update()
	{
	}

	public virtual GUIBase_Widget GUIView_HitTest(ref Vector2 point)
	{
		return null;
	}

	public virtual bool GUIView_ProcessInput(ref IInputEvent evt)
	{
		return false;
	}

//	// Use this for initialization
//	void Start () {
//	
//	}
//	
//	// Update is called once per frame
//	void Update () {
//	
//	}
}

/*
public abstract class ListView<T> : BaseListView where T: class
{
	private T item;
	
	protected abstract T 	GetNewTItem { get; }
}
*/
