using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TcpServer : MonoBehaviour
{
    public int port = 9050;
    TcpListener listener;
    volatile bool running = false;
    List<TcpClient> clients = new List<TcpClient>();
    Thread listenThread;

    public void StartServer()
    {
        if (running) return;
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            running = true;
            listenThread = new Thread(ListenLoop) { IsBackground = true };
            listenThread.Start();

            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log("TCP server started on port " + port);
                var ui = FindFirstObjectByType<NetworkUI_TMPro>();
                if (ui != null) ui.AppendLog($"[Server] TCP server started on {GetLocalIpString()}:{port}");
            });
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError("StartServer error: " + e.Message));
        }
    }

    void ListenLoop()
    {
        try
        {
            while (running)
            {
                TcpClient client = listener.AcceptTcpClient();
                lock (clients) clients.Add(client);
                var remote = client.Client.RemoteEndPoint.ToString();
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log("Client connected: " + remote);
                    var ui = FindFirstObjectByType<NetworkUI_TMPro>();
                    if (ui != null) ui.AppendLog($"[Server] Client connected: {remote}");
                });

                Thread clientThread = new Thread(() => ClientLoop(client)) { IsBackground = true };
                clientThread.Start();
            }
        }
        catch (SocketException se)
        {
            // listener stopped or socket error
            MainThreadDispatcher.Enqueue(() => Debug.LogWarning("ListenLoop stopped: " + se.Message));
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError("Listen error: " + e.Message));
        }
    }

    void ClientLoop(TcpClient client)
    {
        NetworkStream stream = null;
        try
        {
            stream = client.GetStream();
            byte[] buffer = new byte[1024];
            while (running && client.Connected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                string msg = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                // Log to console + UI
                MainThreadDispatcher.Enqueue(() =>
                {
                    Debug.Log("TCP Received: " + msg);
                    var ui = FindFirstObjectByType<NetworkUI_TMPro>();
                    if (ui != null) ui.AppendLog("[Server] " + msg);
                });
                                
                try
                {
                    byte[] outb = Encoding.ASCII.GetBytes("ping");
                    stream.Write(outb, 0, outb.Length);
                }
                catch (Exception writeEx)
                {
                    MainThreadDispatcher.Enqueue(() => Debug.LogWarning("Write to client failed: " + writeEx.Message));
                }
            }
        }
        catch (Exception e)
        {
            MainThreadDispatcher.Enqueue(() => Debug.LogError("Client loop error: " + e.Message));
        }
        finally
        {
            try { client.Close(); } catch { }
            lock (clients) { clients.Remove(client); }
            MainThreadDispatcher.Enqueue(() =>
            {
                Debug.Log("Client disconnected");
                var ui = FindFirstObjectByType<NetworkUI_TMPro>();
                if (ui != null) ui.AppendLog("[Server] Client disconnected");
            });
        }
    }

    public void Broadcast(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        lock (clients)
        {
            foreach (var c in clients.ToArray())
            {
                try
                {
                    if (c != null && c.Connected)
                    {
                        var s = c.GetStream();
                        if (s != null && s.CanWrite)
                        {
                            s.Write(data, 0, data.Length);
                        }
                    }
                }
                catch (Exception e)
                {
                    MainThreadDispatcher.Enqueue(() => Debug.LogWarning("Broadcast error to a client: " + e.Message));
                }
            }
        }
        MainThreadDispatcher.Enqueue(() =>
        {
            var ui = FindFirstObjectByType<NetworkUI_TMPro>();
            if (ui != null) ui.AppendLog("[Server] Broadcast: " + message);
        });
    }

    public void StopServer()
    {
        running = false;
        try
        {
            listener?.Stop();
        }
        catch (Exception e)
        {
            Debug.LogWarning("StopServer listener stop error: " + e.Message);
        }

        lock (clients)
        {
            foreach (var c in clients) try { c.Close(); } catch { }
            clients.Clear();
        }

        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log("TCP server stopped");
            var ui = FindFirstObjectByType<NetworkUI_TMPro>();
            if (ui != null) ui.AppendLog("[Server] Stopped");
        });
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    string GetLocalIpString()
    {
        // localhost for local testing
        return "127.0.0.1";
    }
}
