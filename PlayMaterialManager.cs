using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using RenderHeads.Media.AVProVideo;
using Unity.VisualScripting;

public class PlayMaterialManager : MonoBehaviour
{
    public Dictionary<int, string> fileDictionary;
    int currentSort = 0;
    bool preparedMediaPlayer1;
    bool playMediaPlayer1;
    public Dictionary<int,Sprite> spriteDictionary;
    public Dictionary<int, int> TexturePlayTimeDictionary;
    //int 是播放順序 string[]是時間起訖還有Status:play, stop
    public Dictionary<int, string[]> PlayConstrainDictionary;
    //int 是播放順序，主要是拿來檢查還有幾個素材可以撥放，然後是第幾個素材
    public List<int> canPlaySequenceList = new List<int>();

    public Image ima;
    public DisplayUGUI displayUGUI;
    public MediaPlayer mediaPlayer1;
    public MediaPlayer mediaPlayer2;
    public Text playTime;
    float timer;
    bool caculateTime;
    float texturePlayTime;
    bool playVideo;
    public Button nextMaterialBtn;
    public bool screenShot;
    public int screenshotCount;
    public bool noMaterialCanPlay = true;
    public GameObject alwalsShowIma;
    List<int> earlyMaterialSequence = new List<int>();
    bool materialwaitTimeToPlay;
    public Text mediaPlayerStatusText1;
    public Text mediaPlayerStatusText2;
    private void Awake()
    {
        mediaPlayer1.GetComponent<MediaPlayer>().Events.AddListener(HandleEventMediaPlayer1);
        mediaPlayer2.GetComponent<MediaPlayer>().Events.AddListener(HandleEventMediaPlayer2);
        mediaPlayer1.PlatformOptionsAndroid.videoApi = Android.VideoApi.MediaPlayer;
        mediaPlayer2.PlatformOptionsAndroid.videoApi = Android.VideoApi.MediaPlayer;

    }
    /// <summary>
    /// 檢查素材庫是否有起碼有一個素材可以播放，避免後續進入無限迴圈
    /// </summary>
    public void CheckHaveLegalMaterial()
    {

        //確保如果沒有素材可以撥放，就不會往下執行
        if (fileDictionary.Count <= 0)
        {
            return;
        }
        //先設定所有委刊單的數量等於可以撥放的總數量，之後到後面的邏輯會一一判斷，而減少
        foreach(var dict in fileDictionary)
        {
            canPlaySequenceList.Add(dict.Key);
        }

        alwalsShowIma.SetActive(false);
        InitialPrepareMaterials();  
    }
    /// <summary>
    /// CheckHaveLegalMaterial若有符合條件，則開始準備素材
    /// </summary>
    public void InitialPrepareMaterials()
    {
        PrepardNextMaterial();
        //把所有圖片轉成Sprite
        ToSprite();
        PlayProgressManager();
    }
    
    //主要播放邏輯
    public void PlayProgressManager()
    {
        if (noMaterialCanPlay)
            return;
        if (fileDictionary[currentSort].Contains("mp4"))
        {
            PlayVideo();
            playVideo = true;
        }
        else
        {
            PlayIma();
            playVideo = false;
        }
        currentSort++;
        if (currentSort > fileDictionary.Count - 1)
            currentSort = 0;
        PrepardNextMaterial();
    }
    /// <summary>
    /// 準備下一個素材
    /// </summary>
    public void PrepardNextMaterial()
    {
        //素材播放期間，如果不在合法時間內，就一直找下一個不斷循環
        /*if ((DateTime.Now < CaculateDateTime(PlayConstrainDictionary[currentSort])[0]) || DateTime.Now > CaculateDateTime(PlayConstrainDictionary[currentSort])[1] || PlayConstrainDictionary[currentSort][2] == "Stop")
        {
            Debug.Log("播放日期已經過期了辣辣辣");
            canPlayVolume--;
            if (canPlayVolume == 0)
            {
                NoMaterialCanPlay();

                return;
            }

            currentSort++;
            if (currentSort > fileDictionary.Count - 1)
                currentSort = 0;

            PrepardNextMaterial();
            
        }*/
        if (DateTime.Now.TimeOfDay < CaculateDateTime(PlayConstrainDictionary[currentSort])[0].TimeOfDay)
        {
            //Debug.Log(currentSort + "___播放時間太早了辣辣辣");
            earlyMaterialSequence.Add(currentSort);
            materialwaitTimeToPlay = true;
            if (canPlaySequenceList.Contains(currentSort))
            {
                canPlaySequenceList.Remove(currentSort);
            }
            if (canPlaySequenceList.Count == 0)
            {
                //Debug.Log("HasMaterialCanPlay" + currentSort + "___播放時間太早了辣辣辣");
                HasMaterialCanPlay();
                return;
            }
            currentSort++;
            if (currentSort > fileDictionary.Count - 1)
                currentSort = 0;

            PrepardNextMaterial();
        }
        else if (DateTime.Now.TimeOfDay > CaculateDateTime(PlayConstrainDictionary[currentSort])[1].TimeOfDay)
        {
            //Debug.Log(currentSort + "___播放時間已經過了時間的限制了辣");
            //如果有不合法的物件，就刪除canPlaySequenceList，直到總數等於0的時候就跳出迴圈
            if (canPlaySequenceList.Contains(currentSort))
            {
                canPlaySequenceList.Remove(currentSort);
            }
            if (canPlaySequenceList.Count == 0)
            {
                if (materialwaitTimeToPlay)
                {
                    //Debug.Log("HasMaterialCanPlay" + currentSort + "___播放時間已經過了時間的限制了辣");
                    GetComponent<ManagerCtrl>().SystemStatus.text = "現已經沒有素材可以撥放了捏，因為有的素材時間還沒到撥放起始時間";
                    HasMaterialCanPlay();
                }

                else
                {
                    //Debug.Log("noMaterialCanPlay" + currentSort + "___播放時間已經過了時間的限制了辣");
                    GetComponent<ManagerCtrl>().SystemStatus.text = "已經沒有素材可以撥放了捏";
                    NoMaterialCanPlay();
                }
                    
                return;
            }
            currentSort++;
            if (currentSort > fileDictionary.Count - 1)
                currentSort = 0;

            PrepardNextMaterial();
        }
        else
        {
            //如果是影片必須事先準備，如果是圖片就不必了
            if (fileDictionary[currentSort].Contains("mp4"))
            {
                PrepareVideo(preparedMediaPlayer1, currentSort);
                //Debug.Log("準備影片" + playMediaPlayer1);
            }
        }
        //主要是不想讓使用者一直連續按，避免出錯
        if(!nextMaterialBtn.interactable)
            nextMaterialBtn.interactable = true;
    }
    public void NextMaterial()
    {
        if (noMaterialCanPlay)
            return;
        //主要是不想讓使用者一直連續按，避免出錯
        nextMaterialBtn.interactable = false;
        if (playVideo)
        {
            if (playMediaPlayer1)
            {
                mediaPlayer1.Stop();
               
            }
            else
            {
                mediaPlayer2.Stop();
                
            }
            playVideo = false;
        }
        else
        {
            timer = 0;
            caculateTime = false;
            ima.sprite = null;
        }
        PlayProgressManager();
    }
    void PlayIma()
    {
        texturePlayTime = TexturePlayTimeDictionary[currentSort];
        displayUGUI.gameObject.SetActive(false);
        ima.gameObject.SetActive(true);
        ima.sprite = spriteDictionary[currentSort];
        caculateTime = true;

        /*if (screenShot)
        {
            GameObject.Find("GlobalManager").GetComponent<ScreenshotManager>().CaptureScreenshot();
            CheckScreenshotCount();
        }*/
    }
    void CloseIma()
    {
        ima.sprite = null;
        PlayProgressManager();
    }

    void PlayVideo()
    {
        if (IsInvoking("CloseIma"))
            CancelInvoke("CloseIma");
        ima.gameObject.SetActive(false);
        displayUGUI.gameObject.SetActive(true);
        if (!playMediaPlayer1)
        {
            playMediaPlayer1 = true;
            displayUGUI.CurrentMediaPlayer = mediaPlayer1;
            mediaPlayer1.Play();
            mediaPlayer2.Stop();
            mediaPlayer2.CloseMedia();
            //Debug.Log("PlayVideo1");
        }
        else
        {
            playMediaPlayer1 = false;
            displayUGUI.CurrentMediaPlayer = mediaPlayer2;
            mediaPlayer2.Play();
            mediaPlayer1.Stop();
            mediaPlayer1.CloseMedia();
            //Debug.Log("PlayVideo2");
        }
        //StartCoroutine(CheckVideoisPlaying());
    }

    /*IEnumerator CheckVideoisPlaying()
    {
        yield return new WaitForSeconds(3.0f);
        if (screenShot)
        {
            GameObject.Find("GlobalManager").GetComponent<ScreenshotManager>().CaptureScreenshot();
            CheckScreenshotCount();
        }
    }*/
    void PrepareVideo(bool _preparedMediaPlayer1, int sort)
    {

        if (!_preparedMediaPlayer1)
        {
            preparedMediaPlayer1 = true;
            mediaPlayer1.OpenMedia(MediaPathType.AbsolutePathOrURL, ManagerCtrl.materialFolderPath + "/" + fileDictionary[sort] + ".mp4", false);
        }

        else
        {
            preparedMediaPlayer1 = false;
            mediaPlayer2.OpenMedia(MediaPathType.AbsolutePathOrURL, ManagerCtrl.materialFolderPath + "/" + fileDictionary[sort] + ".mp4", false);
        }
            
    }

    void ToSprite()
    {
        spriteDictionary = new Dictionary<int, Sprite>();
        foreach (KeyValuePair<int, string> file in fileDictionary)
        {
            if (file.Value.Contains("jpg") || file.Value.Contains("png"))
            {
                byte[] imageBytes;
                if (file.Value.Contains("jpg"))
                    imageBytes = File.ReadAllBytes(ManagerCtrl.materialFolderPath + "/" + fileDictionary[file.Key]+".jpg");
                else if (file.Value.Contains("png"))
                    imageBytes = File.ReadAllBytes(ManagerCtrl.materialFolderPath + "/" + fileDictionary[file.Key]+".png");
                else
                    return;
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes);
                Sprite _sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                spriteDictionary.Add(file.Key, _sprite);
            }
        }
    }
    private void Update()
    {
        playTime.text = "已經播放：" + Time.time.ToString("0.0")+"秒";
        if (caculateTime) {
            timer += Time.deltaTime;
            if(timer > texturePlayTime)
            {
                caculateTime = false;
                timer = 0;
                CloseIma();
            }
        }
        
        if (materialwaitTimeToPlay)
        {
            CheckEarlyMaterialDtaeTime();
        }
    }
    /// <summary>
    /// 主要是判斷尚未抵達撥放時間的素材，是否已達起始時間，可以開始撥放了
    /// </summary>
    void CheckEarlyMaterialDtaeTime()
    {
        foreach(int sequence in earlyMaterialSequence)
        {
            if (!materialwaitTimeToPlay)
                return;
            //Debug.Log("CheckSequenceMaterialDateTime" + sequence);
            if (DateTime.Now.TimeOfDay > CaculateDateTime(PlayConstrainDictionary[sequence])[0].TimeOfDay)
            {
                //把時間達到起始時間的素材，從earlyMaterialSequence中移除
                //並且要將canPlaySequenceList的Sequence加回去，若不加回去，在計算是否還有素材可撥放時，會出錯
                earlyMaterialSequence.Remove(sequence);
                if (!canPlaySequenceList.Contains(sequence))
                    canPlaySequenceList.Add(sequence);
                //因為如果不加下面這個限制，萬一再沒多久之後，又有素材符合條件，會在執行一次InitialPrepareMaterials();
                //所以只有當完全沒有素材正在撥放，noMaterialCanPlay=true，我才需要執行下面的if函式
                if (noMaterialCanPlay)
                {
                    noMaterialCanPlay = false;
                    alwalsShowIma.SetActive(false);
                    InitialPrepareMaterials();
                }
                GetComponent<ManagerCtrl>().SystemStatus.text = "";

                break;
            }
        }
        if (earlyMaterialSequence.Count <= 0)
        {
            materialwaitTimeToPlay = false;
        }

    }
    public int GetPreviousCurrentSort()
    {
        if (currentSort - 1 < 0)
        {
            return fileDictionary.Count;
        }
        else return currentSort-1;
    }
    /// <summary>
    /// 轉換起迄時間為真實的DateTime
    /// </summary>
    /// <param name="dateTimeStr"></param>
    /// <returns></returns>
    public DateTime[] CaculateDateTime(string[] dateTimeStr)
    {
        DateTime beginDateTime;
        DateTime endDateTime;
        if (DateTime.TryParseExact(dateTimeStr[0], "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out beginDateTime))
        {
            
            
        }
        else
        {
            //Debug.Log("轉換出錯");
            GetComponent<ManagerCtrl>().SystemStatus.text = "日期無法計算";
        }
        if (DateTime.TryParseExact(dateTimeStr[1], "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out endDateTime))
        {
            //Debug.Log(endDateTime);

        }
        else
        {
            //Debug.Log("轉換出錯");
            GetComponent<ManagerCtrl>().SystemStatus.text = "日期無法計算";
        }
       
        return new DateTime[] { beginDateTime, endDateTime };
    }
    
    /// <summary>
    /// 根據現在可撥放的總數，計算已經截了幾張圖，如果大於可撥放總數，則停止screenshot
    /// </summary>
    public void CheckScreenshotCount()
    {
        //這邊要思考一下，如果在截圖的過程中，剛好有素材到期，會不會影響
        screenshotCount++;
        if(screenshotCount >= canPlaySequenceList.Count)
        {
            screenShot = false;
            screenshotCount = 0;
            GameObject.Find("GlobalManager").GetComponent<ScreenshotManager>().ResetSequence();
        }

    }
    
    /// <summary>
    /// 還有素材可以撥放，只是時間還沒到，所以停止截圖，以及Show固定圖片，跳出撥放回圈，避免當機
    /// </summary>
    public void HasMaterialCanPlay()
    {
        ima.gameObject.SetActive(false);
        displayUGUI.gameObject.SetActive(false);
        mediaPlayer1.Stop();
        mediaPlayer2.Stop();
        mediaPlayer1.CloseMedia();
        mediaPlayer2.CloseMedia();
        preparedMediaPlayer1 = false;
        playMediaPlayer1 = false;
        currentSort = 0;
        timer = 0;
        caculateTime = false;
        playVideo = false;
        noMaterialCanPlay = true;
        screenShot = false;
        screenshotCount = 0;
        alwalsShowIma.SetActive(true);
        
    }
    /// <summary>
    /// 都沒有素材撥放的時候，就會停止截圖，以及Show固定圖片，跳出撥放回圈，避免當機
    /// </summary>
    public void NoMaterialCanPlay()
    {
        materialwaitTimeToPlay = false;
        ima.gameObject.SetActive(false);
        displayUGUI.gameObject.SetActive(false);
        mediaPlayer1.Stop();
        mediaPlayer2.Stop();
        mediaPlayer1.CloseMedia();
        mediaPlayer2.CloseMedia();
        currentSort = 0;
        preparedMediaPlayer1 = false;
        playMediaPlayer1 = false;
        if (fileDictionary != null)
            fileDictionary.Clear();
        if (spriteDictionary!=null)
            spriteDictionary.Clear();
        if(TexturePlayTimeDictionary!=null)
            TexturePlayTimeDictionary.Clear();
        if(PlayConstrainDictionary!=null)
            PlayConstrainDictionary.Clear();
        if (canPlaySequenceList != null)
            canPlaySequenceList.Clear();
        if (earlyMaterialSequence != null)
            earlyMaterialSequence.Clear();
        
        timer = 0;
        caculateTime = false;
        playVideo = false;
        noMaterialCanPlay = true;
        screenShot = false; 
        screenshotCount = 0;
        alwalsShowIma.SetActive(true);
        
    }
    void HandleEventMediaPlayer1(MediaPlayer mp1, MediaPlayerEvent.EventType eventType, ErrorCode code)
    {
        if(eventType == MediaPlayerEvent.EventType.FinishedPlaying)
            PlayProgressManager();
        if(eventType == MediaPlayerEvent.EventType.Stalled)
            mp1.Play();
        string s = mp1.name + " Event: " + eventType.ToString();
        if (eventType == MediaPlayerEvent.EventType.Error)
        {
            s = mp1.name + " Event: " + eventType.ToString() + "_" + code;
            //Debug.LogError("Error: " + code);
        }
        SetMediaPlayerEventText1(s);
    }
    void HandleEventMediaPlayer2(MediaPlayer mp2, MediaPlayerEvent.EventType eventType, ErrorCode code)
    {
        if (eventType == MediaPlayerEvent.EventType.FinishedPlaying)
            PlayProgressManager();
        if (eventType == MediaPlayerEvent.EventType.Stalled)
            mp2.Play();
        string s = mp2.name + " Event: " + eventType.ToString();
        if (eventType == MediaPlayerEvent.EventType.Error)
        {
            s = mp2.name + " Event: " + eventType.ToString() + "_" + code;
            //Debug.LogError("Error: " + code);
        }
        SetMediaPlayerEventText2(s);
    }
    void SetMediaPlayerEventText1(string statusString)
    {
        mediaPlayerStatusText1.text = statusString;
    }
    void SetMediaPlayerEventText2(string statusString)
    {
        mediaPlayerStatusText2.text = statusString;
    }
    private void OnDisable()
    {
        if(mediaPlayer1 != null)
            mediaPlayer1.GetComponent<MediaPlayer>().Events.RemoveListener(HandleEventMediaPlayer1);
        if(mediaPlayer2 != null)
            mediaPlayer2.GetComponent<MediaPlayer>().Events.RemoveListener(HandleEventMediaPlayer2);
    }
}
