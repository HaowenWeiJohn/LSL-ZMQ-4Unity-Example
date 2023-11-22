using LSL;
using NetMQ;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZMQPublisherController : ZMQPublisherInterface
{
    // Start is called before the first frame update

    public string tcpAddress = "tcp://localhost:5555";
    public string topicName = "unity_zmq_my_topic_name";
    public float nominalSamplingRate = 100.0f;

    public int channelNum = 8;

    float start_time;
    public float sent_sample = 0.0f;

    void Start()
    {
        AsyncIO.ForceDotNet.Force();
        InitSocket(tcpAddress);
    }

    // Update is called once per frame
    void Update()
    {

        float elapsed_time = Time.time - start_time;
        int required_sample = (int)(elapsed_time * nominalSamplingRate) - (int)sent_sample;

        for (int i = 0; i < required_sample; i++)
        {
            // create a random array with the same length as channelNum
            float[] randomArray = new float[channelNum];
            for (int j = 0; j < channelNum; j++)
            {
                randomArray[j] = Random.Range(0.0f, 1.0f);
            }
            // send the random array

            // randomArray to byte array
            byte[] randomArrayBytes = new byte[randomArray.Length * sizeof(float)];
            System.Buffer.BlockCopy(randomArray, 0, randomArrayBytes, 0, randomArrayBytes.Length);
            socket.SendMoreFrame(topicName).SendFrame(randomArrayBytes);


        }
        sent_sample += required_sample;

    }

    public void OnApplicationQuit()
    {
        socket.Dispose();
        NetMQConfig.Cleanup();
    }



}
