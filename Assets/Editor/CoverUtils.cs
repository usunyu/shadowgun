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

public struct MinMaxPair
{
	public float m_MinValue;
	public float m_MaxValue;
	
	public void Reset()
	{
		m_MinValue = float.MaxValue; // this is correct, not by a mistake
		m_MaxValue = float.MinValue;
	}
	
	public void Absorb( float distance )
	{
		m_MaxValue = Mathf.Max( m_MaxValue, distance );
		m_MinValue = Mathf.Min( m_MinValue, distance );
	}
	
	public void Absorb( MinMaxPair pair )
	{
		Absorb( pair.m_MinValue );
		Absorb( pair.m_MaxValue );
	}
	
	public bool IsValid()
	{
		return m_MinValue <= m_MaxValue;
	}
};

public class CoverUtils
{
	public static bool VisualizeTests;
	public static Color VisualizationColor = Color.yellow;
	
	private static float ProceedOneCoverDistanceCast( Vector3 fromPos, Vector3 forward, float distance, ref MinMaxPair minMax, float invertDistance = -1 )
	{
		RaycastHit hit;
		
		int Default = 1 << LayerMask.NameToLayer( "Default");
		int PhysicsMetal = 1 << LayerMask.NameToLayer( "PhysicsMetal");
		
		if( true == Physics.Raycast( fromPos, forward, out hit, distance, Default | PhysicsMetal ) )
		{
			DebugDraw.LineOriented( Color.red, fromPos, fromPos + forward*distance, 0.05f );
			
			distance = ( fromPos - hit.point ).magnitude;
			
			if( Vector3.Dot ( hit.normal, forward ) >  -0.1f )
			{
				if( invertDistance < 0 )
				{
					distance = 0;
				}
				else
				{
					distance = invertDistance;
				}
			}
			else
			{
				if( true == VisualizeTests )
				{
					DebugDraw.DisplayTime = 0.00f;
					DebugDraw.Diamond( Color.green, 0.03f, hit.point );
					DebugDraw.LineOriented( VisualizationColor, hit.point, hit.point + hit.normal*0.25f, 0.05f );
				}
			}
		}
		else
		{
			if( invertDistance < 0 )
			{
				Debug.LogWarning( "CoverUtils : Expected collision hit not found, position :" + fromPos );
			}
			return distance;
		}
		
		if( invertDistance > 0 )
		{
			minMax.Absorb( invertDistance - distance );
		}
		else
		{
			minMax.Absorb( distance );
		}
		
		return distance;
	}
	
	static public void LineDistanceCast( Vector3 testedLineStart, Vector3 testedLineEnd, Vector3 forward, float distance, ref MinMaxPair minMax, float invertDistance = -1 )
	{
		Vector3 delta = testedLineEnd - testedLineStart;
		
		float dist = delta.magnitude;
		
		delta.Normalize();
		
		for( float pos = 0; pos<=dist; pos+=0.05f )
		{
			ProceedOneCoverDistanceCast( testedLineStart + delta*pos, forward, distance, ref minMax, invertDistance );
		}
	}
	
	public static bool IsGameObjectSelected()
	{
		if( null != Selection.gameObjects && Selection.gameObjects.Length > 0 )
		{
			return true;
		}
		
		return false;
	}
	
	public static List<Cover> GrabSelectedCovers()
	{
		List<Cover> selectedCovers = new List<Cover>();
		
		foreach( Object obj in Selection.objects )
		{
			GameObject gObject = obj as GameObject;
			
			if( null != gObject )
			{
				Cover[] covers = gObject.GetComponents<Cover>();
				
				if( null != covers )
				{
					foreach( Cover cover in covers )
					{
						selectedCovers.Add( cover );
					}
				}
			}
		}
		
		return selectedCovers;
	}
	
	public static void SelectCovers( string title, System.Predicate<Cover> match = null )
	{
		GUIEditorUtils.RegisterSceneUndo( "Covers : " + title );
		
		List<Cover> coverList = new List<Cover>();
		
		Cover[] covers = Resources.FindObjectsOfTypeAll( typeof( Cover) ) as Cover[];
		
		if( null != covers )
		{
			coverList.AddRange( covers );
			
			coverList = coverList.FindAll( cover => !EditorUtility.IsPersistent( cover.gameObject ) );
		}
		
		if( null != match )
		{
			coverList = coverList.FindAll( match );
		}

		List<GameObject> gameObjectList = new List<GameObject>();
		
		foreach( Cover cover in coverList )
		{
			gameObjectList.Add( cover.gameObject );
		}
		
		Selection.objects = gameObjectList.ToArray();
	}
}
