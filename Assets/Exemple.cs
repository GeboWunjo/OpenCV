using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;
using System.IO;
using System;
using Emgu.CV.Structure;
using Emgu.CV.Util;

public class Exemple : MonoBehaviour {

    public int widthBlur = 0;
    public int heightBlur = 0;
    public Size blurS;

    public int kSize = 17;

    public int widthGaussianBlur = 0;
    public int heightGaussianBlur = 0;
    public Size GaussianBlurS;
    public int sigmaX = 0;
    public int sigmaY = 0;

    public double MinH = 173.8;
    public double MaxH = 179.3;
    public double MinS = 149.4;
    public double MaxS = 255;
    public double MinV = 121.3;
    public double MaxV = 255;


    Hsv seuilBas;
    Hsv seuilHaut;

    Mat image;
    Mat imageBlur;
    Mat imageMedianBlur;
    Mat imageGaussianBlur;

    Mat elemntStruct;

    VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
    VectorOfPoint biggestContours;
    int biggestContoursIndex;
    double biggestContourArea;
    double biggestContourAreaOld = 0;

    bool isFiring;

    public AudioClip hadoken;
    AudioSource audio;

    public GameObject particle;
    public GameObject baguette;

    private CascadeClassifier _frontFacesCascadeClassifier;
    private string _absolutePathToSmileCascadeClassifier = "D:\\Gamagora\\Interface\\Interface\\Assets\\haarcascade_frontalface_alt.xml";
    private Rectangle[] _frontFaces;
    private int MIN_FACE_SIZE = 50;
    private int MAX_FACE_SIZE = 500;
    Quaternion baguetteAngle;

    VideoCapture webcam;
    //VideoWriter writer;

    // Use this for initialization
    void Start() {
        webcam = new VideoCapture(0);
        //webcam.ImageGrabbed += new EventHandler(_handleWebcamQueryFrame);
        isFiring = false;
        audio = GetComponent<AudioSource>();
        baguetteAngle = baguette.transform.rotation;

        //testé avec .avi mais des erreurs lors de la sauvegarde
        //writer = new VideoWriter("D:/test.mp4", 30, new Size(300,300),true);
    }

    // Update is called once per frame
    void Update() {
        ColorObjectDetection();
        //FaceDetection();        
    }

    private void OnDestroy()
    {
        CvInvoke.DestroyAllWindows();
    }

    private void FaceDetection()
    {
        if (webcam.IsOpened)
        {
            webcam.Grab();
        }
    }

    private void _handleWebcamQueryFrame(object sender, EventArgs e)
    {
        if (webcam.IsOpened)
        {
            Mat image = new Mat();
            Mat imageGray = new Mat();
            webcam.Retrieve(image);
            _frontFacesCascadeClassifier = new CascadeClassifier(_absolutePathToSmileCascadeClassifier);
            _frontFaces = _frontFacesCascadeClassifier.DetectMultiScale(image: image, scaleFactor: 1.1, minNeighbors: 5, minSize: new Size(MIN_FACE_SIZE, MIN_FACE_SIZE), maxSize: new Size(MAX_FACE_SIZE, MAX_FACE_SIZE));
            CvInvoke.CvtColor(image, imageGray, ColorConversion.Bgr2Gray);

            foreach (Rectangle face in _frontFaces) {
                CvInvoke.Rectangle(image, face, new MCvScalar(0, 180, 0), 5);
            }
            CvInvoke.Flip(image, image, FlipType.Horizontal);
            CvInvoke.Imshow("Ma tete", image);
        }

    }

    private void ColorObjectDetection()
    {

        image = webcam.QueryFrame();
        Mat imageBis = webcam.QueryFrame();
        imageBlur = webcam.QueryFrame();
        imageMedianBlur = webcam.QueryFrame();
        imageGaussianBlur = webcam.QueryFrame();

        elemntStruct = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(3, 3), new Point(1, 1));

        seuilBas = new Hsv(MinH, MinS, MinV);
        seuilHaut = new Hsv(MaxH, MaxS, MaxV);

        //CvInvoke.CvtColor(image, image, ColorConversion.Bgr2Gray);
        CvInvoke.CvtColor(image, image, ColorConversion.Bgr2Hsv);
        CvInvoke.MedianBlur(image, image, kSize);
        Image<Hsv, byte> imageHsv = image.ToImage<Hsv, byte>();
        Mat imGray = imageHsv.InRange(seuilBas, seuilHaut).Mat;


        CvInvoke.Erode(imGray, imGray, elemntStruct, new Point(1, 1), 3, BorderType.Default, new MCvScalar());
        CvInvoke.Dilate(imGray, imGray, elemntStruct, new Point(1, 1), 3, BorderType.Default, new MCvScalar());

        CvInvoke.FindContours(imGray, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);

        biggestContourArea = 0;
        biggestContoursIndex = 0;
        for (int i = 0; i < contours.Size; i++)
        {
            if (CvInvoke.ContourArea(contours[i]) > biggestContourArea)
            {
                biggestContours = contours[i];
                biggestContoursIndex = i;
                biggestContourArea = CvInvoke.ContourArea(contours[i]);
            }
        }

        CvInvoke.DrawContours(imGray, contours, biggestContoursIndex, new MCvScalar(150, 150, 0), 5);
        CvInvoke.DrawContours(imageHsv, contours, biggestContoursIndex, new MCvScalar(150, 0, 0), 5);
        CvInvoke.DrawContours(imageBis, contours, biggestContoursIndex, new MCvScalar(150, 0, 0), 5);
        //CvInvoke.Imshow("Seuil", imGray);

        //if ((biggestContourAreaOld != 0 ) && (biggestContourArea > (biggestContourAreaOld - (biggestContourArea/2))))        

        if (!isFiring && biggestContourArea > 30000)
        {
            isFiring = true;
            Debug.Log("Boule de Feu");
            audio.Play();
            GameObject clone = Instantiate(particle, transform.position, transform.rotation);
            baguette.transform.Rotate(new Vector3(80, 0, 0));
            baguette.transform.Rotate(Vector3.down * Time.deltaTime, Space.World);

            Debug.Log(clone.transform.position);
        }
        else if (biggestContourArea < 3000)
        {
            if (baguette.transform.rotation != baguetteAngle)
            {
                baguette.transform.Rotate(new Vector3(-80, 0, 0));
                baguetteAngle = baguette.transform.rotation;
            }
            isFiring = false;
        }
        biggestContourAreaOld = biggestContourArea;

        calculPosBaguette();


        /*CvInvoke.Blur(image, imageBlur, new Size(5, 5), new Point(1,1), BorderType.Default);
        CvInvoke.MedianBlur(image, imageMedianBlur, 17);
        CvInvoke.GaussianBlur(image, imageGaussianBlur, new Size(5, 5), 31,0, BorderType.Default);

        CvInvoke.Imshow("Mon image Blur", imageBlur);
        CvInvoke.Imshow("Mon image Median", imageMedianBlur);
        CvInvoke.Imshow("Mon image Gaussian", imageGaussianBlur);*/
        //CvInvoke.Imshow("Mon image", imageHsv);
        CvInvoke.Flip(imageBis, imageBis, FlipType.Horizontal);
        CvInvoke.Imshow("Flipendo", imageBis);
        //CvInvoke.Resize(image, image, new Size(300, 300));
        //writer.Write(image);
    }

    private void calculPosBaguette(){
        // retrieve the position of the object in the player hand, using the centroid of the biggest contour
        // the position is normalized in camera space (from -1 to +1)
        if(biggestContours != null)
        {
            MCvMoments moments = CvInvoke.Moments(biggestContours);
            float objectPosition_x = (-1.0f + 2.0f * (float)(moments.M10 / moments.M00) / 640);
            float objectPosition_y = (+1.0f - 2.0f * (float)(moments.M01 / moments.M00) / 480);
            if (!float.IsNaN(objectPosition_x))
            {
                Debug.Log(objectPosition_x);
                baguette.transform.position = new Vector3(-objectPosition_x, baguette.transform.position.y, baguette.transform.position.z);
            }                
        }              
    }
}
