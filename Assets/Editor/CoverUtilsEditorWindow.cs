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

public class CoverUtilsEditorWindow : EditorWindow
{
	static int m_ButtonSize = 110;
	static float m_TestedCollisonDistance = 2.0f;
	static float m_TestedCollisonDistanceGround = 2.0f;
	
	private MinMaxPair m_DistanceToWallCrouch;
	private MinMaxPair m_DistanceToWallStand;
	private MinMaxPair m_DistanceCrouchCorner;
	private MinMaxPair m_DistanceStandCorner;
	
	private delegate void CoverCommand( Cover cover, float parameter );
	
	[SerializeField]
	private float m_DesiredWallDistance = 0.55f;
	
	[SerializeField]
	private float m_DesiredCornerDistance = 0.35f;
	
	[SerializeField]
	private float m_DesiredGroundDistance = 0.05f;
	
	[MenuItem("MADFINGER/Cover utilities ... ")]
	static void Init()
	{
		EditorWindow.GetWindow<CoverUtilsEditorWindow>( false, "Cover utilities" );
		
		CoverUtilsStatistics statistics = EditorWindow.GetWindow<CoverUtilsStatistics>( false, "Cover statistics" );
		
		if( null != statistics )
		{
			statistics.Scan();
		}
	}
	
	void ModifyCoverCornerDistance( Cover cover, float cornerDistance )
	{
		cover.SetCornerDistanceExt( cornerDistance, m_TestedCollisonDistance );
	}
	
	void ModifyGroundDistance( Cover cover, float groundDistance )
	{
		cover.SetGroundDistanceExt( groundDistance, m_TestedCollisonDistance );
	}
	
	
	
	void ModifyCoverWallDistance( Cover cover, float wallDistance )
	{
		cover.SetWallDistanceExt( wallDistance, m_TestedCollisonDistance );
	}
	
	void OnGUI()
	{
		GUILayout.BeginVertical();
		{
			OnGUISpace();
			
			GUILayout.Label( "This is experimental tool in development process.\nUse it wisely." );
				
			OnGUISpace();
			
			OnGUILines();
			
			OnGUISpace();
			GUILayout.Label( "TODO : vertical aligment ( do we need it? )" );
			
			GUILayout.FlexibleSpace();
			
			OnGUISpace();
			
			GUILayout.BeginHorizontal();
			{
				OnGUISelectCoversButton( "Select stand" ,	cover => 
					cover.TestFlags( Cover.E_CoverFlags.LeftStand ) || 
					cover.TestFlags( Cover.E_CoverFlags.RightStand ) );
				OnGUISelectCoversButton( "Select crouch",	cover => 
					!cover.TestFlags( Cover.E_CoverFlags.LeftStand ) && 
					!cover.TestFlags( Cover.E_CoverFlags.RightStand ) &&
					( cover.TestFlags( Cover.E_CoverFlags.RightCrouch ) || cover.TestFlags( Cover.E_CoverFlags.LeftCrouch ) ) );
				OnGUISelectCoversButton( "Select all" );
			}
			GUILayout.EndHorizontal();
			
			OnGUISpace();
		}
		GUILayout.EndVertical();
	}
	
	private void OnGUISelectCoversButton( string title, System.Predicate<Cover> match = null )
	{
		if( GUILayout.Button( title, GUILayout.Width( m_ButtonSize ) ) == true )
		{
			CoverUtils.SelectCovers( title, match );
		}
	}
	
	private void OnGUISpace()
	{
		GUILayout.Space( 8 );
	}
	
	private void OnGUILines()
	{
		OnGUILine( "Wall Distance",		ModifyCoverWallDistance,	ref m_DesiredWallDistance,		m_TestedCollisonDistance );
		OnGUILine( "Corner Distance",	ModifyCoverCornerDistance,	ref m_DesiredCornerDistance,	m_TestedCollisonDistance );
		OnGUILine( "Ground Distance",	ModifyGroundDistance,		ref m_DesiredGroundDistance,	m_TestedCollisonDistanceGround );
	}
	
	private void OnGUILine( string name, CoverCommand command, ref float floatValue, float maxValue )
	{
		GUILayout.BeginHorizontal();
		{
			floatValue = EditorGUILayout.FloatField( name, floatValue );
			
			floatValue = Mathf.Clamp(floatValue, 0, maxValue);
			
			GUI.enabled = CoverUtils.IsGameObjectSelected();
			
			if( GUILayout.Button( "Modify selection", GUILayout.Width( m_ButtonSize ) ) == true )
			{
				ExecuteCommandOnSelectedCovers( command, floatValue, "CoverUtils: " + name );
			}
			
			GUI.enabled = true;
		}
		GUILayout.EndHorizontal();
	}
	
	private static void ExecuteCommandOnSelectedCovers( CoverCommand command, float param, string undoName )
	{
		List<Cover> covers = CoverUtils.GrabSelectedCovers();
		
		if( covers.Count > 0 )
		{
			GUIEditorUtils.RegisterSceneUndo( undoName );
			
			foreach( Cover cover in covers )
			{
				command( cover, param );
			}
		}
	}
}
