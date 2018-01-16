using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ZedGraph;
using System.Drawing;

public class FaceController : MonoBehaviour {

	[SerializeField]
	UnityEngine.UI.RawImage texture;			// Image used to show the webcam into the Unity scene

	[SerializeField]
	FaceAction faceAction;						// Interface to execute actions with unity GameObjects

	VideoCapture webCam;						// Obtains the images from the camera
	VideoWriter writer;							// Writes the frames into videos (or images)

	Mat imageOrig;								// Image that stores the last webcam image

	// Classifiers
	CascadeClassifier _cascadeFaceClassifier;	// Detects faces
	CascadeClassifier _cascadeMouthClassifier;	// Detects mouths

	int imAddress = 0;							// Indicates the address of the image (0 for webcam)

	string imNameOrig = "Image";				// Name of the window to show the images (ignored since we show the image into a unity scene)
	int imSize = 400;							// Dimension of the window to show the images (used to redimension the image, in order to reduce the size of the image and increase preformance)
	string projPath = "C:\\Users\\Edwyn Luis\\Documents\\Lyon\\gamagora\\vision\\";	// Path to the project [Change your path]

	Vector2 mouthRelativePos;					// As the mouth detector does not detect the mouth every frame, we store the last position of the mouth, in order to use it when the mouth is not detected; The relative position to the face is used to profit of face recognition

	// A queue containing the status of the mouth during the frames. Useful to generate opened and closed mouth event
	/*
	 * An event of opened or closed mouth is obtained when the queue has a certain number of Closed or Opened values.
	 * The queue stores only elements that are all Closed or Opened mouth.
	 * The queue is cleared when a value is not according to the inserted elements.
	*/
	int queueEventTriggerAmount = 2;		// amount of values needed to trigger an event
	Queue<bool> closedQueue;				// a queue containing the last status (true: closed; false: opened)

	// Use this for initialization
	void Start () {
		// Camera
		webCam = new VideoCapture(imAddress);
		// Frame handling function
		webCam.ImageGrabbed += new System.EventHandler(GrabWebCam);
		// Initializing writer : file destination
		//writer = new VideoWriter(projPath+"result.avi", VideoWriter.Fourcc('M','P','4','2'), 20, new Size(webCam.Width, webCam.Height), true);

		// Initializing image
		imageOrig = new Mat();

		// Initializing Classifiers
			// Face
		_cascadeFaceClassifier = new CascadeClassifier( Application.dataPath+"\\Resources\\haarcascade_frontalface_alt.xml");
			// Mouth
		_cascadeMouthClassifier = new CascadeClassifier( Application.dataPath+"\\Resources\\haarcascade_mcs_mouth.xml");

		// Initializing Auxiliar variables
		closedQueue = new Queue<bool>();
		mouthRelativePos = new Vector2();
	}
	
	// Update is called once per frame
	void Update () {
		if( webCam.IsOpened ){
			webCam.Grab();
		}

		if( Input.GetKeyDown(KeyCode.Escape) ){
			Application.Quit();
		}
	}

	void GrabWebCam( object sender, System.EventArgs args ){
		if( webCam.IsOpened ){
			webCam.Retrieve( imageOrig );
		}
		if (imageOrig != null)
		{
			CvInvoke.Flip(imageOrig, imageOrig, FlipType.Horizontal);
			CvInvoke.Resize(imageOrig, imageOrig, new Size(imSize, imSize*webCam.Height/webCam.Width));

			Mat imageGray = new Mat();
			CvInvoke.CvtColor( imageOrig, imageGray, ColorConversion.Bgr2Gray );
			if( imageGray == null || imageGray.IsEmpty ){ return; }

			Rectangle[] faces = new Rectangle[1];
			Rectangle selectedFace = Rectangle.Empty;

			if( _cascadeFaceClassifier != null ){
				Image<Bgr,System.Byte> imageFrame = imageOrig.ToImage<Bgr,System.Byte>();
				int MinFaceSize = 50;
				int MaxFaceSize = 200;
				faces = _cascadeFaceClassifier.DetectMultiScale(imageFrame, 1.1, 10, new Size( MinFaceSize, MinFaceSize ), new Size( MaxFaceSize, MaxFaceSize )); //the actual face detection happens here
				foreach (var face in faces)
				{
					imageFrame.Draw(face, new Bgr(System.Drawing.Color.BurlyWood), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them
				}
				if( faces.Length > 0 ){
					selectedFace = faces[0];
				}
				imageOrig = imageFrame.Mat;
			}

			// Mouth Detecting
			if( _cascadeMouthClassifier != null ){
				Image<Bgr,System.Byte> imageFrame = imageOrig.ToImage<Bgr,System.Byte>();
				int MinFaceSize = 10;
				int MaxFaceSize = 120;
				Rectangle[] mouths = _cascadeMouthClassifier.DetectMultiScale(imageFrame, 1.1, 10, new Size( MinFaceSize, MinFaceSize ), new Size( MaxFaceSize, MaxFaceSize )); //the actual face detection happens here

				Rectangle selectedMouth = Rectangle.Empty;

				if( mouths.Length > 0 ){
					selectedMouth = mouths[0];
					foreach (var mouth in mouths)
					{
						float mouthCenterY = mouth.Top + mouth.Height/2f;
						float mouthArea = mouth.Height * mouth.Width;
						if( !selectedFace.IsEmpty ){
							if( mouthCenterY > selectedFace.Top && mouthCenterY < selectedFace.Bottom && mouthCenterY > selectedFace.Top + 2*selectedFace.Height/3f ){
								// Mouth Detected
								if (selectedMouth.IsEmpty || selectedMouth.Height*selectedMouth.Width < mouthArea){
									// Choose mouth
									selectedMouth = mouth;
								}
							}
						}
					}
				}
				if( selectedMouth != null && !selectedMouth.IsEmpty ){

					if( selectedFace != null ){
						mouthRelativePos.Set( (selectedMouth.Left + selectedMouth.Width/2f)-selectedFace.Left, (selectedMouth.Top + selectedMouth.Height/2f)-selectedFace.Top );
					}

					Mat mouthImage = new Mat( imageOrig, selectedMouth );
					Mat mouthImageBW = new Mat();

					CvInvoke.CvtColor( mouthImage, mouthImageBW, ColorConversion.Bgr2Gray );
					CvInvoke.GaussianBlur( mouthImageBW, mouthImageBW, new Size(3,3), 0 );

					// Edges
					Mat edges = new Mat();
					CvInvoke.Canny( mouthImageBW, edges, 60, 180 );
					CvInvoke.Resize(edges, edges, new Size(imSize, imSize*webCam.Height/webCam.Width));

					// Contours
					VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
					int biggestContourIndex = -1;
					double biggestContourArea = -1;
					Mat hierarchy = new Mat();
					CvInvoke.FindContours( edges, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone );

					// biggest Contour
					if( contours.Size > 0 ){
						biggestContourIndex = 0;
						biggestContourArea = CvInvoke.ContourArea( contours[biggestContourIndex] );

						for( int i = 1; i<contours.Size; i++ ){
							double currentArea = CvInvoke.ContourArea( contours[i] );
							if( currentArea > biggestContourArea ){
								biggestContourIndex = i;
								biggestContourArea = currentArea;
							}
						}

						VectorOfPoint biggestContour = contours[biggestContourIndex];
						CvInvoke.DrawContours( imageOrig, contours, biggestContourIndex, new MCvScalar( 255,0,0 ), 2 );

						float ratio = mouthRatio( biggestContour );
						UpdateRatioQueue( ratio );
					}

//					imageOrig = edges;

					// Check Contours Ratio (Width x Height) to determines if the mouth is opened or closed

					imageFrame.Draw(selectedMouth, new Bgr(System.Drawing.Color.Aqua), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them
				}
				imageOrig = imageFrame.Mat;
			}

			CvInvoke.Imshow(imNameOrig, imageOrig);
			if( texture != null ) texture.texture = toTexture( imageOrig );

			// Storing
			//writer.Write(imageOrig);
		}
		else
		{
			//webCam = new VideoCapture(imAddress);
		}
	}

	Mat drawPoint( Mat imageRGB, int x, int y, int pointSize, int colorR = 0, int colorG = 255, int colorB = 0 ){
		Image<Rgb,System.Byte> showInfo = imageRGB.ToImage<Rgb,System.Byte>();
		for( int i = -pointSize; i < pointSize; i++ ){
			for( int j = -pointSize; j < pointSize; j++ ){
				if( x+j >= 0 && x+j < imageRGB.SizeOfDimemsion[1] && y+i >= 0 && y+i < imageRGB.SizeOfDimemsion[0] ){
					showInfo.Data [ y+i, x+j, 0 ] = (byte)colorB;
					showInfo.Data [ y+i, x+j, 1 ] = (byte)colorG;
					showInfo.Data [ y+i, x+j, 2 ] = (byte)colorR;
				}
			}
		}
		return showInfo.Mat;
	}

	Texture2D toTexture( Mat image ){
		System.IO.MemoryStream stream = new System.IO.MemoryStream();
		image.Bitmap.Save( stream, image.Bitmap.RawFormat ) ;
		Texture2D text = new Texture2D( image.Bitmap.Width, image.Bitmap.Height );
		text.LoadImage( stream.ToArray() );

		stream.Close();
		stream.Dispose();

		return text;
	}

	float mouthRatio( VectorOfPoint contour ){
		int minX = contour[0].X;
		int maxX = contour[0].X;
		int minY = contour[0].Y;
		int maxY = contour[0].Y;
		for( int i = 1; i < contour.Size; i++ ){
			if( contour[i].X < minX ){ minX = contour[i].X; }
			if( contour[i].X > maxX ){ maxX = contour[i].X; }
			if( contour[i].Y < minY ){ minY = contour[i].Y; }
			if( contour[i].Y > maxY ){ maxY = contour[i].Y; }
		}

		imageOrig = drawPoint( imageOrig, minX, minY, 5, 255, 0, 0 );
		imageOrig = drawPoint( imageOrig, maxX, maxY, 5 );

		// width / height
		return ( maxX - minX + 1 )/(float)( maxY - minY + 1 );
	}

	bool isMouthClosed( float ratio ){
		return ratio > 1.7;
	}

	void UpdateRatioQueue( float ratio ){
		bool currentStatus = isMouthClosed( ratio );

		if( closedQueue.Count == 0 || closedQueue.Peek() == currentStatus ){
			closedQueue.Enqueue( currentStatus );

			if( closedQueue.Count == queueEventTriggerAmount ){
				if( currentStatus ){
					// Closed Mouth
					closedMouthEvent();
				}
				else{
					// Opened Mouth
					openedMouthEvent();
				}
			}
		}
		// the queue must be cleared because a different mouvement was started
		else{
			closedQueue.Clear();
			closedQueue.Enqueue( currentStatus );
		}
	}

	void setMouthPosition( Rectangle mouth, Mat image ){
		int mouthCenter = mouth.Left + mouth.Width/2;

		if( faceAction != null ){
			faceAction.setHorizontalPosition( mouthCenter / image.Width );
		}
	}

	void closedMouthEvent(){
		if( faceAction != null ){
			faceAction.CloseMouth();
		}
	}
	void openedMouthEvent(){
		if( faceAction != null ){
			faceAction.OpenMouth();
		}
	}

	void OnDestroy()
	{
		CvInvoke.DestroyAllWindows();
	}
}

// remi.ratajczak@gmail.com