using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

public class HttpUtility : Singleton<HttpUtility>
{    
    static string token = "";
    static string CookieName = "Mora_Game";
    static string SessionName = "session";    

    /// <summary>
    ///  取得UnityWebRequest的Token.
    /// </summary>
    /// <param name="req"></param>
    /// <returns></returns>
    public static string GetToken(UnityWebRequest req)
    {
        // 當沒有Session時，Server會建立Session,從Http回來的資料取得Session.
        // 之後都從session來取得.
        string header = req.GetResponseHeader("Set-Cookie");
        if (header == null) return "";

        var msg = header.Split(';') ;
        Dictionary<string, string> cookies = new Dictionary<string, string>();
        foreach (var item in msg)
        {
            string[] param = item.Split('=');
            if (param.Length > 1) cookies.Add(param[0], param[1]);
        }

        string token = "";
        if (cookies.ContainsKey(CookieName)) token = cookies[CookieName];
        //else if (cookies.ContainsKey(SessionName)) token = cookies[SessionName];
        return token;
    }

    /// <summary>
    /// Unity的UnityWebRequest(WWW)Rx串流.
    /// 會自動判斷Header是否有Session，有的話會記錄，並送出(Server會根據Http裡的Header的SessionID來判斷是哪個玩家).
    /// </summary>
    /// <param name="url">WWW想要Put的網址(API位置)</param>
    /// <param name="json">欲Put的資料</param>
    /// <returns>會返回一個字串Json</returns>
    public static IObservable<string> Put(string url, string json)
    {       
        UnityWebRequest www = UnityWebRequest.Put(url, Encoding.UTF8.GetBytes(json));
        www.SetRequestHeader("Cookie", string.Format("{0}={1}", CookieName, token));
        www.method = UnityWebRequest.kHttpVerbPOST;

        return Observable.Return(www)
            .SelectMany(x => x.SendWebRequest().AsAsyncOperationObservable())
            .Do(req => SetToken(GetToken(req.webRequest)))
            .Do(x => Debug.LogFormat("isNetworkError={0}, ", x.webRequest.isNetworkError))
            .Select(x => x.webRequest.downloadHandler.text)
            .Do(x => Debug.LogFormat("Json={0}",x));
    }
    /// <summary>
    /// 設定WWW的SessionID(或稱Token或Cookie).
    /// 若設置一個空值，將不會紀錄.
    /// </summary>
    /// <param name="session">SessionID字串</param>
    public static void SetToken(string session )
    {
        if (session == null || session.Length == 0) return;

        token = session;
    }             
}
