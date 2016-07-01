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

//--------------------------------------------------------------------------------------------------------------
//                                      Roidz Input Manager V1.4
//                                      ------------------------
// 
//  Read the help pdf for full help if you don't understand claerly !
//  -----------------------------------------------------------------
//  steps to follow to use the custom inputmanager in your project:
// 
//  Step1: - Close your project and unity !
//           Copy over the 'InputManager.asset' that comes with this inputmanager over the 
//           'InputManager.asset' from your project,these are located in the Library subfolder.
//  Step2: - Open your project in unity and import the 'inputmanager' unitypackage.
//  Step3: - Drag the 'InputManagerController' prefab that just got imported into you scene 
//  Step4: - In the script where you have your controller code, add 'public custom_inputs inputManager' 
//           above your functions,then select the 'InputManagerController' object in the inspector.
//  Step5: - Now you can,in the script where you handle your controller code, use following commands...
//
//           inputManager.isInput[0]       - to check if input 0 is pressed
//           inputManager.isInputDown[0]   - to check if input 0 is pressed down
//           inputManager.isInputUp[0]     - to check if input 0 is released
//                  example:  if (inputManager.isInput[0])
//                                {
//                                  print("inputkey 0 has been pressed")
//                                }
// Step6: - Customize the inputmanager in the inspector on the script attached to 'InputManagerController'
// 
//                                        THATS IT !!!
//
//
// This input manager is made in my free time for an upcomming game,check http://teamkozmoz.com
// I decided to release this inputmanager to the comunity because it's a softspot in unity.
//
// This release is completely free but donations are (very) welcome, especially if you release comercially.
// That will push me to release simular scripts/tools to the comunity for sure ;)
// If you appreciate my work and you want to donate something , you can do it on ward.dewaele@pandora.be
// Thats also my email adress incase you have suggestions / requests.
//
//                  
//                                       Thanks to Ian Kirkland for a bugfix and some help :)
//                                       Special Thanks to BLiTZWiNG & NCarter for their neverending help
//
// PS: Even this is completely free to use, this does NOT mean that you can resell it or claim as
// your own creation. Credits are not needed but welcome. I hope this helps some people out.
//
// Legal notice: Although i tried to make sure this script works fine i am not liable for any damage 
//               resulting from use of this script/software.
//
//                                                                             © Ward Dewaele (Roidz)
//
//                                PS a donation would really help | paypal account: ward.dewaele@pandora.be
//--------------------------------------------------------------------------------------------------------------

using UnityEngine;
using System.Collections;

public class custom_inputs : GuiScreen
{
	/*public static custom_inputs Instance;
	
    // custom inputmanager values
    // ---------------------------   
    // is menu on or off

    //logo to show on top of the inputmanager
    public Texture2D inputManagerLogo;

    // Description string array
    private string[] DescriptionString;

    // X & Y locations of the menu
    float DescBox_X = -220;
    float InputBox_X = 0;
    float Boxes_Y = 100;

    //margin between the buttons (vertical)
    float BoxesMargin_Y = 30;
    float BoxesHeight = 25;

    //size of the buttons
    int DescriptionSize = 200;
    int buttonSize = 200;

    //location of the resetbutton
    float resetbuttonLocX = -220;
    float resetbuttonLocY = 550;
    
    float DescriptionBox_X = 0;
    float InputBox1_X = 0;
	
	
    float resetbuttonX = 0;
	float Title_X = 0; 
	float Title_Y = 50;
    
    //should we accept duplicate inputs ? ?
    bool allowDuplicates = false;

	//ktery button je prave configurovan
	int m_InputButtonIndex = -1;

    // GUI skin
    public GUISkin OurSkin;
    
	//lokalizovane texty pro buttony
	string strTitle;
	string strAction;
	string strButton;
	string strDefaults;
	string strDone;

	
	bool Initialized = false; 
	
	void Awake()
	{
		Instance = this;
		DescriptionString = new string[(int)PlayerControlsGamepad.E_Input.COUNT];
	}
	
	void Start()
	{
		Init();
	}
	
	void Init()
    {    	
		//Debug.Log("custom inputs - Init");
		Initialized = true;

		//captions
		strTitle 		= TextDatabase.instance[02020000];
		strAction	 	= TextDatabase.instance[02020001];
		strButton 		= TextDatabase.instance[02020002];
	    strDefaults 	= TextDatabase.instance[02020003];
		strDone 		= TextDatabase.instance[02020004];
		
        // inputmanager ---------------
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Fire] 				= TextDatabase.instance[02020005];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Reload] 			= TextDatabase.instance[02020006];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Sprint] 			= TextDatabase.instance[02020007];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Roll] 				= TextDatabase.instance[02020008];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Pause] 			= TextDatabase.instance[02020009];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Weapon1]			= TextDatabase.instance[02020010];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Weapon2] 			= TextDatabase.instance[02020011];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Weapon3] 			= TextDatabase.instance[02020012];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Item1] 			= TextDatabase.instance[02020013];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Item2] 			= TextDatabase.instance[02020014];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Item3] 			= TextDatabase.instance[02020015];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Axis_MoveRight] 	= TextDatabase.instance[02020016];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Axis_MoveUp] 		= TextDatabase.instance[02020017];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Axis_ViewRight] 	= TextDatabase.instance[02020018];
		DescriptionString[(int)PlayerControlsGamepad.E_Input.Axis_ViewUp] 		= TextDatabase.instance[02020019];
		
		LoadConfig();

        // ********************************
	}
		
	public void LoadConfig()
	{
		if(!Initialized)
			return;

		//pokud je pripojen gamepad a existuje k nemu nastaveni, nacti jej
		string gpadName = Game.CurrentJoystickName();
		GamepadInputManager.Instance.SetConfig(gpadName);
	}
	
	protected override void OnViewUpdate()
    {
	    DescriptionBox_X = ((Screen.width/2) + DescBox_X);
	    InputBox1_X = ((Screen.width/2) + InputBox_X);
	    resetbuttonX = ((Screen.width/2) + resetbuttonLocX);
		resetbuttonLocY = (Screen.height - 75);
		Title_X =  (Screen.width/2) - 220; 
	}
	
	protected override void OnViewShow()
	{
		GuiMenu menu = Owner as GuiMenu;
		if (menu != null)
		{
			menu.SetOverlaysVisible(false);
		}
	}
	
	protected override void OnViewHide()
	{
		GuiMenu menu = Owner as GuiMenu;
		if (menu != null)
		{
			menu.SetOverlaysVisible(true);
		}
	}
	
	protected override bool OnViewProcessInput(ref IInputEvent evt)
	{
		if (evt.Kind == E_EventKind.Key)
		{
			KeyEvent key = (KeyEvent)evt;
			if (key.Code == KeyCode.Escape)
			{
				return true;
			}
		}
	
		return base.OnViewProcessInput(ref evt);
	}	
	
	public void CloseAndSaveConfig()
	{
		m_InputButtonIndex = -1;
		
        // save our configuration
		GamepadInputManager.Instance.SaveConfig( Game.CurrentJoystickName() );

		if (Owner != null)
		{
			Owner.Back();
		}
	}

    void OnGUI()
    {
        // only do this when menu is up
        if (IsVisible == true)
        {
            drawButtons1();
        }
    }
	
    void drawButtons1()
    {
        // mouse and menu stuff
        float inputy = Boxes_Y;
        float ix = Input.mousePosition.x;
        float iy = Input.mousePosition.y;
        Vector3 transMouse = GUI.matrix.inverse.MultiplyPoint3x4(new Vector3(ix, Screen.height - iy, 1));
		
        //GUI.skin = OurSkin;
		
        // draw the logo
		if (inputManagerLogo != null)
		{
        	GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), inputManagerLogo);
		}
		
		//draw boxes
        GUI.Box(new Rect(50, 25, Screen.width-100, Screen.height-50), "","window");

		//captions / buttons
		GUI.Label(new Rect(Title_X, Title_Y, 420, 30), strTitle, "textfield");
        GUI.Label(new Rect(DescriptionBox_X, inputy-10, DescriptionSize, BoxesHeight), strAction,"textfield");
        GUI.Label(new Rect(InputBox1_X, inputy - 10, DescriptionSize, BoxesHeight), strButton, "textfield");
		
        // reset button code
        if (GUI.Button(new Rect(resetbuttonX, resetbuttonLocY, buttonSize, BoxesHeight), strDefaults) && Input.GetMouseButtonUp(0))
        {
			ResetToDefaults();
        }
		
		// save and exit
		if( GUI.Button(new Rect(resetbuttonX + buttonSize + 20, resetbuttonLocY, buttonSize, BoxesHeight), strDone)  && Input.GetMouseButtonUp(0))
		{
			CloseAndSaveConfig();
			return;
		}
		
        for (int n = 0; n < DescriptionString.Length; n++)
        {
            // add the margin between buttons
            inputy += BoxesMargin_Y;
			
            // Description (name) of the buttons
            GUI.Label(new Rect(DescriptionBox_X, inputy, DescriptionSize, BoxesHeight), DescriptionString[n],"box");
            Rect buttonRec = new Rect(InputBox1_X, inputy, buttonSize, BoxesHeight);
			
            // the button with the inputkey 
            GUI.Button(buttonRec, GetButtonLabel(n));
			
            // marks the selected input button
            if (m_InputButtonIndex == n)
            {
                //GUI.Toggle(buttonRec, true, "", OurSkin.button);
                GUI.Toggle(buttonRec, true, "");
            }
			
            // if the button gets pressed
            if (buttonRec.Contains(transMouse) && Input.GetMouseButtonUp(0) && m_InputButtonIndex == -1)
            {
                // were ready to receive input
				m_InputButtonIndex = n;
            }
		}
			
		if(m_InputButtonIndex != -1)
        {
			DetectInputSetup();
        }
    }

	string GetButtonLabel(int btnIndex)
	{
		JoyInput command = GamepadInputManager.Instance.GetActionButton(btnIndex);
		if( command.joyAxis != E_JoystickAxis.NONE )
			return GamepadAxis.GetAxisLabel( command.joyAxis );
		else
			return command.key.ToString();
    }
			
	void DetectInputSetup()
    {
		JoyInput pi = GetPressedInput();
		if(pi != null)
		{
			GamepadInputManager.Instance.SetActionButton(m_InputButtonIndex, pi);
			
			if(pi.joyAxis == E_JoystickAxis.NONE)
	            checDoubles(pi.key, m_InputButtonIndex, 1);
			else
				checDoubleAxis(pi.joyAxis, m_InputButtonIndex,1);				
				
			m_InputButtonIndex = -1;
        }
    }
	
	JoyInput GetPressedInput()
    {
            // KEYBOARD
            // if we received key-input thats not ESCAPE 
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode != KeyCode.Escape)
            {
				KeyCode kc = Event.current.keyCode;
				return new JoyInput(kc, E_JoystickAxis.NONE);
            }

            //JOYSTICK BUTTONS
            for (int joyK = (int)KeyCode.Joystick1Button0; joyK <= (int)KeyCode.Joystick4Button19; joyK++)
            {
                // check for all joystick buttons
                if (Input.GetKey((KeyCode)joyK) && Event.current.keyCode != KeyCode.Escape)
                {
					KeyCode kc = (KeyCode)joyK;
					return new JoyInput(kc, E_JoystickAxis.NONE);
				}
            }
			
            //JOYSTICK AXS
            //----------------------------------------------------------------
            // joystick axis is kind hacky but i don't see a way around it
            // we set the axis in the unity inputmanager and then use them here
            // so we can set them to anything we want
            //----------------------------------------------------------------
		
			//hack pro F510 ktery vraci na tabletech pro osy 4 a 0 stale 1 (a pro 5 a 11 -1 kdyz nejsou stisknute)
			bool f510_Hack =  (Game.CachedJoystickName == "Generic X-Box pad") && (Input.GetAxis( GamepadAxis.GetAxis((E_JoystickAxis)5) ) < 0 || Input.GetAxis(GamepadAxis.GetAxis((E_JoystickAxis)11)) < 0);
		
			int result = -1;
			for(int i = 0; i < (int)E_JoystickAxis.COUNT; i++)
            {
				if(f510_Hack)
				{
					//if(i == 4 || i == 5 || i == 10 || i == 11)	
					if(i == 4 || i == 10)	
						continue;
				}
			
				float trashHold = (i < 4) ? 0.5f : 0.8f;
				string axis = GamepadAxis.GetAxis((E_JoystickAxis)i);
				if(Input.GetAxis( axis ) > trashHold && Event.current.keyCode != KeyCode.Escape)
				{
					//Debug.Log("...................... axis " + ax.axis + ": " + Input.GetAxis( ax.axis) + ", HACK " + f510_Hack);
					//Input.ResetInputAxes();
					result = i;
				}
            }
			if(result != -1)
				return new JoyInput(KeyCode.None, (E_JoystickAxis)result);
   
		return null;
    }
	
	
    void checDoubles(KeyCode testkey,int o,int p)
    {
        if (!allowDuplicates)
        {
            for (int m = 0; m < DescriptionString.Length; m++)
            {
                // check if we allready have testkey in our list and make sure we dont compare with itself

                // input buttons 
				JoyInput button = GamepadInputManager.Instance.GetActionButton(m);
                if (testkey == button.key && (m != o || p == 2))
                {
                    // reset the double key
					GamepadInputManager.Instance.SetActionButton(m, new JoyInput(KeyCode.None, E_JoystickAxis.NONE));
                }

            }
        }
    }
	
    void checDoubleAxis(E_JoystickAxis testAxis, int o,int p)
    {
        if (!allowDuplicates)
        {
            for (int m = 0; m < DescriptionString.Length; m++)
            {
                // check if we allready have testkey in our list and make sure we dont compare with itself

                // input buttons 
				JoyInput button = GamepadInputManager.Instance.GetActionButton(m);
                if (testAxis == button.joyAxis && (m != o || p == 2))
                {
                    // reset the double key
					GamepadInputManager.Instance.SetActionButton(m, new JoyInput(KeyCode.None, E_JoystickAxis.NONE));
                }
            }
        }
    }
	
	void ResetToDefaults()
	{
		m_InputButtonIndex = -1;
		
		GamepadInputManager.Instance.DeleteConfig( Game.CurrentJoystickName() );
		GamepadInputManager.Instance.SetDefaultConfig( Game.CurrentJoystickName() );
	}
	*/
}
