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
	public class RankUp : Base
	{
		// PUBLIC MEMBERS

		public int DesiredRank { get; private set; }

		// C-TOR

		public RankUp(Ftue ftue, int minimalRank, int desiredRank, int labelId, int descriptionId)
						: base(ftue, minimalRank, false)
		{
			DesiredRank = desiredRank;
			LabelId = labelId;
			DescriptionId = descriptionId;
		}

		// USERGUIDEACTION INTERFACE

		protected override bool OnUpdate()
		{
			if (base.OnUpdate() == false)
				return false;

			if (GuiFrontendMain.IsVisible == false)
				return false;

			if (GuideData.LocalPPI.Rank < DesiredRank)
				return false;

			//bool justPromoted = GuideData.LastRoundResult != null ? GuideData.LastRoundResult.NewRank : false;

			// blink with options/controls
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
