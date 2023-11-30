using System;
using System.Collections;
using UnityEngine;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;

using static LSL.LSL;
using System.Linq;

public class CameraCaptureServer : MonoBehaviour
{
    [Header("In-game Objects")]
    public Camera captureCamera;
    //public Camera depthCamera;
    //public GameObject target;


    [Header("Camera Capture Image Size")]
    public int imageWidth = 400;
    public int imageHeight = 400;

    [Header("Runtime Parameters")]
    public float srate = 15f;

    // captured operation objects;
    RenderTexture tempRenderColorTexture;
    RenderTexture tempRenderDepthTexture;
    Texture2D colorImage;
    Texture2D depthImage;

    [Header("Networking Fields")]
    public string tcpAddress = "tcp://localhost:5556";
    public string topicName = "CamCapture";
    public PublisherSocket socket;

    [Header("Networking Information (View-only)")]
    public long imageCounter = 0;
    public float fps = 0;

    private void Start()
    {
        tempRenderColorTexture = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4
        };
        colorImage = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false, true);
        tempRenderDepthTexture = new RenderTexture(imageWidth, imageHeight, 16, RenderTextureFormat.R16) //ARGB32
        {
            antiAliasing = 4
        };
        depthImage = new Texture2D(imageWidth, imageHeight, TextureFormat.R16, false, true);

        ForceDotNet.Force();
        socket = new PublisherSocket(tcpAddress);
        StartCoroutine(UploadCapture2(1f / srate));
    }

    IEnumerator UploadCapture2(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            float frameStartTime = Time.realtimeSinceStartup;
            //byte[][] bytesToSend = Capture();

            socket.SendMoreFrame(topicName).SendFrame(encodeColorCamera());

            //socket.SendMoreFrame(topicName).SendFrame(bytesToSend[1].Concat(bytesToSend[0]).ToArray());
            //socket.SendMoreFrame(topicName).SendMoreFrame(bytesToSend[0]).SendFrame(bytesToSend[1]);

            imageCounter++;
            float frameEndTime = Time.realtimeSinceStartup;
            fps = 1f / (frameEndTime - frameStartTime);
            //Debug.Log(string.Format("Sent camera capture: {0}, FPS: {1}", imageCounter, 1f / (frameEndTime - frameStartTime)));
        }

    }

    IEnumerator UploadCapture(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            float frameStartTime = Time.realtimeSinceStartup;

            #region get the camera capture
            captureCamera.targetTexture = tempRenderColorTexture;
            RenderTexture.active = tempRenderColorTexture;
            captureCamera.Render();

            colorImage.ReadPixels(new Rect(0, 0, colorImage.width, colorImage.height), 0, 0);
            colorImage.Apply();
            #endregion

            CaptureImage(colorImage);

            imageCounter++;
            float frameEndTime = Time.realtimeSinceStartup;
            fps = 1f / (frameEndTime - frameStartTime);
        }
    }

    public byte[] encodeColorCamera()
    {
        //render color camera and save bytes
        captureCamera.targetTexture = tempRenderColorTexture;
        RenderTexture.active = tempRenderColorTexture;
        captureCamera.Render();

        colorImage.ReadPixels(new Rect(0, 0, colorImage.width, colorImage.height), 0, 0);
        colorImage.Apply();

        //return colorImage.GetRawTextureData();
        return colorImage.GetRawTextureData();
    }

    /*public byte[] encodeDepthCamera()
    {
        //render depth camera and save bytes
        depthCamera.targetTexture = tempRenderDepthTexture;
        RenderTexture.active = tempRenderDepthTexture;
        depthCamera.Render();

        depthImage.ReadPixels(new Rect(0, 0, depthImage.width, depthImage.height), 0, 0);
        depthImage.Apply();
        //RenderTexture.active = BACKUP_RT; // or just set it to null.

        Color[] colorSrc = depthImage.GetPixels(0, 0, depthImage.width, depthImage.height);
        depthImage.SetPixels(colorSrc);
        depthImage.Apply(true, false);
        //Debug.Log("Decompressed grayscale: " + (float)Math.Pow(colorSrc[0].grayscale, 4f)*20f);
        RenderTexture.active = null;
        //return depthImage.GetRawTextureData();  
        return depthImage.GetRawTextureData();
    }*/

    public byte[][] Capture()
    {

        byte[][] imageEncoded = new byte[2][];
        //imageEncoded[0] = encodeDepthCamera();
        imageEncoded[1] = encodeColorCamera();

        // record and send timestamp and gaze pixel location
        //Vector3 gazePixelCoordinate = captureCamera.WorldToScreenPoint(target.transform.position);

        return imageEncoded;
    }

    public void CaptureImage(Texture2D image)
    {

        byte[] bytes = image.GetRawTextureData();
        double timestamp = local_clock();
        socket.SendMoreFrame(topicName).SendMoreFrame(BitConverter.GetBytes(timestamp)).SendFrame(bytes);
    }

    private void OnDestroy()
    {
        socket.Dispose();
        NetMQConfig.Cleanup();
    }

}
