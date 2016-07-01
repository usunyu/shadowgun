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

public class CircularBuffer<T> : IEnumerable<T>
{
	// PRIVATE METHODS

	readonly int m_Size;

	int m_Count;
	int m_Head;
	int m_Rear;
	T[] m_Data;

	// GETTERS/SETTERS

	public int Size
	{
		get { return m_Size; }
	}

	public int Count
	{
		get { return m_Count; }
	}

	public T Head
	{
		get { return Peek(); }
	}

	public T Rear
	{
		get
		{
			EnsureQueueNotEmpty();
			return m_Data[m_Rear];
		}
	}

	// CTOR

	public CircularBuffer(int size)
	{
		m_Size = size;

		Clear();
	}

	// PUBLIC METHODS

	public void Enqueue(T obj)
	{
		m_Data[m_Rear] = obj;

		if (Count == Size)
		{
			m_Head = Increment(m_Head);
		}
		m_Rear = Increment(m_Rear);
		m_Count = Mathf.Min(m_Count + 1, m_Size);
	}

	public T Dequeue()
	{
		EnsureQueueNotEmpty();

		T res = m_Data[m_Head];
		m_Data[m_Head] = default(T);
		m_Head = Increment(m_Head);
		m_Count = Mathf.Max(0, m_Count - 1);

		return res;
	}

	public T Peek()
	{
		EnsureQueueNotEmpty();

		return m_Data[m_Head];
	}

	public void Clear()
	{
		m_Count = 0;
		m_Head = 0;
		m_Rear = 0;
		m_Data = new T[m_Size];
	}

	public bool Contains(T other)
	{
		return System.Array.IndexOf(m_Data, other) >= 0 ? true : false;
	}

	public void FromArray(T[] other)
	{
		Clear();
		foreach (var item in other)
		{
			Enqueue(item);
		}
	}

	public T[] ToArray()
	{
		T[] copy = new T[m_Count];
		int pos = m_Head;
		for (int idx = 0; idx < m_Count; ++idx)
		{
			copy[idx] = m_Data[pos];
			pos = Increment(pos);
		}
		return copy;
	}

	// IENUMERABLE INTERFACE

	public IEnumerator<T> GetEnumerator()
	{
		int pos = m_Head;
		for (int idx = 0; idx < m_Count; ++idx)
		{
			yield return m_Data[pos];
			pos = Increment(pos);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	// PRIVATE METHODS

	int Increment(int value)
	{
		return (value + 1)%m_Size;
	}

	void EnsureQueueNotEmpty()
	{
		if (m_Count == 0)
		{
			throw new System.IndexOutOfRangeException();
		}
	}
}
