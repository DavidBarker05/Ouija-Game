using System.IO;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class PostBuildCopy : IPostprocessBuildWithReport
{
	public int callbackOrder => 0;

	public void OnPostprocessBuild(BuildReport report)
	{
		string projectPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')).Replace('/', '\\');
		string buildPath = Path.GetDirectoryName(report.summary.outputPath);
		CopyFile("StoryModel.txt", projectPath, buildPath);
		CopyFile("OuijaModel.txt", projectPath, buildPath);
		CopyFile("Windows_Ollama_Setup.exe", projectPath, buildPath);
		CopyFile("Linux_Ollama_Setup", projectPath, buildPath);
		//CopyFile("Apple_Ollama_Setup.app", applicationPath, buildPath); // Left out because don't own a Mac and don't feel like illegally using a VM for MacOS
		CopyFile("Ollama_Setup.py", projectPath, buildPath);
		string projectOuijaSetupPath = Path.Combine(projectPath, "OuijaSetup");
		string buildOuijaSetupPath = Path.Combine(buildPath, "OuijaSetup");
		CopyFile("OuijaSetup.cpp", projectOuijaSetupPath, buildOuijaSetupPath);
		CopyFile("CMakeLists.txt", projectOuijaSetupPath, buildOuijaSetupPath);
	}

	void CopyFile(string fileName, string fromDir, string toDir)
	{
		string fromPath = Path.Combine(fromDir, fileName);
		if (!Directory.Exists(toDir)) Directory.CreateDirectory(toDir);
		string toPath = Path.Combine(toDir, fileName);
		if (!File.Exists(fromPath))
		{
			Debug.LogError($"{fileName} doesn't exist within {fromDir}");
			return;
		}
		string fileContents = File.ReadAllText(fromPath);
		File.WriteAllText(toPath, fileContents);
	}
}
