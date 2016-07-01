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

//Interface a ruzne implementace touch joysticku

using UnityEngine;
using System.Collections;

public abstract class JoystickBase
{
	public int FingerID = -1;
	public Vector2 Center;

	public Vector2 Dir;
	public float Force;
	public bool Updated;

	public bool On
	{
		get { return FingerID != -1; }
	}

	public void SetCenter(Vector2 center)
	{
		Center = center;
	}

	public float Radius
	{
		get { return 0.07f*Screen.width; }
	}

	public abstract void OnTouchBegin(ref TouchEvent evt);
	public abstract void OnTouchUpdate(ref TouchEvent evt);
	public abstract void OnTouchEnd(ref TouchEvent evt);
	public abstract bool IsInside(ref TouchEvent evt);
};

public class JoystickFloating : JoystickBase
{
	Rect TouchArea; //touch area ve screen coord

	public JoystickFloating(Rect r)
	{
		SetTouchArea(r);
	}

	public void SetTouchArea(Rect r)
	{
		TouchArea.x = Screen.width*r.x;
		TouchArea.y = Screen.height*r.y;
		TouchArea.width = Screen.width*r.width;
		TouchArea.height = Screen.height*r.height;
		//Debug.Log("Move TouchArea " + TouchArea);
	}

	public override bool IsInside(ref TouchEvent evt)
	{
		return TouchArea.Contains(evt.Position);
	}

	//tuhle funkci volat jen kdyz dany fingerId jeste neni nikde v gui pouzity
	public override void OnTouchBegin(ref TouchEvent evt)
	{
		//Debug.Log(">>>> MOVE FLOATING BEGIN");

		//Debug.Log("Joystick aquired");

		FingerID = evt.Id;
		SetCenter(evt.Position);

		Dir = Vector2.zero;
		Force = 0;

		Updated = true;

		//update gui
		if (GuiHUD.Instance)
		{
			GuiHUD.Instance.JoystickBaseShow(evt.Position);
			GuiHUD.Instance.JoystickDown(evt.Position);
		}
	}

	//tuhle funkci volat jen kdyz je FingerID == evt.Id
	public override void OnTouchUpdate(ref TouchEvent evt)
	{
		if (FingerID != evt.Id)
		{
			Debug.LogError("inconsistent finger id in joystick update");
			return;
		}

		Dir = evt.Position - Center;
		float dist = Dir.magnitude;

		if (dist > Radius)
		{
			Dir *= (Radius/dist); //normalise and make length of radious
		}

		//update gui
		if (GuiHUD.Instance)
		{
			GuiHUD.Instance.JoystickUpdate(Center + Dir);
		}

		Force = Mathf.Clamp(dist/Radius, 0, 1);

		Updated = true;
	}

	//tuhle funkci volat jen kdyz je FingerID == evt.Id
	public override void OnTouchEnd(ref TouchEvent evt)
	{
		//Debug.Log(">>>> MOVE FLOATING END :: FingerID="+FingerID);

		if (FingerID == evt.Id)
		{
			//Debug.Log("Joystick relesaed");
			FingerID = -1;
			Dir = Vector2.zero;
			Force = 0;

			//update gui
			if (GuiHUD.Instance)
			{
				GuiHUD.Instance.JoystickUp();
				GuiHUD.Instance.JoystickBaseHide();
			}

			Updated = false;
		}
		else
		{
			Debug.LogError("Inconsistent finger id");
		}
	}
};

public class JoystickFixed : JoystickBase
{
	public float DeadZone
	{
		get { return 0.02f*Screen.width; }
	}

	public JoystickFixed(float posX, float posY)
	{
		//Debug.Log("Setting center for fixed joystick " + posX + " " + posY);
		SetCenter(new Vector2(posX, posY));
	}

	public override void OnTouchBegin(ref TouchEvent evt)
	{
		//Debug.Log(">>>> MOVE FIXED BEGIN");

		Vector2 dir = evt.Position - Center;
		float dist = dir.magnitude;

		if (dist < Radius*1.5f)
		{
			FingerID = evt.Id;

			if (dist > DeadZone)
			{
				Force = (dist - DeadZone)/(Radius - DeadZone);
				Dir = (dir/dist)*Force; //normalize, make length of force
			}
			else
			{
				Dir = Vector2.zero;
				Force = 0;
			}

			//update gui
			if (GuiHUD.Instance)
			{
				GuiHUD.Instance.JoystickDown(Center + Dir*Radius);
			}

			Updated = true;
		}
	}

	public override void OnTouchUpdate(ref TouchEvent evt)
	{
		//Debug.Log(">>>> MOVE FIXED END :: FingerID="+FingerID);

		if (FingerID == evt.Id)
		{
			Vector2 dir = evt.Position - Center;
			float dist = dir.magnitude;

			if (dist > Radius)
			{
				Dir = dir/dist;
				Force = 1.0f;
			}
			else if (dist > DeadZone)
			{
				Force = (dist - DeadZone)/(Radius - DeadZone);
				Dir = (dir/dist)*Force;
			}
			else
			{
				//Debug.Log("Outside Dead zone 2");
				Dir = Vector2.zero;
				Force = 0;
			}

			//update gui
			if (GuiHUD.Instance)
			{
				GuiHUD.Instance.JoystickUpdate(Center + Dir*Radius);
			}

			Updated = true;
		}
	}

	public override void OnTouchEnd(ref TouchEvent evt)
	{
		if (FingerID == evt.Id)
		{
			FingerID = -1;
			Dir = Vector2.zero;
			Force = 0;

			if (GuiHUD.Instance)
			{
				GuiHUD.Instance.JoystickUp();
			}
			//Debug.Log("Joystick releaed");

			Updated = false;
		}
	}

	public override bool IsInside(ref TouchEvent evt)
	{
		Vector2 dir = evt.Position - Center;
		return (dir.magnitude < Radius*1.5f);
	}
};
