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

public class NetworkInterpolatedTransform : MonoBehaviour
{
	public double interpolationBackTime = 0.1;

	internal struct State
	{
		internal double timestamp;
		internal Vector3 pos;
		internal Quaternion rot;
	}

	// We store twenty states with "playback" information
	State[] m_BufferedState = new State[20];
	// Keep track of what slots are used
	int m_TimestampCount;

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		// Always send transform (depending on reliability of the network view)
		if (stream.isWriting)
		{
			Vector3 pos = transform.localPosition;
			Quaternion rot = transform.localRotation;
			stream.Serialize(ref pos);
			stream.Serialize(ref rot);
		}
		// When receiving, buffer the information
		else
		{
			// Receive latest state information
			Vector3 pos = Vector3.zero;
			Quaternion rot = Quaternion.identity;
			stream.Serialize(ref pos);
			stream.Serialize(ref rot);

			// Shift buffer contents, oldest data erased, 18 becomes 19, ... , 0 becomes 1
			for (int i = m_BufferedState.Length - 1; i >= 1; i--)
			{
				m_BufferedState[i] = m_BufferedState[i - 1];
			}

			// Save currect received state as 0 in the buffer, safe to overwrite after shifting
			State state;
			state.timestamp = info.timestamp;
			state.pos = pos;
			state.rot = rot;
			m_BufferedState[0] = state;

			// Increment state count but never exceed buffer size
			m_TimestampCount = Mathf.Min(m_TimestampCount + 1, m_BufferedState.Length);

			// Check integrity, lowest numbered state in the buffer is newest and so on
			for (int i = 0; i < m_TimestampCount - 1; i++)
			{
				if (m_BufferedState[i].timestamp < m_BufferedState[i + 1].timestamp)
					Debug.Log("State inconsistent");
			}

			//Debug.Log("stamp: " + info.timestamp + "my time: " + Network.time + "delta: " + (Network.time - info.timestamp));
		}
	}

	// This only runs where the component is enabled, which is only on remote peers (server/clients)
	void Update()
	{
		if (Network.isServer)
		{
			return;
		}

		Debug.Log("Updating network transform");

		double currentTime = Network.time;
		double interpolationTime = currentTime - interpolationBackTime;
		// We have a window of interpolationBackTime where we basically play 
		// By having interpolationBackTime the average ping, you will usually use interpolation.
		// And only if no more data arrives we will use extrapolation

		// Use interpolation
		// Check if latest state exceeds interpolation time, if this is the case then
		// it is too old and extrapolation should be used
		if (m_BufferedState[0].timestamp > interpolationTime)
		{
			for (int i = 0; i < m_TimestampCount; i++)
			{
				// Find the state which matches the interpolation time (time+0.1) or use last state
				if (m_BufferedState[i].timestamp <= interpolationTime || i == m_TimestampCount - 1)
				{
					// The state one slot newer (<100ms) than the best playback state
					State rhs = m_BufferedState[Mathf.Max(i - 1, 0)];
					// The best playback state (closest to 100 ms old (default time))
					State lhs = m_BufferedState[i];

					// Use the time between the two slots to determine if interpolation is necessary
					double length = rhs.timestamp - lhs.timestamp;
					float t = 0.0F;
					// As the time difference gets closer to 100 ms t gets closer to 1 in 
					// which case rhs is only used
					if (length > 0.0001)
						t = (float)((interpolationTime - lhs.timestamp)/length);

					// if t=0 => lhs is used directly
					transform.localPosition = Vector3.Lerp(lhs.pos, rhs.pos, t);
					transform.localRotation = Quaternion.Slerp(lhs.rot, rhs.rot, t);
					return;
				}
			}
		}
		// Use extrapolation. Here we do something really simple and just repeat the last
		// received state. You can do clever stuff with predicting what should happen.
		else
		{
			State latest = m_BufferedState[0];

			transform.localPosition = latest.pos;
			transform.localRotation = latest.rot;
		}
	}
}
