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

public class MenuController
{
	class Button
	{
		public bool Pressed;
		public bool FirstQuery;
		public float LastRelaxPos;
		public float LastPressedPos;

		public void Reset()
		{
			Pressed = false;
			FirstQuery = true;
			LastRelaxPos = 0;
			LastPressedPos = 0;
		}
	};

	enum E_Dir
	{
		LEFT = 0,
		RIGHT,
		UP,
		DOWN,
	};

	Button[] Buttons = new Button[4] {new Button(), new Button(), new Button(), new Button()};

	float DeadZone = 0.1F; //hodnoty ppod tuto hranici povazujeme za neutral
	float MoveDelta = 0.2F;
		  //vzdalenost kterou musime urazit od posledniho press nebo relax mista abychom povazovali pohyb za dostateny ke zmene stavu.

	public MenuController()
	{
		for (int i = 0; i < Buttons.Length; i++)
		{
			Buttons[i].Reset();
		}
	}

	public void Update()
	{
		//left, right
		{
			Button bLeft = Buttons[(int)E_Dir.LEFT];
			Button bRight = Buttons[(int)E_Dir.RIGHT];

			//detect press 
			float posH = Input.GetAxisRaw("HorizontalMove");
			if (!bRight.Pressed && (posH > bRight.LastRelaxPos + MoveDelta))
			{
				bRight.Pressed = true;
				bRight.LastPressedPos = posH;
			}
			else if (bRight.Pressed && (posH < bRight.LastPressedPos - MoveDelta))
			{
				bRight.Reset();
				bRight.LastRelaxPos = posH;
			}
			else if (!bLeft.Pressed && (posH < bLeft.LastRelaxPos - MoveDelta))
			{
				bLeft.Pressed = true;
				bLeft.LastPressedPos = posH;
			}
			else if (bLeft.Pressed && (posH > bLeft.LastPressedPos + MoveDelta))
			{
				bLeft.Reset();
				bLeft.LastRelaxPos = posH;
			}

			//update last pos
			if (bRight.Pressed)
			{
				//stlaceno doprava, updatuj last pressed na max dosazenou hodnotu
				if (bRight.LastPressedPos < posH)
					bRight.LastPressedPos = posH;
			}
			else
			{
				//neni stlaceno, updatuj relax na nejmensi dosazenou
				if (bRight.LastRelaxPos > posH)
				{
					if (posH > DeadZone)
						bRight.LastRelaxPos = posH;
					else
						bRight.LastRelaxPos = DeadZone;
				}
			}

			if (bLeft.Pressed)
			{
				//stlaceno doleva, updatuj last pressed na max zapornou dosazenou hodnotu
				if (bLeft.LastPressedPos > posH)
					bLeft.LastPressedPos = posH;
			}
			else
			{
				//neni stlaceno, updatuj relax na nejmensi dosazenou zapornou hodnotu
				if (bLeft.LastRelaxPos < posH)
				{
					if (posH < -DeadZone)
						bLeft.LastRelaxPos = posH;
					else
						bLeft.LastRelaxPos = -DeadZone;
				}
			}

			//Debug.Log("posH: " + posH + " R LastPressedPos: " + bRight.LastPressedPos + " R LastRelaxPos: " + bRight.LastRelaxPos + " R pressed: " + bRight.Pressed + " R firstq: " + bRight.FirstQuery
			//	+ " L LastPressedPos: " + bLeft.LastPressedPos + " L LastRelaxPos: " + bLeft.LastRelaxPos + " L pressed: " + bLeft.Pressed + "  L firstq: " + bLeft.FirstQuery );						
		}

		//up, down
		{
			Button bDown = Buttons[(int)E_Dir.DOWN];
			Button bUp = Buttons[(int)E_Dir.UP];

			//detect press 
			float posV = Input.GetAxisRaw("VerticalMove");
			if (!bUp.Pressed && (posV > bUp.LastRelaxPos + MoveDelta))
			{
				bUp.Pressed = true;
				bUp.LastPressedPos = posV;
			}
			else if (bUp.Pressed && (posV < bUp.LastPressedPos - MoveDelta))
			{
				bUp.Reset();
				bUp.LastRelaxPos = posV;
			}
			else if (!bDown.Pressed && (posV < bDown.LastRelaxPos - MoveDelta))
			{
				bDown.Pressed = true;
				bDown.LastPressedPos = posV;
			}
			else if (bDown.Pressed && (posV > bDown.LastPressedPos + MoveDelta))
			{
				bDown.Reset();
				bDown.LastRelaxPos = posV;
			}

			//update last pos
			if (bUp.Pressed)
			{
				//stlaceno doprava, updatuj last pressed na max dosazenou hodnotu
				if (bUp.LastPressedPos < posV)
					bUp.LastPressedPos = posV;
			}
			else
			{
				//neni stlaceno, updatuj relax na nejmensi dosazenou
				if (bUp.LastRelaxPos > posV)
				{
					if (posV > DeadZone)
						bUp.LastRelaxPos = posV;
					else
						bUp.LastRelaxPos = DeadZone;
				}
			}

			if (bDown.Pressed)
			{
				//stlaceno doleva, updatuj last pressed na max zapornou dosazenou hodnotu
				if (bDown.LastPressedPos > posV)
					bDown.LastPressedPos = posV;
			}
			else
			{
				//neni stlaceno, updatuj relax na nejmensi dosazenou zapornou hodnotu
				if (bDown.LastRelaxPos < posV)
				{
					if (posV < -DeadZone)
						bDown.LastRelaxPos = posV;
					else
						bDown.LastRelaxPos = -DeadZone;
				}
			}

			//Debug.Log("posH: " + posH + " R LastPressedPos: " + bUp.LastPressedPos + " R LastRelaxPos: " + bUp.LastRelaxPos + " R pressed: " + bUp.Pressed + " R firstq: " + bUp.FirstQuery
			//	+ " L LastPressedPos: " + bDown.LastPressedPos + " L LastRelaxPos: " + bDown.LastRelaxPos + " L pressed: " + bDown.Pressed + "  L firstq: " + bDown.FirstQuery );									
		}
	}

	public bool PressedLeft()
	{
		if (Buttons[(int)E_Dir.LEFT].Pressed && Buttons[(int)E_Dir.LEFT].FirstQuery)
		{
			Buttons[(int)E_Dir.LEFT].FirstQuery = false;
			return true;
		}
		else
			return false;
	}

	public bool PressedRight()
	{
		if (Buttons[(int)E_Dir.RIGHT].Pressed && Buttons[(int)E_Dir.RIGHT].FirstQuery)
		{
			Buttons[(int)E_Dir.RIGHT].FirstQuery = false;
			return true;
		}
		else
			return false;
	}

	public bool PressedUp()
	{
		if (Buttons[(int)E_Dir.UP].Pressed && Buttons[(int)E_Dir.UP].FirstQuery)
		{
			Buttons[(int)E_Dir.UP].FirstQuery = false;
			return true;
		}
		else
			return false;
	}

	public bool PressedDown()
	{
		if (Buttons[(int)E_Dir.DOWN].Pressed && Buttons[(int)E_Dir.DOWN].FirstQuery)
		{
			Buttons[(int)E_Dir.DOWN].FirstQuery = false;
			return true;
		}
		else
			return false;
	}

	public bool PressedOk()
	{
		if ( /*PlayerControlsGamepad.ButtonDown(PlayerControlsGamepad.E_Input.MenuOk) || */
						PlayerControlsGamepad.ButtonDown(PlayerControlsGamepad.E_Input.Fire) || Input.GetKeyDown("1") || Input.GetKeyDown("8"))
		{
			//Debug.Log(Time.realtimeSinceStartup + " joy: " + Input.GetKeyDown("joystick button 0") + " Fire: " + PlayerControlsGamepad.ButtonDown(PlayerControlsGamepad.E_Input.Fire) );
			return true;
		}

		return false;
	}

	public bool PressedBack()
	{
		if ( /*PlayerControlsGamepad.ButtonDown(PlayerControlsGamepad.E_Input.MenuBack) || */Input.GetKeyDown("escape"))
		{
			return true;
		}

		return false;
	}
};
