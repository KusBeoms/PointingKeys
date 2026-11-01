namespace PointingKeys;

// Shared "boost" flag: Q/S/W held (in any combination) means fast; none held means base speed.
// Read by both MouseMover and Scroller so both speed up together.
internal sealed class SpeedState
{
    private readonly HashSet<int> _boostKeysDown = new();

    public bool Boosted => _boostKeysDown.Count > 0;

    public void SetBoostKey(int vkCode, bool isDown)
    {
        if (isDown)
            _boostKeysDown.Add(vkCode);
        else
            _boostKeysDown.Remove(vkCode);
    }
}
