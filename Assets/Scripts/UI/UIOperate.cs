using System.Collections.Generic;
using System.IO;
using System.Net;
using Robot;
using Robot.Conf;
using Unity.XR.PICO.TOBSupport;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIOperate : MonoBehaviour
{
    public Text SN;
    public Text LocalIP;
    public Text TargetIP;
    public Text TrackNum;
    public Toggle HeadTog;
    public Toggle ControllerTog;
    public Toggle HandTrackingTog;
    public Toggle SendTog;
    public Toggle AcontrolerTog;
    public Dropdown bodyModeDrop;
    public TcpHandler TcpHandler;
    public Text BodyInfo;
    public Toggle HighAccuracy;
    public Text Version;
    public Button ReconnectBtn;
    public Toggle NetshareTog;

    public GameObject Simulator;
    public GameObject CameraObj;
    public GameObject IpInputDialog;
    public GameObject ExtDevPanel;
    public InputActionProperty SendDataAction;

    [Space(30)] [Header("Refactoring")] public VideoSourceManager videoSource;
    public VideoSourceConfigManager sourceConfig => videoSource.videoSourceConfigManager;

    public Dropdown videoSourceDropdown;


    [Header("Dashboard / Settings panels")]
    public GameObject DashboardPanel;
    public GameObject SettingPanel;

    /// <summary>False until first successful TCP connection; then true so Reconnect uses normal Reconnect().</summary>
    private bool _firstConnection = false;

    // Start is called before the first frame update
    private void Awake()
    {
        // Default: show dashboard when assigned, else show settings so UI is visible
        if (DashboardPanel != null)
        {
            DashboardPanel.SetActive(true);
            if (SettingPanel != null) SettingPanel.SetActive(false);
        }
        else if (SettingPanel != null)
            SettingPanel.SetActive(true);

#if UNITY_EDITOR
        if (Simulator != null)
        {
            Simulator.SetActive(true);
        }
#endif
        // ReconnectBtn.gameObject.SetActive(false);

        bodyModeDrop.onValueChanged.AddListener(OnBodyModeDrop);
        HeadTog.onValueChanged.AddListener(OnHeadTog);
        ControllerTog.onValueChanged.AddListener(OnControllerTog);
        HandTrackingTog.onValueChanged.AddListener(OnHandTrackingTog);

        SendTog.onValueChanged.AddListener(OnSendTog);
        Version.text = "v: " + Application.version;
        HighAccuracy.gameObject.SetActive(bodyModeDrop.value > 0);
        NetshareTog.onValueChanged.AddListener(OnNetShareTog);
        HighAccuracy.onValueChanged.AddListener(OnHighAccuracy);
        ReconnectBtn.onClick.AddListener(OnReconnectBtn);
        //The shared network function is only available on B-end devices.
        NetshareTog.gameObject.SetActive(false);
        // Bypass getting sn via enterprise service to enable data transport
        SetDeviceSN("TestDevice");
        bool intEnterprise = PXR_Enterprise.InitEnterpriseService();
        Debug.Log("---InitEnterpriseService :" + intEnterprise);
        PXR_Enterprise.BindEnterpriseService(OnBindEnterpriseService);

        // if (CameraObj != null)
        // {
        //     CameraObj.SetActive(false);
        // }

        AndroidProxy.CallBack += OnAndroidCallBack;
#if UNITY_EDITOR
        SetDeviceSN("TestDevice");
#endif
        // Refactoring
        sourceConfig.OnInitialized += OnSourceConfigOnOnInitialized;
        // Initialize video source configuration
        sourceConfig.Initialize();

        if (TcpHandler != null)
            TcpHandler.OnConnected += () => _firstConnection = true;
    }

    private void Start()
    {
        // Show last connected IP on main UI at startup and set TcpHandler address so Reconnect uses it
        string lastIp = GetLastConnectedIP();
        if (!string.IsNullOrEmpty(lastIp))
        {
            if (TargetIP != null)
                TargetIP.text = lastIp;
            if (TcpHandler != null)
                TcpHandler.SetAddress(lastIp);
        }
    }

    /// <summary>Show dashboard, hide settings. Call from Dashboard panel button (e.g. Back).</summary>
    public void ShowDashboard()
    {
        // Activate dashboard first so the Settings button is visible and can reopen Settings later
        if (DashboardPanel != null)
            DashboardPanel.SetActive(true);
        if (SettingPanel != null)
            SettingPanel.SetActive(false);
    }

    /// <summary>Hide dashboard, show settings. Call from Dashboard panel button (e.g. Settings).</summary>
    public void ShowSettings()
    {
        if (DashboardPanel != null) 
            DashboardPanel.SetActive(false);
        if (SettingPanel != null)
            SettingPanel.SetActive(true);
    }

    private void OnSourceConfigOnOnInitialized()
    {
        // Update videoSourceDropdown options
        print("OnSourceConfigOnOnInitialized");
        videoSourceDropdown.ClearOptions();
        videoSourceDropdown.AddOptions(sourceConfig.GetVideoSourceNames());
    }

    private void OnAndroidCallBack(string key, string value)
    {
        if (key == "RequestPermissionsBack")
        {
            if (value == "0")
            {
                if (CameraObj != null)
                {
                    CameraObj.SetActive(true);
                }
            }
            else
            {
                Toast.Show("Permission denied!");
            }
        }
    }

    private void OnReconnectBtn()
    {
        if (!_firstConnection)
        {
            // No connection made yet: run full TcpConnect with last IP (UI, tracking, send)
            string ip = GetLastConnectedIP() ?? TcpHandler.GetTargetIP;
            if (!string.IsNullOrEmpty(ip))
                TcpConnect(ip);
            else
                TcpHandler.Reconnect();
        }
        else
        {
            TcpHandler.Reconnect();
        }
    }

    /// <summary>Filename for last connected IP under Application.persistentDataPath.</summary>
    public const string LastIPFileName = "LastConnectedIP.txt";

    /// <summary>Full path where the last IP is stored.</summary>
    public static string LastIPFilePath => Path.Combine(Application.persistentDataPath, LastIPFileName);

    /// <summary>Returns the last saved IP, or null if none.</summary>
    public static string GetLastConnectedIP()
    {
        string path = LastIPFilePath;
        if (!File.Exists(path))
            return null;
        try
        {
            string ip = File.ReadAllText(path).Trim();
            return string.IsNullOrWhiteSpace(ip) ? null : ip;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[UIOperate] Could not read last IP from {path}: {e.Message}");
            return null;
        }
    }

    /// <summary>Saves the given IP to Application.persistentDataPath for next session.</summary>
    public static void SaveLastConnectedIP(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return;
        ip = ip.Trim();
        string path = LastIPFilePath;
        try
        {
            File.WriteAllText(path, ip);
            LogWindow.Info($"Stored IP for next session: {ip} (saved to {path})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UIOperate] Could not save last IP to {path}: {e.Message}");
        }
    }

    public void TcpConnect(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return;
        ip = ip.Trim();
        // TargetIP.text = "PC Service: " + ip;
        TargetIP.text = ip;
        ReconnectBtn.gameObject.SetActive(true);
        SaveLastConnectedIP(ip);
        TcpHandler.Connect(ip);
        ConnectSuccess();

        //In here we automatically set the tracking and start send
        
        // Select all three trackers    
        TrackingData.SetHeadOn(true);
        TrackingData.SetControllerOn(true);
        TrackingData.SetHandTrackingOn(true);

        // OnBodyModeDrop
        // Set Mode to Full-Body
        TrackingData.TrackingType tType = TrackingData.TrackingType.Body;
        int res = 0;
        bool support = false;
        MotionTrackerMode trackingMode = PXR_MotionTracking.GetMotionTrackerMode();
        
        if (trackingMode != MotionTrackerMode.BodyTracking)
        {
            res = PXR_MotionTracking.CheckMotionTrackerModeAndNumber(MotionTrackerMode.BodyTracking,
                MotionTrackerNum.TWO);
        }
        PXR_MotionTracking.GetBodyTrackingSupported(ref support);

        // UpdateBodyTracking()
        // TrackingData.TrackingType tType = TrackingData.TrackingType.Body;
        BodyTrackingBoneLength boneLength = new BodyTrackingBoneLength();
        MotionTrackerConnectState state = new MotionTrackerConnectState();
        PXR_MotionTracking.GetMotionTrackerConnectStateWithSN(ref state);
        BodyTrackingMode mode = BodyTrackingMode.BTM_FULL_BODY_LOW; //Do we choose high or low?
        // BodyTrackingMode mode = BodyTrackingMode.BTM_FULL_BODY_HIGH;
        // Enable full body motion capture default mode
        int ret = PXR_MotionTracking.StartBodyTracking(mode, boneLength);
        TrackingData.SetTrackingType(tType);
        
        // Click send
        TcpHandler.SendTrackingData = true;
    }

    public void ConnectSuccess()
    {
        // TargetIP.text = "PC Service: " + TcpHandler.GetTargetIP;
        TargetIP.text = TcpHandler.GetTargetIP;
    }

    private void OnBindEnterpriseService(bool bind)
    {
        Debug.Log("OnBindEnterpriseService " + bind);
        if (bind)
        {
            //The shared network function is only available on B-end devices.
            NetshareTog.gameObject.SetActive(true);
            PXR_Enterprise.GetSwitchSystemFunctionStatus(SystemFunctionSwitchEnum.SFS_USB_TETHERING,
                (value) => { NetshareTog.SetIsOnWithoutNotify(value == 1); });

            string sn = PXR_Enterprise.StateGetDeviceInfo(SystemInfoEnum.EQUIPMENT_SN);
            SetDeviceSN(sn);
        }
    }

    private void SetDeviceSN(string sn)
    {
        TcpHandler.SetDeviceSn(sn);
        Debug.Log("SN: " + sn);
        SN.text = "SN: " + sn;
    }

    private void OnNetShareTog(bool ison)
    {
        Debug.Log("OnNetShareTog:" + ison);
        if (ison)
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_USB_TETHERING, SwitchEnum.S_ON);
        else
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_USB_TETHERING, SwitchEnum.S_OFF);

        PXR_Enterprise.GetSwitchSystemFunctionStatus(SystemFunctionSwitchEnum.SFS_USB_TETHERING,
            (value) => { Debug.Log("SFS_USB_TETHERING:" + value); });
    }

    public void OnQuit()
    {
        Application.Quit();
    }

    public void OnExtraDevBtn()
    {
        ExtDevPanel.SetActive(true);
    }

    public void OnWriteIpBtn()
    {
        IpInputDialog.SetActive(true);
    }

    private void OnBodyModeDrop(int index)
    {
        TrackingData.TrackingType tType = (TrackingData.TrackingType)bodyModeDrop.value;
        int res = 0;
        bool support = false;

        MotionTrackerMode trackingMode = PXR_MotionTracking.GetMotionTrackerMode();
        if (tType == TrackingData.TrackingType.Body)
        {
            if (trackingMode != MotionTrackerMode.BodyTracking)
            {
                res = PXR_MotionTracking.CheckMotionTrackerModeAndNumber(MotionTrackerMode.BodyTracking,
                    MotionTrackerNum.TWO);
            }

            PXR_MotionTracking.GetBodyTrackingSupported(ref support);
        }
        else if (tType == TrackingData.TrackingType.Motion)
        {
            if (trackingMode != MotionTrackerMode.MotionTracking)
            {
                res = PXR_MotionTracking.CheckMotionTrackerModeAndNumber(MotionTrackerMode.MotionTracking,
                    MotionTrackerNum.ONE);
            }

            support = true;
        }

        if (!support || res != 0)
        {
            BodyInfo.text = "Tracker exception, please connect to calibrate tracker!";
            BodyInfo.color = Color.red;

            bodyModeDrop.SetValueWithoutNotify(0);
            // Update UI
            HighAccuracy.gameObject.SetActive(false);
            return;
        }
        
        // Update UI
        HighAccuracy.gameObject.SetActive(index > 0);

        BodyInfo.color = Color.white;
        BodyInfo.text = "Tracker detection is normal!";

        UpdateBodyTracking();
    }


    public void OnOpenCameraOperate()
    {
        if (CameraObj != null)
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Camera) &&
                Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                CameraObj.SetActive(!CameraObj.activeSelf);
            }
            else if (!CameraObj.activeSelf)
            {
                var permissionCallbacks = new PermissionCallbacks();
                permissionCallbacks.PermissionGranted += PermissionGranted;
                permissionCallbacks.PermissionDenied += PermissionDenied;

                string[] permissions = { Permission.Camera, Permission.Microphone };
                Permission.RequestUserPermissions(permissions, permissionCallbacks);
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
            }

            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }
        }
    }

    private void PermissionDenied(string obj)
    {
        Toast.Show("Permission denied!");
    }

    private void PermissionGranted(string obj)
    {
        if (CameraObj != null)
        {
            CameraObj.SetActive(true);
        }
    }

    private void RefreshLocalIP()
    {
        string localIP = Utils.GetLocalIPv4();
        LocalIP.text = localIP;
    }

    // Obtain the local IPv6 address
    private string GetLocalIPv6()
    {
        string localIP = "Not found";
        foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                localIP = ip.ToString();
                break;
            }
        }

        return localIP;
    }


    private void OnHeadTog(bool on)
    {
        TrackingData.SetHeadOn(on);
    }

    private void OnControllerTog(bool on)
    {
        TrackingData.SetControllerOn(on);
    }

    private void OnHandTrackingTog(bool on)
    {
        TrackingData.SetHandTrackingOn(on);
    }

    private void OnSendTog(bool on)
    {
        TcpHandler.SendTrackingData = on;
        // Reset FPS
        if (!on)
        {
            FPSDisplay.Reset();
        }
    }

    private void OnHighAccuracy(bool on)
    {
        UpdateBodyTracking();
    }

    private void UpdateBodyTracking()
    {
        TrackingData.TrackingType tType = (TrackingData.TrackingType)bodyModeDrop.value;
        HighAccuracy.gameObject.SetActive(bodyModeDrop.value > 0);
        Debug.Log("UpdateBodyTracking " + tType);
        TrackNum.text = "";
        // Set bone length
        BodyTrackingBoneLength boneLength = new BodyTrackingBoneLength();
        if (bodyModeDrop.value <= 0)
        {
            int ret = PXR_MotionTracking.StopBodyTracking();
            BodyInfo.text = "BodyTracking close";
        }
        else
        {
            MotionTrackerConnectState state = new MotionTrackerConnectState();
            PXR_MotionTracking.GetMotionTrackerConnectStateWithSN(ref state);
            TrackNum.text = "Num: " + state.trackerSum;

            if (tType == TrackingData.TrackingType.Body)
            {
                BodyTrackingMode mode = BodyTrackingMode.BTM_FULL_BODY_LOW;
                if (HighAccuracy.isOn)
                {
                    mode = BodyTrackingMode.BTM_FULL_BODY_HIGH;
                }

                // Enable full body motion capture default mode
                int ret = PXR_MotionTracking.StartBodyTracking(mode, boneLength);
                BodyInfo.text = "Start BodyTracking " + ret;
                Debug.Log(" UpdateBodyTracking :" + ret + " trackerSum:" + state.trackerSum);
            }
            else if (tType == TrackingData.TrackingType.Motion)
            {
                BodyInfo.text = "Start MotionTracking";
            }
        }

        TrackingData.SetTrackingType(tType);
    }

    private float _lastTime = 0;

    // Update is called once per frame
    void Update()
    {
        if (TcpHandler.State != SocketState.WORKING)
        {
            if (Time.time - _lastTime > 2)
            {
                _lastTime = Time.time;
                RefreshLocalIP();
            }
        }

        if (AcontrolerTog != null && AcontrolerTog.isOn)
        {
            if (SendDataAction.action != null && SendDataAction.action.WasReleasedThisFrame())
            {
                SendTog.isOn = !SendTog.isOn;
                LogWindow.Info("Sending data: " + SendTog.isOn);
            }
        }
    }

    public void OnQuitBtn()
    {
        Application.Quit();
    }
}