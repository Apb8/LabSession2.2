using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpClientConnector : MonoBehaviour
{
    public string serverIp = "127.0.0.1";
    public int serverPort = 9050;

    UdpClient udp;
    IPEndPoint serverEP;
    Thread recvThread;
    bool running = false;

    public void StartClient()
    {
        udp = new UdpClient();
        serverEP = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        running = true;
        recvThread = new Thread(ReceiveLoop) { IsBackground = true };
        recvThread.Start();
        MainThreadDispatcher.Enqueue(() => Debug.Log("UDP client started"));
    }

    public void Send(string message)
    {
        try
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            udp.Send(data, data.Length, serverEP);
        }
        catch (Exception e) { MainThreadDispatcher.Enqueue(() => Debug.Log("UDP send error: " + e.Message)); }
    }

    void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (running)
            {
                byte[] data = udp.Receive(ref remoteEP);
                string msg = Encoding.ASCII.GetString(data);
                MainThreadDispatcher.Enqueue(() => Debug.Log($"UDP Received from {remoteEP}: {msg}"));
            }
        }
        catch (Exception e) { MainThreadDispatcher.Enqueue(() => Debug.Log("UDP client recv error: " + e.Message)); }
    }

    public void StopClient()
    {
        running = false;
        udp?.Close();
    }

    void OnApplicationQuit() { StopClient(); }
}
