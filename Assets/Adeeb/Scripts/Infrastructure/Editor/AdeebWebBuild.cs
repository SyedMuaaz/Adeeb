using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Adeeb.EditorTools
{
    public static class AdeebWebBuild
    {
        public static void Build() => BuildScene("Adeeb", "WebGL", "web-build-status.txt");
        public static void BuildSearch() => BuildScene("Search", "Task2WebGL", "task2-build-status.txt");
        public static void BuildFinal() => BuildScene("Menu", "FinalWebGL", "final-build-status.txt");
        private static void BuildScene(string sceneName, string outputName, string reportName)
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string output=Path.Combine(root,"Builds",outputName);
            string report=Path.Combine(root,"Builds",reportName);
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            File.WriteAllText(report,"Building");
            try
            {
                PlayerSettings.runInBackground=true;
                PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=sceneName == "Menu" ? new[]{"Assets/Adeeb/Scenes/Menu.unity", "Assets/Adeeb/Scenes/Adeeb.unity", "Assets/Adeeb/Scenes/Search.unity"} : new[]{"Assets/Adeeb/Scenes/"+sceneName+".unity"},
                    locationPathName=output,target=BuildTarget.WebGL,options=sceneName == "Menu" ? BuildOptions.None : BuildOptions.Development
                });
                if(result.summary.result==BuildResult.Succeeded)
                {
                    string page=Path.Combine(output,"index.html");
                    string html=File.ReadAllText(page);
                    string style=@"<style id=""adeeb-responsive"">
html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#eff4fa}
#unity-container.unity-desktop{position:absolute;left:50%;top:50%;transform:translate(-50%,-50%);width:min(100vw,calc(100vh * 16 / 9));height:min(100vh,calc(100vw * 9 / 16))}
#unity-canvas{width:100%!important;height:100%!important;display:block}
#unity-footer{display:none}
</style>";
                    if(!html.Contains("adeeb-responsive"))File.WriteAllText(page,html.Replace("</head>",style+"</head>"));
                }
                File.WriteAllText(report,result.summary.result+"; errors="+result.summary.totalErrors+"; size="+result.summary.totalSize);
                if(result.summary.result!=BuildResult.Succeeded)Debug.LogError("WebGL build failed. See Build report.");
            }
            catch(Exception error){File.WriteAllText(report,error.ToString());Debug.LogException(error);}
        }
    }
}
