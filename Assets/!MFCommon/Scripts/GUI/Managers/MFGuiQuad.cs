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
using System.Collections;

public class MFGuiQuad
{
	// PUBLIC MEMBERS

	public readonly int index; // Index of this quad in Renderer's list
	static Vector3[] verts = new Vector3[4];

	// C-TOR / D-TOR

	public MFGuiQuad(int index)
	{
		this.index = index;
	}

	// PUBLIC METHODS

	public void UpdateUVs(MFGuiRenderer guiRenderer, Vector2 uvCoords, Vector2 uvSize)
	{
		guiRenderer.UpdateUV(this, uvCoords, uvSize);
	}

	public void UpdateVertices(MFGuiRenderer guiRenderer, Matrix4x4 matrix, Rect rect, float depth)
	{
		switch (guiRenderer.plane)
		{
		case MFGuiRenderer.SPRITE_PLANE.XZ:
			ComputeVerticesXZ(ref verts, rect, depth);
			break;
		case MFGuiRenderer.SPRITE_PLANE.YZ:
			ComputeVerticesYZ(ref verts, rect, depth);
			break;
		default:
			ComputeVerticesXY(ref verts, rect, depth);
			break;
		}

		guiRenderer.UpdateVertices(this,
								   matrix.MultiplyPoint(verts[0]),
								   matrix.MultiplyPoint(verts[1]),
								   matrix.MultiplyPoint(verts[2]),
								   matrix.MultiplyPoint(verts[3]));
	}

	// Sets the specified color and automatically notifies the
	// SpriteManager to update the colors:
	public void SetColor(MFGuiRenderer guiRenderer, Color color)
	{
		guiRenderer.UpdateColors(this, color);
	}

	public void ReleaseResources(MFGuiRenderer guiRenderer)
	{
		guiRenderer.FreeQuad(this);
	}

	// PRIVATE METHODS

	// Sets the physical dimensions of the sprite in the XY plane:
	void ComputeVerticesXY(ref Vector3[] verts, Rect rect, float depth)
	{
		verts[0] = new Vector3(rect.x - rect.width/2, rect.y + rect.height/2, depth); // Upper-left
		verts[1] = new Vector3(rect.x - rect.width/2, rect.y - rect.height/2, depth); // Lower-left
		verts[2] = new Vector3(rect.x + rect.width/2, rect.y - rect.height/2, depth); // Lower-right
		verts[3] = new Vector3(rect.x + rect.width/2, rect.y + rect.height/2, depth); // Upper-right
	}

	// Sets the physical dimensions of the sprite in the XZ plane:
	void ComputeVerticesXZ(ref Vector3[] verts, Rect rect, float depth)
	{
		verts[0] = new Vector3(rect.x - rect.width/2, rect.y + depth, rect.height/2); // Upper-left
		verts[1] = new Vector3(rect.x - rect.width/2, rect.y + depth, -rect.height/2); // Lower-left
		verts[2] = new Vector3(rect.x + rect.width/2, rect.y + depth, -rect.height/2); // Lower-right
		verts[3] = new Vector3(rect.x + rect.width/2, rect.y + depth, rect.height/2); // Upper-right
	}

	// Sets the physical dimensions of the sprite in the YZ plane:
	void ComputeVerticesYZ(ref Vector3[] verts, Rect rect, float depth)
	{
		verts[0] = new Vector3(rect.x + depth, rect.y + rect.height/2, -rect.width/2); // Upper-left
		verts[1] = new Vector3(rect.x + depth, rect.y - rect.height/2, -rect.width/2); // Lower-left
		verts[2] = new Vector3(rect.x + depth, rect.y - rect.height/2, rect.width/2); // Lower-right
		verts[3] = new Vector3(rect.x + depth, rect.y + rect.height/2, rect.width/2); // Upper-right
	}
}
