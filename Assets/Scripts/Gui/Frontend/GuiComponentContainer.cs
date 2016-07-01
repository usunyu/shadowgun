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

public class GuiComponentContainer<S, T>
				where T : Component
{
	public class Pairs : Dictionary<S, GuiComponent<T>>
	{
	}

	// PRIVATE MEMBERS

	Pairs m_Pairs = new Pairs();

	// GETTERS / SETTER

	public GuiComponent<T> this[S id]
	{
		get { return Get(id); }
	}

	public Pairs.KeyCollection Ids
	{
		get { return m_Pairs.Keys; }
	}

	public Pairs.ValueCollection Components
	{
		get { return m_Pairs.Values; }
	}

	// PUBLIC METHODS

	public N Create<N>(S id, T owner) where N : GuiComponent<T>, new()
	{
		if (m_Pairs.ContainsKey(id) == true)
		{
			Debug.LogError("GuiComponentContainer<" + typeof (T).Name + ">.Create() :: Attempt to create duplicated component " + typeof (N).Name +
						   "<" + id + ">");
			return null;
		}

		N component = new N();
		if (component == null)
		{
			Debug.LogError("GuiComponentContainer<" + typeof (T).Name + ">.Create() :: Can't create component " + typeof (N).Name + "<" + id + ">");
			return null;
		}

		component.Init(owner);
		if (component.IsInitialized == false)
		{
			Debug.LogError("GuiComponentContainer<" + typeof (T).Name + ">.Create() :: Can't initialize component " + typeof (N).Name + "<" + id +
						   ">");
			return null;
		}

		m_Pairs[id] = component;

		return component;
	}

	public GuiComponent<T> Get(S id)
	{
		GuiComponent<T> component = null;
		m_Pairs.TryGetValue(id, out component);
		return component;
	}

	public N Get<N>() where N : GuiComponent<T>
	{
		System.Type type = typeof (N);
		foreach (var pair in m_Pairs)
		{
			if (pair.Value.GetType() == type)
				return pair.Value as N;
		}
		return null;
	}

	public void Destroy(T owner)
	{
		//Debug.Log( typeof(T).Name  + " Destryo count of components " + m_Pairs.Count );
		foreach (var pair in m_Pairs)
		{
			pair.Value.Destroy(owner);
		}

		m_Pairs.Clear();
	}

	public void Show()
	{
		foreach (var pair in m_Pairs)
		{
			pair.Value.Show();
		}
	}

	public void Hide()
	{
		foreach (var pair in m_Pairs)
		{
			pair.Value.Hide();
		}
	}

	public void Update()
	{
		foreach (var pair in m_Pairs)
		{
			pair.Value.Update();
		}
	}

	public void LateUpdate()
	{
		foreach (var pair in m_Pairs)
		{
			pair.Value.LateUpdate();
		}
	}
}
