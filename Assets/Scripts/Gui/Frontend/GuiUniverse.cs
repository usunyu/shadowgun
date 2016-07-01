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

public class GuiUniverse : MonoBehaviour
{
	public static GuiUniverse Instance;

	readonly static string PLAY_BUTTON_NAME = "Buttons/MainMenubuttonPlay";
	readonly static string BANK_BUTTON_NAME = "Buttons/MainMenubuttonBank";
	readonly static string POST_BUTTON_NAME = "Buttons/MainMenubuttonPost";

	enum E_Universe
	{
		Empty,
		Free,
		Premium
	}

	// PRIVATE MEMBERS

	[SerializeField] GameObject m_UniverseEmpty;
	[SerializeField] GameObject m_UniverseFree;
	[SerializeField] GameObject m_UniversePremium;

	IViewOwner m_Owner;
	bool m_IsVisible;
	E_Universe m_Universe;
	bool m_MouseButtonPressed;
	GuiInputUniverse m_InputController = new GuiInputUniverse();

	Collider m_PlayButton;
	Collider m_BankButton;
	Collider m_PostButton;

	// GETTERS/SETTERS

	public GameObject ActiveUniverse
	{
		get
		{
			switch (m_Universe)
			{
			case E_Universe.Empty:
				return m_UniverseEmpty;
			case E_Universe.Free:
				return m_UniverseFree;
			case E_Universe.Premium:
				return m_UniversePremium;
			default:
				throw new System.IndexOutOfRangeException();
			}
		}
	}

	// PUBLIC METHODS

	public void Init(IViewOwner owner)
	{
		m_Owner = owner;

		// setup input
		m_InputController.OnProcessInput += OnProcessInput;
		InputManager.Register(m_InputController);
	}

	public void Show()
	{
		if (m_IsVisible == true)
			return;
		m_IsVisible = true;

		RefreshActiveUniverse(true);
	}

	public void Hide()
	{
		if (m_IsVisible == false)
			return;
		m_IsVisible = false;

		RefreshActiveUniverse(true);
	}

	public void Enable()
	{
		if (m_InputController.CaptureInput == true)
			return;
		m_InputController.CaptureInput = true;

		ActivateButtons(true);
	}

	public void Disable()
	{
		if (m_InputController.CaptureInput == false)
			return;
		m_InputController.CaptureInput = false;

		ActivateButtons(false);
	}

	// MONOBEHAVIOUR INTERFACE

	void Awake()
	{
		Instance = this;

		CloudUser.premiumAcctChanged += OnUserPremiumAcctChanged;

		if (m_UniverseEmpty != null)
		{
			m_UniverseEmpty.SetActive(false);
		}

		if (m_UniverseFree != null)
		{
			m_UniverseFree.SetActive(false);
		}

		if (m_UniversePremium != null)
		{
			m_UniversePremium.SetActive(false);
		}

		RefreshActiveUniverse(true);
	}

	void OnDestroy()
	{
		CloudUser.premiumAcctChanged -= OnUserPremiumAcctChanged;
	}

	// HANDLERS

	void OnUserPremiumAcctChanged(bool state)
	{
		RefreshActiveUniverse(false);
	}

	// PRIVATE METHODS

	void RefreshActiveUniverse(bool force)
	{
		if (m_IsVisible == false)
		{
			SetActiveUniverse(E_Universe.Empty, force);
		}
		else
		{
			bool isPremiumAccountActive = CloudUser.instance.isPremiumAccountActive;
			SetActiveUniverse(isPremiumAccountActive == false ? E_Universe.Free : E_Universe.Premium, force);
		}
	}

	void SetActiveUniverse(E_Universe universe, bool force)
	{
		if (m_Universe == universe && force == false)
			return;

		if (ActiveUniverse != null)
		{
			ActiveUniverse.SetActive(false);
			ActivateButtons(false);
		}

		m_Universe = universe;

		if (ActiveUniverse != null)
		{
			ActiveUniverse.SetActive(true);
			ActivateButtons(m_InputController.CaptureInput);
		}
	}

	void ActivateButtons(bool state)
	{
		m_PlayButton = ActivateButton(PLAY_BUTTON_NAME, state);
		m_BankButton = ActivateButton(BANK_BUTTON_NAME, state);
		m_PostButton = ActivateButton(POST_BUTTON_NAME, state);
	}

	Collider ActivateButton(string name, bool state)
	{
		Collider button = GetButton(name);
		if (button != null)
		{
			button.gameObject.SetActive(state);
			button.enabled = state;
		}
		return state == true ? button : null;
	}

	Collider GetButton(string name)
	{
		Transform trans = ActiveUniverse != null ? ActiveUniverse.transform.Find(name) : null;
		return trans != null ? trans.GetComponent<Collider>() : null;
	}

	bool OnProcessInput(ref IInputEvent evt)
	{
		if (evt.Kind != E_EventKind.Touch)
			return false;

		TouchEvent touch = (TouchEvent)evt;
		if (touch.Phase != TouchPhase.Ended)
			return false;

		return HitTest(touch.Position);
	}

	bool HitTest(Vector2 point)
	{
		Ray ray = Camera.main.ScreenPointToRay(point);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			if (hit.collider == null)
				return false;

			if (m_PlayButton == hit.collider)
			{
				return OnPlayPressed();
			}
			else if (m_BankButton == hit.collider)
			{
				return OnBankPressed();
			}
			else if (m_PostButton == hit.collider)
			{
				return OnPostPressed();
			}
		}

		return false;
	}

	bool OnPlayPressed()
	{
		m_Owner.ShowScreen("PlayServer");
		return true;
	}

	bool OnBankPressed()
	{
		m_Owner.ShowScreen("Bank");
		return true;
	}

	bool OnPostPressed()
	{
		m_Owner.DoCommand("Post");
		return true;
	}
}
