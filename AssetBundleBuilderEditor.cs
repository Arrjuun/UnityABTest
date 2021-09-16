using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public class AssetBundleBuilderEditor : EditorWindow
{
	static readonly Dictionary<Object, string> scenesToBeBundled = new Dictionary<Object, string>();

	[MenuItem("Window/AssetBundle Builder")]
	public static void Open()
	{
		GetWindow<AssetBundleBuilderEditor>(false, "DragDrop", true);
	}

	private void OnGUI()
	{
		if (Event.current.type == EventType.DragUpdated)
		{
			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			Event.current.Use();
		}
		else if (Event.current.type == EventType.DragPerform)
		{
			// To consume drag data.
			DragAndDrop.AcceptDrag();

			// Unity Assets including folder.
			if (DragAndDrop.paths.Length == DragAndDrop.objectReferences.Length)
			{
				for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
				{
					Object obj = DragAndDrop.objectReferences[i];
					string path = DragAndDrop.paths[i];
					//Debug.Log(obj.GetType().Name);

					if(obj.GetType().Name.Equals("SceneAsset"))
                    {
						Debug.Log("You added a SceneAsset");
						if(!scenesToBeBundled.ContainsKey(obj))
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

		foreach(var sceneObj in scenesToBeBundled)
        {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label(sceneObj.Key.name);
			if(GUILayout.Button("Delete"))
            {
				scenesToBeBundled.Remove(sceneObj.Key);
            }
			EditorGUILayout.EndHorizontal();
        }

		if (GUILayout.Button("Build AssetBundle and Assembly"))
		{
			if(scenesToBeBundled.Count > 0)
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
				string assetBundleDirectory = $"CreatedAssetBundles/{EditorUserBuildSettings.activeBuildTarget}";
				if (!Directory.Exists(assetBundleDirectory))
				{
					Directory.CreateDirectory(assetBundleDirectory);
				}

				CompilationPipeline.compilationFinished += OnCompilationFinished;
				BuildAssetBundleOptions options = BuildAssetBundleOptions.None;

				options |= BuildAssetBundleOptions.ForceRebuildAssetBundle;

				AssetBundleManifest assetBundleManifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory, buildMap.ToArray(), options, EditorUserBuildSettings.activeBuildTarget);

			}
			else
            {
				Debug.Log("No content to create Asset bundle");
            }
		}
	}

	static void OnCompilationFinished(object sender)
	{
		CompilationPipeline.compilationFinished -= OnCompilationFinished;

		var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player);

		string dllFile = $"CreatedAssetBundles/{EditorUserBuildSettings.activeBuildTarget}/SCENE_ASSEMBLY_DEF.dll";

		var assembly = assemblies.FirstOrDefault(e => e.name.Equals("SCENE_ASSEMBLY_DEF"));

		Debug.Log(assembly.outputPath);

		if (File.Exists(dllFile))
		{
			File.Delete(dllFile);
		}

		File.Copy(assembly.outputPath, dllFile);
	}
}


// using System.Collections;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;

//public class AssetBundleBuilderEditor : EditorWindow
//{
//    [MenuItem("Window/AssetBundle Builder")]
//	public static void Open()
//	{
//		GetWindow<AssetBundleBuilderEditor>(false, "DragDrop", true);
//	}

//	private void OnGUI()
//	{
//		if (Event.current.type == EventType.DragUpdated)
//		{
//			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
//			Event.current.Use();
//		}
//		else if (Event.current.type == EventType.DragPerform)
//		{
//			// To consume drag data.
//			DragAndDrop.AcceptDrag();

//			// GameObjects from hierarchy.
//			if (DragAndDrop.paths.Length == 0 && DragAndDrop.objectReferences.Length > 0)
//			{
//				Debug.Log("GameObject");
//				foreach (Object obj in DragAndDrop.objectReferences)
//				{
//					Debug.Log("- " + obj);
//				}
//			}
//			// Object outside project. It mays from File Explorer (Finder in OSX).
//			else if (DragAndDrop.paths.Length > 0 && DragAndDrop.objectReferences.Length == 0)
//			{
//				Debug.Log("File");
//				foreach (string path in DragAndDrop.paths)
//				{
//					Debug.Log("- " + path);
//				}
//			}
//			// Unity Assets including folder.
//			else if (DragAndDrop.paths.Length == DragAndDrop.objectReferences.Length)
//			{
//				Debug.Log("UnityAsset");
//				for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
//				{
//					Object obj = DragAndDrop.objectReferences[i];
//					string path = DragAndDrop.paths[i];
//					Debug.Log(obj.GetType().Name);

//					// Folder.
//					if (obj is DefaultAsset)
//					{
//						Debug.Log(path);
//					}
//					// C# or JavaScript.
//					else if (obj is MonoScript)
//					{
//						Debug.Log(path + "\n" + obj);
//					}
//					else if (obj is Texture2D)
//					{

//					}

//				}
//			}
//			// Log to make sure we cover all cases.
//			else
//			{
//				Debug.Log("Out of reach");
//				Debug.Log("Paths:");
//				foreach (string path in DragAndDrop.paths)
//				{
//					Debug.Log("- " + path);
//				}

//				Debug.Log("ObjectReferences:");
//				foreach (Object obj in DragAndDrop.objectReferences)
//				{
//					Debug.Log("- " + obj);
//				}
//			}
//		}

//		if(GUILayout.Button("Click Me"))
//        {
//			Debug.Log("Clicked the button");
//        }
//	}
//}
