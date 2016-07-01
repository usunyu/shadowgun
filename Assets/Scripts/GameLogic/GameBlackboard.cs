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
using System;

[Serializable]
[ExecuteInEditMode]
public class GameEvents
{
	public delegate void EventHandler(string Name, E_State state);

	public enum E_State
	{
		False = 0,
		True = 1,
	}

	[SerializeField] List<string> _Names = new List<string>();
	[SerializeField] List<E_State> _States = new List<E_State>();

	Dictionary<string, EventHandler> EventHandlers = new Dictionary<string, EventHandler>();

	public List<string> Names
	{
		get { return _Names; }
	}

	public int Count
	{
		get { return _Names.Count; }
	}

	public void Clear()
	{
		_Names.Clear();
		_States.Clear();

		Add("RESET", E_State.False);
	}

	public bool Add(string name, E_State state)
	{
		if (_Names.Contains(name))
			return false;

		_Names.Add(name);
		_States.Add(state);

		return true;
	}

	public void Remove(string name)
	{
		int i = FindIndex(name);

		if (i == -1)
			return;

		_Names.RemoveAt(i);
		_States.RemoveAt(i);
	}

	public void Update(string name, E_State state)
	{
		int i = FindIndex(name);

		if (i == -1)
			return;

		_States[i] = state;

		if (Application.isPlaying && EventHandlers.ContainsKey(name))
		{
			EventHandlers[name](name, state);
		}
	}

	public E_State GetState(string name)
	{
		int i = FindIndex(name);

		if (i == -1)
			return E_State.False;

		if (i > _States.Count - 1)
			return E_State.False;

		return _States[i];
	}

	int FindIndex(String s)
	{
		for (int i = 0; i < _Names.Count; i++)
			if (_Names[i] == s)
				return i;

		return -1;
	}

	public bool Exist(String inEventName)
	{
		return (FindIndex(inEventName) != -1);
	}

	List<string> EventNames
	{
		get { return _Names; }
	}

	public void AddEventChangeHandler(string name, EventHandler handler)
	{
		if (_Names.Contains(name) == false)
		{
			Debug.LogError("GameEvents dont contact event " + name);
			return;
		}

		if (EventHandlers.ContainsKey(name))
			EventHandlers[name] += handler;
		else
			EventHandlers.Add(name, handler);
	}

	public void RemoveEventChangeHandler(string name, EventHandler handler)
	{
		if (_Names.Contains(name) == false)
		{
			Debug.LogError("GameEvents dont contact event " + name);
			return;
		}

		if (EventHandlers.ContainsKey(name))
			EventHandlers[name] -= handler;

		if (EventHandlers[name] == null)
			EventHandlers.Remove(name);
	}

	public void Save_Save()
	{
		//Debug.Log(Time.timeSinceLevelLoad + "SAVE - save BB");

		for (int i = 0; i < _States.Count; i++)
		{
			PlayerPrefs.SetInt(Game.Instance.GameType + "GB" + i, (int)_States[i]);
		}
	}

	public void Save_Load()
	{
		//Debug.Log(Time.timeSinceLevelLoad + "SAVE - load BB");

		for (int i = 0; i < _States.Count; i++)
		{
			_States[i] = (E_State)PlayerPrefs.GetInt(Game.Instance.GameType + "GB" + i, 0);
		}
	}
}

[Serializable]
[ExecuteInEditMode]
public class GameBlackboard : MonoBehaviour
{
	[SerializeField] GameEvents _GameEvents = new GameEvents();

	public GameEvents GameEvents
	{
		get { return _GameEvents; }
	}

	public int NumberOfGameEvents
	{
		get { return GameEvents.Count; }
	}

	public static GameBlackboard Instance { get; set; }

	void Awake()
	{
		if (Application.isPlaying)
		{
			if (Instance)
			{
				Destroy(this);
				return;
			}
		}
		Instance = this;

		GameEvents.Add("RESET", global::GameEvents.E_State.False);
	}

	// Use this for initialization
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
	}

	public void Save_Save()
	{
		_GameEvents.Save_Save();
	}

	public void Save_Load()
	{
		_GameEvents.Save_Load();
	}

	public void Save_Clear()
	{
	}
}
