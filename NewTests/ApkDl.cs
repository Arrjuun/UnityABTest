using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ApkDl : MonoBehaviour
{
    [SerializeField]
    string url = "";

    [SerializeField]
    Text debugText;

    [SerializeField]
    Text packagesListText;

    [SerializeField]
    InputField inputField;

    public string applicationPackageName;

    public string installerPackageName;

    private void Start()
    {
        debugText.text = $"Click button to download and install apk from\n\n {url}";
    }
    public void OnDownloadInstallButtonClick()
    {
        StartCoroutine(DownLoadFromServer());
    }

    IEnumerator DownLoadFromServer()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "data");
        savePath = Path.Combine(savePath, "test.apk");

        //Dictionary<string, string> header = new Dictionary<string, string>();
        //string userAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 Safari/537.36";
        //header.Add("User-Agent", userAgent);
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        var requestOperation = webRequest.SendWebRequest();

        while (!requestOperation.isDone)
        {
            //Must yield below/wait for a frame
            debugText.text = $"Download progress : { requestOperation.progress * 100} %";
            yield return null;
        }

        byte[] yourBytes = webRequest.downloadHandler.data;

        debugText.text = "Done downloading. Size: " + yourBytes.Length;


        //Create Directory if it does not exist
        if (!Directory.Exists(Path.GetDirectoryName(savePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            //debugText.text = "Created Dir";
        }

        try
        {
            //Now Save it
            File.WriteAllBytes(savePath, yourBytes);
            Debug.Log("Saved Data to: " + savePath.Replace("/", "\\"));
            //debugText.text = "Saved Data";

            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");

            try
            {
                AndroidJavaObject packageArchiveInfo = pm.Call<AndroidJavaObject>("getPackageArchiveInfo", savePath, 0);
                debugText.text = $"Downloaded and stored apk with package name : {packageArchiveInfo.Get<string>("packageName")}";
            }
            catch (Exception ex)
            {
                debugText.text = $"Error when getting package Info {ex.Message}";
            }


        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed To Save Data to: " + savePath.Replace("/", "\\"));
            Debug.LogWarning("Error: " + e.Message);
            //debugText.text = "Error Saving Data";
        }

        yield return new WaitForSeconds(3f);

        Debug.Log(savePath);

#if UNITY_EDITOR
        Debug.Log("In Unity Editor");
#elif UNITY_ANDROID
        //Install APK
        InstallApp(savePath);
#else
        Debug.Log("Unsupported Platform");
#endif
    }

    private bool InstallApp(string apkPath)
    {
        bool success = true;
        debugText.text = "Installing App";

        Debug.Log("In Install App");

        try
        {
            //Get Activity then Context
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaObject pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");

            //Get the package Name
            string packageName = unityContext.Call<string>("getPackageName");
            string authority = packageName + ".FileProvider";

            AndroidJavaClass intentObj = new AndroidJavaClass("android.content.Intent");
            string ACTION_VIEW = intentObj.GetStatic<string>("ACTION_VIEW");
            //AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", ACTION_VIEW);
            AndroidJavaObject intent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", installerPackageName);
            //intent.Call<AndroidJavaObject>("putExtra", "authToken", "This is the token passed from other app");

            int FLAG_ACTIVITY_NEW_TASK = intentObj.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK");
            int FLAG_GRANT_READ_URI_PERMISSION = intentObj.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION");

            //File fileObj = new File(String pathname);
            AndroidJavaObject fileObj = new AndroidJavaObject("java.io.File", apkPath);
            //FileProvider object that will be used to call it static function
            AndroidJavaClass fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider");
            //getUriForFile(Context context, String authority, File file)
            AndroidJavaObject uri = fileProvider.CallStatic<AndroidJavaObject>("getUriForFile", unityContext, authority, fileObj);

            intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
            intent.Call<AndroidJavaObject>("addFlags", FLAG_ACTIVITY_NEW_TASK);
            intent.Call<AndroidJavaObject>("addFlags", FLAG_GRANT_READ_URI_PERMISSION);
            currentActivity.Call("startActivity", intent);

            debugText.text = "Successfully downloaded and stored the apk";
        }
        catch (Exception e)
        {
            debugText.text = "Error: " + e.Message;
            success = false;
        }

        return success;
    }

    public void OnGetPackagesListClick()
    {
        StartCoroutine(GetInstalledPackagesSearchForInstalledPackage());
    }

    IEnumerator GetInstalledPackagesSearchForInstalledPackage()
    {
        yield return null;
        packagesListText.text = $"Checking if {applicationPackageName} is installed on the device";

        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");
        //int flag = new AndroidJavaClass("android.content.pm.PackageManager").GetStatic<int>("GET_META_DATA");
        AndroidJavaObject pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
        //AndroidJavaObject packages = pm.Call<AndroidJavaObject>("getInstalledApplications", flag);
        AndroidJavaObject launchIntent;


        //Check if package is installed
        try
        {
            AndroidJavaObject packageInfo = pm.Call<AndroidJavaObject>("getPackageInfo", applicationPackageName, 0);
            packagesListText.text = $"Package {applicationPackageName} is installed on the device";

            launchIntent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", applicationPackageName);
            launchIntent.Call<AndroidJavaObject>("putExtra", "authToken", inputField.text);
            currentActivity.Call("startActivity", launchIntent);

            launchIntent.Dispose();
        }
        catch (Exception)
        {
            packagesListText.text = $"Package {applicationPackageName} is not installed on the device\n\n";
        }

        pm.Dispose();
        currentActivity.Dispose();
        jc.Dispose();
    }
}
