using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System.Net;

public class ZMQPublisherInterface : MonoBehaviour
{
    // Start is called before the first frame update

    public PublisherSocket socket;


    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitSocket(string tcpAddress)
    {
        socket = new PublisherSocket(tcpAddress);
    }


}
