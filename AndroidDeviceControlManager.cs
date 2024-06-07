using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndroidDeviceControlManager : MonoBehaviour
{
  
    /*public void SleepDevice()
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        AndroidJavaObject powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");
        powerManager.Call("goToSleep");
    }*/

    public void RebootDevice()
    {
        Application.Quit();
    }
    void OnApplicationQuit()
    {
        if(FirebaseController.reboot == true)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");
            powerManager.Call("reboot", "");

        }
       
    }
    /*public void WakeupDevice()
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        AndroidJavaObject powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");

        // Check if the device is currently in a state of sleeping
        if (powerManager.Call<bool>("isInteractive") == false)
        {
            AndroidJavaObject wakeLock = powerManager.Call<AndroidJavaObject>("newWakeLock", 10, 268435456); // Use the integer values directly
            wakeLock.Call("acquire", 10 * 60 * 1000); // Acquire the wake lock for 10 minutes (adjust as needed)
            wakeLock.Call("release");
        }
    }*/

    /*public void Shutdown()
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        AndroidJavaObject powerManager = new AndroidJavaObject("android.os.PowerManager");
        AndroidJavaObject powerService = currentActivity.Call<AndroidJavaObject>("getSystemService", "power");

        // 执行关机操作
        try
        {
            powerService.Call("shutdown", false, false);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to shutdown: " + e.Message);
        }
    }*/
    // 设置音量
    /*void SetVolume(float volume)
    {
        AndroidJavaClass audioManagerClass = new AndroidJavaClass("android.media.AudioManager");
        AndroidJavaObject audioManager = audioManagerClass.CallStatic<AndroidJavaObject>("getSystemService", "audio");

        int streamType = 4; // AudioManager.STREAM_MUSIC 的值是 3

        audioManager.Call("setStreamVolume", streamType, Mathf.RoundToInt(volume * maxVolume), 0);
    }

    // 获取最大音量
    int GetMaxVolume()
    {
        AndroidJavaClass audioManagerClass = new AndroidJavaClass("android.media.AudioManager");
        AndroidJavaObject audioManager = audioManagerClass.CallStatic<AndroidJavaObject>("getSystemService", "audio");
        int streamType = 4; // AudioManager.STREAM_MUSIC 的值是 3
        return audioManager.Call<int>("getStreamMaxVolume", streamType);
    }*/
}
