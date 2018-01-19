using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ZedGraph;
using System.Drawing;

/// <summary>
/// This class receives the image from the camera and share it between the handlers.
/// This behaviour avoids that the handlers take the image directly from the camera.
/// </summary>
public class FrameController : MonoBehaviour {

	// Objects that will manipulate the image
	[SerializeField]
	AbstractFrameHandler frameHandlers_Face;
	[SerializeField]
	AbstractFrameHandler frameHandlers_Ball;

	// Objects that will show the result image into the scene
	[SerializeField]
	FrameToTexture frameDisplay_Face;
	[SerializeField]
	FrameToTexture frameDisplay_Ball;

	VideoCapture webCam;						// Obtains the images from the camera
	int imAddress = 0;							// Indicates the address of the image (0 for webcam)

	int imSize = 600;							// Dimension of the window to show the images (used to redimension the image, in order to reduce the size of the image and increase preformance)

	Mat imageOrig;								// Image that stores the last webcam image

	// Use this for initialization
	void Start () {
		// Initializing image
		imageOrig = new Mat();

		// Camera
		webCam = new VideoCapture(imAddress);
		// Frame handling function
		webCam.ImageGrabbed += new System.EventHandler(GrabWebCam);
	}
	
	// Update is called once per frame
	void Update () {
		if( webCam.IsOpened ){
			webCam.Grab();
		}
	}

	void GrabWebCam( object sender, System.EventArgs args ){
		// Retrieving image
		if( webCam.IsOpened ){
			webCam.Retrieve( imageOrig );

			if( imageOrig != null ){
				// Reducing the size of the image in order to increase the performance
				CvInvoke.Resize(imageOrig, imageOrig, new Size(imSize, imSize*webCam.Height/webCam.Width));
				// Image is originally inverted : flipping
				//CvInvoke.Flip(imageOrig, imageOrig, FlipType.Horizontal);

				// Send the frame to registered handlers : Face
				if( frameHandlers_Face != null ){
					Mat result = frameHandlers_Face.handleFrame( imageOrig );
					// Display result
					if( result != null && frameDisplay_Face != null ){
						frameDisplay_Face.DisplayImage( result );
					}
				}
				// Send the frame to registered handlers : Ball
				if( frameHandlers_Ball != null ){
					Mat result = frameHandlers_Ball.handleFrame( imageOrig );
					// Display result
					if( result != null && frameDisplay_Ball != null ){
						frameDisplay_Ball.DisplayImage( result );
					}
				}
			}
		}
	}
}
