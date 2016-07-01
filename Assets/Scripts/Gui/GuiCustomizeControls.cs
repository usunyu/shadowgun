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

[AddComponentMenu("GUI/Menu/GuiCustomizeControls")]
public class GuiCustomizeControls : GuiScreen
{
	class CustomControl
	{
		public string m_Name;
		public GUIBase_Widget m_WidgetDummy;
		public Transform m_Transform;
		public Vector2 m_TempOffset;
		public GuiOptions.ControlPos m_OptionsPos;
		//Vector3 OrigScale;
		//Quaternion OrigRotation;
	}

	static string moveDummyName = "DpadMoveDummy";
	static string gadgetDummnyName = "GadgetDummy";

	//zastupne widdgety ktere se v customuze screenu zobrazuji misto ovladacih prvku (jsou to pouze sprity, ne buttony)
	List<CustomControl> m_Controls = new List<CustomControl>();

	Vector2 DragBeginPos;
	Vector2 DragBeginOffset;
	CustomControl DraggingControl;

	protected override void OnViewInit()
	{
		//Debug.Log("Customise init");

		if (!GuiOptions.customControlsInitialised)
			GuiHUD.StoreControlsPositions();

		m_Controls.Add(new CustomControl() {m_Name = "FireDummy", m_OptionsPos = GuiOptions.FireUseButton});
		m_Controls.Add(new CustomControl() {m_Name = "ReloadDummy", m_OptionsPos = GuiOptions.ReloadButton});
		m_Controls.Add(new CustomControl() {m_Name = "RollDummy", m_OptionsPos = GuiOptions.RollButton});
		m_Controls.Add(new CustomControl() {m_Name = moveDummyName, m_OptionsPos = GuiOptions.MoveStick});
		m_Controls.Add(new CustomControl() {m_Name = "WeaponsDummy", m_OptionsPos = GuiOptions.WeaponButton});
		//m_Controls.Add(new CustomControl(){m_Name = "PauseDummy", m_OptionsPos = GuiOptions.PauseButton});
		m_Controls.Add(new CustomControl() {m_Name = "SprintDummy", m_OptionsPos = GuiOptions.SprintButton});

		for (int i = 0; i < GuiOptions.GadgetButtons.Length; i++)
		{
			m_Controls.Add(new CustomControl() {m_Name = gadgetDummnyName + (i + 1), m_OptionsPos = GuiOptions.GadgetButtons[i]});
		}

		//init all sprites
		foreach (CustomControl c in m_Controls)
		{
			GUIBase_Sprite sprite = GuiBaseUtils.PrepareSprite(m_ScreenLayout, c.m_Name);
			c.m_WidgetDummy = sprite.Widget;
			c.m_Transform = c.m_WidgetDummy.transform;
		}
	}

	protected override void OnViewShow()
	{
		GuiMenu menu = Owner as GuiMenu;
		if (menu != null)
		{
			menu.SetBackgroundVisibility(false);
			menu.SetOverlaysVisible(false);
		}

		InitTempPositions();
		//SetGadgetsSprites(); //TODO: nefunguje tak jak bych si predstavoval, doresit logiku

		ShowSchemeSpecificSprites();

		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "ResetButton", null, ResetButtonDelegate);
		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "Back_Button", null, ConfirmButtonDelegate);
	}

	//zakomentovane,  nebude se asi pouzivat (cisla zatim staci).pokud se odkomentuje, je treba pridat k widgetu sprite pro ikonu ("GadgetIcon").
	/*void SetGadgetsSprites()
	{
		//TODO: zobrazovani itemu muze byt matouci, pokud je obsazeno mene slotu tak se to v gadgetech collapsuje a obsazuji se nejprve prvni, pak druhy, etc (i dyz je v equipu obsazen napr druhy a treti)
		//zjisti ktery item je ve kterem slotu. 
		//GuiHUD.Instance.Gadgets jeste nejsou inicializovene takze to zjistitme z ppi
		List<PPIItemData> equipedItems = PPIManager.Instance.GetLocalPPI().EquipList.Items;
		for(int i = 0; i < GuiOptions.GadgetButtons.Length; i++)
		{
			string gName = gadgetDummnyName + (i+1);
			CustomControl c = m_Controls.Find(f => f.m_Name == gName);
			//E_ItemID itemID = GuiHUD.Instance.Gadgets.Gadgets[i];
			PPIItemData itmData = equipedItems.Find(f => f.EquipSlotIdx == i);
			E_ItemID itemID = itmData.ID;
            GUIBase_Sprite s = GuiBaseUtils.GetChildSprite( c.m_WidgetDummy, "GadgetIcon" );
			if(itemID != E_ItemID.None)
			{
	            ItemSettings itmSet = ItemSettingsManager.Instance.Get(itemID);
	            s.Widget.CopyMaterialSettings(itmSet.HudWidget);
				s.Widget.FadeAlpha	= 1.0f; 	//Show
			}
			else
			{
				s.Widget.FadeAlpha	= 0f;  		//hide
			}
		}
	}*/

	protected override void OnViewHide()
	{
		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "ResetButton", null, null);
		GuiBaseUtils.RegisterButtonDelegate(m_ScreenLayout, "Back_Button", null, null);

		GuiMenu menu = Owner as GuiMenu;
		if (menu != null)
		{
			menu.SetOverlaysVisible(true);
			menu.SetBackgroundVisibility(true);
		}
	}

	void InitTempPositions()
	{
		foreach (CustomControl c in m_Controls)
		{
			c.m_TempOffset = c.m_OptionsPos.Offset;

			if (c.m_WidgetDummy.name == "FireDummy")
			{
				c.m_WidgetDummy.transform.localScale = GuiHUD.m_OriginalAttackButtonScale*GuiOptions.fireButtonScale;
			}
			//Debug.Log("Control " + c.m_Name + ": origPos " + c.m_OptionsPos.OrigPos + " offset "+ c.m_OptionsPos.Offset);
		}

		UpdateSpritesPos();
		ShowSprites();
	}

	void CancelButtonDelegate(bool inside)
	{
		if (inside)
		{
			Owner.Back();
		}
	}

	void ResetButtonDelegate(bool inside)
	{
		if (inside)
		{
			//restore default positons and update gui
			foreach (CustomControl c in m_Controls)
			{
				c.m_TempOffset = Vector2.zero;

				//in lefthand mode, set default positions on opposite side
				if (GuiOptions.leftHandAiming && c.m_OptionsPos.Side != GuiOptions.E_ControlSide.Neutral)
				{
					c.m_TempOffset.x = (Screen.width - c.m_OptionsPos.OrigPos.x) - c.m_OptionsPos.OrigPos.x;
				}
			}

			UpdateSpritesPos();
		}
	}

	void ConfirmButtonDelegate(bool inside)
	{
		if (inside)
		{
			//store changes in options 
			foreach (CustomControl c in m_Controls)
			{
				c.m_OptionsPos.Offset = c.m_TempOffset;
			}

			if (GuiHUD.Instance != null && GuiHUD.Instance.IsInitialized == true)
			{
				GuiHUD.Instance.UpdateControlsPosition();
			}
			if (Player.LocalInstance != null && Player.LocalInstance.Controls != null)
			{
				Player.LocalInstance.Controls.ControlSchemeChanged(); //update positions of joysticks
			}

			GuiOptions.Save();

			//return to menu
			Owner.Back();
		}
	}

	void MouseTouchEvent()
	{
		if (Input.GetMouseButtonDown(0))
		{
			TouchBegin(Input.mousePosition);
		}
		else if (Input.GetMouseButtonUp(0))
		{
			TouchEnd(Input.mousePosition);
		}
		else if (Input.GetMouseButton(0))
		{
			TouchUpdate(Input.mousePosition);
		}
	}

	void TouchBegin(Vector2 pos)
	{
		pos.y = Screen.height - pos.y;

		foreach (CustomControl c in m_Controls)
		{
			if (c.m_WidgetDummy.IsVisible())
			{
				if (c.m_WidgetDummy.IsMouseOver(pos))
				{
					//Debug.Log("mouse over: " + c.m_Name + pos);
					DraggingControl = c;
					DragBeginOffset = c.m_TempOffset;
					DragBeginPos = pos;
					break;
				}
			}
		}
	}

	void TouchUpdate(Vector2 pos)
	{
		pos.y = Screen.height - pos.y;
		Vector2 dragDelta = pos - DragBeginPos;
		//dragDelta.y = dragDelta.y; //widget ma y osu invertnutou

		//	Debug.Log("pos " + pos + " " + dragDelta);
		if (DraggingControl == null)
			return;

		DraggingControl.m_TempOffset = DragBeginOffset + dragDelta;

		UpdateSpritesPos();
	}

	void TouchEnd(Vector2 pos)
	{
		DraggingControl = null;
	}

	protected override void OnViewUpdate()
	{
		//AnimateControls();

		if (Input.touchCount == 0)
		{
#if UNITY_EDITOR
			MouseTouchEvent();
#endif
			return;
		}

		Touch touch = Input.GetTouch(0);

		if (touch.phase == TouchPhase.Began)
		{
			TouchBegin(touch.position);
		}
		else if (touch.phase == TouchPhase.Moved)
		{
			TouchUpdate(touch.position);
		}
		else if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
		{
			TouchEnd(touch.position);
		}
	}

	void UpdateSpritesPos()
	{
		foreach (CustomControl c in m_Controls)
		{
			c.m_Transform.position = c.m_OptionsPos.OrigPos + c.m_TempOffset;
			c.m_WidgetDummy.SetModify(true);
		}
	}

	void ShowSprites()
	{
		foreach (CustomControl c in m_Controls)
			c.m_WidgetDummy.ShowImmediate(true, true);
	}

	void ShowSchemeSpecificSprites()
	{
		//movepad
		CustomControl movePad = m_Controls.Find(f => f.m_Name == moveDummyName);
		bool showMovePad = (GuiOptions.m_ControlScheme == GuiOptions.E_ControlScheme.FixedMovePad);
		movePad.m_WidgetDummy.ShowImmediate(showMovePad, true);
	}

	void AnimateControls()
	{
		//Rotaci nepouzivat, widgety ted s ni maji problem 
		/*Quaternion fromRot = Quaternion.AngleAxis(30, Vector3.back);
		Quaternion toRot = Quaternion.AngleAxis(-30, Vector3.back);
		float speed = 1.00f;
		
		foreach(CustomControl c in m_Controls)
		{
			float animPos = Mathf.PingPong(Time.realtimeSinceStartup*speed, 1);
			c.m_Transform.localRotation = Quaternion.Slerp (fromRot, toRot, animPos);
			c.m_WidgetDummy.SetModify(true);
		}*/

		/*foreach(CustomControl c in m_Controls)
		{
			float scl = 1.0f + 0.025f* Mathf.Sin(Time.realtimeSinceStartup*3);
			c.m_Transform.localScale = c.OrigScale *scl;
			c.m_WidgetDummy.SetModify(true);
		}*/
	}
}
