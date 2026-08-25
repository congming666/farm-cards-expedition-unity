using System;
using System.Collections.Generic;
using UnityEngine;

// ================= 渲染后端：整帧软件渲染 + OnGUI 呈现（对应网页 canvas 渲染链路） =================
// 帧画布用 y 向下（左上原点）缓冲；UploadTo 时翻转到贴图顺位，GUI.DrawTexture 即正常显示。
public class RenderBackend
{
    public const int VW = 1280, VH = 720;
    public Canvas2D frame;       // 每帧世界渲染缓冲
    public Canvas2D fog;         // 战争迷雾缓冲
    public Texture2D frameTex, fogTex, lastFrameTex;
    public Rect viewRect;        // 16:9 视口（屏幕坐标）
    public bool hasExpedition;

    // 地形块缓存：key = cx,cy -> 缓冲
    public class Chunk { public Color32[] px; public int cw, ch; }
    public Dictionary<long, Chunk> chunkCache = new Dictionary<long, Chunk>();
    public const int CHUNK = 512;

    public RenderBackend()
    {
        frame = new Canvas2D(VW, VH);
        fog = new Canvas2D(VW, VH);
        frameTex = new Texture2D(VW, VH, TextureFormat.RGBA32, false);
        fogTex = new Texture2D(VW, VH, TextureFormat.RGBA32, false);
    }

    public void ClearFrame()
    {
        frame.Clear();
        frame.globalAlpha = 1; frame.composite = 0;
    }

    public void ClearFog()
    {
        fog.Clear();
        fog.globalAlpha = 1; fog.composite = 0;
    }

    // 从缓冲 blit 一张地形块到帧缓冲
    public void BlitChunk(Chunk chunk, float dx, float dy)
    {
        if (chunk == null) return;
        frame.DrawImage(chunk.px, chunk.cw, chunk.ch, dx, dy);
    }

    public Chunk GetChunk(int cx, int cy)
    {
        long key = (long)cx * 100000 + cy;
        Chunk c;
        if (chunkCache.TryGetValue(key, out c)) return c;
        c = BakeChunk(cx, cy);
        chunkCache[key] = c;
        return c;
    }

    // 由 Expedition 通过 TerrainPainter 委托烘焙地形块；默认空实现，由 Expedition 注入
    public Action<Canvas2D, int, int> TerrainPainter;
    Chunk BakeChunk(int cx, int cy)
    {
        int cw = Math.Min(CHUNK, 2400 - cx*CHUNK);
        int ch = Math.Min(CHUNK, 2400 - cy*CHUNK);
        var c2d = new Canvas2D(cw, ch);
        if (TerrainPainter != null) TerrainPainter(c2d, cx*CHUNK, cy*CHUNK);
        var chunk = new Chunk{ px=c2d.px, cw=cw, ch=ch };
        return chunk;
    }

    public void ClearChunks(){ chunkCache.Clear(); }

    // 上传帧/雾到贴图（翻转 y 使 GUI 正确显示）
    public void UploadFrame()
    {
        FlipUpload(frame, frameTex);
    }
    public void UploadFog()
    {
        FlipUpload(fog, fogTex);
    }
    void FlipUpload(Canvas2D c, Texture2D tex)
    {
        Color32[] flipped = new Color32[c.w * c.h];
        for (int y=0;y<c.h;y++){
            int srcRow = y*c.w, dstRow = (c.h-1-y)*c.w;
            Array.Copy(c.px, srcRow, flipped, dstRow, c.w);
        }
        tex.SetPixels32(flipped);
        tex.Apply(false);
    }

    // GUI 呈现：先画帧，再画雾，按 16:9 letterbox
    public void DrawFrameGUI()
    {
        UploadFrame();
        if (hasExpedition) { UploadFog(); }
        GUI.DrawTexture(viewRect, frameTex);
        if (hasExpedition) GUI.DrawTexture(viewRect, fogTex);
    }

    // 视图 rect 计算（16:9 letterbox）
    public void ComputeViewRect(float screenW, float screenH)
    {
        float scale = Math.Min(screenW / (float)VW, screenH / (float)VH);
        float w = VW * scale, h = VH * scale;
        viewRect = new Rect((screenW-w)/2f, (screenH-h)/2f, w, h);
    }
}
