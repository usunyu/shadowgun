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

#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
//#define ENABLED
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerControlsDrone
{
	public static string DesiredIP = "192.168.1.138";

#if ENABLED
	
	public static bool Enabled
	{ 
		get
		{
			return Debug.isDebugBuild;
		}
	}
	
	private PlayerControlStates States;
	
	private Vector3 m_LastPosition;
	private float m_NextPositionCheck;
	
	private bool m_FireStateOn;
	private float m_NextFireChange;	
	private float m_NextGadgetUsage;
	private float m_NextRoll;
	
	private static bool m_CoroutineRunning;
	
	public PlayerControlsDrone( PlayerControlStates states )
	{
		States = states;
	}
	
	public void Update()
	{
		if( !Enabled )
		{
			return;
		}
		
		UpdateGame();
	}
	
	public static void UpdateMenu()
	{
		if( null != MFGuiManager.Instance )
		{
			if( !m_CoroutineRunning )
			{
				ProceedLayoutAndButton("SMPauseButtons_Layout", "Resume_Button");
				ProceedLayoutAndButton("SMSpawnButtons_Layout", "Spawn_Button");
				
				if( !ProceedLayoutAndButton("01Buttons_Layout", "Connect_Button", true ) )
				{
					ProceedLayoutAndButton("SMSideButtons_Layout", "Game_Button");
				}
				
				ProceedLayoutAndButton( "Offer_Layout", "Close_Button" );
				ProceedLayoutAndButton( "Promote_Layout", "Close_Button" );
			}
		}
	}
	
	private static bool ProceedLayoutAndButton( string layoutName, string buttonName, bool probeOnly = false )
	{
		GUIBase_Layout layout = MFGuiManager.Instance.GetLayout( layoutName );
		
		if( null != layout )
		{
			if( layout.IsEnabled( true ) && layout.IsVisible( false ) )
			{
				GUIBase_Widget widget = layout.GetWidget( buttonName );
				
				if( null != widget )
				{
					if( widget.IsEnabled( true ) && widget.IsVisible( false ) )
					{
						if( !probeOnly )
						{
							m_CoroutineRunning = true;
							
							if( null != Game.Instance )
							{
								Game.Instance.StartCoroutine( AutoClick( widget ) );
							}
						}
						
						return true;
					}
				}
			}
		}
		
		return false;
	}
	
	static IEnumerator  AutoClick( GUIBase_Widget widget )
	{
		widget.HandleTouchEvent( GUIBase_Widget.E_TouchPhase.E_TP_CLICK_BEGIN, null );
		yield return new WaitForSeconds(1.0f);
		
		widget.HandleTouchEvent( GUIBase_Widget.E_TouchPhase.E_TP_CLICK_RELEASE, null );
		yield return new WaitForSeconds(1.0f);
		
		m_CoroutineRunning = false;
	}
	
	private void UpdateGame()
	{
		if( null != Player.LocalInstance )
		{
			AgentHuman owner = Player.LocalInstance.Owner;
			
			bool hasEnemy = false;
			
			if( States.View.Enabled )
			{
				float yaw = ComputeYaw( Player.LocalInstance.Owner, ref hasEnemy );
				
				if( Time.time > m_NextPositionCheck )
				{
					m_NextPositionCheck = Time.time + 0.25f;
					
					if( ( m_LastPosition - owner.Transform.position ).magnitude < 0.1f )
					{
						yaw = (Random.Range( 0, 2 ) > 0) ? 90 : -90;
					}
					
					m_LastPosition = owner.Transform.position;
				}
					
				States.View.SetNewRotation( yaw, 0);
			}
			
			if( States.Move.Enabled )
			{
				if( false == hasEnemy )
				{
					States.Move.Direction.x = 0.0f;
					States.Move.Direction.z = 1.0f;
					States.Move.Direction.Normalize();
					
					States._Temp.eulerAngles = new Vector3( 0, owner.Transform.rotation.eulerAngles.y, 0);
					States.Move.Direction = States._Temp.TransformDirection( States.Move.Direction );
					States.Move.Force = 1.0f;
				}
			}
			
			if( true == hasEnemy )
			{
				Fire( true );
			}
			else
			{
				if( Time.time > m_NextFireChange )
				{
					m_FireStateOn = !m_FireStateOn;
					
					if( m_FireStateOn )
					{
						m_NextFireChange = TimeNext( 0.25f, 0.75f );
					}
					else
					{
						m_NextFireChange = TimeNext( 5.75f , 15.5f );
					}
				}

				Fire( m_FireStateOn );
				
				if( Time.time > m_NextGadgetUsage )
				{
					UseGadget();
					
					m_NextGadgetUsage = TimeNext( 0.75f, 6.5f );
				}
				else if( Time.time > m_NextRoll )
				{
					Roll();
					m_NextRoll = TimeNext( 3.0f, 8.0f );
				}
			}
			
			
			
		}
	}
	
	private static float TimeNext( float from, float to )
	{
		return Time.time + Random.Range( from, to );
	}
	
	private void Fire( bool state )
	{
		if( Player.LocalInstance.InUseMode )
		{
			if( state == true )
			{
				States.UseDelegate();
			}
		}
		else
		{
			if( state == true )
			{
				States.FireDownDelegate();
			}
			else
			{
				States.FireUpDelegate();
			}
		}
	}
	
	private void UseGadget()
	{
		List<E_ItemID> list = new List<E_ItemID>();
		
		AddGadgetToList( list, 0 );
		AddGadgetToList( list, 1 );
		AddGadgetToList( list, 2 );
		
		
		if( list.Count > 0 )
		{
			States.UseGadgetDelegate( list[ Random.Range( 0, list.Count ) ] );
		}
		
	}
		
	private void AddGadgetToList( List<E_ItemID> list, int index )
	{
		if( null != GuiHUD.Instance && null != GuiHUD.Instance.Gadgets )
		{
			E_ItemID id = GuiHUD.Instance.GetGadgetInInventoryIndex( index );
			
			if( id != E_ItemID.None && Player.LocalInstance.Owner.GadgetsComponent.GetGadget(id).Settings.ItemUse == E_ItemUse.Activate )
			{ 
				list.Add( id ); 
			}
		}
	}
	
	private void Roll()
	{
		States.RollDelegate();
	}
	
	private float ComputeYaw( AgentHuman humanFrom , ref bool hasEnemy )
	{
		GameObject objFrom = humanFrom.gameObject;
		
		Vector3 from = objFrom.transform.position + Vector3.up*1.0f;
		Vector3 direction = objFrom.transform.rotation * Vector3.forward;
		Vector3 right = objFrom.transform.rotation * Vector3.right;
		
		float hornRadius = 0.5f;
		float hornLength = 7.5f;
		
		float yawPower = 400.0f*Time.deltaTime;
		
		Vector3 hitPosition = from;
		Vector3 hitNormal = Vector3.up;
		
		float result = 0.0f;
		
		if( true == GetHornInfo( from, direction, hornRadius, hornLength, ref hitPosition, ref hitNormal ) ) // horn hit collision
		{
			hitNormal.y = 0;
			hitNormal.Normalize();

			float distance01 = ( from - hitPosition ).magnitude / hornLength;
			float power01 = 0.5f * ( 1.0f-Vector3.Dot( hitNormal, direction ) );
			
			result = yawPower*distance01*power01;
			
			float dotRight = Vector3.Dot( hitNormal, right );
			
			if( dotRight < 0 )
			{
				result *= -1;
			}
		}
		
		foreach( KeyValuePair<uLink.NetworkPlayer, ComponentPlayer> pair in Player.Players )
		{
			if( null == pair.Value )
			{
				continue;
			}
			
			AgentHuman human =  pair.Value.Owner;
			
			if( null == human || human.IsFriend( humanFrom ) )
			{
				continue;
			}
			
			Vector3 vectorTo = human.ChestPosition - humanFrom.ChestPosition;
			vectorTo.y = 0.0f;
			Vector3 dirTo = vectorTo.normalized;
			
			if( Vector3.Dot ( humanFrom.Forward, dirTo ) > 0.3f )
			{
				float dist = vectorTo.magnitude;
				
				if( dist < 10 )
				{
					RaycastHit hit;
					
					if( !Physics.Raycast( humanFrom.ChestPosition, dirTo, out hit, dist, ObjectLayerMask.Default | ObjectLayerMask.PhysicsMetal ) )
					{
						result = Vector3.Angle( humanFrom.Forward, vectorTo );
						
						if( Vector3.Cross( humanFrom.Forward, vectorTo ).y < 0 )
						{
							result *= -1;
						}
						
						hasEnemy = true;
					}
				}
			}
			
		}
		
		//hasEnemy = true;
		
		return result;
		
	}
	
	private bool GetHornInfo( Vector3 hornFrom, Vector3 hornDirection, float hornRadius, float hornLength, ref Vector3 hitPosition, ref Vector3 hitNormal )
	{
		RaycastHit[] hits = Physics.SphereCastAll( hornFrom, hornRadius, hornDirection, hornLength, ObjectLayerMask.Default | ObjectLayerMask.PhysicsMetal );
			
		if( hits.Length > 1 )
		{
			System.Array.Sort( hits, CollisionUtils.CompareHits );
		}
		
		foreach ( RaycastHit h in hits )
		{
			if( h.normal.y < 0.7f )
			{
				hitPosition = h.point;
				hitNormal = h.normal;
				
				return true;
			}
		}
		
		return false;
	}
	
#else // ENABLED

	public static bool Enabled
	{
		get { return false; }
		set { }
	}

	public void Update()
	{
	}

	public static void UpdateMenu()
	{
	}

	public PlayerControlsDrone(PlayerControlStates states)
	{
	}

#endif // ENABLED	
}
