using UnityEngine;


public class DiscardGroupLayout : MonoBehaviour
{
    [Header("横方向の基準間隔（小さめ推奨：重なり気味になる）")]
    public float spacingX = 40f;

    [Header("1枚ごとのズレ（±）")]
    public float jitterX = 18f;
    public float jitterY = 12f;

    [Header("1枚ごとの回転（±度）")]
    public float jitterRotZ = 8f;

    [Header("中央揃え（束っぽく）")]
    public bool centerAlign = true;

    [Header("毎回同じ散らばりにしたいなら固定シードを使う")]
    public bool useFixedSeed = false;
    public int seed = 0;

    public void Rebuild()
    {
        int n = transform.childCount;
        if (n == 0) return;

        if (useFixedSeed) Random.InitState(seed);

        float startX = centerAlign ? -spacingX * (n - 1) * 0.5f : 0f;

        for (int i = 0; i < n; i++)
        {
            var rt = transform.GetChild(i) as RectTransform;
            if (rt == null) continue;

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            float x = startX + spacingX * i + Random.Range(-jitterX, jitterX);
            float y = Random.Range(-jitterY, jitterY);
            float rz = Random.Range(-jitterRotZ, jitterRotZ);

            rt.anchoredPosition = new Vector2(x, y);
            rt.localRotation = Quaternion.Euler(0, 0, rz);
            rt.localScale = Vector3.one;

            rt.SetSiblingIndex(i);
        }
    }
}
