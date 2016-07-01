
@script ExecuteInEditMode()

@CustomEditor (ColorCorrectionCurves)
class ColorCorrectionCurvesEditor extends Editor 
{
	public var showShaders : boolean = false;	
		
	function Awake () {

	}
	
	function OnEnable () {
		if(!target.redChannel)
			target.redChannel = new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0));
		if(!target.greenChannel)
			target.greenChannel = new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0));
		if(!target.blueChannel)
			target.blueChannel = new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0));	

		if(!target.depthRedChannel)
			target.depthRedChannel = new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0));
		if(!target.depthGreenChannel)
			target.depthGreenChannel = new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0));
		if(!target.depthBlueChannel)
			target.depthBlueChannel = new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0));	
			
		if(!target.zCurve)
			target.zCurve = new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0));	
			
		EditorUtility.SetDirty (target);		
	}
    		
    function OnInspectorGUI () 
    {        
    	
    	target.mode = EditorGUILayout.EnumPopup ("Mode", target.mode, EditorStyles.popup);
    	EditorGUILayout.Separator ();
    	
    	
		EditorGUILayout.BeginHorizontal ();
		
        target.redChannel = EditorGUILayout.CurveField (GUIContent("Red"), target.redChannel, Color.red, Rect(0.0,0.0,1.0,1.0));
        //EditorGUILayout.CurveField (GUIContent("Red"), property, settings );
        
        if (GUILayout.Button ("Reset")) {
        	 target.redChannel = EditorGUILayout.CurveField (GUIContent("Red"), new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0)), Color.red, Rect(0.0,0.0,1.0,1.0));	
        	 target.updateTextures = true;
        	 EditorUtility.SetDirty (target);
        }
        EditorGUILayout.EndHorizontal ();
        
        EditorGUILayout.BeginHorizontal ();
        target.greenChannel = EditorGUILayout.CurveField (GUIContent("Green"), target.greenChannel, Color.green, Rect(0.0,0.0,1.0,1.0));
        if (GUILayout.Button ("Reset")) {
        	 target.greenChannel = EditorGUILayout.CurveField (GUIContent("Green"), new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0)), Color.green, Rect(0.0,0.0,1.0,1.0));	
  			 target.updateTextures = true;  
        	 EditorUtility.SetDirty (target);
        }
        EditorGUILayout.EndHorizontal ();
        
        EditorGUILayout.BeginHorizontal ();
        target.blueChannel = EditorGUILayout.CurveField (GUIContent("Blue"), target.blueChannel, Color.blue, Rect(0.0,0.0,1.0,1.0));
        if (GUILayout.Button ("Reset")) {
        	 target.blueChannel = EditorGUILayout.CurveField (GUIContent("Blue"), new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0)), Color.blue, Rect(0.0,0.0,1.0,1.0));	
        	 target.updateTextures = true;
        	 EditorUtility.SetDirty (target);
        }
        EditorGUILayout.EndHorizontal ();
        
        EditorGUILayout.Separator ();
        
        //target.useDepthCorrection = EditorGUILayout.Toggle ("Depth Correction", target.useDepthCorrection);
        if( target.mode > 0 )
        	target.useDepthCorrection = true;
        else 
        	target.useDepthCorrection = false;
        
        if (target.useDepthCorrection) 
        {
        	EditorGUILayout.Separator ();
	        
	        EditorGUILayout.BeginHorizontal ();
	        target.depthRedChannel = EditorGUILayout.CurveField (GUIContent("Red (Depth)"), target.depthRedChannel, Color.red, Rect(0.0,0.0,1.0,1.0));
	        if (GUILayout.Button ("Reset")) {
	        	 target.depthRedChannel = EditorGUILayout.CurveField (GUIContent("Red (Depth)"), new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0)), Color.red, Rect(0.0,0.0,1.0,1.0));	
	        	 target.updateTextures = true;
	        	 EditorUtility.SetDirty (target);
	        }
	        EditorGUILayout.EndHorizontal ();
	        
	        EditorGUILayout.BeginHorizontal ();
	        target.depthGreenChannel = EditorGUILayout.CurveField (GUIContent("Green (Depth)"), target.depthGreenChannel, Color.green, Rect(0.0,0.0,1.0,1.0));
	        if (GUILayout.Button ("Reset")) {
	        	 target.depthGreenChannel = EditorGUILayout.CurveField (GUIContent("Green (Depth)"), new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0)), Color.green, Rect(0.0,0.0,1.0,1.0));	
	        	 target.updateTextures = true;
	        	 EditorUtility.SetDirty (target);
	        }
	        EditorGUILayout.EndHorizontal ();
	        
	        EditorGUILayout.BeginHorizontal ();
	        target.depthBlueChannel = EditorGUILayout.CurveField (GUIContent("Blue (Depth)"), target.depthBlueChannel, Color.blue, Rect(0.0,0.0,1.0,1.0));
	        if (GUILayout.Button ("Reset")) {
	        	 target.depthBlueChannel = EditorGUILayout.CurveField (GUIContent("Blue (Depth)"), new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0)), Color.blue, Rect(0.0,0.0,1.0,1.0));	
	        	 target.updateTextures = true;
	        	 EditorUtility.SetDirty (target);
	        }  
	        EditorGUILayout.EndHorizontal ();
	        
	        EditorGUILayout.Separator ();
	        
	        EditorGUILayout.BeginHorizontal ();
	        target.zCurve = EditorGUILayout.CurveField (GUIContent("z Curve"), target.zCurve, Color.white, Rect(0.0,0.0,1.0,1.0));
	        if (GUILayout.Button ("Reset")) {
	        	 target.zCurve = EditorGUILayout.CurveField (GUIContent("z Curve"), new AnimationCurve(Keyframe(0, 0.0, 1.0, 1.0), Keyframe(1, 1.0, 1.0, 1.0)), Color.white, Rect(0.0,0.0,1.0,1.0));				
	        	 target.updateTextures = true;
	        	 EditorUtility.SetDirty (target);
	        }          
	        EditorGUILayout.EndHorizontal ();   
        }
        
		EditorGUILayout.Separator ();
		target.selectiveCc = EditorGUILayout.Toggle ("Selective", target.selectiveCc);
        
        if (target.selectiveCc) 
        {
        	target.selectiveFromColor = EditorGUILayout.ColorField("Key color", target.selectiveFromColor);
        	target.selectiveToColor = EditorGUILayout.ColorField("Target color", target.selectiveToColor);
        }            
        
       if (GUI.changed) {
       		target.updateTextures = true;
        	EditorUtility.SetDirty (target);     
       }
       
    }
}
