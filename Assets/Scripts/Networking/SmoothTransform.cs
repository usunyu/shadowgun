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

//#define DEBUG_VISUAL

using UnityEngine;

public class SmoothTransform : MonoBehaviour
{
	public enum EProxySimulationType
	{
		PrefferInterpolation,
		ForceExtrapolation,
		RawData
	}

#if DEBUG_VISUAL
	
	public GameObject DebugPrefabExtrapolate = null;
	public GameObject DebugPrefabNone = null;
	public GameObject DebugPrefabVelocity = null;
	
	private GameObject m_DbgExtrapolate = null;
	private GameObject m_DbgNone = null;
	private GameObject m_DbgVelocity = null;
	
	void Start()
	{
		if ( BuildInfo.Version.Stage != BuildInfo.Stage.Release )
		{
			m_DbgExtrapolate = CreateDebugPrefab( DebugPrefabExtrapolate );
			m_DbgNone = CreateDebugPrefab( DebugPrefabNone );		
			m_DbgVelocity = CreateDebugPrefab( DebugPrefabVelocity );
		}
	}
	
	GameObject CreateDebugPrefab( GameObject prefab )
	{
		if( null != prefab )
		{
			GameObject obj = Instantiate( prefab, Vector3.zero, Quaternion.identity ) as GameObject;
			
			if( null != obj )
			{
				obj.transform.parent = transform;
				obj.transform.localPosition = new Vector3( 0, 1, 0 );
				obj.active = false;
			}
			
			return obj;
		}
		
		return null;
	}
	
#endif // DEBUG_VISUAL

	public static double TimeNow()
	{
		return uLink.Network.time;
		//return (double)Time.time;
	}

	public static double GetTime(uLink.NetworkMessageInfo info)
	{
		return info.timestamp;
		//return (double)Time.time;
	}

	public EProxySimulationType SimulationType;

	//public double interpolationBackTime = 0.05;
	public double interpolationBackTime = 0.1;
	public double extrapolationForwardTime = 0.0;
				  //Must stay 0 here. This value is mentioned to be set in the editor only !!! (for debug/testing purposes)
	public double extrapolationLimit = 0.5;

	protected double lastUpdateTime;

	struct State
	{
		public double timestamp;
		public Vector3 pos;
		public Vector3 vel;
		public Quaternion rot;
	}

	// We store twenty states with "playback" information
	State[] proxyStates = new State[20];

	// Keep track of what slots are used
	int proxyStateCount;

	Vector3 m_Position = new Vector3();
	Vector3 m_Velocity = new Vector3();
	Quaternion m_Rotation = new Quaternion();

	public Vector3 Position
	{
		get { return m_Position; }
	}

	public Vector3 Velocity
	{
		get { return m_Velocity; }
	}

	public Quaternion Rotation
	{
		get { return m_Rotation; }
	}

	public void Reset()
	{
		proxyStateCount = 0;
		proxyStates = new State[20];
	}

	public void AddState(double Timestamp, Vector3 pos, Vector3 vel, Quaternion rot)
	{
		// Shift the buffer sideways, deleting state 20
		for (int i = proxyStates.Length - 1; i >= 1; i--)
		{
			proxyStates[i] = proxyStates[i - 1];
		}

		// Record current state in slot 0
		proxyStates[0].pos = pos;
		proxyStates[0].vel = vel;
		proxyStates[0].rot = rot;
		proxyStates[0].timestamp = Timestamp;

		// Update used slot count, however never exceed the buffer size
		// Slots aren't actually freed so this just makes sure the buffer is
		// filled up and that uninitalized slots aren't used.
		proxyStateCount = Mathf.Min(proxyStateCount + 1, proxyStates.Length);

		// Check if states are in order
		if (proxyStates[0].timestamp < proxyStates[1].timestamp)
		{
			Debug.LogError("Timestamp inconsistent: " + proxyStates[0].timestamp + " should be greater than " + proxyStates[1].timestamp);
		}
	}

	protected bool CalcInterpolation(double timeNow, ref Vector3 retPosition, ref Quaternion retRotation, ref Vector3 retVelocity)
	{
		// This is the target playback time of the rigid body
		double interpolationTime = timeNow - interpolationBackTime;

		// Use interpolation if the target playback time is present in the buffer
		if (proxyStates[0].timestamp <= interpolationTime)
		{
			//Debug.Log( "Cant't extrapolate: time=" + TimeNow() + " delta=" + (proxyStates[0].timestamp - interpolationTime) );
			return false;
		}

		// Go through buffer and find correct state to play back
		for (int i = 0; i < proxyStateCount; i++)
		{
			if (proxyStates[i].timestamp <= interpolationTime || i == proxyStateCount - 1)
			{
				// The state one slot newer (<100ms) than the best playback state
				State rhs = proxyStates[Mathf.Max(i - 1, 0)];
				// The best playback state (closest to 100 ms old (default time))
				State lhs = proxyStates[i];

				// Use the time between the two slots to determine if interpolation is necessary
				double length = rhs.timestamp - lhs.timestamp;
				float t = 0.0F;
				// As the time difference gets closer to 100 ms t gets closer to 1 in 
				// which case rhs is only used
				// Example:
				// Time is 10.000, so sampleTime is 9.900 
				// lhs.time is 9.910 rhs.time is 9.980 length is 0.070
				// t is 9.900 - 9.910 / 0.070 = 0.14. So it uses 14% of rhs, 86% of lhs
				if (length > 0.0001)
					t = (float)((interpolationTime - lhs.timestamp)/length);

				// if t=0 => lhs is used directly
				//transform.position = ( lhs.pos == rhs.pos ? lhs.pos : Vector3.Lerp( lhs.pos, rhs.pos, t ) );
				//rotation = ( lhs.rot == rhs.rot ? lhs.rot : Quaternion.Slerp( lhs.rot, rhs.rot, t ) );
				//velocity = proxyStates[i].vel;

				retPosition = (lhs.pos == rhs.pos ? lhs.pos : Vector3.Lerp(lhs.pos, rhs.pos, t));
				retRotation = (lhs.rot == rhs.rot ? lhs.rot : Quaternion.Slerp(lhs.rot, rhs.rot, t));
				retVelocity = proxyStates[i].vel;

				return true;
			}
		}

		// that should never happen
		return false;
	}

	protected bool CalcExtrapolation(double timeNow, ref Vector3 retPosition, ref Quaternion retRotation, ref Vector3 retVelocity)
	{
		State latest = proxyStates[0];

		double totalExtrapolationLength = (timeNow - latest.timestamp);

		// Don't extrapolation for more than 500 ms, you would need to do that carefully
		if (totalExtrapolationLength < extrapolationLimit)
		{
			double extrapolationLength = totalExtrapolationLength;

			retVelocity = latest.vel;
			retRotation = latest.rot;

			// The trick: we need to take in account the interpolationBackTime otherwise there will be a time discontinuity between interpolation and extrapolation
#if UNITY_EDITOR
			retPosition = latest.pos + latest.vel*(float)(extrapolationLength + extrapolationForwardTime - interpolationBackTime);
#else
			retPosition = latest.pos + latest.vel * (float)(extrapolationLength - interpolationBackTime);
#endif

//			if( character.enabled )
//				character.SimpleMove( latest.vel );

			return true;
		}

		// wea re not going to extrapolate more as we already extrapolated to the limit
		return false;
	}

#if UNITY_EDITOR

	void DebugRenderPosition(Vector3 pos, Quaternion rot, Vector3 vel, Color color)
	{
		Debug.DrawLine(pos, pos + new Vector3(0.0f, 2.5f, 0.0f), color);
		Vector3 arrowPos = pos + new Vector3(0.0f, 2.0f, 0.0f);
		Debug.DrawLine(arrowPos, arrowPos + rot*Vector3.forward, color);
		Debug.DrawLine(arrowPos, arrowPos + vel, color, 0.04f);
	}

#endif // UNITY_EDITOR	

	// We have a window of interpolationBackTime where we basically play 
	// By having interpolationBackTime the average ping, you will usually use interpolation.
	// And only if no more data arrives we will use extra polation
	// @return TRUE if computed values was changed
	public bool UpdateCustom()
	{
		bool Result = false;
		double timeNow = TimeNow();

#if DEBUG_VISUAL
		if( null != m_DbgVelocity )
		{
			m_DbgVelocity.active = true;
			
			Vector3 scale = m_DbgVelocity.transform.localScale;
			scale.z = 1+m_Velocity.magnitude;
			m_DbgVelocity.transform.localPosition = Vector3.up*2.0f;
			m_DbgVelocity.transform.localScale = scale;
			
			if( m_Velocity.magnitude > Mathf.Epsilon )
			{
				m_DbgVelocity.transform.LookAt( m_Position+10*m_Velocity );
			}
			else
			{
				m_DbgVelocity.transform.rotation = m_Rotation;
			}
		}		
#endif // DEBUG_VISUAL

		if (lastUpdateTime > 0.01f)
		{
			Vector3 interpolatedPosition = new Vector3();
			Quaternion interpolatedRotation = new Quaternion();
			Vector3 interpolatedVelocity = new Vector3();
			bool canInterpolate = CalcInterpolation(timeNow, ref interpolatedPosition, ref interpolatedRotation, ref interpolatedVelocity);

			Vector3 extrapolatedPosition = new Vector3();
			Quaternion extrapolatedRotation = new Quaternion();
			Vector3 extrapolatedVelocity = new Vector3();
			bool canExtrapolate = CalcExtrapolation(timeNow, ref extrapolatedPosition, ref extrapolatedRotation, ref extrapolatedVelocity);

#if UNITY_EDITOR
			DebugRenderPosition(proxyStates[0].pos, proxyStates[0].rot, proxyStates[0].vel, Color.green);
#endif // UNITY_EDITOR

			if ((SimulationType == EProxySimulationType.PrefferInterpolation) && canInterpolate)
			{
				m_Position = interpolatedPosition;
				m_Rotation = interpolatedRotation;
				m_Velocity = interpolatedVelocity;

#if DEBUG_VISUAL
				ShowDebugPrefabs( false, false );
#endif // DEBUG_VISUAL

#if UNITY_EDITOR
				DebugRenderPosition(interpolatedPosition, interpolatedRotation, interpolatedVelocity, Color.cyan);
#endif
			}
			else if ((SimulationType == EProxySimulationType.ForceExtrapolation) || canExtrapolate)
			{
				m_Position = extrapolatedPosition;
				m_Rotation = extrapolatedRotation;
				m_Velocity = extrapolatedVelocity;

#if DEBUG_VISUAL
				ShowDebugPrefabs( true, false );
#endif // DEBUG_VISUAL

#if UNITY_EDITOR
				DebugRenderPosition(extrapolatedPosition, extrapolatedRotation, extrapolatedVelocity, Color.red);
#endif
			}
			else
			{
				State latest = proxyStates[0];
				m_Position = latest.pos;
				m_Rotation = latest.rot;
				m_Velocity = latest.vel;

#if DEBUG_VISUAL			
				ShowDebugPrefabs( false, true );
#endif // DEBUG_VISUAL
			}

			Result = true;
		}

		lastUpdateTime = timeNow;

		return Result;
	}

#if DEBUG_VISUAL
	
	private void ShowDebugPrefabs( bool extrapolate, bool none )
	{
		if( null != m_DbgExtrapolate )
		{
			if( m_DbgExtrapolate.active != extrapolate )
			{
				m_DbgExtrapolate.active = extrapolate;
			}
		}
		
		if( null != m_DbgNone )
		{
			if( m_DbgNone.active != none )
			{
				m_DbgNone.active = none;
			}
		}
	}
	
#endif // DEBUG_VISUAL
};
