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

class HudMessageBox
{
	GUIBase_Pivot m_Pivot;
	GUIBase_Layout m_Layout;
	GUIBase_Label m_Label;

	public void InitGui()
	{
		m_Pivot = MFGuiManager.Instance.GetPivot("MessageBox");
		m_Layout = m_Pivot.GetLayout("MessageBox_Layout");
		m_Label = GuiBaseUtils.PrepareLabel(m_Layout, "MessageBox_Label");
	}

	public void Show(string message)
	{
		MFGuiManager.Instance.ShowLayout(m_Layout, true);
		m_Label.SetNewText(message);
	}

	public void Hide()
	{
		MFGuiManager.Instance.ShowLayout(m_Layout, false);
	}
};

[AddComponentMenu("GUI/Menu/GuiHUD")]
public class GuiHUD : MonoBehaviour, IViewOwner
{
	public class FloatingFireButtonInteractingWidget
	{
		public readonly GUIBase_Button Button;

		public FloatingFireButtonInteractingWidget(GUIBase_Button button)
		{
			Button = button;

			// Debug.Log( "### FFB: name=" + Button.name + " scales=" + Button.m_TouchableAreaHeightScale + " " +  Button.m_TouchableAreaWidthScale + " TouchRect=" + Button.GetTouchRect() );
		}

		public bool ShowFFBWhenAimingReleased(Vector3 pos)
		{
			if (!Button.Widget.Visible)
				return true;

			Rect rect = Button.GetTouchRect();
			return !rect.Contains(pos);
		}
	};

	List<FloatingFireButtonInteractingWidget> FFBInteractingWidgets = new List<FloatingFireButtonInteractingWidget>();

	public enum E_ActionButton
	{
		None,
		Fire,
		Use,
	}

	enum E_HudWidget
	{
		Pause = 0,
		Move,
		Fire,
		Use,
		Reload,
		Roll,
		Sprint,
		COUNT,
	}

	enum E_HudComponent
	{
		Crosshair,
		Weapon,
		Console,
		CombatInfo,
		Radar,
		Gadgets,
		CommandMenu,
		DeathMatchState,
		DominationState,
		TeammatesInfo,
		ZoneInfo,
		Max,
	}

	public static GuiHUD Instance;
	public static float InfoMessageTime = 5.0f;

	//int 					m_GuiSelectedWeaponIndex;
	public bool IsVisible { get; private set; }
	public bool IsInitialized { get; private set; }
	public ComponentPlayerLocal LocalPlayer { get; private set; }

	int timedMessageTimer = -1;

	GuiInputHUD m_InputController = new GuiInputHUD();

	GUIBase_Pivot m_PivotMain;
	GUIBase_Layout m_LayoutMain;
	GUIBase_Button m_PauseButton;
	GUIBase_Sprite m_DPad;
	GUIBase_Sprite m_DPadjoy;
	GUIBase_Button m_AttackButton;
	GUIBase_Button m_ReloadButton;
	GUIBase_Button m_RollButton;
	GUIBase_Button m_SprintButton;
	GUIBase_Button m_Feedback_Button;
	GUIBase_Sprite m_Connection;
	GUIBase_MultiSprite m_ConnectionIcon;
	GUIBase_Button m_AnticheatButton;
	GuiOverlayFtue m_OverlayFtue;

	public static Vector3 m_OriginalAttackButtonScale = new Vector3(1.0f, 1.0f, 1.0f);

	//GUIBase_Button 			m_ScoreButton;
	GuiScreenScore m_Score;

	//GUIBase_Pivot			m_PivotMessages;
	//GUIBase_Layout[]		m_LayoutMessage;

	//AgentAction 					m_WaitForEndOfWeaponChange;
	//bool destroyed = false;

	bool m_FireButtonPressed;
	bool m_HideFloatingFireButton;
#pragma warning disable 414
	E_ActionButton m_ActionButtonState = E_ActionButton.None;
#pragma warning restore 414
	bool hideWeaponControls = false; //hide controls for shooting (player still can move an pause)
	bool hideTouchControls = false; //hide all controls but current weapon
	bool hideCrosshair = false;

	static string s_PivotMainName = "MainHUD";
	static string s_LayoutMainName = "HUD_Layout";
	static string s_PauseButtonName = "PauseButton";
	static string s_DPadName = "Dpad";
	static string s_DPadjoyName = "Dpadjoy";
	static string s_AttackButtonName = "FireButton";
	static string s_ReloadButtonName = "ReloadButton";
	static string s_RollButtonName = "RollButton";
	static string s_SprintButtonName = "SprintButton";

	GuiComponentContainer<E_HudComponent, GuiHUD> m_Components = new GuiComponentContainer<E_HudComponent, GuiHUD>();

	public HudComponentCrosshair Crosshair
	{
		get { return (HudComponentCrosshair)m_Components[E_HudComponent.Crosshair]; }
	}

	public HudComponentCombatInfo CombatInfo
	{
		get { return (HudComponentCombatInfo)m_Components[E_HudComponent.CombatInfo]; }
	}

	public HudComponentGadgets Gadgets
	{
		get { return (HudComponentGadgets)m_Components[E_HudComponent.Gadgets]; }
	}

	public HudComponentWeaponsInventory Weapon
	{
		get { return (HudComponentWeaponsInventory)m_Components[E_HudComponent.Weapon]; }
	}

	public HudComponentRadar Radar
	{
		get { return (HudComponentRadar)m_Components[E_HudComponent.Radar]; }
	}

	public HudComponentTeammatesInfo TeamInfo
	{
		get { return (HudComponentTeammatesInfo)m_Components[E_HudComponent.TeammatesInfo]; }
	}

	public HudComponentCommandMenu CommandMenu
	{
		get { return (HudComponentCommandMenu)m_Components[E_HudComponent.CommandMenu]; }
	}

	GuiGameMessages m_GuiGameMessages = new GuiGameMessages();

	HudMessageBox m_HudMessageBox = new HudMessageBox();

	LinkedList<MedKit> m_RegisteredMedkits = new LinkedList<MedKit>();
	LinkedList<AmmoKit> m_RegisteredAmmokits = new LinkedList<AmmoKit>();

	//kdyz se prida nejaky message sem, musi se pridat take jmeno jeho layoutu do s_MessageName
	public enum E_HudMessageType
	{
		E_NONE = -1,

		//first mission fail message
		/*E_DEATH = 0,		//keep this first mission fail message!
		E_FAILED_SAVE_SARA,
		
		//hardcoded
		E_001_GameLoaded, 
		E_002_Checkpoint,
		E_003_PickedAmmoSMG,
		E_004_PickedAmmoShotg,
		E_005_PickedAmmoGrnd,
		E_006_PickedAmmoRPG,
		E_007_NewWeaponShotg,
		E_008_NewWeaponGrnd,
		E_009_NewWeaponRPG,
		E_010_SupplyCrate,
		E_011_Collectible,
		
		//first game message
		COUNT //last*/
	}
	//musi byt ve stejnem poradi jako enum E_HudMessageType
	/*static string[]	s_MessageName	= new string[] {
		"Death_Layout", 
		"FailedSara_Layout", 
		
		//hardcoded
		"01_GameLoaded", 
		"02_Checkpoint",
		"03_PickedAmmoSMG",
		"04_PickedAmmoShotg",
		"05_PickedAmmoGrnd",
		"06_PickedAmmoRPG",
		"07_NewWeaponShotg",
		"08_NewWeaponGrnd",
		"09_NewWeaponRPG",
		"10_SupplyCrate",
		"11_Collectible",
		
		//first game message
	};
	
	public E_HudMessageType m_ShowingMessage = E_HudMessageType.E_NONE;*/

	bool IsPartHidden(E_HudWidget part)
	{
		if (IsVisible == false)
			return true;

		AgentHuman player = LocalPlayer ? LocalPlayer.Owner : null;

		bool hide = m_Score.IsVisible;
		bool dead = player != null ? !player.IsAlive : true;

		switch (part)
		{
		case E_HudWidget.Pause:
			return hide;
		case E_HudWidget.Move:
			return hideTouchControls || hide || dead;
		case E_HudWidget.Fire:
			return hideTouchControls || hideWeaponControls || hide || dead || m_HideFloatingFireButton;
		case E_HudWidget.Use:
			return hideTouchControls || hideWeaponControls || hide || dead;
		case E_HudWidget.Reload:
			return hideTouchControls || hideWeaponControls || hide || dead;
		case E_HudWidget.Roll:
			return hideTouchControls || hideWeaponControls || hide || dead;
		case E_HudWidget.Sprint:
			return hideTouchControls || hideWeaponControls || hide || dead;
		}

		return false;
	}

	static public bool ControledByTouchScreen()
	{
		return InputManager.HasTouchScreenControl();
	}

	bool ShouldBeVisible(E_HudComponent component)
	{
		if (IsVisible == false)
			return false;

		AgentHuman player = LocalPlayer ? LocalPlayer.Owner : null;

		bool show = !m_Score.IsVisible;
		bool alive = player != null ? player.IsAlive : false;

		switch (component)
		{
		case E_HudComponent.Crosshair:
			return (hideWeaponControls || hideCrosshair) ? false : show && alive;

		case E_HudComponent.Weapon:
			return hideWeaponControls ? false : show && alive;

		case E_HudComponent.Console:
			return show;

		case E_HudComponent.DeathMatchState:
			return alive;

		case E_HudComponent.DominationState:
			return alive;
		}

		return show && alive;
	}

	public static GuiHUD.E_HudMessageType WeaponAmmoMessage(E_WeaponID t)
	{
		//FIX IT
		/*switch(t)
		{
		case E_WeaponID.SMG: return GuiHUD.E_HudMessageType.E_003_PickedAmmoSMG;
		case E_WeaponID.Shotgun: return GuiHUD.E_HudMessageType.E_004_PickedAmmoShotg;
		case E_WeaponID.GrenadeLauncher: return GuiHUD.E_HudMessageType.E_005_PickedAmmoGrnd;
		case E_WeaponID.RocketLauncher: return GuiHUD.E_HudMessageType.E_006_PickedAmmoRPG;
		}*/

		return GuiHUD.E_HudMessageType.E_NONE;
	}

	void Awake()
	{
		Instance = this;

		m_InputController.OnInputHitTest += OnInputHitTest;
		m_InputController.OnProcessInput += OnProcessInput;
		InputManager.Register(m_InputController);
	}

	void OnDestroy()
	{
		m_GuiGameMessages.Destroy();

		InputManager.Unregister(m_InputController);
		m_InputController.OnProcessInput -= OnProcessInput;
		m_InputController.OnInputHitTest -= OnInputHitTest;

		if (m_Score != null)
		{
			m_Score.DestroyView();
		}

		if (m_OverlayFtue != null)
		{
			m_OverlayFtue.DestroyView();
		}

		StopAllCoroutines();
		CancelInvoke();

		Instance = null;
	}

	public static void StoreControlsPositions()
	{
		//Debug.Log("StoreControlsPositions");

		GUIBase_Pivot pivot = MFGuiManager.Instance.GetPivot("Customize");
		GUIBase_Layout layout = pivot.GetLayout("CustomizeLayout");

		//find buttons
		//GUIBase_Button pauseButton 	= GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(layout, "PauseDummy");
		GUIBase_Widget attackButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(layout, "FireDummy");
		GUIBase_Widget reloadButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(layout, "ReloadDummy");
		GUIBase_Widget rollButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(layout, "RollDummy");
		GUIBase_Widget sprintButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(layout, "SprintDummy");

		//find sprites
		GUIBase_Widget movePad = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(layout, "DpadMoveDummy");
		GUIBase_Widget weapon = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(layout, "WeaponsDummy");

		//set orig positions
		GuiOptions.FireUseButton.OrigPos = new Vector2(attackButton.transform.position.x, attackButton.transform.position.y);
		//GuiOptions.PauseButton.OrigPos 		= new Vector2(pauseButton.transform.position.x, pauseButton.transform.position.y);
		GuiOptions.MoveStick.OrigPos = new Vector2(movePad.transform.position.x, movePad.transform.position.y);
		GuiOptions.ReloadButton.OrigPos = new Vector2(reloadButton.transform.position.x, reloadButton.transform.position.y);
		GuiOptions.RollButton.OrigPos = new Vector2(rollButton.transform.position.x, rollButton.transform.position.y);
		GuiOptions.SprintButton.OrigPos = new Vector2(sprintButton.transform.position.x, sprintButton.transform.position.y);
		GuiOptions.WeaponButton.OrigPos = new Vector2(weapon.transform.position.x, weapon.transform.position.y);

		m_OriginalAttackButtonScale = attackButton.transform.localScale;

		//gadgets
		for (int i = 0; i < GuiOptions.GadgetButtons.Length; i++)
		{
			GUIBase_Widget gadgetParent = GuiBaseUtils.FindLayoutWidget<GUIBase_Widget>(layout, "GadgetDummy" + (i + 1));
			GuiOptions.GadgetButtons[i].OrigPos = new Vector2(gadgetParent.transform.position.x, gadgetParent.transform.position.y);
			//Debug.Log("storing Gadget " + i + " pos: " + GuiOptions.GadgetButtons[i].OrigPos );
		}

		GuiOptions.customControlsInitialised = true;

		if (GuiOptions.leftHandControlsNeedUpdate)
			GuiOptions.SwitchLeftHandAimingControls();
	}

	void FakeInit()
	{
// do not call this method, its only because AOT compiler issues with templates
		m_Components.Create<HudComponentCrosshair>(E_HudComponent.Crosshair, this);
		m_Components.Create<HudComponentWeaponsInventory>(E_HudComponent.Weapon, this);
		m_Components.Create<HudComponentConsole>(E_HudComponent.Console, this);
		m_Components.Create<HudComponentCombatInfo>(E_HudComponent.CombatInfo, this);
		m_Components.Create<HudComponentGadgets>(E_HudComponent.Gadgets, this);
		m_Components.Create<HudComponentCommandMenu>(E_HudComponent.CommandMenu, this);
		m_Components.Create<HudComponentRadar>(E_HudComponent.Radar, this);
		m_Components.Create<HudComponentDeathMatchState>(E_HudComponent.DeathMatchState, this);
		m_Components.Create<HudComponentDominationState>(E_HudComponent.DominationState, this);
		m_Components.Create<HudComponentTeammatesInfo>(E_HudComponent.TeammatesInfo, this);
		m_Components.Create<HudComponentZoneInfo>(E_HudComponent.ZoneInfo, this);

		MFScreenSpaceVertexGridFX temp = new MFScreenSpaceVertexGridFX();
		temp.InternalUpdate();
	}

	public void Init(ComponentPlayerLocal localPlayer)
	{
		if (localPlayer == null)
			return;

		LocalPlayer = localPlayer;

		// init ftue overlay
		if (m_OverlayFtue == null)
		{
			m_OverlayFtue = GetComponent<GuiOverlayFtue>();
			m_OverlayFtue.InitView();
		}

		// create score screen
		if (m_Score == null)
		{
			m_Score = gameObject.AddComponent<GuiScreenScore>();
			m_Score.InitView();
			m_Score.HideView(null);
		}

		// register common components		
		RegisterComponent<HudComponentCrosshair>(E_HudComponent.Crosshair);
		RegisterComponent<HudComponentWeaponsInventory>(E_HudComponent.Weapon);
		RegisterComponent<HudComponentConsole>(E_HudComponent.Console);
		RegisterComponent<HudComponentCombatInfo>(E_HudComponent.CombatInfo);
		RegisterComponent<HudComponentGadgets>(E_HudComponent.Gadgets);

		// register components for zone control game
		if (Client.Instance.GameState.GameType == E_MPGameType.ZoneControl)
		{
			RegisterComponent<HudComponentCommandMenu>(E_HudComponent.CommandMenu);
			RegisterComponent<HudComponentRadar>(E_HudComponent.Radar);
			RegisterComponent<HudComponentDominationState>(E_HudComponent.DominationState);
			RegisterComponent<HudComponentTeammatesInfo>(E_HudComponent.TeammatesInfo);
			RegisterComponent<HudComponentZoneInfo>(E_HudComponent.ZoneInfo);
		}
		else
		{
			RegisterComponent<HudComponentDeathMatchState>(E_HudComponent.DeathMatchState);

			PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(localPlayer.networkView.owner);
			foreach (PPIItemData data in ppi.EquipList.Items)
			{
				if ((data.ID == E_ItemID.EnemyDetector) || (data.ID == E_ItemID.EnemyDetectorII))
				{
					RegisterComponent<HudComponentRadar>(E_HudComponent.Radar);
					break;
				}
			}
		}

		PrecacheWidgets();

		// Register delegates
		GuiBaseUtils.RegisterButtonDelegate(m_PauseButton, null, PauseButtonDelegate);
		GuiBaseUtils.RegisterButtonDelegate(m_AttackButton, AttackButtonBeginDelegate, AttackButtonEndDelegate);
		GuiBaseUtils.RegisterButtonDelegate(m_ReloadButton, OnReloadButton, null);
		GuiBaseUtils.RegisterButtonDelegate(m_RollButton, OnRollButton, null);
		GuiBaseUtils.RegisterButtonDelegate(m_SprintButton, OnSprintBegin, OnSprintEnd);
		GuiBaseUtils.RegisterButtonDelegate(m_LayoutMain, "ScoreButton", ScoreButtonDown, ScoreButtonUp);
		GuiBaseUtils.RegisterButtonDelegate(m_LayoutMain, "AnticheatButton", null, OnAnticheatPressed);

		m_HudMessageBox.InitGui();

		UpdateControlsPosition();

		//po nastaveni origPositions muzeme konecne vytvorit joysticky se spravnymi pozicemi
		localPlayer.Controls.ControlSchemeChanged();
		//m_GuiSelectedWeaponIndex = 0;

		//InitMessages();

		m_Connection = GuiBaseUtils.GetControl<GUIBase_Sprite>(m_LayoutMain, "Connection");
		m_ConnectionIcon = GuiBaseUtils.GetControl<GUIBase_MultiSprite>(m_LayoutMain, "ConncectionIcon");

		m_Feedback_Button = GuiBaseUtils.RegisterButtonDelegate(m_LayoutMain, "Feedback_Button", null, OnFeedbackPressed);
		if (m_Feedback_Button != null)
		{
			bool showFeedback = BuildInfo.Version.Stage == BuildInfo.Stage.Beta;
			m_Feedback_Button.Widget.m_VisibleOnLayoutShow = showFeedback;
			m_Feedback_Button.Widget.Show(showFeedback, true);
		}

		foreach (MedKit medkit in m_RegisteredMedkits)
		{
			if (TeamInfo != null)
				TeamInfo.RegisterMedkit(medkit);
		}
		foreach (AmmoKit ammokit in m_RegisteredAmmokits)
		{
			if (TeamInfo != null)
				TeamInfo.RegisterAmmokit(ammokit);
		}

		m_GuiGameMessages.Init();

		IsInitialized = true;
	}

	void PrecacheWidgets()
	{
		if (m_PivotMain != null)
			return;

		m_PivotMain = MFGuiManager.Instance.GetPivot(s_PivotMainName);
		if (m_PivotMain == null)
		{
			Debug.LogError("'" + s_PivotMainName + "' not found!!! Assert should come now");
			return;
		}

		m_LayoutMain = m_PivotMain.GetLayout(s_LayoutMainName);
		if (m_LayoutMain == null)
		{
			Debug.LogError("'" + s_LayoutMainName + "' not found!!! Assert should come now");
			return;
		}

		{
			GUIBase_Widget anchor = m_LayoutMain.GetWidget("AnchorTop");
			Vector3 pos = anchor.transform.position;
			pos.y = 0.0f;
			anchor.transform.position = pos;
		}

		{
			GUIBase_Widget anchor = m_LayoutMain.GetWidget("AnchorBottom");
			Vector3 pos = anchor.transform.position;
			pos.y = Screen.height;
			anchor.transform.position = pos;
		}

		//buttons
		m_PauseButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Button>(m_LayoutMain, s_PauseButtonName);
		m_AttackButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Button>(m_LayoutMain, s_AttackButtonName);
		m_ReloadButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Button>(m_LayoutMain, s_ReloadButtonName);
		m_RollButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Button>(m_LayoutMain, s_RollButtonName);
		m_SprintButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Button>(m_LayoutMain, s_SprintButtonName);
		m_AnticheatButton = GuiBaseUtils.FindLayoutWidget<GUIBase_Button>(m_LayoutMain, "AnticheatButton");

		//sprites
		m_DPad = GuiBaseUtils.FindLayoutWidget<GUIBase_Sprite>(m_LayoutMain, s_DPadName);
		m_DPadjoy = GuiBaseUtils.FindLayoutWidget<GUIBase_Sprite>(m_LayoutMain, s_DPadjoyName);

		RegisterFloatingFireCooperatingItem(m_RollButton);
		RegisterFloatingFireCooperatingItem(m_ReloadButton);
		RegisterFloatingFireCooperatingItem(m_SprintButton);
	}

	public void RegisterFloatingFireCooperatingItem(GUIBase_Button button)
	{
		FFBInteractingWidgets.Add(new FloatingFireButtonInteractingWidget(button));
	}

	public void UnregisterFloatingFireCooperatingItem(GUIBase_Button button)
	{
		FFBInteractingWidgets.RemoveAll(e => e.Button == button);
	}

	public void Deinit(ComponentPlayerLocal localPlayer)
	{
		m_Components.Destroy(this);

		LocalPlayer = null;

		IsInitialized = false;
	}

	/*IEnumerator TestMessageBox()
	{
		//yield return new WaitForSeconds(2.25f);
		//ShowMessageBox("Test Msg box");
		//yield return new WaitForSeconds(2.25f);
		//HideMessageBox();
		//yield return new WaitForSeconds(2.25f);
		ShowCrosshairMessage("xxxxxxxxxxxxxx", 4);
		yield return new WaitForSeconds(2.25f);
		ShowCrosshairMessage("aaa", 5);

		yield return new WaitForSeconds(6f);
		
		ShowCrosshairMessage("zzzzzzzzz");
		yield return new WaitForSeconds(2);
		HideCrosshairMessage();
	}*/

	/*void InitMessages()
	{
		
		m_LayoutMessage	= new GUIBase_Layout[(int)E_HudMessageType.COUNT];
		m_PivotMessages = MFGuiManager.Instance.GetPivot("Messages");
		
		if (! m_PivotMessages)
		{
			Debug.LogError("Pivot 'Messages' not found! ");
			return;
		}
		
		//pokud tu nastane array index is out of range, asi nekdo pridal nebo ubral message, ale nezmeni pocet jmen
		for(int msgIndex = 0; msgIndex < (int)E_HudMessageType.COUNT; msgIndex++)
			m_LayoutMessage[msgIndex] = GuiBaseUtils.GetLayout(s_MessageName[msgIndex], m_PivotMessages);
		
	}*/

	GUIBase_Number GetChildNumber(GUIBase_Button btn, string name)
	{
		Transform t = btn.transform.Find(name);

		return (t != null) ? t.GetComponent<GUIBase_Number>() : null;
	}

	// -----
	public void Hide()
	{
		if (IsInitialized == false)
			return;
		if (IsVisible == false)
			return;
		IsVisible = false;

		SetFtueVisibility(false);

		// stop capture input
		m_InputController.CaptureInput = false;

		if (LocalPlayer)
			LocalPlayer.Owner.BlackBoard.ActionHandler -= HandleAction;

		HideScore();
		UpdateActiveParts();

		//Debug.Log("Hide");
		MFGuiManager.Instance.ShowPivot(m_PivotMain, false);

		// release fire button if needed
		ForceReleaseFireButton();

		// release sprint button if needed
		if (m_SprintButton != null && m_SprintButton.isDown == true)
		{
			OnSprintEnd(true);
			m_SprintButton.ForceDownStatus(false);
		}

		OnHide();

		//CancelInvoke("UpdateCrosshair");

		//ClearWaitingOnWeaponChange();
	}

	// -----
	public void Show()
	{
		if (IsInitialized == false)
			return;
		if (IsVisible == true)
			return;
		IsVisible = true;

		LocalPlayer.Owner.BlackBoard.ActionHandler += HandleAction;

		// HACK FIX
		AgentHuman player = LocalPlayer ? LocalPlayer.Owner : null;
		bool alive = player != null ? player.IsAlive : false;

		if (!alive)
		{
			// This is a hotfix. The problem here is that if the HUD is shown when the playe is already dead, something makes elements
			// of several components visible but the component itself think that they are still hidden (IsVisible == false)
			// Later UpdateActiveParts tries to hide the components again. But their Hide method skips prematurely because of the following condition:
			// if (IsVisible == false)
			//     return;
			// Thus, we have to make the components visible first, to be able to Hide it later. :-)
			// Typical workaround.
			HudComponent component = (HudComponent)m_Components[E_HudComponent.Gadgets];
			if (component != null)
			{
				component.Show();
			}

			component = (HudComponent)m_Components[E_HudComponent.Weapon];
			if (component != null)
			{
				component.Show();
			}
		}

		//Debug.Log("Show");
		MFGuiManager.Instance.ShowPivot(m_PivotMain, true);

		//HideDPadSprites();
		ShowJoystickUp();

		LocalPlayer.UpdateUseModeHACK(); //TODO: doresit zobrazeni HUDU v zavislosti na hernim stavu.
		UpdateActiveParts();

		OnShow();

		// start capture input
		m_InputController.CaptureInput = true;

		//InvokeRepeating("UpdateCrosshair", 0, 0.1f);
		//StartCoroutine(TestMessageBox());

#if MADFINGER_KEYBOARD_MOUSE
		m_PauseButton.Widget.Show(false, true);
		m_ReloadButton.Widget.Show(false, true);
		m_RollButton.Widget.Show(false, true); 
		m_SprintButton.Widget.Show(false, true);
		m_AttackButton.Widget.Show(false, true);
#endif
	}

	// ------
	public void OnUpdate()
	{
		if (IsInitialized == false)
			return;
		if (Game.Instance == null)
			return;

		if (m_OverlayFtue != null)
		{
			SetFtueVisibility(Ftue.ActiveAction != null ? true : false);

			m_OverlayFtue.UpdateView();
		}

		if (m_Score.IsVisible == true)
		{
			m_Score.UpdateView();
			m_Components.Update();
			return;
		}

		AgentHuman player = LocalPlayer ? LocalPlayer.Owner : null;

		if (!player)
		{
			return;
		}

		BlackBoard blackBoard = player.BlackBoard;

		// update crosshair
		if (player.IsInCover == true &&
			blackBoard.CoverPosition == E_CoverDirection.Middle &&
			blackBoard.Cover.CoverFlags.Get(Cover.E_CoverFlags.UpCrouch) == false)
		{
			EnableCrosshair(false);
		}
		else
		{
			if (blackBoard.Desires.MeleeTarget != null)
				Crosshair.ShowMeleeCrosshair(blackBoard.Desires.MeleeTarget.IsFriend(player) ? false : true);
			else
				Crosshair.ShowMeleeCrosshair(false);
			EnableCrosshair(true);
		}

		// update controls

#if UNITY_ANDROID || UNITY_EDITOR
		//hide on screen controls
		if ((Game.Instance.GamepadConnected && NoTouchForSec(10)) || MogaGamepad.IsConnected() || GamepadInputManager.Instance.IsNvidiaShield())
			HideControls(true);
		else
			HideControls(false);
#endif

		UpdateConnectionInfo();

		// update registered components
		m_Components.Update();

		UpdateTimesMessages();

		if (player.IsAlive == false)
		{
			UpdateActionButton();
			UpdateActiveParts();
		}

		if (m_PauseButton != null && m_PauseButton.Widget.Visible == true)
		{
			m_PauseButton.animate = true;
			m_PauseButton.isHighlighted = Ftue.IsActionActive<FtueAction.Controls>();
		}
	}

	// ---------
	bool NoTouchForSec(float inactivityTime)
	{
		return (Game.Instance != null && (Game.Instance.LastTouchControlTime + inactivityTime) < Time.timeSinceLevelLoad);
	}

	// ------
	public void OnLateUpdate()
	{
		if (IsInitialized == false)
			return;
		if (IsVisible == false)
			return;

		// late update registered components
		m_Components.LateUpdate();

		m_GuiGameMessages.Update();
	}
	
	void SetAnticheatButtonVisibility(bool state)
	{
		if (m_AnticheatButton == null)
			return;
		
		if (m_AnticheatButton.Widget.Visible == state)
			return;
		
		m_AnticheatButton.Widget.ShowImmediate(state, true);
	}

	public void ShowMessageBox(string msg)
	{
		m_HudMessageBox.Show(msg);
	}

	public void HideMessageBox()
	{
		m_HudMessageBox.Hide();
	}

	//V gui je y-osa invertnuta 
	static Vector3 ScreenToWidget(Vector2 pos)
	{
		pos.y = Screen.height - pos.y;
		return new Vector3(pos.x, pos.y, 0.0f);
	}

	// -----
	public void Heal()
	{
		CombatInfo.Heal();
	}

	// -----
	public void RechargeAmmo()
	{
		CombatInfo.RechargeAmmo();
	}

	public void JoystickBaseShow(Vector2 center)
	{
		if (m_DPad)
		{
			m_DPad.Widget.Show(!IsPartHidden(E_HudWidget.Move), true);
			Vector3 pos = ScreenToWidget(center);
			m_DPad.transform.position = pos;
		}
	}

	public void JoystickBaseHide()
	{
		if (m_DPad)
		{
			m_DPad.Widget.Show(false, true);
		}
	}

	public void JoystickDown(Vector2 center)
	{
		//Game.Instance.ControlsJoystickPosition = center;

		if (m_DPadjoy)
		{
			m_DPadjoy.Widget.Show(true, true);
			Vector3 pos = ScreenToWidget(center);
			m_DPadjoy.transform.position = pos;
			m_DPadjoy.Widget.SetModify();
		}
	}

	public void JoystickUpdate(Vector2 center)
	{
		if (m_DPadjoy)
		{
			Vector3 position = ScreenToWidget(center);
			Transform trans = m_DPadjoy.transform;
			if (trans.position != position)
			{
				trans.position = position;
				m_DPadjoy.Widget.SetModify();
			}
		}
	}

	public void JoystickUp()
	{
		if (m_DPadjoy)
		{
			m_DPadjoy.Widget.Show(false, true);
		}
		//HideDPadSprites();		
	}

	public void AimJoystickDown(Vector2 center)
	{
		if (GuiOptions.floatingFireButton)
		{
			if (m_AttackButton)
			{
				m_HideFloatingFireButton = true;
				UpdateActionButton();
				//m_AttackButton.Widget.Show(false, true);
			}
		}
	}

	public void AimJoystickUp(Vector2 center)
	{
		if (GuiOptions.floatingFireButton)
		{
			if (m_AttackButton)
			{
				m_HideFloatingFireButton = false;

				Vector3 pos = ScreenToWidget(center);

				foreach (FloatingFireButtonInteractingWidget widget in FFBInteractingWidgets)
				{
					if (widget.ShowFFBWhenAimingReleased(pos) == false)
					{
						// GUIBase_Button Button = widget.Button;
						// Debug.Log( "### FFB: name=" + Button.name + " scales=" + Button.m_TouchableAreaHeightScale + " " +  Button.m_TouchableAreaWidthScale + " TouchRect=" + Button.GetTouchRect() );
						m_HideFloatingFireButton = true;
						break;
					}
				}

				m_AttackButton.transform.position = pos;
				UpdateActionButton();
			}

			if (m_FireButtonPressed)
			{
				if (LocalPlayer != null)
				{
					LocalPlayer.Controls.FireUpDelegate();
				}

				m_FireButtonPressed = false;
			}
		}
	}

	void ShowJoystickUp()
	{
		switch (GuiOptions.m_ControlScheme)
		{
		case GuiOptions.E_ControlScheme.FloatingMovePad:
		{
			//Debug.Log("ShowJoystickUp 1 '" + m_DPad + "',  '" + m_DPadjoy + "' ");

			if (m_DPad)
				m_DPad.Widget.Show(false, true);

			if (m_DPadjoy)
				m_DPadjoy.Widget.Show(false, true);
		}
			break;

		case GuiOptions.E_ControlScheme.FixedMovePad:
		{
			//Debug.Log("ShowJoystickUp 2 '" + m_DPad + "',  '" + m_DPadjoy + "' ");
			if (m_DPad)
			{
				m_DPad.Widget.Show(!IsPartHidden(E_HudWidget.Move), true);
				Vector3 pos = new Vector3(GuiOptions.MoveStick.Positon.x, GuiOptions.MoveStick.Positon.y, 0.0f);
				m_DPad.transform.position = pos;
				m_DPad.Widget.SetModify();
			}

			if (m_DPadjoy)
				m_DPadjoy.Widget.Show(false, true);
		}
			break;
		}
	}

	GUIBase_Sprite PrepareSprite(GUIBase_Layout layout, string name)
	{
		GUIBase_Sprite sprite = null;
		GUIBase_Widget tmpWidget = layout.GetWidget(name);

		if (tmpWidget)
		{
			sprite = tmpWidget.GetComponent<GUIBase_Sprite>();
		}

		return sprite;
	}

	GUIBase_Number PrepareNumber(GUIBase_Layout layout, string name)
	{
		GUIBase_Number number = null;
		GUIBase_Widget tmpWidget = layout.GetWidget(name);

		if (tmpWidget)
		{
			number = tmpWidget.GetComponent<GUIBase_Number>();
		}

		return number;
	}

	void PauseButtonDelegate(bool inside)
	{
		if (inside)
		{
			GuiFrontendIngame.ShowPauseMenu();
		}
	}

	void OnFeedbackPressed(bool inside)
	{
		if (inside)
		{
			//GuiBaseUtils.ShowConfirmDialog(0104000, 0104111, OnFeedbackConfirmation);
			ShowFeedbackForm();
		}
	}

	void OnFeedbackConfirmation(GuiPopup inPopup, E_PopupResultCode inResult)
	{
		if (inResult != E_PopupResultCode.Ok)
			return;

		ShowFeedbackForm();
	}

	void ShowFeedbackForm()
	{
		if (IsVisible == false)
			return;

#if UNITY_IPHONE || UNITY_ANDROID

		// try to open an email client
		if (EmailHelper.ShowSupportEmailComposerForIOSOrAndroid() == true)
			return;

		// inform user that there is not an email client available
		GuiBaseUtils.ShowMessageBox(0104000, 0104010, null);

#else	
		
		Application.OpenURL(Constants.SUPPORT_URL);
		
#endif
	}

	void OnHide()
	{
		//zakaz pausu hry v prubehu fadovani
		if (MFGuiFader.Fading == true)
			return;

		GuiSubtitles.ShowAllRunning(false);

		//SuspendCurrentMessage(true);
		BossHealth.SuspendAllRunning(true);
		GuiSubtitlesRenderer.Suspend(true);
	}

	void OnShow()
	{
		GuiSubtitles.ShowAllRunning(true);

		//SuspendCurrentMessage(false);
		BossHealth.SuspendAllRunning(false);
		GuiSubtitlesRenderer.Suspend(false);
	}

	void AttackButtonBeginDelegate()
	{
		//Debug.Log(">>>> FIRE BEGIN");

		m_FireButtonPressed = true;

		if (LocalPlayer != null)
		{
			LocalPlayer.Controls.FireDownDelegate();
		}
	}

	void AttackButtonEndDelegate(bool inside)
	{
		//Debug.Log(">>>> FIRE FINISH");

		//Debug.Log("AttackButtonEndDelegate() :: inside="+inside);

		//Problem: nebude fungovat s utoky, vyzadujici trigger up
		//if ( !GuiOptions.floatingFireButton )
		if (inside)
		{
			m_FireButtonPressed = false;

			if (LocalPlayer != null &&
				LocalPlayer.Controls != null &&
				LocalPlayer.Controls.FireUpDelegate != null)
			{
				LocalPlayer.Controls.FireUpDelegate();
			}
		}
	}

	void ForceReleaseFireButton()
	{
		if (m_FireButtonPressed == true && m_AttackButton != null &&
			LocalPlayer != null && LocalPlayer.Owner != null)
		{
			LocalPlayer.Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
			AttackButtonEndDelegate(m_FireButtonPressed);
			m_AttackButton.ForceDownStatus(false);
		}
	}

	void UseButtonDelegate()
	{
		if (LocalPlayer != null)
		{
			LocalPlayer.Controls.UseDelegate();
		}
	}

	void OnReloadButton()
	{
		//Debug.Log("Reload");
		if (LocalPlayer != null)
		{
			LocalPlayer.Controls.ReloadDelegate();
		}
	}

	void OnRollButton()
	{
		//Debug.Log("Roll");
		if (LocalPlayer != null)
		{
			LocalPlayer.Controls.RollDelegate();
		}
	}

	void OnSprintBegin()
	{
		//Debug.Log(">>>> SPRINT BEGIN");

		if (LocalPlayer != null)
		{
			LocalPlayer.Controls.SprintDownDelegate();
		}
	}

	void OnSprintEnd(bool inside)
	{
		//Debug.Log(">>>> SPRINT END");

		if (LocalPlayer != null && LocalPlayer.Controls.SprintUpDelegate != null)
		{
			LocalPlayer.Controls.SprintUpDelegate();
		}
	}

	public void showTimedMessage(string msg, int time = 60)
	{
		ShowMessageBox(msg);
		timedMessageTimer = time;
	}

	void hideTimedMessage()
	{
		HideMessageBox();
	}

	void UpdateTimesMessages()
	{
		if (timedMessageTimer > 0)
		{
			timedMessageTimer --;
			if (timedMessageTimer == 0)
				hideTimedMessage();
		}
	}

	void UpdateConnectionInfo()
	{
		NetUtils.E_ConnectionQuality quality = NetUtils.GetConnectionQuality();

		bool shouldBeVisible = quality == NetUtils.E_ConnectionQuality.Good ? false : true;
		if (m_Connection.Widget.Visible != shouldBeVisible)
		{
			m_Connection.Widget.Show(shouldBeVisible, true);
		}

		string connectionState = "Connection_0" + (int)quality;
		if (connectionState != m_ConnectionIcon.State)
		{
			if (quality >= NetUtils.E_ConnectionQuality.None)
			{
				showTimedMessage(TextDatabase.instance[0503009]);
			}

			m_ConnectionIcon.State = connectionState;
		}
	}

	public void ShowScore()
	{
		if (m_Score.IsVisible == true)
			return;

		ForceReleaseFireButton();
		m_Score.ShowView(null);
		UpdateActiveParts();

		if (m_OverlayFtue != null)
		{
			m_OverlayFtue.SetActiveScreen(m_Score.name);
		}
	}

	public void HideScore()
	{
		if (m_Score.IsVisible == false)
			return;

		if (m_OverlayFtue != null)
		{
			m_OverlayFtue.SetActiveScreen(null);
		}

		m_Score.HideView(null);
		UpdateActiveParts();
	}

	void SetFtueVisibility(bool state)
	{
		if (m_OverlayFtue == null)
			return;

		if (state)
		{
			m_OverlayFtue.ShowView(this);
			m_OverlayFtue.EnableView();
		}
		else
		{
			m_OverlayFtue.DisableView();
			m_OverlayFtue.HideView(this);
		}
	}

	void ScoreButtonDown()
	{
		ShowScore();
	}

	void ScoreButtonUp(bool inside)
	{
		HideScore();
	}

	void OnAnticheatPressed(bool inside)
	{
		if (inside)
		{
			SecurityTools.ToggleAnticheatLogging();
		}
	}

	public void SelectWeapon(int index)
	{
	}

	public void SelectGadget(int index)
	{
		Gadgets.SelectGadget(index);
	}

	public void UpdateControlsPosition()
	{
		m_AttackButton.transform.position = GuiOptions.FireUseButton.Positon;
		//m_PauseButton.transform.position = GuiOptions.PauseButton.Positon;
		m_ReloadButton.transform.position = GuiOptions.ReloadButton.Positon;
		m_RollButton.transform.position = GuiOptions.RollButton.Positon;
		m_SprintButton.transform.position = GuiOptions.SprintButton.Positon;

		Weapon.UpdateControlsPosition();
		Gadgets.UpdateControlsPosition();

		UpdateAttackButtonSettings();
	}

	public void UpdateAttackButtonSettings()
	{
		if (m_AttackButton == null)
			return;

		m_AttackButton.transform.position = GuiOptions.FireUseButton.Positon;

		if (GuiOptions.floatingFireButton)
		{
			m_AttackButton.transform.localScale = m_OriginalAttackButtonScale*0.45f*GuiOptions.fireButtonScale;
		}
		else
		{
			m_HideFloatingFireButton = false;
			m_AttackButton.transform.localScale = m_OriginalAttackButtonScale*GuiOptions.fireButtonScale;
		}
	}

	/*public void ShowMessage(E_HudMessageType message, bool show)
	{	
		if(message ==  GuiHUD.E_HudMessageType.E_NONE ||  message == GuiHUD.E_HudMessageType.COUNT)
			return;
		
		if(m_LayoutMessage == null)
			return;
		
		GUIBase_Layout l = m_LayoutMessage[(int) message];
		if(l == null)
		{
			Debug.LogWarning("Message  " + message + "not found" );	
			return;
		}
		
		//Debug.Log("Showing : " + message + " : " + show + " last msg: " + m_ShowingMessage);
		
		if(show) 
		{
			if(m_ShowingMessage != GuiHUD.E_HudMessageType.E_NONE)
			{
				//Hide prev message	
				GUIBase_Layout hideLyaout = m_LayoutMessage[(int) m_ShowingMessage];
				MFGuiManager.Instance.ShowLayout(hideLyaout, false);
			}
			m_ShowingMessage = message;
		}
		else
		{
			if(m_ShowingMessage == message)
				m_ShowingMessage = GuiHUD.E_HudMessageType.E_NONE;
		}
		MFGuiManager.Instance.ShowLayout(l, show);
	}
	
	void SuspendCurrentMessage(bool suspend)
	{
		if(m_ShowingMessage !=  GuiHUD.E_HudMessageType.E_NONE)
		{
			//Hide temporarily current message	
			GUIBase_Layout hideLyaout = m_LayoutMessage[(int) m_ShowingMessage];
			hideLyaout.ShowImmediate(!suspend, false);
		}
	}
	
	public void ShowMessageTimed(E_HudMessageType message, float time)
	{
		StartCoroutine(_ShowMessageTimed(message, time));
	}
	
	IEnumerator _ShowMessageTimed(E_HudMessageType message, float time)
	{
		ShowMessage(message, true);
		yield return new WaitForSeconds(time);
		ShowMessage(message, false);
	}
	
	public void HideAllMessages()
	{
		//Debug.Log("HideAllMessages");
		foreach(GUIBase_Layout l in m_LayoutMessage)
		{
			if(l)
				MFGuiManager.Instance.ShowLayout(l, false);
		}
		
		if(m_PivotMessages)
			MFGuiManager.Instance.ShowPivot(m_PivotMessages, false);
		
		m_ShowingMessage = E_HudMessageType.E_NONE;
	}*/

	public void HideWeaponControls()
	{
		hideWeaponControls = true;
		UpdateActiveParts();
	}

	public void ShowWeaponControls()
	{
		hideWeaponControls = false;
		UpdateActiveParts();
	}

	void EnableCrosshair(bool on)
	{
		bool hide = !on;
		if (hideCrosshair == hide)
			return;
		hideCrosshair = hide;

		UpdateActiveParts();
	}

	//Tahle funkce je adept na prepsani, chtelo by to nejkay prehlednejsi system jak skryvat casti hudu
	void UpdateActiveParts()
	{
		// update visibility of all registered components
		for (int idx = 0; idx < (int)E_HudComponent.Max; ++idx)
		{
			HudComponent component = (HudComponent)m_Components[(E_HudComponent)idx];
			if (component == null)
				continue;

			if (ShouldBeVisible((E_HudComponent)idx) == true)
			{
				component.Show();
			}
			else
			{
				component.Hide();
			}
		}

#if !MADFINGER_KEYBOARD_MOUSE
		if (m_PauseButton)
			m_PauseButton.Widget.Show(!IsPartHidden(E_HudWidget.Pause), true);

		if (m_ReloadButton)
			m_ReloadButton.Widget.Show(!IsPartHidden(E_HudWidget.Reload), true);

		if (m_RollButton)
			m_RollButton.Widget.Show(!IsPartHidden(E_HudWidget.Roll), true);

		if (m_SprintButton)
			m_SprintButton.Widget.Show(!IsPartHidden(E_HudWidget.Sprint), true);
#endif

		SetAnticheatButtonVisibility(SecurityTools.UserHasAnticheatManagementPermissions());

		//fire and use
		UpdateActionButton();

#if !UNITY_EDITOR
				//move dpad
		if( m_DPad && (GuiOptions.m_ControlScheme == GuiOptions.E_ControlScheme.FixedMovePad || IsPartHidden(E_HudWidget.Move)) )
		{	
			m_DPad.Widget.Show( !IsPartHidden(E_HudWidget.Move), true );
		}	
		
		if(m_DPadjoy && IsPartHidden(E_HudWidget.Move))
		{	
			m_DPadjoy.Widget.Show( !IsPartHidden(E_HudWidget.Move), true );
		}	
#else
		m_DPad.Widget.Show(false, true);
		m_DPadjoy.Widget.Show(false, true);
#endif
	}

	void UpdateActionButton()
	{
#if !MADFINGER_KEYBOARD_MOUSE
		if (m_AttackButton)
			m_AttackButton.Widget.Show(!IsPartHidden(E_HudWidget.Fire) && (m_ActionButtonState == E_ActionButton.Fire), true);
#endif

		//if(m_UseButton)
		//	m_UseButton.Widget.Show( !IsPartHidden(E_HudWidget.Use) && (m_ActionButtonState == E_ActionButton.Use), true ); 
	}

	public void ShowActionButton(E_ActionButton inState)
	{
		m_ActionButtonState = inState;
		UpdateActionButton();
	}

	/*public void Reset()
	{
		//Debug.Log("GuiHUD.Reset");
		//zrusit selekci zbrane
		m_GuiSelectedWeaponIndex = 0;

		hideWeaponControls 	= false;
		hideCrosshair		= false;

		ClearWaitingOnWeaponChange();
	}*/

	public void HandleAction(AgentAction a)
	{
		/*   if(a is AgentActionWeaponChange)
        {
				//if(destroyed)
				//Debug.LogError("+++++++++++++++++++   accessing HUD Destroyed");
			
			DisableWeaponSelection(true);
			m_WaitForEndOfWeaponChange = a;
        }
		else if(a is AgentActionDeath)
		{
			ClearWaitingOnWeaponChange();
		}*/
	}

	/*
	void ClearWaitingOnWeaponChange()
	{
		//Debug.Log("ClearWaitingOnWeaponChange");
	//	DisableWeaponSelection(false);
		//m_WaitForEndOfWeaponChange = null;
	}
	*/

	void HideControls(bool hide)
	{
		if (hideTouchControls != hide)
		{
			//Debug.Log("HideControls: " + hide);
			hideTouchControls = hide;
			UpdateActiveParts();
		}
	}

	public void UpdateGadgetSelection()
	{
		if (Gadgets != null)
		{
			Gadgets.ShowSelection(IsVisible && Gadgets.IsVisible);
		}
	}

	// ------
	//create gadgets buttons
	public void CreateGadgetInventory(PlayerPersistantInfo ppi)
	{
		Gadgets.CreateGadgetInventory(ppi);
	}

	// -------
	public E_ItemID GetGadgetInInventoryIndex(int index)
	{
		return Gadgets.GetGadgetInInventoryIndex(index);
	}

	public void ShowCombatText(Client.E_MessageType type, string text)
	{
		m_GuiGameMessages.AddNewMessage(type, text);
	}

	// ------
	public void RegisterWeaponsInInventory()
	{
		Weapon.RegisterWeaponsInInventory();
	}

	// -------
	public E_WeaponID GetWeaponInInventoryIndex(int index)
	{
		return Weapon.GetWeaponOnIndex(index);
	}

	// -------
	public int GetInventoryWeaponsCount()
	{
		return Weapon.Weapons.Count;
	}

	// -------
	public void RegisterMedkit(MedKit medkit)
	{
		if (medkit != null)
			m_RegisteredMedkits.AddLast(medkit);

		if (TeamInfo != null)
			TeamInfo.RegisterMedkit(medkit);
	}

	// -------
	public void UnregisterMedkit(MedKit medkit)
	{
		m_RegisteredMedkits.Remove(medkit);
		m_RegisteredMedkits.Remove((MedKit)null); // prevence

		if (TeamInfo != null)
			TeamInfo.UnregisterMedkit(medkit);
	}

	// -------
	public void RegisterAmmokit(AmmoKit ammokit)
	{
		if (ammokit != null)
			m_RegisteredAmmokits.AddLast(ammokit);

		if (TeamInfo != null)
			TeamInfo.RegisterAmmokit(ammokit);
	}

	// -------
	public void UnregisterAmmokit(AmmoKit ammokit)
	{
		m_RegisteredAmmokits.Remove(ammokit);
		m_RegisteredAmmokits.Remove((AmmoKit)null); // prevence

		if (TeamInfo != null)
			TeamInfo.UnregisterAmmokit(ammokit);
	}

	// -------
	public void StartTeamCommand(AgentHuman agent, E_CommandID command)
	{
		if (Radar != null)
			Radar.StartTeamCommand(agent, command);
	}

	// -------
	public void StopTeamCommand(AgentHuman agent)
	{
		if (Radar != null)
			Radar.StopTeamCommand(agent);
	}

	// PRIVATE METHODS
	HudComponent RegisterComponent<T>(E_HudComponent id) where T : HudComponent, new()
	{
		return m_Components.Create<T>(id, this);
	}

	GUIBase_Widget OnInputHitTest(ref Vector2 point)
	{
		if (m_OverlayFtue != null)
		{
			GUIBase_Widget widget = m_OverlayFtue.HitTest(ref point);
			if (widget != null)
				return widget;
		}

		return m_LayoutMain.HitTest(ref point);
	}

	bool OnProcessInput(ref IInputEvent evt)
	{
		if (m_OverlayFtue != null)
		{
			if (m_OverlayFtue.ProcessInput(ref evt) == true)
				return true;
		}

		return m_LayoutMain.ProcessInput(ref evt);
	}

	/*public void OnGamepadConnectionChanged(bool connected)
	{
		if (!IsInitialized || !IsVisible || m_Score.IsVisible)
			return;
		
		HideControls(connected);
	}*/

	#region IViewOwner implementation

	void IViewOwner.DoCommand(string inCommand)
	{
	}

	void IViewOwner.ShowScreen(string inScreenName, bool inClearStack)
	{
	}

	GuiPopup IViewOwner.ShowPopup(string inPopupName, string inCaption, string inText, PopupHandler inHandler)
	{
		return null;
	}

	void IViewOwner.Back()
	{
	}

	void IViewOwner.Exit()
	{
	}

	bool IViewOwner.IsTopView(GuiView inView)
	{
		return false;
	}

	bool IViewOwner.IsAnyScreenVisible()
	{
		return false;
	}

	bool IViewOwner.IsAnyPopupVisible()
	{
		return false;
	}

	#endregion
}
