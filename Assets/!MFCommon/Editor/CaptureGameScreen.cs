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
using DateTime = System.DateTime;
using System.IO;

public class CaptureGameScreen : EditorWindow
{
	enum ImageType
	{
		PNG,
		JPG,
		SUPERSIZED_PNG
	};
	
	[SerializeField]
	private static ImageType m_ImageType = ImageType.SUPERSIZED_PNG;
	
	[SerializeField]
	private static int m_JPEGQuality = 75;
	
	[SerializeField]
	private static int m_Supersize = 2;
	
	static CaptureGameScreen()
	{
		LoadFromEditorPrefs();
	}
	
	[MenuItem("Custom/Capture Screenshot &#p")]
	static void CaptureScreenshot()
	{
		if( !Application.isPlaying && m_ImageType != ImageType.SUPERSIZED_PNG )
		{
			Debug.LogError( "Screenshot is possible only in game mode. \nHowever, you can use 'SUPERSIZED_PNG' mode ( see Capture screenshot setup )" );
			
			return;
		}
			
		Directory.CreateDirectory( "Screenshots" );
		
		string fileName = "Screenshots/Screenshot_" + DateTime.Now.ToString( "dd.MM.yyyy_hh.mm.ss" );
		
		Debug.Log( "Preparing screenshot '" + fileName + "' as " + m_ImageType.ToString() );
		
		if( m_ImageType == ImageType.JPG )
		{
			Debug.Log( "JPG quality : " + m_JPEGQuality );
		}
		
		switch( m_ImageType )
		{
		case ImageType.SUPERSIZED_PNG:
			Application.CaptureScreenshot( fileName + ".png", m_Supersize );
			Debug.Log( "Supersize : " +m_Supersize );
			break;
		default:
			byte [] bytes = EncodeTexture( GrabScreenTexture(), m_ImageType );
			
			if( null != bytes )
			{
				WriteBytes( fileName, m_ImageType.ToString(), bytes );
			}
			break;
		}
	}
	
	[MenuItem("Custom/Capture screenshot setup ... ")]
	static void Init()
	{
		EditorWindow.GetWindow<CaptureGameScreen>( false, "Capture setup" );
	}
	
	void OnGUI()
	{
		GUIEditorUtils.LookLikeControls();
		
		EditorGUI.BeginChangeCheck();
		
		m_ImageType = (ImageType)EditorGUILayout.EnumPopup( "Choose file format : ", m_ImageType );
		
		
		switch( m_ImageType )
		{
		case ImageType.JPG:
			m_JPEGQuality = EditorGUILayout.IntField( "JPEG quality (25-100) %", m_JPEGQuality );
			m_JPEGQuality = Mathf.Clamp( m_JPEGQuality, 25, 100 );
			break;
		case ImageType.SUPERSIZED_PNG:
			m_Supersize = EditorGUILayout.IntField( "Supersize (1-100) ", m_Supersize );
			m_Supersize = Mathf.Clamp( m_Supersize, 1, 100 );
			break;
		}
		
		if( EditorGUI.EndChangeCheck() )
		{
			SaveInEditorPrefs();
		}
		
		//GUIEditorUtils.LookLikeInspector();
		
		GUILayout.FlexibleSpace();
		
		GUILayout.Label( "Note : for taking screenshot out of game mode\nplease use 'SUPERSIZED_PNG' mode." );
		
		GUILayout.Space( 10 );
		
	}
	
	static Texture2D GrabScreenTexture()
	{
		Texture2D texture = new Texture2D( Screen.width, Screen.height, TextureFormat.ARGB32, false );
		{
			texture.ReadPixels( new Rect( 0, 0, Screen.width, Screen.height ), 0, 0, false );
			texture.Apply();
		}		
		return texture;
	}
	
	static byte [] EncodeTexture( Texture2D texture, ImageType type )
	{
		byte [] encodedBytes = null;
		
		switch( type )
		{
			
		case ImageType.PNG:
			encodedBytes = texture.EncodeToPNG();
			break;
			
		case ImageType.JPG:
			JPGEncoder encoder = new JPGEncoder( texture, m_JPEGQuality );
			encoder.doEncoding();
			
			if( encoder.isDone )
			{
				encodedBytes = encoder.GetBytes();
			}
			break;
		}
		
		return encodedBytes;
	}
	
	
	static void WriteBytes( string fileName, string ext, byte [] bytes )
	{
		using( Stream fs = File.OpenWrite( fileName + "." + ext ) )
		{
			using( BinaryWriter writer = new BinaryWriter( fs ) )
			{
				writer.Write( bytes ); 
			}
		}
	}
	
	static void SaveInEditorPrefs()
	{
		EditorPrefs.SetInt( "CapScreenType", (int)m_ImageType );
		EditorPrefs.SetInt( "CapScreenJPGQuality", m_JPEGQuality );
	}
	
	static void LoadFromEditorPrefs()
	{
		if( EditorPrefs.HasKey( "CapScreenType" ) )
		{
			m_ImageType = (ImageType)EditorPrefs.GetInt( "CapScreenType" );
		}
		
		if( EditorPrefs.HasKey( "CapScreenJPGQuality" ) )
		{
			m_JPEGQuality = EditorPrefs.GetInt( "CapScreenJPGQuality" );
		}
	}
}