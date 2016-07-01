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
using JsonData = LitJson.JsonData;

namespace FtueAction
{
	public abstract class Base : UserGuideAction
	{
		// PRIVATE MEMBERS

		FtueState m_State;
		int m_LabelId;
		int m_DescId;
		string m_UniqueId;

		// PUBLIC MEMBERS

		public string UniqueId
		{
			get
			{
				if (string.IsNullOrEmpty(m_UniqueId) == true)
				{
					m_UniqueId = string.Format("{0}{1}{2}", GetType().Name, LabelId, DescriptionId);
				}
				return m_UniqueId;
			}
		}

		public Ftue Ftue { get; private set; }
		public int MinimalRank { get; private set; }
		public GuiScreen Screen { get; private set; }
		public bool IsSpawned { get; private set; }
		public bool ShouldBeIngame { get; private set; }

		public bool IsIdle
		{
			get { return m_State == FtueState.None; }
		}

		public bool IsActive
		{
			get { return m_State == FtueState.InProgress; }
		}

		public bool IsFinished
		{
			get { return m_State == FtueState.Finished; }
		}

		public bool IsSkipped
		{
			get { return m_State == FtueState.Skipped; }
		}

		public FtueState State
		{
			get { return m_State; }
			protected set { SetState(value); }
		}

		public int LabelId
		{
			get { return m_LabelId; }
			protected set
			{
				m_UniqueId = null;
				m_LabelId = value;
			}
		}

		public int DescriptionId
		{
			get { return m_DescId; }
			protected set
			{
				m_UniqueId = null;
				m_DescId = value;
			}
		}

		public string Label
		{
			get { return TextDatabase.instance[m_LabelId]; }
		}

		public string Description
		{
			get { return TextDatabase.instance[m_DescId]; }
		}

		public Vector2 DescriptionScale { get; protected set; }
		public System.Type ScreenType { get; protected set; }

		// ABSTRACT INTERFACE

		public virtual string HintText()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			if (ppi != null && ppi.Rank < MinimalRank)
				return string.Format(TextDatabase.instance[09900007], MinimalRank);
			if (ShouldBeIngame == false)
				return GuiFrontendMain.IsVisible ? null : TextDatabase.instance[09900008];
			if (GuiFrontendIngame.IsVisible == false && GuiFrontendIngame.IsHudVisible == false)
				return TextDatabase.instance[09900009];
			return null;
		}

		public virtual bool CanActivate()
		{
			if (ShouldBeIngame == false)
				return GuiFrontendMain.IsVisible;
			return GuiFrontendIngame.IsVisible || GuiFrontendIngame.IsHudVisible;
		}

		// C-TOR

		public Base(Ftue ftue, int minimalRank, bool shouldBeIngame)
		{
			Ftue = ftue;
			State = FtueState.None;
			Priority = (int)E_UserGuidePriority.Tutorial + Index;
			MinimalRank = Mathf.Clamp(minimalRank, 1, PlayerPersistantInfo.MAX_RANK);
			ShouldBeIngame = shouldBeIngame;
			DescriptionScale = new Vector2(1, 1);
		}

		// PUBLIC METHODS

		public void Load(JsonData data)
		{
			if (data.IsObject == false)
				return;

			State = data.HasValue("State") ? (FtueState)(int)data["State"] : FtueState.None;

			// reset state if we are not finished yet
			if (State == FtueState.InProgress)
			{
				State = FtueState.None;
			}
		}

		public JsonData Save()
		{
			JsonData data = new JsonData();
			data["State"] = (int)State;
			return data;
		}

		public void Activate()
		{
			State = FtueState.InProgress;
		}

		public void Deactivate()
		{
			Terminate();
		}

		public void Skip()
		{
			if (IsFinished == false)
			{
				State = FtueState.Skipped;
			}
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnInitialize()
		{
			if (base.OnInitialize() == false)
				return false;

			if (ShouldBeIngame == true)
			{
				ComponentPlayerMPOwner.OnActivated += OnPlayerActivated;
				ComponentPlayerMPOwner.OnDeactivated += OnPlayerDeactivated;
			}

			return true;
		}

		protected override void OnDeinitialize()
		{
			if (ShouldBeIngame == true)
			{
				ComponentPlayerMPOwner.OnActivated -= OnPlayerActivated;
				ComponentPlayerMPOwner.OnDeactivated -= OnPlayerDeactivated;
			}

			base.OnDeinitialize();
		}

		protected override bool OnExecute()
		{
			if (IsSkipped == true)
				return false;
			if (IsFinished == true)
				return false;

			if (Ftue == null)
				return false;

			return true;
		}

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (State != FtueState.InProgress)
				return false;

			if (ScreenType != null && Screen == null)
			{
				if (GuideData.Menu == null)
					return false;

				Screen = GuideData.Menu.GetComponentInChildren(ScreenType) as GuiScreen;
				if (Screen == null)
					Screen = GuideData.Menu.transform.parent.GetComponentInChildren(ScreenType) as GuiScreen;
				if (Screen == null)
					return false;
			}

			GuideData.ShowOffers = false;

			return true;
		}

		protected override void OnTerminate()
		{
			if (State == FtueState.InProgress)
			{
				State = FtueState.None;
			}

			Screen = null;

			base.OnTerminate();
		}

		// HANDLERS

		void OnPlayerActivated(ComponentPlayerMPOwner localPlayer)
		{
			IsSpawned = true;
		}

		void OnPlayerDeactivated(ComponentPlayerMPOwner localPlayer)
		{
			IsSpawned = false;
		}

		// PRIVATE METHODS

		void SetState(FtueState state)
		{
			if (m_State == state)
				return;
			m_State = state;

#if UNITY_EDITOR
//			Debug.Log(">>>> "+GetType().FullName+".State="+m_State);
#endif
		}
	}
}
