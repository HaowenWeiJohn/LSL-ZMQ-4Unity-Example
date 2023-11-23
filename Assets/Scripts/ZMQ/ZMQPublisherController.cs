using System.Collections;
using UnityEngine;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using System;

public class ZMQPublisherController : MonoBehaviour
{
    [Header("In-game Objects")]
    public Camera captureCamera;  // in your editor, set this to the camera you want to capture

    [Header("Camera Capture Image Size")]
    public int imageWidth = 400;
    public int imageHeight = 400;

    [Header("Runtime Parameters")]
    public float srate = 15f;

    // objects to hold the image data;
    RenderTexture tempRenderColorTexture;
    Texture2D colorImage;

    [Header("Networking Fields")]
    public string tcpAddress = "tcp://localhost:5557";
    public string topicName = "unity_zmq_my_stream_name";
    PublisherSocket socket;

    [Header("Networking Information (View-only)")]
    public long imageCounter = 0;


    private void Start()
    {
        // check if capture camera has been set
        if (captureCamera == null)
        {
            Debug.LogError("CameraCaptureServer: captureCamera is not set. Please set it in the editor.");
            return;
        }

        tempRenderColorTexture = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4
        };

        colorImage = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false, true);

        ForceDotNet.Force();
        socket = new PublisherSocket(tcpAddress);
        StartCoroutine(UploadCapture(1f / srate));
    }

    IEnumerator UploadCapture(float waitTime)
    {
        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            float frameStartTime = Time.realtimeSinceStartup;
            byte[] imageBytes = encodeColorCamera();

            double timestamp = Time.unscaledTime;
            socket.SendMoreFrame(topicName).SendMoreFrame(BitConverter.GetBytes(timestamp)).SendFrame(imageBytes);

            imageCounter++;
            float frameEndTime = Time.realtimeSinceStartup;

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

        return colorImage.GetRawTextureData();
    }

    private void OnDestroy()
    {
        socket.Dispose();
        NetMQConfig.Cleanup();
    }

}