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

public class CoverUtilsStatistics : EditorWindow
{
	static float m_TestedCollisonDistance = 2.0f;
	
	private int m_Covers = -1;
	
	private int m_FlagCrouchLeft;
	private int m_FlagCrouchRight;
	private int m_FlagCrouchUp;
	private int m_FlagStandLeft;
	private int m_FlagStandRight;
	private int m_FlagJumpUp;
	
	private MinMaxPair m_DistanceToWallCrouch;
	private MinMaxPair m_DistanceToWallStand;
	private MinMaxPair m_DistanceCrouchCorner;
	private MinMaxPair m_DistanceStandCorner;
	
	[SerializeField]
	private bool m_VisualizeTests;
	
	private void Reset()
	{
		m_Covers = -1;
		
		m_FlagCrouchLeft	=	m_FlagCrouchRight	= m_FlagCrouchUp	= 0;
		m_FlagStandLeft		=	m_FlagStandRight	= m_FlagJumpUp		= 0;
		
		m_DistanceToWallStand.Reset();
		m_DistanceToWallCrouch.Reset();
		m_DistanceCrouchCorner.Reset();
		m_DistanceStandCorner.Reset();
	}
	
	public void Scan()
	{
		Reset();
		
		ScanArray( Resources.FindObjectsOfTypeAll( typeof( Cover) ) as Cover[] );
	}
	
	private void ScanSelection()
	{
		Reset();
		
		ScanArray( CoverUtils.GrabSelectedCovers().ToArray() );
	}
	
	private void ScanArray( Cover [] coverComponents )
	{
		if( null == coverComponents )
		{
			return;
		}
		
		CoverUtils.VisualizeTests = m_VisualizeTests;
		
		m_Covers = 0;
		
		foreach( Cover cover in coverComponents )
		{
			if( EditorUtility.IsPersistent( cover.gameObject ) )
			{
				continue;
			}
			
			m_Covers++;
			
			ProceedOneCover( cover );
		}
		
		CoverUtils.VisualizeTests = false;
	}
	
	private void ProceedOneCover( Cover cover )
	{
		ProceedOneCoverFlags( cover );
		ProceedOneCoverDistances( cover );
	}
	
	private void ProceedOneCoverFlags( Cover cover )
	{
		int standLeft = cover.TestFlagsI( Cover.E_CoverFlags.LeftStand );
		int standRight = cover.TestFlagsI( Cover.E_CoverFlags.RightStand );
		
		m_FlagCrouchUp		+= cover.TestFlagsI( Cover.E_CoverFlags.UpCrouch );
		
		m_FlagStandLeft		+= standLeft;
		m_FlagStandRight	+= standRight;
		
		if( standLeft <= 0 && standRight <= 0 )
		{
			m_FlagCrouchLeft	+= cover.TestFlagsI( Cover.E_CoverFlags.LeftCrouch );
			m_FlagCrouchRight	+= cover.TestFlagsI( Cover.E_CoverFlags.RightCrouch );
		}
		
		m_FlagJumpUp		+= cover.CanJumpUp ? 1 : 0;
	}
	
	private void ProceedOneCoverDistances( Cover cover )
	{
		bool canStandRight = cover.TestFlags( Cover.E_CoverFlags.RightStand );
		bool canStandLeft = cover.TestFlags( Cover.E_CoverFlags.LeftStand );
		
		CoverUtils.VisualizationColor = Color.blue;
		
		if( true == canStandRight || true == canStandLeft )
		{
			m_DistanceToWallStand.Absorb( cover.GetWallDistanceExt( m_TestedCollisonDistance ) );
		}
		else if( cover.TestFlags( Cover.E_CoverFlags.LeftCrouch ) || cover.TestFlags( Cover.E_CoverFlags.RightCrouch ))
		{
			m_DistanceToWallCrouch.Absorb( cover.GetWallDistanceExt( m_TestedCollisonDistance ) );
		}
		
		CoverUtils.VisualizationColor = Color.yellow;
		
		if( true == canStandRight )
		{
			m_DistanceStandCorner.Absorb( cover.GetCornerDistanceExt( true, m_TestedCollisonDistance ) );
		}
		else if( cover.TestFlags( Cover.E_CoverFlags.RightCrouch ) )
		{
			m_DistanceCrouchCorner.Absorb( cover.GetCornerDistanceExt( true, m_TestedCollisonDistance ) );
		}
		else
		{
			// right side of cover without special flag
		}
		
		if( true == canStandLeft )
		{
			m_DistanceStandCorner.Absorb( cover.GetCornerDistanceExt( false, m_TestedCollisonDistance ) );
		}
		else if( cover.TestFlags( Cover.E_CoverFlags.LeftCrouch ) )
		{
			m_DistanceStandCorner.Absorb( cover.GetCornerDistanceExt( false, m_TestedCollisonDistance ) );
		}
		else
		{
			// left side of cover without special flag
		}
	}
	
	void OnGUI()
	{
		GUILayout.BeginVertical();
		{
			GUILayout.Space( 8 );
			
			int buttonSize = 110;
			
			GUILayout.BeginHorizontal( "" );
			{
				if( GUILayout.Button( "Scan scene", GUILayout.Width( buttonSize ) ) == true )
				{
					Scan();
				}
				
				GUI.enabled = CoverUtils.IsGameObjectSelected();
				if( GUILayout.Button( "Scan selection", GUILayout.Width( buttonSize ) ) == true )
				{
					ScanSelection();
				}
				GUI.enabled = true;
			}
			GUILayout.EndHorizontal();
			
			
			m_VisualizeTests = GUILayout.Toggle( m_VisualizeTests, "Visualize tests" );
			
			GUILayout.Space( 8 );
			
			if( m_Covers >= 0 )
			{
				GUILayout.BeginVertical( "" );
				{
					GUILayout.Label( "Statistics" );
					OnGUIBreak( true );
					OnGUILine( "Covers count",	m_Covers );
					
					if( m_Covers > 0 )
					{
						OnGUILine( "Left crouch",	m_FlagCrouchLeft );
						OnGUILine( "Right crouch",	m_FlagCrouchRight );
						OnGUILine( "Up crouch",		m_FlagCrouchUp );
						OnGUILine( "Left stand",	m_FlagStandLeft );
						OnGUILine( "Right stand",	m_FlagStandRight );
						OnGUILine( "Jump up",		m_FlagJumpUp );
						
						if( m_DistanceToWallCrouch.IsValid() )
						{
							OnGUIBreak( false );
							OnGUILine( "Min dist to wall (crouch)", m_DistanceToWallCrouch.m_MinValue );
							OnGUILine( "Max dist to wall (crouch)", m_DistanceToWallCrouch.m_MaxValue );
						}
						if( m_DistanceToWallStand.IsValid() )
						{
							OnGUIBreak( false );
							OnGUILine( "Min dist to wall (stand)", m_DistanceToWallStand.m_MinValue );
							OnGUILine( "Max dist to wall (stand)", m_DistanceToWallStand.m_MaxValue );
						}
						if( m_DistanceCrouchCorner.IsValid() )
						{
							OnGUIBreak( false );
							OnGUILine( "Min dist to corner (crouch)", m_DistanceCrouchCorner.m_MinValue );
							OnGUILine( "Max dist to corner (crouch)", m_DistanceCrouchCorner.m_MaxValue );
						}					
						if( m_DistanceStandCorner.IsValid() )
						{
							OnGUIBreak( false );
							OnGUILine( "Min dist to corner (stand)", m_DistanceStandCorner.m_MinValue );
							OnGUILine( "Max dist to corner (stand)", m_DistanceStandCorner.m_MaxValue );
						}
					}
					
					OnGUIBreak( true );
				}
				GUILayout.EndVertical();
			}
		}
		GUILayout.EndVertical();
		
	}
	
	private void OnGUILine<T>( string title, T val )
	{
		GUILayout.BeginHorizontal();
		{
			GUILayout.Label( title, GUILayout.Width( 160 ) );
			GUILayout.Label( ":" );
			GUILayout.Label( val.ToString() );
			GUILayout.FlexibleSpace();
		}
		GUILayout.EndHorizontal();
	}
	
	private void OnGUIBreak( bool stronger )
	{
		if( true == stronger )
		{
			GUILayout.Label( "==================================" );
		}
		else
		{
			GUILayout.Label( "-------------------------------------------------------------" );
		}
	}
}
