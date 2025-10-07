using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpClientConnector : MonoBehaviour
{
    public string serverIp = "127.0.0.1";
    public int serverPort = 9050;

    TcpClient client;
    NetworkStream stream;
    Thread clientThread;

    public void Connect()
    {
        clientThread = new Thread(ConnectThread) { IsBackground = true };
        clientThread.Start();
    }

    void ConnectThread()
    {
        try
        {
            client = new TcpClient();
            client.Connect(serverIp, serverPort); // blocking
            stream = client.GetStream();
            MainThreadDispatcher.Enqueue(() => Debug.Log("Connected to TCP server"));
            ReceiveLoop();
        }
        catch (Exception e) { MainThreadDispatcher.Enqueue(() => Debug.Log("Connect error: " + e.Message)); }
    }

    void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];
        try
        {
            while (client != null && client.Connected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length); // blocking
                if (bytesRead == 0) break;
                string msg = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                MainThreadDispatcher.Enqueue(() => Debug.Log("TCP Client Received: " + msg));
            }
        }
        catch (Exception e) { MainThreadDispatcher.Enqueue(() => Debug.Log("ReceiveLoop error: " + e.Message)); }
        finally { Disconnect(); }
    }

    public void Send(string message)
    {
        try
        {
            if (client != null && client.Connected)
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
            else MainThreadDispatcher.Enqueue(() => Debug.Log("TCP not connected"));
        }
        catch (Exception e) { MainThreadDispatcher.Enqueue(() => Debug.Log("Send error: " + e.Message)); }
    }

    public void Disconnect()
    {
        try { client?.Close(); }
        catch { }
        MainThreadDispatcher.Enqueue(() => Debug.Log("TCP client disconnected"));
    }

    void OnApplicationQuit() { Disconnect(); }
}
