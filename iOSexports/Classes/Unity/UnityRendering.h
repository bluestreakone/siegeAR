#pragma once

#include <stdint.h>

#ifdef __OBJC__
@class CAMetalLayer;
@protocol CAMetalDrawable;
@protocol MTLDrawable;
@protocol MTLDevice;
@protocol MTLTexture;
@protocol MTLCommandBuffer;
@protocol MTLCommandQueue;
@protocol MTLCommandEncoder;

typedef id<CAMetalDrawable>     CAMetalDrawableRef;
typedef id<MTLDevice>           MTLDeviceRef;
typedef id<MTLTexture>          MTLTextureRef;
typedef id<MTLCommandBuffer>    MTLCommandBufferRef;
typedef id<MTLCommandQueue>     MTLCommandQueueRef;
typedef id<MTLCommandEncoder>   MTLCommandEncoderRef;
#else
typedef struct objc_object      CAMetalLayer;
typedef struct objc_object*     CAMetalDrawableRef;
typedef struct objc_object*     MTLDeviceRef;
typedef struct objc_object*     MTLTextureRef;
typedef struct objc_object*     MTLCommandBufferRef;
typedef struct objc_object*     MTLCommandQueueRef;
typedef struct objc_object*     MTLCommandEncoderRef;
#endif

// if this is set, then variables that there were moved from UnityDisplaySurfaceMTL to swapchain
//   will be still updated when acquiring drawable.
// NOTE: in this case we assume that all the UnityViewSwapchain pointers passed to unity players are coming from UnityDisplaySurfaceMTL
// if this is set UnityDisplaySurfaceBase still has removed variables, but they are no longer updated
#if !defined(UNITY_DISPLAY_SURFACE_MTL_BACKWARD_COMPATIBILITY) && UNITY_TRAMPOLINE_IN_USE
    #define UNITY_DISPLAY_SURFACE_MTL_BACKWARD_COMPATIBILITY 1
#endif

// unity internal native render buffer struct (the one you acquire in C# with RenderBuffer.GetNativeRenderBufferPtr())
struct RenderSurfaceBase;
typedef struct RenderSurfaceBase* UnityRenderBufferHandle;

// be aware that this struct is shared with unity implementation so you should absolutely not change it
typedef struct UnityRenderBufferDesc
{
    unsigned    width, height, depth;
    unsigned    samples;

    int         backbuffer;
} UnityRenderBufferDesc;

// trick to make structure inheritance work transparently between c/cpp
// for c we use "anonymous struct"
#ifdef __cplusplus
    #define START_STRUCT(T, Base)   struct T : Base {
    #define END_STRUCT(T)           };
#else
    #define START_STRUCT(T, Base)   typedef struct T { struct Base;
    #define END_STRUCT(T)           } T;
#endif

// we will keep objc objects in struct, so we need to explicitely mark references as strong to not confuse ARC
// please note that actual object lifetime is managed in objc++ code, so __unsafe_unretained is good enough for objc code
// DO NOT assign objects to UnityDisplaySurface* members in objc code.
// DO NOT store objects from UnityDisplaySurface* members in objc code, as this wont be caught by ARC
#ifdef __OBJC__
    #ifdef __cplusplus
        #define OBJC_OBJECT_PTR __strong
    #else
        #define OBJC_OBJECT_PTR __unsafe_unretained
    #endif
#else
    #define OBJC_OBJECT_PTR
#endif

// unity common rendering (display) surface
typedef struct UnityDisplaySurfaceBase
{
    UnityRenderBufferHandle unityColorBuffer;
    UnityRenderBufferHandle unityDepthBuffer;

    UnityRenderBufferHandle systemColorBuffer;

    unsigned            targetW, targetH;
    unsigned            systemW, systemH;

    int                 msaaSamples;
    int                 srgb;                   // [bool]
    int                 wideColor;              // [bool]
    int                 hdr;                    // [bool]
    int                 disableDepthAndStencil; // [bool]
    int                 allowScreenshot;        // [bool] currently we allow screenshots (from script) only on main display
    int                 memorylessDepth;        // [bool]

    int                 api;                    // [UnityRenderingAPI]

    // these are no longer supported, we keep them only to avoid breaking compilation
#if UNITY_DISPLAY_SURFACE_MTL_BACKWARD_COMPATIBILITY
    UnityRenderBufferHandle systemDepthBuffer   __attribute__((deprecated));
    int                 useCVTextureCache       __attribute__((deprecated));
    void*               cvTextureCache          __attribute__((deprecated));
    void*               cvTextureCacheTexture   __attribute__((deprecated));
    void*               cvPixelBuffer           __attribute__((deprecated));
#endif

} UnityDisplaySurfaceBase;

typedef struct UnityViewSwapchain
{
    OBJC_OBJECT_PTR CAMetalLayer*       layer;

    // drawable handling:
    // CADisplayLink: we will call [CAMetalLayer nextDrawable] first time we need to use "backbuffer"
    // CAMetalDisplayLink: we are getting drawable to render to from the callback
    //   alas it might happen that we get next callback while still using current drawable (as in - while rendering previous frame)
    //   thus we will "mimic" the logic for the old display link, by setting "next drawable to use" in callback
    //   and "acquiring" drawable on first draw to the backbuffer just updates current drawable to the stored one
    OBJC_OBJECT_PTR CAMetalDrawableRef  drawable;
    OBJC_OBJECT_PTR CAMetalDrawableRef  nextDrawable;
    OBJC_OBJECT_PTR MTLTextureRef       drawableTexture;
} UnityViewSwapchain;

// START_STRUCT confuse clang c compiler (though it is idiomatic c code that works)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wmissing-declarations"

// on iOS/tvOS: we render to the drawable directly
//              and we need proxy only to delay acquiring drawable until we actually want to render to the "backbuffer"
//              thus just one proxy and it will be marked as "empty" (we only need it to query texture params, like extents)
// on macOS:    we render to the offscreen RT and blit to the drawable, thus we need several proxy RT
//              and all of them will be full blown textures (with GPU backing)
#if PLATFORM_OSX
    #define kUnityNumOffscreenSurfaces 2
#else
    #define kUnityNumOffscreenSurfaces 1
#endif

// Metal display surface
START_STRUCT(UnityDisplaySurfaceMTL, UnityDisplaySurfaceBase)
UnityViewSwapchain                  swapchain;
OBJC_OBJECT_PTR MTLDeviceRef        device;

UnityRenderBufferHandle             targetColorRB;
UnityRenderBufferHandle             targetAAColorRB;

OBJC_OBJECT_PTR MTLTextureRef       drawableProxyRT[kUnityNumOffscreenSurfaces];
UnityRenderBufferHandle             drawableProxyRS[kUnityNumOffscreenSurfaces];

// This is used on a Mac with drawableProxyRT when off-screen rendering is used
int                                 proxySwaps;         // Counts times proxy RTs have swapped since surface recreated
int                                 proxyReady;         // [bool] Proxy RT has swapped since last present; frame ended
int                                 calledPresentDrawable; // Tracks presenting for editor.
int                                 vsync;              // Is vsync enabled or not

unsigned                            colorFormat;        // [MTLPixelFormat]
unsigned                            depthFormat;        // [MTLPixelFormat]
int                                 framebufferOnly;

// these were moved to a separate structure. to simplify the lives of plugin writers we are keeping them here for some time
// if these need to be updated XXX need to be defined: we will try to update these, but please move on from using them
#if UNITY_DISPLAY_SURFACE_MTL_BACKWARD_COMPATIBILITY
OBJC_OBJECT_PTR CAMetalLayer*       layer           __attribute__((deprecated));
OBJC_OBJECT_PTR CAMetalDrawableRef  nextDrawable    __attribute__((deprecated));
OBJC_OBJECT_PTR CAMetalDrawableRef  drawable        __attribute__((deprecated));
OBJC_OBJECT_PTR MTLTextureRef       drawableTex     __attribute__((deprecated));

// these are no longer used, and should have never been used before - we still keep them around but they stay zero-inited
OBJC_OBJECT_PTR MTLTextureRef       systemColorRB __attribute__((deprecated));
int                                 drawableProxyNeedsClear[kUnityNumOffscreenSurfaces] __attribute__((deprecated));

// these we removed in favor of RTs managed inside player library, we now have renderbuffers instead
OBJC_OBJECT_PTR MTLTextureRef       targetColorRT   __attribute__((deprecated));
OBJC_OBJECT_PTR MTLTextureRef       targetAAColorRT __attribute__((deprecated));

OBJC_OBJECT_PTR MTLTextureRef       depthRB         __attribute__((deprecated));
OBJC_OBJECT_PTR MTLTextureRef       stencilRB       __attribute__((deprecated));
#endif

END_STRUCT(UnityDisplaySurfaceMTL)

// START_STRUCT confuse clang c compiler (though it is idiomatic c code that works)
#pragma clang diagnostic pop

// be aware that this enum is shared with unity implementation so you should absolutely not change it
typedef enum UnityRenderingAPI
{
    apiMetal        = 4,

    // command line argument: -nographics
    // does not initialize real graphics device and bypass all the rendering
    // currently supported only on simulators
    apiNoGraphics   = -1,
} UnityRenderingAPI;

typedef struct RenderingSurfaceParams
{
    // rendering setup
    int msaaSampleCount;
    int renderW;
    int renderH;
    int srgb;
    int wideColor;
    int hdr;
    int metalFramebufferOnly;
    int metalMemorylessDepth;

    // unity setup
    int disableDepthAndStencil;

    // no longer supported
#if UNITY_DISPLAY_SURFACE_MTL_BACKWARD_COMPATIBILITY
    int useCVTextureCache   __attribute__((deprecated));
#endif
} RenderingSurfaceParams;

#ifdef __cplusplus
extern "C" {
#endif
int UnitySelectedRenderingAPI(void);
#ifdef __cplusplus
} // extern "C"
#endif

// metal
#ifdef __cplusplus
extern "C" {
#endif

void InitRenderingMTL(void);

void CreateSystemRenderingSurfaceMTL(UnityDisplaySurfaceMTL* surface);
void CreateUnityRenderBuffersMTL(UnityDisplaySurfaceMTL* surface);
void DestroyUnityRenderBuffersMTL(UnityDisplaySurfaceMTL* surface);
void StartFrameRenderingMTL(UnityDisplaySurfaceMTL* surface);
void EndFrameRenderingMTL(UnityDisplaySurfaceMTL* surface);
void PreparePresentMTL(UnityDisplaySurfaceMTL* surface, MTLCommandBufferRef cb);
void PresentMTL(UnityDisplaySurfaceMTL* surface, MTLCommandBufferRef cb);

// Acquires CAMetalDrawable resource for the surface and returns the drawable texture
// AcquireDrawableMTL is deprecated and not used from unity side anymore
//   we keep it here only to avoid breaking native plugins that might have used it
MTLTextureRef AcquireDrawableMTL(UnityDisplaySurfaceMTL* surface);
MTLTextureRef AcquireSwapchainDrawable(UnityViewSwapchain* swapchain);

unsigned UnityHDRSurfaceDepth(void);

// starting with ios11 apple insists on having just one presentDrawable per command buffer
// hence we keep normal processing for main screen, but when airplay is used we will create extra command buffers
void PreparePresentNonMainScreenMTL(UnityDisplaySurfaceMTL* surface);

#ifdef __cplusplus
} // extern "C"
#endif

// no graphics
#ifdef __cplusplus
extern "C" {
#endif

void InitRenderingNULL(void);
void CreateSystemRenderingSurfaceNULL(UnityDisplaySurfaceBase* surface);
void CreateUnityRenderBuffersNULL(UnityDisplaySurfaceBase* surface);
void DestroyUnityRenderBuffersNULL(UnityDisplaySurfaceBase* surface);
void StartFrameRenderingNULL(UnityDisplaySurfaceBase* surface);
void EndFrameRenderingNULL(UnityDisplaySurfaceBase* surface);
void PreparePresentNULL(UnityDisplaySurfaceBase* surface);
void PresentNULL(UnityDisplaySurfaceBase* surface);

#ifdef __cplusplus
} // extern "C"
#endif


#ifdef __cplusplus
extern "C" {
#endif

// for Create* functions if surf is null we will actuially create new one, otherwise we update the one provided
// metal: resolveTex should be non-nil only if tex have AA
UnityRenderBufferHandle UnityCreateExternalSurfaceMTL(UnityRenderBufferHandle surf, int isColor, MTLTextureRef tex, const UnityRenderBufferDesc* desc);
// Passing non-nil displaySurface will mark render surface as proxy and will do a delayed drawable acquisition when setting up framebuffer
UnityRenderBufferHandle UnityCreateExternalColorSurfaceMTL(UnityRenderBufferHandle surf, MTLTextureRef tex, MTLTextureRef resolveTex, const UnityRenderBufferDesc* desc, UnityDisplaySurfaceMTL* displaySurface);
UnityRenderBufferHandle UnityCreateExternalDepthSurfaceMTL(UnityRenderBufferHandle surf, MTLTextureRef tex, MTLTextureRef stencilTex, const UnityRenderBufferDesc* desc);
// creates "dummy" surface - will indicate "missing" buffer (e.g. depth-only RT will have color as dummy)
UnityRenderBufferHandle UnityCreateDummySurface(UnityRenderBufferHandle surf, int isColor, const UnityRenderBufferDesc* desc);

// external render surfaces and textures are "out of scope" for memory profiler, hence we add means to register them separately
// the separate mechanism is needed because unity cannot know what manages the lifetime of textures in this case
//   specifically since we allow external render surfaces and textures to share metal textures
void UnityRegisterExternalRenderSurfaceTextureForMemoryProfiler(MTLTextureRef tex);
void UnityRegisterExternalTextureForMemoryProfiler(MTLTextureRef tex);
void UnityUnregisterMetalTextureForMemoryProfiler(MTLTextureRef tex);

// handling of unity "backbuffer"
// internally we still pretend that backbuffer has depth, can have MSAA, or extents different from the window/view size
// we had UnityCreateExternal* api before that was slightly too "wordy" to use, and was also using UnityDisplaySurfaceMTL
//   which is wrong, and introduced to big a coupling
// now, we have two things happening (with different velocities), triggering the need to update API
//   even if this is not (might be not) the final form
// first of all we are working on introducing swift trampoline, where we can simplify rendering logic;
//   but also we do not want to drag in the whole UnityDisplaySurfaceMTL (it doesn't make sense nowadays)
// thus we introduce intermediate explicit "swapchain" structure
// this also plays nicely with moving towards having explicit "swapchain" concept internally,
//   where (not matching view) resolution, MSAA, etc will be handled separately from "here is the connection to window to give to compositor"
// hence we introduce the api using new UnityViewSwapchain structure (with minimal connection to view internals)
// alas we are not yet ready to fully switch to have only swapchain in the platform layer (trampoline)
//   thus we still keep the possibility to create "AA backbuffer that will be resolved to swapchain" or even custom resolution (copied to actual drawable)
// another thing to note is that we are still bound to support "old" CADisplayLink, thus we need to be able to delay acquiring drawable
//   so we still need to make sure we have RenderBuffer connecting to a "swapchain" to do the magic when we want to render to it
// and that's why we have such a big api surface instead of simple "here is your swapchain, please render to it"
//   but this will hopefully change soon
// things of note:
//   * note that we always pass "current" pointer (rbBackbuffer param)
//     this api is working similarly to realloc: if you pass null we allocate and create new render buffer, otherwise we update the existing one
//   * due to this, UnitySwapchainDestroyBackbuffer is called only if we really do not need this type of surface anymore

UnityRenderBufferHandle UnitySwapchainCreateBackbuffer(UnityViewSwapchain* swapchain, UnityRenderBufferHandle rbBackbuffer);
UnityRenderBufferHandle UnitySwapchainCreateBackbufferForExtents(UnityViewSwapchain* swapchain, UnityRenderBufferHandle rbBackbuffer, unsigned width, unsigned height);
UnityRenderBufferHandle UnitySwapchainCreateAABackbufferResolveToSwapchain(UnityViewSwapchain* swapchain, UnityRenderBufferHandle rbBackbuffer, unsigned sampleCount);
UnityRenderBufferHandle UnitySwapchainCreateAABackbuffer(UnityViewSwapchain* swapchain, UnityRenderBufferHandle rbBackbuffer, unsigned sampleCount, UnityRenderBufferHandle rbResolveTo);
UnityRenderBufferHandle UnitySwapchainCreateDepthForBackbuffer(UnityViewSwapchain* swapchain, UnityRenderBufferHandle rbColorBackbuffer, UnityRenderBufferHandle rbDepthBackbuffer);
void                    UnitySwapchainDestroyBackbuffer(UnityViewSwapchain* swapchain, UnityRenderBufferHandle rbBackbuffer);
void                    UnitySwapchainBlitBackbuffer(UnityViewSwapchain* swapchain, UnityRenderBufferHandle rbColorBackbuffer, MTLCommandBufferRef cb);

// disable rendering to render buffers (all Cameras that were rendering to one of buffers would be reset to use backbuffer)
void    UnityDisableRenderBuffers(UnityRenderBufferHandle color, UnityRenderBufferHandle depth);
// destroys render buffer
void    UnityDestroyExternalSurface(UnityRenderBufferHandle surf);
// sets current render target
void    UnitySetRenderTarget(UnityRenderBufferHandle color, UnityRenderBufferHandle depth);
// final blit to backbuffer
void    UnityBlitToBackbuffer(UnityRenderBufferHandle srcColor, UnityRenderBufferHandle dstColor, UnityRenderBufferHandle dstDepth);
// get native renderbuffer from handle

// sets vSync on OSX 10.13 and up
#if PLATFORM_OSX
void MetalUpdateDisplaySync(void);
#endif

UnityRenderBufferHandle UnityNativeRenderBufferFromHandle(void *rb);

MTLCommandBufferRef UnityCurrentMTLCommandBuffer(void);

#ifdef __cplusplus
} // extern "C"
#endif

// metal/gles unification

#define GLES_METAL_COMMON_IMPL_SURF(f)                                                                  \
inline void f(UnityDisplaySurfaceBase* surface)                                                         \
{                                                                                                       \
    switch(surface->api) {                                                                              \
        case apiMetal:                          f ## MTL((UnityDisplaySurfaceMTL*)surface);     break;  \
        case apiNoGraphics:                     f ## NULL(surface);                             break;  \
    }                                                                                                   \
}                                                                                                       \

#define GLES_METAL_COMMON_IMPL(f)                                       \
inline void f()                                                         \
{                                                                       \
    switch(UnitySelectedRenderingAPI()) {                               \
        case apiMetal:                          f ## MTL();     break;  \
        case apiNoGraphics:                     f ## NULL();    break;  \
    }                                                                   \
}                                                                       \


GLES_METAL_COMMON_IMPL(InitRendering);

GLES_METAL_COMMON_IMPL_SURF(CreateSystemRenderingSurface);
GLES_METAL_COMMON_IMPL_SURF(CreateUnityRenderBuffers);
GLES_METAL_COMMON_IMPL_SURF(DestroyUnityRenderBuffers);
GLES_METAL_COMMON_IMPL_SURF(StartFrameRendering);
GLES_METAL_COMMON_IMPL_SURF(EndFrameRendering);

#undef GLES_METAL_COMMON_IMPL_SURF
#undef GLES_METAL_COMMON_IMPL
