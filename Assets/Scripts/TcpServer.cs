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
    bool running = false;
    List<TcpClient> clients = new List<TcpClient>();

    Thread listenThread;

    public void StartServer()
    {
        if (running) return;
        running = true;
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        listenThread = new Thread(ListenLoop) { IsBackground = true };
        listenThread.Start();
        MainThreadDispatcher.Enqueue(() => Debug.Log("TCP server started on port " + port));
    }

    void ListenLoop()
    {
        try
        {
            while (running)
            {
                TcpClient client = listener.AcceptTcpClient(); // blocking
                lock (clients) clients.Add(client);
                var remote = client.Client.RemoteEndPoint.ToString();
                MainThreadDispatcher.Enqueue(() => Debug.Log("Client connected: " + remote));
                Thread clientThread = new Thread(() => ClientLoop(client)) { IsBackground = true };
                clientThread.Start();
            }
        }
        catch (Exception e) { MainThreadDispatcher.Enqueue(() => Debug.Log("Listen error: " + e.Message)); }
    }

    void ClientLoop(TcpClient client)
    {
        var stream = client.GetStream();
        byte[] buffer = new byte[1024];
        try
        {
            while (running && client.Connected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length); // blocking
                if (bytesRead == 0) break;
                string msg = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                MainThreadDispatcher.Enqueue(() => Debug.Log("TCP Received: " + msg));
                // responder con "ping"
                byte[] outb = Encoding.ASCII.GetBytes("ping");
                stream.Write(outb, 0, outb.Length);
            }
        }
        catch (Exception e) { MainThreadDispatcher.Enqueue(() => Debug.Log("Client loop error: " + e.Message)); }
        finally
        {
            client.Close();
            lock (clients) clients.Remove(client);
            MainThreadDispatcher.Enqueue(() => Debug.Log("Client disconnected"));
        }
    }

    public void StopServer()
    {
        running = false;
        try { listener?.Stop(); }
        catch { }
        lock (clients) { foreach (var c in clients) c.Close(); clients.Clear(); }
    }

    void OnApplicationQuit() { StopServer(); }
}
