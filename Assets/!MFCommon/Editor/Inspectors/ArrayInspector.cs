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

//#####################################################################################################################

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//#####################################################################################################################
//
//  Example of usage:
//
//		MyType []                  myArray;
//		bool                       myArrayExpanded;
//		ArrayInspector< MyType >   myArrayInspector;
//		
//		...
//		
//		void OnGUI()
//		{
//			...
//			
//			myArrayInspector.m_CreateItem      = CreateMyType;      // or use: MiscUtils.Create< MyType >;
//			myArrayInspector.m_DuplicateItem   = CloneMyType;       // or use: MiscUtils.DeepCopy< MyType >;
//			myArrayInspector.m_DisplayItem     = DisplayMyType;
//			myArrayInspector.m_ShowPlusButton  = ShowPlusButton;    // optional
//			myArrayInspector.m_ShowMinusButton = ShowMinusButton;   // optional
//			
//			myArrayInspector.Display( "This is my array...", ref myArrayExpanded, ref myArray );
//			
//			...
//		}
//		
//		MyType CreateMyType()
//		{
//			MyType a = new MyType();
//			...
//			return a;
//		}
//		
//		MyType CloneMyType( MyType a )
//		{
//			return new MyType( a );
//		}
//		
//		void DisplayMyType( int arrayItemIndex, ref MyType arrayItem )
//		{
//			GUILayout.BeginVertical();
//			...
//			GUILayout.EndVertical();
//		}
//		
//		bool ShowPlusButton( int arrayItemIndex )
//		{
//			return myArray.Length < 4; // limit size of the array
//		}
//		
//		bool ShowMinusButton( int arrayItemIndex )
//		{
//			return false; // don't remove existing items
//		}
//		
//#####################################################################################################################

public abstract class ContainerInspector
{
	#region constants /////////////////////////////////////////////////////////////////////////////////////////////////
	
	// size of plus/minus button
	protected const   int   ButtonSize    = 16;
	// empty space around plus/minus buttons
	protected const   int   ButtonPadding = 3;
	
	#endregion
	#region fields ////////////////////////////////////////////////////////////////////////////////////////////////////
	
	// gui already initialized ?
	private static     bool       m_GuiInitialized = false;
	// gui-style used for plus / minus button
	protected static   GUIStyle   m_GuiStyle_Button;
	// gui-style used for size (number of items)
	protected static   GUIStyle   m_GuiStyle_Size;
	
	#endregion
	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////
	
	//-----------------------------------------------------------------------------------------------------------------
	protected void Init()
	{
		// create gui styles...
		
		if ( !m_GuiInitialized )
		{
			m_GuiInitialized = true;
			
			m_GuiStyle_Button               = new GUIStyle( "" );
			m_GuiStyle_Button.name          = "Centered Icon Button";
			m_GuiStyle_Button.imagePosition = ImagePosition.ImageOnly;
			m_GuiStyle_Button.alignment     = TextAnchor.MiddleCenter;
			m_GuiStyle_Button.fixedWidth    = ButtonSize;
			m_GuiStyle_Button.fixedHeight   = ButtonSize;
			m_GuiStyle_Button.stretchWidth  = false;
			m_GuiStyle_Button.stretchHeight = false;
			m_GuiStyle_Button.margin        = new RectOffset( 0, 0, 1, 0 );
			
			m_GuiStyle_Size               = new GUIStyle( "Label" );
			m_GuiStyle_Size.imagePosition = ImagePosition.TextOnly;
			m_GuiStyle_Size.alignment     = TextAnchor.MiddleRight;
			m_GuiStyle_Size.fixedWidth    = 40;
			m_GuiStyle_Size.fixedHeight   = ButtonSize;
			m_GuiStyle_Size.font          = GUI.skin.font;
			m_GuiStyle_Size.fontSize      = 10;
			m_GuiStyle_Size.fontStyle     = FontStyle.Normal;
			m_GuiStyle_Size.normal.textColor = new Color( 0.4f, 0.4f, 0.4f );
			m_GuiStyle_Size.stretchWidth  = false;
			m_GuiStyle_Size.stretchHeight = false;
			m_GuiStyle_Size.padding       = new RectOffset( 0, 0, -2, 0 );
		}
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	protected static bool ShowButtonAlways( int Index )
	{
		return true;
	}
	
	#endregion
}

//#####################################################################################################################

public class ArrayInspector< T > : ContainerInspector
{
	#region delegates /////////////////////////////////////////////////////////////////////////////////////////////////
	
	// create new item for insertion into managed list
	public delegate   T      CreateItem();
	// optional: duplicate existing item
	public delegate   T      DuplicateItem( T Item );
	// display item
	public delegate   void   DisplayItem( int Index, ref T Item );
	// optional: show plus button for specified item ?
	public delegate   bool   ShowPlusButton( int Index );
	// optional: show minus button for specified item ?
	public delegate   bool   ShowMinusButton( int Index );
	
	#endregion
	#region fields ////////////////////////////////////////////////////////////////////////////////////////////////////
	
	// creates/duplicates item
	public    CreateItem        m_CreateItem;    // = MiscUtils.Create< MyType >;
	public    DuplicateItem     m_DuplicateItem; // = MiscUtils.DeepCopy< MyType >;
	// displays items
	public    DisplayItem       m_DisplayItem;
	// plus/minus button delegate
	public    ShowPlusButton    m_ShowPlusButton    = ContainerInspector.ShowButtonAlways;
	public    ShowMinusButton   m_ShowMinusButton   = ContainerInspector.ShowButtonAlways;
	// size of array
	private   int               m_Size;
	// max buttons (+/-) simultenously displayed for one item
	private   int               m_ButtonsNum;
	
	#endregion
	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////
	
	//-----------------------------------------------------------------------------------------------------------------
	private bool Init( T [] Array )
	{
		if ( m_DisplayItem == null )
		{
			return false;
		}
		
		if (( m_CreateItem == null ) && ( m_ShowPlusButton != null ))
		{
			return false;
		}
		
		base.Init();
		
		m_Size       = Array != null ? Array.Length : 0;
		m_ButtonsNum = 0;
		
		if ( m_ShowPlusButton  != null ) ++m_ButtonsNum;
		if ( m_ShowMinusButton != null ) ++m_ButtonsNum;
		
		return true;
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	public void Display( string Label, ref T [] Array )
	{
		if ( !Init( Array ) )
		{
			return;
		}
		
		DisplayHeader( Label, ref Array );
		
		EditorGUI.indentLevel++;
		
		DisplayItems( ref Array );
		
		EditorGUI.indentLevel--;
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	public void Display( string Label, ref bool Expanded, ref T [] Array )
	{
		if ( !Init( Array ) )
		{
			return;
		}
		
		DisplayHeader( Label, ref Expanded, ref Array );
		
		if ( Expanded )
		{
			EditorGUI.indentLevel += 2;
			
			DisplayItems( ref Array );
			
			EditorGUI.indentLevel -= 2;
		}
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private void DisplayHeader( string Label, ref T [] Array )
	{
		GUILayout.BeginHorizontal();
		
		EditorGUILayout.LabelField( Label );
		
		int missing = m_ButtonsNum;
		
		GUILayout.Label( "[" + m_Size.ToString() + "]", m_GuiStyle_Size );
		
		if (( m_ShowPlusButton != null ) && ( m_ShowPlusButton(-1) == true ))
		{
			DisplayPlusButton( -1, ref Array );   --missing;
		}
		
		GUILayout.Space( ButtonPadding + missing * ButtonSize );
	//	GUILayout.Space( missing * ( ButtonPadding + ButtonSize ) );
		
		GUILayout.EndHorizontal();
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private void DisplayHeader( string Label, ref bool Expanded, ref T [] Array )
	{
		GUILayout.BeginHorizontal();
		
		Expanded = EditorGUILayout.Foldout( Expanded, Label );
		
		int missing = m_ButtonsNum;
		
		GUILayout.Label( "[" + m_Size.ToString() + "]", m_GuiStyle_Size );
		
		if ( Expanded )
		{
			if (( m_ShowPlusButton != null ) && ( m_ShowPlusButton(-1) == true ))
			{
				DisplayPlusButton( -1, ref Array );   --missing;
			}
		}
		
		GUILayout.Space( ButtonPadding + missing * ButtonSize );
	//	GUILayout.Space( missing * ( ButtonPadding + ButtonSize ) );
		
		GUILayout.EndHorizontal();
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private void DisplayItems( ref T [] Array )
	{
		int missing;
		
		for ( int i = 0; i < m_Size; ++i )
		{
			GUILayout.BeginHorizontal();
			
			m_DisplayItem( i, ref Array[i] );
			
			GUILayout.Space( ButtonPadding );
			
			missing = m_ButtonsNum;
			
			if (( m_ShowPlusButton != null ) && ( m_ShowPlusButton(i) == true ))
			{
				--missing;
				
				if ( DisplayPlusButton( i, ref Array ) )
				return;
			}
			
			if (( m_ShowMinusButton != null ) && ( m_ShowMinusButton(i) == true ))
			{
				--missing;
				
				if ( DisplayMinusButton( i, ref Array ) )
				return;
			}
			
			GUILayout.Space( ButtonPadding + missing * ButtonSize );
			
			GUILayout.EndHorizontal();
		}
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private bool DisplayPlusButton( int Index, ref T [] Array )
	{
		if ( GUILayout.Button( GuiResources.BoxedPlusTexture, m_GuiStyle_Button ) == false )
		{
			return false; // not pressed
		}
		
		T     item;
		bool  adding = ( m_DuplicateItem == null ) || ( Index == -1 );
		
		// insert a new one
		if (( adding == true ) || ( Event.current.control != true ))
		{
			item = m_CreateItem();
		}
		else
		// insert a copy
		{
			item = m_DuplicateItem( Array[Index] );
		}
		
		GUI.changed = true;
		
		Insert( ref Array, Index+1, item );
		
		return true;
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private bool DisplayMinusButton( int Index, ref T [] Array )
	{
		if ( GUILayout.Button( GuiResources.BoxedMinusTexture, m_GuiStyle_Button ) == false )
		{
			return false; // not pressed
		}
		
		GUI.changed = true;
		
		Remove( ref Array, Index );
		
		return true;
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private void Insert( ref T [] Array, int Index, T Item )
	{
		int   i        = 0;
		int   oldSize  = ( Array == null ) ? 0 : Array.Length;
		T []  newArray = new T [ oldSize + 1 ];
		
		while ( i < Index )
		{
			newArray[i] = Array[i++];
		}
		
		newArray[Index] = Item;
		
		while ( i < oldSize )
		{
			newArray[i+1] = Array[i++];
		}
		
		Array = newArray;
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private void Remove( ref T [] Array, int Index )
	{
		int   i        = 0;
		int   oldSize  = Array.Length;
		T []  newArray = new T [ oldSize - 1 ];
		
		while ( i < Index )
		{
			newArray[i] = Array[i++];
		}
		
		while ( ++i < oldSize )
		{
			newArray[i-1] = Array[i];
		}
		
		Array = newArray;
	}
	
	#endregion
}

//#####################################################################################################################
