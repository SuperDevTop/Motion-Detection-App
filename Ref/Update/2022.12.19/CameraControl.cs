using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.VideoioModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Video;
using Rect = UnityEngine.Rect;

public class CameraControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public GameObject mainUI;
    public GameObject alertText;
    //public Image bulb;
    public RawImage cameraView;
    public RawImage detectionView;
    public Text pathText;
    public Text valueText;
    public WebCamTexture webcamTexture;
    public VideoPlayer video;

    Texture2D texture;
    Point point1;
    Point point2;
    VideoWriter videoWriter;
    VideoWriter videoWriter1;
    int zoneHeight;
    int zoneWidth;
    public int index;
    public int playIndex;
    bool isDraw;
    bool isSetZone;
    bool isRecord;
    public bool isDelay;
    public bool isCreatedVideo;

    // Start is called before the first frame update
    void Start()
    {
        playIndex = 1;
        //obtain cameras avialable
        WebCamDevice[] cam_devices = WebCamTexture.devices;
        //create camera texture
        webcamTexture = new WebCamTexture(cam_devices[0].name, Screen.width, Screen.height, 30);
        //set raw image texture to obtain feed from camera texture
        detectionView.texture = webcamTexture;
        detectionView.material.mainTexture = webcamTexture;
        //start camera
        webcamTexture.Play();
        //start coroutine
        StartCoroutine(MotionDetection());

        //Size size = new Size(2560, 1440);
        //videoWriter1 = new VideoWriter(Application.streamingAssetsPath + "/" + "1.mp4", VideoWriter.fourcc('m', 'p', '4', 'v'), 24, size);

        point1 = new Point(0, 0);
        point2 = new Point(0, 0);        

        //playIndex = PlayerPrefs.GetInt("Index") - 1;
    }

    // Update is called once per frame
    void Update()
    {      
        mainUI.transform.localScale = new Vector3(Screen.width / 2532f, Screen.height / 1170f, 1f);
    }

    Mat DiffImage(Mat t0, Mat t1, Mat t2)
    {
        Mat d1 = new Mat();
        Core.absdiff(t2, t1, d1);
        Mat d2 = new Mat();
        Core.absdiff(t1, t0, d2);
        Mat diff = new Mat();
        Core.bitwise_and(d1, d2, diff);
        return diff;
    }

    private IEnumerator MotionDetection()
    {
        while (true)
        {           
            if (isDraw)
            {
                Mat t4 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t4);
                //create Three Mats
                Mat t0 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t0); //obtain fram from webcam
                yield return new WaitForSeconds(0.04F);// wait for 0.04s
                yield return new WaitForEndOfFrame();// wait till end of frame
                Mat t1 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t1);
                yield return new WaitForSeconds(0.04F);
                yield return new WaitForEndOfFrame();
                Mat t2 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t2);
                yield return new WaitForSeconds(0.04F);
                yield return new WaitForEndOfFrame();
                //Change color to greyscale
                Imgproc.cvtColor(t0, t0, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor(t1, t1, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor(t2, t2, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor(t4, t4, Imgproc.COLOR_RGBA2RGB);
                //obtain difference in frames
                Mat subInterest = new Mat();
                subInterest = t4.submat((int)point1.y, (int)point2.y, (int)point1.x, (int)point2.x);
                Mat final = new Mat();
                final = DiffImage(t0, t1, t2);
                OpenCVForUnity.CoreModule.Rect rect = new OpenCVForUnity.CoreModule.Rect((int)point1.x, (int)point1.y, zoneWidth, zoneHeight);

                // My code
                Mat interest = final.submat((int)point1.y, (int)point2.y, (int)point1.x, (int)point2.x);
                int countNoneZero = Core.countNonZero(interest);

                //print("Countnonzero in ROI:  " + countNoneZero);

                //

                //set final Mat to texture of raw image
                Texture2D texture = new Texture2D(final.cols(), final.rows(), TextureFormat.RGBA32, false);
                try
                {
                    Utils.matToTexture2D(final, texture);
                    detectionView.texture = texture;
                }
                catch (Exception)
                {
                }
                //change bulb alpha value

                Byte value = 0;
                try
                {
                    value = Convert.ToByte(Core.countNonZero(interest) / 1000);
                    //print("!!!");
                }
                catch (OverflowException)
                {
                    value = 255;
                }

                print(value);
                valueText.text = "" + value;
                valueText.color = Color.blue;

                if (value != 0 && isRecord)
                {
                    //pathText.text = Application.absoluteURL + "/" + index + ".mp4";
                    alertText.SetActive(false);
                    index++;
                    ScreenCapture.CaptureScreenshot(Application.absoluteURL + "/" + index + ".mp4", 2);
                }
                else if (value != 0)
                {
                    valueText.text = "" + value;
                    valueText.color = Color.blue;
                    alertText.SetActive(true);
                    StartCoroutine(DelayRecording());
                }              
            }
            else
            {
                Mat t4 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t4);
                //create Three Mats
                Mat t0 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t0); //obtain fram from webcam
                yield return new WaitForSeconds(0.04F);// wait for 0.04s
                yield return new WaitForEndOfFrame();// wait till end of frame
                Mat t1 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t1);
                yield return new WaitForSeconds(0.04F);
                yield return new WaitForEndOfFrame();
                Mat t2 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t2);
                yield return new WaitForSeconds(0.04F);
                yield return new WaitForEndOfFrame();
                //Change color to greyscale
                Imgproc.cvtColor(t0, t0, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor(t1, t1, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor(t2, t2, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor(t4, t4, Imgproc.COLOR_RGBA2RGB);
                //obtain difference in frames
                Mat final = new Mat();
                final = DiffImage(t0, t1, t2);
                //set final Mat to texture of raw image
                Texture2D texture = new Texture2D(final.cols(), final.rows(), TextureFormat.RGBA32, false);
                try
                {
                    Utils.matToTexture2D(final, texture);
                    //detectionView.texture = texture;
                }
                catch (Exception)
                {
                }
                //change bulb alpha value
                Byte value = 0;
                try
                {
                    value = Convert.ToByte(Core.countNonZero(final) / 1000);
                    //print("!!!");
                }
                catch (OverflowException)
                {
                    value = 255;
                }

                print(value);
                valueText.text = "" + value;
                valueText.color = Color.red;


                if (value != 0 && isRecord)
                {
                    //pathText.text = Application.absoluteURL + "/" + index + ".mp4";
                    alertText.SetActive(false);
                    index++;
                    ScreenCapture.CaptureScreenshot(Application.absoluteURL + "/" + index + ".mp4", 2);
                }
                else if (value != 0)
                {
                    valueText.text = "" + value;
                    valueText.color = Color.red;
                    alertText.SetActive(true);
                    StartCoroutine(DelayRecording());
                }                
            }            

            if (isDraw)
            {              
                Mat t4 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Imgproc.cvtColor(t4, t4, Imgproc.COLOR_RGBA2RGB);
                Utils.webCamTextureToMat(webcamTexture, t4);
                Imgproc.rectangle(t4, new Point(point1.x / 2560f * Screen.width, point1.y), new Point(point2.x / 2560f * Screen.width, point2.y), new Scalar(0, 255, 0), 5, 8);
                //Imgproc.rectangle(t4, new Point(point1.x / Screen.width * 2560f, point1.y / Screen.height * 1440f), new Point(point2.x / Screen.width * 2560f, point2.y / Screen.height * 1440f), new Scalar(0, 255, 0), 5, 8);
                texture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
                Utils.matToTexture2D(t4, texture);
                detectionView.texture = texture;
                //videoWriter1.write(t4);

                //OpenCVForUnity.CoreModule.RotatedRect();                
            }
        }
    }

    public void SelectZone()
    {
        point1 = new Point(0, 0);
        point2 = new Point(0, 0);
        isDraw = false;
        isSetZone = true;
        isCreatedVideo = false;
    }

    public void RecordClick()
    {
        isRecord = true;
    }

    public void StopRecordClick()
    {
        isRecord = false;
        isDelay = false;
    }   

    public void SwitchToBackCam()
    {
        webcamTexture.Stop();
        WebCamDevice[] cam_devices = WebCamTexture.devices;

        foreach (var device in cam_devices)
        {
            print(device.name);

            if (device.isFrontFacing)
            {                
            }
            else
            {
                webcamTexture = new WebCamTexture(device.name, Screen.width, Screen.height, 30);
            }
        }
       
        detectionView.texture = webcamTexture;
        detectionView.material.mainTexture = webcamTexture;
        //start camera
        webcamTexture.Play();
        //start coroutine
        StartCoroutine(MotionDetection());
    }

    public void SwitchToFrontCam()
    {
        webcamTexture.Stop();
        WebCamDevice[] cam_devices = WebCamTexture.devices;

        foreach (var device in cam_devices)
        {
            print(device.name);

            if (device.isFrontFacing)
            {
                print("!!!!!!!!");
                webcamTexture = new WebCamTexture(device.name, Screen.width, Screen.height, 30);
            }           
        }

        detectionView.texture = webcamTexture;
        detectionView.material.mainTexture = webcamTexture;
        //start camera
        webcamTexture.Play();
        //start coroutine
        StartCoroutine(MotionDetection());
    }

    IEnumerator DelayRecording()
    {
        yield return new WaitForSeconds(1f);
        alertText.SetActive(false);
        isDelay = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isSetZone)
        {
            if (point1 == new Point(0, 0))
            {
                //point1 = new Point(Input.mousePosition.x, (Screen.height - Input.mousePosition.y) * 1430f/1170f);
                //point1 = new Point((Input.mousePosition.x / Screen.width) * Screen.height * (1080f / Screen.height), (Screen.height - Input.mousePosition.y) * (1080f / Screen.height));
                point1 = new Point(Input.mousePosition.x, (Screen.height - Input.mousePosition.y));
                //pathText.text = "" + Input.mousePosition.y;
                pathText.text = "" + Input.mousePosition.x + ", " + Input.mousePosition.y;
            }
            else if (point1 != new Point(0, 0) && point2 == new Point(0, 0))
            {
                //point2 = new Point(Input.mousePosition.x, (Screen.height - Input.mousePosition.y) * 1430f / 1170f);
                //point2 = new Point((Input.mousePosition.x / Screen.width) * Screen.height * (1080f/Screen.height), (Screen.height - Input.mousePosition.y) * (1080f / Screen.height));
                point2 = new Point(Input.mousePosition.x, (Screen.height - Input.mousePosition.y));
                //pathText.text = "" + Input.mousePosition.y;
                pathText.text = "" + Input.mousePosition.x + ", " + Input.mousePosition.y;
                isDraw = true;
                zoneHeight = (int)Math.Abs(point1.y - point2.y);
                zoneWidth = (int)Math.Abs(point1.x - point2.y);               
            }          
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isSetZone)
        {        
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        
    }

    public void PlayClick()
    {
        if (playIndex > 0)
        {

            video.url = Application.absoluteURL + "/" + playIndex + ".mp4";

            video.Play();
        }        
    }

    public void StopClick()
    {
        if (playIndex > 0)
        {

            video.url = Application.absoluteURL + "/" + playIndex + ".mp4";

            video.Stop();
        }
    }

    public void NextClick()
    {        
        if (playIndex <= 1)
        {
            //playIndex = PlayerPrefs.GetInt("Index") - 1;
            playIndex = index - 1;
        }
        else
        {
            playIndex--;
        }
    }
}
