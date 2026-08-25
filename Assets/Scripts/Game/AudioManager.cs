using System;
using UnityEngine;

// ============ 程序化音频：流式 AudioClip 合成背景乐 + 预生成命中音效 ============
public class AudioManager : MonoBehaviour
{
    static AudioManager inst;
    public static AudioManager I { get { if(inst==null){ var go=new GameObject("AudioManager"); inst=go.AddComponent<AudioManager>(); DontDestroyOnLoad(go); } return inst; } }

    AudioSource bgmSource, sfxSource;
    public bool enabled2 = true;
    public float volume = 0.24f;
    string scene = "menu";
    float phase;              // 采样相位
    int sampleRate = 44100;
    int noteStep;
    float noteTimer;
    AudioClip[] hitClips;

    static float[][] Patterns = {
        new float[]{196f,246.94f,293.66f,246.94f,220f,261.63f,329.63f,261.63f}, // menu
        new float[]{174.61f,220f,261.63f,329.63f,293.66f,246.94f,220f,196f},   // farm
        new float[]{164.81f,196f,246.94f,293.66f,220f,261.63f,329.63f,293.66f},// prep
        new float[]{146.83f,174.61f,196f,233.08f,164.81f,196f,220f,261.63f},   // expedition
        new float[]{196f,246.94f,293.66f,392f,329.63f,293.66f,246.94f,196f},   // result
    };

    void Awake(){
        sampleRate = AudioSettings.outputSampleRate;
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true; bgmSource.volume = 1f; bgmSource.spatialBlend = 0;
        // 只让背景音乐的流式片段调用合成回调。这样同一对象上的音效 AudioSource
        // 不会再重复触发 MonoBehaviour.OnAudioFilterRead。
        bgmSource.clip = AudioClip.Create("ProceduralBGM",sampleRate*2,2,sampleRate,true,OnBgmRead,OnBgmSetPosition);
        bgmSource.Play();
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.spatialBlend = 0;
        hitClips = new AudioClip[]{ MakeHit("normal"), MakeHit("heavy"), MakeHit("kill") };
    }

    void Start(){ SetScene("menu"); }

    public void SetScene(string s){ scene = s; noteStep=0; noteTimer=0; }

    public void PlayHit(string kind){
        if(!enabled2||sfxSource==null) return;
        int idx = kind=="kill"?2:kind=="heavy"?1:0;
        sfxSource.PlayOneShot(hitClips[idx], 0.9f);
    }
    public void PlaySfx(string kind){ PlayHit(kind); }

    public void Toggle(){ enabled2=!enabled2; }
    public void SetVolume(float v){ volume=v; }

    int SceneIndex(){ return scene=="menu"?0:scene=="farm"?1:scene=="prep"?2:scene=="result"?4:3; }

    // 流式片段采样回调：合成双声道背景乐
    void OnBgmRead(float[] data){
        if(!enabled2){ for(int i=0;i<data.Length;i++) data[i]=0; return; }
        var pat = Patterns[SceneIndex()];
        float noteDur = scene=="expedition"?0.52f:1.25f;
        for(int i=0;i<data.Length;i+=2){
            float t = (float)noteTimer;
            int idx = noteStep % pat.Length;
            float freq = pat[idx];
            float env = Mathf.Exp(-3f*t);
            float sample = Sine(freq, phase)*0.045f* env;
            sample += Sine(freq/2f, phase)*0.025f*env;              // sub octave
            sample += Ping2(freq*2f, phase)*0.018f*env;             // faint octave
            data[i]=sample*volume*4f;
            data[i+1]=sample*volume*4f;
            phase += 2f*(float)Math.PI*freq/sampleRate;
            noteTimer += 1f/sampleRate;
            if(noteTimer>=noteDur){ noteTimer-=noteDur; noteStep++; }
            if(phase>1e9f) phase-=1e9f;
        }
    }
    void OnBgmSetPosition(int position){ phase=0; noteStep=0; noteTimer=0; }
    float Sine(float f, float ph){ return (float)Math.Sin(ph); }
    float Ping2(float f, float ph){ return (float)Math.Sin(ph); }

    // 预生成命中毒/杀音效
    AudioClip MakeHit(string kind){
        float dur = kind=="kill"?0.19f:kind=="heavy"?0.12f:0.075f;
        float[] data = new float[(int)(dur*sampleRate)];
        float start = kind=="kill"?96f:kind=="heavy"?132f:185f;
        float end = kind=="kill"?38f:kind=="heavy"?62f:105f;
        float amp = kind=="kill"?0.15f:kind=="heavy"?0.12f:0.085f;
        float fphase=0;
        for(int i=0;i<data.Length;i++){ float t=i/(float)sampleRate; float f=Mathf.Lerp(start,end,t/dur); float env=Mathf.Exp(-6f*t/dur);
            fphase += 2f*(float)Math.PI*f/sampleRate;
            float sample=(float)Math.Sin(fphase)*amp*env;
            data[i]=sample;
        }
        AudioClip c=AudioClip.Create(kind+"hit",data.Length,1,sampleRate,false); c.SetData(data,0); return c;
    }
}
