using UnityEngine;
using UnityEditor;

namespace Bitverse.PackageExporter.Editor
{
	public static class Module
	{
		public static string Version = "1.1";

		#region Actions
		public static string GetEditorPrefsKey(string key)
		{
			return string.Format("Bitverse.PackageExporter.{0}", key);
		}
		#endregion

		#region Private
		[MenuItem("Tools/Bitverse/PackageExporter/Version", false, 10000)]
		private static void Show()
		{
			EditorUtility.DisplayDialog("Bitverse: PackageExporter", string.Format("(c) 2014 Bitwise Design, Inc.\n\nVersion {0}", Version), "OK");
		}
		#endregion
	}
}
