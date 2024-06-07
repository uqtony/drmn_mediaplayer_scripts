using Firebase;
using Firebase.Messaging;
using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;


public class FirebaseController : MonoBehaviour
{
    float timer;
    public static bool reboot;
    void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("GlobalManager");

        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }
    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            // ��l��Firebase Messaging
            FirebaseMessaging.TokenReceived += OnTokenReceived;
            FirebaseMessaging.MessageReceived += OnMessageReceived;

            FirebaseMessaging.SubscribeAsync("DASU").ContinueWith(subscribeTask =>
            {
                ManagerCtrl.notificationTopic = "Subscribed to topic with DASU Topic";
            });
        });
    }

    // ��]�Ʀ���s���q���ɩI�s
    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        ManagerCtrl.notificationProjectID = e.Message.From;
        ManagerCtrl.notificationTitle = e.Message.Notification.Title;
        ManagerCtrl.notificationBody = e.Message.Notification.Body;
        if (ManagerCtrl.notificationTitle == "playlist" && !GameObject.Find("Manager").GetComponent<ManagerCtrl>().updatingPlayList)
        {
            GameObject.Find("Manager").GetComponent<ManagerCtrl>().updatePlayList = true;
            GameObject.Find("Manager").GetComponent<ManagerCtrl>().checkPlayListTimeText.text = "CheckPlaylist : ���������q���A�i��M���s" + DateTime.Now.ToString();
        }
        if (ManagerCtrl.notificationTitle == "screenshot")
        {
            StartCoroutine(ScreenShot());
        }
        if(ManagerCtrl.notificationTitle == "volume")
        {
            GameObject.Find("Manager").GetComponent<ManagerCtrl>().SetDeviceVolume(ManagerCtrl.notificationBody);

        }
        if (ManagerCtrl.notificationTitle == "reboot")
        {
            if (Application.platform != RuntimePlatform.Android)
                return;
            reboot = true;
            GetComponent<AndroidDeviceControlManager>().RebootDevice();
        }
    }

    public IEnumerator ScreenShot()
    {
        yield return new WaitForEndOfFrame();
        //ManagerCtrl.timeStamp = Convert.ToInt32(DateTimeOffset.Now.ToUnixTimeSeconds());
        //�@������I�ϰT���A�N���I����U���ϡA����N�u�|�^���C�ӯ������Ĥ@�w�Ϥ��C
        //GameObject.Find("Manager").GetComponent<PlayMaterialManager>().screenShot = true;
        StartCoroutine(GetComponent<ScreenshotManager>().TakeScreenshot());
        //GameObject.Find("Manager").GetComponent<PlayMaterialManager>().CheckScreenshotCount();
    }
    // ��]�ƪ��O�P��s�ɩI�s
    private void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        ManagerCtrl.notificationToken = token.Token;
        GameObject.Find("Manager").GetComponent<ManagerCtrl>().WriteSystemProfile();
    }

}
