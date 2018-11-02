using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ToastMgr
{
    static Toast toast;
    public static void SetToast(string msg)
    {
        if (toast == null) toast = GameObject.FindObjectOfType<Toast>();
        toast.SetToast(msg);
    }
}
