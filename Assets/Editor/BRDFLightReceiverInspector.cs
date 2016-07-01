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

//namespace UnityEditor
//{
	[CustomEditor(typeof(BRDFLightReceiver))]
	internal class BRDFLightReceiverInspector : Editor
	{
		private bool changed = false;
		private bool fastPreview = true;
		private bool previewRGB = true;
		
		private static string directory = "Assets/GeneratedTextures";
		
		private static int kTexturePreviewBorder = 8;
		private static string[] kTextureSizes = { "16", "32", "64", "128", "256" } ;
		private static int[] kTextureSizesValues = { 16, 32, 64, 128, 256 } ;

		private Texture2D PersistLookupTexture (string name, Texture2D tex)
		{
			if (!System.IO.Directory.Exists(directory))
				System.IO.Directory.CreateDirectory(directory);	

			string assetPath = System.IO.Path.Combine(directory, name + ".png");
			AssetDatabase.DeleteAsset(assetPath);
			
			System.IO.File.WriteAllBytes (assetPath, tex.EncodeToPNG());
			
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			TextureImporter texSettings = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			texSettings.textureFormat = TextureImporterFormat.AutomaticTruecolor;
			
			AssetDatabase.Refresh();

			Texture2D newTex = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D)) as Texture2D;
			newTex.wrapMode = TextureWrapMode.Clamp;
			return newTex;
		}
		
		private void Bake ()
		{
			BRDFLightReceiver l = target as BRDFLightReceiver;
			if (!l) return;

			l.Bake ();
			l.lookupTexture = PersistLookupTexture (l.gameObject.name, l.lookupTexture);
			l.Update ();
			
			changed = false;
		}
		
		public void OnEnable ()
		{
			BRDFLightReceiver l = target as BRDFLightReceiver;
			if (!l) return;
			
			string path = AssetDatabase.GetAssetPath(l.lookupTexture);
			if (path == "")
				changed = true;
		}
		
		public void OnDestroy ()
		{
			if (changed)
				Bake ();
		}

		public override void OnInspectorGUI ()
		{
			BRDFLightReceiver l = target as BRDFLightReceiver;

			EditorGUILayout.BeginHorizontal ();
			var prevAffectChildren = l.affectChildren;
			var prevOffsetRenderQueue = l.offsetRenderQueue;
			l.affectChildren = EditorGUILayout.Toggle ("Affect Children", l.affectChildren);
			l.offsetRenderQueue = EditorGUILayout.IntSlider ("Rending Order", l.offsetRenderQueue, -10, 10);
			if (prevAffectChildren != l.affectChildren || prevOffsetRenderQueue != l.offsetRenderQueue)
				l.Update ();
			EditorGUILayout.EndHorizontal ();
			
			EditorGUILayout.Space ();
			l.intensity = EditorGUILayout.Slider ("Intensity", l.intensity, 0f, 8f);

            EditorGUILayout.Space ();
			l.diffuseIntensity = EditorGUILayout.Slider ("Diffuse", l.diffuseIntensity, 0f, 2f);
			if (l.diffuseIntensity > 1e-6)
			{
				EditorGUI.indentLevel++;

				l.keyColor = EditorGUILayout.ColorField ("Key Color", l.keyColor);
				l.fillColor = EditorGUILayout.ColorField ("Fill Color", l.fillColor);
				l.backColor = EditorGUILayout.ColorField ("Back Color", l.backColor);
				l.wrapAround = EditorGUILayout.Slider ("Wrap Around", l.wrapAround, -1f, 1f);
				l.metalic = EditorGUILayout.Slider ("Metalic", l.metalic, 0f, 4f);

				EditorGUI.indentLevel--;
			}
            
            EditorGUILayout.Space();
			l.specularIntensity = EditorGUILayout.Slider ("Specular", l.specularIntensity, 0f, 8f);
			if (l.specularIntensity > 1e-6)
			{
				EditorGUI.indentLevel++;
				l.specularShininess = EditorGUILayout.Slider ("Smoothness", l.specularShininess, 0.03f, 1f);
				EditorGUI.indentLevel--;
			}

            EditorGUILayout.Space();
			l.fresnelIntensity = EditorGUILayout.Slider ("Fresnel", l.fresnelIntensity, 0f, 2f);
			if (l.fresnelIntensity > 1e-6)
			{
				EditorGUI.indentLevel++;
				l.fresnelSharpness = EditorGUILayout.Slider ("Sharpness", l.fresnelSharpness, 0f, 1f);
				l.fresnelReflectionColor = EditorGUILayout.ColorField ("Refl. Color", l.fresnelReflectionColor);
				EditorGUI.indentLevel--;
			}
            
            EditorGUILayout.Space();
			l.translucency = EditorGUILayout.Slider ("Translucency", l.translucency, 0f, 1f);
			if (l.translucency > 1e-6)
			{
				EditorGUI.indentLevel++;
				l.translucentColor = EditorGUILayout.ColorField ("Color", l.translucentColor);
				EditorGUI.indentLevel--;
			}
			
			
            EditorGUILayout.Space();
			GUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Lookup Texture", "MiniPopup");
			l.lookupTextureWidth = EditorGUILayout.IntPopup(l.lookupTextureWidth, kTextureSizes, kTextureSizesValues, GUILayout.MinWidth(40));
			GUILayout.Label("x");
			l.lookupTextureHeight = EditorGUILayout.IntPopup(l.lookupTextureHeight, kTextureSizes, kTextureSizesValues, GUILayout.MinWidth(40));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			
			if (GUI.changed)
			{
				Undo.RegisterCompleteObjectUndo(l, "BRDFLight Params Change");
				changed = true;
			}
			
			
			// preview
			GUILayout.BeginHorizontal();
			fastPreview = EditorGUILayout.Toggle("Fast Preview", fastPreview);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button (previewRGB? "RGB": "Alpha", "MiniButton", GUILayout.MinWidth(38)))
				previewRGB = !previewRGB;
			GUILayout.EndHorizontal();
			
			if (changed || !l.lookupTexture)
			{
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button ("Bake", GUILayout.MinWidth(64)))
				{
					Bake ();
				}
				else
				{
					if (fastPreview)
						l.Preview ();
					else
						l.Bake ();
				}
				GUILayout.EndHorizontal();
			}

			Rect r = GUILayoutUtility.GetAspectRect(1.0f);
			r.x += kTexturePreviewBorder;
			r.y += kTexturePreviewBorder;
			r.width -= kTexturePreviewBorder * 2;
			r.height -= kTexturePreviewBorder * 2;
			if (previewRGB)
				EditorGUI.DrawPreviewTexture(r, l.lookupTexture);
			else
				EditorGUI.DrawTextureAlpha(r, l.lookupTexture);

		}
		
	}
//}