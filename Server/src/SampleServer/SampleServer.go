package main

import (
	"Utility/session"
	"encoding/json"
	"fmt"
	"log"
	"math/rand"
	"net/http" //"github.com/gin-contrib/cors"

	"github.com/gorilla/mux"
)

type RegisterReq struct {
	Name string `json:"name"`
	Pw   string `json:"pw"`
}
type RegisterResp struct {
	Result int `json:"result"`
}
type LoginReq struct {
	Name string `json:"name"`
	Pw   string `json:"pw"`
}
type LoginResp struct {
	Result int    `json:"result"`
	Name   string `json:"name"`
	Token  string `json:"token"`
	Money  int    `json:"money"`
}

type MoraResp struct {
	Result  int    `json:"result"`
	Mora    int    `json:"mora"`
	GameRes int    `json:"gameRes"`
	Win     int    `json:"win"`
	Draw    int    `json:"draw"`
	Lose    int    `json:"lose"`
	Name    string `json:"name"`
	Top     int    `json:"top"`
	Money   int    `json:"money"`
}

type MoraReq struct {
	//Name  string `json:"name"`
	Index int `json:"index"`
	Bet   int `json:"bet"`
}

// Aid = Account, 唯一值.
type UserInfo struct {
	Aid   string `json:"aid"`
	Name  string `json:"name"`
	Pw    string `json:"pw"`
	Win   int    `json:"win"`
	Draw  int    `json:"draw"`
	Lose  int    `json:"lose"`
	Money int    `json:"money"`
	Token string `json:"Token"`
}

type LoginInfo struct {
	//Token string `json:"token"`
	Aid  string `json:"aid"`
	Name string `json:"name"`
}

var Cnt int = 0

const (
	Win  int = 0
	Lose int = 1
	Draw int = 2
)

const (
	RES_OK    int = 0
	COND_ERR  int = 1
	TOKEN_ERR int = 2
	ERROR     int = 3
)

// Map, Key=aid, Value=使用者資料.
var UserDB map[string]UserInfo

// Map, Key=最後一次上線Token, Value=登入者資料.
var LoginDB map[string]LoginInfo
var sessionMgr *Helper.SessionMgr = nil

func main() {

	// create Map Data.
	UserDB = make(map[string]UserInfo)
	LoginDB = make(map[string]LoginInfo)

	var name = "SampleServer Start!!!"
	// Cookie的Name由Server建立,帶Client第二次送來封包時,才會擁有Cookie Name.
	sessionMgr = Helper.NewSessionMgr("Mora_Game", 3600)
	fmt.Println(name)
	router := mux.NewRouter().StrictSlash(true)

	//router.Methods("GET", "POST")
	//router.Headers("X-Requested-With", "XMLHttpRequest")
	router.HandleFunc("/", index)
	router.HandleFunc("/login", Login)       //.Methods("PUT")
	router.HandleFunc("/register", Register) //.Methods("PUT")
	router.HandleFunc("/mora", MoraGame)
	//router.HandleFunc("/todos/{todoId}", MoraGame)
	/*
		router.Use(cors.New(cors.Options{
			AllowedOrigins:   []string{"*"},
			AllowCredentials: true,
		}))
	*/
	log.Fatal(http.ListenAndServe(":8080", router))
}
func index(w http.ResponseWriter, r *http.Request) {
}
func Login(w http.ResponseWriter, r *http.Request) {
	var req LoginReq
	json.NewDecoder(r.Body).Decode(&req)

	// 取得session的方式,從Server生成或從封包取得.
	var sessionID = sessionMgr.CheckCookieValid(w, r)
	if sessionID == "" {
		http.Redirect(w, r, "/login", http.StatusFound)
		sessionID = sessionMgr.StartSession(w, r)
	}
	/*
		var sessionID string
		if Cookie, err := r.Cookie("session"); err != nil || Cookie == nil || Cookie.Value == "" {
			sessionID = sessionMgr.StartSession(w, r)
		} else {
			sessionID = Cookie.Value
			http.SetCookie(w, Cookie)
		}
	*/

	// 先從Name和PW取出資訊.
	if info, find := GetUser(req.Name, req.Pw); find {

		user := info.(UserInfo)
		// 剔除重複登入的 .
		var onlineSessionIDList = sessionMgr.GetSessionIDList()
		for _, onlineSessionID := range onlineSessionIDList {
			if info, ok := sessionMgr.GetSessionVal(onlineSessionID, "UserInfo"); ok {
				if value, ok := info.(UserInfo); ok {
					if value.Aid == user.Aid {
						sessionMgr.EndSessionBy(onlineSessionID)
					}
				}
			}
		}

		// 更新Token, 將登入資料寫入session表單內.
		user.Token = sessionID
		sessionMgr.SetSessionVal(sessionID, "UserInfo", user)

		resp := &LoginResp{
			Name:   user.Name,
			Result: RES_OK,
			Money:  user.Money,
			Token:  sessionID,
		}

		json.NewEncoder(w).Encode(resp)
		fmt.Println("Req Init, Name:%s, RESULT = %d", req.Name, resp.Result)
	} else {

		json.NewEncoder(w).Encode(LoginResp{Result: ERROR})
	}

}
func Register(w http.ResponseWriter, r *http.Request) {
	var req RegisterReq
	json.NewDecoder(r.Body).Decode(&req)

	// can't find , create new data to save.
	if _, find, _ := GetUserData(req.Name); !find {

		// 取得session的方式,從Server生成或從封包取得.
		/*
			var sessionID string
			if Cookie, _ := r.Cookie("session"); Cookie.Value == "" {
				sessionID = sessionMgr.StartSession(w, r)
			} else {
				sessionID = Cookie.Value
			}
		*/

		// 註冊一定是從Server生成Token,不理會註冊封包裡面是否有Token.
		sessionID := sessionMgr.StartSession(w, r)
		var accountID = req.Name + sessionID
		//userInfo := UserInfo{Name: req.Name, Pw: req.pw, Money: 10000, Aid: accountID}

		// 找的到使用者(aid)資料就刪除,避免Aid占用.
		if _, ok, _ := GetUserData(accountID); ok {
			DelUserData(accountID)
		}

		// user to Login
		// 找不到登入資料才正常.
		if _, ok := GetLoginData(sessionID); ok {
			// 刪除SessionID和登入資料.
			DelLoginData(sessionID)
		}

		if ok := SetLoginData(sessionID, LoginInfo{Name: req.Name, Aid: accountID}); ok {
			fmt.Println("SetLoginData , Token:%s, aid = %s", sessionID, accountID)
		}

		// add User to DB
		AddUserData(accountID, req.Name, req.Pw)

		// resp packet.
		json.NewEncoder(w).Encode(RegisterResp{Result: RES_OK})
		fmt.Println("RegisterReq , Name:%s, Result = %d", req.Name, RES_OK)

	} else {
		// find  data, can't Register User data.
		json.NewEncoder(w).Encode(RegisterResp{Result: ERROR})
		fmt.Println("RegisterReq , Name:%s, Result = %d", req.Name, ERROR)
	}
}

func MoraGame(w http.ResponseWriter, r *http.Request) {
	fmt.Println("Mora Req Connect")

	// 每次動作都要判斷SessionID的合法性.
	var sessionID = sessionMgr.CheckCookieValid(w, r)
	if sessionID == "" {
		http.Redirect(w, r, "/login", http.StatusFound)
		json.NewEncoder(w).Encode(MoraResp{Result: TOKEN_ERR})
		return
	}

	// http put的資料轉class.
	var req MoraReq

	json.NewDecoder(r.Body).Decode(&req)
	if info, find := sessionMgr.GetSessionVal(sessionID, "UserInfo"); find {

		userInfo := info.(UserInfo)
		// 邏輯條件判斷(game Cond).
		index := rand.Intn(3)
		gameResult := Compare(req.Index, index)

		// update GameData.
		if gameResult == Win {
			userInfo.Win++
			userInfo.Money += req.Bet
		} else if gameResult == Draw {
			userInfo.Draw++
		} else if gameResult == Lose {
			userInfo.Lose++
			userInfo.Money -= req.Bet
		}

		// Update to DB or Data.
		SetUserData(sessionID, userInfo)

		// set resp Data, and class convert Json.
		data := &MoraResp{
			Result:  RES_OK,
			GameRes: gameResult,
			Mora:    index,
			Money:   userInfo.Money,
			Win:     userInfo.Win,
			Draw:    userInfo.Draw,
			Lose:    userInfo.Lose,
		}

		fmt.Printf("req Index = %d, NPC Select =%d, Resp, Result = %d, Money = %d\n", req.Index, index, data.Result, data.Money)
		json.NewEncoder(w).Encode(data)
	}

}

func GetUserData(aid string) (UserInfo, bool, error) {

	// check user's data in Map.

	if data, ok := UserDB[aid]; ok {
		return data, true, nil
	}

	//UserList[token] = UserInfo{Name: name, Money: 1000}
	return UserInfo{}, false, nil
}

func GetUser(name string, pw string) (interface{}, bool) {

	for _, value := range UserDB {
		if value.Name == name && value.Pw == pw {
			return value, true
		}
	}

	return nil, false
}

func AddUserData(aid string, name string, pw string) {
	UserDB[aid] = UserInfo{
		Money: 10000,
		Name:  name,
		Aid:   aid,
		Pw:    pw,
	}
}
func SetUserData(aid string, user UserInfo) bool {

	if _, ok := UserDB[aid]; ok {
		UserDB[aid] = user
		return true
	}

	return false
}

func DelUserData(aid string) {
	delete(UserDB, aid)
}

func SetLoginData(token string, info LoginInfo) bool {

	if _, ok := LoginDB[token]; ok {
		LoginDB[token] = info
		return true
	}

	LoginDB[token] = info
	return false
}
func GetLoginData(token string) (LoginInfo, bool) {
	if _, ok := LoginDB[token]; ok {
		return LoginDB[token], true
	}

	return LoginInfo{}, false
}

func DelLoginData(token string) {
	delete(LoginDB, token)
}

func Compare(a1, a2 int) int {

	if a1 == a2 {
		return Draw
	}
	if a1 == (a2 + 1) {
		return Win
	}
	if a1 == 0 && a2 == 2 {
		return Win
	}

	return Lose
}
func TodoIndex(w http.ResponseWriter, r *http.Request) {
	fmt.Fprintln(w, "Todo Index!")
}

func TodoShow(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	todoId := vars["todoId"]
	fmt.Fprintln(w, "Todo show:", todoId)
}
