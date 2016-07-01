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

[HideInInspector]
public class LevelToolsData : MonoBehaviour
{
#if UNITY_EDITOR
	public enum E_ObjectModification
	{
		Disable,
		Enable,
		Static,
		Dynamic,
	};

	static E_ObjectModification[] m_OppositeModification =
	{
		E_ObjectModification.Enable, //  Disable,
		E_ObjectModification.Disable, //  Enable,
		E_ObjectModification.Dynamic, //  Static,
		E_ObjectModification.Static, //  Dynamic,
	};

	public static E_ObjectModification GetOpposite(E_ObjectModification inMod)
	{
		return m_OppositeModification[(int)inMod];
	}

	//[SerializeField]
	[System.Serializable]
	public class ObjectModification
	{
		public GameObject m_GameObject = null;
		public E_ObjectModification m_Operation = E_ObjectModification.Disable;
		public bool m_ApplyOnChild = true;

		public ObjectModification()
		{
		}

		public ObjectModification(GameObject inGameObject, E_ObjectModification inOperation, bool inApplyOnChild)
		{
			m_GameObject = inGameObject;
			m_Operation = inOperation;
			m_ApplyOnChild = inApplyOnChild;
		}
	};

	public enum E_LevelOperation
	{
		None = -1,
		Lightening = 0,
		PVS = 1,
		Navmesh = 2,
	}

	// modification on scene needed for building ligtmaps
	public List<ObjectModification> m_LighteningMod = new List<ObjectModification>();
	// modification on scene needed for building PVS
	public List<ObjectModification> m_PVSMod = new List<ObjectModification>();
	// modification on scene needed for building Navmeshes
	public List<ObjectModification> m_NavMeshMod = new List<ObjectModification>();

	// Actual operation which was made
	public E_LevelOperation m_ActiveOperation = E_LevelOperation.None;
	// List of modifications which was made on scene/level.
	// we need this list to make a correct undo after finishing active operation.
	public List<ObjectModification> m_BackupDtata = new List<ObjectModification>();

#endif //UNITY_EDITOR
}

/*
 Reasons ::
    some of ours coders (usually me) will have to update timeing of events every time when artist change animations.

Do you plan to improve using of AnimationEvent in next version of UNITY ?

 ***/
