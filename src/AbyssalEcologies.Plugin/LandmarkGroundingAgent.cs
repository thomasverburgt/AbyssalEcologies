using System.Collections;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal sealed class LandmarkGroundingAgent : MonoBehaviour
{
    private const int MaximumAttempts = 20;
    private const float InitialDelaySeconds = 1f;
    private const float RetryDelaySeconds = 0.5f;

    private string _contentId = string.Empty;
    private Vector3 _originalPosition;
    private bool _configured;

    public void Configure(string contentId, Vector3 originalPosition)
    {
        _contentId = contentId;
        _originalPosition = originalPosition;
        _configured = true;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(InitialDelaySeconds);
        if (!_configured)
        {
            Destroy(this);
            yield break;
        }

        var lastReason = "terrain collision data did not become available";
        var lastAdjustment = 0f;
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            if (ContentRegistrar.TryGroundLandmark(_contentId, gameObject, _originalPosition, out var adjustment, out var detail))
            {
                ContentRegistrar.RecordLandmarkGrounding(_contentId, true, adjustment, $"attempt {attempt}/{MaximumAttempts}; {detail}");
                Destroy(this);
                yield break;
            }

            lastAdjustment = adjustment;
            lastReason = detail;
            if (attempt < MaximumAttempts)
                yield return new WaitForSeconds(RetryDelaySeconds);
        }

        ContentRegistrar.RecordLandmarkGrounding(_contentId, false, lastAdjustment, $"after {MaximumAttempts} local retries: {lastReason}");
        Destroy(this);
    }
}
