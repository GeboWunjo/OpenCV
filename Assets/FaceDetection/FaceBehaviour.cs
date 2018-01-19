using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FaceBehaviour : FaceAction {


    private GameObject[] enemies; 
    #region implemented abstract members of FaceAction
   
    public override void OpenMouth ()
	{
		Debug.Log("open");
        Enemy.Freeze();
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