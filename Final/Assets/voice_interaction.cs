using Pico.Platform;
using Pico.Platform.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class voice_interaction : MonoBehaviour
{

    private bool isListening = false;
    

    
    // Start is called before the first frame update
    void Start()
    {
        SpeechService.InitAsrEngine();
        SpeechService.SetOnAsrResultCallback(OnAsrResult);
        SpeechService.SetOnSpeechErrorCallback(OnSpeechError);

        // 开始监听
        StartListening();
    }
    // 开始语音识别
    void StartListening()
    {   
        while(true)
        {
            if (!isListening)
            {
                SpeechService.StartAsr(true, true, 10000);
                isListening = true;
            }
        }
        
    }
    void OnAsrResult(Message<AsrResult> msg) 
    {
        if (msg.Data.IsFinalResult)
        {
            isListening = false;
        }
        
    }

    void OnSpeechError(Message<SpeechError> msg) 
    {
        Debug.LogError($"语音错误: 错误码 {msg.Data.Code} - 错误信息 {msg.Data.Message} - 会话ID {msg.Data.SessionId}");
    }
}
