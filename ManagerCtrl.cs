using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DASUAPI;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManagerCtrl : MonoBehaviour
{
    public string appVersion = "1.0";
    public static string materialFolderPath;
    //string url = "http://211.21.158.77:66/api/Playlist/";
    string url = "http://test.drmn.net:88/api/Playlist/";
    //string registerURL = "http://211.21.158.77:66/api/Register";
    string registerURL = "http://test.drmn.net:88/api/Register";
    string accessKey = "Hos3AjfoT8O36Xsglp0J2auxmB3Y9Y2fv9Y";
    //string heartbeatURL = "http://211.21.158.77:66/api/Heartbeat/";
    string heartbeatURL = "http://test.drmn.net:88/api/Heartbeat/";
    //string deviceURL = "http://211.21.158.77:66/api/Device";
    string deviceURL = "http://test.drmn.net:88/api/Device";
    //string screenshotURL = "http://211.21.158.77:66/api/DeviceScreenshot";
    string screenshotURL = "http://test.drmn.net:88/api/DeviceScreenshot";
    
    // 後台http://211.21.158.77:88/Device/List
    // APIhttp://211.21.158.77:88/Device/List
    string getDataURL;

    bool registered;
    string registerKey = "";
    public SystemProfile systemProfile;
    public ActivityObj[] activityObjs;
    public Text SystemStatus;
    /// <summary>
    /// 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_registerKey,7_volume
    /// </summary>
    string[] deviceRelatedInfo = new string[8];
    public Text deviceRelatedInfoText;

    bool locationProcessed = false;
    bool checkInternet = true;
    bool internetConnectStatus;
    public bool startPlay;
    /// <summary>
    /// 只給推撥判斷用的，避免一直在Update裡面一直跑
    /// </summary>
    public bool updatePlayList;
    public bool updatingPlayList;

    //string tempMac = "D4:12:43:6C:DE:62";
    //string tempMac = "44:91:60:6c:DE:D9";
    //string tempMac = "CC:4B:73:F5:0A:C0";
    //string tempMac = "C4:BD:E5:24:DE:7F";
    string tempMac = "18:84:C1:01:D9:73";
    //string tempMac = "D4:9C:DD:14:8B:A2";
    int playSequence;
    int maxRequestTimes = 20;
    
    public static string notificationTopic;
    public static string notificationProjectID;
    public static string notificationBody;
    public static string notificationTitle;
    public static string notificationToken;

    public Text notificationTopicText;
    public Text notificationProjectIDText;
    public Text notificationTitleText;
    public Text notificationBodyText;
    public Text notificationTokenText;
    public Text checkPlayListTimeText;
   
    bool heartBeat = true;
    bool isStopheartBeat = true;
    bool isDownloading;
    float downloadProgress;
    UnityWebRequest getFileRequest;
    string downloadMaterialID;
    public Slider downloadSlider;

    public Animator registerCanvasAnim;
    public Button registerBtn;
    public Text registerText;

    //以下是WEB相關控制
    public GameObject webObj;

    //public GameObject uniWebView;
    //int i = 0;
    private void Awake()
    {
        //防止休眠
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        //RuninBackGround
        Application.runInBackground = true;
        materialFolderPath = new PathRelated().MaterialsFolderPath("DownloadMaterials");
    }
    IEnumerator Start()
    {
        //先判斷本機是否有DownloadMaterial的資料夾
        StartCoroutine(CreateDownloadMaterialsFolder());
        yield return new WaitForEndOfFrame();

        //取得DeviceUUID..................
        deviceRelatedInfo[0] = new GetDeciceInfos().DeviceUID;
        //取得MacAdress..................
        string macAdress = new GetDeciceInfos().MacAdress();
        if (macAdress == "AA:BB:CC:DD:EE")
            deviceRelatedInfo[1] = "無法取得";
        else
            deviceRelatedInfo[1] = macAdress;
        //開始CheckInternectConnection
        StartCoroutine(CheckInternet());
        yield return new WaitForSeconds(3.0f);
        deviceRelatedInfo[2] = internetConnectStatus == true ? "連線中" : "失去網路連線";
        //取得ScreenSize..............
        deviceRelatedInfo[4] = new GetDeciceInfos().DeviceScreenInfo();

        // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
        SetDeviceRelatedInfo();
        //先將音量調到最大聲
        deviceRelatedInfo[7] = "1";
        SetDeviceVolume();

        ReadSystemProfile();
        
        InitialSetting();
        //如果發現沒有註冊，後面就都不用執行了，直接等待註冊後，然後再重新載入場景一次
        if(!registered)
            yield break;

        //取得播放清單，如果有就直接更新本機的清單
        yield return StartCoroutine(GetPlayList());

        //只是去讀本機目前的清單，如果沒有清單，一樣break
        if(!ReadPlayList())
            yield break;
        //一旦有清單之後，不管是本機的還是熱騰騰的，都去檢查Material是否存在，接著設定撥放清單
        StartCoroutine(CheckFileExists());

        DateTime currentTime = DateTime.Now;
        TimeSpan timeUntilMidnight = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0).AddDays(1) - currentTime;
        TimeSpan timeUntilNextHour = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour + 1, 0, 0) - currentTime;
        
        Invoke("ReloadScene", (float)timeUntilMidnight.TotalSeconds);
        InvokeRepeating("CheckListFromTime", (float)timeUntilNextHour.TotalSeconds, 3600); // 3600秒是一小时的秒数

        webObj.SetActive(true);
    }
    /// <summary>
    /// 檢查是否有SystemProfile且可以被正確解析
    /// </summary>
    public void ReadSystemProfile()
    {
        if (!File.Exists(new PathRelated().SystemProfilePath))
        {
            SystemStatus.text = "本機沒有系統檔案資料";
            //跳出機器註冊的畫面
            GameObject.Find("RegisterCanvas").GetComponent<Animator>().SetBool("showRegister", true);
            registered = false;
            return;
        }

        string jsonText = File.ReadAllText(new PathRelated().SystemProfilePath);
        //即使有檔案，也要檢查是否有能夠被正確解析
        systemProfile = new SystemProfile();
        try
        {
            //檔案可以正確被解析，但每個參數有沒有值，就還不知道唷
            systemProfile = JsonUtility.FromJson<SystemProfile>(jsonText);
            registerKey = systemProfile.RegisterKey;
            deviceRelatedInfo[6] = registerKey;
            notificationToken = systemProfile.Token;
            deviceRelatedInfo[7] = systemProfile.Volume;
            SetDeviceRelatedInfo();
        }
        catch (Exception e)
        {
            SystemStatus.text = "本機儲存的Profile資料有誤，請重新註冊。錯誤代碼:" + e.Message;
            GameObject.Find("RegisterCanvas").GetComponent<Animator>().SetBool("showRegister", true);
            registered = false;
            return;
        }
    }
    /// <summary>
    /// 是否有Token,如果有就要傳給Server；如果沒有Key，就代表這台機器尚未註冊過，需要註冊
    /// </summary>
    void InitialSetting()
    {
        if (systemProfile == null)
        {
            GameObject.Find("RegisterCanvas").GetComponent<Animator>().SetBool("showRegister", true);
            registered = false;
            return;
        }
        if (string.IsNullOrWhiteSpace(registerKey))
        {
            //跳出機器註冊的畫面
            SystemStatus.text = "本機儲存的RegisterKey有誤，請重新註冊";
            GameObject.Find("RegisterCanvas").GetComponent<Animator>().SetBool("showRegister", true);
            registered = false;
            return;
        }
        else
        {
            registered = true;
        }
        if (!string.IsNullOrWhiteSpace(notificationToken) && !string.IsNullOrWhiteSpace(registerKey))
        {
            //因為Token太長了，所以簡單呈現前10碼
            
            StartCoroutine(SendTokenToServer(notificationToken));
        }
        if (!string.IsNullOrWhiteSpace(deviceRelatedInfo[7]))
        {
            //直接根據讀進來的值，轉換音量
            SetDeviceVolume();
        }
    }
    /// <summary>
    /// 傳送Token給Server
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    IEnumerator SendTokenToServer(string token)
    {
        WWWForm form = new WWWForm();
        form.AddField("Mac", registerKey);
        form.AddField("Version", appVersion);
        form.AddField("Token", token);

        // 創建請求
        UnityWebRequest request = UnityWebRequest.Post(deviceURL, form);

        // 發送請求
        yield return request.SendWebRequest();

        // 檢查請求結果
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            SystemStatus.text = request.error.ToString();
        }
        else
        {
            SystemStatus.text = request.downloadHandler.text == "OK" ? "已成功傳送Token" : "Token沒有回傳成功喔";
            Invoke("ClearDownloadStatusText", 5);
        }
    }
    /// <summary>
    /// 根據網路連線與否取得清單，如果有連線就上網去抓並存下來，但還沒擷取資料，僅顯示接下來的清單來源，沒有設定updating的狀態
    /// </summary>
    IEnumerator GetPlayList()
    {
        if (internetConnectStatus)
        {
            yield return StartCoroutine(GetPlayListDataFromServer(result => {
               
                if (result.Contains("GetDataError:"))
                {
                    deviceRelatedInfo[3] = "清單取得失敗，所以撥放本地既有清單。錯誤訊息 : " +result ;
                    SystemStatus.text = "清單取得失敗，所以撥放本地既有清單";
                    // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
                    SetDeviceRelatedInfo();
                }
                else if (result == "NoPlayList")
                {
                    deviceRelatedInfo[3] = "今日無清單可撥放，所以撥放本地既有清單";
                    SystemStatus.text = "今日無清單可撥放，所以撥放本地既有清單";
                    // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
                    SetDeviceRelatedInfo();
                }
                else if(result == "ScuuessGetPlayList")
                {
                    deviceRelatedInfo[3] = "播放清單來源:熱騰騰剛抓下來的";
                    SystemStatus.text = "已取得撥放清單";
                    // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
                    SetDeviceRelatedInfo();
                }
            }));
        }
        else
        {
            deviceRelatedInfo[3] = "網路沒有連線，無法取得清單，所以撥放本地既有清單";
            SystemStatus.text = "網路沒有連線，無法取得清單，所以撥放本地既有清單";
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
        }
        
    }
    /// <summary>
    /// 取得播放清單From Server，如果有，直接存下來，，沒有設定updating的狀態
    /// </summary>
    /// <param name="getDataURL"></param>
    /// <returns></returns>
    IEnumerator GetPlayListDataFromServer(Action<string> callback)
    {
        //getDataURL = url + tempKey;
        getDataURL = url + registerKey;

        // 建立UnityWebRequest物件
        UnityWebRequest request = UnityWebRequest.Get(getDataURL);

        // 送出Request並等待回應
        yield return request.SendWebRequest();

        // 檢查是否有錯誤
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            callback("GetDataError:" + request.error);
        }
        else
        {
            //Debug.Log(request.downloadHandler.text);
            // 取得回應的資料
            //如果是空陣列，我也不修改本機的清單
            string jsonResponse = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(jsonResponse) || jsonResponse == "[]")
            {
                callback("NoPlayList");
                yield break;
            }
            try
            {   //更新現在本機的PlayList Json，如果上面的if拿掉，會寫入空陣列，這樣如果今天是空清單，就會沒有東西撥放
                File.WriteAllText(new PathRelated().PlayListFilePath, jsonResponse);
                callback("ScuuessGetPlayList");
            }
            catch
            {
                SystemStatus.text = "這台機器尚未註冊，好像是這樣辣....不然就是還沒有素材可以播放。Get" + jsonResponse;
                callback("NoPlayList");
            }

        }

    }
    /// <summary>
    /// 讀取本機的撥放清單，放入到ActiveObjs
    /// </summary>
    bool ReadPlayList()
    {
        string localJosnPath = new PathRelated().PlayListFilePath;
        if (File.Exists(localJosnPath))
        {
            string jsonData = File.ReadAllText(localJosnPath);
            if(string.IsNullOrWhiteSpace(jsonData) || jsonData == "[]")
            {
                SystemStatus.text = "本機既有清單是空的唷";
                updatingPlayList = false;
                return false;
            }
            try
            {
                // 將JSON資料轉換為物件；因為Unity無法解析複雜的Json資料，所以用JsonHelper
                activityObjs = JsonHelper.FromJson<ActivityObj>(jsonData);
                return true;
            }
            catch (Exception e)
            {
                SystemStatus.text = "本機既有清單格式錯誤。錯誤代碼: " + e.Message;
                updatingPlayList = false;
                return false;
            }
        }
        else
        {
            SystemStatus.text = "本機沒有已存的撥放清單";
            updatingPlayList = false;
            return false;
        }
    }
    //把所有File下載下來
    IEnumerator CheckFileExists()
    {
        if (activityObjs.Length == 0)
        {
            SystemStatus.text = "確認檔案時，發生錯誤:ActivityObj = 0";
            Invoke("ClearDownloadStatusText", 5);
            yield break;
        }
        foreach (ActivityObj activityObj in activityObjs)
        {
            string url = activityObj.filePath.ToLower();
            string savePath = "";
            if (url.Contains(".jpg"))
                savePath = materialFolderPath + "/" + activityObj.materialId + "jpg.jpg"; // 儲存路徑
            else if (url.Contains(".png"))
                savePath = materialFolderPath + "/" + activityObj.materialId + "png.png"; // 儲存路徑
            else if (url.Contains(".mp4"))
                savePath = materialFolderPath + "/" + activityObj.materialId + "mp4.mp4"; // 儲存路徑

            if (File.Exists(savePath))
            {
                //Debug.Log("本地已經有這個檔案了，我就不下載囉～");
                SystemStatus.text = "本地已經有這個檔案了，我就不下載囉";
                continue;
            }
            isDownloading = true;
            downloadSlider.gameObject.SetActive(true);
            //這裡送出一定要用activityObj.filePath，不能用url，因為有被改過小寫
            getFileRequest = UnityWebRequest.Get(activityObj.filePath);
            downloadMaterialID = activityObj.materialId.ToString();

            // 送出Request並等待回應
            yield return getFileRequest.SendWebRequest();

            if (getFileRequest.result == UnityWebRequest.Result.ConnectionError || getFileRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                SystemStatus.text = getFileRequest.error + "下載" + activityObj.materialId + "時發生錯誤";
                //Debug.LogError(request.error);
            }
            else
            {
                // 將下載的資料寫入本地檔案
                File.WriteAllBytes(savePath, getFileRequest.downloadHandler.data);
            }
        }
        isDownloading = false;
        downloadSlider.gameObject.SetActive(false);
        SystemStatus.text = "都下載完了喔，能不能播我不知道嘿";
        Invoke("ClearDownloadStatusText", 3);
        StartCoroutine(SetPlayList());
    }

    /// <summary>
    /// 設備相關數據狀態顯示,deviceRelatedInfo[]
    /// </summary>
    public void SetDeviceRelatedInfo()
    {
        deviceRelatedInfoText.text = "Device ID : " + deviceRelatedInfo[0] + "\n\n" +
                                     "Mac Adress : " + deviceRelatedInfo[1] + "\n\n" +
                                     "Internect Connection Status : " + deviceRelatedInfo[2] + "\n\n" +
                                     "PlayList is from : " + deviceRelatedInfo[3] + "\n\n" +
                                     "Device Screen Size : " + deviceRelatedInfo[4] + "\n\n" +
                                     "Device Location : " + deviceRelatedInfo[5] + "\n\n" + 
                                     "Register Key : " + deviceRelatedInfo[6] + "\n\n"+
                                     "System Volume : " + deviceRelatedInfo[7];
    }
   
    IEnumerator HeartBeat()
    {
        while (heartBeat)
        {
            isStopheartBeat = false;
            UnityWebRequest request = UnityWebRequest.Get(heartbeatURL + registerKey);
           
            yield return request.SendWebRequest();

            // 檢查是否有錯誤
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                SystemStatus.text = request.error;
                //Debug.LogError(request.error);
                yield return new WaitForSeconds(10.0f);
            }
            else
            {
                //Debug.Log(request.result);
                yield return new WaitForSeconds(180.0f);
            } 
        }
        isStopheartBeat = true;
    }
    /// <summary>
    /// //////////////////////////////////////////////////////////////////
    /// </summary>
    private void Update()
    {
        /*if(Input.touchCount == 3)
        {
            uniWebView.SetActive(false);
        }
        else if (Input.touchCount == 4)
        {
            uniWebView.SetActive(true);
        }*/////////////////////////////////////////////////////////////////////////////////////////
        
        notificationTopicText.text = notificationTopic;
        notificationProjectIDText.text = "Received a new message from: " + notificationProjectID;
        notificationTitleText.text = "Last Time Message Title: " + notificationTitle;
        notificationBodyText.text = "Last Time Message body: " + notificationBody;
        notificationTokenText.text = "This Device Token is " + notificationToken;
        if (!registered)
            return;
        if (startPlay)
        {
            GetComponent<PlayMaterialManager>().noMaterialCanPlay = false;
            GetComponent<PlayMaterialManager>().CheckHaveLegalMaterial();
            startPlay = false;
        }
        if (updatePlayList)
        {
            updatePlayList = false;
            StartCoroutine(UpdateList());
        }
        if (internetConnectStatus)
        {
            heartBeat = true;
            if (isStopheartBeat)
            {
                StartCoroutine(HeartBeat());
                isStopheartBeat = false;
            }
               
        }
        else
        {
            heartBeat = false;
            isStopheartBeat= true;
        }

        if (isDownloading)
        {
            downloadProgress = getFileRequest.downloadProgress;
            // 在这里你可以使用downloadProgress来更新进度条或显示下载百分比
            //Debug.Log(downloadMaterialID + "下載進度: " + (downloadProgress * 100).ToString("F2") + "%");
            SystemStatus.text = downloadMaterialID + "下載進度: " + (downloadProgress * 100).ToString("F2") + "%";
            downloadSlider.value = downloadProgress;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            StartCoroutine(GameObject.Find("GlobalManager").GetComponent<FirebaseController>().ScreenShot());
        }
        
    }
    /// <summary>
    /// 由Update觸發，當updatePlayList = true 時，會啟動一次
    /// </summary>
    /// <returns></returns>
    IEnumerator UpdateList()
    {
        if (updatingPlayList)
        {
            checkPlayListTimeText.text = "CheckPlaylist : 沒有更新，因為剛好遇到在更新。 " + DateTime.Now.ToString();
            yield break;
        }
        updatingPlayList = true;
        //去網路存取清單，如果有資料，就會直接存一份新到本地
        yield return StartCoroutine(GetPlayList());
        //去讀本機目前的清單，如果沒有清單，一樣break，到此為止
        if (!ReadPlayList())
            yield break;
        //如果有準確的讀取到清單，就會開始確認素材檔案
        StartCoroutine(CheckFileExists());
    }
    void CheckListFromTime()
    {
        if (updatingPlayList)
        {
            //Debug.Log("CheckPlaylist : 沒有更新，因為剛好遇到正在推撥更新");
            checkPlayListTimeText.text = "CheckPlaylist : 整點沒有更新，因為剛好遇到正在推撥更新。 " + DateTime.Now.ToString();
            return;
        }
        StartCoroutine(CheckPlaylist()); 
    }
    /// <summary>
    /// 檢查是否有DownloadMaterial的資料夾
    /// </summary>
    /// <returns></returns>
    IEnumerator CreateDownloadMaterialsFolder()
    {
        //檢查是否有DownloadMaterials的資料夾
        if (!Directory.Exists(materialFolderPath))
        {
            Directory.CreateDirectory(materialFolderPath);
            yield return new WaitForEndOfFrame();
        }
    }
    public void DeleteMaterialsFolderFiles()
    {
        new PathRelated().ClearMaterialsFiles();
        
        //這邊先暫時用Reload場景來代替重新開始
        ReloadScene();
    }
    
    /// <summary>
    /// 根據Json字串，建立播放清單
    /// </summary>
    IEnumerator SetPlayList()
    {
        startPlay = false;
        GetComponent<PlayMaterialManager>().NoMaterialCanPlay();
        yield return new WaitForEndOfFrame();

        GetComponent<PlayMaterialManager>().fileDictionary = new Dictionary<int, string>();
        GetComponent<PlayMaterialManager>().TexturePlayTimeDictionary = new Dictionary<int, int>();
        GetComponent<PlayMaterialManager>().PlayConstrainDictionary = new Dictionary<int, string[]>();
        // 使用資料...
        foreach (ActivityObj activityObj in activityObjs)
        {
            string _path = activityObj.filePath.ToLower();
            //Debug.Log(_path);
//            Debug.Log(activityObj.materialID);
            //名字後目前要加上JPG或PNG或mp4是為了知道我接下來要播的是圖片還是影片
            if (_path.Contains(".jpg"))
            {
                GetComponent<PlayMaterialManager>().fileDictionary.Add(playSequence, activityObj.materialId + "jpg");
                GetComponent<PlayMaterialManager>().TexturePlayTimeDictionary.Add(playSequence, activityObj.playSeconds);
                GetComponent<PlayMaterialManager>().PlayConstrainDictionary.Add(playSequence, new string[] { activityObj.beginDate, activityObj.endDate, activityObj.status.ToString()});
               
            }

            else if (_path.Contains(".png"))
            {
                GetComponent<PlayMaterialManager>().fileDictionary.Add(playSequence, activityObj.materialId + "png");
                GetComponent<PlayMaterialManager>().TexturePlayTimeDictionary.Add(playSequence, activityObj.playSeconds);
                GetComponent<PlayMaterialManager>().PlayConstrainDictionary.Add(playSequence, new string[] { activityObj.beginDate, activityObj.endDate, activityObj.status.ToString() });
            }

            else if (_path.Contains(".mp4"))
            {
                GetComponent<PlayMaterialManager>().fileDictionary.Add(playSequence, activityObj.materialId + "mp4");
                GetComponent<PlayMaterialManager>().TexturePlayTimeDictionary.Add(playSequence, activityObj.playSeconds);
                GetComponent<PlayMaterialManager>().PlayConstrainDictionary.Add(playSequence, new string[] { activityObj.beginDate, activityObj.endDate, activityObj.status.ToString() });
            }

            playSequence++;
        }
        playSequence = 0;
        startPlay = true;
        updatingPlayList = false;
    }
    /// <summary>
    /// 整點去要清單的時，先檢查是否需要更新，然後會由update裡面的updatePlayList這個bool，決定啟動更新的流程
    /// </summary>
    IEnumerator CheckPlaylist()
    {
        if (updatingPlayList)
        {
            yield break;
        }
        if (internetConnectStatus)
        {
            yield return StartCoroutine(GetPlayListDataFromServer(result => {

                if (result.Contains("GetDataError:"))
                {
                    deviceRelatedInfo[3] = "整點清單取得失敗，所以撥放本地既有清單。錯誤訊息 : " + result;
                    SystemStatus.text = "整點清單取得失敗，所以撥放本地既有清單";
                    checkPlayListTimeText.text = "CheckPlaylist : " + DateTime.Now.ToString() +"__"+ SystemStatus.text;
                }
                else if (result == "NoPlayList")
                {
                    deviceRelatedInfo[3] = "整點無清單可撥放，所以撥放本地既有清單";
                    SystemStatus.text = "整點無清單可撥放，所以撥放本地既有清單";
                    checkPlayListTimeText.text = "CheckPlaylist : " + DateTime.Now.ToString() + "__" + SystemStatus.text;
                }
                else if(result == "ScuuessGetPlayList")
                {
                    if (File.Exists(new PathRelated().PlayListFilePath))
                    {
                        string jsonText = File.ReadAllText(new PathRelated().PlayListFilePath);
                        ActivityObj[] checkActivitys;
                        try
                        {
                            checkActivitys = JsonHelper.FromJson<ActivityObj>(jsonText);
                            if (checkActivitys.Length != activityObjs.Length)
                            {
                                //需要更新
                                deviceRelatedInfo[3] = "播放清單來源:整點確認清單時，有變動故已取得新的撥放清單";
                                SystemStatus.text = "播放清單來源:整點確認清單時，有變動故已取得新的撥放清單";
                                checkPlayListTimeText.text = "CheckPlaylist : " + DateTime.Now.ToString() + "__" + SystemStatus.text;
                                updatePlayList = true;
                            }
                            else
                            {
                                for (int i = 0; i < checkActivitys.Length; i++)
                                {
                                    if (activityObjs[i].materialId != checkActivitys[i].materialId)
                                    {
                                        deviceRelatedInfo[3] = "播放清單來源:整點確認清單時，有變動故已取得新的撥放清單";
                                        SystemStatus.text = "播放清單來源:整點確認清單時，有變動故已取得新的撥放清單";
                                        checkPlayListTimeText.text = "CheckPlaylist : " + DateTime.Now.ToString() + "__" + SystemStatus.text;
                                        updatePlayList = true;
                                        //這邊一定要break，不然迴圈會一直跑，但是其實也沒差，也或許有差
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            checkPlayListTimeText.text = "CheckPlaylist : " + DateTime.Now.ToString() + "__" + "未知錯誤";
                            Debug.Log(ex.Message);
                        }
                       
                    }
                }
            }));
        }
        else
        {
            deviceRelatedInfo[3] = "網路沒有連線，整點時無法取得清單，所以撥放本地既有清單";
            SystemStatus.text = "網路沒有連線，整點時無法取得清單，所以撥放本地既有清單";
            checkPlayListTimeText.text = "CheckPlaylist : " + DateTime.Now.ToString() + "__" + SystemStatus.text;
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
        }
    }

    public void RegisterDevice()
    {
        StartCoroutine(DeviceRegister());
    }
    IEnumerator DeviceRegister()
    {
        registerBtn.interactable = false;
        string latitude = "0.0";
        string longitude = "0.0";
        
        if (!Input.location.isEnabledByUser)
        {
            deviceRelatedInfo[5] = "Location not enabled on device or app does not have permission to access location\n GPS will send 0,0";
            //因為即使抓不到，我還是要往下進行註冊，所以locationProcrssed要變成true
            locationProcessed = true;
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
        }
        else
        {
            Input.location.Start();
            yield return new WaitForEndOfFrame();
            yield return StartCoroutine(GetDeviceLocatioInfo((result) => {
                latitude = result[0].ToString();
                longitude = result[1].ToString();
            }));
        }
        yield return new WaitUntil(() => locationProcessed);
        WWWForm form = new WWWForm();
        string key = registerKey == "" ? new GetDeciceInfos().MacAdress(): registerKey; 
        form.AddField("AccessKey", accessKey);
        form.AddField("Mac", key);
        form.AddField("Lat", latitude);
        form.AddField("Lng", longitude);
        form.AddField("ResolutionW", Screen.width);
        form.AddField("ResolutionH", Screen.height);
        // 創建請求
        UnityWebRequest request = UnityWebRequest.Post(registerURL, form);

        // 發送請求
        yield return request.SendWebRequest();

        // 檢查請求結果
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            SystemStatus.text = request.error;
            registerText.text = "發生未知錯誤，請重新註冊";
            locationProcessed = false;
            yield return new WaitForSeconds(3.0f);
        }
        else
        {
            if(request.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log(request.downloadHandler.text);
                registerKey = request.downloadHandler.text;
                SystemStatus.text = "已註冊成功";
                registerText.text = "已註冊成功，軟體將重新啟動";
                WriteSystemProfile();
                yield return new WaitForEndOfFrame();
            }
            
        }
        Invoke("ClearDownloadStatusText", 5);
        locationProcessed = false;
        yield return new WaitForSeconds(3.0f);
        //為了傳送Token給Server，因為註冊前，Server尚未有Mac碼，所以即使取得Token，送過去也無法送，又因為重新載入場景，會先去讀取看看有沒有Token資料，如果有就會直接傳給Server
        ReloadScene();
    }


    IEnumerator GetDeviceLocatioInfo(Action<string[]> callback)
    {
        // Waits until the location service initializes
        deviceRelatedInfo[5] = "偵測Location中，請稍候....";
        // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
        SetDeviceRelatedInfo();
        int maxWait = 10;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 60 seconds this cancels location service use.
        if (maxWait < 1)
        {
            deviceRelatedInfo[5] = "Location Get Timed out, GPS will send 0,0";
            string[] locationData = new string[2];
            locationData[0] = "0";
            locationData[1] = "0";
            callback(locationData);
            locationProcessed = true;
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
            yield break;
        }
        yield return new WaitForSeconds(1.0f);
        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            deviceRelatedInfo[5] = "Unable to determine device location GPS will send 0,0";
            string[] locationData = new string[2];
            locationData[0] = "0";
            locationData[1] = "0";
            callback(locationData);
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
            locationProcessed = true;
            yield break;
        }
        else
        {
            string[] locationData = new string[2];
            locationData[0] = Input.location.lastData.latitude.ToString();
            locationData[1] = Input.location.lastData.longitude.ToString();
            callback(locationData);
            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
            deviceRelatedInfo[5] = "Location: " + "latitude is "+ Input.location.lastData.latitude + ", longitude is " + Input.location.lastData.longitude + ", altitude is " + Input.location.lastData.altitude + ",\n horizontalAccuracy is " + Input.location.lastData.horizontalAccuracy + ", verticalAccuracy is " + Input.location.lastData.verticalAccuracy + ", timestamp is " + Input.location.lastData.timestamp;
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
        }
        locationProcessed = true;
        // Stops the location service if there is no need to query location updates continuously.
        Input.location.Stop();
    }

    /// <summary>
    /// 取得GPS的位置
    /// </summary>
    /// <param name="btn"></param>
    public void UpdateGPSInfo(Button btn)
    {
        if (!Input.location.isEnabledByUser)
        {
            deviceRelatedInfo[5] = "Location not enabled on device or app does not have permission to access location\n GPS will is 0,0";
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
        }
        else
        {
            StartCoroutine(UpdateDeviceLocatioInfoToShow(btn));
        }
    }
    IEnumerator UpdateDeviceLocatioInfoToShow(Button btn)
    {
        btn.interactable = false;
        deviceRelatedInfo[5] = "偵測Location中，請稍候....";
        // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
        SetDeviceRelatedInfo();
        Input.location.Start();
        yield return new WaitForEndOfFrame();
        // Waits until the location service initializes
        int maxWait = 10;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 60 seconds this cancels location service use.
        if (maxWait < 1)
        {
            deviceRelatedInfo[5] = "Location Get Timed out";
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
            btn.interactable = true;
            yield break;
        }
        yield return new WaitForSeconds(1.0f);
        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            deviceRelatedInfo[5] = "Unable to determine device location";
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
            btn.interactable = true;
            yield break;
        }
        else
        {
            deviceRelatedInfo[5] = "Location: " + "latitude is " + Input.location.lastData.latitude + ", longitude is " + Input.location.lastData.longitude + ", altitude is " + Input.location.lastData.altitude + ",\n horizontalAccuracy is " + Input.location.lastData.horizontalAccuracy + ", verticalAccuracy is " + Input.location.lastData.verticalAccuracy + ", timestamp is " + Input.location.lastData.timestamp;
            // 0_deviceID,1_deviceMac,2_internectConnect,3_playListFrom,4_deviceScreenSize,5_deviceLocation,6_RegisteryKey
            SetDeviceRelatedInfo();
        }

        // Stops the location service if there is no need to query location updates continuously.
        Input.location.Stop();
        btn.interactable = true;
    }
    void ClearDownloadStatusText()
    {
        SystemStatus.text = "";
    }
    public void ReloadScene()
    {
        GetComponent<PlayMaterialManager>().NoMaterialCanPlay();
        CancelInvoke();
        StopAllCoroutines();
        SceneManager.LoadScene(0);
    }
    
    public void WriteSystemProfile()
    {
        SystemProfile _systemProfile = new SystemProfile();
        _systemProfile.Token = notificationToken;
        _systemProfile.RegisterKey = registerKey;
        _systemProfile.AppVersion = appVersion;
        _systemProfile.Volume = deviceRelatedInfo[7];
        string jsonText = JsonUtility.ToJson(_systemProfile);
        File.WriteAllText(new PathRelated().SystemProfilePath, jsonText);
    }


    public void SendJPGToServer(int sequence, string textureBase64)
    {
        StartCoroutine(IESendJPGToServer(sequence, textureBase64));
    }
    IEnumerator IESendJPGToServer(int sequence, string textureBase64)
    {
        if (string.IsNullOrWhiteSpace(registerKey))
        {
            SystemStatus.text = "註冊序號是空的，無法上傳截圖";
            yield break;
        }
            
        int requestTimes = 0;
        WWWForm form = new WWWForm();
        form.AddField("Ticket", notificationBody);
        form.AddField("Mac", registerKey);
        //timeStamp = Convert.ToInt32(DateTimeOffset.Now.ToUnixTimeSeconds());
        //form.AddField("ScreenshotId", timeStamp);
        form.AddField("Sequence", sequence);
        form.AddField("Screenshot", textureBase64);
        while (requestTimes <= maxRequestTimes)
        {
            // 創建請求
            UnityWebRequest request = UnityWebRequest.Post(screenshotURL, form);
            // 發送請求
            yield return request.SendWebRequest();

            // 檢查請求結果
            if (request.result != UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                requestTimes++;
                if (requestTimes <= maxRequestTimes)
                {
                    //Debug.LogError("Retry Upload because Screenshot Response: error " + request.error);
                    SystemStatus.text = "Retry Upload because Screenshot Response: error " + request.error.ToString();
                    yield return new WaitForSeconds(3.0f);
                }
                else
                {
                    //Debug.LogError("Retry Upload because Screenshot Response: error " + request.error);
                    SystemStatus.text = "氣死我了，傳不上去，不想傳了 =.= ";
                    Invoke("ClearDownloadStatusText", 5);
                    yield break;
                }    
            }
            else
            {
                Debug.Log("Screenshot Response: " + request.downloadHandler.text);
                SystemStatus.text = request.downloadHandler.text == "OK" ? "已成功傳送截圖" : "截圖可能沒有回傳成功喔";
                Invoke("ClearDownloadStatusText", 5);
                yield break;
            }
        }
            
    }
    /// <summary>
    /// 檢查網路是否連線，如果要終止判斷，要去把checkInternet改成False
    /// </summary>
    private IEnumerator CheckInternet()
    {
        while (checkInternet)
        {
            using (UnityWebRequest www = new UnityWebRequest("http://www.google.com"))
            {
                www.method = UnityWebRequest.kHttpVerbHEAD; // 使用HEAD请求，不需要下载页面内容

                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    if (internetConnectStatus)
                    {
                        deviceRelatedInfo[2] = "失去網路連線";
                        SetDeviceRelatedInfo();
                    }
                    internetConnectStatus = false;
                }
                else
                {
                    if (!internetConnectStatus)
                    {
                        deviceRelatedInfo[2] = "連線中";
                        SetDeviceRelatedInfo();
                    }
                    internetConnectStatus = true;
                }
            }
            yield return new WaitForSeconds(30.0f);
        }
    }
    public void ShowRegisterWindow()
    {
        if (registered)
            registerText.text = "重新註冊機器，請點擊註冊按鈕";
        else
        {
            registerText.text = "此機器尚未註冊，請點擊註冊按鈕";
        }
        registerCanvasAnim.SetBool("showRegister", true);
    }
    public void CloseRegisetrWindow()
    {
        registerCanvasAnim.SetBool("showRegister", false);
    }
    public void SetDeviceVolume(string globalVolume)
    {
        if (float.TryParse(globalVolume, out float _globalVolume))
        {
            _globalVolume = _globalVolume / 10.0f;
            deviceRelatedInfo[7] = _globalVolume.ToString();
            SetDeviceRelatedInfo();
            WriteSystemProfile();
            GetComponent<PlayMaterialManager>().mediaPlayer1.AudioVolume = _globalVolume;
            GetComponent<PlayMaterialManager>().mediaPlayer2.AudioVolume = _globalVolume;
        }
       
    }
    public void SetDeviceVolume()
    {
        if (float.TryParse(deviceRelatedInfo[7], out float _globalVolume))
        {
            GetComponent<PlayMaterialManager>().mediaPlayer1.AudioVolume = _globalVolume;
            GetComponent<PlayMaterialManager>().mediaPlayer2.AudioVolume = _globalVolume;
        }

    }

    private void OnDisable()
    {
        heartBeat = false;
        checkInternet = false;
        StopAllCoroutines();
    }
    [Serializable]
    public class SystemProfile
    {
        public string Token;
        public string RegisterKey;
        public string AppVersion;
        public string Volume;
    }
    [Serializable]
    public class ActivityObj
    {
        public int deviceId;
        public string devicedName;
        public string mac;
        public int postId;
        public string postName;
        public string beginDate;
        public string endDate;
        public string status;
        public int materialId;
        public string materialName;
        public string type;
        public int width;
        public int height;
        public string filePath;
        public float fileSize;
        public string md5;
        public int playSeconds;
    }
}
