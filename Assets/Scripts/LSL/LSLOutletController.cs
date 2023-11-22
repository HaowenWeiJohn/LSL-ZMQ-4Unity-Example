using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSLOutletController : LSLOutletInterface
{
    // Start is called before the first frame update

    public string streamName = "unity_lsl_my_stream_name";
    public string streamType = "LSL";
    public int channelNum = 8;
    public float nominalSamplingRate = 100.0f;

    public LSL.channel_format_t channelFormat = LSL.channel_format_t.cf_float32;

    
    float start_time;
    public float sent_sample = 0.0f;

    void Start()
    {
        start_time = Time.time;
        InitLSLStreamOutlet(streamName, streamType, channelNum, nominalSamplingRate, channelFormat);

    }

    // Update is called once per frame
    void Update()
    {   
        float elapsed_time = Time.time - start_time;
        int required_sample = (int)(elapsed_time * nominalSamplingRate) - (int)sent_sample;

        for (int i = 0; i < required_sample; i++)
        {
            float[] sample = CreateRandomArray();
            streamOutlet.push_sample(sample);
        }
        sent_sample += required_sample;
    }
}
