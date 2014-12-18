using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Bitverse.PackageExporter.Editor;

public class PackageExporter
{
	static string[] commonAssets = new string[]
	{
		/*
		"Assets/Tools/Bitverse/PackageExporter/Skin/skin-dark.guiskin",
		"Assets/Tools/Bitverse/PackageExporter/Skin/skin-light.guiskin",
		"Assets/Tools/Bitverse/PackageExporter/Skin/edit-dark.png",
		"Assets/Tools/Bitverse/PackageExporter/Skin/edit-light.png",
		*/
	};

	static string[] rawAssets = new string[]
	{
		// dependencies
		
		// editor
		"Assets/Tools/Bitverse/PackageExporter/Editor/Module.cs",
		"Assets/Tools/Bitverse/PackageExporter/Editor/PackageExportCollectionInspector.cs",
		"Assets/Tools/Bitverse/PackageExporter/Editor/PackageExportCollectionManager.cs",

		// base
		"Assets/Tools/Bitverse/PackageExporter/Scripts/AssetCollection.cs",
	};

	static string[] distributionAssets = new string[]
	{
		"Assets/Tools/Bitverse/PackageExporter/Editor/PackageExporter.Editor.dll",
		"Assets/Tools/Bitverse/PackageExporter/PackageExporter.Runtime.dll",
	};
	
	[MenuItem("Tools/PackageExporter/Export/Distribution", false, 2000)]
	public static void Export()
	{
		string filename = string.Format("{0}-u{1}-v{2}.unitypackage", "../dist/PackageExporter", Application.unityVersion, Module.Version);
		
		List<string> assets = new List<string>(commonAssets);
		assets.AddRange(distributionAssets);

		AssetDatabase.ExportPackage(assets.ToArray(), filename, ExportPackageOptions.Default);
		Debug.Log("Created package: "+filename);
	}

	[MenuItem("Tools/PackageExporter/Export/Raw", false, 2001)]
	public static void ExportRaw()
	{
		string filename = string.Format("{0}-Raw-u{1}-v{2}.unitypackage", "../dist/PackageExporter", Application.unityVersion, Module.Version);
		
		List<string> assets = new List<string>(commonAssets);
		assets.AddRange(rawAssets);
		
		AssetDatabase.ExportPackage(assets.ToArray(), filename, ExportPackageOptions.Default);
		Debug.Log("Created package: "+filename);
	}
	
}
