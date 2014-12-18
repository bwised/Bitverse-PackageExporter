using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace Bitverse.PackageExporter.Editor
{
	public sealed class PackageExportCollectionManager
	{
		public static string DEFAULT_ASSET_NAME = "New";

		#region Actions
		[MenuItem("Assets/Create/Bitverse/Asset Collection")]
		public static void CreateNew()
		{
			Object[] selectedObjects = Selection.objects;
			Object rootAsset = null;
			foreach (Object selectedObject in selectedObjects)
			{
				if (AssetDatabase.Contains(selectedObject))
				{
					rootAsset = selectedObject;
					break;
				}
			}
			
			string path = "Assets";
			if (rootAsset != null)
			{
				string rootAssetPath = AssetDatabase.GetAssetPath(rootAsset);
				if (Directory.Exists(rootAssetPath))
				{
					path = rootAssetPath;
				}
				else
				{
					path = rootAssetPath.Substring(0, rootAssetPath.LastIndexOf("/"));
				}
			}
			AssetCollection assetCollection = Create(System.String.Format("{0}/{1}", path, DEFAULT_ASSET_NAME));
			Selection.activeObject = assetCollection;
		}

		public static AssetCollection Create(string filePath)
		{
			string tick = string.Empty;
			int tickCnt = 0;
			string path = System.String.Format("{0}{1}.AssetCollection.asset", filePath, tick);
			AssetCollection assetCollection = AssetDatabase.LoadAssetAtPath(path, typeof(AssetCollection)) as AssetCollection;
			while (assetCollection != null)
			{
				tickCnt++;
				tick = System.String.Format(" {0}", tickCnt);
				path = System.String.Format("{0}{1}.AssetCollection.asset", filePath, tick);
				assetCollection = AssetDatabase.LoadAssetAtPath(path, typeof(AssetCollection)) as AssetCollection;
			}
			
			AssetDatabase.CreateAsset(ScriptableObject.CreateInstance(typeof(AssetCollection)), path);
			assetCollection = Load(path.Substring(0, path.LastIndexOf(".asset")));
			EditorUtility.SetDirty(assetCollection);
			AssetDatabase.Refresh();
			
			return assetCollection;
		}

		public static AssetCollection CreateAs(string path, string name)
		{
			string fullPath = System.String.Format("{0}/{1}.AssetCollection.asset", path, name);
			AssetCollection assetCollection = AssetDatabase.LoadAssetAtPath(fullPath, typeof(AssetCollection)) as AssetCollection;
			if (assetCollection != null)
			{
				Debug.LogError("File already exists: "+fullPath);
			}
			else
			{
				AssetDatabase.CreateAsset(ScriptableObject.CreateInstance(typeof(AssetCollection)), fullPath);
				assetCollection = Load(fullPath.Substring(0, fullPath.LastIndexOf(".asset")));
				EditorUtility.SetDirty(assetCollection);
				AssetDatabase.Refresh();
			}
			
			return assetCollection;
		}

		public static AssetCollection Load(string path)
		{
			AssetCollection assetCollection = AssetDatabase.LoadAssetAtPath(path + ".asset", typeof(AssetCollection)) as AssetCollection;
			if (assetCollection == null)
			{
				Debug.LogError("Unable to load package export collection: "+path);
			}
			return assetCollection;
		}
		#endregion
	}
}