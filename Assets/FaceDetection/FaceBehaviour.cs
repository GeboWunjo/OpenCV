using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceBehaviour : FaceAction {
	#region implemented abstract members of FaceAction
	public override void OpenMouth ()
	{
		Debug.Log("open");
	}
	public override void CloseMouth ()
	{
		Debug.Log("close");
	}
	#endregion

	// Use this for initialization
	void Start () {
	}

	// Update is called once per frame
	void Update () {
	}
}
