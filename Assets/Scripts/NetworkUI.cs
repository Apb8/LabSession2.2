using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkUI_TMPro : MonoBehaviour
{
    public TMP_InputField serverNameInput;
    public Button startTcpServerBtn;
    public Button startUdpServerBtn;

    public TMP_InputField serverIpInput;
    public TMP_InputField playerNameInput;
    public Button connectTcpBtn;
    public Button connectUdpBtn;
    public Button sendNameBtn;

    public TMP_Text logText;

    public TcpServer tcpServer;
    public TcpClientConnector tcpClient;
    public UdpServer udpServer;
    public UdpClientConnector udpClient;

    void Start()
    {
        startTcpServerBtn.onClick.AddListener(() =>
        {
            tcpServer.StartServer();
            AppendLog("TCP server started");
        });

        startUdpServerBtn.onClick.AddListener(() =>
        {
            udpServer.StartServer();
            AppendLog("UDP server started");
        });

        connectTcpBtn.onClick.AddListener(() =>
        {
            tcpClient.serverIp = serverIpInput.text;
            tcpClient.serverPort = 9050;
            tcpClient.Connect();
            AppendLog("Trying TCP connect to " + serverIpInput.text);
        });

        connectUdpBtn.onClick.AddListener(() =>
        {
            udpClient.serverIp = serverIpInput.text;
            udpClient.serverPort = 9050;
            udpClient.StartClient();
            AppendLog("UDP client started to " + serverIpInput.text);
        });

        sendNameBtn.onClick.AddListener(() =>
        {
            string name = playerNameInput.text;
            tcpClient.Send(name);
            udpClient.Send(name);
            AppendLog("Sent name: " + name);
        });
    }

    public void AppendLog(string s)
    {
        logText.text += s + "\n";
    }
}
