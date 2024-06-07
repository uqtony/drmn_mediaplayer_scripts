using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Linq;
using System.Net.NetworkInformation;
using System.IO;
using UnityEngine.Networking;

namespace DASUAPI
{
    public class PathRelated
    {
        /// <summary>
        /// 根據不同平台，將傳入的FolderName整合，設定materialFolder的路徑
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        public string MaterialsFolderPath(string folderName)
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return Path.Combine(Application.persistentDataPath, folderName);
            }
            else
            {
                return Path.Combine(Application.dataPath, folderName);
            }
        }
        public string PlayListFilePath
        {
            get
            {
                if (Application.platform == RuntimePlatform.Android || Application.platform==RuntimePlatform.IPhonePlayer)
                    return Path.Combine(Application.persistentDataPath, "PlayList.json");
                else
                    return Path.Combine(Application.dataPath, "PlayList.json");
            }
        }
        public void ClearMaterialsFiles()
        {
            string[] files = Directory.GetFiles(ManagerCtrl.materialFolderPath);

            // 遍歷所有檔案，刪除它們
            foreach (string file in files)
            {
                File.Delete(file);
                
            }
        }
        public string SystemProfilePath
        {
            get
            {
                if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                    return Path.Combine(Application.persistentDataPath, "SystemProfile.json");
                else
                    return Path.Combine(Application.dataPath, "SystemProfile.json");
            }
        }
        public string ScreenshotRootPath()
        { 
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                return Path.Combine(Application.persistentDataPath, DateTime.Now.ToString("yyyyMMddHHmmss") +".jpg");
            else
                return Path.Combine(Application.dataPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg");
        }
    }
    
    public class GetDeciceInfos
    {
        /// <summary>
        /// 獲取機器的UID，若查無，則反回Sorry!NoDeviceUUID
        /// </summary>
        public string DeviceUID
        {
            get{
                //取得Device deviceUniqueIdentifier
                if (!string.IsNullOrWhiteSpace(SystemInfo.deviceUniqueIdentifier))
                    return SystemInfo.deviceUniqueIdentifier.ToString();
                else
                    return "Sorry!NoDeviceUUID";
            }
            
        }
        /// <summary>
        /// 獲取機器的Mac位置，若查無，則反回Mac Adress is AA:BB:CC:DD:EE
        /// </summary>
        public string MacAdress()
        {
            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
            if (nis.Length > 0)
            {
                foreach (NetworkInterface ni in nis)
                {
                    //Debug.Log(ni.Description.ToLowerInvariant());
                    //排除虛擬接口或是循環接口
                    if (!ni.Description.ToLowerInvariant().Contains("virtual") && !ni.Description.ToLowerInvariant().Contains("loopback"))
                    {
                        // 只保留無線網卡和乙太網路
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                        {
                            
                            // 保留有在進行上傳活動的接口
                            if (ni.OperationalStatus == OperationalStatus.Up)
                            {
                                var ipProperties = ni.GetIPProperties();

                                if (ipProperties.UnicastAddresses.Any(ua => ua.Address.Equals(CurrentIP)))
                                {
                                    PhysicalAddress pa = ni.GetPhysicalAddress();
                                    byte[] bytes = pa.GetAddressBytes();
                                    //Debug.Log("Mac Adress   " + "-----" + string.Join(":", bytes.Select(b => b.ToString("X2"))));
                                    return string.Join(":", bytes.Select(b => b.ToString("X2")));
                                }
                                /*PhysicalAddress pa = ni.GetPhysicalAddress();
                                byte[] bytes = pa.GetAddressBytes();
                                Debug.Log("Mac Adress   " + i + "-----" + string.Join(":", bytes.Select(b => b.ToString("X2"))));

                                deviceMacText.text = "Mac Adress:" + string.Join(":", bytes.Select(b => b.ToString("X2")));

                                i++;*/
                            }
                        }
                    }
                   

                }
            }
            Debug.Log("Mac Adress is AA:BB:CC:DD:EE Because Internet maybe notwork");
            return "AA:BB:CC:DD:EE";
        }
        /// <summary>
        /// 獲取現在正在連線的IP
        /// </summary>
        IPAddress CurrentIP
        {
            get
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip;
                    }
                }
                return null; // 返回 null 或其他預設值，如果沒有找到 IPv4 地址
            }
        }

        public string DeviceScreenInfo()
        {
            int[] screenInfo = { Screen.width, Screen.height };
            return screenInfo[0] + "x" + screenInfo[1];
        }

      
        public bool InternetAbility
        {
            get
            {
                return Application.internetReachability != NetworkReachability.NotReachable;
            }
        }
    }



}

