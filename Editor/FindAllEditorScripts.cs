using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Compilation;

public class FindAllEditorScripts : MonoBehaviour
{

    [MenuItem("Window/Goodgulf/Seperate Emerald 2024 Editor Scripts")]
    private static void FindAllEmeraldEditorScriptsInProject()
    {
        Debug.Log("Step 1: get all Editor scripts in this project");
        string[] editorScriptsInProject = AssetDatabase.FindAssets("a:assets glob:\"Editor/*\"");
        Debug.Log($"Found {editorScriptsInProject.Length} Editor Scripts");

        Debug.Log("Step 2: filter to get Emerald scripts only");
        List<string> emeraldEditorScripts = new List<string>();
        foreach (var guid in editorScriptsInProject)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if(path.Contains("Emerald AI"))
                emeraldEditorScripts.Add(path);
        }
        Debug.Log($"Found {emeraldEditorScripts.Count} Emerald Editor Scripts");
        
        Debug.Log("Step 3: find the root folder");
        string rootFolder="";
        string[] foldersInProject = AssetDatabase.FindAssets("a:assets glob:\"Emerald AI\"");

        if (foldersInProject.Length > 1)
        {
            rootFolder = AssetDatabase.GUIDToAssetPath(foldersInProject[0]);
            Debug.LogWarning($"Found more than one Emerald AI folder, picking the first.");
        }
        else if (foldersInProject.Length == 1)
        {
            rootFolder = AssetDatabase.GUIDToAssetPath(foldersInProject[0]);
        }
        else
        {
            Debug.LogError("Did not find Emerald AI folder. Did you rename it?");
            return;
        }
        Debug.Log($"Found Emerald AI folder: {rootFolder}");        
      
        Debug.Log("Step 4: create the new EditorScripts folder");
        string folderGuid = AssetDatabase.CreateFolder(rootFolder, "EditorScripts");
        string newFolderPath = $"{rootFolder}/EditorScripts";

        foreach (string emeraldEditorScript in emeraldEditorScripts)
        {
            string assetName = AssetDatabase.LoadMainAssetAtPath(emeraldEditorScript).name;
            AssetDatabase.MoveAsset(emeraldEditorScript, $"{newFolderPath}/{assetName}.cs");
        }

        Debug.Log("Step 5: create the Assembly Definitions");
        // Get a reference to the Animation Rigging package:
        string riggingPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("Unity.Animation.Rigging");
        string riggingAssetGuid = AssetDatabase.AssetPathToGUID(riggingPath);
        string riggingAsmDefAssetGuid = CompilationPipeline.GUIDToAssemblyDefinitionReferenceGUID(riggingAssetGuid);
        
        // Create the EmeraldAI Assembly Definition File 
        string emeraldScriptsPath = $"{rootFolder}/Scripts/EmeraldAI.asmdef";
        string emeraldAIAsmdef = $"{{\n    \"name\": \"EmeraldAI\",\n    \"rootNamespace\": \"\",\n    \"references\": [\n        \"{riggingAsmDefAssetGuid}\"\n    ],\n    \"includePlatforms\": [],\n    \"excludePlatforms\": [],\n    \"allowUnsafeCode\": false,\n    \"overrideReferences\": false,\n    \"precompiledReferences\": [],\n    \"autoReferenced\": true,\n    \"defineConstraints\": [],\n    \"versionDefines\": [],\n    \"noEngineReferences\": false\n}}";
        File.WriteAllText(emeraldScriptsPath, emeraldAIAsmdef);
        AssetDatabase.ImportAsset(emeraldScriptsPath);
        AssetDatabase.Refresh();
        // Create the EmeraldAI Assembly Definition File GUID Reference
        string emeraldAIAssetGuid = AssetDatabase.AssetPathToGUID(emeraldScriptsPath);
        string emeraldAIAsmDefAssetGUID = CompilationPipeline.GUIDToAssemblyDefinitionReferenceGUID(emeraldAIAssetGuid);
        
        // Create the EmeraldAI Editor Assembly Definition File
        string editorAsmdef = $"{{\n    \"name\": \"EmeraldEditor\",\n    \"rootNamespace\": \"\",\n    \"references\": [\n        \"{riggingAsmDefAssetGuid}\",\n        \"{emeraldAIAsmDefAssetGUID}\"\n    ],\n    \"includePlatforms\": [\n        \"Editor\"\n    ],\n    \"excludePlatforms\": [],\n    \"allowUnsafeCode\": false,\n    \"overrideReferences\": false,\n    \"precompiledReferences\": [],\n    \"autoReferenced\": true,\n    \"defineConstraints\": [],\n    \"versionDefines\": [],\n    \"noEngineReferences\": false\n}}";
        string editorAsmdefPath = newFolderPath + "/EmeraldEditor.asmdef";
        File.WriteAllText(editorAsmdefPath, editorAsmdef);
        AssetDatabase.ImportAsset(editorAsmdefPath);
        AssetDatabase.Refresh();
        
        Debug.Log("Ready");
    } 
    
}
