#pragma strict
@script ExecuteInEditMode()

var intensity : float = 1.0;
private var renderers : Component[];
private var shader : Shader;

var diffuseIntensity : float = 1.0;
var keyColor : Color = Color(188.0/255, 158.0/255, 118.0/255, 0.0);
var fillColor : Color = Color(86.0/255, 91.0/255, 108.0/255, 0.0);
var backColor : Color = Color(44.0/255, 54.0/255, 57.0/255, 0.0);
var wrapAround : float = 0.0;
var metalic : float = 0.0;

var specularIntensity : float = 1.0;
var specularShininess : float = 0.078125;

var fresnelIntensity : float = 0.0; // water, rim
var fresnelSharpness : float = 0.5;
var fresnelReflectionColor : Color = Color(86.0/255, 91.0/255, 108.0/255, 0.0);

var translucency : float = 0.0; // skin
var translucentColor : Color = Color(255.0/255, 82.0/255, 82.0/255, 0.0);

var lookupTextureWidth : int = 128;
var lookupTextureHeight : int = 128;

var lookupTexture : Texture2D;
private var internallyCreatedTexture : Texture2D;

var offsetRenderQueue : int = 0;
var affectChildren : boolean = true;

function Start () {
	if (lookupTexture)
		lookupTexture.wrapMode = TextureWrapMode.Clamp;

	if (Application.isEditor)
	{
		shader = Shader.Find( "MADFINGER/Characters/BRDFLit  (Supports Backlight)" );
		UpdateRenderers ();
		if (!lookupTexture)
			Preview ();
	}
}

function Preview () {
	UpdateRenderers ();
	UpdateBRDFTexture (32, 64);
}

function Bake () {
	UpdateRenderers ();
	UpdateBRDFTexture (lookupTextureWidth, lookupTextureHeight);
}

function Update () {
	if (Application.isEditor)
	{
		UpdateRenderers ();
		SetupShader (shader, lookupTexture);
		
		if (internallyCreatedTexture != lookupTexture)
			DestroyImmediate (internallyCreatedTexture);
	}
}

private function UpdateRenderers ()
{
	if (affectChildren)
		renderers = gameObject.GetComponentsInChildren(Renderer, true);
	else
	{
		renderers =new Array (gameObject.GetComponent(Renderer));
	}
}

private function SetupShader (shader : Shader, brdfLookupTex : Texture2D) {
	brdfLookupTex.wrapMode = TextureWrapMode.Clamp;

	for (var c : Component in renderers)
	{
		var r = c as Renderer;
		for (var mat : Material in r.sharedMaterials)
		{
			if (shader && mat.shader != shader)
				mat.shader = shader;
				
			if (brdfLookupTex)
				mat.SetTexture("_BRDFTex", brdfLookupTex);
				
			mat.renderQueue = 2000 + offsetRenderQueue; // Background is 1000, Geometry is 2000, Transparent is 3000 and Overlay is 4000
		}
	}
}

private function PixelFunc (ndotl : float, ndoth : float) : Color
{
	// pseudo metalic diffuse falloff
	ndotl *= Mathf.Pow(ndoth, metalic);
	var modDiffuseIntensity = (1.0+metalic*0.25)* Mathf.Max(0.0, diffuseIntensity - (1.0-ndoth) * metalic);

	// diffuse tri-light
	var t0 = Mathf.Clamp01(Mathf.InverseLerp(-wrapAround, 1.0, ndotl * 2.0 - 1.0));
	var t1 = Mathf.Clamp01(Mathf.InverseLerp(-1.0, Mathf.Max(-0.99,-wrapAround), ndotl * 2.0 - 1.0));
	var diffuse = modDiffuseIntensity * Color.Lerp(backColor, Color.Lerp(fillColor, keyColor, t0), t1);
	
	// Blinn-Phong specular (with energy conservation)
	var n : float = specularShininess * 128.0;
	var energyConservationTerm : float = ((n + 2)*(n + 4)) / (8 * Mathf.PI * (Mathf.Pow(2.0, -n/2.0) + n)); // by ryg
	//var energyConservationTerm : float = (n + 8) / (8 * Mathf.PI); // from Real-Time Rendering
	var specular = specularIntensity * energyConservationTerm * Mathf.Pow(ndoth, n);
	
	// Fresnel reflection (Schlick approximation)
	var fresnelR0 = Mathf.Lerp(0.3, -1.0, fresnelSharpness);
	var fresnelTerm = fresnelIntensity * Mathf.Max(0.0, fresnelR0 + (1.0-fresnelR0) * Mathf.Pow(1.0-ndoth, 5.0));
	
	// pseudo translucency (view dependent)
	var t = 0.5 * translucency * Mathf.Clamp01(1.0-ndoth) * Mathf.Clamp01(1.0-ndotl);

	//var c = Color(0,0,0, specular);
	var c = diffuse * intensity + fresnelReflectionColor * fresnelTerm + translucentColor * t + Color(0,0,0, specular);
	return c * intensity;
	
}

private function FillPseudoBRDF (tex : Texture2D)
{
	for (var y = 0; y < tex.height; ++y)
		for (var x = 0; x < tex.width; ++x)
		{
			var w : float = tex.width;
			var h : float = tex.height;
			var vx : float = x / w;
			var vy : float = y / h;
			
			var NdotL : float = vx;
			var NdotH : float = vy;
			var c : Color = PixelFunc (NdotL, NdotH);
			tex.SetPixel(x, y, c);
		}
}

private function UpdateBRDFTexture (width : int, height : int) {
	var tex : Texture2D;
	if (lookupTexture == internallyCreatedTexture && lookupTexture && lookupTexture.width == width && lookupTexture.height == height)
		tex = lookupTexture;
	else
	{
		if (lookupTexture == internallyCreatedTexture)
			DestroyImmediate(lookupTexture);
		tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
		internallyCreatedTexture = tex;
	}
	
	FillPseudoBRDF (tex);
	tex.Apply();

	SetupShader (shader, tex);
	lookupTexture = tex;
}
