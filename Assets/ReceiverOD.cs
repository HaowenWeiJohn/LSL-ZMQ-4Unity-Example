using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;


public class ReceiverOD
{
    private readonly Thread receiveThread;
    private bool running;
    private RequestSocket socket;
    public ReceiverOD(string address)
    {
        receiveThread = new Thread((object callback) =>
        {
            using (socket = new RequestSocket())
            {
                socket.Connect(address);

                while (running)
                {
                    if (!socket.IsDisposed)
                    {
                        socket.SendFrameEmpty();
                        try
                        {
                            string message = socket.ReceiveFrameString();
                            DataOD data = JsonUtility.FromJson<DataOD>(message);
                            ((Action<DataOD>)callback)(data);
                        }
                        catch (TerminatingException)
                        {
                            Debug.Log("ZMQ context closed.");
                            return;
                        }
                        //Debug.Log("Send request successful");
                    }
                }
            }
        });
    }

    public void Start(Action<DataOD> callback)
    {
        running = true;
        receiveThread.Start(callback);
    }

    public void Stop()
    {
        running = false;
        socket.Dispose();
        receiveThread.Abort();
    }
}
