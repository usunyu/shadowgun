
@script ExecuteInEditMode()

@CustomEditor (DepthOfField)

class DepthOfFieldEditor extends Editor 
{	
	var serObj : SerializedObject;	
		
	var resolution : SerializedProperty; // = DofResolutionSetting.Normal;
	var quality : SerializedProperty; // = DofQualitySetting.High;

	var focalZDistance : SerializedProperty;//float = 0.0;
	var focalZStart : SerializedProperty;//float = 0.0;
	var focalZEnd : SerializedProperty;//float = 10000.0;
	var focalFalloff : SerializedProperty;//float = 1.0;

	var focusOnThis : SerializedProperty;//Transform = null;
	var focusOnScreenCenterDepth : SerializedProperty;//boolean = false;
	var focalSize : SerializedProperty;//float = 0.0375;
	var focalChangeSpeed : SerializedProperty;//float = 2.275;

	var blurIterations : SerializedProperty;//int = 2;
	var foregroundBlurIterations : SerializedProperty;//int = 2;

	var blurSpread : SerializedProperty;//float = 1.5;
	var foregroundBlurSpread : SerializedProperty;//float = 1.5;
	var foregroundBlurStrength : SerializedProperty;//float = 1.5;
	var foregroundBlurThreshhold : SerializedProperty;//float = 0.001;
	
	function OnEnable () {
		serObj = new SerializedObject (target);
		
		resolution = serObj.FindProperty("resolution");
		quality = serObj.FindProperty("quality");
		
        focalZDistance = serObj.FindProperty("focalZDistance");
        focalZStart = serObj.FindProperty("focalZStart");
        focalZEnd = serObj.FindProperty("focalZEnd");
        focalFalloff = serObj.FindProperty("focalFalloff");
        
        focusOnThis = serObj.FindProperty("focusOnThis");
        focusOnScreenCenterDepth = serObj.FindProperty("focusOnScreenCenterDepth");
        focalSize = serObj.FindProperty("focalSize");
        focalChangeSpeed = serObj.FindProperty("focalChangeSpeed");
        
        blurIterations = serObj.FindProperty("blurIterations");
        foregroundBlurIterations = serObj.FindProperty("foregroundBlurIterations");
        
        blurSpread = serObj.FindProperty("blurSpread");
        foregroundBlurSpread = serObj.FindProperty("foregroundBlurSpread");
        foregroundBlurStrength = serObj.FindProperty("foregroundBlurStrength");
        foregroundBlurThreshhold = serObj.FindProperty("foregroundBlurThreshhold");
	}
    		
    function OnInspectorGUI ()
    {        
    	EditorGUILayout.PropertyField (resolution,  new GUIContent("Resolution"));
            
        EditorGUILayout.PropertyField (quality,  new GUIContent("Quality"));
        
        EditorGUILayout.Separator ();
        
        focalZDistance.floatValue = EditorGUILayout.FloatField("Focal Distance", focalZDistance.floatValue);
        focalZStart.floatValue = EditorGUILayout.FloatField("Focal Start", focalZStart.floatValue);
        focalZEnd.floatValue = EditorGUILayout.FloatField("Focal End", focalZEnd.floatValue);
        focalFalloff.floatValue = EditorGUILayout.FloatField("Focal Falloff", focalFalloff.floatValue);
        
        EditorGUILayout.Separator ();
          
        EditorGUILayout.PropertyField (focusOnScreenCenterDepth, new GUIContent("Focus On Center", "This will enable automatic depth buffer read to focus on the area centered around a raycast throught the center of the screen."));
        
        if(focusOnScreenCenterDepth.boolValue) 
        {
        	EditorGUILayout.PropertyField (focalSize, new GUIContent("Focal Size"));
        	EditorGUILayout.PropertyField (focalChangeSpeed, new GUIContent("Adjust Speed"));
        } 
        else 
        {
        	EditorGUILayout.PropertyField (focusOnThis,  new GUIContent("Focus on transform")); 	
        }
        
        EditorGUILayout.Separator ();
        
        blurIterations.intValue = EditorGUILayout.IntSlider ("Blur Iterations", blurIterations.intValue, 1,10);
        blurSpread.floatValue = EditorGUILayout.Slider ("Blur Spread",blurSpread.floatValue,0.0,5.0);
        
        EditorGUILayout.Separator ();
        
        if (quality.intValue > 1) {
        	GUILayout.Label("Foreground Blur Settings");
            foregroundBlurIterations.intValue = EditorGUILayout.IntSlider ("Iterations", foregroundBlurIterations.intValue, 1,5);
            foregroundBlurSpread.floatValue = EditorGUILayout.Slider ("Spread",foregroundBlurSpread.floatValue,0.0,5.0);   
            foregroundBlurStrength.floatValue = EditorGUILayout.FloatField ("Strength",foregroundBlurStrength.floatValue);   
            foregroundBlurThreshhold.floatValue = EditorGUILayout.FloatField ("Threshhold",foregroundBlurThreshhold.floatValue);   
        }
    	
    	serObj.ApplyModifiedProperties();
    	
    	
    }
}
