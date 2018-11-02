using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UniRx;

/// <summary>
/// Button的擴充函式.
/// </summary>
public static class ButtonUtility
{
    /// <summary>
    /// 連結按鈕要掛勾的動作(函式).
    /// </summary>
    /// <param name="btn"></param>
    /// <param name="proc">欲執行的動作</param>
    /// <returns>成功連結返回true</returns>
    public static bool Attach(this Button btn, System.Action proc)
    {
        if (proc == null) return false;

        btn.OnClickAsObservable().Subscribe(_ => proc.Invoke());
        return true;
    }

    /// <summary>
    /// 移除按鈕執行的動作.
    /// </summary>
    /// <param name="btn"></param>
    public static void Detach(this Button btn)
    {
        btn.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 按鈕的Observable, 會帶一個自身物件引數,以及一個數字.
    /// </summary>
    /// <param name="button">按鈕物件本身</param>
    /// <param name="index">欲帶入地的數值</param>
    /// <returns></returns>
    public static IObservable<Tuple<GameObject,int>> OnClickAsObservableGo(this Button button, int index)
    {
        return Observable.Return( Tuple.Create(button,index))
            .SelectMany(x => x.Item1.OnClickAsObservable(), (x, current) => Tuple.Create(x.Item1.gameObject,x.Item2));
    }

    /// <summary>
    /// 按鈕的Observable, 會帶一個自身物件引數.
    /// </summary>
    /// <param name="button"></param>
    /// <returns></returns>
    public static IObservable<GameObject> OnClickAsObservableGo(this Button button)
    {
        return Observable.Return(button)
            .SelectMany(x => x.OnClickAsObservable(), (x, current) => x.gameObject);
    }

    /// <summary>
    /// Toggle的Observable, 會帶一個自身物件引數.
    /// </summary>
    /// <param name="toggle"></param>
    /// <returns></returns>
    public static IObservable<GameObject> OnClickAsObservableGo(this Toggle toggle)
    {
        return Observable.Return(toggle)
            .SelectMany(x => x.OnValueChangedAsObservable(), (x, current) => x.gameObject);
    }

    /// <summary>
    /// 判斷滑鼠目前是否在物件範圍內.
    /// </summary>
    /// <param name="rtf">欲擴充的物件.</param>
    /// <returns>是否在本物件範圍內</returns>
    public static bool IsInside(this RectTransform rtf)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rtf, GetMouseInScreen());
    }

    /// <summary>
    /// 取得目前滑鼠在螢幕上的座標位置,僅限於本專案使用.
    /// </summary>
    /// <returns></returns>
    public static Vector3 GetMouseInScreen()
    {
        for (int j = 0; j < Camera.allCameras.Length; j++)
        {
            var camera = Camera.allCameras[j];
            if (camera == null) continue;

            return (camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 100))); //滑鼠           
        }

        return new Vector3();
    }
}
