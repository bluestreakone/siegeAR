//==============================================================================
//
//  ReplayKit Unity Interface


#import "UnityReplayKit.h"

extern "C"
{
#if UNITY_REPLAY_KIT_AVAILABLE

    UNITY_EXPORT int UnityReplayKitAPIAvailable()
    {
        return [UnityReplayKit sharedInstance].apiAvailable ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitRecordingAvailable()
    {
        return [UnityReplayKit sharedInstance].recordingPreviewAvailable ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitIsCameraEnabled()
    {
        return [UnityReplayKit sharedInstance].cameraEnabled != NO ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitSetCameraEnabled(bool yes)
    {
        BOOL value = yes ? YES : NO;
        [UnityReplayKit sharedInstance].cameraEnabled = value;
        return [UnityReplayKit sharedInstance].cameraEnabled == value;
    }

    UNITY_EXPORT int UnityReplayKitIsMicrophoneEnabled()
    {
        return [UnityReplayKit sharedInstance].microphoneEnabled != NO ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitSetMicrophoneEnabled(bool yes)
    {
        if ([UnityReplayKit sharedInstance].isRecording)
        {
            printf_console("It is not possible to change microphoneEnabled during recording.\n");
            return 0;
        }

        BOOL value = yes ? YES : NO;
        [UnityReplayKit sharedInstance].microphoneEnabled = value;
        return [UnityReplayKit sharedInstance].microphoneEnabled == value;
    }

    UNITY_EXPORT const char* UnityReplayKitLastError()
    {
        NSString* err = [UnityReplayKit sharedInstance].lastError;
        if (err == nil)
        {
            return NULL;
        }
        const char* error = [err cStringUsingEncoding: NSUTF8StringEncoding];
        if (error != NULL)
        {
            error = strdup(error);
        }
        return error;
    }

    UNITY_EXPORT int UnityReplayKitStartRecording()
    {
        return [[UnityReplayKit sharedInstance] startRecording] ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitIsRecording()
    {
        return [UnityReplayKit sharedInstance].isRecording ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitShowCameraPreviewAt(float x, float y, float width, float height)
    {
#if !PLATFORM_VISIONOS
        float q = 1.0f / UnityScreenScaleFactor([UIScreen mainScreen]);
#else
        float q = 1.0f;
#endif
        float h = [[UIScreen mainScreen] bounds].size.height;
        return [[UnityReplayKit sharedInstance] showCameraPreviewAt: CGPointMake(x * q, h - y * q) width: width height: height] ? 1 : 0;
    }

    UNITY_EXPORT void UnityReplayKitHideCameraPreview()
    {
        [[UnityReplayKit sharedInstance] hideCameraPreview];
    }

    UNITY_EXPORT int UnityReplayKitStopRecording()
    {
#if !PLATFORM_TVOS
        UnityReplayKitHideCameraPreview();
        UnityReplayKitSetCameraEnabled(false);
#endif
        return [[UnityReplayKit sharedInstance] stopRecording] ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitDiscard()
    {
        return [[UnityReplayKit sharedInstance] discardPreview] ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitPreview()
    {
        return [[UnityReplayKit sharedInstance] showPreview] ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitBroadcastingAPIAvailable()
    {
        return [[UnityReplayKit sharedInstance] broadcastingApiAvailable] ? 1 : 0;
    }

    UNITY_EXPORT void UnityReplayKitStartBroadcasting(void* callback)
    {
        [[UnityReplayKit sharedInstance] startBroadcastingWithCallback: callback];
    }

    UNITY_EXPORT void UnityReplayKitStopBroadcasting()
    {
#if !PLATFORM_TVOS
        UnityReplayKitHideCameraPreview();
#endif
        [[UnityReplayKit sharedInstance] stopBroadcasting];
    }

    UNITY_EXPORT void UnityReplayKitPauseBroadcasting()
    {
        [[UnityReplayKit sharedInstance] pauseBroadcasting];
    }

    UNITY_EXPORT void UnityReplayKitResumeBroadcasting()
    {
        [[UnityReplayKit sharedInstance] resumeBroadcasting];
    }

    UNITY_EXPORT int UnityReplayKitIsBroadcasting()
    {
        return [[UnityReplayKit sharedInstance] isBroadcasting] ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitIsBroadcastingPaused()
    {
        return [[UnityReplayKit sharedInstance] isBroadcastingPaused] ? 1 : 0;
    }

    UNITY_EXPORT int UnityReplayKitIsPreviewControllerActive()
    {
        return [[UnityReplayKit sharedInstance] isPreviewControllerActive] ? 1 : 0;
    }

    UNITY_EXPORT const char* UnityReplayKitGetBroadcastURL()
    {
        NSURL *url = [[UnityReplayKit sharedInstance] broadcastURL];
        if (url != nil)
        {
            return [[url absoluteString] UTF8String];
        }
        return nullptr;
    }

    UNITY_EXPORT void UnityReplayKitCreateOverlayWindow()
    {
        [[UnityReplayKit sharedInstance] createOverlayWindow];
    }

#if !PLATFORM_VISIONOS
    extern "C" float UnityScreenScaleFactor(UIScreen* screen);
#endif

#else

// Impl when ReplayKit is not available.

    UNITY_EXPORT int UnityReplayKitAPIAvailable()        { return 0; }
    UNITY_EXPORT int UnityReplayKitRecordingAvailable()  { return 0; }
    UNITY_EXPORT const char* UnityReplayKitLastError()   { return NULL; }
    UNITY_EXPORT int UnityReplayKitStartRecording(int enableMicrophone, int enableCamera) { return 0; }
    UNITY_EXPORT int UnityReplayKitIsRecording()         { return 0; }
    UNITY_EXPORT int UnityReplayKitStopRecording()       { return 0; }
    UNITY_EXPORT int UnityReplayKitDiscard()             { return 0; }
    UNITY_EXPORT int UnityReplayKitPreview()             { return 0; }

    UNITY_EXPORT int UnityReplayKitIsCameraEnabled() { return 0; }
    UNITY_EXPORT int UnityReplayKitSetCameraEnabled(bool) { return 0; }
    UNITY_EXPORT int UnityReplayKitIsMicrophoneEnabled() { return 0; }
    UNITY_EXPORT int UnityReplayKitSetMicrophoneEnabled(bool) { return 0; }
    UNITY_EXPORT int UnityReplayKitShowCameraPreviewAt(float x, float y, float width, float height) { return 0; }
    UNITY_EXPORT void UnityReplayKitHideCameraPreview() {}
    UNITY_EXPORT void UnityReplayKitCreateOverlayWindow() {}

    void UnityReplayKitTriggerBroadcastStatusCallback(void*, bool, const char*);
    UNITY_EXPORT int UnityReplayKitBroadcastingAPIAvailable() { return 0; }
    UNITY_EXPORT void UnityReplayKitStartBroadcasting(void* callback) { UnityReplayKitTriggerBroadcastStatusCallback(callback, false, "ReplayKit not implemented."); }
    UNITY_EXPORT void UnityReplayKitStopBroadcasting() {}
    UNITY_EXPORT void UnityReplayKitPauseBroadcasting() {}
    UNITY_EXPORT void UnityReplayKitResumeBroadcasting() {}
    UNITY_EXPORT int UnityReplayKitIsBroadcasting() { return 0; }
    UNITY_EXPORT int UnityReplayKitIsBroadcastingPaused() { return 0; }
    UNITY_EXPORT int UnityReplayKitIsPreviewControllerActive() { return 0; }
    UNITY_EXPORT const char* UnityReplayKitGetBroadcastURL() { return nullptr; }

#endif  // UNITY_REPLAY_KIT_AVAILABLE
}  // extern "C"
