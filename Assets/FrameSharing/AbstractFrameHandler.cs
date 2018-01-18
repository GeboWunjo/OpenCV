using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ZedGraph;
using System.Drawing;

public abstract class AbstractFrameHandler : MonoBehaviour {
	/// <summary>
	/// Receives a frame and manipulates it
	/// </summary>
	/// <returns>The resulting image</returns>
	/// <param name="frame">Frame to be manipulated</param>
	abstract public Mat handleFrame( Mat frame );
}
