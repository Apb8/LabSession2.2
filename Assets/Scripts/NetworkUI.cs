using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Reflection;

public class NetworkUI_TMPro : MonoBehaviour
{
    // CONSTANTE fallback si no se provee puerto
    const int DEFAULT_PORT = 9050;

    // COMMON
    public TMP_Text logText;
    public ScrollRect logScrollRect; // asigna el ScrollView (ScrollRect)

    // SERVER UI
    public TMP_InputField serverNameInput;
    public TMP_InputField serverPortInput; // ahora editable en CreateGame
    public TMP_Dropdown serverProtocolDropdown; // 0 = TCP, 1 = UDP
    public Button startServerBtn;
    public Button stopServerBtn;
    public TMP_Text statusTextServer;

    // CLIENT UI
    public TMP_InputField serverIpInput;
    public TMP_InputField clientPortInput; // puerto en JoinGame
    public TMP_InputField playerNameInput;
    public TMP_Dropdown clientProtocolDropdown; // 0 = TCP, 1 = UDP
    public Button connectBtn;
    public Button disconnectBtn;
    public TMP_Text statusTextClient;

    // Messaging (optional)
    public TMP_InputField messageInputServer;
    public Button sendServerBtn;
    public TMP_InputField messageInputClient;
    public Button sendClientBtn;

    // Network scripts
    public TcpServer tcpServer;
    public TcpClientConnector tcpClient;
    public UdpServer udpServer;
    public UdpClientConnector udpClient;

    void Start()
    {
        string scene = SceneManager.GetActiveScene().name.ToLower();
        Debug.Log("NetworkUI: scene = " + scene);

        // Log safety
        if (logText == null) Debug.LogWarning("NetworkUI: logText NO asignado");
        if (logScrollRect == null) Debug.LogWarning("NetworkUI: logScrollRect NO asignado (mejor para scroll del log)");

        // SERVER SCENE ---------------------------------------------------
        bool isServerScene = scene.Contains("server") || scene.Contains("creategame");
        if (isServerScene)
        {
            if (startServerBtn == null) Debug.LogError("NetworkUI: startServerBtn NO asignado (Server scene)");
            if (serverProtocolDropdown == null) Debug.LogWarning("NetworkUI: serverProtocolDropdown NO asignado (Server scene)");
            if (tcpServer == null && udpServer == null) Debug.LogWarning("NetworkUI: tcpServer y udpServer son ambos null (Server)");
            if (stopServerBtn == null) Debug.LogWarning("NetworkUI: stopServerBtn NO asignado (Server scene)");
            if (statusTextServer == null) Debug.LogWarning("NetworkUI: statusTextServer NO asignado (Server scene)");

            // Start server
            if (startServerBtn != null)
            {
                startServerBtn.onClick.AddListener(() =>
                {
                    int proto = serverProtocolDropdown != null ? serverProtocolDropdown.value : 0; // 0 TCP, 1 UDP
                    int port = ParsePort(serverPortInput, DEFAULT_PORT);
                    string sName = serverNameInput != null ? serverNameInput.text : "Server";

                    if (proto == 0) // TCP
                    {
                        if (tcpServer != null)
                        {
                            tcpServer.port = port;
                            tcpServer.StartServer();
                            AppendLog($"[Server] TCP started on {GetLocalIpString()}:{port} (Name: {sName})");
                            SetServerStatus($"Running (TCP) on {port}");
                        }
                        else AppendLog("[Server] TCP server script no asignado");
                    }
                    else // UDP
                    {
                        if (udpServer != null)
                        {
                            udpServer.port = port;
                            udpServer.StartServer();
                            AppendLog($"[Server] UDP started on {GetLocalIpString()}:{port} (Name: {sName})");
                            SetServerStatus($"Running (UDP) on {port}");
                        }
                        else AppendLog("[Server] UDP server script no asignado");
                    }
                });
            }

            // Stop server
            if (stopServerBtn != null)
            {
                stopServerBtn.onClick.AddListener(() =>
                {
                    if (tcpServer != null)
                    {
                        try { tcpServer.StopServer(); AppendLog("[Server] TCP stopped"); }
                        catch (Exception e) { AppendLog("[Server] Error stopping TCP: " + e.Message); }
                    }
                    if (udpServer != null)
                    {
                        try { udpServer.StopServer(); AppendLog("[Server] UDP stopped"); }
                        catch (Exception e) { AppendLog("[Server] Error stopping UDP: " + e.Message); }
                    }
                    SetServerStatus("Stopped");
                });
            }

            // server send message (optional: will try to call a broadcast/send method on server if exists)
            if (sendServerBtn != null)
            {
                sendServerBtn.onClick.AddListener(() =>
                {
                    string msg = messageInputServer != null ? messageInputServer.text : "";
                    if (string.IsNullOrEmpty(msg)) { AppendLog("[Server] Empty message, nothing sent."); return; }

                    bool sent = false;
                    // Try TCP server broadcast method if exists
                    if (tcpServer != null)
                    {
                        MethodInfo mi = tcpServer.GetType().GetMethod("Broadcast", BindingFlags.Instance | BindingFlags.Public);
                        if (mi != null)
                        {
                            mi.Invoke(tcpServer, new object[] { msg });
                            sent = true;
                        }
                    }
                    // Try UDP server send method if exists
                    if (!sent && udpServer != null)
                    {
                        MethodInfo mi = udpServer.GetType().GetMethod("Broadcast", BindingFlags.Instance | BindingFlags.Public);
                        if (mi != null)
                        {
                            mi.Invoke(udpServer, new object[] { msg });
                            sent = true;
                        }
                    }

                    if (sent) AppendLog($"[Server] Sent: {msg}");
                    else AppendLog("[Server] No broadcast method found on server scripts. Implement Broadcast(string) to enable server sending.");
                });
            }
        }

        // CLIENT SCENE ---------------------------------------------------
        bool isClientScene = scene.Contains("client") || scene.Contains("joingame");
        if (isClientScene)
        {
            if (connectBtn == null) Debug.LogError("NetworkUI: connectBtn NO asignado (Client scene)");
            if (clientProtocolDropdown == null) Debug.LogWarning("NetworkUI: clientProtocolDropdown NO asignado (Client scene)");
            if (tcpClient == null && udpClient == null) Debug.LogWarning("NetworkUI: tcpClient y udpClient son ambos null (Client)");
            if (disconnectBtn == null) Debug.LogWarning("NetworkUI: disconnectBtn NO asignado (Client scene)");
            if (statusTextClient == null) Debug.LogWarning("NetworkUI: statusTextClient NO asignado (Client scene)");

            if (connectBtn != null)
            {
                connectBtn.onClick.AddListener(() =>
                {
                    int proto = clientProtocolDropdown != null ? clientProtocolDropdown.value : 0;
                    int port = ParsePort(clientPortInput, DEFAULT_PORT);
                    string ip = serverIpInput != null ? serverIpInput.text : "127.0.0.1";

                    if (proto == 0) // TCP
                    {
                        if (tcpClient != null)
                        {
                            tcpClient.serverIp = ip;
                            tcpClient.serverPort = port;
                            tcpClient.Connect();
                            AppendLog($"[Client] Trying TCP connect to {ip}:{port}");
                            SetClientStatus($"Connecting (TCP)...");
                        }
                        else AppendLog("[Client] TCP client script no asignado");
                    }
                    else // UDP
                    {
                        if (udpClient != null)
                        {
                            udpClient.serverIp = ip;
                            udpClient.serverPort = port;
                            udpClient.StartClient();
                            AppendLog($"[Client] UDP client started to {ip}:{port}");
                            SetClientStatus($"Connecting (UDP)...");
                        }
                        else AppendLog("[Client] UDP client script no asignado");
                    }
                });
            }

            // Disconnect button
            if (disconnectBtn != null)
            {
                disconnectBtn.onClick.AddListener(() =>
                {
                    try
                    {
                        if (tcpClient != null) tcpClient.Disconnect();
                        if (udpClient != null) udpClient.StopClient();
                        AppendLog("[Client] Disconnected");
                        SetClientStatus("Disconnected");
                    }
                    catch (Exception e)
                    {
                        AppendLog("[Client] Error disconnecting: " + e.Message);
                    }
                });
            }

            // client send message (general purpose)
            if (sendClientBtn != null)
            {
                sendClientBtn.onClick.AddListener(() =>
                {
                    string msg = messageInputClient != null ? messageInputClient.text : "";
                    if (string.IsNullOrEmpty(msg)) { AppendLog("[Client] Empty message, nothing sent."); return; }

                    int proto = clientProtocolDropdown != null ? clientProtocolDropdown.value : 0;
                    if (proto == 0)
                    {
                        if (tcpClient != null) { tcpClient.Send(msg); AppendLog("[Client] Sent TCP: " + msg); }
                        else AppendLog("[Client] TCP client script no asignado");
                    }
                    else
                    {
                        if (udpClient != null) { udpClient.Send(msg); AppendLog("[Client] Sent UDP: " + msg); }
                        else AppendLog("[Client] UDP client script no asignado");
                    }
                });
            }
        
        }

        // If neither server nor client scene, warn
        if (!isServerScene && !isClientScene)
            Debug.Log("NetworkUI: escena no reconocida; coloca el NetworkUI apropiado o renombra la escena para que contenga 'server'/'client' o 'creategame'/'joingame'.");
    }

    int ParsePort(TMP_InputField portField, int fallback)
    {
        if (portField == null) return fallback;
        int p = fallback;
        if (!int.TryParse(portField.text, out p))
        {
            AppendLog($"Puerto inválido '{portField.text}', usando {fallback}");
            p = fallback;
        }
        return p;
    }

    // Helper para mostrar IP local en logs (intenta IPv4 localhost)
    string GetLocalIpString()
    {
        return "127.0.0.1";
    }

    // Append log + autoscroll
    public void AppendLog(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        if (logText != null)
        {
            logText.text += $"[{DateTime.Now:HH:mm:ss}] {s}\n";
            if (logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 0f;
                Canvas.ForceUpdateCanvases();
            }
        }
        else Debug.Log(s);
    }

    // Small helpers to update status text
    void SetServerStatus(string s)
    {
        if (statusTextServer != null) statusTextServer.text = s;
        else Debug.Log("[ServerStatus] " + s);
    }

    void SetClientStatus(string s)
    {
        if (statusTextClient != null) statusTextClient.text = s;
        else Debug.Log("[ClientStatus] " + s);
    }
}
