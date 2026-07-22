#include "UnityRendering.h"

#import <Metal/Metal.h>
#import <QuartzCore/QuartzCore.h>

#include "UnityAppController.h"
#include "CVTextureCache.h"

#include "ObjCRuntime.h"
#include <libkern/OSAtomic.h>
#include <utility>

extern "C" void InitRenderingMTL()
{
}

static MTLPixelFormat GetColorFormatForSurface(const UnityDisplaySurfaceMTL* surface)
{
    MTLPixelFormat colorFormat = MTLPixelFormatInvalid;

#if PLATFORM_IOS || PLATFORM_VISIONOS
    if (surface->hdr)
    {
        // 0 = 10 bit, 1 = 16bit
        if (@available(iOS 16.0, *))
            colorFormat = UnityHDRSurfaceDepth() == 0 ? MTLPixelFormatRGB10A2Unorm : MTLPixelFormatRGBA16Float;
    }
#endif

    if(colorFormat == MTLPixelFormatInvalid && surface->wideColor)
    {
        // at some point we tried using MTLPixelFormatBGR10_XR formats, but it seems that apple CoreImage have issues with that
        //   and we are not alone here, see for example https://forums.developer.apple.com/forums/thread/66166
        // when application goes to background the colors are changed (more white-ish?)
        // no matter what we tried, the issue persists
        // NOTE: the most funny thing is when we set color space to be P3 we get same whitish colors always
        // NOTE: but this time they become normal when going to background
        // in all, it seems that using rgba f16 is the most robust option here, so we are back to it again
        colorFormat = MTLPixelFormatRGBA16Float;
    }

    if(colorFormat == MTLPixelFormatInvalid)
        colorFormat = surface->srgb ? MTLPixelFormatBGRA8Unorm_sRGB : MTLPixelFormatBGRA8Unorm;

    return colorFormat;
}

extern "C" void CreateSystemRenderingSurfaceMTL(UnityDisplaySurfaceMTL* surface)
{
    MTLPixelFormat colorFormat = GetColorFormatForSurface(surface);
    surface->swapchain.layer.presentsWithTransaction = NO;
    surface->swapchain.layer.drawsAsynchronously = YES;

    if (UnityPreserveFramebufferAlpha())
    {
        const CGFloat components[] = {1.0f, 1.0f, 1.0f, 0.0f};
        CGColorSpaceRef colorSpace = CGColorSpaceCreateDeviceRGB();
        CGColorRef color = CGColorCreate(colorSpace, components);
        surface->swapchain.layer.opaque = NO;
        surface->swapchain.layer.backgroundColor = color;
        CGColorRelease(color);
        CGColorSpaceRelease(colorSpace);
    }

    CGColorSpaceRef colorSpaceRef = nil;
    if (surface->hdr)
    {
        if (@available(iOS 16.0, *))
            colorSpaceRef = UnityHDRSurfaceDepth() == 0 ? CGColorSpaceCreateWithName(CFSTR("kCGColorSpaceITUR_2100_PQ")) : CGColorSpaceCreateWithName(CFSTR("kCGColorSpaceExtendedLinearITUR_2020"));
    }
    if(colorSpaceRef == nil)
    {
        if (surface->wideColor)
            colorSpaceRef = CGColorSpaceCreateWithName(surface->srgb ? kCGColorSpaceExtendedLinearSRGB : kCGColorSpaceExtendedSRGB);
        else
            colorSpaceRef = CGColorSpaceCreateWithName(kCGColorSpaceSRGB);
    }

    surface->swapchain.layer.colorspace = colorSpaceRef;
    CGColorSpaceRelease(colorSpaceRef);

    surface->swapchain.layer.device = surface->device;
    surface->swapchain.layer.pixelFormat = colorFormat;
    surface->swapchain.layer.framebufferOnly = (surface->framebufferOnly != 0);
    surface->colorFormat = (unsigned)colorFormat;
}

extern "C" void CreateUnityRenderBuffersMTL(UnityDisplaySurfaceMTL* surface)
{
    const int w = surface->targetW, h = surface->targetH;
    const bool needInterimColor = w != surface->systemW || h != surface->systemH;

    if (needInterimColor)
    {
        surface->targetColorRB = UnitySwapchainCreateBackbufferForExtents(&surface->swapchain, surface->targetColorRB, w, h);
    }
    else
    {
        UnityDestroyExternalSurface(surface->targetColorRB);
        surface->targetColorRB = 0;
    }

    if (surface->msaaSamples > 1)
    {
        if (needInterimColor)
            surface->targetAAColorRB = UnitySwapchainCreateAABackbuffer(&surface->swapchain, surface->targetAAColorRB, surface->msaaSamples, surface->targetColorRB);
        else
            surface->targetAAColorRB = UnitySwapchainCreateAABackbufferResolveToSwapchain(&surface->swapchain, surface->targetAAColorRB, surface->msaaSamples);
    }
    else
    {
        UnityDestroyExternalSurface(surface->targetAAColorRB);
        surface->targetAAColorRB = 0;
    }

    surface->systemColorBuffer = UnitySwapchainCreateBackbuffer(&surface->swapchain, surface->systemColorBuffer);

    if (surface->targetAAColorRB)
        surface->unityColorBuffer = surface->targetAAColorRB;
    else if (surface->targetColorRB)
        surface->unityColorBuffer = surface->targetColorRB;
    else
        surface->unityColorBuffer = surface->systemColorBuffer;

    surface->unityDepthBuffer  = UnitySwapchainCreateDepthForBackbuffer(&surface->swapchain, surface->unityColorBuffer, surface->unityDepthBuffer);
}

extern "C" void DestroyUnityRenderBuffersMTL(UnityDisplaySurfaceMTL* surface)
{
    UnitySwapchainDestroyBackbuffer(&surface->swapchain, surface->systemColorBuffer);
    UnitySwapchainDestroyBackbuffer(&surface->swapchain, surface->unityDepthBuffer);
    UnitySwapchainDestroyBackbuffer(&surface->swapchain, surface->targetColorRB);
    UnitySwapchainDestroyBackbuffer(&surface->swapchain, surface->targetAAColorRB);
    surface->targetColorRB = surface->targetAAColorRB = surface->systemColorBuffer = surface->unityDepthBuffer = 0;

    surface->unityColorBuffer = 0;
}

extern "C" void PreparePresentMTL(UnityDisplaySurfaceMTL* surface, MTLCommandBufferRef cb)
{
    if (surface->targetColorRB)
        UnitySwapchainBlitBackbuffer(&surface->swapchain, surface->targetColorRB, cb);
    APP_CONTROLLER_RENDER_PLUGIN_METHOD(onFrameResolved);
}

extern "C" void PresentMTL(UnityDisplaySurfaceMTL* surface, MTLCommandBufferRef cb)
{
    UnityViewSwapchain* swapchain = &surface->swapchain;
    @autoreleasepool
    {
        if (swapchain->drawable)
            [cb presentDrawable:swapchain->drawable];

        if (swapchain->drawableTexture)
            UnityUnregisterMetalTextureForMemoryProfiler(swapchain->drawableTexture);

        swapchain->nextDrawable = nil;
        swapchain->drawable = nil;
        swapchain->drawableTexture = nil;
    }

    surface->calledPresentDrawable = 1;
}

UNITY_EXPORT extern "C" MTLTextureRef AcquireSwapchainDrawable(UnityViewSwapchain* swapchain)
{
    // check if have acquired the backbuffer texture already
    if (swapchain->drawableTexture)
        return swapchain->drawableTexture;

    // this is coming from CAMetalDisplayLinkUpdate
    if (swapchain->nextDrawable)
        swapchain->drawable = swapchain->nextDrawable;

    // this is coming from CADisplayLink: query next drawable
    if (!swapchain->drawable)
        swapchain->drawable = [swapchain->layer nextDrawable];

    id<MTLTexture> drawableTex = [swapchain->drawable texture];
    if (drawableTex)
    {
        UnityUnregisterMetalTextureForMemoryProfiler(swapchain->drawableTexture);
        swapchain->drawableTexture = drawableTex;
        UnityRegisterExternalRenderSurfaceTextureForMemoryProfiler(drawableTex);
    }

#if UNITY_DISPLAY_SURFACE_MTL_BACKWARD_COMPATIBILITY
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
#pragma clang diagnostic ignored "-Winvalid-offsetof"

    const uintptr_t surfacePtr = (uintptr_t)swapchain - offsetof(UnityDisplaySurfaceMTL, swapchain);
    UnityDisplaySurfaceMTL* surface = (UnityDisplaySurfaceMTL*)surfacePtr;
    surface->layer          = swapchain->layer;
    surface->nextDrawable   = swapchain->nextDrawable;
    surface->drawable       = swapchain->drawable;
    surface->drawableTex    = swapchain->drawableTexture;

#pragma clang diagnostic pop
#endif

    return drawableTex;

}

UNITY_EXPORT extern "C" MTLTextureRef AcquireDrawableMTL(UnityDisplaySurfaceMTL* surface)
{
    if (!surface)
        return nil;

    return AcquireSwapchainDrawable(&surface->swapchain);
}

UNITY_EXPORT extern "C" int UnityCommandQueueMaxCommandBufferCountMTL()
{
    // customizable argument to pass towards [MTLDevice newCommandQueueWithMaxCommandBufferCount:],
    // the default value is 64 but with Parallel Render Encoder workloads, it might need to be increased

    return 256;
}

UNITY_EXPORT extern "C" void StartFrameRenderingMTL(UnityDisplaySurfaceMTL* surface)
{
}

UNITY_EXPORT extern "C" void EndFrameRenderingMTL(UnityDisplaySurfaceMTL* surface)
{
}
