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
using UnityEditor;
using System.Collections.Generic;

public class InspectorUtils
{
	public static void VizualizeList<T>(ref List<T> inList) where T : Object	
	{	VizualizeList<T>(ref inList, false);	}
	public static void VizualizeList<T>(ref List<T> inList, bool inRemoveEmptyItems) where T : Object
	{
		int listSize = (inList == null) ? 0: inList.Count;
		int newListSize = listSize;
		
		newListSize = EditorGUILayout.IntField("Size", listSize);
		
		if(listSize > newListSize)
		{
			inList.RemoveRange(newListSize, listSize-newListSize);
		}
		else if(listSize < newListSize)
		{
			for(int i = listSize; i < newListSize; i++)
				inList.Add(null);
		}
		
		if(inList != null)
		{
			for (int i = 0; i < inList.Count; i++)
			{
				inList[i] = (T)EditorGUILayout.ObjectField("Element " + i, inList[i], typeof(T), true);
			}
		}
		
		if(inRemoveEmptyItems)
		{
			for (int i = inList.Count-1; i>=0; --i)
			{
				if(inList[i] == null)
				{
					inList.RemoveAt(i);
				}
			}
		}
		
		T newObject = null;
		newObject = (T)EditorGUILayout.ObjectField("new Element", newObject, typeof(T), true);
	
		if(newObject != null)
		{
			inList.Add(newObject);
		}
	}	
	
   
}
