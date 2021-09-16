using System.Collections;
using UnityEngine.UI;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class AssetBundleLoader : MonoBehaviour
{
    public string assemblyUrl = "http://localhost/assets/assembly_for_assetbundle.dll";
    public string bundleUrl = "http://localhost/assets/myassetbundle.unity3d";
    public Text loggingText;

    void Start()
    {
        StartCoroutine(LoadBundledScene());
    }

    IEnumerator LoadBundledScene()
    {
        UnityWebRequest dllWebRequest = UnityWebRequest.Get(assemblyUrl);
        UnityWebRequest assetBundleWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl);

        loggingText.text += "Starting dll download\n";
        yield return dllWebRequest.SendWebRequest();

        if (dllWebRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(dllWebRequest.error);
            loggingText.text += "Error getting dll\n";
        }
        else
        {
            Debug.Log("Successfully downloaded dll");
            loggingText.text += "Successfully downloaded dll\n";
            byte[] dllData = dllWebRequest.downloadHandler.data;
            Assembly a = Assembly.Load(dllData);
            var m = a.Modules;
            foreach(var ma in m)
            {
                loggingText.text += $"{ma.FullyQualifiedName}\n";
            }
        }

        loggingText.text += "Starting asset bundle download\n";
        yield return assetBundleWebRequest.SendWebRequest();

        if (assetBundleWebRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(assetBundleWebRequest.error);
            loggingText.text += "Error getting asset bundle\n";
        }
        else
        {
            Debug.Log("Successfully downloaded Asset bundle");
            loggingText.text += "Successfully downloaded assetbundle\n";
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(assetBundleWebRequest);

            string[] scenePath = bundle.GetAllScenePaths();
            //Debug.Log(scenePath.Length);

            foreach (var path in scenePath)
            {
                loggingText.text += $"{path}\n";
            }

            SceneManager.LoadSceneAsync(scenePath[0]);

            //Instantiate(bundle.LoadAsset(assetName));
            //bundle.Unload(false);
        }
    }
}
