using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Mora
{    
    public class Mora : MonoBehaviour
    {
        public string LOGIN = "http://127.0.0.1:8080/";
        public string REGIST = "http://127.0.0.1:8080/";
        public string BET = "http://127.0.0.1:8080/mora";

        enum PlayType : int
        {
            USER = 0,
            NPC,
        }

        enum Result : int
        {
            WIN = 0,
            LOSE,
            DRAW
        }

        enum MoraType : int
        {
            Cloth = 0,  // 布.
            Shears,     // 剪刀.
            Stone,      // 石頭.
            MAX,
        }

        

        [SerializeField] Text[] PlayerTxt;
        [SerializeField] Image[] Icon;
        [SerializeField] Text[] ResultTxt;
        [SerializeField] Animator[] UserAnim;
        [SerializeField] Button[] SelectBtn;
        [SerializeField] Button ResetBtn;

        [SerializeField] Text[] ResultNum;
        [SerializeField] Text BetNum;
        [SerializeField] Text MoneyNum;

        // Use this for initialization
        void Start()
        {            

            BetNum.text = 100.ToString();           

            // 定義按鈕的功能.
            for (int i = 0; i < SelectBtn.Length; i++)
            {                
                SelectBtn[i].OnClickAsObservableGo(i)
                    .Select(x => new MoraReq { index = x.Item2, bet = int.Parse(BetNum.text), name = PlayerTxt[(int)PlayType.USER].text })
                    .Select(req => JsonConvert.SerializeObject(req))    // 將class轉成Json.                
                    .SelectMany(json => HttpUtility.Put(BET, json))     // 送出Http請求,並等待回應.
                    .Select(x => JsonConvert.DeserializeObject<MoraResp>(x))    // 將Http回應的字串轉成class.
                    .Do(resp => setResult(resp))   // 將返回的類別資料給結果.
                    .Subscribe()
                    .AddTo(this.gameObject);
            }

            // Init.           
            Init(UserInfoMgr.Instance.name, UserInfoMgr.Instance.money);                      
        }
       
        /// <summary>
        /// 初始化(姓名和金錢).
        /// </summary>
        /// <param name="name">名稱</param>
        /// <param name="money">金錢</param>
        void Init(string name, long money)
        {
            PlayerTxt[(int)PlayType.USER].text = name;
            MoneyNum.text = money.ToString();
        }      

        /// <summary>
        /// 猜拳比較結果.
        /// </summary>
        /// <param name="type1">A玩家</param>
        /// <param name="type2">B玩家</param>
        /// <returns></returns>
        Result Compare(MoraType type1, MoraType type2)
        {
            if (type1 == type2) return Result.DRAW;
            if (type1 == type2 + 1) return Result.WIN;
            if (type1 == MoraType.Cloth && type2 == MoraType.Stone) return Result.WIN;
            return Result.LOSE;
        }


        /// <summary>
        /// 設定結果.
        /// </summary>
        /// <param name="resp">猜拳結果Resp</param>
        void setResult(MoraResp resp)
        {
            ResultTxt.ToList().ForEach(obj => obj.gameObject.SetActive(true));
            UserAnim[(int)PlayType.NPC].Play(((MoraType)resp.mora).ToString().ToLower());
            ResetBtn.gameObject.SetActive(true);
            ResultNum[(int)Result.WIN].text = resp.win.ToString();
            ResultNum[(int)Result.LOSE].text = resp.lose.ToString();
            ResultNum[(int)Result.DRAW].text = resp.draw.ToString();
            MoneyNum.text = resp.money.ToString();

            if (resp.result == (int)Result.DRAW)
            {
                ResultTxt.ToList().ForEach(obj => obj.text = "DRAW");             
                return;
            }

            ResultTxt[(int)PlayType.USER].text = (resp.result == (int)Result.WIN) ? "WIN" : "LOSE";
            ResultTxt[(int)PlayType.NPC].text = (resp.result == (int)Result.WIN) ? "LOSE" : "WIN";
            string msg = (resp.result == (int)Result.WIN) ? string.Format("+{0}", BetNum.text) : string.Format("-{0}", BetNum.text);
            ToastMgr.SetToast(msg);
        }

        /// <summary>
        ///  重製遊戲狀態.
        /// </summary>
        public void Reset()
        {
            UserAnim.ToList().ForEach(obj => obj.Play("wait"));
            SelectBtn.ToList().ForEach(obj => obj.interactable = true);
            ResultTxt.ToList().ForEach(obj => obj.gameObject.SetActive(false));
            ResetBtn.gameObject.SetActive(false);
        }
    }	
}
