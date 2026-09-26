using System.Collections;
using UnityEngine;

namespace AbyssalEcologies.Plugin;

internal sealed class GeneratedLandformLifetime : MonoBehaviour
{
    private GameObject? _owner;
    private bool _configured;

    public void Configure(GameObject owner)
    {
        _owner = owner;
        _configured = true;
    }

    private IEnumerator Start()
    {
        while (!_configured)
            yield return null;

        var interval = new WaitForSeconds(1f);
        while (_owner != null)
            yield return interval;

        Destroy(gameObject);
    }
}
