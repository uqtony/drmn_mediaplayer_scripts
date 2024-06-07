using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebSetting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //GetComponent<UniWebView>().Alpha = 0.5f;   
        GetComponent<UniWebView>().BackgroundColor = new Color(0.0f,0.0f,0.0f,0.0f);
        GetComponent<UniWebView>().SetUserInteractionEnabled(true);
        
    }

    public void HideWeb()
    {
        GetComponent<UniWebView>().Hide(true);
    }
    public void ShowWeb()
    {
        GetComponent<UniWebView>().Show(true);
    }
}
