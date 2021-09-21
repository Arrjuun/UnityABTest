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

    public string remoteAssetBundleFileName = "assetbundle";

    public bool localPath = true;

    public string dllFileName = "";
    public string assetBundleFileName = "";

    AssetBundle loadedAssetBundle;
    byte[] dllData;

    public uint version = 1;
    //public uint crc;

    public string modelId = "5b770936-255e-49b8-9473-c0cf44494155";

    public Image loadingImage;

    public bool isDownloadedOnFileSystem = false;

    string projectBundlePath;
    string downloadedBundlesPath;

    private void Start()
    {
        projectBundlePath = Path.Combine(Application.persistentDataPath, "ProjectBundles");
        downloadedBundlesPath = Path.Combine(Application.persistentDataPath, "DownloadedBundles");

        if (!Directory.Exists(projectBundlePath))
        {
            Directory.CreateDirectory(projectBundlePath);
        }

        if (!Directory.Exists(downloadedBundlesPath))
        {
            Directory.CreateDirectory(downloadedBundlesPath);
        }

        Cache currentCache = Caching.AddCache(projectBundlePath);
        Caching.currentCacheForWriting = currentCache;
        Debug.Log($"Current Cache for writing : {Caching.currentCacheForWriting.path}");

    }

    public void OnClickLoadScene()
    {
        StartCoroutine(LoadBundledScene());
    }

    void LoadAssetBundleScene()
    {
        if (dllData != null)
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

                    if (assetBundleRequest.result == UnityWebRequest.Result.Success)
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
            string dir = Path.Combine(downloadedBundlesPath, $"{modelId}_{version}");
            Debug.Log(dir);

            if (!Directory.Exists(dir))
            {
                Debug.Log("Creating Directory");
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(Path.Combine(dir, remoteAssetBundleFileName)))
            {
                bundleUrl = Path.Combine(dir, remoteAssetBundleFileName);
                isDownloadedOnFileSystem = true;
            }
            else
            {
                UnityWebRequest downloadAssetBundleRequest = UnityWebRequest.Get(bundleUrl);

                var downloadAssetBundleOperation = downloadAssetBundleRequest.SendWebRequest();

                float downloadAssetBundleProgress = 0;

                while (!downloadAssetBundleOperation.isDone)
                {
                    downloadAssetBundleProgress = downloadAssetBundleRequest.downloadProgress * 100;

                    Debug.Log("Download: " + downloadAssetBundleProgress);
                    yield return null;
                }

                if (downloadAssetBundleRequest.result != UnityWebRequest.Result.Success)
                {

                }
                else
                {
                    File.WriteAllBytes(Path.Combine(dir, remoteAssetBundleFileName), downloadAssetBundleRequest.downloadHandler.data);
                    bundleUrl = Path.Combine(dir, remoteAssetBundleFileName);
                }
            }

            if (!assemblyUrl.Equals(""))
            {
                UnityWebRequest dllWebRequest = UnityWebRequest.Get(assemblyUrl);

                var dllDownloadOperation = dllWebRequest.SendWebRequest();
                float dllDownloadDataProgress;

                while (!dllDownloadOperation.isDone)
                {
                    dllDownloadDataProgress = dllWebRequest.downloadProgress * 100;

                    Debug.Log("Download: " + dllDownloadDataProgress);
                    yield return null;
                }

                Debug.Log($"Download Progress : {dllWebRequest.downloadProgress * 100}");

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

            Debug.Log($"Loading Asset Bundle from {bundleUrl}");

            UnityWebRequest assetBundleWebRequest = UnityWebRequestAssetBundle.GetAssetBundle(bundleUrl);

            var assetBundleDownloadOperation = assetBundleWebRequest.SendWebRequest();
            float assetBundleDownloadDataProgress;

            while (!assetBundleDownloadOperation.isDone)
            {
                assetBundleDownloadDataProgress = assetBundleWebRequest.downloadProgress * 100;

                Debug.Log("Loading : " + assetBundleDownloadDataProgress);
                yield return null;
            }

            Debug.Log($"Loading Progress : {assetBundleWebRequest.downloadProgress * 100}");

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
