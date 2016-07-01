
@script ExecuteInEditMode()

@CustomEditor (SunShafts)

class SunShaftsEditor extends Editor 
{	
	var serObj : SerializedObject;	
		
	var sunTransform : SerializedProperty;
	var radialBlurIterations : SerializedProperty;
	var sunColor : SerializedProperty;
	var sunShaftBlurRadius : SerializedProperty;
	var sunShaftIntensity : SerializedProperty;
	var useSkyBoxAlpha : SerializedProperty;
	var useDepthTexture : SerializedProperty;
    var resolution : SerializedProperty;
    
    var maxRadius : SerializedProperty;

	function OnEnable () {
		serObj = new SerializedObject (target);
		
		sunTransform = serObj.FindProperty("sunTransform");
		sunColor = serObj.FindProperty("sunColor");
		
		sunShaftBlurRadius = serObj.FindProperty("sunShaftBlurRadius");
		radialBlurIterations = serObj.FindProperty("radialBlurIterations");
		
		sunShaftIntensity = serObj.FindProperty("sunShaftIntensity");
		useSkyBoxAlpha = serObj.FindProperty("useSkyBoxAlpha");
        
        resolution =  serObj.FindProperty("resolution");
        
        maxRadius = serObj.FindProperty("maxRadius"); 
		
		useDepthTexture = serObj.FindProperty("useDepthTexture");
	}
	
	var _dist : float = 500.0;
    		
    function OnInspectorGUI ()
    {        
		var oldVal : boolean = useDepthTexture.boolValue;
		EditorGUILayout.PropertyField (useDepthTexture, new GUIContent("Depth Texture"));
		
		GUILayout.Label(" Camera depth texture mode: "+target.camera.depthTextureMode);
		
		var newVal : boolean = useDepthTexture.boolValue;
		if(newVal != oldVal) {
			if(newVal)
				target.camera.depthTextureMode |= DepthTextureMode.Depth;
			else
				target.camera.depthTextureMode &= ~DepthTextureMode.Depth;
		}
		
    	EditorGUILayout.PropertyField (resolution,  new GUIContent("Resolution"));
        
        EditorGUILayout.Separator ();
    
    	EditorGUILayout.PropertyField (sunTransform, new GUIContent("Sun caster", "Chose a transform that acts as a root point for the produced sun shafts"));
    	
    	if(target.sunTransform && target.camera) {
    		GUILayout.Label(" Sun placement");
    		GUILayout.BeginHorizontal();
    		_dist = EditorGUILayout.FloatField("Distance", _dist);
    		if( GUILayout.Button("Align to viewport center")) {
    			var ray : Ray = target.camera.ViewportPointToRay(Vector3(0.5,0.5,0));
    			target.sunTransform.position = ray.origin + ray.direction * _dist;
    			target.sunTransform.LookAt(target.transform);
    		}
    		GUILayout.EndHorizontal();
    	}
    	
    	EditorGUILayout.PropertyField (sunColor,  new GUIContent("Sun color"));
        EditorGUILayout.PropertyField (maxRadius,  new GUIContent("Radius"));
    	
    	EditorGUILayout.Separator ();
    	
    	sunShaftBlurRadius.floatValue = EditorGUILayout.Slider ("Blur offset", sunShaftBlurRadius.floatValue, 0.0,0.1);
    	radialBlurIterations.intValue = EditorGUILayout.IntSlider ("Blur iterations", radialBlurIterations.intValue, 0,6);
    	
    	EditorGUILayout.Separator ();
    	
    	EditorGUILayout.PropertyField (sunShaftIntensity,  new GUIContent("Intensity"));
    	useSkyBoxAlpha.floatValue = EditorGUILayout.Slider ("Use alpha mask", useSkyBoxAlpha.floatValue, 0.0, 1.0);    	
    	
    	serObj.ApplyModifiedProperties();
    }
}
