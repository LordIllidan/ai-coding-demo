namespace PolicyPlatform.Domain.Claims;

/// <summary>Channel a claim was initiated through. MobileNative marks claims started
/// directly in the mobile app's own UI, without redirecting to a browser/webview.</summary>
public enum ClaimChannel
{
    MobileNative,
    Web,
}
