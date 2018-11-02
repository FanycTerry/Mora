using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class Toast : MonoBehaviour {

    [SerializeField] GameObject Root;
    [SerializeField] Text Msg;

    // Use this for initialization
    void Start () {
        DontDestroyOnLoad(this.gameObject);
	}
	
    public void SetToast(string msg)
    {
        Observable.Return(msg)
            .Do(x => this.Msg.text = x)
            .Do(_ => this.Root.SetActive(true))
            .Delay(TimeSpan.FromSeconds(3.0f))
            .Do(_ => this.Root.SetActive(false))
            .Do(_ => this.Msg.text = "")
            .Subscribe()
            .AddTo(this.gameObject);        
    }
	
}
