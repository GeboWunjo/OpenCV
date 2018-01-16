using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class FaceAction : MonoBehaviour {

	protected float horizontalFacePosition;	// [0,1]

	public abstract void OpenMouth();
	public abstract void CloseMouth();

	public void setHorizontalPosition( float pos ){
		this.horizontalFacePosition = pos;
	}
}
