using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildPlayer
{
    [MenuItem("Build/Windows64")]
    public static void Build(){
        var scenes=new[]{"Assets/Scenes/SampleScene.unity"};
        var opt=new BuildPlayerOptions{
            scenes=scenes,
            locationPathName="Build/FarmCards.exe",
            target=BuildTarget.StandaloneWindows64
        };
        var report=BuildPipeline.BuildPlayer(opt);
        var r=report.summary.result;
        UnityEngine.Debug.Log("BUILD_RESULT "+(r==BuildResult.Succeeded?"OK":"FAILED")+" totalErrors="+report.summary.totalErrors+" size="+report.summary.totalSize);
        if(r==BuildResult.Succeeded) { try { EditorApplication.Exit(0); } catch{} }
        else { try { EditorApplication.Exit(1); } catch{} }
    }
}
