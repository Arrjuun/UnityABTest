using System.Collections;
using UnityEngine.UI;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.IO;

public class AssetBundleLoader : MonoBehaviour
{
    public string assemblyUrl = "http://localhost/assets/assembly_for_assetbundle.dll";
    public string bundleUrl = "http://localhost/assets/myassetbundle";

    public bool localPath = true;

    public string dllFileName = "";
    public string assetBundleFileName = "";

    AssetBundle loadedAssetBundle;
    byte[] dllData;

    public void OnClickLoadScene()
    {
        StartCoroutine(LoadBundledScene());
    }

    void LoadAssetBundleScene()
    {
        if(dllData != null)
        {
            Assembly a = Assembly.Load(dllData);
        }

        string[] scenePath = loadedAssetBundle.GetAllScenePaths();

        SceneManager.LoadScene(scenePath[0]);
    }

    IEnumerator LoadBundledScene()
    {
        if (localPath)
        {
            if (!dllFileName.Equals(""))
            {
                string filePath = Path.Combine(Application.streamingAssetsPath, dllFileName);
                if (filePath.Contains("://") || filePath.Contains(":///"))
                {
                    
                    UnityWebRequest assetBundleRequest = UnityWebRequest.Get(filePath);
                    yield return assetBundleRequest.SendWebRequest();

                    if(assetBundleRequest.result == UnityWebRequest.Result.Success)
                    {
                        dllData = assetBundleRequest.downloadHandler.data;
                    }
                }
                else
                {
                    dllData = File.ReadAllBytes(filePath);
                }
            }

            loadedAssetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, assetBundleFileName));
        }
        else
        {
            if(!assemblyUrl.Equals(""))
            {
                UnityWebRequest dllWebRequest = UnityWebRequest.Get(assemblyUrl);

                yield return dllWebRequest.SendWebRequest();

                if (dllWebRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(dllWebRequest.error);
                }
                else
                {
                    Debug.Log("Successfully downloaded dll");
                    dllData = dllWebRequest.downloadHandler.data;
                }
            }

            UnityWebRequest assetBundleWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl);

            yield return assetBundleWebRequest.SendWebRequest();

            if (assetBundleWebRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(assetBundleWebRequest.error);
            }
            else
            {
                Debug.Log("Successfully downloaded Asset bundle");
                loadedAssetBundle = DownloadHandlerAssetBundle.GetContent(assetBundleWebRequest);
            }
        }

        LoadAssetBundleScene();
    }
}
