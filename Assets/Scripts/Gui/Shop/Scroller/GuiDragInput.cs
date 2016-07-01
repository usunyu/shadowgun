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

//ovladaci cast scrollbaru (funkcnost):
//1) pri zacatku dragu ulozime pocatecni pozici a cas
//2) v prubehu dragu updatujeme pozici scrollbaru. posun bud muze byt v pixelech, nebo relativni podle velikosti obrazovky nebo widgetu (doresit)
//3) na konci dragu vyhodnotime inertii (bud rychlost scrollingu za posledni drag [jednodussi], nebo za poslednich cca 300ms [vyzaduje bud ukladat do bufferu aktualnio akcelerace, nebo pocatecni pozice s casem]) 
class GuiDragInput
{
	public bool isHorizontal = false;
	float m_Velocity = 0;

	float timeTouchPhaseEnded = 0f;
	float touchBeginTime = 0;
	float touchBeginPos = 0; //verticalni nebo horizontalni pozice touche v okamziku zacatku dotyku
	float m_LastUpdateTime = 0; //for fixed update

	const float maxTapDuration = 0.5f; //[sec]
	const float minPopupHoldDuration = 0.75f; //[sec]
	const float maxPopupMoveDistance = 50f; //pixels? relative distance?
	const float maxTapMoveDistance = 50f;
	//const float MaxInertia = 3500;
	public float MinSpeed
	{
		get { return 300; }
	}

	const float frictionPerSec = 900.0f; //constant slow down per sec

	const float inertiaFixedUpdate = 0.1f; //inertia step (10x per sec)
	const float frictionPerCent = 0.15f; //proportional slow down inertia - we apply it in fixed step 10x per second  

	const float frictionDuration = 1.2f;

	//TODO: vyzkouset menit friction v zavislosti na case od konce inertia 
	const float minFriction = 0.05f;
	const float maxFriction = 0.25f;
	const float frictionAccDuration = 1.5f; //time for which we accelerate friction from minFriction to maxFriction

	public bool tapEvent { get; private set; }
	public float tapEventPos { get; private set; }
	public bool isHolding { get; private set; }
	public float holdingPos { get; private set; }
	bool notMovedSinceTouch = false;

	int m_FingerId = -1;

	Vector3 lastMouseDragPos;
	float lastDragPos;

	public Vector2 ScrollDelta { get; private set; }
	public bool IsDragging { get; private set; }

	public float MoveSpeed
	{
		get { return Mathf.Abs(m_Velocity); }
	}

	const int maxSmooth = 10;
	List<float> lastValues = new List<float>(maxSmooth);
	Rect m_ActiveArea;

	public GuiDragInput()
	{
		IsDragging = false;
	}

	public void SetActiveArea(Rect rect)
	{
		m_ActiveArea = rect;
		//Debug.Log("SetActiveArea:   x: " + m_ActiveArea.x + " y: " + m_ActiveArea.y + " w: " + m_ActiveArea.width + " h: " + m_ActiveArea.height);
	}

	public void ClearTapEvent()
	{
		tapEvent = false;
		tapEventPos = 0;
	}

	void AddInertia(float inr)
	{
		if (lastValues.Count == maxSmooth)
			lastValues.RemoveAt(0);

		lastValues.Add(inr);
	}

	float GetInertia()
	{
		float inertia = 0;
		for (int i = 0; i < lastValues.Count; i++)
			inertia += lastValues[i]*i/lastValues.Count;

		inertia /= lastValues.Count;
		return inertia;
	}

	public void Update()
	{
		ScrollDelta = Vector2.zero;

		//detect touch or mouse drag
		if (Input.touchCount > 0)
		{
			TouchUpdate();
		}
		else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
		{
			MouseUpdate();
		}
		else
		{
			UpdateVelocity();
		}
	}

	void TouchUpdate()
	{
		int touchIndex = -1;

		//pokud zatim zadny touch event nedrzime, najdeme si prvni touch begin, jinak ten ktery zacal v gui
		for (int i = 0; i < Input.touchCount; i++)
		{
			Touch t1 = Input.GetTouch(i);
			if (m_FingerId == -1 && t1.phase == TouchPhase.Began)
			{
				//check if touch accured inside area we are interested in
				if (!m_ActiveArea.Contains(t1.position))
				{
					//Debug.Log("touch outside test area: " + t1.position);
					continue;
				}
				else
				{
					//Debug.Log("touch in test area. Area: " + m_ActiveArea + " pos: " + t1.position);
				}

				touchIndex = i;
				m_FingerId = t1.fingerId;
				break;
			}
			else if (t1.fingerId == m_FingerId) //reagujeme pouze na touch ktery zacal v gui
			{
				touchIndex = i;
				break;
			}
		}

		if (touchIndex == -1)
			return;

		Touch touch = Input.GetTouch(touchIndex);
		float curPos = isHorizontal ? touch.position.x : -touch.position.y;
		//float deltaPos = isHorizontal ? touch.deltaPosition.x : -touch.deltaPosition.y;
		//	Vector3 mp = (Input.mousePosition - lastMouseDragPos);
		float deltaPos = curPos - lastDragPos;
		lastDragPos = curPos;

		if (touch.phase == TouchPhase.Began)
		{
			OnDragBegin(curPos);
		}
		else if (touch.phase == TouchPhase.Canceled)
		{
			OnDragCancel();
			m_FingerId = -1;
		}
		else if (touch.phase == TouchPhase.Moved)
		{
			OnDragMove(deltaPos, touch.deltaTime);
		}
		else if (touch.phase == TouchPhase.Ended)
		{
			OnDragEnd(deltaPos, touch.deltaTime, curPos);
			m_FingerId = -1;
		}
		else if (touch.phase == TouchPhase.Stationary)
		{
			OnTouchUpdate(curPos);
		}
	}

	void CleanTouch()
	{
		IsDragging = false;
		touchBeginPos = 0;
		touchBeginTime = 0;
		isHolding = false;
		notMovedSinceTouch = false;
		holdingPos = 0;
		lastDragPos = 0;
	}

	void OnDragBegin(float startPos)
	{
		m_Velocity = 0.0f;
		touchBeginPos = startPos;
		touchBeginTime = Time.time;
		lastDragPos = startPos;
		notMovedSinceTouch = true;

		IsDragging = true;
	}

	void OnTouchUpdate(float curPos)
	{
		//check distance from touch pos
		if (notMovedSinceTouch)
		{
			notMovedSinceTouch = (Mathf.Abs(touchBeginPos - curPos) < maxPopupMoveDistance);
		}

		if (IsDragging && Time.time > touchBeginTime + minPopupHoldDuration && notMovedSinceTouch)
		{
			//Debug.Log("time: " + Time.time + " begin: " +  touchBeginTime + " end: "  + (touchBeginTime + minPopupHoldDuration) );
			//if(Mathf.Abs(touchBeginPos - curPos) < maxPopupMoveDistance)
			{
				isHolding = true;
				holdingPos = curPos;
			}
		}
	}

	void OnDragMove(float deltaPos, float deltaTime)
	{
		// dragging
		if (isHorizontal)
			ScrollDelta = new Vector2(deltaPos, 0);
		else
			ScrollDelta = new Vector2(0, deltaPos);

		// ignore small deltaTime, it can cause ultra-high velocity
		if (deltaTime >= 0.0001f)
		{
			float inr = (deltaPos/deltaTime);
			AddInertia(inr);
		}
	}

	void OnDragEnd(float deltaPos, float deltaTime, float curPos)
	{
		timeTouchPhaseEnded = Time.time;

		// ignore small deltaTime, it can cause ultra-high velocity
		if (deltaTime >= 0.0001f)
		{
			float inr = (deltaPos/deltaTime);
			AddInertia(inr);
		}

		if ((Time.time < touchBeginTime + maxTapDuration) && (Mathf.Abs(curPos - touchBeginPos) < maxTapMoveDistance))
		{
			tapEvent = true;
			tapEventPos = curPos;
		}
		else
		{
			m_Velocity = GetInertia();
			//m_Velocity = Mathf.Clamp(GetInertia(), -MaxInertia, MaxInertia);
		}

		lastValues.Clear();
		CleanTouch();
	}

	public bool HasMomentum()
	{
		return (MoveSpeed > MinSpeed); //TODO: momentum pocitat podle velikosti displaye nebo realnemu rozestupu mezi itemy
	}

	void OnDragCancel()
	{
		CleanTouch();
	}

	void MouseUpdate()
	{
		float curPos = isHorizontal ? Input.mousePosition.x : -Input.mousePosition.y;
		if (Input.GetMouseButtonDown(0))
		{
			//check if touch accured inside area we are interested in
			Vector2 testPos = Input.mousePosition;

			if (!m_ActiveArea.Contains(testPos))
			{
				//Debug.Log("click outside test area: " + testPos);
				return;
			}
			else
			{
				//Debug.Log("click in test area. Area: " + m_ActiveArea + " pos: " + testPos);
			}

			lastMouseDragPos = Input.mousePosition;
			OnDragBegin(curPos);
		}
		else if (Input.GetMouseButtonUp(0))
		{
			Vector3 mp = (Input.mousePosition - lastMouseDragPos);
			float deltaPos = isHorizontal ? mp.x : -mp.y;
			OnDragEnd(deltaPos, Time.deltaTime, curPos);
			lastMouseDragPos = Input.mousePosition;
		}
		else if (Input.GetMouseButton(0))
		{
			Vector3 mp = (Input.mousePosition - lastMouseDragPos);
			float deltaPos = isHorizontal ? mp.x : -mp.y;

			if (Mathf.Abs(deltaPos) > 0.001f)
				OnDragMove(deltaPos, Time.deltaTime);
			else
				OnTouchUpdate(curPos);

			lastMouseDragPos = Input.mousePosition;
		}

		/*if(Input.GetMouseButton(0))
			lastMousePos = Input.mousePosition;*/
	}

	float GetFriction()
	{
		//apply greater velicity over time
		float timeSinceTouchEnd = Time.time - timeTouchPhaseEnded;
		float t = Mathf.Clamp(timeSinceTouchEnd/frictionDuration, 0, 1);
		float currFriction = Mathf.Lerp(minFriction, maxFriction, t);
		//Debug.Log(currFriction);
		return currFriction;
	}

	void UpdateVelocity()
	{
		float updateDelta = (Time.time - m_LastUpdateTime);

		//apply friction to velocity
		//UpdateFrictionConstant( Time.deltaTime );

		while (updateDelta > inertiaFixedUpdate)
		{
			updateDelta -= inertiaFixedUpdate;
			UpdateFrictionProportional();
		}
		m_LastUpdateTime = Time.time - updateDelta; /**/

		//compute move delta
		float deltaPos = (m_Velocity*Time.deltaTime);

		if (isHorizontal)
			ScrollDelta = new Vector2(deltaPos, 0);
		else
			ScrollDelta = new Vector2(0, deltaPos);
	}

	void UpdateFrictionProportional()
	{
		m_Velocity -= (m_Velocity*GetFriction());
	}

	void UpdateFrictionConstant(float inDeltaTime)
	{
		if (m_Velocity != 0)
		{
			//Debug.Log("Vel : " + m_Velocity);
			float frameFrict = frictionPerSec*inDeltaTime;

			//apply friction opposite to velocity
			if (m_Velocity > 0)
			{
				m_Velocity -= frameFrict;
				if (m_Velocity < 0)
					m_Velocity = 0;
			}
			else
			{
				m_Velocity += frameFrict;
				if (m_Velocity > 0)
					m_Velocity = 0;
			}
		}
	}
}
