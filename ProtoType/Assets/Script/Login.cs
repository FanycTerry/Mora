using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Mora;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

public class Login : MonoBehaviour {

    public string LOGIN = "http://127.0.0.1:8080/Login";
    public string REGIST = "http://127.0.0.1:8080/Register";

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
        /*
        LoginBtn.OnClickAsObservable()
            .Select(_ => Tuple.Create(Name.text, Pw.text))
            .Select(x => new LoginReq() { name = x.Item1, pw = x.Item2 })
            .Select(req => JsonConvert.SerializeObject(req) )
            .SelectMany(json => HttpUtility.PutWWW(LOGIN, json))
            .Select(json => JsonConvert.DeserializeObject<LoginResp>(json))
            .Where(resp => resp.result == (int)ResultType.OK)  
            .Do(resp => UpdateInfo(resp))
            .Do(_ => ToastMgr.SetToast("登入中"))
            .Do(_ => SceneManager.LoadSceneAsync("Mora", LoadSceneMode.Single ))
            .Subscribe()
            .AddTo(this.gameObject);
            */

        // 新式實驗體1.
        /*
        LoginBtn.OnClickAsObservable()
            .Select(_ => Tuple.Create(Name.text, Pw.text))
            .Select(x => new LoginReq() { name = x.Item1, pw = x.Item2 })
            .Select(req => JsonConvert.SerializeObject(req))
            .Do(json => StartCoroutine(HttpUtility.Put<string>(LOGIN, json, (x) => { UpdateInfo(x); }  )))           
            .Do(_ => ToastMgr.SetToast("登入中"))
            .Do(_ => SceneManager.LoadSceneAsync("Mora", LoadSceneMode.Single))
            .Subscribe()
            .AddTo(this.gameObject);
            */

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
     
        /*
        RegistBtn.OnClickAsObservable()
            .Select(_ => Tuple.Create(Name.text, Pw.text))
            .Select(x => new RegisterReq() { name = x.Item1, pw = x.Item2 })
            .Select(req => JsonConvert.SerializeObject(req))
            .Do(json => StartCoroutine( HttpUtility.Put<string>(REGIST, json, null)))
            .Subscribe()
            .AddTo(this.gameObject);
            */


        /*
        RegistBtn.OnClickAsObservable()
            .Select(_ => Tuple.Create(Name.text, Pw.text))
            .Select(x => new RegisterReq() { name = x.Item1, pw = x.Item2 })
            .Select(req => JsonConvert.SerializeObject(req))
            .SelectMany(json => HttpUtility.PutWWW(REGIST, json))
            .Select(json => JsonConvert.DeserializeObject<RegisterResp>(json))
            .Where(resp => resp.result == (int)ResultType.OK)
            .Do(_ => ToastMgr.SetToast("註冊成功"))
            .Subscribe()
            .AddTo(this.gameObject);

        RegistBtn.OnClickAsObservable()
            .Select(_ => Tuple.Create(Name.text, Pw.text))
            .Select(x => new RegisterReq() { name = x.Item1, pw = x.Item2 })
            .Select(req => JsonConvert.SerializeObject(req))
            .SelectMany(json => HttpUtility.PutWWW(REGIST, json))
            .Select(json => JsonConvert.DeserializeObject<RegisterResp>(json))
            .Where(resp => resp.result != (int)ResultType.OK)
            .Do(_ => ToastMgr.SetToast("註冊失敗"))
            .Subscribe()
            .AddTo(this.gameObject);
            */

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
