using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 从 StreamingAssets 加载精灵（转换后的 PNG），带缓存
public static class SpriteStore
{
    static Dictionary<string,Sprite> cache = new Dictionary<string,Sprite>();
    static HashSet<string> missing = new HashSet<string>();
    public static Sprite Get(string rel){
        if(cache.TryGetValue(rel,out var s)) return s;
        string path = Path.Combine(Application.streamingAssetsPath,"sprites",rel+".png");
        if(!File.Exists(path)){ if(missing.Add(rel)) Debug.LogWarning("SPRITE_MISSING "+rel+" path="+path); return null; }
        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2,2,TextureFormat.RGBA32,false); tex.LoadImage(bytes);
        tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
        var spr = Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(0.5f,0.5f),100f);
        cache[rel]=spr; Debug.Log("SPRITE_LOADED "+rel+" "+tex.width+"x"+tex.height); return spr;
    }
    public static Sprite Monster(string type,string state){ return Get("monsters/"+type+"-"+state); }
    public static Sprite Obstacle(string type){ return Get("obstacles/"+type); }
    public static Sprite Boss(int tier){ return Get("bosses/"+(tier<=2?"t"+tier+"-stone-maw":"t"+(tier-1)+"-storm-drake")); }
    public static Sprite Map(string id){
        string file=id=="t1"?"t1-spring-forest":id=="t2"?"t2-golden-city":id=="t3"?"t3-ice-canyon":"t4-night-stars";
        return Get("maps/"+file);
    }
}
