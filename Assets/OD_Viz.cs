using AsyncIO;
using NetMQ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ODViz : MonoBehaviour
{
    public int maxNumBoundingBox = 10;

    [Header("In-game Objects")]
    public Camera captureCamera;
    public Camera depthCamera;

    [Header("Game Objects")]
    public GameObject boundingQuad;
    public GameObject wireframeCubeBB;

    [Header("Camera Capture Image Size")]
    public int imageWidth = 400;//1280;
    public int imageHeight = 400;//720;

    [Header("Networking Fields")]
    public string tcpAddress = "tcp://localhost:5556";
    private ReceiverOD receiver;
    private readonly ConcurrentQueue<Action> runOnMainThread = new ConcurrentQueue<Action>();

    public List<XRNodeState> nodeStates;
    private XRNodeState single_xrnode;

    private Vector3 left_eye_pos, right_eye_pos;
    private Quaternion left_eye_rot, right_eye_rot;

    bool doneOnce = false;
    Camera m_MainCamera;
    void getEyeTrackingStates()
    {
        InputTracking.GetNodeStates(nodeStates);

        // Try to get the parameters and place them into the correct variables
        nodeStates[0].TryGetPosition(out left_eye_pos);
        nodeStates[0].TryGetRotation(out left_eye_rot);

        nodeStates[1].TryGetPosition(out right_eye_pos);
        nodeStates[1].TryGetRotation(out right_eye_rot);


        Debug.Log("Left eye position: " + left_eye_pos);
        Debug.Log("Left eye rotation: " + left_eye_rot);
        Debug.Log("Right eye position: " + right_eye_pos);
        Debug.Log("Right eye rotation: " + right_eye_rot);
    }

    void draw2Dbb(ref List<GameObject> boundingBoxes, int i, DataOD d)
    {
        float bb_depth = captureCamera.nearClipPlane;
        imageHeight = captureCamera.pixelHeight;
        imageWidth = captureCamera.pixelWidth;

        Tuple<Vector3, Vector3, Vector3> coords = captureCamDisplay(d, i);

        float xScale = coords.Item3.x - coords.Item2.x; //upperRight - lowerLeft corner
        float yScale = coords.Item3.y - coords.Item2.y;

        boundingBoxes[i].transform.localScale = new Vector3(xScale, yScale, 1);

        //rotate red bbx
        boundingBoxes[i].transform.rotation = depthCamera.transform.rotation;

        boundingBoxes[i].transform.position = coords.Item1;
        boundingBoxes[i].GetComponent<Renderer>().enabled = true;
    }
    void draw3Dbb(ref List<GameObject> boundingBoxes3D, int i, DataOD d)
    {
        float near = depthCamera.nearClipPlane;
        float far = depthCamera.farClipPlane;
        //print(d.minDepth[i] + ", " + d.maxDepth[i] + ", " + d.aveDepth[i]);
        float objDepth = (d.maxDepth[i] - d.minDepth[i]) * 2;
        // float objDepth = (d.maxDepth[i] - d.minDepth[i]) * far;

        Vector3 lowerCorner = new Vector3(d.xs[i] - d.ws[i] / 2, imageHeight - (d.ys[i] - d.hs[i] / 2), d.minDepth[i] * far);
        lowerCorner = depthCamera.ScreenToWorldPoint(lowerCorner);
        lowerCorner += near * depthCamera.transform.forward;

        Vector3 upperCorner = new Vector3(d.xs[i] + d.ws[i] / 2, imageHeight - (d.ys[i] + d.hs[i] / 2), d.minDepth[i] * far);
        upperCorner = depthCamera.ScreenToWorldPoint(upperCorner);
        upperCorner += near * depthCamera.transform.forward;

        float xScale = upperCorner.x - lowerCorner.x;
        float yScale = upperCorner.y - lowerCorner.y;

        //Debug.Log("Depth = " + objDepth);

        boundingBoxes3D[i].transform.localScale = new Vector3(xScale, yScale, objDepth);

        // rotate the bounding box towards the correct direction
        // boundingBoxes3D[i].transform.LookAt(depthCamera.transform.eulerAngles);
        // boundingBoxes3D[i].transform.rotation = depthCamera.transform.rotation;

        Vector3 p = new Vector3(d.xs[i] + d.ws[i] / 2.0f, (imageHeight - d.ys[i]) - d.hs[i] / 2.0f, d.minDepth[i] * far);
        p = depthCamera.ScreenToWorldPoint(p);
        //Debug.Log(p.z);
        p += near * depthCamera.transform.forward;
        p.z += objDepth / 2;

        boundingBoxes3D[i].transform.position = p;
        boundingBoxes3D[i].GetComponent<Renderer>().enabled = true;
    }


    Tuple<Vector3, Vector3, Vector3> captureCamDisplay(DataOD d, int i) //i = bb index
    {
        //Point in 480x480 space converted from OpenCV coordinates

        Vector3 p = new Vector3(d.xs[i] + d.ws[i] / 2.0f, (imageHeight - d.ys[i]) - d.hs[i] / 2.0f, captureCamera.nearClipPlane);

        p = captureCamera.ScreenToWorldPoint(p);

        Vector3 lowerCorner = new Vector3(d.xs[i] - d.ws[i] / 2, imageHeight - (d.ys[i] - d.hs[i] / 2), captureCamera.nearClipPlane);
        lowerCorner = captureCamera.ScreenToWorldPoint(lowerCorner);
        Vector3 upperCorner = new Vector3(d.xs[i] + d.ws[i] / 2, imageHeight - (d.ys[i] + d.hs[i] / 2), captureCamera.nearClipPlane);
        upperCorner = captureCamera.ScreenToWorldPoint(upperCorner);
        return Tuple.Create(p, lowerCorner, upperCorner);
    }


    // Start is called before the first frame update
    void Start()
    {

        m_MainCamera = Camera.main;

        ForceDotNet.Force();

        nodeStates = new List<XRNodeState>();

        // Add left eye node to the nodeStates list, which will be represented by nodeStates[0]
        single_xrnode.nodeType = XRNode.LeftEye;
        nodeStates.Add(single_xrnode);

        // Add right eye node to the nodeStates list, which will be represented by nodeStates[1]
        single_xrnode.nodeType = XRNode.RightEye;
        nodeStates.Add(single_xrnode);

        // create canvas for bounding boxes
        List<GameObject> boundingBoxes = new List<GameObject> { };
        List<GameObject> boundingBoxes3D = new List<GameObject> { };

        for (int i = 0; i < maxNumBoundingBox; i++)
        {
            GameObject bb = Instantiate(boundingQuad, new Vector3(0, 0, 0), Quaternion.identity);
            GameObject bb3D = Instantiate(wireframeCubeBB, new Vector3(0, 0, 0), Quaternion.identity);

            // add a button to the bottom of bb
            // GameObject button = 

            bb.layer = LayerMask.NameToLayer("NotRenderInCaptureCamera");
            bb3D.layer = LayerMask.NameToLayer("NotRenderInCaptureCamera");
            bb.GetComponent<Renderer>().enabled = false;
            bb3D.GetComponent<Renderer>().enabled = false;
            boundingBoxes.Add(bb);
            boundingBoxes3D.Add(bb3D);
        }

        receiver = new ReceiverOD(tcpAddress);
        receiver.Start((DataOD d) => runOnMainThread.Enqueue(() =>
        {
            /*try
            {
                getEyeTrackingStates();
            }
            catch { Debug.Log("Failed to get eye tracking states"); }*/

            for (int i = 0; i < maxNumBoundingBox; i++)
            {
                boundingBoxes[i].GetComponent<Renderer>().enabled = false;
                boundingBoxes3D[i].GetComponent<Renderer>().enabled = false;
            }
            for (int i = 0; i < Math.Min(d.xs.Length, maxNumBoundingBox); i++)
            {
                //draw2Dbb(ref boundingBoxes, i, d);
                draw3Dbb(ref boundingBoxes3D, i, d);
            }

        }
        ));
    }

    // Update is called once per frame
    void Update()
    {
        if (!runOnMainThread.IsEmpty)
        {
            Action action;
            while (runOnMainThread.TryDequeue(out action))
            {
                action.Invoke();
            }
        }
    }

    private void OnDestroy()
    {
        receiver.Stop();
        NetMQConfig.Cleanup();
    }

    //float recoverDepthFrom16Bit(float val)
    //{
    //    float near = depthCamera.nearClipPlane;
    //    float far = depthCamera.farClipPlane;
    //    float max16bitval = 65535;
    //    float min16bitval = 0;

    //    //map from 0-65535 16bit range to compressed 0-1 range which was output as pow(linearZFromNear, k) in UberReplacement.shader
    //    float scale = (1f - 0f) / (max16bitval - min16bitval);
    //    float offset = -min16bitval * scale + 0f;
    //    float compressed = val * scale + offset;

    //    float decompressed = (float) Math.Pow(compressed, 4f); //decompress from 0.25 compression to recover 0-1 linear scale (linearZFromNear) in UberReplacement.shader

    //    //float depth01 = -(decompressed - 1) / (1 + near / far) + near / far; //convert from [0 @ near .. 1 @ far] back to [0 @ eye .. 1 @ far]
    //    //Debug.Log("Recovered depth01: " + depth01);
    //    //map from [0 @ near .. 1 @ far] to world coordinate distance values

    //    return decompressed * far;//depth01*far;
    //}

}
