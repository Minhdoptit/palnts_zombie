using DG.Tweening;
using Spine.Unity;
using System.Collections;
using UnityEngine;

public class PlantVisualHandler : MonoBehaviour
{
    private SkeletonAnimation skeletonAnim;
    private MeshRenderer meshRenderer;
    private SpriteRenderer customSpriteRenderer;
    private Material glowMaterial;
    private GameObject shadow;
    private Vector3 baseScale;
    private Tween fusionTween;
    private Tween idleBobTween;
    private Tween recoilTween;
    private bool usingCustomSprite;

    private Coroutine flashCoroutine;
    private const float flashDuration = 0.09f;
    private const float flashIntensity = 1f;

    private static readonly int PropGlowIntensity = Shader.PropertyToID("_GlowIntensity");

    private void Awake()
    {
        baseScale = transform.localScale;
        skeletonAnim = GetComponent<SkeletonAnimation>();
        meshRenderer = skeletonAnim != null ? skeletonAnim.GetComponent<MeshRenderer>() : null;
        var shadowTf = transform.Find("Shadow");
        if (shadowTf != null)
        {
            shadow = shadowTf.gameObject;
            shadow.GetComponent<SpriteRenderer>().sortingOrder = SortingOrderUtility.GetSortingOrder(LayerType.Shadow);
        }
        else
        {
            Debug.LogWarning($"[PlantBase] Not found Shadow Object!");
        }
    }

    private void EnsureCustomSpriteRenderer()
    {
        if (customSpriteRenderer != null) return;

        var child = new GameObject("CustomPlantSprite");
        child.transform.SetParent(transform, false);
        child.transform.localPosition = new Vector3(0f, -0.35f, 0f);
        child.transform.localScale = Vector3.one;
        customSpriteRenderer = child.AddComponent<SpriteRenderer>();
        customSpriteRenderer.enabled = false;
    }

    private bool ApplyCustomSpriteIfAvailable(PlantType? plantType, int sortingOrder)
    {
        Sprite customSprite = plantType.HasValue ? RuntimeVisualLibrary.GetPlantSprite(plantType.Value) : null;
        if (customSprite != null)
        {
            EnsureCustomSpriteRenderer();
            usingCustomSprite = true;
            if (meshRenderer != null) meshRenderer.enabled = false;
            customSpriteRenderer.sprite = customSprite;
            customSpriteRenderer.color = Color.white;
            customSpriteRenderer.sortingOrder = sortingOrder;
            customSpriteRenderer.enabled = true;
            return true;
        }

        usingCustomSprite = false;
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
            meshRenderer.sortingOrder = sortingOrder;
        }
        if (customSpriteRenderer != null)
            customSpriteRenderer.enabled = false;
        return false;
    }

    private void EnsureGlowMaterial()
    {
        if (glowMaterial != null) return;
        if (meshRenderer == null || skeletonAnim == null) return;

        var originalMaterial = meshRenderer.sharedMaterial;
        if (originalMaterial == null) return;

        glowMaterial = new Material(originalMaterial);
        skeletonAnim.CustomMaterialOverride[originalMaterial] = glowMaterial;
    }

    public void FlashHit()
    {
        if (!gameObject.activeInHierarchy) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    public void PlayFusionFeedback()
    {
        FlashHit();
        fusionTween?.Kill();
        idleBobTween?.Kill();
        transform.localScale = baseScale * 0.7f;
        fusionTween = transform.DOScale(baseScale, 0.28f)
            .SetEase(Ease.OutBack)
            .OnComplete(StartIdleBobIfCustom);
    }

    public void PlayAttackRecoil()
    {
        if (!usingCustomSprite || customSpriteRenderer == null) return;
        recoilTween?.Kill();
        customSpriteRenderer.transform.localScale = new Vector3(1.16f, 0.88f, 1f);
        recoilTween = customSpriteRenderer.transform.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutBack);
    }

    private void StartIdleBobIfCustom()
    {
        idleBobTween?.Kill();
        if (!usingCustomSprite || customSpriteRenderer == null) return;
        customSpriteRenderer.transform.localScale = Vector3.one;
        idleBobTween = customSpriteRenderer.transform
            .DOScale(new Vector3(1.04f, 0.96f, 1f), 0.55f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private IEnumerator FlashRoutine()
    {
        if (usingCustomSprite && customSpriteRenderer != null)
        {
            var prev = customSpriteRenderer.color;
            customSpriteRenderer.color = new Color(1f, 0.95f, 0.55f, prev.a);
            yield return new WaitForSeconds(flashDuration);
            if (customSpriteRenderer != null)
                customSpriteRenderer.color = new Color(1f, 1f, 1f, prev.a);
            flashCoroutine = null;
            yield break;
        }

        EnsureGlowMaterial();
        if (glowMaterial == null) yield break;

        glowMaterial.SetFloat(PropGlowIntensity, flashIntensity);
        yield return new WaitForSeconds(flashDuration);
        ResetGlow();
        flashCoroutine = null;
    }

    private void ResetGlow()
    {
        EnsureGlowMaterial();
        if (glowMaterial == null) return;

        glowMaterial.SetFloat(PropGlowIntensity, 0f);
    }

    public void SetGhostVisual(PlantType? plantType = null)
    {
        if (shadow != null) shadow.SetActive(false);
        int order = SortingOrderUtility.GetSortingOrder(LayerType.GhostPlant);
        idleBobTween?.Kill();
        ApplyCustomSpriteIfAvailable(plantType, order);
        SetAlpha(0.7f);
    }

    public void SetRealVisual(PlantType plantType, int row)
    {
        if (shadow != null) shadow.SetActive(true);
        var layerSorting = plantType.GetFieldType() == FieldType.Normal
                         ? LayerType.NormalPlant : LayerType.SupportPlant;
        int order = SortingOrderUtility.GetSortingOrder(layerSorting, row);
        if (!ApplyCustomSpriteIfAvailable(plantType, order))
            ApplyPlantTint(plantType);
        else
            StartIdleBobIfCustom();
        SetAlpha(1f);
    }

    public void SetDisplayVisual(PlantType? plantType = null)
    {
        if (shadow != null) shadow.SetActive(false);
        int order = SortingOrderUtility.GetSortingOrder(LayerType.DisplayPlant);
        idleBobTween?.Kill();
        if (!ApplyCustomSpriteIfAvailable(plantType, order) && plantType.HasValue)
            ApplyPlantTint(plantType.Value);
        SetAlpha(1f);
    }

    public void SetAlpha(float alpha)
    {
        if (usingCustomSprite && customSpriteRenderer != null)
        {
            var c = customSpriteRenderer.color;
            c.a = alpha;
            customSpriteRenderer.color = c;
            return;
        }

        if (skeletonAnim == null || skeletonAnim.Skeleton == null) return;
        var color = skeletonAnim.Skeleton.GetColor();
        color.a = alpha;
        skeletonAnim.Skeleton.SetColor(color);
    }

    private void ApplyPlantTint(PlantType plantType)
    {
        if (skeletonAnim == null || skeletonAnim.Skeleton == null) return;
        Color tint = plantType switch
        {
            PlantType.Peashooter => Color.white,
            PlantType.Sunflower => new Color(1f, 0.92f, 0.25f, 1f),
            PlantType.Twinflower => new Color(1f, 0.72f, 0.15f, 1f),
            PlantType.Wallnut => new Color(0.75f, 0.52f, 0.28f, 1f),
            PlantType.Tallnut => new Color(0.55f, 0.36f, 0.18f, 1f),
            PlantType.Exonut => new Color(0.9f, 0.35f, 0.25f, 1f),
            PlantType.Repeater => new Color(0.45f, 0.95f, 0.45f, 1f),
            PlantType.Splitpea => new Color(0.3f, 0.85f, 0.65f, 1f),
            PlantType.Snowpea => new Color(0.4f, 0.85f, 1f, 1f),
            PlantType.Iceberg => new Color(0.6f, 0.95f, 1f, 1f),
            PlantType.IceStorm => new Color(0.3f, 0.65f, 1f, 1f),
            PlantType.Fireshooter => new Color(1f, 0.45f, 0.2f, 1f),
            PlantType.Jalapeno => new Color(1f, 0.25f, 0.2f, 1f),
            PlantType.CherryBomb => new Color(0.95f, 0.2f, 0.3f, 1f),
            PlantType.GooPeashooter => new Color(0.75f, 0.35f, 0.95f, 1f),
            PlantType.BooShroom => new Color(0.65f, 0.4f, 0.85f, 1f),
            PlantType.CabbagePult => new Color(0.6f, 1f, 0.45f, 1f),
            PlantType.KernelPult => new Color(1f, 0.88f, 0.45f, 1f),
            PlantType.MelonPult => new Color(0.45f, 0.85f, 0.35f, 1f),
            PlantType.WinterMelon => new Color(0.45f, 0.9f, 1f, 1f),
            PlantType.PepperPult => new Color(1f, 0.4f, 0.25f, 1f),
            PlantType.Pumpkin => new Color(1f, 0.6f, 0.15f, 1f),
            PlantType.PeaVine => new Color(0.55f, 1f, 0.7f, 1f),
            PlantType.CherryPeashooter => new Color(1f, 0.28f, 0.32f, 1f),
            PlantType.CherryRepeater => new Color(0.92f, 0.12f, 0.22f, 1f),
            _ => new Color(0.85f, 0.85f, 0.95f, 1f)
        };
        skeletonAnim.Skeleton.SetColor(tint);
    }

    public void ResetAll()
    {
        fusionTween?.Kill();
        idleBobTween?.Kill();
        recoilTween?.Kill();
        transform.localScale = baseScale;
        if (customSpriteRenderer != null)
        {
            customSpriteRenderer.transform.localScale = Vector3.one;
            customSpriteRenderer.color = Color.white;
            customSpriteRenderer.enabled = false;
        }
        usingCustomSprite = false;
        if (meshRenderer != null) meshRenderer.enabled = true;
        ResetGlow();
        if (skeletonAnim != null && skeletonAnim.Skeleton != null)
            skeletonAnim.Skeleton.SetColor(Color.white);
    }
}
