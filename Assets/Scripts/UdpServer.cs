using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpServer : MonoBehaviour
{
    public int port = 9050;
    UdpClient udp;
    bool running = false;
    Thread recvThread;

    public void StartServer()
    {
        udp = new UdpClient(port);
        running = true;
        recvThread = new Thread(ReceiveLoop) { IsBackground = true };
        recvThread.Start();
        MainThreadDispatcher.Enqueue(() => Debug.Log("UDP server started on port " + port));
    }

    void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (running)
            {
                byte[] data = udp.Receive(ref remoteEP); // blocking
                string msg = Encoding.ASCII.GetString(data);
                MainThreadDispatcher.Enqueue(() => Debug.Log($"UDP Received from {remoteEP}: {msg}"));
                // responder con ping
                byte[] outb = Encoding.ASCII.GetBytes("ping");
                udp.Send(outb, outb.Length, remoteEP);
            }
        }
        catch (Exception e) { MainThreadDispatcher.Enqueue(() => Debug.Log("UDP server error: " + e.Message)); }
    }

    public void StopServer()
    {
        running = false;
        udp?.Close();
    }

    void OnApplicationQuit() { StopServer(); }
}
