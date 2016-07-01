
@script ExecuteInEditMode()

@CustomEditor (EdgeDetectEffectNormals)

class EdgeDetectEffectNormalsEditor extends Editor 
{	
	var serObj : SerializedObject;	
		
	var highQuality : SerializedProperty;
	var sensitivityDepth : SerializedProperty;
	var sensitivityNormals : SerializedProperty;
	var spread : SerializedProperty;

	var edgesIntensity : SerializedProperty;
	var edgesOnly : SerializedProperty;
	var edgesOnlyBgColor : SerializedProperty;

	var edgeBlur : SerializedProperty;
	var blurSpread : SerializedProperty;
	var blurIterations : SerializedProperty;	
	
   	var showShaders : boolean = false;

	function OnEnable () {
		serObj = new SerializedObject (target);
		
		highQuality = serObj.FindProperty("highQuality");
		
		sensitivityDepth = serObj.FindProperty("sensitivityDepth");
		sensitivityNormals = serObj.FindProperty("sensitivityNormals");
		spread = serObj.FindProperty("spread");

		edgesIntensity = serObj.FindProperty("edgesIntensity");
		edgesOnly = serObj.FindProperty("edgesOnly");
		edgesOnlyBgColor = serObj.FindProperty("edgesOnlyBgColor");

		edgeBlur = serObj.FindProperty("edgeBlur");
		blurSpread = serObj.FindProperty("blurSpread");
		blurIterations = serObj.FindProperty("blurIterations");			
	}
    		
    function OnInspectorGUI ()
    {        
    	EditorGUILayout.PropertyField (highQuality, new GUIContent("Advanced"));
    	
    	if (highQuality.boolValue) {
    		GUILayout.Label(" Sensitivity");
    		EditorGUILayout.PropertyField (sensitivityDepth, new GUIContent("Depth"));
    		EditorGUILayout.PropertyField (sensitivityNormals, new GUIContent("Normals"));
    		EditorGUILayout.Separator ();
    		
    		spread.floatValue = EditorGUILayout.Slider ("Spread", spread.floatValue, 0.1, 2.0);
    		
    		
    		EditorGUILayout.PropertyField (edgesIntensity, new GUIContent("Edge intensity"));
    		
    		EditorGUILayout.Separator ();
    		
    		edgesOnly.floatValue = EditorGUILayout.Slider ("Draw edges only", edgesOnly.floatValue, 0.0, 1.0);
    		EditorGUILayout.PropertyField (edgesOnlyBgColor, new GUIContent ("Background"));
    		
    		EditorGUILayout.PropertyField (edgeBlur, new GUIContent("Blur edges"));
    		
    		if (edgeBlur.boolValue) {
    			EditorGUILayout.Separator ();
    			
    			blurSpread.floatValue = EditorGUILayout.Slider ("Blur spread", blurSpread.floatValue, 0.1, 10.0);
    			blurIterations.intValue = EditorGUILayout.IntSlider ("Blur iterations", blurIterations.intValue, 1, 10);
    		}
    	}
    	
    	serObj.ApplyModifiedProperties();
    	

    	/*
    	// some genral tweak needs
    	EditorGUILayout.PropertyField (bloomIntensity, new GUIContent("bloomIntensity"));	
    	bloomBlurIterations.intValue = EditorGUILayout.IntSlider ("Blur iterations", bloomBlurIterations.intValue, 1, 10);
    	if(1==tweakMode)
    		sepBlurSpread.floatValue = EditorGUILayout.Slider ("Blur spread", sepBlurSpread.floatValue, 0.1, 2.0);
    	else
    		sepBlurSpread.floatValue = 1.0;    	
    	bloomThreshhold.floatValue = EditorGUILayout.Slider ("Threshhold", bloomThreshhold.floatValue, 0.1, 2.0);
    	
    	if(1==tweakMode)
    		useSrcAlphaAsMask.floatValue = EditorGUILayout.Slider (new  GUIContent("Use image alpha as mask","How much should the image alpha values (deifned by all materials, colors and textures alpha values define the bright (blooming/glowing) areas of the image"), useSrcAlphaAsMask.floatValue, 0.0, 1.0);
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
    			hollywoodFlareBlurIterations.intValue = EditorGUILayout.IntSlider (" Blur iterations", hollywoodFlareBlurIterations.intValue, 1, 10);
    			
    			EditorGUILayout.PropertyField (flareColorA, new GUIContent(" Color"));
    			
    		} else if (lensflareMode.intValue == 2) {
    			// both
    			EditorGUILayout.PropertyField (hollyStretchWidth, new GUIContent(" Stretch width"));
    			hollywoodFlareBlurIterations.intValue = EditorGUILayout.IntSlider (" Blur iterations", hollywoodFlareBlurIterations.intValue, 1, 10);
    			
    			EditorGUILayout.PropertyField (flareColorA, new GUIContent(" Color"));
    			EditorGUILayout.PropertyField (flareColorB, new GUIContent(" Color"));
    			EditorGUILayout.PropertyField (flareColorC, new GUIContent(" Color"));
    			EditorGUILayout.PropertyField (flareColorD, new GUIContent(" Color"));    			
    		} 
    	}
    	
    	EditorGUILayout.Separator ();
    	
    	if(0==tweakMode) {
    		
    	} else if (1==tweakMode) {
    		EditorGUILayout.PropertyField (enableAddToBloomLayer, new GUIContent("Bloom specific layers?","If you want to always have objects in specific layers to be glowing, chose an appropriate layer mask here. These objects will be glowing/blooming no matter what their material writes to alpha. Make sure to specify the layer mask as precise as possible for maximum performance."));
    		if (enableAddToBloomLayer.boolValue)
    			EditorGUILayout.PropertyField (addToBloomLayers, new GUIContent(" Choose mask","If you want to always have objects in specific layers to be glowing, chose an appropriate layer mask here. These objects will be glowing/blooming no matter what their material writes to alpha.")); 

    		EditorGUILayout.PropertyField (enableRemoveFromBloomLayer, new GUIContent("Don't bloom specific layers?"));
    		if (enableRemoveFromBloomLayer.boolValue)
    			EditorGUILayout.PropertyField (removeFromBloomLayers, new GUIContent(" Choose mask"));     		
    		
		
			EditorGUILayout.Separator ();
    	}
    	
    	// maybe show the fucking shaders
    	showShaders = EditorGUILayout.Toggle ("Show assigned shaders", showShaders);
    	if (showShaders) {
 	    	target.addAlphaHackShader = EditorGUILayout.ObjectField(" shader",target.addAlphaHackShader,Shader as System.Type);
 			target.vignetteShader = EditorGUILayout.ObjectField(" shader",target.vignetteShader,Shader as System.Type);
 			target.lensFlareShader = EditorGUILayout.ObjectField(" shader",target.lensFlareShader,Shader as System.Type);
 			target.separableBlurShader = EditorGUILayout.ObjectField(" shader",target.separableBlurShader,Shader as System.Type);
 			
 			target.addBrightStuffShader = EditorGUILayout.ObjectField(" shader",target.addBrightStuffShader,Shader as System.Type);
    		target.addBrightStuffOneOneShader = EditorGUILayout.ObjectField(" shader",target.addBrightStuffOneOneShader,Shader as System.Type);
    		target.hollywoodFlareBlurShader = EditorGUILayout.ObjectField(" shader",target.hollywoodFlareBlurShader,Shader as System.Type);
    		target.hollywoodFlareStretchShader = EditorGUILayout.ObjectField(" shader",target.hollywoodFlareStretchShader,Shader as System.Type);
    		target.brightPassFilterShader = EditorGUILayout.ObjectField(" shader",target.brightPassFilterShader,Shader as System.Type);
    	}
    	
    	serObj.ApplyModifiedProperties();
       */
    }
}
