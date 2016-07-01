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

public static class CoverExtensions
{
	static public MinMaxPair GetWallDistanceExt( this Cover cover, float testedWallDistance )
	{
		float testedDistance = testedWallDistance + 0.05f;
		Vector3 fromPos, forward, rightPos, leftPos, right;
		
		cover.GetMetricsExt( out fromPos, out right, out forward, out rightPos, out leftPos );
		
		MinMaxPair tmp = new MinMaxPair();
		
		tmp.Reset();
		
		CoverUtils.LineDistanceCast( rightPos, leftPos, forward, testedDistance, ref tmp );
		
		return tmp;
	}
	
	static public void SetWallDistanceExt( this Cover cover, float desiredDistance, float testedCollisionDistance )
	{
		float testedDistance = testedCollisionDistance + 0.05f;
		float currentDistance = cover.GetWallDistanceExt( testedDistance ).m_MinValue;
		
		if( currentDistance >= 0 && currentDistance <= testedDistance )
		{
			float delta = desiredDistance - currentDistance;
			cover.transform.position -= cover.transform.forward*delta;
		}
	}
	
	static public float GetCornerDistanceExt( this Cover cover, bool rightSide, float testedCollisionDistance )
	{
		float testedCornerDistanceLeftRight = 1.2f;
		float testedCornerDistanceForward = 1.0f; // TODO : we can compute this calue using distance to the wall
		
		Vector3 fromPos, forward, rightPos, leftPos, right;
		
		cover.GetMetricsExt( out fromPos, out right, out forward, out rightPos, out leftPos );
		
		// new approach
		Vector3 fromPosRFrom = rightPos + right * testedCornerDistanceLeftRight;
		Vector3 fromPosRTo = fromPosRFrom + forward * testedCornerDistanceForward;
		
		Vector3 fromPosLFrom = leftPos - right * testedCornerDistanceLeftRight;
		Vector3 fromPosLTo = fromPosLFrom + forward * testedCornerDistanceForward;
		
		MinMaxPair tmp = new MinMaxPair();
		
		tmp.Reset();
		
		if( rightSide )
		{
			CoverUtils.LineDistanceCast( fromPosRFrom, fromPosRTo, -right, testedCollisionDistance, ref tmp, testedCornerDistanceLeftRight );
		}
		else // left
		{
			CoverUtils.LineDistanceCast( fromPosLFrom, fromPosLTo, right, testedCollisionDistance, ref tmp, testedCornerDistanceLeftRight );
		}
		
		return tmp.m_MaxValue;
	}
	
	static public void SetGroundDistanceExt( this Cover cover, float desiredDistance, float testedCollisionDistance )
	{
		MinMaxPair tmp = new MinMaxPair();
		
		tmp.Reset();
		
		Vector3 from = cover.transform.position + Vector3.up*testedCollisionDistance*0.5f;
		Vector3 right = cover.transform.right;

		CoverUtils.LineDistanceCast(  from + right, from - right, Vector3.down, testedCollisionDistance, ref tmp );
		
		if( tmp.IsValid() )
		{
			float dist = ( desiredDistance - ( tmp.m_MinValue - testedCollisionDistance * 0.5f ) );
			
			cover.transform.position += dist*Vector3.up;
		}
	}
	
	static public void SetCornerDistanceExt( this Cover cover, float desiredDistance, float testedCollisionDistance )
	{
		if( cover.TestFlags( Cover.E_CoverFlags.RightStand ) || cover.TestFlags( Cover.E_CoverFlags.RightCrouch ) )
		{
			float rightDistance = cover.GetCornerDistanceExt( true, testedCollisionDistance );
			
			if( rightDistance >= 0 )
			{
				// align right corner
				cover.AlignCornersExt( 0, rightDistance - desiredDistance );
			}
		} 
		// without else
		
		if( cover.TestFlags( Cover.E_CoverFlags.LeftStand) || cover.TestFlags( Cover.E_CoverFlags.LeftCrouch ) )
		{
			float leftDistance = cover.GetCornerDistanceExt( false, testedCollisionDistance );
			
			if( leftDistance >= 0 )
			{
				// align left corner
				cover.AlignCornersExt( leftDistance - desiredDistance , 0);
			}
		} 
	}
	
	public static int TestFlagsI( this Cover cover, Cover.E_CoverFlags testedFlags )
	{
		if( cover.TestFlags( testedFlags ) )
		{
			return 1;
		}
		
		return 0;
	}
	
	public static bool TestFlags( this Cover cover, Cover.E_CoverFlags testedFlags )
	{
		return cover.CoverFlags.Get( testedFlags );
	}
	
	public static void AlignCornersExt( this Cover cover, float deltaLeft, float deltaRight )
	{
		Transform T = cover.transform;
		
		//deltaRight = deltaLeft = 0;
		
		Vector3 scale = T.lossyScale;
		float desiredSize = scale.x + deltaRight + deltaLeft;
		Vector3 right = T.right;
		
		Vector3 desiredR = T.position + right * ( scale.x * 0.5f + deltaRight );
		Vector3 desiredL = T.position - right * ( scale.x * 0.5f + deltaLeft );
		
		scale.x = desiredSize;
		T.localScale = scale;
		
		T.position = 0.5f * ( desiredR + desiredL );

	}
	
	public static void GetMetricsExt( this Cover cover, out Vector3 fromPos, out Vector3 right, out Vector3 forward, out Vector3 rightPos, out Vector3 leftPos  )
	{
		Transform T = cover.transform;
		
		Vector3 scale	= T.lossyScale;
		fromPos			= T.position + Vector3.up*scale.y*0.5f;
		forward			= T.forward;
		right			= T.right;
		rightPos		= fromPos + right * scale.x * 0.5f;
		leftPos			= fromPos - right * scale.x * 0.5f;
	}
}
