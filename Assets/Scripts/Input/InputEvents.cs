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
using System.Linq;

public enum E_EventKind : byte
{
	Key,
	Mouse,
	Touch
}

public interface IInputEvent
{
	E_EventKind Kind { get; }
}

// KeyEvent

public enum E_KeyState : byte
{
	Pressed,
	Repeating,
	Released
}

public struct KeyEvent : IInputEvent
{
	public E_EventKind Kind
	{
		get { return E_EventKind.Key; }
	}

	public KeyCode Code;
	public E_KeyState State;
	public float StartTime;
	public float EndTime;

	public override string ToString()
	{
		return "KeyEvent(Code=" + Code + ", State=" + State + ")";
	}
}

// Mouse Event

public struct MouseEvent : IInputEvent
{
	public E_EventKind Kind
	{
		get { return E_EventKind.Mouse; }
	}

	public TouchPhase Phase;
	public bool[] Buttons;
	public Vector2 Position;
	public Vector2 StartPosition;
	public Vector2 DeltaPosition;
	public float StartTime;
	public float DeltaTime;
	public float EndTime;
	public float ScrollWheel;

	public override string ToString()
	{
		string buttons = "[";
		buttons += (Buttons == null) ? "" : Buttons.Skip(1).Aggregate(Buttons[0].ToString(), (s, v) => s + "," + v.ToString());
		buttons += "]";
		return "MouseEvent(Phase=" + Phase + ", Buttons=" + buttons + ", Position=" + Position + ", DeltaPosition=" + DeltaPosition +
			   ", DeltaTime=" + DeltaTime + ")";
	}
}

// TouchEvent

public enum E_TouchType
{
	Finger,
	MouseButton
}

public struct TouchEvent : IInputEvent
{
	public E_EventKind Kind
	{
		get { return E_EventKind.Touch; }
	}

	public bool Started
	{
		get { return Phase == TouchPhase.Began; }
	}

	public bool Active
	{
		get { return Phase != TouchPhase.Ended && Phase != TouchPhase.Canceled; }
	}

	public bool Finished
	{
		get { return Phase == TouchPhase.Ended || Phase == TouchPhase.Canceled; }
	}

	public int Id;
	public TouchPhase Phase;
	public E_TouchType Type;
	//public int         TapCount;
	public Vector2 Position;
	public Vector2 StartPosition;
	public Vector2 DeltaPosition;
	public float StartTime;
	public float DeltaTime;
	public float EndTime;

	public override string ToString()
	{
		return "TouchEvent(Id=" + Id + ", Phase=" + Phase + ", Position=" + Position + ", DeltaPosition=" + DeltaPosition + ", DeltaTime=" +
			   DeltaTime + ")";
	}
}
