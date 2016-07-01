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

public class PlayerControlsTouch
{
	//static float useRayTestDistance = 10.0f; 	//Vzdalenost do jake testujeme kolizi z kamery pri hledani touchnuteho interaction objectu
	//static float usePlayerDistance = 2.0f;  	//vzdalenost InteractionObjectu od playera na kterou jej muzme pouzit.
	//static float usePlayerDistance = 4.0f;  	//CRASHUJE 

	static Rect leftArea = new Rect(0, 0, 0.5f, 0.82f);
	static Rect rightArea = new Rect(0.5f, 0, 0.5f, 0.82f);

	public class JoystickBaseOld
	{
		public int FingerID = -1;
		//public bool Enabled = true;
		public bool On
		{
			get { return FingerID != -1; }
		}

		public Vector2 Center;
		public float Left, Bottom, Right, Top;
		public Vector2 LastTouchPosition;
		public bool FirstDelta;

		List<Vector2> DeltaPositions = new List<Vector2>();
		int FilterPositions = 10;

		public JoystickBaseOld(float left, float bottom, float width, float height)
		{
			Left = Screen.width*left;
			Bottom = Screen.height*bottom;
			Right = Left + Screen.width*width;
			Top = Bottom + Screen.height*height;

			//Debug.Log("View area " + Left + " " + Bottom + " " + Right + " " + Top);
		}

		public void SetCenter(Vector2 center)
		{
			Center = center;
			LastTouchPosition = center;
			FirstDelta = true;
			DeltaPositions.Clear();
			FilterPositions = 10;
		}

		public bool IsInside(ref TouchEvent evt)
		{
			if (evt.Position.x > Left && evt.Position.x < Right && evt.Position.y > Bottom && evt.Position.y < Top)
			{
				return true;
			}
			return false;
		}

		public void AddDelta(Vector2 delta)
		{
			if (DeltaPositions.Count == FilterPositions)
				DeltaPositions.RemoveAt(0);

			if (DeltaPositions.Count > 0)
			{
//we can also do this, if inout is negative to previous delta, we can clear deltaposition
				Vector3 lastDelta = DeltaPositions[DeltaPositions.Count - 1];
				if (delta.x*lastDelta.x < 0 || delta.y*lastDelta.y < 0)
					DeltaPositions.Clear();
			}

			DeltaPositions.Add(delta);

			//this should work in this way :
			// when its first input, we are filtering input at max value
			//then we are decreasing filtering, so its more precise
			if (delta != Vector2.zero)
			{
				FilterPositions = Mathf.Max(3, FilterPositions - 1);

				while (DeltaPositions.Count > FilterPositions)
					DeltaPositions.RemoveAt(0);
			}
		}

		public Vector2 GetDelta()
		{
			if (DeltaPositions.Count == 0)
				return Vector2.zero;

			Vector2 delta = Vector2.zero;
			foreach (Vector2 v in DeltaPositions)
				delta += v;

			return delta/DeltaPositions.Count;
		}
	}

	/*public class JoystickControlDynamic : JoystickBaseOld
     {
         public Vector3 Direction = new Vector3();
         public float Force;
		 public bool Updated;

        public float Radius { get { return 0.07f * Screen.width; } }

        public JoystickControlDynamic(float left, float bottom, float width, float height) : base(left, bottom, width, height) {
        }

    }*/

	public class ViewControl : JoystickBaseOld
	{
		public Quaternion Rotation;
		public float Yaw;
		public float Pitch;

		public bool Updated;

		public float minimumYaw = -360f;
		public float maximumYaw = 360f;

		public float minimumPitch = 60f;
		public float maximumPitch = 60f;

		public ViewControl(float left, float bottom, float width, float height) : base(left, bottom, width, height)
		{
		}

		public void OnTouchEnd(ref TouchEvent evt)
		{
			//Debug.Log(">>>> VIEW END :: FingerID="+FingerID);

			FingerID = -1;
		}
	}

	Transform _Temp;

	//public JoystickControlDynamic MoveJoystick = null;
	ViewControl m_ViewJoystick = null;
	JoystickBase m_MoveJoystick = null;

	PlayerControlStates m_States;

	public PlayerControlsTouch(PlayerControlStates inStates)
	{
		m_States = inStates;

		Start();
	}

	void CreateDefaultJoystics()
	{
		m_MoveJoystick = new JoystickFloating(leftArea);
		m_ViewJoystick = new ViewControl(rightArea.x, rightArea.y, rightArea.width, rightArea.height);
		OnControlSchemeChange();
	}

	public void OnControlSchemeChange()
	{
		//Debug.Log("OnControlSchemeChange: " + GuiOptions.m_ControlScheme + " " + GuiOptions.leftHandAiming);

		switch (GuiOptions.m_ControlScheme)
		{
		case GuiOptions.E_ControlScheme.FloatingMovePad:
		{
			m_MoveJoystick = new JoystickFloating(GuiOptions.leftHandAiming ? rightArea : leftArea);
		}
			break;

		case GuiOptions.E_ControlScheme.FixedMovePad:
		{
			m_MoveJoystick = new JoystickFixed(GuiOptions.MoveStick.Positon.x, Screen.height - GuiOptions.MoveStick.Positon.y);

			//
		}
			break;
		}

		//view joystick
		Rect viewArea = GuiOptions.leftHandAiming ? leftArea : rightArea;
		m_ViewJoystick = new ViewControl(viewArea.x, viewArea.y, viewArea.width, viewArea.height);
	}

	void Start()
	{
		GameObject t = new GameObject();
		_Temp = t.transform;

		//tohle je berlicka - diky pozdni inicializaci guinemuzeme vytvorit joysticky hned pri startu (widgety jeste nejsou nactene n nevedeli bychom pozici pro joystick)
		//takze vytvorime defaultne joysticky ktere pozice nepotrebuji a po nacteni gui je prepiseme podle nastaveni v guioptions
		CreateDefaultJoystics();
	}

	public void Update()
	{
	}

	public bool Process(ref IInputEvent evt)
	{
		if (evt.Kind != E_EventKind.Touch)
			return false;

#if UNITY_ANDROID
		if (GamepadInputManager.Instance != null && GamepadInputManager.Instance.IsNvidiaShield())
			return false;
#endif

		//Debug.Log(">>>> time="+Time.time+", evt="+evt+", lockCursor="+Screen.lockCursor);

		TouchEvent touch = (TouchEvent)evt;

		if (touch.Type == E_TouchType.MouseButton && SysUtils.Screen_lockCursor == true)
			return false;

		m_MoveJoystick.Updated = false;
		m_ViewJoystick.Updated = false;

		//ViewJoystick.Yaw = 0; ViewJoystick.Pitch = 0; //neni nutne pokud updatujeme jen pri ViewJoystick.Updated

		if (touch.Started == true)
		{
			TouchBegin(ref touch);
		}
		else if (touch.Active == true)
		{
			TouchUpdate(ref touch);
		}
		else
		{
			TouchEnd(ref touch);
		}

		if (m_MoveJoystick.Updated)
		{
			m_States.Move.Direction.x = m_MoveJoystick.Dir.x;
			m_States.Move.Direction.z = m_MoveJoystick.Dir.y;
			m_States.Move.Direction.y = 0;
			m_States.Move.Direction.Normalize();
			_Temp.transform.eulerAngles = new Vector3(0, Player.LocalInstance.Owner.Transform.rotation.eulerAngles.y, 0);

			m_States.Move.Direction = _Temp.transform.TransformDirection(m_States.Move.Direction);
			m_States.Move.Force = m_MoveJoystick.Force;

			//_States.Move.Direction = MoveJoystick.Direction;
			//_States.Move.Force = MoveJoystick.Force;
		}

		if (m_ViewJoystick.Updated)
		{
			m_States.View.SetNewRotation(m_ViewJoystick.Yaw, m_ViewJoystick.Pitch);
		}

		return true;
	}

	public static InteractionObject TouchedInteractionIcon(Vector2 position)
	{
		/*
		if(GuiHUD.Instance.IsHidden)
			return null;
		
		Camera cameraForRay = Camera.main;
		if(cameraForRay == null)
			return null;
		
		//zjisiti jetli jsme se dotkli nektere use icony
		 Ray ray = cameraForRay.ScreenPointToRay(  position ); 
	
		
        RaycastHit[] hits;
		hits = Physics.RaycastAll(ray, useRayTestDistance); 
		
        //sort by distance
        if (hits.Length > 1)
            System.Array.Sort(hits, CollisionUtils.CompareHits);
		
		////Debug.Log("hits: " + hits.Length);
		
        foreach (RaycastHit hit in hits)
        {
			GameObject hitObj = hit.transform.gameObject;
			////Debug.Log("Hit: " + hit.transform.name + " layer: " + hitObj.layer);
			
			if(hitObj == Player.LocalInstance.Owner.GameObject)
				continue;
			
			if(hitObj.layer  !=  InteractionObject.UseLayer)
				break;
			
			InteractionObject interactionObj = Mission.Instance.GameZone.InteractionObjectWithIcon(hitObj);
			if(interactionObj != null)
			{
				//zjisti jestli je v povolenem range od hrace
				float sqrPlayerDistance = (interactionObj.Position - Player.LocalInstance.Owner.Position).sqrMagnitude;
				////Debug.Log("Object " + interactionObj.Position + " player " + Player.Instance.Owner.Position);
				////Debug.Log("Hit use object in distance " +  Mathf.Sqrt(sqrPlayerDistance) + " from player, and " +  (ray.origin-interactionObj.Position).magnitude + " from camera. Limit is " + usePlayerDistance);
				
				if( sqrPlayerDistance <= usePlayerDistance*usePlayerDistance )
				{
					////Debug.Log("use object found: " + interactionObj.name);
					return interactionObj;
				}
			}
			break;
        }*/
		return null;
	}

	float GetSensitivity()
	{
		return (GuiOptions.sensitivity);
	}

	void TouchBegin(ref TouchEvent evt)
	{
//		Debug.Log("BENY: -------------------------------------------------------------------------------------------------------------------------");
//		Debug.Log("BENY: " + Time.timeSinceLevelLoad +  " TouchBegin : id=" + evt.Id + ", pos=" + evt.Position + ", delta=" + touch.deltaPosition);

		if (FingerIdInUse(ref evt, false))
		{
			if (m_MoveJoystick.FingerID == evt.Id)
			{
				m_MoveJoystick.OnTouchEnd(ref evt);
			}
			else if (m_ViewJoystick.FingerID == evt.Id)
			{
				m_ViewJoystick.OnTouchEnd(ref evt);
			}
		}

		/*InteractionObject touchedInteraction = TouchedInteractionIcon(evt.Position);
		if(touchedInteraction)
		{
			_States.UseObjectDelegate(touchedInteraction);			
			return;
		}*/

		if (m_States.Move.Enabled && m_MoveJoystick.FingerID == -1 && m_MoveJoystick.IsInside(ref evt))
		{
			m_MoveJoystick.OnTouchBegin(ref evt);
			return;
		}

		if (m_States.View.Enabled && m_ViewJoystick.On == false && m_ViewJoystick.IsInside(ref evt))
		{
			//Debug.Log(Time.timeSinceLevelLoad + " View Joystick aquired " + evt.Id + "pos " + evt.Position);

			//Debug.Log(">>>> VIEW BEGIN");

			m_ViewJoystick.FingerID = evt.Id;
			m_ViewJoystick.SetCenter(evt.Position);
			m_ViewJoystick.Rotation = Player.LocalInstance.Owner.BlackBoard.Desires.Rotation;

			m_States.View.ZeroInput();
		}
	}

	void TouchUpdate(ref TouchEvent evt)
	{
		//Debug.Log("BENY: " + Time.timeSinceLevelLoad +  " TouchUpdate : id=" + evt.Id + ", pos=" + evt.Position + ", delta=" + touch.deltaPosition);

		//Debug.Log(Time.timeSinceLevelLoad + " testing TouchUpdate : " + evt.Position);

		//
		if (m_States.Move.Enabled)
		{
			if (m_MoveJoystick.FingerID == evt.Id)
			{
//				if (MoveJoystick.IsInside(touch))
				Rect r = GuiOptions.leftHandAiming ? rightArea : leftArea;
				Rect moveArea = new Rect(Screen.width*r.x, Screen.height*r.y, Screen.width*r.width, Screen.height*r.height);

				if (moveArea.Contains(evt.Position))
				{
					m_MoveJoystick.OnTouchUpdate(ref evt);
				}
				else
				{
					m_MoveJoystick.OnTouchEnd(ref evt);
				}
				return;
			}
//			else if ( MoveJoystick.FingerID == -1 && MoveJoystick.IsInside(touch) && !FingerIdInUse(touch, false) && GuiOptions.m_ControlScheme == GuiOptions.E_ControlScheme.Scheme1 )
			else if (m_MoveJoystick.FingerID == -1 && m_MoveJoystick.IsInside(ref evt))
			{
				if (!FingerIdInUse(ref evt, false))
				{
					m_MoveJoystick.OnTouchBegin(ref evt);
				}
			}
		}

		m_ViewJoystick.Yaw = 0;
		m_ViewJoystick.Pitch = 0;

		if (m_States.View.Enabled && m_ViewJoystick.FingerID == evt.Id)
		{
			if (m_ViewJoystick.IsInside(ref evt))
			{
				ViewJoystickUpdate(ref evt);
			}
			else
			{
				m_ViewJoystick.OnTouchEnd(ref evt);
			}
			//  //Debug.Log("enabled");
		}
		//else
		//  //Debug.Log("disabled " + _States.View.Enabled  + " "  + ViewJoystick.FingerID + " " + evt.Id);

//        //Debug.Log(ViewJoystick.Yaw + " " + ViewJoystick.Pitch);
	}

	void ViewJoystickUpdate(ref TouchEvent evt)
	{
		Vector2 delta;

		if (GuiHUD.Instance != null)
			GuiHUD.Instance.AimJoystickDown(evt.Position);

		if (m_ViewJoystick.FirstDelta)
		{
			m_ViewJoystick.AddDelta(evt.Position - m_ViewJoystick.LastTouchPosition);
			delta = m_ViewJoystick.GetDelta();

			//Debug.Log(Time.timeSinceLevelLoad + " FirstDelta : " + evt.Position.y + " delta " + (evt.Position - ViewJoystick.LastTouchPosition).y + " / " + delta.x + "-" + delta.y );
		}
		else
		{
			m_ViewJoystick.AddDelta(evt.Position - m_ViewJoystick.LastTouchPosition);
			delta = m_ViewJoystick.GetDelta();

			//Debug.Log(Time.timeSinceLevelLoad + " NextDelta : " + evt.Position.y + " delta " + (evt.Position - ViewJoystick.LastTouchPosition).y + " / " + + delta.x + "-" + delta.y);
			//				delta = evt.Position - ViewJoystick.LastTouchPosition;

			//Debug.Log(Time.timeSinceLevelLoad + " testing Delta : " + evt.Position.y + " delta " + delta.y);
		}

		m_ViewJoystick.LastTouchPosition = evt.Position;

		// Debug.Log(Time.timeSinceLevelLoad + " unity delta " + touch.deltaPosition + " my delta" + delta);

		if (delta != Vector2.zero)
		{
			m_ViewJoystick.FirstDelta = false;
			float DegreePerPixelsY = 45.0f/(Screen.width*0.25f);
			float DegreePerPixelsP = 30.0f/(Screen.height*0.25f);

			//				Debug.Log(Time.timeSinceLevelLoad + " degreePerPixelY " + DegreePerPixelsY + " DegreePerPixelP " + DegreePerPixelsP);

			//delta.x *= Mathf.Abs(delta.x);
			//delta.y *= Mathf.Abs(delta.y);

			float yaw = delta.x*DegreePerPixelsY;
			float pitch = delta.y*DegreePerPixelsP;

			//apply options sensitivity
			float touchSensitivity = GetSensitivity();
			yaw *= touchSensitivity;
			pitch *= touchSensitivity*0.7f;

			//Debug.Log(Time.timeSinceLevelLoad + " yaw " + yaw + " pitch " + pitch + " timedelta " + Time.deltaTime);

			m_ViewJoystick.Yaw = yaw;
			m_ViewJoystick.Pitch = -pitch;
			m_ViewJoystick.Updated = true;
		}
	}

	void TouchEnd(ref TouchEvent evt)
	{
		if (m_MoveJoystick.FingerID == evt.Id)
		{
			m_MoveJoystick.OnTouchEnd(ref evt);
			return;
		}

		if (m_ViewJoystick.FingerID == evt.Id)
		{
			if (GuiHUD.Instance != null)
				GuiHUD.Instance.AimJoystickUp(evt.Position);

			m_ViewJoystick.OnTouchEnd(ref evt);
			return;
		}
	}

	void JoystickDown(Vector2 pos)
	{
		if (GuiHUD.Instance)
		{
			GuiHUD.Instance.JoystickDown(pos);
		}
	}

	void JoystickUpdate(Vector2 pos)
	{
		if (GuiHUD.Instance)
		{
			GuiHUD.Instance.JoystickUpdate(pos);
		}
	}

	void JoystickUp()
	{
		if (GuiHUD.Instance)
		{
			GuiHUD.Instance.JoystickUp();
		}
	}

	bool FingerIdInUse(ref TouchEvent evt, bool joysticksOnly)
	{
		if (m_MoveJoystick.FingerID == evt.Id)
			return true;

		if (m_ViewJoystick.FingerID == evt.Id)
			return true;

		return false;
	}

	public void Reset()
	{
		m_MoveJoystick.FingerID = -1;
		m_MoveJoystick.Dir = Vector3.zero;
		m_MoveJoystick.Force = 0;

		//GuiManager.Instance.ResetControls();
	}
}
