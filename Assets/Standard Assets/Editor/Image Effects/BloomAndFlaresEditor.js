
@script ExecuteInEditMode()

@CustomEditor (BloomAndFlares)

enum TweakMode {
	Simple = 0,
	Advanced = 1,
}
		
class BloomAndFlaresEditor extends Editor 
{	
	var tweakMode : SerializedProperty;
	
	var serObj : SerializedObject;
	
	var bloomThisTag : SerializedProperty;

	var sepBlurSpread : SerializedProperty;
	var useSrcAlphaAsMask : SerializedProperty;

	var bloomIntensity : SerializedProperty;
	var bloomThreshhold : SerializedProperty;
	var bloomBlurIterations : SerializedProperty;
	
	var lensflares : SerializedProperty;
	
	var hollywoodFlareBlurIterations : SerializedProperty;
	
	var lensflareMode : SerializedProperty;
	var hollyStretchWidth : SerializedProperty;
	var lensflareIntensity : SerializedProperty;
	var lensflareThreshhold : SerializedProperty;
	var flareColorA : SerializedProperty;
	var flareColorB : SerializedProperty;
	var flareColorC : SerializedProperty;
	var flareColorD : SerializedProperty;	
	
	var blurWidth : SerializedProperty;


	function OnEnable () {
		serObj = new SerializedObject (target);
		
		bloomThisTag = serObj.FindProperty("bloomThisTag");
		
		sepBlurSpread = serObj.FindProperty("sepBlurSpread");
		useSrcAlphaAsMask = serObj.FindProperty("useSrcAlphaAsMask");
		
		bloomIntensity = serObj.FindProperty("bloomIntensity");
		bloomThreshhold = serObj.FindProperty("bloomThreshhold");
		bloomBlurIterations = serObj.FindProperty("bloomBlurIterations");
		
		lensflares = serObj.FindProperty("lensflares"); 
		
		lensflareMode = serObj.FindProperty("lensflareMode");
		hollywoodFlareBlurIterations = serObj.FindProperty("hollywoodFlareBlurIterations");
		hollyStretchWidth = serObj.FindProperty("hollyStretchWidth");
		lensflareIntensity = serObj.FindProperty("lensflareIntensity");
		lensflareThreshhold = serObj.FindProperty("lensflareThreshhold");
		flareColorA = serObj.FindProperty("flareColorA");
		flareColorB = serObj.FindProperty("flareColorB");
		flareColorC = serObj.FindProperty("flareColorC");
		flareColorD = serObj.FindProperty("flareColorD");		
		
		
		blurWidth = serObj.FindProperty("blurWidth");
		
		tweakMode = serObj.FindProperty("tweakMode");
	}
    		
    function OnInspectorGUI ()
    {        
    	//tweakMode = EditorGUILayout.EnumPopup("Mode", tweakMode, EditorStyles.popup);
		EditorGUILayout.PropertyField (tweakMode, new GUIContent("Mode"));	
    	
    	EditorGUILayout.Separator ();
    	// some genral tweak needs
    	EditorGUILayout.PropertyField (bloomIntensity, new GUIContent("Intensity"));	
    	EditorGUILayout.Separator ();
    	
    	bloomBlurIterations.intValue = EditorGUILayout.IntSlider ("Blur iterations", bloomBlurIterations.intValue, 1, 10);
    	if(1==tweakMode.intValue)
    		sepBlurSpread.floatValue = EditorGUILayout.Slider ("Blur spread", sepBlurSpread.floatValue, 0.1, 2.0);
    	else
    		sepBlurSpread.floatValue = 1.0;    	
    	bloomThreshhold.floatValue = EditorGUILayout.Slider ("Threshhold", bloomThreshhold.floatValue, -0.05, 1.0);
    	
    	if(1==tweakMode.intValue)
    		useSrcAlphaAsMask.floatValue = EditorGUILayout.Slider (new  GUIContent("Use alpha mask","How much should the image alpha values (deifned by all materials, colors and textures alpha values define the bright (blooming/glowing) areas of the image"), useSrcAlphaAsMask.floatValue, 0.0, 1.0);
    	else
    		useSrcAlphaAsMask.floatValue = 1.0;
    	
    	EditorGUILayout.Separator ();
    	
    	EditorGUILayout.PropertyField (lensflares, new GUIContent("Cast lens flares"));
    	if(lensflares.boolValue) {
    		
    		EditorGUILayout.PropertyField (lensflareIntensity, new GUIContent("Intensity"));
    		lensflareThreshhold.floatValue = EditorGUILayout.Slider ("Threshhold", lensflareThreshhold.floatValue, 0.0, 1.0);
    		
    		EditorGUILayout.Separator ();
    		
    		// further lens flare tweakings
    		EditorGUILayout.PropertyField (lensflareMode, new GUIContent(" Mode"));
    		
    		if (lensflareMode.intValue == 0) {
    			// ghosting	
    			EditorGUILayout.PropertyField (flareColorA, new GUIContent(" Color"));
    			EditorGUILayout.PropertyField (flareColorB, new GUIContent(" Color"));
    			EditorGUILayout.PropertyField (flareColorC, new GUIContent(" Color"));
    			EditorGUILayout.PropertyField (flareColorD, new GUIContent(" Color"));
    			
    		} else if (lensflareMode.intValue == 1) {
    			// hollywood
    			EditorGUILayout.PropertyField (hollyStretchWidth, new GUIContent(" Stretch width"));
    			hollywoodFlareBlurIterations.intValue = EditorGUILayout.IntSlider (" Blur iterations", hollywoodFlareBlurIterations.intValue, 1, 8);
    			    			
    			EditorGUILayout.PropertyField (flareColorA, new GUIContent(" Color"));
    			
    		} else if (lensflareMode.intValue == 2) {
    			// both
    			EditorGUILayout.PropertyField (hollyStretchWidth, new GUIContent(" Stretch width"));
    			hollywoodFlareBlurIterations.intValue = EditorGUILayout.IntSlider (" Blur iterations", hollywoodFlareBlurIterations.intValue, 1, 8);
    			    			
    			EditorGUILayout.PropertyField (flareColorA, new GUIContent(" Color"));
    			EditorGUILayout.PropertyField (flareColorB, new GUIContent(" Color"));
    			EditorGUILayout.PropertyField (flareColorC, new GUIContent(" Color"));
    			EditorGUILayout.PropertyField (flareColorD, new GUIContent(" Color"));    			
			} 		
    	}
    	
    	EditorGUILayout.Separator ();
    	
    	if(0==tweakMode.intValue) {
    		
    	} 
    	else if (1==tweakMode.intValue) 
    	{		
    		

    			//EditorGUILayout.PropertyField (bloomThisTag, new GUIContent("Extra Bloom Tag","If you want to always have objects of a certain tag to be 'glowing', select the tag here and tag the game objects in question. These objects will start glowing/blooming no matter what their material writes to the alpha channel."));    
    		GUILayout.Label("Add extra bloom on tagged objects?");
    		EditorGUILayout.BeginHorizontal();		
    		GUILayout.Label(" Tag");
			bloomThisTag.stringValue = EditorGUILayout.TagField(bloomThisTag.stringValue);
			EditorGUILayout.EndHorizontal();		
		
			EditorGUILayout.Separator ();
    	}
    	
    	
    	serObj.ApplyModifiedProperties();

       /*
       if (GUI.changed) {
        	EditorUtility.SetDirty (target);     
        	//(target._dirty = true;
       }
       */
    }
}
