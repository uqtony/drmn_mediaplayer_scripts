using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using DASUAPI;
public class GetAndroidFileFolder : MonoBehaviour
{
    //E95F-6370 MyUSB
    //Android Internal Root Folder Path = "/storage/emulated/0/";
    public static string usbSourcesFolderPath;
    bool _usbPlugin;
    string materialFolderPath;
    /// <summary>
    /// Set&GetUSB Plugin or false
    /// </summary>
    public bool usbPluginStatus
    {
        get { return _usbPlugin; }

        set
        {
            _usbPlugin = value;
        }

    }

    void Awake()
    {
        materialFolderPath = new PathRelated().MaterialsFolderPath("DownloadMaterials");
        SetUSBMaterialRootPathAndPluginStatus();
        //如果不是android or win 就關閉程式
        //Application.Quit();
    }
    private IEnumerator Start()
    {
        if (usbPluginStatus || Application.platform != RuntimePlatform.Android)
        {
            StartCoroutine(CheckFiles());
        }
        //這邊之後可以插入一個 資源複製的動態效果
        yield return new WaitForEndOfFrame();
        //.....................//這段記得要打開，如果是寫USB讀取版本
        GetComponent<OfflinePlayMaterialManager>().StartToPlay() ;
    }

    /// <summary>
    /// 根據平台式win or Android 設定Sources資料夾路徑以及隨身碟是否插入
    /// </summary>
    void SetUSBMaterialRootPathAndPluginStatus()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            //取得隨身碟的名稱
            string s = GetUSBFolderName();

            if (s != "沒有隨身碟")
            {
                usbPluginStatus = true;
                //指定來源在隨身碟的Sources資料夾下
                usbSourcesFolderPath = "/storage/" + s + "/Sources";
            }
            else
            {
                usbPluginStatus = false;
            }
        }
        else
        {
            //如果是電腦版的，素材的目錄會在Application DataPath/Sources資料夾下
            usbSourcesFolderPath = Application.dataPath + "/Sources";
        }
    }



    /// <summary>
    /// 獲取USB的機碼，但是只排除emulated and mnt的，如果插入兩個隨身碟以上，會回傳最後一個隨身碟機碼
    /// </summary>
    string GetUSBFolderName()
    {
        string[] androidfoldersNames = Directory.GetDirectories("/storage");
        for (int i = 0; i < androidfoldersNames.Length; i++)
        {
            if (androidfoldersNames[i].Contains("storage"))
            {
                string s = "";
                if (!androidfoldersNames[i].Contains("emulated") && !androidfoldersNames[i].Contains("mnt") && !androidfoldersNames[i].Contains("self"))
                {
                    s = androidfoldersNames[i].Replace("/storage/", "");
                    return s;
                }

            }
        }
        return "沒有隨身碟";
    }
    /// <summary>
    /// 確認APP路徑下是否有Huanding資料夾，沒有就產生一個
    /// </summary>
    /// <returns></returns>
    IEnumerator CheckFiles()
    {
        //確認APP路徑下是否有DownloadMaterials資料夾，沒有就產生一個
        if (!Directory.Exists(materialFolderPath))
        {
            Directory.CreateDirectory(materialFolderPath);
        }
        yield return new WaitForEndOfFrame();

        CopyFile(usbSourcesFolderPath, materialFolderPath);
    }

    void CopyFile(string sourceDir, string destinationDir)
    {
        //確認USB Sources資料夾路徑在不在
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists)
        {
            Debug.Log("USB裡沒有Sources資料夾");
            return;
        }
        var sourceDirFiles = new DirectoryInfo(usbSourcesFolderPath);
        foreach (FileInfo file in sourceDirFiles.GetFiles())
        {
            string targetFilePath = Path.Combine(materialFolderPath, file.Name);
            /*if (File.Exists(targetFilePath))
            {
                //詢問是否複寫檔案
            }
            else
            {

            }*/
            if (!file.Name.StartsWith("."))
                file.CopyTo(targetFilePath, true);//遇到相同檔名，直接覆寫。
                                              //Debug.Log(file.Length);
                                              //Debug.Log(file.LastWriteTime);
                                              //Debug.Log(file.FullName);
        }
    }
    /*private void Update()
    {
        times += Time.deltaTime;
        if (times > 1.0f)
        {
            infoText2.text = "系統記憶體 : " + SystemInfo.systemMemorySize + " , 圖型記憶體：" + SystemInfo.graphicsMemorySize;

        }

    }*/
    //Environment.getExternalStoragePublicDirectory("").toString()  //"/storage/emulated/0"
    //string filePath = "/storage/E95F-6370/USB/text33.txt";

    //File.AppendAllText(filePath, "AAAAA");

}
