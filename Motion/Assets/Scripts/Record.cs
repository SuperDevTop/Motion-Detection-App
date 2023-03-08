using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.VideoioModule;
using OpenCVForUnityExample;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Video;
using Rect = UnityEngine.Rect;

public class Record : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnPostRender()
    {
        //print("!!!!!!!!!!!");
        if (CameraControl.Instance.isProcessing)
        {
            if (CameraControl.Instance.frameCount >= 300 ||
                CameraControl.Instance.recordingFrameRgbMat.width() != CameraControl.Instance.webcamTexture.width || CameraControl.Instance.recordingFrameRgbMat.height() != CameraControl.Instance.webcamTexture.height)
            {
                return;
            }

            CameraControl.Instance.frameCount++;

            print("in onpostrecord...");
            //// Take screen shot.
            //CameraControl.Instance.screenCapture.ReadPixels(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), 0, 0);
            //CameraControl.Instance.screenCapture.Apply();
            //Utils.texture2DToMat(CameraControl.Instance.screenCapture, CameraControl.Instance.recordingFrameRgbMat);
            //Imgproc.cvtColor(CameraControl.Instance.recordingFrameRgbMat, CameraControl.Instance.recordingFrameRgbMat, Imgproc.COLOR_RGB2BGR);
            CameraControl.Instance.recordingFrameRgbMat = new Mat(CameraControl.Instance.webcamTexture.height, CameraControl.Instance.webcamTexture.width, CvType.CV_8UC4);
            Utils.webCamTextureToMat(CameraControl.Instance.webcamTexture, CameraControl.Instance.recordingFrameRgbMat);
            CameraControl.Instance.videoWriter.write(CameraControl.Instance.recordingFrameRgbMat);
        }
    }
}
