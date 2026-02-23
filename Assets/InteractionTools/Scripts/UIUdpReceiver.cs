using System.Collections.Generic;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIUdpReceiver : MonoBehaviour
{
    private const int TCP_PORT = 63901;

    private const int UdpListenPort = 29888; // 监听的端口
    [FormerlySerializedAs("UIRobot")] public UIOperate uiOperate;
    public UdpReceiver UdpReceiver;
    public GameObject IpSelectDialog;
    public Button IpButtonItem;
    public TextMeshProUGUI ShowDiscovering;

    private bool _closed = false;

    void Start()
    {
        IpButtonItem.gameObject.SetActive(false);
        UdpReceiver.ReceiveEvent += OnUdpReceive;
        UdpReceiver.ListenTo(UdpListenPort);
        IpSelectDialog.SetActive(true);
        UpdateDiscoveringVisibility();
    }

    private void OnEnable()
    {
        // When SettingPanel is shown again after Close(), reopen so discovery works and panel is usable
        if (_closed && UdpReceiver != null)
        {
            _closed = false;
            UdpReceiver.ListenTo(UdpListenPort);
        }
    }

    // WE CAN CHANGE THE NAME HERE - we get from a list of previously input IPs
    private HashSet<string> _receiveIps = new HashSet<string>();

    private void UpdateDiscoveringVisibility()
    {
        if (ShowDiscovering != null)
            ShowDiscovering.gameObject.SetActive(_receiveIps.Count == 0);
    }

    private void ReceiveUdpIP(NetPacket package)
    {
        // IpSelectDialog.SetActive(true);
        string ip = package.ToString();
        if (_receiveIps.Contains(ip))
        {
            return;
        }

        Button but = Instantiate(IpButtonItem, IpButtonItem.transform.parent);
        but.gameObject.SetActive(true);
        but.GetComponentInChildren<Text>().text = ip;
        but.onClick.AddListener(() => { OnClickIP(ip); });
        _receiveIps.Add(ip);
        UpdateDiscoveringVisibility();
    }

    private void OnClickIP(string ip)
    {
        UdpReceiver.Close();
        uiOperate.TcpConnect(ip);
        // IpSelectDialog.SetActive(false);
        uiOperate.ShowDashboard();
    }

    private void OnUdpReceive(NetPacket package)
    {
        if (_closed)
        {
            return;
        }

        if (package.Cmd == NetCMD.PACKET_CMD_TCPIP)
        {
            ReceiveUdpIP(package);
        }
    }

    public void Close()
    {
        _closed = true;
        UdpReceiver.Close();
        // Show dashboard first so the Settings button is visible and clickable to reopen Settings
        if (uiOperate != null)
            uiOperate.ShowDashboard();
    }
}