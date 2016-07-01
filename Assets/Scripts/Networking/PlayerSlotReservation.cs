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
using System.Collections.Generic;

public class PlayerSlotReservation
{
	public class Reservation
	{
		public readonly int Id;
		public E_Team Team;
		public float ValidUntil;

		public Reservation(int id)
		{
			Id = id;
		}

		public bool IsValid
		{
			get { return PlayerSlotReservation.TimeNow < ValidUntil; }
		}
	}

	List<Reservation> Reservations = new List<Reservation>();

	static float TimeNow
	{
		get { return Time.realtimeSinceStartup; }
	}

	public int ReservedSlotCount(E_Team team)
	{
		int result = 0;

		foreach (Reservation r in Reservations)
		{
			if ((r.Team == team) && r.IsValid)
				result++;
		}

		return result;
	}

	public int ReservedSlotCount()
	{
		int result = 0;

		foreach (Reservation r in Reservations)
		{
			if (r.IsValid)
				result++;
		}

		return result;
	}

	public void MakeNew(int id, E_Team team, float duration)
	{
		Reservation reservation = new Reservation(id);
		reservation.Team = team;
		reservation.ValidUntil = TimeNow + duration;

		Reservations.Add(reservation);
	}

	public void Cancel(int id)
	{
		Reservations.RemoveAll(e => e.Id == id);
	}

	public void Apply(Reservation reservation)
	{
		Reservations.Remove(reservation);
	}

	public Reservation Find(int id)
	{
		return Reservations.Find(e => e.Id == id);
	}

	public Reservation FindValid(int id)
	{
		Reservation r = Reservations.Find(e => e.Id == id);
		if (r == null)
			return null;

		return r.IsValid ? r : null;
	}

	public void RemoveAll()
	{
		Reservations.RemoveAll(e => true);
	}

	public void RemoveAllInvalid()
	{
		Reservations.RemoveAll(e => !e.IsValid);
	}
}
