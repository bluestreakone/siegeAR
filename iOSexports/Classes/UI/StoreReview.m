#if PLATFORM_IOS || PLATFORM_VISIONOS

// This definition is here only for compiler to know about selector requestReview
@interface UnityStoreReviewController
+ requestReview;
@end

UNITY_EXPORT bool UnityRequestStoreReview()
{
    Class classSKStoreReviewController = NSClassFromString(@"SKStoreReviewController");
    if (!classSKStoreReviewController || ![classSKStoreReviewController respondsToSelector: @selector(requestReview)])
        return false;

    [classSKStoreReviewController performSelector: @selector(requestReview)];
    return true;
}

#endif // PLATFORM_IOS || PLATFORM_VISIONOS
