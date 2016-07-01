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

//#####################################################################################################################

static class GuiResources
{
	#region properties ////////////////////////////////////////////////////////////////////////////////////////////////
	
	static public   Texture   PlusTexture         { get; private set; }
	static public   Texture   MinusTexture        { get; private set; }
	
	static public   Texture   BoxedPlusTexture    { get; private set; }
	static public   Texture   BoxedMinusTexture   { get; private set; }
	
	static public   Texture   UpTexture           { get; private set; }
	static public   Texture   DownTexture         { get; private set; }
	
	static public   Texture   ExpandTexture       { get; private set; }
	static public   Texture   CollapseTexture     { get; private set; }
	
	#endregion
	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////
	
	//-----------------------------------------------------------------------------------------------------------------
	static GuiResources()
	{
		PlusTexture       = LoadAsset< Texture >( "Assets/!MFCommon/Editor Resources/GuiTexture_Plus.png"  );
		MinusTexture      = LoadAsset< Texture >( "Assets/!MFCommon/Editor Resources/GuiTexture_Minus.png" );
		
		BoxedPlusTexture  = LoadAsset< Texture >( "Assets/!MFCommon/Editor Resources/GuiTexture_PlusBoxed.png"  );
		BoxedMinusTexture = LoadAsset< Texture >( "Assets/!MFCommon/Editor Resources/GuiTexture_MinusBoxed.png" );
		
		UpTexture         = LoadAsset< Texture >( "Assets/!MFCommon/Editor Resources/GuiTexture_Up.png"   );
		DownTexture       = LoadAsset< Texture >( "Assets/!MFCommon/Editor Resources/GuiTexture_Down.png" );
		
		ExpandTexture     = LoadAsset< Texture >( "Assets/!MFCommon/Editor Resources/GuiTexture_Expand.png"   );
		CollapseTexture   = LoadAsset< Texture >( "Assets/!MFCommon/Editor Resources/GuiTexture_Collapse.png" );
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	static public T LoadAsset< T >( string Name ) where T : Object
	{
		T resource = AssetDatabase.LoadAssetAtPath( Name, typeof(T) ) as T;
		
		if ( resource == null )
		{
			Debug.LogWarning( "GuiResources::LoadAsset() ... Asset '" + Name + "' not loaded!" );
		}
		
		return resource;
	}
	
	#endregion
}

//#####################################################################################################################
