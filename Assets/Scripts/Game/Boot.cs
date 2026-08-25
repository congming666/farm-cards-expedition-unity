using UnityEngine;
// 自动创建主控制器（无需手动摆放场景对象）
public static class Boot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init(){
        if(GameFlow.I==null){
            var go=new GameObject("GameFlow");
            go.AddComponent<GameFlow>();
        }
    }
}
