using UnityEditor;
using UnityEngine;

public static class ProjectSetup
{
	[MenuItem("Tools/PackageExporter/Setup", false, 10001)]
	public static void Run()
	{
		PlayerSettings.companyName = "Bitwise Design";
		PlayerSettings.productName = "Bitverse-PackageExporter";
		
		AssetDatabase.SaveAssets();
	}
}
