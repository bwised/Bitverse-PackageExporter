#!/bin/bash

if [ -f .bitverse_env ]
then
	source .bitverse_env
elif [ -f ~/.bitverse_env ]
then
	source ~/.bitverse_env
fi
export MODULE=PackageExporter
export PACKAGE=${PACKAGE:="Bitverse"}
export UNITY_PATH=${UNITY_PATH:="/Applications/Unity"}

echo "============"
echo "Building ${PACKAGE}/${MODULE} using Unity at path: ${UNITY_PATH}"
echo "============"

# Copying Unity DLLs
echo "... Copying Unity DLLs"
cp ${UNITY_PATH}/Unity.app/Contents/Frameworks/Managed/UnityEditor.dll md/libs-unity/.
cp ${UNITY_PATH}/Unity.app/Contents/Frameworks/Managed/UnityEngine.dll md/libs-unity/.

# Build raw package first
echo "... Exporting raw package"
${UNITY_PATH}/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath `pwd`/unity -logFile unity-export-raw.log -executeMethod PackageExporter.ExportRaw

# Build the release DLLs
echo "... Building DLLs"
cd md
xbuild /property:Project=${MODULE} /property:Configuration=Release /property:Platform=x86 > xbuild-${MODULE}.log
xbuild /property:Project=${MODULE}Editor /property:Configuration=Release /property:Platform=x86 > xbuild-${MODULE}Editor.log
cp bin/Release/${MODULE}.Runtime.dll ../unity-export/Assets/Tools/${PACKAGE}/${MODULE}/.
cp bin/Release/${MODULE}.Editor.dll ../unity-export/Assets/Tools/${PACKAGE}/${MODULE}/Editor/.
cd ..

# Build the distribution package
echo "... Exporting distribution package"
cp unity/Assets/Editor/PackageExporter.cs unity-export/Assets/Editor/PackageExporter.cs
${UNITY_PATH}/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath `pwd`/unity-export -logFile unity-export.log -executeMethod PackageExporter.Export

echo "Build complete."
