using System;
using System.Collections.Generic;
using UnityEngine;

// ============ 软件光栅化器：移植浏览器 Canvas 2D 的绘制子集 ============
// 用于把远征地图的地形块离线烘焙成纹理，以及把粒子/弹道/天气等动态特效逐帧画到一张叠加纹理。
// 支持：矩形、圆弧/圆、椭圆、圆角矩形、折线/二次贝塞尔路径（描边+填充）、
//       纯色/线性渐变/径向渐变填充、全局透明度、仿射变换(平移/旋转/缩放)、
//       混合模式 (SourceOver / DestOut / Screen)、纹理 blit(drawImage)。
public class Canvas2D
{
    public Color32[] px;
    public int w, h;
    public float globalAlpha = 1f;
    public int composite = 0; // 0 source-over, 1 destination-out, 2 screen
    public Color fillColor = Color.white;
    public Color strokeColor = Color.white;
    public float lineWidth = 1f;
    public bool lineCapRound = true;

    private struct Xf { public float a,b,c,d,tx,ty; }
    private List<Xf> stack = new List<Xf>();
    private Xf cur = new Xf{ a=1, d=1 };

    public Canvas2D(int width, int height)
    {
        w = width; h = height;
        px = new Color32[w * h];
    }

    public void SaveTransform() { stack.Add(cur); }
    public void RestoreTransform() { if (stack.Count > 0) cur = stack[stack.Count-1]; }
    public void Translate(float x, float y) { cur.tx += x; cur.ty += y; }
    public void Rotate(float rad) { float c = (float)Math.Cos(rad), s = (float)Math.Sin(rad); Mul2x2(c, s, -s, c); }
    public void Scale(float sx, float sy) { Mul2x2(sx, 0, 0, sy); }
    private void Mul2x2(float a2,float b2,float c2,float d2)
    {
        float na = cur.a*a2 + cur.b*c2, nb = cur.a*b2 + cur.b*d2;
        float nc = cur.c*a2 + cur.d*c2, nd = cur.c*b2 + cur.d*d2;
        cur.a=na; cur.b=nb; cur.c=nc; cur.d=nd;
    }
    private float X(float x, float y) { return cur.a*x + cur.c*y + cur.tx; }
    private float Y(float x, float y) { return cur.b*x + cur.d*y + cur.ty; }

    public void Clear() { Array.Clear(px, 0, px.Length); }

    public void FillRect(float x, float y, float ww, float hh, Color c)
    {
        float[] poly = { x,y, x+ww,y, x+ww,y+hh, x,y+hh };
        FillPoly(poly, c, null, null);
    }

    public void StrokeRect(float x, float y, float ww, float hh, Color c, float lw)
    {
        // four capsuled strokes
        StrokeLine(x, y, x+ww, y, c, lw);
        StrokeLine(x+ww, y, x+ww, y+hh, c, lw);
        StrokeLine(x+ww, y+hh, x, y+hh, c, lw);
        StrokeLine(x, y+hh, x, y, c, lw);
    }

    public void RoundRect(float x, float y, float ww, float hh, float r, Color c)
    {
        float[] poly = new float[4*(5+1)*2];
        int n = 0;
        void add(float fx, float fy) { poly[n++]=fx; poly[n++]=fy; }
        int seg = 5;
        // corners
        AddArc(poly, ref n, x+r,y+r, r, (float)Math.PI, (float)Math.PI*1.5f, seg);
        AddArc(poly, ref n, x+ww-r, y+r, r, (float)Math.PI*1.5f, (float)Math.PI*2, seg);
        AddArc(poly, ref n, x+ww-r, y+hh-r, r, 0, (float)Math.PI*0.5f, seg);
        AddArc(poly, ref n, x+r, y+hh-r, r, (float)Math.PI*0.5f, (float)Math.PI, seg);
        FillPoly(poly, c, null, null);
    }

    private static void AddArc(float[] poly, ref int n, float cx, float cy, float r, float a0, float a1, int seg)
    {
        for (int i=0;i<=seg;i++){ float a=a0+(a1-a0)*i/seg; poly[n++]=cx+(float)Math.Cos(a)*r; poly[n++]=cy+(float)Math.Sin(a)*r; }
    }

    public void FillCircle(float cx, float cy, float r, Color c)
    {
        if (r <= 0) return;
        float[] poly = new float[(16+1)*2]; int n=0;
        AddArc(poly, ref n, cx, cy, r, 0, (float)Math.PI*2, 16);
        FillPoly(poly, c, null, null);
    }

    public void StrokeCircle(float cx, float cy, float r, Color c, float lw)
    {
        if (r <= 0) return;
        int seg = Math.Max(8, (int)(r*2));
        float prevx = 0, prevy = 0;
        for (int i=0;i<=seg;i++){ float a=(float)Math.PI*2*i/seg; float x=cx+(float)Math.Cos(a)*r, y=cy+(float)Math.Sin(a)*r; if(i>0) StrokeLine(prevx,prevy,x,y,c,lw); prevx=x; prevy=y; }
    }

    public void FillEllipse(float cx, float cy, float rx, float ry, Color c, float rot=0)
    {
        SaveTransform(); Translate(cx, cy); Rotate(rot);
        float[] poly = new float[(16)*2]; int n=0;
        for (int i=0;i<16;i++){ float a=(float)Math.PI*2*i/16; poly[n++]=(float)Math.Cos(a)*rx; poly[n++]=(float)Math.Sin(a)*ry; }
        FillPoly(poly, c, null, null);
        RestoreTransform();
    }

    public void FillPoly(float[] pts, Color c, Color[] gradColors, float[] gradStops)
    {
        // transform points
        float[] tp = new float[pts.Length];
        float minx=float.MaxValue,maxx=float.MinValue,miny=float.MaxValue,maxy=float.MinValue;
        for (int i=0;i<pts.Length;i+=2){ float vx=X(pts[i],pts[i+1]), vy=Y(pts[i],pts[i+1]); tp[i]=vx; tp[i+1]=vy;
            if(vx<minx)minx=vx; if(vx>maxx)maxx=vx; if(vy<miny)miny=vy; if(vy>maxy)maxy=vy; }
        int x0=(int)Math.Floor(minx), x1=(int)Math.Ceiling(maxx), y0=(int)Math.Floor(miny), y1=(int)Math.Ceiling(maxy);
        if (x1-x0<=0||y1-y0<=0) return;
        // clamp to canvas
        int cx0=Math.Max(0,x0), cx1=Math.Min(w,x1), cy0=Math.Max(0,y0), cy1=Math.Min(h,y1);
        int n = pts.Length/2;
        for (int y=cy0;y<cy1;y++) for (int x=cx0;x<cx1;x++)
        {
            if (!PointInPoly(tp, n, x+0.5f, y+0.5f)) continue;
            Color col = c;
            if (gradColors != null) {
                float t = (float)Math.Sqrt(((x+0.5)-minx)*((x+0.5)-minx)+((y+0.5)-miny)*((y+0.5)-miny)) / Math.Max(1, Math.Max(maxx-minx, maxy-miny));
                col = SampleGradient(gradColors, gradStops, t);
            }
            Blend(x, y, col);
        }
    }

    // Linear gradient fill over a normalized box (map t across bounding box). colorStops[0..1]
    public void FillRectLinearGrad(float x, float y, float ww, float hh, Color[] colors, float[] stops, bool vertical)
    {
        float[] poly = { x,y, x+ww,y, x+ww,y+hh, x,y+hh };
        // approximate vertical/horizontal gradient via per-pixel t
        int x0=Math.Max(0,(int)x), x1=Math.Min(w,(int)(x+ww)), y0=Math.Max(0,(int)y), y1=Math.Min(h,(int)(y+hh));
        for (int py=y0;py<y1;py++) for (int pxx=x0;pxx<x1;pxx++)
        {
            float t = vertical ? (py-y)/(float)Math.Max(1,hh) : (pxx-x)/(float)Math.Max(1,ww);
            Color col = SampleGradient(colors, stops, t);
            Blend(pxx, py, col);
        }
    }

    public void FillRadialGrad(float cx, float cy, float r0, float r1, Color[] colors, float[] stops, bool erase=false)
    {
        int x0=Math.Max(0,(int)(cx-r1)), x1=Math.Min(w,(int)(cx+r1)), y0=Math.Max(0,(int)(cy-r1)), y1=Math.Min(h,(int)(cy+r1));
        for (int py=y0;py<y1;py++) for (int pxx=x0;pxx<x1;pxx++)
        {
            float d=(float)Math.Sqrt((pxx+0.5-cx)*(pxx+0.5-cx)+(py+0.5-cy)*(py+0.5-cy));
            if (d>r1) continue;
            float t = r1<=r0?0:(d-r0)/Math.Max(0.0001f,(r1-r0));
            Color col = SampleGradient(colors, stops, t);
            Blend(pxx, py, col);
        }
    }

    private static Color SampleGradient(Color[] colors, float[] stops, float t)
    {
        t = Mathf.Clamp01(t);
        for (int i=0;i<stops.Length-1;i++){
            if (t<=stops[i+1] && t>=stops[i]){
                float f = (t-stops[i])/Math.Max(0.0001f,(stops[i+1]-stops[i]));
                return Color.Lerp(colors[i], colors[i+1], f);
            }
        }
        return colors[colors.Length-1];
    }

    // capsuled round-cap stroke between two transformed points
    public void StrokeLine(float x0,float y0,float x1,float y1, Color c, float lw)
    {
        float ax=X(x0,y0), ay=Y(x0,y0), bx=X(x1,y1), by=Y(x1,y1);
        float r=lw*0.5f;
        float dx=bx-ax, dy=by-ay; float len=(float)Math.Sqrt(dx*dx+dy*dy);
        if (len<0.0001f){ FillCircle(ax,ay,r,c); return; }
        float nx=-dy/len*r, ny=dx/len*r;
        float[] poly={ ax+nx,ay+ny, bx+nx,by+ny, bx-nx,by-ny, ax-nx,ay-ny };
        FillPoly(poly, c, null, null);
        FillCircle(ax,ay,r,c); FillCircle(bx,by,r,c);
    }

    public void StrokePolyLine(float[] pts, Color c, float lw)
    {
        for (int i=0;i+3<pts.Length;i+=2) StrokeLine(pts[i],pts[i+1],pts[i+2],pts[i+3],c,lw);
    }

    // quadratic bezier as flattened polyline stroke
    public void StrokeQuadCurve(float x0,float y0,float cx,float cy,float x1,float y1, Color c, float lw, int seg=12)
    {
        float px=x0, py=y0;
        for (int i=1;i<=seg;i++){ float t=i/(float)seg, mt=1-t;
            float qx=mt*mt*x0+2*mt*t*cx+t*t*x1, qy=mt*mt*y0+2*mt*t*cy+t*t*y1;
            StrokeLine(px,py,qx,qy,c,lw); px=qx; py=qy; }
    }

    // Quadratic-curve closed blob fill (used by terrain patches/fields)
    public void FillBlob(float cx, float cy, float rx, float ry, float rot, float phase, Color c, float wobbleAmt=0.08f)
    {
        SaveTransform(); Translate(cx, cy); Rotate(rot);
        float[] poly = new float[(20+1)*2]; int n=0;
        for (int i=0;i<=20;i++){ float a=(float)(i/20.0*Math.PI*2);
            float wob = 1 + (float)Math.Sin(a*3+phase)*wobbleAmt + (float)Math.Sin(a*5-phase)*wobbleAmt*0.55f;
            float pxx=(float)Math.Cos(a)*rx*wob, pyy=(float)Math.Sin(a)*ry*wob;
            poly[n++]=pxx; poly[n++]=pyy; }
        FillPoly(poly, c, null, null);
        RestoreTransform();
    }

    // Image blit (with alpha) at dest screen position
    public void DrawImage(Color32[] src, int sw, int sh, float dx, float dy)
    {
        for (int sy=0; sy<sh; sy++) for (int sx=0; sx<sw; sx++){
            int dxp=(int)(dx)+sx, dyp=(int)(dy)+sy;
            if (dxp<0||dxp>=w||dyp<0||dyp>=h) continue;
            Blend(dxp, dyp, src[sy*sw+sx]);
        }
    }

    public void Blend(int x, int y, Color c)
    {
        if (x<0||x>=w||y<0||y>=h) return;
        int i = y*w + x;
        Color32 src = c;
        Color32 dst = px[i];
        float sa = src.a/255f * globalAlpha;
        float da = dst.a/255f;
        byte r,g,b,a;
        if (composite==1) { // destination-out: erase where src alpha
            a = (byte)(dst.a * (1-sa)); r=dst.r; g=dst.g; b=dst.b;
        } else if (composite==2) { // screen
            float na = sa + da*(1-sa);
            float fr = (src.r/255f), fg=src.g/255f, fb=src.b/255f, dr=dst.r/255f, dg=dst.g/255f, db=dst.b/255f;
            float sr = 1-(1-fr)*(1-dr), sg=1-(1-fg)*(1-dg), sb=1-(1-fb)*(1-db);
            if (na<=0){ r=0;g=0;b=0;a=0; } else { r=(byte)(sr*255); g=(byte)(sg*255); b=(byte)(sb*255); a=(byte)(na*255); }
        } else { // source-over
            float na = sa + da*(1-sa);
            if (na<=0){ r=0;g=0;b=0;a=0; } else {
                r=(byte)((src.r/255f*sa + dst.r/255f*da*(1-sa))/na*255);
                g=(byte)((src.g/255f*sa + dst.g/255f*da*(1-sa))/na*255);
                b=(byte)((src.b/255f*sa + dst.b/255f*da*(1-sa))/na*255);
                a=(byte)(na*255);
            }
        }
        px[i]=new Color32(r,g,b,a);
    }

    public void PutPixel(int x,int y,Color c){ int i=y*w+x; if(x<0||x>=w||y<0||y>=h)return; px[i]=c; }

    private static bool PointInPoly(float[] poly, int n, float x, float y)
    {
        bool inside=false;
        for (int i=0,j=n-1;i<n;j=i++){
            float xi=poly[i*2], yi=poly[i*2+1], xj=poly[j*2], yj=poly[j*2+1];
            if (((yi>y)!=(yj>y)) && (x < (xj-xi)*(y-yi)/(yj-yi)+xi)) inside=!inside;
        }
        return inside;
    }

    // Raster-compatible fill of a polygon (used for hero/creature fallback shapes) — simple alias for FillPoly
    public void FillPathPoly(float[] pts, Color c) { FillPoly(pts, c, null, null); }

    public Color32[,] ToTexture2D(ref Texture2D tex)
    {
        if (tex==null) tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply(false);
        return null;
    }

    public void UploadTo(Texture2D tex)
    {
        if (tex==null) return;
        tex.SetPixels32(px);
        tex.Apply(false);
    }
}
