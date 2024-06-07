using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DASUAPI;
using System;
using Unity.VisualScripting;

public class ScreenshotManager : MonoBehaviour
{
    int sequence = 1;
    public IEnumerator TakeScreenshot()
    {
        sequence = 1;
        yield return new WaitForEndOfFrame();

        //截圖
        Texture2D screenshotTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshotTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshotTexture.Apply();

        // 將截圖以PNG的方式存成byte[]
        byte[] bytes = screenshotTexture.EncodeToJPG();
        //File.WriteAllBytes(new PathRelated().ScreenshotRootPath(), bytes);
        //寫入本機
        //sequence++;
        //轉成Base64存上雲端
        string textureBase64 = Convert.ToBase64String(bytes);
        GameObject.Find("Manager").GetComponent<ManagerCtrl>().SendJPGToServer(sequence,textureBase64);

        // 刪除纹理
        Destroy(screenshotTexture);
    }
    /// <summary>
    /// 這邊是等著當截圖結束一輪，就要被呼叫
    /// </summary>
    public void ResetSequence()
    {
        sequence = 1;
    }
}
