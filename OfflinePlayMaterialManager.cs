using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using DASUAPI;
using UnityEngine.UI;
using RenderHeads.Media.AVProVideo;

public class OfflinePlayMaterialManager : MonoBehaviour
{
    //如果是要寫USB複製檔案的版本，以下這段宣告不用，因為USB那邊已經寫過了
    //const string folderName = "DownloadMaterials";

    public Dictionary<int, string> fileDictionary;
    int currentSort = 1;
    bool playMP1;
    public Dictionary<int, Sprite> spriteDictionary;
    public Image ima;
    public DisplayUGUI displayUGUI;
    public MediaPlayer mediaPlayer1;
    public MediaPlayer mediaPlayer2;
    int sequence;
    string materialFolderPath;
    //如果是要寫USB複製檔案的版本，以下這段start關閉，不須執行
    /*private void Start()
    {
        spriteDictionary = new Dictionary<int, Sprite>();
        fileDictionary = new Dictionary<int, string>();

        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            StaticVar.materialsFolderPath = Path.Combine(Application.persistentDataPath, folderName);
        }
        else
        {
            StaticVar.materialsFolderPath = Path.Combine(Application.dataPath, folderName);
        }

        //如果是要寫USB複製檔案的版本，以下這段要關閉，不須執行
        StartCoroutine(CreateDownloadMaterialsFolder());
    }*/
    private void Start()
    {
        materialFolderPath = new PathRelated().MaterialsFolderPath("DownloadMaterials");
    }
    //如果是要寫USB複製檔案的版本，以下這段要打開
    public void StartToPlay()
    {
        spriteDictionary = new Dictionary<int, Sprite>();
        fileDictionary = new Dictionary<int, string>();
        CheckFile();
    }
    /// <summary>
    /// 如果是要寫USB複製檔案的版本，以下這段要關閉，因為在USB讀取那邊已經寫過了
    /// </summary>
    /*IEnumerator CreateDownloadMaterialsFolder()
    {
        //檢查是否有DownloadMaterials的資料夾
        if (!Directory.Exists(StaticVar.materialsFolderPath))
        {
            Directory.CreateDirectory(StaticVar.materialsFolderPath);
            yield return new WaitForEndOfFrame();
        }
        CheckFile();
    }*/
    void CheckFile()
    {
        string[] filePaths = Directory.GetFiles(materialFolderPath);
        if (filePaths.Length == 0)
            return;
        sequence = 1;
        foreach (string filePath in filePaths)
        {

            if (!filePath.Contains("meta"))
            {
                if (filePath.Contains(sequence.ToString("00")))
                {
                    fileDictionary.Add(sequence, filePath);
                }
                sequence++;
            }
            
        }
        InitialPrepareMaterials();
    }
    public void InitialPrepareMaterials()
    {
        if (fileDictionary[currentSort].Contains("mp4"))
        {
            PrepareVideo(playMP1);
        }

        ToSprite();
        PlayProgressManager();
    }
    public void PlayProgressManager()
    {

        if (fileDictionary[currentSort].Contains("mp4"))
        {
            PlayVideo();
            if (playMP1)
                playMP1 = false;
            else
                playMP1 = true;
        }
        else
        {
            PlayIma();
        }
        //Debug.Log(currentSort);
        currentSort++;
        if (currentSort > fileDictionary.Count)
            currentSort = 1;
        if (fileDictionary[currentSort].Contains("mp4"))
            PrepareVideo(playMP1);

    }
    void PlayIma()
    {
        displayUGUI.gameObject.SetActive(false);
        ima.gameObject.SetActive(true);
        ima.sprite = spriteDictionary[currentSort];
        Invoke("CloseIma", 30);
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
        if (!playMP1)
        {

            displayUGUI.CurrentMediaPlayer = mediaPlayer1;
            mediaPlayer1.Play();
            mediaPlayer2.Stop();
        }
        else
        {
            displayUGUI.CurrentMediaPlayer = mediaPlayer2;
            mediaPlayer2.Play();
            mediaPlayer1.Stop();
        }

    }

    void PrepareVideo(bool _playMP1)
    {

        if (!_playMP1)
            mediaPlayer1.OpenMedia(MediaPathType.AbsolutePathOrURL, fileDictionary[currentSort] , false);
        else
            mediaPlayer2.OpenMedia(MediaPathType.AbsolutePathOrURL, fileDictionary[currentSort] , false);
    } 


    void ToSprite()
    {
        foreach (KeyValuePair<int, string> file in fileDictionary)
        {
            if (file.Value.Contains("jpg") || file.Value.Contains("png"))
            {
                byte[] imageBytes;
                if (file.Value.Contains("jpg"))
                    imageBytes = File.ReadAllBytes(fileDictionary[file.Key]);
                else if (file.Value.Contains("png"))
                    imageBytes = File.ReadAllBytes(fileDictionary[file.Key]);
                else
                    return;
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes);
                Sprite _sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                spriteDictionary.Add(file.Key, _sprite);
            }
        }
    }


}
