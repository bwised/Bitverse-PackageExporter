using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Bitverse.PackageExporter.Editor
{
	[CustomEditor(typeof(AssetCollection))]
	public sealed class PackageExportCollectionInspector : UnityEditor.Editor
	{
		#region Unity
		void OnEnable()
		{
			assetCollection = (AssetCollection)target;

			// properties
			assets = serializedObject.FindProperty("assets");
			
			// meta
			for (int i=0; i<assets.arraySize; i++)
			{
				assetsList.Add((Object)serializedObject.FindProperty("assets.Array.data["+i+"]").objectReferenceValue);
			}
			CalculateUnincludedDependencies();

			includeCollectionAsset = EditorPrefs.GetBool(Module.GetEditorPrefsKey("includeCollectionAsset"), true);
			includeDatetimeInFilename = EditorPrefs.GetBool(Module.GetEditorPrefsKey("includeDatetimeInFilename"), true);
			exportPackageOptions = (ExportPackageOptions)EditorPrefs.GetInt(Module.GetEditorPrefsKey("exportPackageOptions"), (int)ExportPackageOptions.IncludeDependencies);

			EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemOnGUI;
		}

		void OnDisable()
		{
			EditorPrefs.SetBool(Module.GetEditorPrefsKey("includeCollectionAsset"), includeCollectionAsset);
			EditorPrefs.SetBool(Module.GetEditorPrefsKey("includeDatetimeInFilename"), includeDatetimeInFilename);
			EditorPrefs.SetInt(Module.GetEditorPrefsKey("exportPackageOptions"), (int)exportPackageOptions);

			EditorApplication.projectWindowItemOnGUI -= OnProjectWindowItemOnGUI;
		}
		
		public override void OnInspectorGUI()
		{
			bool isGUIEnabled = GUI.enabled;
			Color origGUIColor = GUI.color;
			
			serializedObject.Update();
			GUILayout.Space(4);
			
			// assets
			//GUILayout.Space(4);
			GUILayout.Label("Assets", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal("box", GUILayout.Height(48));
			GUILayout.FlexibleSpace();
			GUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Drag Existing Project Assets to Add", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(4);

			EventType eventType = Event.current.type;
			switch (eventType)
			{
			case EventType.MouseDrag:
				DragAndDrop.PrepareStartDrag();
				Event.current.Use();
				break;
				
			case EventType.DragUpdated:
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				Event.current.Use();
				break;
			case EventType.DragPerform:
				DragAndDrop.AcceptDrag();
				Object[] objectsDraggedIn = DragAndDrop.objectReferences;
				foreach (Object objectDraggedIn in objectsDraggedIn)
				{
					// make sure it's a project asset
					if (!AssetDatabase.Contains(objectDraggedIn))
						continue;
					
					// folder
					if (AssetIsFolder(objectDraggedIn))
					{
						Object[] subAssets = GatherAssetsInFolder(objectDraggedIn);
						if (subAssets.Length > 0)
						{
							foreach (Object subAsset in subAssets)
							{
								if (!(subAsset is AssetCollection) && !AssetIsAlreadyIncluded(subAsset))
								{
									assetsToAdd.Add(subAsset);
								}
							}
						}
					}
					
					// individual asset
					else if (!AssetIsAlreadyIncluded(objectDraggedIn))
					{
						assetsToAdd.Add(objectDraggedIn);
					}

				}
				Event.current.Use();
				break;
			}
			foreach (Object assetToAdd in assetsToAdd)
			{
				assets.arraySize++;
				SerializedProperty newAsset = serializedObject.FindProperty("assets.Array.data["+(assets.arraySize-1)+"]");
				newAsset.objectReferenceValue = assetToAdd;
				assetsList.Add(assetToAdd);
			}
			if (assetsToAdd.Count > 0)
				CalculateUnincludedDependencies();
			assetsToAdd.Clear();

			int assetToRemove = -1;
			assetListScrollPosition = EditorGUILayout.BeginScrollView(assetListScrollPosition);
			for (int i=0; i<assets.arraySize; i++)
			{
				SerializedProperty asset = serializedObject.FindProperty("assets.Array.data["+i+"]");
				if (asset.objectReferenceValue != null)
				{
					asset.objectReferenceValue = EditorGUILayout.ObjectField(asset.objectReferenceValue, typeof(Object), false);
					GUI.color = origGUIColor;
				}
				else
				{
					asset.objectReferenceValue = EditorGUILayout.ObjectField(new GUIContent("Asset "+i), asset.objectReferenceValue, typeof(Object), false);
					assetToRemove = i;
				}				
			}
			if (assetToRemove >= 0)
			{
				for (int i=assetToRemove; i<assets.arraySize-1; i++)
				{
					assets.MoveArrayElement(i+1, i);
				}
				assets.arraySize--;
				assetsList.RemoveAt(assetToRemove);	
				CalculateUnincludedDependencies();
			}
			EditorGUILayout.EndScrollView();

			GUILayout.FlexibleSpace();

			if (unincludedAssetsList != null && unincludedAssetsList.Count > 0)
			{
				GUILayout.BeginVertical(GUILayout.MaxHeight(128));
				GUILayout.Label("Unincluded Dependencies", EditorStyles.boldLabel);
				dependencyListScrollPosition = EditorGUILayout.BeginScrollView(dependencyListScrollPosition);
				foreach (Object unincludedAsset in unincludedAssetsList)
				{
					EditorGUILayout.ObjectField(unincludedAsset, typeof(Object), false);
				}
				EditorGUILayout.EndScrollView();
				GUILayout.EndVertical();
			}

			EditorGUILayout.BeginVertical();
			GUILayout.Label("Settings", EditorStyles.boldLabel);
			
			serializedObject.ApplyModifiedProperties();		
			
			// package building
			GUI.enabled = isGUIEnabled;
			includeCollectionAsset = EditorGUILayout.Toggle(new GUIContent("Include Collection Asset"), includeCollectionAsset);
			includeDatetimeInFilename = EditorGUILayout.Toggle(new GUIContent("Date/Time in Filename"), includeDatetimeInFilename);
			exportPackageOptions = (ExportPackageOptions)EditorGUILayout.EnumMaskField("Export Options", exportPackageOptions);
			GUILayout.Space(8);
			if (GUILayout.Button("Create Package"))
			{
				if (assetsList.Count == 0)
					EditorUtility.DisplayDialog("No Assets", "This asset collection has no assets assigned to it.", "OK");
				else
					ExportPackage();
			}
			EditorGUILayout.EndVertical();
			GUI.enabled = isGUIEnabled;
		}
		#endregion

		#region Handlers
		void OnProjectWindowItemOnGUI(string guid, Rect selectionRect)
		{
			DrawStatusIcon(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), typeof(Object)), selectionRect);
		}
		#endregion
		
		#region Private
		private Vector2 assetListScrollPosition = Vector2.zero;
		private Vector2 dependencyListScrollPosition = Vector2.zero;
		private AssetCollection assetCollection;
		private SerializedProperty assets;
		private List<Object> assetsToAdd = new List<Object>();
		private List<Object> assetsList = new List<Object>();
		private List<Object> unincludedAssetsList = new List<Object>();
		private bool includeCollectionAsset;
		private bool includeDatetimeInFilename;
		private ExportPackageOptions exportPackageOptions;

		private const int iconSize = 8;
		private const int borderSize = 1;
		private static readonly Color pastelBlue = new Color(0.3f, 0.55f, 0.85f);
		//private static readonly Color lightgrey = new Color(0.55f, 0.55f, 0.55f);
		private static readonly StatusIcon includedIcon = new StatusIcon(pastelBlue, pastelBlue, iconSize, borderSize);

		private class StatusIcon
		{
			public enum IconType
			{
				Normal,
				Small,
				Hollow
			}
			
			public StatusIcon(Color color)
			{
				Initilize(color, color, 8, 1);
			}
			
			public StatusIcon(Color color, Color borderColor, int iconSize, int borderSize)
			{
				Initilize(color, borderColor, iconSize, borderSize);
			}
			
			public Texture2D GetTexture(IconType iconType)
			{
				switch (iconType)
				{
				case IconType.Small:
					return smallIcon;
				case IconType.Hollow:
					return hollowIcon;
				case IconType.Normal:
				default:
					return icon;
				}
			}
			
			void Initilize(Color color, Color borderColor, int iconSize, int borderSize)
			{
				icon = CreateSquareTextureWithBorder(iconSize, borderSize, color, borderColor);
				smallIcon = CreateSquareTextureWithBorder(iconSize, iconSize / 4, color, new Color(1, 1, 1, 0));
				hollowIcon = CreateSquareTextureWithBorder(iconSize, borderSize, new Color(1, 1, 1, 0), color);
			}
			
			Texture2D icon;
			Texture2D smallIcon;
			Texture2D hollowIcon;
		}

		private void DrawStatusIcon(Object obj, Rect rect)
		{
			Rect iconRect = GetRightAligned(rect, iconSize);
			DrawIcon(iconRect, StatusToGUIContent(obj), obj);
		}

		private static Rect GetRightAligned(Rect rect, float size)
		{
			float border = (rect.height - size);
			rect.x = rect.width - size - (border / 2.0f);
			rect.width = size;
			rect.y = rect.y + border / 2.0f;
			rect.height = size;
			return rect;
		}

		private GUIContent StatusToGUIContent(Object obj)
		{
			if (assetsList.Contains(obj))
				return new GUIContent(includedIcon.GetTexture(StatusIcon.IconType.Normal), "Included");
			return new GUIContent("-");
		}

		private static void DrawIcon(Rect rect, GUIContent content, Object obj)
		{
			if (content.image) GUI.DrawTexture(rect, content.image);
		}

		private void CalculateUnincludedDependencies()
		{
			unincludedAssetsList.Clear();
			if (assetsList.Count > 0)
			{
				Object[] allDependencies = EditorUtility.CollectDependencies(assetsList.ToArray());
				unincludedAssetsList.AddRange(allDependencies);
				foreach (Object includedAssets in assetsList)
				{
					if (unincludedAssetsList.Contains(includedAssets))
						unincludedAssetsList.Remove(includedAssets);
				}

				// remove ones from the list that aren't their own entity on disk
				List<Object> objectsToIgnore = new List<Object>();
				foreach (Object unincludedAsset in unincludedAssetsList)
				{
					if (!AssetDatabase.IsMainAsset(unincludedAsset))
						objectsToIgnore.Add(unincludedAsset);
				}
				foreach (Object objectToIgnore in objectsToIgnore)
				{
					unincludedAssetsList.Remove(objectToIgnore);
				}
			}
		}

		private static Texture2D CreateSquareTextureWithBorder(int size, int borderSize, Color inner, Color outer)
		{
			Color[] colors = new Color[size * size];
			for (int x = 0; x < size; ++x)
			{
				for (int y = 0; y < size; ++y)
				{
					if (x < borderSize || x >= size - borderSize || y < borderSize || y >= size - borderSize)
					{
						colors[x + y * size] = outer;
					}
					else
					{
						colors[x + y * size] = inner;
					}
				}
			}
			
			Texture2D iconTexture = new Texture2D(size, size) { hideFlags = HideFlags.HideAndDontSave };
			iconTexture.SetPixels(colors);
			iconTexture.wrapMode = TextureWrapMode.Clamp;
			iconTexture.filterMode = FilterMode.Point;
			iconTexture.Apply();
			return iconTexture;
		}

		private bool AssetIsAlreadyIncluded(Object asset)
		{
			bool alreadyInList = false;
			foreach (Object a in assetsList)
			{
				if (a == asset)
				{
					alreadyInList = true;
					break;
				}
			}
			return alreadyInList;
		}
		
		private bool AssetIsFolder(Object asset)
		{
			if (AssetDatabase.IsSubAsset(asset))
				return false;
			string assetPath = AssetDatabase.GetAssetPath(asset);
			FileAttributes attr = File.GetAttributes(assetPath);
			return (attr & FileAttributes.Directory) == FileAttributes.Directory;
		}

		// pre-condition: "directory" parameter is an asset that is *definitely* a folder
		private Object[] GatherAssetsInFolder(Object folderAsset)
		{
			List<Object> subAssets = new List<Object>();
			string folderAssetPath = AssetDatabase.GetAssetPath(folderAsset);
			string applicationDataPath  = Application.dataPath;
			string folderPath = applicationDataPath.Substring(0 ,applicationDataPath.Length-6) + folderAssetPath;
			string[] subFilePaths = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
			if (subFilePaths != null)
			{
				foreach (string subFilePath in subFilePaths)
				{
					if (subFilePath.EndsWith(".meta"))
						continue;

					int indexOfAssets = subFilePath.IndexOf("/Assets");
					string subAssetPath = subFilePath.Substring(indexOfAssets+1);
					Object asset =  AssetDatabase.LoadAssetAtPath(subAssetPath,typeof(Object));
					subAssets.Add(asset);
				}
			}
			return subAssets.ToArray();
		}

		private void ExportPackage()
		{
			string assetCollectionName = assetCollection.name.Substring(0, assetCollection.name.IndexOf("."));

			string filename;
			if (includeDatetimeInFilename)
				filename = string.Format("{0}/{1}-{2}.unitypackage", "..", assetCollectionName, System.DateTime.Now.ToString("yyyyMMddHHmmss"));
			else
				filename = string.Format("{0}/{1}.unitypackage", "..", assetCollectionName);
			
			List<string> assetPaths = new List<string>(assetsList.Count);
			foreach (Object asset in assetsList)
			{
				assetPaths.Add(AssetDatabase.GetAssetPath(asset));
			}
			if (includeCollectionAsset)
			{
				assetPaths.Add(AssetDatabase.GetAssetPath(assetCollection));
			}

			AssetDatabase.SaveAssets();
			EditorUtility.DisplayProgressBar("Exporting Package...", string.Format("... exporting to {0}", filename), 0f);
			AssetDatabase.ExportPackage(assetPaths.ToArray(), filename, exportPackageOptions);
			EditorUtility.ClearProgressBar();
			Debug.Log("Created package: "+filename);
		}

		#endregion
	}
}