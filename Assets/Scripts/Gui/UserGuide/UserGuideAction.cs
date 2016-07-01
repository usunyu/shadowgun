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

public abstract class UserGuideAction
{
	// PRIVATE MEMBERS

	bool m_IsExecutable = true;
	static int m_NextFreeIndex = 0;

	// PUBLIC MEMBERS

	public bool IsInitialized
	{
		get { return GuideData != null ? true : false; }
	}

	public bool IsExecutable
	{
		get
		{
			if (IsInitialized == false)
				return false;
			if (AllowRepeatedExecution == false && NumberOfExecutions > 0)
				return false;
			return m_IsExecutable;
		}
		protected set { m_IsExecutable = value; }
	}

	public bool IsExecuted { get; private set; }
	public bool AllowRepeatedExecution { get; protected set; }
	public int NumberOfExecutions { get; private set; }
	public int Priority { get; set; }
	public int Index { get; private set; }
	public UserGuideData GuideData { get; private set; }

	// ABSTRACT INTERFACE

	protected virtual bool OnInitialize()
	{
		return true;
	}

	protected virtual void OnDeinitialize()
	{
	}

	protected abstract bool OnExecute();

	protected virtual bool OnUpdate()
	{
		return true;
	}

	protected virtual void OnTerminate()
	{
	}

	protected virtual void OnReset()
	{
	}

	// C-TOR

	protected UserGuideAction()
	{
		Index = m_NextFreeIndex;
		m_NextFreeIndex++;
	}

	// INTERNAL METHODS used by UserGuide

	internal bool Initialize(UserGuideData data)
	{
		if (IsInitialized == true)
			return true;

		GuideData = data;

		Reset();

		if (OnInitialize() == false)
		{
			Deinitialize(data);

			return false;
		}

		return true;
	}

	internal void Deinitialize(UserGuideData data)
	{
		if (IsInitialized == false)
			return;

		OnDeinitialize();

		Terminate();

		GuideData = null;
	}

	internal void Execute()
	{
		if (IsExecutable == false)
			return;
		if (IsExecuted == true)
			return;

		IsExecuted = true;

		if (OnExecute() == true)
		{
			NumberOfExecutions += 1;
		}
		else
		{
			Terminate();
		}
	}

	internal void Update()
	{
		if (IsExecuted == false)
			return;

		OnUpdate();
	}

	internal void Terminate()
	{
		if (IsExecuted == false)
			return;

		OnTerminate();

		IsExecuted = false;
	}

	internal void Reset()
	{
		Terminate();

		NumberOfExecutions = 0;

		OnReset();
	}

	// PROTECTED METHODS

	protected void StoreBool(string key, bool value)
	{
		string token = ConstructKey(key);
		Game.Settings.SetBool(token, value);
	}

	protected bool RestoreBool(string key, bool defaultValue)
	{
		string token = ConstructKey(key);
		return Game.Settings.GetBool(token, defaultValue);
	}

	protected void StoreString(string key, string value)
	{
		string token = ConstructKey(key);
		Game.Settings.SetString(token, value);
	}

	protected string RestoreString(string key, string defaultValue)
	{
		string token = ConstructKey(key);
		return Game.Settings.GetString(token, defaultValue);
	}

	// PRIVATE MEMBERS

	string ConstructKey(string key)
	{
		return string.Format("{0}.{1}", GetType().Name, key);
	}
}
