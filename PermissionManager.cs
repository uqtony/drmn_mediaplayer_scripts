using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
public class PermissionManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        if(Application.platform==RuntimePlatform.Android)
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                // Request location permission
                Permission.RequestUserPermission(Permission.FineLocation);
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
            {
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
            }

        }
      
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
