
enum DofResolutionSetting {
    Low = 0,
    Normal = 1,
    High = 2,
}

enum DofQualitySetting {
	Low = 0,
	Medium = 1,
	High = 2,
}

public var resolution : DofResolutionSetting = DofResolutionSetting.Normal;
public var quality : DofQualitySetting = DofQualitySetting.High;

public var focalZDistance : float = 0.0;
public var focalZStart : float = 0.0;
public var focalZEnd : float = 10000.0;

private var _focalDistance01 : float;
private var _focalStart01 : float = 0.0;
private var _focalEnd01 : float = 1.0;

public var focalFalloff : float = 1.0;

public var focusOnThis : Transform = null;
public var focusOnScreenCenterDepth : boolean = false;
public var focalSize : float = 0.075;
public var focalChangeSpeed : float = 2.275;

public var blurIterations : int = 2;
public var foregroundBlurIterations : int = 2;

public var blurSpread : float = 1.25;
public var foregroundBlurSpread : float = 1.75;
public var foregroundBlurStrength : float = 1.15;
public var foregroundBlurThreshhold : float = 0.002;

@script ExecuteInEditMode
@script RequireComponent (Camera)
@script AddComponentMenu ("Image Effects/Depth of Field") 

class DepthOfField extends PostEffectsBase {
	
	public var weightedBlurShader : Shader;
	private var _weightedBlurMaterial : Material = null;	
	
	public var preDofShader : Shader;
	private var _preDofMaterial : Material = null;
	
	public var preDofZReadShader : Shader;
	private var _preDofZReadMaterial : Material = null;
	
	public var dofShader : Shader;
	private var _dofMaterial : Material = null;	
    
    public var blurShader : Shader;
    private var _blurMaterial : Material = null;
    
    public var foregroundShader : Shader;
    private var _foregroundBlurMaterial : Material = null;
    
    public var depthConvertShader : Shader;
    private var _depthConvertMaterial : Material = null;
    
    public var depthBlurShader : Shader;
    private var _depthBlurMaterial : Material = null;   
    
    public var recordCenterDepthShader : Shader;
    private var _recordCenterDepthMaterial : Material = null;
    
    private var _onePixel : RenderTexture = null;
	
	function CreateMaterials () 
	{		
		 if(!_onePixel)
		 	_onePixel = new RenderTexture(1,1, 0); 
		 	_onePixel.hideFlags = HideFlags.HideAndDontSave;
		
		if(!_recordCenterDepthMaterial) {
			if(!CheckShader(recordCenterDepthShader)) {
				enabled = false;
				return;				
			}
			_recordCenterDepthMaterial = new Material(recordCenterDepthShader);	
			_recordCenterDepthMaterial.hideFlags = HideFlags.HideAndDontSave;				
		}
		
		if (!_weightedBlurMaterial) {
			if(!CheckShader(blurShader)) {
				enabled = false;
				return;
				}
			_weightedBlurMaterial = new Material(weightedBlurShader);	
			_weightedBlurMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
        
		if (!_blurMaterial) {
			if(!CheckShader(blurShader)) {
				enabled = false;
				return;
				}
			_blurMaterial = new Material(blurShader);	
			_blurMaterial.hideFlags = HideFlags.HideAndDontSave;
		}        
		
		if (!_dofMaterial) { 
			if(!CheckShader(dofShader)) {
				enabled = false;
				return;
				}
			_dofMaterial = new Material(dofShader);	
			_dofMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
		
		if (!_preDofMaterial) {
			if(!CheckShader(preDofShader)) {
				enabled = false;
				return;
				}
			_preDofMaterial = new Material(preDofShader);	
			_preDofMaterial.hideFlags = HideFlags.HideAndDontSave;
		}

		if (!_preDofZReadMaterial) {
			if(!CheckShader(preDofZReadShader)) {
				enabled = false;
				return;
				}
			_preDofZReadMaterial = new Material(preDofZReadShader);	
			_preDofZReadMaterial.hideFlags = HideFlags.HideAndDontSave;
		}
        
        if (!_foregroundBlurMaterial) {
        	if(!CheckShader(foregroundShader)) {
				enabled = false;
				return;
				}
			_foregroundBlurMaterial = new Material(foregroundShader);	
			_foregroundBlurMaterial.hideFlags = HideFlags.HideAndDontSave;        
        }
        
        if (!_depthConvertMaterial) {
        	if(!CheckShader(depthConvertShader)) {
				enabled = false;
				return;
				}
			_depthConvertMaterial = new Material(depthConvertShader);	
			_depthConvertMaterial.hideFlags = HideFlags.HideAndDontSave;        
        }
     
        if (!_depthBlurMaterial) {
        	if(!CheckShader(depthBlurShader)) {
				enabled = false;
				return;
				}
			_depthBlurMaterial = new Material(depthBlurShader);	
			_depthBlurMaterial.hideFlags = HideFlags.HideAndDontSave;        
        }         
        
		if(!SystemInfo.SupportsRenderTextureFormat (RenderTextureFormat.Depth)) {
			enabled = false;
			return;	
		}                  
	}
	
	function Start () {
		CreateMaterials ();
	}


	function OnEnable(){
		GetComponent.<Camera>().depthTextureMode |= DepthTextureMode.Depth;		
	}
	
	function OnRenderImage (source : RenderTexture, destination : RenderTexture)
	{	
		CreateMaterials ();
			
		if(foregroundBlurIterations<1)
			foregroundBlurIterations = 1;

		if(blurIterations<1)
			blurIterations = 1;			
		
		// quality and resolution settings
		
        var divider : float = 4.0;
        if(resolution == DofResolutionSetting.Normal)
            divider = 2.0;
        else if(resolution == DofResolutionSetting.High) 
            divider = 1.0;
		                 
        // support for a game object / transform defining the focal distance      
        
        var preDofMaterial = _preDofMaterial;  
                
		if(focusOnThis) 
		{
			var vpPoint = GetComponent.<Camera>().WorldToViewportPoint(focusOnThis.position);
			vpPoint.z = (vpPoint.z) / (GetComponent.<Camera>().farClipPlane);
			_focalDistance01 = vpPoint.z;
		} 
		else if(focusOnScreenCenterDepth) 
		{ 
			preDofMaterial = _preDofZReadMaterial;
        	// OK, here the zBuffer based "raycast" mode, let's record the blurred
        	// depth buffer center area and add some smoothing
        	_recordCenterDepthMaterial.SetFloat("deltaTime", Time.deltaTime * focalChangeSpeed);
        	Graphics.Blit(_onePixel, _onePixel, _recordCenterDepthMaterial);			
		}
		else 
		{
			_focalDistance01 = GetComponent.<Camera>().WorldToViewportPoint(focalZDistance * GetComponent.<Camera>().transform.forward + GetComponent.<Camera>().transform.position).z / (GetComponent.<Camera>().farClipPlane);	
		}
		
		if(focalZEnd > GetComponent.<Camera>().farClipPlane)
			focalZEnd = GetComponent.<Camera>().farClipPlane;
		_focalStart01 = GetComponent.<Camera>().WorldToViewportPoint(focalZStart * GetComponent.<Camera>().transform.forward + GetComponent.<Camera>().transform.position).z / (GetComponent.<Camera>().farClipPlane);
		_focalEnd01 = GetComponent.<Camera>().WorldToViewportPoint(focalZEnd * GetComponent.<Camera>().transform.forward + GetComponent.<Camera>().transform.position).z / (GetComponent.<Camera>().farClipPlane);

		
		if(_focalEnd01<_focalStart01)
			_focalEnd01 = _focalStart01+Mathf.Epsilon;		
		
        // we use the alpha channel for storing the COC
        // which also means, that unfortunately, alpha based
        // image effects such as sun shafts, bloom or glow
        // won't work if placed *after* the DOF image effect
        
		// simple COC calculation based on distance to focal point     
        
		preDofMaterial.SetFloat("focalDistance01", _focalDistance01);
		preDofMaterial.SetFloat("focalFalloff", focalFalloff);
        preDofMaterial.SetFloat("focalStart01",_focalStart01);
        preDofMaterial.SetFloat("focalEnd01",_focalEnd01);
        preDofMaterial.SetFloat("focalSize", focalSize * 0.5);        
        preDofMaterial.SetTexture("_onePixelTex", _onePixel);
		Graphics.Blit(source, source, preDofMaterial); // ColorMask == ALPHA
		
        var lrTex0 : RenderTexture = RenderTexture.GetTemporary (source.width / divider, source.height / divider, 0); 
		var lrTex1 : RenderTexture = RenderTexture.GetTemporary (source.width / divider, source.height / divider, 0); 
		var lrTex2 : RenderTexture = RenderTexture.GetTemporary (source.width / divider, source.height / divider, 0); 
		
		// high quality DOF should blur the forground       
        
        if(quality >= DofQualitySetting.High) 
        {        	
            lrTex0.filterMode = FilterMode.Point;
            lrTex1.filterMode = FilterMode.Point;   
            lrTex2.filterMode = FilterMode.Point;     
            source.filterMode = FilterMode.Point;   	
        	
            // downsample depth (and encoding in RGBA)
            
            Graphics.Blit(source, lrTex1, _depthConvertMaterial);

            source.filterMode = FilterMode.Bilinear;
            	
			//
        	// depth blur 	(result in lrTex1)          	       
            
            for(var it : int = 0; it < foregroundBlurIterations; it++) {
                _depthBlurMaterial.SetVector ("offsets", Vector4 (0.0, (foregroundBlurSpread) / lrTex1.height, 0.0, _focalDistance01));
                Graphics.Blit (lrTex1, lrTex0, _depthBlurMaterial);
                _depthBlurMaterial.SetVector ("offsets", Vector4 ((foregroundBlurSpread) / lrTex1.width,  0.0, 0.0, _focalDistance01));		
                Graphics.Blit (lrTex0, lrTex1, _depthBlurMaterial);	   
            }      
            
			//
        	// simple blur 	(result in lrTex0)
        
			lrTex0.filterMode = FilterMode.Bilinear;
			lrTex1.filterMode = FilterMode.Bilinear;
			lrTex2.filterMode = FilterMode.Bilinear;   
			
			var tempBlurIterations = blurIterations;      
        
        	if(resolution != DofResolutionSetting.High) 
        	{
				Graphics.Blit(source, lrTex0);     
        	} 
        	else // high resolution, divider is 1
        	{
            	_blurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread) / lrTex1.height, 0.0, 0.0));
           		Graphics.Blit (source, lrTex2, _blurMaterial);
            	_blurMaterial.SetVector ("offsets", Vector4 ((blurSpread) / lrTex1.width,  0.0, 0.0, 0.0));		
            	Graphics.Blit (lrTex2, lrTex0, _blurMaterial);	        			
            	
            	tempBlurIterations -= 1;          		
        	}  
         
        	for(it = 0; it < tempBlurIterations; it++) {
            	_blurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread) / lrTex1.height, 0.0, 0.0));
           		Graphics.Blit (lrTex0, lrTex2, _blurMaterial);
            	_blurMaterial.SetVector ("offsets", Vector4 ((blurSpread) / lrTex1.width,  0.0, 0.0, 0.0));		
            	Graphics.Blit (lrTex2, lrTex0, _blurMaterial);	           
        	}             

        	// calculate the foreground blur factor and add to our COC
        	
        	lrTex1.filterMode = FilterMode.Point; 
            
            _foregroundBlurMaterial.SetFloat("focalFalloff", focalFalloff);
            _foregroundBlurMaterial.SetFloat("focalDistance01", _focalDistance01);
            _foregroundBlurMaterial.SetFloat("focalStart01",_focalStart01);
            _foregroundBlurMaterial.SetFloat("focalEnd01",_focalEnd01);    
            _foregroundBlurMaterial.SetFloat("foregroundBlurStrength", foregroundBlurStrength);  
            _foregroundBlurMaterial.SetFloat("foregroundBlurThreshhold", foregroundBlurThreshhold);
            _foregroundBlurMaterial.SetTexture("_SourceTex", source);
            _foregroundBlurMaterial.SetTexture("_BlurredCoc", lrTex0);
           	Graphics.Blit (lrTex1, source, _foregroundBlurMaterial);    
            
            lrTex0.filterMode = FilterMode.Bilinear;
            lrTex1.filterMode = FilterMode.Bilinear;
            lrTex2.filterMode = FilterMode.Bilinear;
        } // high quality
        
		// weighted by COC blur
		
		var tempStdBlurIterations : int = blurIterations;
        
        if(resolution != DofResolutionSetting.High) {
			Graphics.Blit(source, lrTex1);            
        }
        else // high resolution and divider == 1
        {
            if(quality >= DofQualitySetting.Medium) 
            {        	
				_weightedBlurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread) / lrTex1.height, 0.0, _focalDistance01));
				Graphics.Blit (source, lrTex2, _weightedBlurMaterial);
				_weightedBlurMaterial.SetVector ("offsets", Vector4 ((blurSpread) / lrTex1.width,  0.0, 0.0, _focalDistance01));		
				Graphics.Blit (lrTex2, lrTex1, _weightedBlurMaterial);	 
            }
            else
            {
				_blurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread) / lrTex1.height, 0.0, 0.0));
				Graphics.Blit (source, lrTex2, _blurMaterial);
				_blurMaterial.SetVector ("offsets", Vector4 ((blurSpread) / lrTex1.width,  0.0, 0.0, 0.0));		
				Graphics.Blit (lrTex2, lrTex1, _blurMaterial);	             	
            }       	
        	
        	tempStdBlurIterations -= 1;
        }
            
        for(it = 0; it < tempStdBlurIterations; it++) 
        {
            if(quality >= DofQualitySetting.Medium) 
            {
                _weightedBlurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread) / lrTex1.height, 0.0, _focalDistance01));
                Graphics.Blit (lrTex1, lrTex2, _weightedBlurMaterial);
                _weightedBlurMaterial.SetVector ("offsets", Vector4 ((blurSpread) / lrTex1.width,  0.0, 0.0, _focalDistance01));		
                Graphics.Blit (lrTex2, lrTex1, _weightedBlurMaterial);	
            } 
            else 
            {
                 _blurMaterial.SetVector ("offsets", Vector4 (0.0, (blurSpread) / lrTex1.height, 0.0, 0.0));
                Graphics.Blit (lrTex1, lrTex2, _blurMaterial);
                _blurMaterial.SetVector ("offsets", Vector4 ((blurSpread) / lrTex1.width,  0.0, 0.0, 0.0));		
                Graphics.Blit (lrTex2, lrTex1, _blurMaterial);	           
            }
        }       
		
		//
		// composite the final image, depending on COC, source and low resolution color buffers
        
        _dofMaterial.SetFloat ("focalDistance01", _focalDistance01);
		_dofMaterial.SetTexture ("_LowRez", lrTex1);
		Graphics.Blit(source, destination, _dofMaterial);	// also writes ALPHA = 1
        
        RenderTexture.ReleaseTemporary(lrTex0);
		RenderTexture.ReleaseTemporary(lrTex1);
		RenderTexture.ReleaseTemporary(lrTex2);
	}	
}
