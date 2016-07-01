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

public abstract class GuiComponent<T>
				where T : Component
{
	class UpdateTimer
	{
		float m_RemainingTime = -1.0f;

		public bool CanUpdate(float interval)
		{
			if (interval <= 0.0f)
				return true;

			m_RemainingTime -= Time.deltaTime;
			if (m_RemainingTime > 0.0f)
				return false;

			m_RemainingTime = interval;

			return true;
		}

		public void Reset()
		{
			m_RemainingTime = -1.0f;
		}
	}

	// PRIVATE MEMBERS

	UpdateTimer m_UpdateTimer = new UpdateTimer();
	UpdateTimer m_LateUpdateTimer = new UpdateTimer();

	// GETTERS / SETTERS

	public T Owner { get; private set; }

	public bool IsInitialized
	{
		get { return Owner != null ? true : false; }
	}

	public bool IsVisible { get; private set; }

	// ABSTRACT INTERFACE

	public virtual float UpdateInterval
	{
		get { return 0.0f; }
	}

	public virtual float LateUpdateInterval
	{
		get { return 0.0f; }
	}

	protected virtual bool OnInit()
	{
		return true;
	}

	protected virtual void OnDestroy()
	{
	}

	protected virtual void OnShow()
	{
	}

	protected virtual void OnHide()
	{
	}

	protected virtual void OnUpdate()
	{
	}

	protected virtual void OnLateUpdate()
	{
	}

	// PUBLIC METHODS

	public void Init(T owner)
	{
		if (Owner != null)
			return;
		Owner = owner;

		if (OnInit() == false)
		{
			Owner = null;
		}
	}

	public void Destroy(T owner)
	{
		if (Owner == null)
			return;

		OnDestroy();

		Owner = null;
	}

	public void Show()
	{
		if (IsInitialized == false)
			return;

		if (IsVisible == true)
			return;
		IsVisible = true;

		// reset update timers so we will get first update just after show
		m_UpdateTimer.Reset();
		m_LateUpdateTimer.Reset();

		OnShow();
	}

	public void Hide()
	{
		if (IsInitialized == false)
			return;

		if (IsVisible == false)
			return;
		IsVisible = false;

		OnHide();
	}

	public void Update()
	{
		if (IsVisible == false)
			return;

		if (m_UpdateTimer.CanUpdate(UpdateInterval) == false)
			return;

		OnUpdate();
	}

	public void LateUpdate()
	{
		if (IsVisible == false)
			return;

		if (m_LateUpdateTimer.CanUpdate(LateUpdateInterval) == false)
			return;

		OnLateUpdate();
	}
}
