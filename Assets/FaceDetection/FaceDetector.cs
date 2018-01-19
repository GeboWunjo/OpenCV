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
/// Detects the mouvement of opening and closing the mouth.
/// It detects the mouth (which is inside a face) and check its ratio. If the ratio indicates a mouth with a large width, the mouth is considered closed; If the ratio indicates a mouth with a large height, the mouth is considered opened.
/// It uses a classifier to detect the Face and another one to detect the mouth.
/// A stack containing the status of the mouth during the frames is useful to generate opened and closed mouth event.
/// The ratio of the mouth is defined by its contours. A Canny filter is used to give emphasis to the mouth borders. The biggest contour inside the mouth area defines a square, which is used to calculate the ratio
/// </summary>
public class FaceDetector : AbstractFrameHandler {
	
	[SerializeField]
	FaceAction faceAction;						// Interface to execute actions with unity GameObjects

	VideoWriter writer;							// Writes the frames into videos (or images)

	// Classifiers
	CascadeClassifier _cascadeFaceClassifier;	// Detects faces
	CascadeClassifier _cascadeMouthClassifier;	// Detects mouths

	string imNameOrig = "Image";				// Name of the window to show the images (ignored since we show the image into a unity scene)
	string projPath = "C:\\Users\\Edwyn Luis\\Documents\\Lyon\\gamagora\\vision\\";	// Path to the project [Change your path]
	int imSize = 400;							// Dimension of the window to show the images (used to redimension the image, in order to reduce the size of the image and increase preformance)

	// As the mouth detector does not detect the mouth every frame, we store the last position of the mouth, in order to use it when the mouth is not detected;
	Rectangle lastMouth;
    
	// A stack containing the status of the mouth during the frames. Useful to generate opened and closed mouth event
	/*
	 * An event of opened or closed mouth is obtained when the stack has a certain number of Closed or Opened values.
	 * The stack stores only elements that have the same values (all Closed or Opened mouth).
	 * The stack is cleared when a value is not according to the inserted elements.
	*/
	int EventTriggerAmount = 3;				// amount of values needed to trigger an event
	Stack<bool> closedStack;				// a stack containing the last status (true: closed; false: opened)
	bool stackStatusClosed;					// stores the current status of values in the stack

	// Use this for initialization
	void Start () {
		// Initializing writer : file destination
		//writer = new VideoWriter(projPath+"result.avi", VideoWriter.Fourcc('M','P','4','2'), 20, new Size(webCam.Width, webCam.Height), true);

		// Initializing Classifiers
			// Face
		_cascadeFaceClassifier = new CascadeClassifier( Application.dataPath+"\\Resources\\haarcascade_frontalface_alt.xml");
			// Mouth
		_cascadeMouthClassifier = new CascadeClassifier( Application.dataPath+"\\Resources\\haarcascade_mcs_mouth.xml");

		// Initializing Auxiliar variables
		closedStack = new Stack<bool>();
		stackStatusClosed = true;
		lastMouth = Rectangle.Empty;
	}
	
	// Update is called once per frame
	void Update () {}

	// Handling camera frame
	public override Mat handleFrame (Mat image){
		if (image != null)
		{
			Mat imageOrig = image.Clone();
            // Reducing the size of the image in order to increase the performance
            CvInvoke.Resize(imageOrig, imageOrig, new Size(imSize, imSize * imageOrig.Height / imageOrig.Width));
            // Image is originally inverted : flipping
            CvInvoke.Flip(imageOrig, imageOrig, FlipType.Horizontal);

            // Original image (MAT) into image format
			Image<Bgr,System.Byte> imageFrame = imageOrig.ToImage<Bgr,System.Byte>();

			// Face Detection
			Rectangle selectedFace = detectFace( imageFrame );

			// Mouth Detection
			Rectangle selectedMouth = detectMouth( imageFrame, selectedFace );

			// if mouth was not detected, use the position of last mouth
			if( selectedMouth.IsEmpty ){
				if( !lastMouth.IsEmpty ){
					selectedMouth = adjustMouthToImage( lastMouth, imageOrig );
				}
				else{
					// do anything
					return imageOrig;
				}
			}
			else{
				lastMouth = selectedMouth;
			}

			imageFrame.Draw(selectedFace, new Bgr(System.Drawing.Color.BurlyWood), 3);
			imageFrame.Draw(selectedMouth, new Bgr(System.Drawing.Color.Aqua), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them

			if( !selectedMouth.IsEmpty ){
				
				float ratio = mouthRatio( detectMouthContours( imageOrig, selectedMouth ) );
				if( ratio > 0.05 ){
					// Check Contours Ratio (Width x Height) to determines if the mouth is opened or closed
					UpdateRatioStack( ratio );
				}
			}
				
			imageOrig = imageFrame.Mat;

			//CvInvoke.Imshow(imNameOrig, image);

			// Storing
			//writer.Write(imageOrig);

			return imageOrig;
		}
		return image;
	}

	// Use the Classifier to detect the faces
	Rectangle detectFace( Image<Bgr,System.Byte> image ){
		if( _cascadeFaceClassifier != null ){

			// Dimensions for face recognition
			int MinFaceSize = 50;
			int MaxFaceSize = 300;

			// Classifier
			Rectangle[] faces = _cascadeFaceClassifier.DetectMultiScale(image, 1.1, 10, new Size( MinFaceSize, MinFaceSize ), new Size( MaxFaceSize, MaxFaceSize ));
			if( faces.Length > 0 ){
				return faces[0];
			}
		}
		return Rectangle.Empty;
	}

	// Use the Classifier to detect the mouths
	// Consider only the mouths that are inside the face
	Rectangle detectMouth( Image<Bgr,System.Byte> image, Rectangle face ){
		if( _cascadeMouthClassifier != null ){

			// Dimensions for mouth recognition
			int MinMouthSize = 10;
			int MaxMouthSize = 150;

			// Classifier
			Rectangle[] mouths = _cascadeMouthClassifier.DetectMultiScale(image, 1.1, 10, new Size( MinMouthSize, MinMouthSize ), new Size( MaxMouthSize, MaxMouthSize ));

			// Stores the selected mouth
			Rectangle selectedMouth = Rectangle.Empty;
			// Choose the mouth which is under the 1/3 of the face; the biggest area
			foreach (var mouth in mouths)
			{
				float mouthCenterY = mouth.Top + mouth.Height/2f;
				float mouthArea = mouth.Height * mouth.Width;

				if( !face.IsEmpty ){
					if( mouthCenterY > face.Top && mouthCenterY < face.Bottom
					&& mouthCenterY > face.Top + 2*face.Height/3f ){
						// Mouth Detected
						if (selectedMouth.IsEmpty || selectedMouth.Height*selectedMouth.Width < mouthArea){
							// Choose mouth
							selectedMouth = mouth;
						}
					}
				}
			}
			return selectedMouth;
		}
		return Rectangle.Empty;
	}

	Rectangle adjustMouthToImage( Rectangle _mouth, Mat image ){
		Rectangle newMouth = _mouth;

		// increase area of the mouth (open mouths are not well detected)
		newMouth.X -= Mathf.FloorToInt( newMouth.Width*0.1f );
		newMouth.Y += Mathf.FloorToInt( newMouth.Height*0.1f );
		newMouth.Height += Mathf.FloorToInt( newMouth.Height*0.2f );
		newMouth.Width += Mathf.FloorToInt( newMouth.Width*0.2f );

		int newX_min = Mathf.Max( 0, Mathf.Min( image.Width-1, newMouth.X ) );
		int newX_max = Mathf.Min( image.Width-1, newMouth.X+newMouth.Width );
		int newY_min = Mathf.Max( 0, Mathf.Min( image.Height-1, newMouth.Y ) );
		int newY_max = Mathf.Min( image.Height-1, newMouth.Y+newMouth.Height );

		newMouth.X = newX_min;
		newMouth.Y = newY_min;
		newMouth.Width = newX_max - newX_min;
		newMouth.Height = newY_max - newY_min;

		return newMouth;
	}

	// mouth contour is the biggest detected in the mouth area
	VectorOfPoint detectMouthContours( Mat image, Rectangle mouth, Image<Bgr,System.Byte> imageToDraw = null ){
		// Cut the mouth from the image and generate a BW version
		Mat mouthImage = new Mat( image, mouth );
		Mat mouthImageBW = new Mat();
		CvInvoke.CvtColor( mouthImage, mouthImageBW, ColorConversion.Bgr2Gray );
		// Apply bluring : failed
		// CvInvoke.GaussianBlur( mouthImageBW, mouthImageBW, new Size(3,3), 0 );

		// Edge Detection : emphasis to the edge in order to facilitate contours detection
		Mat edges = new Mat();
		CvInvoke.Canny( mouthImageBW, edges, 60, 180 );

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
			CvInvoke.DrawContours( image, contours, biggestContourIndex, new MCvScalar( 255,0,0 ), 2 );
			//CvInvoke.Imshow("new", image);
			return biggestContour;
		}
		return null;
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

	float mouthRatio( VectorOfPoint contour ){
		if( contour != null && contour.Size > 0 ){
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

			//		imageOrig = drawPoint( imageOrig, minX, minY, 5, 255, 0, 0 );
			//		imageOrig = drawPoint( imageOrig, maxX, maxY, 5 );

			// width / height
			return ( maxX - minX + 1 )/(float)( maxY - minY + 1 );
		}
		return 0;
	}

	bool isMouthClosed( float ratio ){
		return ratio > 1.7;
	}

	#region Ratio Stack
	void UpdateRatioStack( float ratio ){
		bool currentStatus = isMouthClosed( ratio );

		if( closedStack.Count == 0 || closedStack.Peek() == currentStatus ){
			closedStack.Push( currentStatus );

			if( stackStatusClosed != currentStatus && closedStack.Count == EventTriggerAmount ){
				if( currentStatus ){
					// Closed Mouth
					closedMouthEvent();
					stackStatusClosed = true;
				}
				else{
					// Opened Mouth
					openedMouthEvent();
					stackStatusClosed = false;
				}
			}
		}
		// the stack must be cleared because a different mouvement was started
		else{
			closedStack.Clear();
			closedStack.Push( currentStatus );
		}
	}
	#endregion

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