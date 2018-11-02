using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mora
{
    /// <summary>
    /// 註冊請求.
    /// </summary>
    public class RegisterReq
    {
        public string name;
        public string pw;
    }
    /// <summary>
    /// 註冊回應.
    /// </summary>
    public class RegisterResp
    {        
        public int result;
    }
    /// <summary>
    /// 登入請求.
    /// </summary>
    public class LoginReq
    {
        public string name;
        public string pw;
    }
    /// <summary>
    /// 登入回應.
    /// </summary>
    public class LoginResp
    {
        public int result;
        public string name;
        public string token;
        public int money;
    }
    /// <summary>
    /// 遊戲-猜拳(下注)請求.
    /// </summary>
    public class MoraResp
    {
        public int result;
        public int mora;
        public int gameRes;
        public int win;
        public int draw;
        public int lose;
        public string name;
        public int top;
        public int money;
    }
    /// <summary>
    /// 遊戲-猜拳(下注)回應.
    /// </summary>
    public class MoraReq
    {
        public string name;
        public int index;
        public int bet;
    }
    /// <summary>
    /// 使用者資訊.
    /// </summary>
    public class UserInfo
    {
        public string name;
        public int win;
        public int draw;
        public int lose;
        public int top;
        public int money;
    }
}
