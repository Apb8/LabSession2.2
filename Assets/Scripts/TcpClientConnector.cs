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
    volatile bool running = false;

    public bool IsConnected => client != null && client.Connected;

    public void Connect()
    {
        if (running) return;
        running = true;
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
            MainThreadDispatcher.Enqueue(() => Debug.Log($"Connected to TCP server {serverIp}:{serverPort}"));
            MainThreadDispatcher.Enqueue(() => {
                var ui = FindFirstObjectByType<NetworkUI_TMPro>();
                if (ui != null) ui.AppendLog($"[Client] Conectado a {serverIp}:{serverPort} (TCP)");
            });
            ReceiveLoop();
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError("Connect error: " + e.Message));
            running = false;
            try { client?.Close(); } catch { }
            client = null;
            stream = null;
        }
    }

    void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];
        try
        {
            while (running && client != null && client.Connected)
            {
                if (stream == null)
                {
                    Thread.Sleep(10);
                    continue;
                }

                int bytesRead = stream.Read(buffer, 0, buffer.Length); // blocking
                if (bytesRead == 0) break;
                string msg = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                MainThreadDispatcher.Enqueue(() => {
                    Debug.Log("TCP Client Received: " + msg);
                    var ui = FindFirstObjectByType<NetworkUI_TMPro>();
                    if (ui != null) ui.AppendLog("[Client] " + msg);
                });
            }
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError("ReceiveLoop error: " + e.Message));
        }
        finally
        {
            Disconnect();
        }
    }

    public void Send(string message)
    {
        try
        {
            if (!IsConnected || stream == null)
            {
                MainThreadDispatcher.Enqueue(() => Debug.LogWarning("TCP not connected - can't send"));
                return;
            }

            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError("Send error: " + e.Message));
        }
    }

    public void Disconnect()
    {
        running = false;
        try { stream?.Close(); } catch { }
        try { client?.Close(); } catch { }

        stream = null;
        client = null;

        MainThreadDispatcher.Enqueue(() => {
            Debug.Log("TCP client disconnected");
            var ui = FindFirstObjectByType<NetworkUI_TMPro>();
            if (ui != null) ui.AppendLog("[Client] Disconnected");
        });
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }
}
