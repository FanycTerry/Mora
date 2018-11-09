using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using UnityEngine;
using System.Runtime.InteropServices;
using UniRx;
using Kernys.Bson;


public class WebSocket : IDisposable
{
    class TimeoutCheckData
    {
        public float Maturity { get; private set; }
        public string Type { get; private set; }
        public TimeoutCheckData(string type)
        {
            Type = type;
            //Debug.Log(Time.time);
            Maturity = Time.time + 20; // 10 second timeout
        }
    }

    static Dictionary<string, WebSocket> instances = new Dictionary<string, WebSocket>();

    /// <summary>
    /// 建立或取得一個新的連線
    /// </summary>
    /// <param name="key">新連線的名稱</param>
    /// <param name="url">連線位置</param>
    /// <returns>返回一個可以連線的WebSocket</returns>
    public static WebSocket CreateInstance(string key, Uri url)
    {
        if (!instances.ContainsKey(key))
            instances.Add(key, new WebSocket(url, key, getCredentials(key)));

        return instances[key];
    }

    /// <summary>
    /// 取得WebSocket登入各連線的密碼.
    /// </summary>
    /// <param name="key">服務名稱.</param>
    /// <returns>密碼字串.</returns>
    static string getCredentials(string key)
    {
        switch(key)
        {
            case "LoginHost": return "4vT3eKUQqnF6NJtP";
            case "AgentHost": return "l9xmVfdHaNRD3KcF";
            case "ChatHost": return "CU96uk4wRElC46DH";
            default: return "";
        }
    }

    /// <summary>
    /// Removes the 的WebSocket.
    /// </summary>
    /// <param name="key">欲移除的名稱（Key）.</param>
    /// <param name="removeErrMsg">移除＆解構時，會一同移除關閉和錯誤連線的提示訊息＆事件.</param>
    public static void RemoveInstance(string key, bool removeErrMsg = false)
    {
        if (instances.ContainsKey(key))
        {
            Debug.LogFormat("解構{0}的WebSocket",key);
#if !UNITY_WEBGL
            if (removeErrMsg) instances[key].RemoveErrMsg();
#endif
            instances[key].Close();
            instances[key].Dispose();
            instances.Remove(key);            
        }
    }

    public Uri Url { get; private set; }
    /// <summary>
    ///  登入帳號(名稱).
    /// </summary>
    public string Id { get; private set; }
    /// <summary>
    /// 驗證碼.
    /// </summary>
    public string Credentials { get; private set; }
    public bool IsConnected { get; private set; }
    private Subject<BSONObject> onRecv = new Subject<BSONObject>();
    private Subject<bool> onConnect = new Subject<bool>();
    private Subject<int> onDisconnect = new Subject<int>();
    private CompositeDisposable subscribeList = new CompositeDisposable();
    private List<TimeoutCheckData> sendList = new List<TimeoutCheckData>();
    private Subject<bool> OnReqTimeOut = new Subject<bool>();

#if !UNITY_WEBGL
    /// <summary>
    /// 連線錯誤時的事件.
    /// </summary>
    /// <value>The event error.</value>
    EventHandler<WebSocketSharp.ErrorEventArgs> eventErr
    {
        get { return (sender, e) => { m_Error = e.Message; onConnect.OnError(new Exception(e.Message)); }; }
    }
    /// <summary>
    /// 連線關閉時的事件.
    /// </summary>
    /// <value>The event close.</value>
    EventHandler<WebSocketSharp.CloseEventArgs> eventClose
    {
        get { return (sender, e) => { IsConnected = false; onDisconnect.OnNext(e.Code); Debug.LogFormat("[WebSocket] {0} OnClose Result Code = {1}", Id, e.Code); }; }
    }
#endif

    /// <summary>
    /// 建構式.
    /// </summary>
    /// <param name="url">欲連結位置</param>
    /// <param name="id">連線登入名稱</param>
    /// <param name="credentials">密碼</param>
    private WebSocket(Uri url, string id, string credentials = "")
    {
        Url = url;
        Id = id;
        Credentials = credentials;

        string protocol = Url.Scheme;
        if (!protocol.Equals("ws") && !protocol.Equals("wss"))
            throw new ArgumentException("Unsupported protocol: " + protocol);

        onRecv.Subscribe(packet => {
            for (var i = 0; i < sendList.Count; i++)
            {
                if (sendList[i].Type == packet["type"].stringValue)
                {
                    sendList.RemoveAt(i);
                    break;
                }
            }
        }).AddTo(subscribeList);

        Observable.Interval(TimeSpan.FromMilliseconds(100)).Subscribe(t => {
            for (var i = 0; i < sendList.Count; i++)
            {
                if (Time.time > sendList[i].Maturity)
                {
                    OnReqTimeOut.OnNext(true);
                    onRecv.OnError(new TimeoutException(string.Format("time out on packet '{0}'", sendList[i].Type)));

                    //如果是聊天，自動斷線
                    Debug.Log("chat or gm:"+((sendList[i].Type.StartsWith("chat") || sendList[i].Type.StartsWith("gmservice"))));
                    if (sendList[i].Type.StartsWith("chat") || sendList[i].Type.StartsWith("gmservice"))
                        Debug.Log("Error");

                    sendList.RemoveAt(i);

                    break;
                }
            }
        }).AddTo(subscribeList);

        // 當連線時要做的流程.
        onConnect
             .Where(conn => conn)
             .Do(_ => setCheckNetwork())
             .Subscribe()
             .AddTo(subscribeList);
    }

    /// <summary>
    /// 設定檢查網路連線是否正常.
    /// </summary>    
    void setCheckNetwork()
    {
#if !UNITY_WEBGL
        // change APP check network rx.
        Observable.EveryApplicationFocus()
             //.Do(_ => Debug.Log("[網路連線檢查]切換APP"))
             .Do(_ =>
             {
                 if (m_Socket.Ping())
                 {
                     //Debug.Log("沒有斷線.........................   " + Id);
                 }
                 else
                 {
                     //onDisconnect.OnNext(-1);
                     if (Id != "LoginHost")
                         Debug.Log("[網路連線檢查]切換APP 斷線了!!!!!!!!!!!!!!!!!!!!!!!!!!!   " + Id);

                     if (Id == "ChatHost")
                         ClientGlobal.Instance.ChatError.OnNext(true);
                 }
             })
             .Subscribe()
             .AddTo(subscribeList);

        // every 30 sec check network rx.
        Observable.Interval(TimeSpan.FromSeconds(30.0f))
             //.Do(_ => Debug.Log("[網路連線檢查]30秒檢查"))
             .Do(_ =>
             {
                 if (m_Socket.Ping())
                 {
                     //Debug.Log("沒有斷線.........................   " + Id);
                 }
                 else
                 {
                     //onDisconnect.OnNext(-1);
                     if (Id != "LoginHost")
                         Debug.Log("[網路連線檢查]30秒檢查 斷線了!!!!!!!!!!!!!!!!!!!!!!!!!!!   " + Id);

                     if (Id == "ChatHost")
                         ClientGlobal.Instance.ChatError.OnNext(true);
                 }
             })
             .Subscribe()
             .AddTo(subscribeList);
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
	private static extern int SocketCreate (string url);

	[DllImport("__Internal")]
	private static extern int SocketState (int socketInstance);

	[DllImport("__Internal")]
	private static extern void SocketSend (int socketInstance, byte[] ptr, int length);

	[DllImport("__Internal")]
	private static extern void SocketRecv (int socketInstance, byte[] ptr, int length);

	[DllImport("__Internal")]
	private static extern int SocketRecvLength (int socketInstance);

	[DllImport("__Internal")]
	private static extern void SocketClose (int socketInstance);

	[DllImport("__Internal")]
	private static extern int SocketError (int socketInstance, byte[] ptr, int length);

	int m_NativeRef = 0;
    IDisposable timerCancal = null;

    public IObservable<bool> Connect()
    {
        // set JavaScript WebSocket, url = url + id + pw.
        string title = string.Format("wss://{0}:{1}@", Id, Credentials);
        string new_url = Url.ToString().Replace("wss://", title);
        Debug.LogFormat("[WebSocket] Conn URL = {0}", new_url);
        m_NativeRef = SocketCreate(new_url);
        IsConnected = true;

        IDisposable cancal = null;
        cancal = Observable.Interval(System.TimeSpan.FromMilliseconds(100))
            .Subscribe(_ =>
            {
                var socketState = SocketState(m_NativeRef);
                if (SocketState(m_NativeRef) != 0)
                {
                    onConnect.OnNext(socketState == 1);
					onConnect.OnCompleted();
                    if (cancal != null) cancal.Dispose();
                    if(socketState == 1)
                    {
                        timerCancal = Observable.Interval(TimeSpan.FromMilliseconds(100))
                                        .Subscribe(t => recv());
                    }
                }
            });

        errorClose            
            .Do(value => onDisconnect.OnNext(value))
            .Do(value => IsConnected = false)
            .Do(value => Debug.LogFormat("[WebSocket] {0} OnClose Result Code = {1}", Id, value))
            .Subscribe()
            .AddTo(subscribeList);      

        Observable.Interval( TimeSpan.FromSeconds(3))
            .Select(_ => error)
            .Where(x => x != null && x.Length > 0)
            //.Do(str => Debug.LogFormat("[Websocket] 每秒檢查 err = {0}, 長度 = {1}", str, str.Length))            
            .Select(x => int.Parse(x))
            .Do(x => errorClose.Value = x )            
            .Subscribe()
            .AddTo(subscribeList);


        return onConnect.ObserveOnMainThread().Timeout(System.TimeSpan.FromSeconds(10));
    }

	public void Send(byte[] buffer, string recvType)
	{
		sendList.Add (new TimeoutCheckData(recvType));
        CheckBsonValue.ShowBsonData(SimpleBSON.Load(buffer), "packet send: ");
        SocketSend (m_NativeRef, buffer, buffer.Length);
    }

	private void recv()
	{
		int length = SocketRecvLength (m_NativeRef);
		if (length != 0)
        {
            byte[] buffer = new byte[length];
            SocketRecv(m_NativeRef, buffer, length);
			var bson = SimpleBSON.Load(buffer);
			CheckBsonValue.ShowBsonData(bson, "packet recv: ");
			onRecv.OnNext(bson);
        }
	}

    public IObservable<int> OnDisconnect()
    {
        return onDisconnect.AsObservable();
    }

    public IObservable<bool> onReqTimeOut()
    {
        return OnReqTimeOut.AsObservable();
    }
 
	public void Close()
	{
        timerCancal.Dispose();
        timerCancal = null;
        SocketClose(m_NativeRef);
        IsConnected = false;
	}

    public ReactiveProperty<int> errorClose = new ReactiveProperty<int>( 0 );
	public string error
	{
		get {
			const int bufsize = 1024;
			byte[] buffer = new byte[bufsize];
			int result = SocketError (m_NativeRef, buffer, bufsize);

			if (result == 0)
				return null;

			return Encoding.UTF8.GetString (buffer);				
		}
	}
#else
    WebSocketSharp.WebSocket m_Socket;

    string m_Error = null;

    public IObservable<bool> Connect()
    {
        m_Socket = new WebSocketSharp.WebSocket(Url.ToString());
        m_Socket.SetCredentials(Id, Credentials, true);        

        m_Socket.OnMessage += (sender, e) => {
            var bson = SimpleBSON.Load(e.RawData);
            CheckBsonValue.ShowBsonData(bson, "packet recv: ");
            onRecv.OnNext(bson);
        };

        initSocket();
        m_Socket.ConnectAsync();
        
        return onConnect.ObserveOnMainThread().Timeout(System.TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// 初始化SOCKET的open,error,close事件.
    /// 需注意的是,WebsocketSharp的DLL在WebGL版是無法使用,但在編輯器模式下必須讓Websocket可運行
    /// </summary>
    public void initSocket()
    {
        m_Socket.OnOpen += eventOpen;
        m_Socket.OnError += eventErr;        
        m_Socket.OnClose += eventClose;
    }

    void eventOpen(object sender, EventArgs e)
    {
        IsConnected = true;
        onConnect.OnNext(true);
        onConnect.OnCompleted();
    }

    void eventErr(object sender, WebSocketSharp.ErrorEventArgs e)
    {
        m_Error = e.Message;
        onConnect.OnError(new Exception(e.Message));
    }

    void eventClose(object sender, WebSocketSharp.CloseEventArgs e)
    {
        IsConnected = false;
        onDisconnect.OnNext(e.Code);
        Debug.LogFormat("[WebSocket] {0} OnClose Result Code = {1}", Id, e.Code);
    }

    /// <summary>
    /// 移除Socket事件,主要是用在登出時,OnError和OnClose會被觸發,必須在登出時取消這事件.
    /// </summary>
    public void RemoveErrMsg()
    {
        m_Socket.OnError -= eventErr;
        m_Socket.OnClose -= eventClose;
    }

    public IObservable<int> OnDisconnect()
    {
        Debug.Log("=====================OnDisconnect");
        return onDisconnect.AsObservable();
    }

    public IObservable<bool> onReqTimeOut()
    {
        return OnReqTimeOut.AsObservable();
    }

    public void Send(byte[] buffer, string recvType)
	{
		sendList.Add (new TimeoutCheckData(recvType));
        CheckBsonValue.ShowBsonData(SimpleBSON.Load(buffer), "packet send: ");
        m_Socket.Send(buffer);
    }

	public void Close()
	{                
        if (IsConnected) m_Socket.Close();
        RemoveErrMsg();
    }

	public string error
	{
		get {
			return m_Error;
		}
	}
#endif

    public IDisposable OnRecv(string type, Action<BSONObject> callback)
	{
		return onRecv
			.Where(p => p["type"].stringValue == type)
			.ObserveOnMainThread ()
			.Subscribe (callback)
			.AddTo (subscribeList);
	}

	public void Dispose()
	{
        Debug.LogFormat("======斷線======{0}",Id);
        subscribeList.Clear ();

#if !UNITY_WEBGL
        if (m_Socket != null) m_Socket.Close();
        m_Socket = null;
#endif
    }
}