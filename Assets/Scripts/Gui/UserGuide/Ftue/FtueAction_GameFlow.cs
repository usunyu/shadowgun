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

namespace FtueAction
{
	public class Welcome : Base
	{
		// C-TOR

		public Welcome(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			LabelId = 9960081;
			DescriptionId = 9960082;
		}
	}

	public class FinalText : Base
	{
		// C-TOR

		public FinalText(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			LabelId = 9960401;
			DescriptionId = 9960402;
			DescriptionScale = new Vector2(0.8f, 0.8f);
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Rank >= MinimalRank ? base.CanActivate() : false;
		}
	}

	public class Profile : Base
	{
		// PRIVATE METHODS

		bool WasVisible = false;

		// C-TOR

		public Profile(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			ScreenType = typeof (GuiPopupPlayerProfile);
			LabelId = 9960091;
			DescriptionId = 9960092;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (WasVisible == false && Screen.IsVisible == false)
				return false;

			WasVisible = true;
			if (Screen.IsVisible == true)
				return false;

			State = FtueState.Finished;

			return true;
		}
	}

	public class DeathMatch : Base
	{
		// C-TOR

		public DeathMatch(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			//ScreenType    = typeof(GuiScreenLobbyRandom);
			LabelId = 9960001;
			DescriptionId = 9960002;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (Client.Instance == null)
				return false;
			if (Client.Instance.GameState.GameType != E_MPGameType.DeathMatch)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			return true;
		}
	}

	public class ZoneControl : Base
	{
		// PUBLIC MEMBERS

		public static int DefaultMinimalRank
		{
			get
			{
				GameTypeInfo gameInfo = GameInfoSettings.GetGameInfo(E_MPGameType.ZoneControl);
				return gameInfo != null ? gameInfo.MinimalDesiredRankToPlay : 1;
			}
		}

		// C-TOR

		public ZoneControl(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			ScreenType = typeof (GuiScreenLobbyRandom);
			LabelId = 9960601;
			DescriptionId = 9960602;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (Client.Instance == null)
				return false;
			if (Client.Instance.GameState.GameType != E_MPGameType.ZoneControl)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Rank >= MinimalRank ? true : false;
		}
	}

	public class Spawn : Base
	{
		// C-TOR

		public Spawn(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, true)
		{
			LabelId = 9960011;
			DescriptionId = 9960012;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (IsSpawned == false)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			return base.CanActivate() || IsSpawned;
		}
	}

	public class Hud : Base
	{
		// C-TOR

		public Hud(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, true)
		{
			LabelId = 9960501;
			DescriptionId = 9960502;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			return IsSpawned;
		}
	}

	public class Controls : Base
	{
		// PRIVATE METHODS

		bool WasVisible = false;

		// C-TOR

		public Controls(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			ScreenType = typeof (GuiScreenOptions); //GuiPageOptionsControls);
			LabelId = 9960021;
			DescriptionId = 9960022;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (WasVisible == false && Screen.IsVisible == false)
				return false;

			WasVisible = true;
			if (Screen.IsVisible == true)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			return true;
		}
	}

	public class Stats : Base
	{
		// PRIVATE METHODS

		bool WasVisible = false;

		// C-TOR

		public Stats(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			ScreenType = typeof (GuiScreenPlayerStats);
			LabelId = 9960031;
			DescriptionId = 9960032;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (WasVisible == false && Screen.IsVisible == false)
				return false;

			WasVisible = true;
			if (Screen.IsVisible == true)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override string HintText()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			if (ppi != null && ppi.Experience <= 0)
				return TextDatabase.instance[09900010];
			return base.HintText();
		}

		public override bool CanActivate()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Experience > 0 ? base.CanActivate() : false;
		}
	}

	public class Leaderboards : Base
	{
		// PRIVATE METHODS

		bool WasVisible = false;

		// C-TOR

		public Leaderboards(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			ScreenType = typeof (GuiScreenLeaderBoards);
			LabelId = 9960041;
			DescriptionId = 9960042;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (WasVisible == false && Screen.IsVisible == false)
				return false;

			WasVisible = true;
			if (Screen.IsVisible == true)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Experience > 0 ? base.CanActivate() : false;
		}
	}

	public class Friends : Base
	{
		// PRIVATE METHODS

		bool WasVisible = false;

		// C-TOR

		public Friends(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			ScreenType = typeof (FriendsScreen);
			LabelId = 9960201;
			DescriptionId = 9960202;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (WasVisible == false && Screen.IsVisible == false)
				return false;

			WasVisible = true;
			if (Screen.IsVisible == true)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Rank >= MinimalRank ? base.CanActivate() : false;
		}
	}

	public class Chat : Base
	{
		// PRIVATE METHODS

		bool WasVisible = false;

		// C-TOR

		public Chat(Ftue ftue, int minimalRank = 0)
						: base(ftue, minimalRank, false)
		{
			ScreenType = typeof (GuiScreenChat);
			LabelId = 9960301;
			DescriptionId = 9960302;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (WasVisible == false && Screen.IsVisible == false)
				return false;

			WasVisible = true;
			if (Screen.IsVisible == true)
				return false;

			State = FtueState.Finished;

			return true;
		}

		// FTUEACTION.BASE INTERFACE

		public override bool CanActivate()
		{
			PlayerPersistantInfo ppi = PPIManager.Instance.GetLocalPPI();
			return ppi != null && ppi.Rank >= MinimalRank ? base.CanActivate() : false;
		}
	}
}
