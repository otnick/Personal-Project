using UnityEngine;
using NativeWebSocket;
using System;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WebSocketController : MonoBehaviour
{
    public static WebSocketController Instance;

    private WebSocket websocket;

    [Header("Connection")]
    public string serverIP = "10.204.0.49";
    public int serverPort = 8081;
    public bool autoReconnect = true;
    public float reconnectDelay = 3f;

    [Header("Lights")]
    public GameObject Sun;
    public GameObject FrontLight;

    [Range(0, 255)]
    public int ledIntensity = 0;

    public bool IsConnected => websocket != null && websocket.State == WebSocketState.Open;

    public event Action OnConnected;
    public event Action OnDisconnected;

    private bool isConnecting = false;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        await Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif
    }

    async void OnDestroy()
    {
        if (websocket != null)
            await websocket.Close();
    }

    #endregion

    #region Connection

    public async Task Connect()
    {
        if (isConnecting) return;

        isConnecting = true;

        websocket = new WebSocket($"ws://{serverIP}:{serverPort}");

        websocket.OnOpen += async () =>
        {
            Debug.Log("WebSocket Connected");
            isConnecting = false;

            OnConnected?.Invoke();

            string UUID = SystemInfo.deviceUniqueIdentifier;
            await websocket.SendText("Device (Unity): " + SystemInfo.deviceName +
                                     " | UUID: " + UUID);
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received: " + message);
            IncomingMessageParser(message);
        };

        websocket.OnClose += (code) =>
        {
            Debug.Log("WebSocket Closed");
            OnDisconnected?.Invoke();

            if (autoReconnect)
                Invoke(nameof(Reconnect), reconnectDelay);
        };

        websocket.OnError += (err) =>
        {
            Debug.LogError("WebSocket Error: " + err);
        };

        await websocket.Connect();
    }

    private async void Reconnect()
    {
        Debug.Log("Reconnecting...");
        await Connect();
    }

    #endregion

    #region Send Methods

    private async Task SafeSend(string message)
    {
        if (IsConnected)
        {
            await websocket.SendText(message);
            Debug.Log("Sent: " + message);
        }
        else
        {
            Debug.LogWarning("WebSocket not connected. Message not sent: " + message);
        }
    }

    public async void SendHello()
    {
        await SafeSend("Hello from Unity");
    }

    public async void SendLedON()
    {
        await SafeSend("button:1");
    }

    public async void SendLedOFF()
    {
        await SafeSend("button:0");
    }

    public async void SendLedIntensity()
    {
        await SafeSend("LED_INTENSITY:" + ledIntensity);
    }

    #endregion

    #region Incoming Messages

    public void IncomingMessageParser(string msg)
    {
        if (!msg.Contains(":")) return;

        string valueParsed = msg.Substring(msg.IndexOf(":") + 1);

        if (msg.Contains("button"))
        {
            if (valueParsed == "1")
            {
                Debug.Log("ESP32 Button Pressed");

                if (Sun) Sun.GetComponent<Light>().intensity = 0;
                if (FrontLight) FrontLight.GetComponent<Light>().intensity = 50f;
            }
            else if (valueParsed == "0")
            {
                Debug.Log("ESP32 Button Released");

                if (Sun) Sun.GetComponent<Light>().intensity = 33f;
                if (FrontLight) FrontLight.GetComponent<Light>().intensity = 0;
            }
        }
    }

    #endregion
}
