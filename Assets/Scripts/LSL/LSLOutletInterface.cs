using LSL;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSLOutletInterface : MonoBehaviour
{
    // Start is called before the first frame update
    public StreamOutlet streamOutlet;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void InitLSLStreamOutlet(string streamName, string streamType, int channelNum, float nominalSamplingRate, LSL.channel_format_t channelFormat)
    {
        StreamInfo streamInfo = new StreamInfo(

            streamName,
            streamType,
            channelNum,
            nominalSamplingRate,
            channelFormat

            );

        streamOutlet = new StreamOutlet(streamInfo);
    }

    public float[] CreateEventMarkerArrayFloat()
    {
        int channel_count = streamOutlet.info().channel_count();
        float[] zerosArray = new float[channel_count];
        return zerosArray;
    }

    public float[] CreateRandomArray()
    {
        int channel_count = streamOutlet.info().channel_count();
        float[] randomArray = new float[channel_count];
        for (int i = 0; i < channel_count; i++)
        {
            randomArray[i] = Random.Range(0.0f, 1.0f);
        }
        return randomArray;

    }

}
