using System.Collections;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal sealed class SurfaceGroundingAgent : MonoBehaviour
{
    private const int MaximumAttempts = 20;
    private const float InitialDelaySeconds = 1f;
    private const float RetryDelaySeconds = 0.5f;
    private const float ProbeHeight = 60f;
    private const float ProbeDistance = 200f;
    private const float SurfaceOffset = 0.15f;

    private Vector3 _originalPosition;
    private bool _configured;

    public void Configure(Vector3 originalPosition)
    {
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

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            var origin = _originalPosition + (Vector3.up * ProbeHeight);
            if (Physics.Raycast(origin, Vector3.down, out var hit, ProbeDistance, Voxeland.GetTerrainLayerMask(), QueryTriggerInteraction.Ignore))
            {
                transform.position = new Vector3(_originalPosition.x, hit.point.y + SurfaceOffset, _originalPosition.z);
                Destroy(this);
                yield break;
            }

            if (attempt < MaximumAttempts)
                yield return new WaitForSeconds(RetryDelaySeconds);
        }

        Plugin.Log.LogWarning($"Surface grounding failed for Prism Kelp at {_originalPosition}; no local terrain or generated seamount was found.");
        Destroy(this);
    }
}
