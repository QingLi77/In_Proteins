using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class VoiceMovement : MonoBehaviour
{
    public Transform[] targets;
    private int index;
    private Dictionary<string, Action> actions = new Dictionary<string, Action>();
    // 短语识别器
    private PhraseRecognizer m_PhraseRecognizer;
    // 可信度
    public ConfidenceLevel m_confidenceLevel = ConfidenceLevel.Medium;
    public Text text;
    public Scrollbar scrollbar;

    private void Start()
    {
        actions.Add("forward", ()=> {
            targets[index].Translate(0,0,1);
        });
        actions.Add("up", () => {
            targets[index].Translate(0, 1, 0);
        });
        actions.Add("down", () => {
            targets[index].Translate(0, -1, 0);
        });
        actions.Add("back", () => {
            targets[index].Translate(0, 0, -1);
        });
        actions.Add("rotation", ()=> {
            targets[index].Rotate(0,30,0);
        });
        actions.Add("zoom", ()=> {
            var value = UnityEngine.Random.Range(0.2f,1f);
            targets[index].localScale = new Vector3(value, value, value);
        });
        actions.Add("open", () => {
            targets[index].gameObject.SetActive(true);
        });
        actions.Add("next", () => {
            targets[index].gameObject.SetActive(false);
            index++;
            if (index >= targets.Length)
                index = 0;
            targets[index].gameObject.SetActive(true);
        });
        actions.Add("close", () => {
            targets[index].gameObject.SetActive(false);
        });
        //创建一个识别器
        //m_PhraseRecognizer = new KeywordRecognizer(actions.Keys.ToArray(), m_confidenceLevel);
        //通过注册监听的方法
        //m_PhraseRecognizer.OnPhraseRecognized += RecognizedSpeech;
        //开启识别器
        //m_PhraseRecognizer.Start();
        //Debug.Log("创建识别器成功");
    }

    public void RecognizedSpeech(string text)
    {
        Debug.Log(text);
        this.text.text += text + "\n";
        scrollbar.value = 0;
        if (actions.ContainsKey(text))
            actions[text].Invoke();
    }

    private void OnDestroy()
    {
        //判断场景中是否存在语音识别器，如果有，释放
        if (m_PhraseRecognizer != null)
        {
            //用完应该释放，否则会带来额外的开销
            m_PhraseRecognizer.Dispose();
        }
    }
}
