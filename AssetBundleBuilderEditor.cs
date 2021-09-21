using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public class AssetBundleBuilderEditor : EditorWindow
{
    static readonly Dictionary<Object, string> scenesToBeBundled = new Dictionary<Object, string>();

    Object sceneToBeRemoved;


    static bool forceRebuild = false;
    static BuildTarget buildTarget = BuildTarget.Android;
    static string assemblyDefinitionFileName = "";
    static bool enableElixrLogin = false;
    static string userName;
    static string password;
    static string email;
    static string organizationName;
    static string organizationId;

    static string assetBundleDirectory = "";

    GUIContent compressionContent;
    internal enum CompressOptions
    {
        Uncompressed = 0,
        StandardCompression,
        ChunkBasedCompression,
    }
    GUIContent[] compressionGUIOptions =
    {
            new GUIContent("No Compression"),
            new GUIContent("Standard Compression (LZMA)"),
            new GUIContent("Chunk Based Compression (LZ4)")
    };
    int[] compressionValues = { 0, 1, 2 };

    static CompressOptions compression = CompressOptions.StandardCompression;

    [MenuItem("Window/AssetBundle Builder")]
    public static void Open()
    {
        GetWindow<AssetBundleBuilderEditor>(false, "DragDrop", true);
    }

    private void OnGUI()
    {
        Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        //GUI.Box(drop_area, "Add Trigger");
        var centeredLabelStyle = GUIStyle.none;
        centeredLabelStyle.normal.textColor = Color.white;
        centeredLabelStyle.fontStyle = FontStyle.Bold;
        centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(drop_area, "+ Drop Scene Asset Here", centeredLabelStyle);



        if (Event.current.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            Event.current.Use();
        }
        else if (Event.current.type == EventType.DragPerform)
        {
            if (!drop_area.Contains(Event.current.mousePosition))
                return;

            // To consume drag data.
            DragAndDrop.AcceptDrag();

            // Unity Assets
            if (DragAndDrop.paths.Length == DragAndDrop.objectReferences.Length)
            {
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    Object obj = DragAndDrop.objectReferences[i];
                    string path = DragAndDrop.paths[i];
                    //Debug.Log(obj.GetType().Name);

                    if (obj.GetType().Name.Equals("SceneAsset"))
                    {
                        if (!scenesToBeBundled.ContainsKey(obj))
                        {
                            scenesToBeBundled.Add(obj, path);
                        }
                    }
                    else
                    {
                        Debug.Log("Please add a SceneAsset");
                    }

                }
            }
            else
            {
                Debug.Log("Please add a SceneAsset");
            }
        }

        HorizontalLine(Color.grey);

        EditorGUILayout.Space();

        GUILayout.Label(" Scenes to be Bundled",centeredLabelStyle);

        EditorGUILayout.Space();

        if(scenesToBeBundled.Count == 0)
        {
            GUILayout.Label("No scenes added to be bundled");
        }

        foreach (var sceneObj in scenesToBeBundled)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(sceneObj.Key.name);
            if (GUILayout.Button("Delete"))
            {
                sceneToBeRemoved = sceneObj.Key;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (sceneToBeRemoved)
        {
            scenesToBeBundled.Remove(sceneToBeRemoved);
            sceneToBeRemoved = null;
        }

        EditorGUILayout.Space();
        HorizontalLine(Color.grey);

        //Build Target DropDown
        buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Select the Build target", buildTarget);


        //Compression dropdown
        compressionContent = new GUIContent("Compression", "Choose no compress, standard (LZMA), or chunk based (LZ4)");
        CompressOptions cmp = (CompressOptions)EditorGUILayout.IntPopup(
                        compressionContent,
                        (int)compression,
                        compressionGUIOptions,
                        compressionValues);
        if (cmp != compression)
        {
            compression = cmp;
        }


        //Force Rebuild Toggle
        forceRebuild = EditorGUILayout.Toggle("Force Rebuild", forceRebuild);

        //Assembly definition file name
        assemblyDefinitionFileName = EditorGUILayout.TextField("Assembly Definition File Name", assemblyDefinitionFileName);

        EditorGUILayout.Space();
        HorizontalLine(Color.grey);

        //Elixr login 
        GUILayout.Label(" Login and Upload to Elixr", centeredLabelStyle);
        EditorGUILayout.Space();

        //Force Rebuild Toggle
        enableElixrLogin = EditorGUILayout.Toggle("Login & Upload to Elixr", enableElixrLogin);
        EditorGUILayout.Space();

        GUI.enabled = enableElixrLogin;

        EditorGUILayout.TextField("UserName ", userName);
        EditorGUILayout.TextField("Email ", email);
        EditorGUILayout.TextField("Password ", password);
        EditorGUILayout.TextField("Organization Name ", organizationName);
        EditorGUILayout.TextField("Organization ID ", organizationId);
        EditorGUILayout.Space();

        GUI.enabled = true;


        GUI.enabled = scenesToBeBundled.Count > 0;
        //Build Assembly and Assetbundle Button
        if (GUILayout.Button("Build AssetBundle and Assembly"))
        {
            bool startBundling = true;
            if (assemblyDefinitionFileName.Equals(""))
            {
                 startBundling = EditorUtility.DisplayDialog("Assembly Definition File name not specified",
                    "Assembly Definition File name is not specified. Do you want to continue ?",
                    "Yes",
                    "No");
            }
            if (scenesToBeBundled.Count > 0 && startBundling)
            {
                List<AssetBundleBuild> buildMap = new List<AssetBundleBuild>();
                foreach (KeyValuePair<Object, string> entry in scenesToBeBundled)
                {
                    buildMap.Add(new AssetBundleBuild()
                    {
                        assetBundleName = entry.Key.name,
                        assetNames = new string[]
                        {
                            entry.Value
                        }
                    });
                }
                assetBundleDirectory = $"CreatedAssetBundles/{buildTarget}";
                if (!Directory.Exists(assetBundleDirectory))
                {
                    Directory.CreateDirectory(assetBundleDirectory);
                }

                CompilationPipeline.compilationFinished += OnCompilationFinished;
                BuildAssetBundleOptions options = BuildAssetBundleOptions.None;

                if(forceRebuild)
                {
                    options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
                }

                if (compression == CompressOptions.Uncompressed)
                    options |= BuildAssetBundleOptions.UncompressedAssetBundle;
                else if (compression == CompressOptions.ChunkBasedCompression)
                    options |= BuildAssetBundleOptions.ChunkBasedCompression;

                AssetBundleManifest assetBundleManifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory, buildMap.ToArray(), options, EditorUserBuildSettings.activeBuildTarget);
                RenameAssetBundles();
            }
            GUI.enabled = true;
        }
    }

    static void OnCompilationFinished(object sender)
    {
        if(!assemblyDefinitionFileName.Equals(""))
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished;

            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

            string dllFile = $"CreatedAssetBundles/{buildTarget}/{assemblyDefinitionFileName}.dll";

            var assembly = assemblies.FirstOrDefault(e => e.name.Equals(assemblyDefinitionFileName));

            if(assembly != null)
            {

                if (File.Exists(dllFile))
                {
                    File.Delete(dllFile);
                }

                File.Copy(assembly.outputPath, dllFile);
            }
            else
            {
                Debug.Log("Assembly not found");
            }
        }
    }

    //Note: this is the provided BuildTarget enum with some entries removed as they are invalid in the dropdown
    internal enum BuildTarget
    {
        //NoTarget = -2,        --doesn't make sense
        //iPhone = -1,          --deprecated
        //BB10 = -1,            --deprecated
        //MetroPlayer = -1,     --deprecated
        StandaloneOSXUniversal = 2,
        //StandaloneOSXIntel = 4,
        StandaloneWindows = 5,
        //WebPlayer = 6,
        //WebPlayerStreamed = 7,
        //iOS = 9,
        //PS3 = 10,
        //XBOX360 = 11,
        Android = 13,
        StandaloneLinux = 17,
        StandaloneWindows64 = 19,
        //WebGL = 20,
        //WSAPlayer = 21,
        //StandaloneLinux64 = 24,
        //StandaloneLinuxUniversal = 25,
        //WP8Player = 26,
        //StandaloneOSXIntel64 = 27,
        //BlackBerry = 28,
        //Tizen = 29,
        //PSP2 = 30,
        //PS4 = 31,
        //PSM = 32,
        //XboxOne = 33,
        //SamsungTV = 34,
        //N3DS = 35,
        //WiiU = 36,
        //tvOS = 37,
        //Switch = 38
    }

    // create your style
    static GUIStyle horizontalLine;
 
    // utility method
    static void HorizontalLine(Color color)
    {
        horizontalLine = new GUIStyle();
        horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
        horizontalLine.margin = new RectOffset(0, 0, 4, 4);
        horizontalLine.fixedHeight = 1;

        var c = GUI.color;
        GUI.color = color;
        GUILayout.Box(GUIContent.none, horizontalLine);
        GUI.color = c;
    }

    static void RenameAssetBundles()
    {
        string[] files = Directory.GetFiles(assetBundleDirectory);

        foreach (var a in files)
        {
            if (!a.Contains("."))
            {
                File.Move(a, a + ".unity3d");
            }
        }
    }
}
