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

public enum E_UserGuidePriority
{
	BanMessage = -900, //NOTE: this should be the first action ever!

	Welcome = -801,
	NewFeatures = -800,

	ResetResearch = -600,

	Default = 0,

	FinalResults = 500,
	PlayerEarnings = 501,
	Promotion = 502,
	UnlockedItems = 503,

	Tutorial = 700,

	DailyRewards = 800,

	Offers = 900,
	Post = 901,
}

public class UserGuideData
{
	public string PrimaryKey = "default";
	public GuiMenu Menu = null;
	public PlayerPersistantInfo LocalPPI = null;
	public RoundFinalResult LastRoundResult = null;

	public bool ShowOffers = true;
}

public class UserGuide : MonoBehaviour
{
	static UserGuide m_Instance;

	public static UserGuide Instance
	{
		get
		{
			if (m_Instance == null && Game.Instance != null)
			{
				m_Instance = Game.Instance.gameObject.AddComponent<UserGuide>();
			}
			return m_Instance;
		}
	}

	static float FIRST_EXECUTION_DELAY = 0.5f;
	static float NEXT_EXECUTION_DELAY = 0.1f;
	static float NEXT_REPEAT_CYCLE_DELAY = 0.5f;

	// PRIVATE MEMBERS

	UserGuideData m_GuideData = new UserGuideData();
	List<UserGuideAction> m_Actions = new List<UserGuideAction>();
	UserGuideAction m_ActiveAction = null;
	bool m_IsDirty = true;

	// GETTERS/SETTERS

	public static bool IsActive
	{
		get { return Instance.m_GuideData.PrimaryKey != null ? true : false; }
	}

	public static UserGuideAction ActiveAction
	{
		get { return Instance.m_ActiveAction; }
	}

	public static bool InitialSequenceFinished { get; private set; }

	// PUBLIC MEMBERS

	public static void RegisterMenu(GuiMenu menu)
	{
		if (menu == null)
			return;

		// ensure WILL create new instance here
		if (Instance == null)
			return;
		if (m_Instance.m_GuideData.Menu != null)
			return;

		m_Instance.m_GuideData.Menu = menu;

		m_Instance.m_IsDirty = true;
	}

	public static void UnregisterMenu(GuiMenu menu)
	{
		if (menu == null)
			return;

		// ensure WILL NOT create new instance here
		if (m_Instance == null)
			return;
		if (m_Instance.m_GuideData.Menu != menu)
			return;

		m_Instance.m_GuideData.Menu = null;

		m_Instance.m_IsDirty = true;
	}

	public static bool RegisterAction(UserGuideAction action)
	{
		if (action == null)
			return false;

		// ensure WILL create new instance here
		if (Instance == null)
			return false;
		if (m_Instance.m_Actions.Contains(action) == true)
			return false;

		m_Instance.m_Actions.Add(action);

		m_Instance.m_IsDirty = true;

		return true;
	}

	public static void UnregisterAction(UserGuideAction action)
	{
		if (action == null)
			return;

		// ensure WILL NOT create new instance here
		if (m_Instance == null)
			return;

		m_Instance.m_Actions.Remove(action);

		m_Instance.m_IsDirty = true;
	}

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		CloudUser.authenticationChanged += OnUserAuthenticationChanged;
	}

	void Start()
	{
		// system dialogs
		RegisterAction(new UserGuideAction_BanMessage());
		RegisterAction(new UserGuideAction_Welcome());
		//RegisterAction(new UserGuideAction_NewFeatures());
		RegisterAction(new UserGuideAction_ResetResearch());

		// last round results
		RegisterAction(new UserGuideAction_Promotion());
		RegisterAction(new UserGuideAction_UnlockedItems());

		// offers
		RegisterAction(new UserGuideAction_Offers());
	}

	void OnDestroy()
	{
		CloudUser.authenticationChanged -= OnUserAuthenticationChanged;
		OnUserAuthenticationChanged(false);

		m_Instance = null;
	}

	void Update()
	{
		if (IsActive == false)
			return;
		if (m_IsDirty == false)
			return;
		m_IsDirty = false;

		// stop all currently running coroutines
		StopAllCoroutines();

		// prepare actions
		DeinitializeActions();
		UpdateGuideData();
		InitializeActions();

		// execute actions
		StartCoroutine(ExecuteActions_Coroutine());
	}

	// HANDLERS

	void OnUserAuthenticationChanged(bool state)
	{
		if (state == true)
		{
			Activate(CloudUser.instance.primaryKey);
		}
		else
		{
			Deactivate(m_GuideData.PrimaryKey);
		}
	}

	// COROUTINES

	IEnumerator ExecuteActions_Coroutine()
	{
		List<UserGuideAction> banPass = new List<UserGuideAction>();
		List<UserGuideAction> firstPass = new List<UserGuideAction>();
		List<UserGuideAction> secondPass = new List<UserGuideAction>();

		// collect actions for each pass
		foreach (var action in m_Actions)
		{
			if (action == null)
				continue;

			if (action.Priority == (int)E_UserGuidePriority.BanMessage)
			{
				banPass.Add(action);
				secondPass.Add(action);
			}
			else if (action.Priority < (int)E_UserGuidePriority.Default)
			{
				firstPass.Add(action);
			}
			else
			{
				secondPass.Add(action);
			}
		}

		// ban pass
		if (banPass.Count > 0)
		{
			yield return StartCoroutine(ExecuteActions_Coroutine(banPass.ToArray()));
		}

		// run initial pass
		if (InitialSequenceFinished == false)
		{
			yield return new WaitForSeconds(FIRST_EXECUTION_DELAY);

			yield return StartCoroutine(ExecuteActions_Coroutine(firstPass.ToArray()));

			InitialSequenceFinished = true;
		}
		else
		{
			yield return new WaitForSeconds(NEXT_EXECUTION_DELAY);
		}

		// run looped pass
		while (secondPass.Count > 0)
		{
			yield return StartCoroutine(ExecuteActions_Coroutine(secondPass.ToArray()));

			// reset last round result
			if (GuiFrontendMain.IsVisible == true)
			{
				Game.Instance.LastRoundResult = null;
			}

			// remove actions which no longer support repeated execution
			for (int idx = secondPass.Count - 1; idx >= 0; --idx)
			{
				UserGuideAction action = secondPass[idx];
				if (action == null || action.AllowRepeatedExecution == false)
				{
					secondPass.RemoveAt(idx);
				}
			}

			// wait for a while before next loop
			yield return new WaitForSeconds(NEXT_REPEAT_CYCLE_DELAY);
		}
	}

	IEnumerator ExecuteActions_Coroutine(UserGuideAction[] actions)
	{
		foreach (var action in actions)
		{
			if (action == null)
				continue;

			if (action.IsExecutable == false)
				continue;

			action.Execute();
			if (action.IsExecuted == false)
				continue;

			m_ActiveAction = action;

			while (action.IsExecuted == true)
			{
				action.Update();

				yield return new WaitForEndOfFrame();
			}

			yield return new WaitForSeconds(NEXT_EXECUTION_DELAY);

			m_ActiveAction = null;
		}
	}

	// PRIVATE METHODS

	void Activate(string primaryKey)
	{
		if (m_GuideData.PrimaryKey == primaryKey)
			return;
		if (string.IsNullOrEmpty(primaryKey) == true)
			return;

		m_GuideData.PrimaryKey = primaryKey;
		InitialSequenceFinished = false;

		m_IsDirty = true;
	}

	void Deactivate(string primaryKey)
	{
		if (m_GuideData.PrimaryKey != primaryKey)
			return;
		if (string.IsNullOrEmpty(m_GuideData.PrimaryKey) == true)
			return;

		StopAllCoroutines();

		DeinitializeActions();

		m_GuideData.PrimaryKey = null;

		m_IsDirty = true;
	}

	void UpdateGuideData()
	{
		m_GuideData.LocalPPI = PPIManager.Instance.GetLocalPPI();
		m_GuideData.LastRoundResult = Game.Instance.LastRoundResult;
		m_GuideData.ShowOffers = GuiFrontendMain.IsVisible;
	}

	void InitializeActions()
	{
		// sort actions by priority
		m_Actions.Sort((x, y) =>
					   {
						   int res = x.Priority.CompareTo(y.Priority);
						   return res != 0 ? res : x.Index.CompareTo(y.Index);
					   });

		// initialize actions
		foreach (var action in m_Actions)
		{
			if (action != null)
			{
				action.Initialize(m_GuideData);
			}
		}
	}

	void DeinitializeActions()
	{
		// deinitialize actions
		foreach (var action in m_Actions)
		{
			if (action != null)
			{
				action.Deinitialize(m_GuideData);
			}
		}
	}
}
