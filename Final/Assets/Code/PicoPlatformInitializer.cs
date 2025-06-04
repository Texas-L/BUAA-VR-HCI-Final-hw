using UnityEngine;
using Pico.Platform; // 导入 PICO 平台服务的命名空间

public class PicoPlatformInitializer : MonoBehaviour
{
    void Awake()
    {
        // 在Awake或Start中调用，确保在其他PICO SDK API调用之前
        InitializePicoPlatform();
    }

    private void InitializePicoPlatform()
    {
       try
        {
            // 直接调用初始化方法，因为它不返回任何值
            CoreService.Initialize();
            Debug.Log("PICO Platform SDK 尝试初始化。");

            // 在这种情况下，你不能直接用CoreService.Initialize()的返回值来判断成功。
            // 你可能需要依赖：
            // 1. 检查后续API调用的成功与否 (例如 GetLoggedInUser 的错误)
            // 2. 如果SDK提供了初始化完成回调，则使用回调。
            // 3. 在某些简单的应用中，可能直接假设初始化成功，并在后续API报错时处理。

            // 示例：获取登录用户（作为判断平台是否可用的间接方式）
            // 这是你原代码中已经有的逻辑，可以作为判断平台是否就绪的一种方式。
            GetPicoLoggedInUser(); 
        }
        catch (UnityException e)
        {
            // 捕获在Unity环境中可能发生的异常，例如SDK文件缺失
            Debug.LogError($"初始化 PICO Platform SDK 时发生 Unity 异常: {e.Message}");
            // 如果捕获到异常，通常不应该继续执行依赖PICO SDK的代码
            // throw; // 如果希望在Editor中强制停止执行，可以取消注释
        }
        // 捕获其他可能的异常
        catch (System.Exception e)
        {
            Debug.LogError($"初始化 PICO Platform SDK 时发生通用异常: {e.Message}");
        }
    }

   private void GetPicoLoggedInUser()
    {
        UserService.GetLoggedInUser().OnComplete(userMessage =>
        {
            if (userMessage.IsError)
            {
                Debug.LogError($"获取登录用户失败: Code={userMessage.Error.Code}, Message={userMessage.Error.Message}");
                // 这里可以判断平台是否真的没初始化成功或者用户没有登录
                return;
            }
            Debug.Log($"PICO 登录用户: {userMessage.Data.DisplayName} (ID: {userMessage.Data.ID})");
        });
    }
}