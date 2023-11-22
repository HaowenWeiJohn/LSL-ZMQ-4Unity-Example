using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LSL;

public class LSLOutletController : MonoBehaviour
{
    // Start is called before the first frame update
    public StreamOutlet streamOutlet;

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

        StreamInfo streamInfo = new StreamInfo(
                                                streamName,
                                                streamType,
                                                channelNum,
                                                nominalSamplingRate,
                                                channelFormat
                                                );
        streamOutlet = new StreamOutlet(streamInfo);
    }

    // Update is called once per frame
    void Update()
    {   
        float elapsed_time = Time.time - start_time;
        int required_sample = (int)(elapsed_time * nominalSamplingRate) - (int)sent_sample;

        for (int i = 0; i < required_sample; i++)
        {
            // you can also get the channel count from streamOutlet.info().channel_count()
            float[] randomArray = new float[channelNum];
            for (int j = 0; j < channelNum; j++)
            {
                randomArray[j] = Random.Range(0.0f, 1.0f);
            }
            streamOutlet.push_sample(randomArray);

        }
        sent_sample += required_sample;
    }
}
