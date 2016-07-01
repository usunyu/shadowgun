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

using System;
using System.Collections.Generic;
using UnityEngine;

#if false
public class TouchEvent : System.Object
{
	static int _id;

	public int Id;// { get { return UID; } private set { UID = value;} }


	private List<Vector2> Positions = new List<Vector2>();
	private static Queue<TouchEvent> UnusedEvents = new Queue<TouchEvent>();

	public TouchPhase CurrentPhase;
	public float StartTime;


	public static TouchEvent Create(Touch touch) 
	{
		TouchEvent iEvent = null; 
		if(UnusedEvents.Count > 0)
			iEvent = UnusedEvents.Dequeue();
		else 
			iEvent = new  TouchEvent();

		iEvent.Id = touch.fingerId;
		iEvent.CurrentPhase = TouchPhase.Began;
		iEvent.StartTime = Time.timeSinceLevelLoad;
		iEvent.Positions.Add(touch.position);

		return iEvent;
	}
	
	public static void Return(TouchEvent iEvent)
	{
		iEvent.Id = -1;
		iEvent.Positions.Clear();
		iEvent.CurrentPhase = TouchPhase.Canceled;
		UnusedEvents.Enqueue(iEvent);
	}

	public void Update(Touch touch)
	{
		CurrentPhase = touch.phase;
		Positions.Add(touch.position); 
	}

	public Vector2 GetStartPos() { return Positions[0]; }
	public Vector2 GetEndPos() { return Positions[Positions.Count -1];}
	public float GetTouchTime() { return Time.timeSinceLevelLoad - StartTime; }

    public Vector2 GetPos(int index) { return Positions[index]; }
    public int CountOfPositions { get { return Positions.Count; } }
    
}

public interface InputInterface
{
	void ReceiveInput(TouchEvent iEvent);
}

public class InputComponent : MonoBehaviour
{
	static int MaxTouches = 4;
	private List<TouchEvent> TouchEvents = new List<TouchEvent>();
	private List<InputInterface> Receivers = new List<InputInterface>();

	public static InputComponent Instance = null;

	void Awake()
	{
		Instance = this;
	}


	void Update()
	{
                    
		if (Input.touchCount == 0)
			return;
        
		Touch touch = Input.GetTouch(0);

		if (touch.phase == TouchPhase.Began)
		{
			TouchBegin(touch);
		}
		else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
		{
			TouchUpdate(touch);
		}
		else if (touch.phase == TouchPhase.Ended)
		{
			TouchEnd(touch);
		}
	}

	private void TouchBegin(Touch touch)
	{
		if (TouchEvents.Count == MaxTouches)
			return;

		/*if (touch.position.y > 450)
		{
			//			  Debug.Log(touch.position.y.ToString());
			return;
		}*/


		TouchEvent newtTouch = TouchEvent.Create(touch);
		TouchEvents.Add(newtTouch);

		SendToReceivers(newtTouch);

	}

	private void TouchEnd(Touch touch)
	{
		TouchEvent touchEvent;

		for (int i = 0 ; i < TouchEvents.Count ; i++)
		{
			if (((TouchEvent)TouchEvents[i]).Id == touch.fingerId)
			{
				touchEvent = (TouchEvent)TouchEvents[i];
				touchEvent.Update(touch);

				SendToReceivers(touchEvent);

				TouchEvents.RemoveAt(i);
				TouchEvent.Return(touchEvent);
				return;
			}
		}
	}

	private void TouchUpdate(Touch touch)
	{
		TouchEvent touchEvent;
		for (int i = 0 ; i < TouchEvents.Count ; i++)
		{
			if (((TouchEvent)TouchEvents[i]).Id == touch.fingerId)
			{
				touchEvent = (TouchEvent)TouchEvents[i];
				touchEvent.Update(touch);

				SendToReceivers(touchEvent);

				break;
			}
		}
	}


	private void SendToReceivers(TouchEvent touch)
	{
		for (int i = 0 ; i < Receivers.Count ; i++)
			Receivers[i].ReceiveInput(touch);
	}

	public void AddReceiver(InputInterface inputReceiver)
	{
		Receivers.Add(inputReceiver);
	}

}
#endif
