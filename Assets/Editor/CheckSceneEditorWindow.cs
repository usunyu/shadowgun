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

public class CheckSceneEditorWindow : EditorWindow
{
	static int m_ButtonSize = 170;
	
	[SerializeField]
	static float m_Radius = 0.6f;
	
	[SerializeField]
	static float m_Height = 2.0f;
	
	[SerializeField]
	static float m_MinimalDistanceBetweenCovers = 1.0f;
	
	delegate bool CheckObject<T>( T obj, T[] allObjects ) where T : MonoBehaviour;
	
	[MenuItem("MADFINGER/Check scene ... ")]
	static void Init()
	{
		EditorWindow.GetWindow<CheckSceneEditorWindow>( false, "Check scene" );
	}
	
	bool CheckCapsule( Cover c, AgentActionCoverLeave.E_Type type )
	{
		bool result = true;
		
		Transform T = c.transform;
		
		Vector3 center		= T.position;
		Vector3 scale		= T.lossyScale;
		Vector3 right		= T.right;
		Vector3 forward		= T.forward;
		Vector3 rightPos	= center + right * scale.x * 0.5f;
		Vector3 leftPos		= center - right * scale.x * 0.5f;
		Vector3 up  		= Vector3.up * scale.y * 0.5f;
		
		Vector3 testedPosition = center;
		Vector3 fromPos = center;
		
		Color debugColor = Color.white;
		
		switch( type )
		{
		case AgentActionCoverLeave.E_Type.JumpUp:
			testedPosition = AnimStateCoverLeave.BuildSafeFinalPositionForLeavingCover( type, center, right, forward );
			debugColor = Color.cyan;
			fromPos = center;
			
			if( !CheckCapsuleAtPos( AnimStateCoverLeave.BuildSafeFinalPositionForLeavingCover( type, rightPos, right, forward ), debugColor, rightPos + up ) )
			{
				result = false;
			}
			
			if( !CheckCapsuleAtPos( AnimStateCoverLeave.BuildSafeFinalPositionForLeavingCover( type, leftPos, right, forward ), debugColor, leftPos + up ) )
			{
				result = false;
			}
			break;
		case AgentActionCoverLeave.E_Type.Left:
			testedPosition = AnimStateCoverLeave.BuildSafeFinalPositionForLeavingCover( type, leftPos, right, forward );
			debugColor = Color.red;
			fromPos = leftPos;
			break;
		case AgentActionCoverLeave.E_Type.Right:
			testedPosition = AnimStateCoverLeave.BuildSafeFinalPositionForLeavingCover( type, rightPos, right, forward );
			debugColor = Color.green;
			fromPos = rightPos;
			break;
		case AgentActionCoverLeave.E_Type.Back:
			testedPosition = AnimStateCoverLeave.BuildSafeFinalPositionForLeavingCover( type, center, right, -forward );
			debugColor = Color.blue;
			fromPos = center;
			
			if( !CheckCapsuleAtPos( AnimStateCoverLeave.BuildSafeFinalPositionForLeavingCover( type, rightPos, right, -forward ), debugColor, rightPos + up ) )
			{
				result = false;
			}
			
			if( !CheckCapsuleAtPos( AnimStateCoverLeave.BuildSafeFinalPositionForLeavingCover( type, leftPos, right, -forward ), debugColor, leftPos + up ) )
			{
				result = false;
			}
			break;
		}
		
		if( !CheckCapsuleAtPos( testedPosition, debugColor, fromPos + up ) )
		{
			result = false;
		}
		
		
		return result;
	}
	
	bool CheckOverlappedCover( Cover c, Cover[] allObjects )
	{
		bool result = true;
		
		foreach( Cover toCompare in allObjects )
		{
			if( c == toCompare )
			{
				continue;
			}
			
			float distance = ( c.transform.position - toCompare.transform.position ).magnitude;
			
			if( distance < m_MinimalDistanceBetweenCovers )
			{
				Debug.LogWarning( "Cover '" + c.name + "' is near the cover '" + toCompare.name + "' ( distance is " + distance.ToString( "0.00" ) + " m )" );
				result = false;
			}
		}
		
		return result;
	}
	
	bool CheckCover( Cover c, Cover[] allObjects )
	{
		bool result = true;
		string resString = "";
		
		if( c.TestFlags( Cover.E_CoverFlags.LeftStand ) || c.TestFlags( Cover.E_CoverFlags.LeftCrouch ) )
		{
			if( !CheckCapsule( c, AgentActionCoverLeave.E_Type.Left ) )
			{
				resString += "LEFT";
				result = false;
			}
		}
		
		if( c.TestFlags( Cover.E_CoverFlags.RightStand ) || c.TestFlags( Cover.E_CoverFlags.RightCrouch ) )
		{
			if( !CheckCapsule( c, AgentActionCoverLeave.E_Type.Right ) )
			{
				Debug.LogWarning( "Cover " + c.name + " leaving position in collision : direction RIGHT" );
				
				resString += result ? "RIGHT" : ", RIGHT";
				result = false;
			}
		}
		
		if( c.CanJumpUp )
		{
			if( !CheckCapsule( c, AgentActionCoverLeave.E_Type.JumpUp ) )
			{
				Debug.LogWarning( "Cover " + c.name + " leaving position in collision : direction JUMPUP" );
				
				resString += result ? "JUMPUP" : ", JUMPUP";
				result = false;
			}
		}
		
		if( !CheckCapsule( c, AgentActionCoverLeave.E_Type.Back ) )
		{
			Debug.LogWarning( "Cover " + c.name + " leaving position in collision : direction BACK" );
			resString += result ? "BACK" : ", BACK";
			result = false;
		}
		
		if( !result )
		{
			Debug.LogWarning(" Cover " + c.GetFullName() + " wrong dirrections : " + resString);
		}
		else
		{
			/*result = CheckLowEdgeOfCover( c );
			if( !result )
			{
				Debug.LogWarning(" Cover " + c.GetFullName() + " with bottom edge under the terrain.");
			}*/
		}
		
		return result;
	}
	
	/*private bool CheckLowEdgeOfCover( Cover c )
	{
		Vector3 from = c.transform.position;
		
		return Physics.Raycast( from , Vector3.down, 0.4f );
	}*/
	
	bool CheckSpawnpoint( SpawnPointPlayer sp, SpawnPointPlayer[] allObjects )
	{
		return CheckCapsuleAtPos( sp.transform.position, Color.white, sp.transform.position );
	}
	
	bool CheckCapsuleAtPos( Vector3 pos, Color debugColor, Vector3 fromPos )
	{
		Vector3 spawnPosReal = CollisionUtils.GetGroundedPos(pos);

		Vector3 A = spawnPosReal + Vector3.up*(m_Radius+0.05f); // take it little bit over the ground
		Vector3 B = spawnPosReal + Vector3.up*(m_Height-m_Radius);
		
		int Default = 1 << LayerMask.NameToLayer( "Default");
		int PhysicsMetal = 1 << LayerMask.NameToLayer( "PhysicsMetal");
		
		if(  Physics.CheckCapsule(A, B, m_Radius, Default | PhysicsMetal ) )
		{
			DebugDraw.DisplayTime = 1;
			//DebugDraw.Diamond( debugColor, 0.03f, A );
			//DebugDraw.LineOriented( debugColor, A, B, 0.05f );
			
			DebugDraw.Capsule(debugColor, m_Radius, A, B);
			
			//if( fromPos != null )
			{
				DebugDraw.LineOriented( Color.magenta, fromPos, A );
				DebugDraw.LineOriented( Color.magenta, fromPos, B );
				DebugDraw.LineOriented( Color.magenta, fromPos, 0.5f*(A+B) );
			}
			
			return false;
		}
		
		return true;
	}
	
	void OnGUI()
	{
		GUILayout.BeginVertical();
		{
			GUILayout.Space( 8 );
			
			GUILayout.Label( "This is experimental tool in development process.\nUse it wisely." );
				
			GUILayout.Space( 8 );
			
			m_Radius = EditorGUILayout.FloatField( "Character radius", m_Radius );
			m_Height = EditorGUILayout.FloatField( "Character height", m_Height );
			m_MinimalDistanceBetweenCovers = EditorGUILayout.FloatField( "Minimal covers distance", m_MinimalDistanceBetweenCovers );
			
			GUILayout.Space( 8 );
			
			OnGUICheck<SpawnPointPlayer>( CheckSpawnpoint,		"spawnpoint");
			OnGUICheck<Cover>			( CheckCover,			"cover");
			OnGUICheck<Cover>			( CheckOverlappedCover,	"overlapped cover");
			OnGUICheckSelection<Cover>	( CheckCover,			"cover");
			
			GUILayout.FlexibleSpace();
		}
		GUILayout.EndVertical();
	}
	
	void OnGUICheck<T>( CheckObject<T> checkDelegate, string name ) where T : MonoBehaviour
	{
		if( GUILayout.Button( "Check " + name + "s", GUILayout.Width( m_ButtonSize ) ) == true )
		{
			T[] allObjects = Resources.FindObjectsOfTypeAll( typeof( T ) ) as T[];
			
			CheckObjects<T>( name, checkDelegate, allObjects );
		}
	}
	
	void OnGUICheckSelection<T>( CheckObject<T> checkDelegate, string name ) where T : MonoBehaviour
	{
		if( GUILayout.Button( "Check " + name + "s in selection", GUILayout.Width( m_ButtonSize ) ) == true )
		{
			List<T> allObjects = new List<T>();
			
			foreach( Object obj in Selection.gameObjects )
			{
				if( obj is GameObject )
				{
					GameObject gameObj = obj as GameObject;
					
					T component = gameObj.GetComponent<T>();
					if( component != null )
					{
						allObjects.Add( component );
					}
				}
			}
			
			CheckObjects<T>( name, checkDelegate, allObjects.ToArray() );
		}
	}
	
	void CheckObjects<T>( string name, CheckObject<T> checkDelegate, T[] allObjects ) where T : MonoBehaviour
	{
		List<GameObject> problematicObjects = new List<GameObject>();
		
		//T[] allObjects = Resources.FindObjectsOfTypeAll( typeof( T ) ) as T[];
		
		if( null != allObjects )
		{
			foreach( T obj in allObjects )
			{
				if( EditorUtility.IsPersistent( obj.gameObject ) )
				{
					continue;
				}
				
				if( !checkDelegate( obj, allObjects ) )
				{
					problematicObjects.Add( obj.gameObject );
				}
			}
		}
		
		if( problematicObjects.Count > 0 )
		{
			string output = problematicObjects.Count + " problematic " + name +"s found ! See selection for more details. ( " + name + "s ";
			
			bool first = true;
			
			foreach( GameObject go in problematicObjects )
			{
				if( !first )
				{
					output += ", ";
				}
				else
				{
					first = false;
				}
				output += go.name;
			}
			output += ")";
			
			Debug.LogError( output );
			
			GUIEditorUtils.RegisterSceneUndo( "Checking " + name + "s : selection of problematic " + name + "s" );
		}
		else
		{
			Debug.Log( "No  problematic " + name + "s found." );
		}
		
		Selection.objects = problematicObjects.ToArray();
	}
}
