using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ZedGraph;
using System.Drawing;

public class FrameToTexture : MonoBehaviour {

	[SerializeField]
	UnityEngine.UI.RawImage texture;			// Image used to show the webcam into the Unity scene

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void DisplayImage( Mat image ){
		Texture2D result = toTexture( image );
		if( texture != null && result != null ) texture.texture = toTexture( image );
	}

	Texture2D toTexture( Mat image ){
		if( image != null ){
			Texture2D text = new Texture2D( image.Bitmap.Width, image.Bitmap.Height );

			System.IO.MemoryStream stream = new System.IO.MemoryStream();
			image.Bitmap.Save( stream, image.Bitmap.RawFormat ) ;

			text.LoadImage( stream.ToArray() );

			stream.Close();
			stream.Dispose();

			return text;
		}
		return null;
	}
}
