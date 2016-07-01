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
using LitJson;

public enum FtueState
{
	None,
	InProgress,
	Finished,
	Skipped
}

public class Ftue : MonoBehaviour
{
	static Ftue m_Instance;

	public static Ftue Instance
	{
		get
		{
			if (m_Instance == null && Game.Instance != null)
			{
				m_Instance = Game.Instance.gameObject.AddComponent<Ftue>();
			}
			return m_Instance;
		}
	}

	// PRIVATE MEMBERS

	List<FtueAction.Base> m_Actions = new List<FtueAction.Base>();
	FtueAction.Base m_ActiveAction = null;
	FtueAction.Base m_PendingAction = null;
	int m_ActionsIdle = 0;
	int m_ActionsFinished = 0;
	int m_ActionsSkipped = 0;
	FtueAction.Base m_CachedAction = null;

	// PUBLIC MEMBERS

	public static bool IsActive
	{
		get { return !IsFinished; }
	}

	public static bool IsFinished
	{
		get { return ActiveAction == null && ActionsIdle == 0 ? true : false; }
	}

	public static FtueAction.Base ActiveAction
	{
		get { return Instance != null ? m_Instance.m_ActiveAction : default(FtueAction.Base); }
	}

	public static FtueAction.Base PendingAction
	{
		get { return Instance != null ? m_Instance.m_PendingAction : default(FtueAction.Base); }
	}

	public static int ActionsTotal
	{
		get { return Instance != null ? m_Instance.m_Actions.Count : 0; }
	}

	public static int ActionsIdle
	{
		get { return Instance != null ? m_Instance.m_ActionsIdle : 0; }
	}

	public static int ActionsFinished
	{
		get { return Instance != null ? m_Instance.m_ActionsFinished : 0; }
	}

	public static int ActionsSkipped
	{
		get { return Instance != null ? m_Instance.m_ActionsSkipped : 0; }
	}

	public static string NextActionHint
	{
		get
		{
			if (ActiveAction != null)
				return null;
			FtueAction.Base action = PendingAction;
			if (action == null)
				return null;
			return action.CanActivate() == false ? action.HintText() : null;
		}
	}

	// PUBLIC METHODS

	public static bool IsActionIdle<T>() where T : FtueAction.Base
	{
		if (Instance == null)
			return false;
		if (IsActive == false)
			return false;

		FtueAction.Base action = m_Instance.GetAction(typeof (T));
		if (action == null)
			return false;

		FtueAction.Base current = ActiveAction;
		if (current == null)
			return action.IsIdle;

		if (action.Index > current.Index)
			return true;

		return action.IsIdle;
	}

	public static bool IsActionActive<T>() where T : FtueAction.Base
	{
		if (Instance == null)
			return false;
		if (IsActive == false)
			return false;

		FtueAction.Base action = m_Instance.GetAction(typeof (T));
		if (action == null)
			return false;

		FtueAction.Base current = ActiveAction;
		if (current == null)
			return false;

		if (action.Index == current.Index)
			return true;

		return action.IsActive;
	}

	public static bool IsActionFinished<T>() where T : FtueAction.Base
	{
		if (Instance == null)
			return true;
		if (IsActive == false)
			return true;

		FtueAction.Base action = m_Instance.GetAction(typeof (T));
		if (action == null)
			return true;

		bool finished = action.IsSkipped || action.IsFinished;

		FtueAction.Base current = ActiveAction;
		if (current == null)
			return finished;

		if (action.Index > current.Index)
			return false;

		return finished;
	}

	public static void SkipAll()
	{
		if (Instance == null)
			return;
		m_Instance.m_Actions.ForEach((obj) => { obj.Skip(); });
	}

	public static void RegisterMenu(GuiMenu menu)
	{
	}

	public static void UnregisterMenu(GuiMenu menu)
	{
		if (Instance == null)
			return;
		m_Instance.DeactivateAction(m_Instance.m_ActiveAction);
	}

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		// FirstTime
		RegisterAction(new FtueAction.Welcome(this));
		RegisterAction(new FtueAction.Profile(this));
		RegisterAction(new FtueAction.DeathMatch(this));

		// FirstGame
		RegisterAction(new FtueAction.Spawn(this));
		RegisterAction(new FtueAction.Hud(this));
		RegisterAction(new FtueAction.Controls(this));

		// FirstProgress
		RegisterAction(new FtueAction.Stats(this)); // show stats ...
		RegisterAction(new FtueAction.Leaderboards(this)); // show leaderboards ...
		RegisterAction(new FtueAction.Shop(this)); // show shop ...
		RegisterAction(new FtueAction.RankUp(this, 1, 2, 9960071, 9960072)); // play until rank 2 ...

		// FirstShopping
		RegisterAction(new FtueAction.Research(this, 2, GuiShop.E_ItemType.Weapon, (int)E_WeaponID.AR1)); // buy new gun ...
		RegisterAction(new FtueAction.Equip(this, 2, GuiShop.E_ItemType.Weapon, (int)E_WeaponID.AR1)); // equip new gun ...
		RegisterAction(new FtueAction.RankUp(this, 2, FtueAction.ZoneControl.DefaultMinimalRank, 9960701, 9960702)); // play until rank 4 ...

		// FirstZoneControl
		RegisterAction(new FtueAction.ZoneControl(this, FtueAction.ZoneControl.DefaultMinimalRank));
		RegisterAction(new FtueAction.Friends(this, FtueAction.ZoneControl.DefaultMinimalRank)); // show friendlist ...
		RegisterAction(new FtueAction.Chat(this, FtueAction.ZoneControl.DefaultMinimalRank)); // show chat / pm

		// Done
		RegisterAction(new FtueAction.FinalText(this, FtueAction.ZoneControl.DefaultMinimalRank));
	}

	void Start()
	{
		CloudUser.authenticationChanged += OnUserAuthenticationChanged;
		UserSettings.SettingsLoaded += OnSettingsLoaded;
		UserSettings.SettingsSaving += OnSettingsSaving;
	}

	void OnDestroy()
	{
		while (m_Actions.Count > 0)
		{
			UnregisterAction(m_Actions[0]);
		}

		CloudUser.authenticationChanged -= OnUserAuthenticationChanged;
		UserSettings.SettingsLoaded -= OnSettingsLoaded;
		UserSettings.SettingsSaving -= OnSettingsSaving;

		m_Instance = null;
	}

	void Update()
	{
		m_PendingAction = null;
		m_ActionsIdle = 0;
		m_ActionsFinished = 0;
		m_ActionsSkipped = 0;

		if (m_ActiveAction != null && m_ActiveAction.IsActive == false)
		{
			DeactivateAction(m_ActiveAction);
		}

		bool activate = true;
		for (int idx = 0; idx < m_Actions.Count; ++idx)
		{
			FtueAction.Base action = m_Actions[idx];

			switch (action.State)
			{
			case FtueState.None:
			case FtueState.InProgress:
				if (activate == true)
				{
					ActivateAction(action);
					activate = false;
				}
				if (m_PendingAction == null && action.IsIdle == true)
				{
					m_PendingAction = action;
				}
				break;
			case FtueState.Finished:
			case FtueState.Skipped:
				DeactivateAction(action);
				break;
			}
		}

		for (int idx = 0; idx < m_Actions.Count; ++idx)
		{
			FtueAction.Base action = m_Actions[idx];

			switch (action.State)
			{
			case FtueState.None:
				m_ActionsIdle += 1;
				break;
			case FtueState.Finished:
				m_ActionsFinished += 1;
				break;
			case FtueState.Skipped:
				m_ActionsSkipped += 1;
				break;
			case FtueState.InProgress:
				break;
			}
		}
	}

	// HANDLERS

	void OnUserAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			OnSettingsLoaded(Game.Settings);
		}
		else
		{
			OnSettingsSaving(Game.Settings);

			DeactivateAction(m_ActiveAction);
		}

		enabled = state;
	}

	void OnSettingsLoaded(UserSettings settings)
	{
		string json = settings.GetString("ftue.actions", "");
		JsonData data = JsonMapper.ToObject(json) ?? new JsonData();

		foreach (var action in m_Actions)
		{
			string key = action.UniqueId;
			action.Load(data.HasValue(key) ? data[key] : new JsonData(JsonType.Object));
		}
	}

	void OnSettingsSaving(UserSettings settings)
	{
		JsonData data = new JsonData(JsonType.Object);

		foreach (var action in m_Actions)
		{
			string key = action.UniqueId;
			data[key] = action.Save();
		}

		string json = data.ToJson();
		settings.SetString("ftue.actions", json);
	}

	// PRIVATE METHODS

	void RegisterAction(FtueAction.Base action)
	{
		if (action == null)
			return;
		if (m_Actions.Contains(action) == true)
			return;

		if (UserGuide.RegisterAction(action) == true)
		{
			m_Actions.Add(action);

			DeactivateAction(m_ActiveAction);
		}
	}

	void UnregisterAction(FtueAction.Base action)
	{
		if (action == null)
			return;
		if (m_Actions.Contains(action) == false)
			return;

		UserGuide.UnregisterAction(action);

		m_Actions.Remove(action);

		DeactivateAction(m_ActiveAction);
	}

	void ActivateAction(FtueAction.Base action)
	{
		if (m_ActiveAction != null)
			return;
		if (m_ActiveAction == action)
			return;

		if (action == null)
			return;
		if (action.IsSkipped == true)
			return;
		if (action.IsFinished == true)
			return;

		if (action.CanActivate() == false)
			return;

		m_ActiveAction = action;

		m_ActiveAction.Activate();
	}

	void DeactivateAction(FtueAction.Base action)
	{
		if (m_ActiveAction != action)
			return;

		if (m_ActiveAction != null)
		{
			action.Deactivate();
		}

		m_ActiveAction = null;
	}

	FtueAction.Base GetAction(System.Type type)
	{
		if (m_CachedAction != null && m_CachedAction.GetType() == type)
			return m_CachedAction;

		m_CachedAction = m_Actions.Find(obj => obj.GetType() == type);

		return m_CachedAction;
	}
}
