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

public class CameraControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public static CameraControl Instance;
    public GameObject mainUI;
    public GameObject alertText;
    public GameObject zone;
    //public Image bulb;
    public GameObject senseSlider;
    public RawImage cameraView;
    public RawImage detectionView;
    public Text pathText;
    public Text valueText;
    public WebCamTexture webcamTexture;
    public VideoPlayer video;

    Texture2D texture;
    Texture2D previrwTexture;
    Point point1;
    Point point2;
    Point point3;
    Point point4;
    public VideoWriter videoWriter;
    VideoCapture capture;
    Mat subInterest;
    Mat previewRgbMat;
    public Mat recordingFrameRgbMat;
    Mat t4;
    public int zoneHeight;
    public int zoneWidth;
    public int index;
    public int playIndex;
    public bool isDraw;
    bool isSetZone;
    bool isRecord;
    bool isTest;
    bool isPlaying;
    public bool isCreatedVideo;
    public bool isProcessing;
    public bool isDetect;
    double fps;   // frame per second
    public FpsMonitor fpsMonitor;
    long prevFrameTickCount;
    long currentFrameTickCount;
    bool shouldUpdateVideoFrame = false;
    bool isDrawRect;
    public const int maxframeCount = 300;
    public Texture2D screenCapture;
    public int frameCount;
    // 3 for isDraw & 100 for full screen
    public int sensetivityIndex;

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {       
        //obtain cameras avialable
        WebCamDevice[] cam_devices = WebCamTexture.devices;
        //create camera texture
        webcamTexture = new WebCamTexture(cam_devices[0].name, Screen.width, Screen.height, 30);
        //set raw image texture to obtain feed from camera texture
        detectionView.texture = webcamTexture;
        detectionView.material.mainTexture = webcamTexture;        

        //start camera
        webcamTexture.Play();
        sensetivityIndex = 300;
        //start coroutine
        StartCoroutine(MotionDetection());        

        point1 = new Point(0, 0);
        point2 = new Point(0, 0);
        point3 = new Point(0, 0);
        point4 = new Point(0, 0);


        fps = 12;
        //Size size = new Size(webcamTexture.width, webcamTexture.height);

        //videoWriter = new VideoWriter();
        //videoWriter.open("/storage/emulated/0/Android/data/demo.avi", VideoWriter.fourcc('M', 'J', 'P', 'G'), 5, size);
    }

    // Update is called once per frame
    void Update()
    {
        mainUI.transform.localScale = new Vector3(Screen.width / 2532f, Screen.height / 1170f, 1f);

        if (isPlaying && shouldUpdateVideoFrame)
        {
            shouldUpdateVideoFrame = false;

            //Loop play
            if (capture.get(Videoio.CAP_PROP_POS_FRAMES) >= capture.get(Videoio.CAP_PROP_FRAME_COUNT))
                    capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

                if (capture.grab())
                {
                    capture.retrieve(previewRgbMat);

                    Imgproc.rectangle(previewRgbMat, new Point(0, 0), new Point(previewRgbMat.cols(), previewRgbMat.rows()), new Scalar(0, 0, 255), 3);

                    Imgproc.cvtColor(previewRgbMat, previewRgbMat, Imgproc.COLOR_BGR2RGB);

                if (fpsMonitor != null)
                {
                    fpsMonitor.Add("CAP_PROP_POS_MSEC", capture.get(Videoio.CAP_PROP_POS_MSEC).ToString());
                    fpsMonitor.Add("CAP_PROP_POS_FRAMES", capture.get(Videoio.CAP_PROP_POS_FRAMES).ToString());
                    fpsMonitor.Add("CAP_PROP_POS_AVI_RATIO", capture.get(Videoio.CAP_PROP_POS_AVI_RATIO).ToString());
                    fpsMonitor.Add("CAP_PROP_FRAME_COUNT", capture.get(Videoio.CAP_PROP_FRAME_COUNT).ToString());
                    int msec = (int)Math.Round(1000.0 * (currentFrameTickCount - prevFrameTickCount) / Core.getTickFrequency());
                    int fps = (int)Math.Round(1000.0 / msec);
                    fpsMonitor.Add("STATE", msec + "ms (" + fps + "fps)");
                }

                Utils.matToTexture2D(previewRgbMat, previrwTexture);
                }          
        }

        if (isProcessing)
        {           
            //videoWriter.write(t4);
        }
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
                t4 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t4);

                // Create Three Mats
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
                //Imgproc.cvtColor(t4, t4, Imgproc.COLOR_RGBA2RGB);

                // Obtain difference in frames
                subInterest = new Mat();

                // Change
                if (point1.y < point2.y)
                {
                    subInterest = t4.submat((int)point1.y, (int)point2.y, (int)(point1.x / Screen.width * webcamTexture.width), (int)(point2.x / Screen.width * webcamTexture.width));
                }
                else
                {
                    subInterest = t4.submat((int)point2.y, (int)point1.y, (int)(point1.x / Screen.width * webcamTexture.width), (int)(point2.x / Screen.width * webcamTexture.width));
                }
                // End

                Mat final = new Mat();
                final = DiffImage(t0, t1, t2);
                //OpenCVForUnity.CoreModule.Rect rect = new OpenCVForUnity.CoreModule.Rect((int)point1.x, (int)point1.y, zoneWidth, zoneHeight);

                // Change
                Mat interest;

                if (point1.y < point2.y)
                {
                    interest = final.submat((int)point1.y, (int)point2.y, (int)(point1.x / Screen.width * webcamTexture.width), (int)(point2.x / Screen.width * webcamTexture.width));
                }
                else
                {
                    interest = final.submat((int)point2.y, (int)point1.y, (int)(point1.x / Screen.width * webcamTexture.width), (int)(point2.x / Screen.width * webcamTexture.width));
                }
                // End

                int countNoneZero = Core.countNonZero(interest);

                // print("Countnonzero in ROI:  " + countNoneZero);

                // Set final Mat to texture of raw image
                //Texture2D texture = new Texture2D(final.cols(), final.rows(), TextureFormat.RGBA32, false);
                //try
                //{
                //    Utils.matToTexture2D(final, texture);
                //}
                //catch (Exception)
                //{
                //}
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

                valueText.text = "" + value;
                valueText.color = Color.blue;
                //(value * Screen.height * Screen.width) / (zoneWidth * zoneHeight)

                print(sensetivityIndex);
                
                if (value > sensetivityIndex && isRecord)
                {
                    if (!videoWriter.isOpened())
                    {
                        Debug.LogError("writer.isOpened() false");
                        videoWriter.release();
                    }

                    screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                    //recordingFrameRgbMat = new Mat(Screen.height, Screen.width, CvType.CV_8UC3);
                    recordingFrameRgbMat = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC3);
                    frameCount = 0;
                    cameraView.gameObject.SetActive(false);
                    isCreatedVideo = false;
                    isProcessing = true;
                    isDetect = true;
                }
                else if (value < sensetivityIndex && isRecord && isDetect)
                {
                    alertText.SetActive(false);
                    isDetect = false;
                    StartCoroutine(DelayRecording());
                }
            }
            else
            {
                //print("else");

                t4 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t4);

                // Create Three Mats
                Mat t0 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t0); //obtain frame from webcam
                yield return new WaitForSeconds(0.04F);  // wait for 0.04s
                yield return new WaitForEndOfFrame();  // wait till end of frame

                Mat t1 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t1);
                yield return new WaitForSeconds(0.04F);
                yield return new WaitForEndOfFrame();

                Mat t2 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
                Utils.webCamTextureToMat(webcamTexture, t2);
                yield return new WaitForSeconds(0.04F);
                yield return new WaitForEndOfFrame();

                // Change color to greyscale
                Imgproc.cvtColor(t0, t0, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor(t1, t1, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor(t2, t2, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor(t4, t4, Imgproc.COLOR_RGBA2RGB);

                // Obtain difference in frames
                Mat final = new Mat();
                final = DiffImage(t0, t1, t2);

                //Set final Mat to texture of raw image
                //Texture2D texture = new Texture2D(final.cols(), final.rows(), TextureFormat.RGBA32, false);
                //try
                //{
                //    Utils.matToTexture2D(final, texture);
                //}
                //catch (Exception)
                //{

                //}

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

                //print(value);
                valueText.text = "" + value;
                valueText.color = Color.red;

                if (value > sensetivityIndex && isRecord)
                {

                    if (!videoWriter.isOpened())
                    {
                        Debug.LogError("writer.isOpened() false");
                        videoWriter.release();
                    }

                    screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                    // recordingFrameRgbMat = new Mat(Screen.height, Screen.width, CvType.CV_8UC3);
                    recordingFrameRgbMat = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC3);
                    frameCount = 0;

                    cameraView.gameObject.SetActive(false);
                    isCreatedVideo = false;
                    isProcessing = true;
                    isDetect = true;
                }
                else if (value < sensetivityIndex && isRecord && isDetect)
                {
                    alertText.SetActive(false);
                    isDetect = false;
                    StartCoroutine(DelayRecording());
                }
            }

            //if (isDraw)
            //{                
            //    Mat t5 = new Mat(webcamTexture.height, webcamTexture.width, CvType.CV_8UC4);
            //    Imgproc.cvtColor(t5, t5, Imgproc.COLOR_RGBA2RGB);
            //    Utils.webCamTextureToMat(webcamTexture, t5);
            //    Imgproc.rectangle(t5, new Point(point1.x / Screen.width * webcamTexture.width, point1.y), new Point(point2.x / Screen.width * webcamTexture.width, point2.y), new Scalar(0, 255, 0), 5, 8);   
            //    texture = new Texture2D(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, false);
            //    Utils.matToTexture2D(t5, texture);
            //    detectionView.texture = texture;                                         
            //}

        }

    }

    public void SelectZone()
    {      
        point1 = new Point(0, 0);
        point2 = new Point(0, 0);
        isDraw = false;
        isSetZone = true;
        isCreatedVideo = false;

        if (isRecord)
        {
            videoWriter.Dispose();
        }

        //StartCoroutine(DelayTest());      
    }

    public void SensetivityChanged()
    {
        sensetivityIndex = 1000 - (int)senseSlider.GetComponent<UnityEngine.UI.Slider>().value;
        //sensetivityIndex = 255 * 5 - (int)senseSlider.GetComponent<UnityEngine.UI.Slider>().value * 5;
        //print((int)senseSlider.GetComponent<UnityEngine.UI.Slider>().value);
    }

    public void RecordClick()
    {              
        isRecord = true;

        index++;
        playIndex = index - 1;
        //Size size = new Size(zoneWidth, zoneHeight);
        Size size = new Size(webcamTexture.width, webcamTexture.height);
        //Size size = new Size(Screen.width, Screen.height);
        videoWriter = new VideoWriter();
        //videoWriter.open("/storage/emulated/0/Android/data/com.Camera.Motion/files/" + index + ".avi", VideoWriter.fourcc('M', 'J', 'P', 'G'), 3, size);
        videoWriter.open(Application.persistentDataPath + "/" + index + ".avi", VideoWriter.fourcc('M', 'J', 'P', 'G'), fps, size);
    }

    public void StopRecordClick()
    {
        if (!isRecord)
            return;

        if (videoWriter != null && !videoWriter.IsDisposed)
            videoWriter.release();

        if (recordingFrameRgbMat != null && !recordingFrameRgbMat.IsDisposed)
            recordingFrameRgbMat.Dispose();

        isProcessing = false;       
        isRecord = false;       
    }   

    public void SwitchToBackCam()
    {
        if (isRecord)
        {
            videoWriter.Dispose();
        }

        webcamTexture.Stop();       
        WebCamDevice[] cam_devices = WebCamTexture.devices;

        foreach (var device in cam_devices)
        {            
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

        // Start camera
        webcamTexture.Play();

        // Start coroutine
        StartCoroutine(MotionDetection());
    }

    public void SwitchToFrontCam()
    {
        if (isRecord)
        {
            videoWriter.Dispose();
        }

        webcamTexture.Stop();    
        WebCamDevice[] cam_devices = WebCamTexture.devices;

        foreach (var device in cam_devices)
        {            
            if (device.isFrontFacing)
            {                
                webcamTexture = new WebCamTexture(device.name, Screen.width, Screen.height, 30);
            }           
        }

        detectionView.texture = webcamTexture;
        detectionView.material.mainTexture = webcamTexture;
        
        //Start camera
        webcamTexture.Play();
        
        //Start coroutine
        StartCoroutine(MotionDetection());
    }

    IEnumerator DelayRecording()
    {
        yield return new WaitForSeconds(2f);
         
        if (isDetect == false)
        {
            isProcessing = false;

            if (isCreatedVideo == false)
            {
                isCreatedVideo = true;
                index++;
                playIndex = index - 1;
                //Size size = new Size(subInterest.width(), subInterest.height());
                Size size = new Size(webcamTexture.width, webcamTexture.height);
                //Size size = new Size(Screen.width, Screen.height);
                videoWriter.Dispose();
                videoWriter = new VideoWriter();
                //videoWriter.open("/storage/emulated/0/Android/data/com.Camera.Motion/files/" + index + ".avi", VideoWriter.fourcc('M', 'J', 'P', 'G'), 3, size);
                videoWriter.open(Application.persistentDataPath + "/" + index + ".avi", VideoWriter.fourcc('M', 'J', 'P', 'G'), fps, size);

                if (playIndex > 0)
                {
                    cameraView.gameObject.SetActive(true);
                    //video.url = "/storage/emulated/0/Android/data/1.avi";
                    //video.url = "/storage/emulated/0/Android/data/com.Camera.Motion/files/" + playIndex + ".avi";
                    //pathText.text = "/storage/emulated/0/Android/data/com.Camera.Motion/files/" + playIndex + ".avi";
                    //video.Play();
                    capture = new VideoCapture();
                    capture.open(Application.persistentDataPath + "/" + playIndex + ".avi");
                    previewRgbMat = new Mat();
                    capture.read(previewRgbMat);

                    int frameWidth = previewRgbMat.cols();
                    int frameHeight = previewRgbMat.rows();
                    previrwTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
                    capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);
                    cameraView.texture = previrwTexture;
                    StartCoroutine("WaitFrameTime");
                    isPlaying = true;
                }
            }
        }
    }

    private IEnumerator WaitFrameTime()
    {
        double videoFPS = (capture.get(Videoio.CAP_PROP_FPS) <= 0) ? 10.0 : capture.get(Videoio.CAP_PROP_FPS);
        float frameTime_sec = (float)(1000.0 / videoFPS / 1000.0);
        WaitForSeconds wait = new WaitForSeconds(frameTime_sec);
        prevFrameTickCount = currentFrameTickCount = Core.getTickCount();

        capture.grab();

        while (true)
        {
            if (isPlaying)
            {
                shouldUpdateVideoFrame = true;

                prevFrameTickCount = currentFrameTickCount;
                currentFrameTickCount = Core.getTickCount();

                yield return wait;
            }
            else
            {
                yield return null;
            }
        }
    }

    IEnumerator DelayTest()
    {
        isTest = true;

        yield return new WaitForSeconds(5f);
        isTest = false;
        videoWriter.Dispose();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isSetZone)
        {
            if (point1 == new Point(0, 0))
            {
                //point1 = new Point(Input.mousePosition.x, (Screen.height - Input.mousePosition.y) * 1430f/1170f);
                //point1 = new Point((Input.mousePosition.x / Screen.width) * Screen.height * (1080f / Screen.height), (Screen.height - Input.mousePosition.y) * (1080f / Screen.height));
                point1 = new Point(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                point3 = new Point(Input.mousePosition.x, Input.mousePosition.y);
                //pathText.text = "" + Input.mousePosition.y;
                pathText.text = "" + Input.mousePosition.x + ", " + Input.mousePosition.y;
            }
            else if (point1 != new Point(0, 0) && point2 == new Point(0, 0))
            {
                //point2 = new Point(Input.mousePosition.x, (Screen.height - Input.mousePosition.y) * 1430f / 1170f);
                //point2 = new Point((Input.mousePosition.x / Screen.width) * Screen.height * (1080f/Screen.height), (Screen.height - Input.mousePosition.y) * (1080f / Screen.height));
                point2 = new Point(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                point4 = new Point(Input.mousePosition.x, Input.mousePosition.y);
                //pathText.text = "" + Input.mousePosition.y;
                pathText.text = "" + Input.mousePosition.x + ", " + Input.mousePosition.y;                
                zoneHeight = (int)Math.Abs((int)point1.y - (int)point2.y);
                zoneWidth = (int)Math.Abs((int)point1.x - (int)point2.x);
                isDraw = true;
                isDrawRect = true;
                zone.SetActive(true);
                zone.transform.position = new Vector3((float)(point3.x + (point4.x - point3.x) / 2), (float)(point3.y + (point4.y - point3.y) / 2), 0);
                zone.GetComponent<RectTransform>().sizeDelta = new Vector2(Math.Abs((float)(point4.x - point3.x) * 2532f / Screen.width), Math.Abs((float)(point4.y - point3.y) * 1170f / Screen.height));
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
            video.url = "/storage/emulated/0/Android/data/com.Camera.Motion/files/" + playIndex + ".avi";
            video.Play();
        }        
    }

    public void StopClick()
    {
        if (playIndex > 0)
        {

            video.url = "/storage/emulated/0/Android/data/com.Camera.Motion/files/" + playIndex + ".avi";
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
