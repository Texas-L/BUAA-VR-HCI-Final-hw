using UnityEngine;
using UnityEngine.UI;
using Pico.Platform;
using Pico.Platform.Models;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.Windows;
using static UnityEngine.InputSystem.InputControlScheme.MatchResult;



#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
using System.Collections;

public class VoiceCommandHandler : MonoBehaviour
{
    [Header("绑定对象")]
    public Text[] shoppingItems = new Text[8];
    public Text debugText;      // 调试文本（可选）

    [Header("ASR配置")]
    [Tooltip("自动停止ASR服务（设为false可强制持续监听）")]
    public bool autoStop = true;          // 控制是否自动停止ASR
    public int maxDuration = 60000;       // 最大监听时长（毫秒）

    private bool isAsrConfigured = false; // ASR引擎是否初始化成功
    private bool permissionRequested;     // 是否已请求过麦克风权限

    private static readonly Regex ChineseNumberPattern =
        new Regex(@"([一二三四五六七八九十]+)([个只件条瓶盒袋桶包]+)");
    private static readonly Regex ChineseNumber =
        new Regex(@"([一二三四五六七八九十]+)");

    void Start()
    {
        // 初始权限检查
        RequestMicrophonePermission();
        UpdateDebugText("初始权限检查 ");
    }

    // ================== 权限管理 ==================
    #region 权限逻辑
    private void RequestMicrophonePermission()
    {
#if PLATFORM_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            InitializeAndStartAsrSystem();
        }
        else
        {
            Permission.RequestUserPermission(Permission.Microphone);
            UpdateDebugText("权限弹窗！");
            StartCoroutine(CheckPermissionAfterRequest());
            permissionRequested = true;
        }
#else
        // 非Android平台直接初始化（测试用）
        InitializeAndStartAsrSystem();
#endif
    }

#if PLATFORM_ANDROID
    private IEnumerator CheckPermissionAfterRequest()
    {
        
        // 等待权限弹窗关闭
        yield return new WaitUntil(() => Application.isFocused);
        yield return new WaitForSeconds(0.1f);

        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            InitializeAndStartAsrSystem();
        }
        else
        {
            UpdateDebugText("麦克风权限被拒绝！");
        }
    }
#endif
    #endregion

    // ================== ASR核心逻辑 ==================
    #region ASR服务管理
    private void InitializeAndStartAsrSystem()
    {
        if (isAsrConfigured)
        {
            Debug.Log("ASR已初始化，跳过重复操作");
            return;
        }

        // 注册回调
        SpeechService.SetOnAsrResultCallback(HandleAsrResult);
        SpeechService.SetOnSpeechErrorCallback(HandleSpeechError);

        // 初始化引擎
        AsrEngineInitResult result = SpeechService.InitAsrEngine();
        if (result == AsrEngineInitResult.Success)
        {
            isAsrConfigured = true;
            UpdateDebugText("就绪：请说出指令");
            StartAsrService();
        }
        else
        {
            UpdateDebugText($"初始化失败: {result}");
        }
    }

    private void StartAsrService()
    {
        SpeechService.StartAsr(autoStop, showPunctual: true, maxDuration);
        Debug.Log("ASR服务已启动");
    }

    private void StopAsrService()
    {
        if (isAsrConfigured)
        {
            SpeechService.StopAsr();
            Debug.Log("ASR服务已停止");
        }
    }
    #endregion

    // ================== 语音回调处理 ==================
    #region 语音事件处理
    private void HandleAsrResult(Message<AsrResult> msg)
    {
        if (!isActiveAndEnabled) return;

        if (msg.IsError)
        {
            UpdateDebugText($"ASR错误: {msg.GetError().Message}");
            return;
        }

        AsrResult result = msg.Data;
        if (result.IsFinalResult && !string.IsNullOrEmpty(result.Text))
        {
            string command = result.Text.Trim().ToLower();
            UpdateDebugText($"识别到: {command}");
            ProcessVoiceCommand(command);

            // 关键修复：处理完成后重启ASR服务
            if (autoStop) StartAsrService();
        }
    }

    private void HandleSpeechError(Message<SpeechError> msg)
    {
        UpdateDebugText($"语音错误: {msg.Data.Message}");
        if (msg.Data.Code == -402) // 权限错误
        {
            isAsrConfigured = false;
            RequestMicrophonePermission();
        }
        // 超时或无语音错误（假设Code=-1001）
        else if (msg.Data.Code == 1014)
        {
            UpdateDebugText("未检测到语音，重新启动监听...");
            StopAsrService();
            StartAsrService();
        }
        // 其他未知错误
        else
        {
            UpdateDebugText("请继续，我在听。");
            StopAsrService();
            StartAsrService();
        }
    }
    #endregion

    // ================== 指令处理 ==================
    #region 交互逻辑
    private void ProcessVoiceCommand(string command)
    {
        UpdateDebugText(command);
        if (command.Contains("牛奶巧克力") || 
            command.Contains("咖啡") || 
            command.Contains("矿泉水") || 
            command.Contains("餐巾纸") || 
            command.Contains("鸡蛋") ||
            command.Contains("三明治") ||
            command.Contains("巧克力棒") ||
            command.Contains("麦片") ||
            command.Contains("泡面") 
            ) 
        {
            string commodity = "";
            if (command.Contains("牛奶巧克力")) commodity = "牛奶巧克力";
            else if (command.Contains("咖啡")) commodity = "咖啡";
            else if (command.Contains("矿泉水")) commodity = "矿泉水";
            else if (command.Contains("餐巾纸")) commodity = "餐巾纸";
            else if (command.Contains("鸡蛋")) commodity = "鸡蛋";
            else if (command.Contains("三明治")) commodity = "三明治";
            else if (command.Contains("巧克力棒")) commodity = "巧克力棒";
            else if (command.Contains("麦片")) commodity = "麦片";
            else if (command.Contains("泡面")) commodity = "泡面";

            MatchCollection matches = ChineseNumberPattern.Matches(command);
            if (matches.Count > 0) {
                
                for (int i = 0; i < 8; i++)
                {
                    if (shoppingItems[i].text == "") 
                    {
                        string chineseNumber = matches[0].Groups[1].Value; // 汉语数字部分
                        string unit = matches[0].Groups[2].Value;          // 量词部分
                        switch (chineseNumber)
                        {
                            case "一": chineseNumber = "1"; break;
                            case "二": chineseNumber = "2"; break;
                            case "三": chineseNumber = "3"; break;
                            case "四": chineseNumber = "4"; break;
                            case "五": chineseNumber = "5"; break;
                            case "六": chineseNumber = "6"; break;
                            case "七": chineseNumber = "7"; break;
                            case "八": chineseNumber = "8"; break;
                            case "九": chineseNumber = "9"; break;
                        }
                        shoppingItems[i].text = $"{commodity}  {chineseNumber}{unit}";
                        break;
                    }
                }
            }
        }
        else if (command.Contains("删除"))
        {
            MatchCollection matches = ChineseNumber.Matches(command);
            if (command.Contains("全部")) 
            {
                for (int i = 0; i < 8; i++)
                {
                    shoppingItems[i].text = "";
                }
            }
            else if(matches.Count > 0) 
            { 
                int index = 0;
                string chineseNumber = matches[0].Groups[1].Value;
                switch (chineseNumber)
                {
                    case "一": index = 0; break;
                    case "二": index = 1; break;
                    case "三": index = 2; break;
                    case "四": index = 3; break;
                    case "五": index = 4; break;
                    case "六": index = 5; break;
                    case "七": index = 6; break;
                    case "八": index = 7; break;
                }
                shoppingItems[index].text = "";
            }
        }
    }
    #endregion

    // ================== 生命周期管理 ==================
    #region 启用/禁用处理
    private void OnEnable()
    {
        if (isAsrConfigured)
        {
            // 重新注册回调并启动服务
            SpeechService.SetOnAsrResultCallback(HandleAsrResult);
            SpeechService.SetOnSpeechErrorCallback(HandleSpeechError);
            StartAsrService();
        }
    }

    private void OnDisable()
    {
        // 清理回调并停止服务
        SpeechService.SetOnAsrResultCallback(null);
        SpeechService.SetOnSpeechErrorCallback(null);
        StopAsrService();
    }

    private void OnDestroy()
    {
        StopAsrService();
    }
    #endregion

    // ================== 辅助方法 ==================
    private void UpdateDebugText(string message)
    {
        if (debugText != null) debugText.text = message;
        Debug.Log(message);
    }
}