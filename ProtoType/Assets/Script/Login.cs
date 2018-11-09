using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Mora;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class Login : MonoBehaviour {

    public string LOGIN = "http://127.0.0.1:8080/login" ;
    public string REGIST = "http://127.0.0.1:8080/register" ;

    public enum ResultType: int
    {
        OK = 0,
        ERROR,
    }


    [SerializeField] Text Name;
    [SerializeField] Text Pw;
    [SerializeField] Toggle toggle;
    [SerializeField] Button LoginBtn;
    [SerializeField] Button RegistBtn;
    // Use this for initialization
    void Start () {        

        // 新式實驗體2.  
        RegistBtn.OnClickAsObservable()
            .Select(_ => Tuple.Create(Name.text, Pw.text))
            .Do(x => Debug.LogFormat("name : {0}, pw :{1}",x.Item1, x.Item2))
            .Select(x => new RegisterReq() { name = x.Item1, pw = x.Item2 })
            .Select(req => JsonConvert.SerializeObject(req))
            .SelectMany(json => HttpUtility.Put(REGIST, json))
            .Subscribe()
            .AddTo(this.gameObject);  

        LoginBtn.OnClickAsObservable()
            .Select(_ => Tuple.Create(Name.text, Pw.text))
            .Do(x => Debug.LogFormat("name : {0}, pw :{1}", x.Item1, x.Item2))
            .Select(x => new LoginReq() { name = x.Item1, pw = x.Item2 })
            .Select(req => JsonConvert.SerializeObject(req))
            .SelectMany(json => HttpUtility.Put(LOGIN, json))
            .Select(json => JsonConvert.DeserializeObject<LoginResp>(json))
            .Where(resp => resp.result == (int)ResultType.OK)
            .Do(resp => UpdateInfo(resp))
            .Do(_ => ToastMgr.SetToast("登入中"))
            .Do(_ => SceneManager.LoadSceneAsync("Mora", LoadSceneMode.Single))
            .Subscribe()
            .AddTo(this.gameObject);

        WebSocket.RemoveInstance("server");
        websocket = WebSocket.CreateInstance("server", new System.Uri("wss://127.0.0.1:17701"));
        websocket.Connect()            
            .Subscribe(
             success => {
                 Debug.LogFormat("連線成功={0}", success);
             },ex => {
                 Debug.LogFormat("連線失敗={0}", ex);
             });

        var regist = new RegisterReq() { name = "XZXX", pw = "1111" };
        var json2 = JsonConvert.SerializeObject(regist);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json2);
        
        websocket.Send(bytes, "XXX");
        
    }
    WebSocket websocket;

    public void OnDestroy()
    {
        Debug.Log("OnDestroy");           
        websocket.Close();
        websocket.Dispose();
    }

    void UpdateInfo(LoginResp resp)
    {
        UserInfoMgr.Instance.name = resp.name;
        UserInfoMgr.Instance.money = resp.money ;        
    }

    void UpdateInfo(string json)
    {
        var info = JsonConvert.DeserializeObject<LoginResp>(json);
        if (info == null) return;

        UserInfoMgr.Instance.name = info.name;
        UserInfoMgr.Instance.money = info.money;
    }


}
