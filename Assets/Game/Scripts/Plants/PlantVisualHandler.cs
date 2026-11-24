using Spine.Unity;
using System.Collections;
using UnityEngine;

public class PlantVisualHandler : MonoBehaviour
{
    private SkeletonAnimation skeletonAnim;
    private MeshRenderer meshRenderer;
    private Material glowMaterial;
    private GameObject shadow;

    private Coroutine flashCoroutine;
    private const float flashDuration = 0.09f;
    private const float flashIntensity = 1f;

    private static readonly int PropGlowIntensity = Shader.PropertyToID("_GlowIntensity");

    private void Awake()
    {
        skeletonAnim = GetComponent<SkeletonAnimation>();
        meshRenderer = skeletonAnim.GetComponent<MeshRenderer>();
        shadow = transform.Find("Shadow").gameObject;
        if (shadow == null)
        {
            Debug.LogWarning($"[PlantBase] Not found Shadow Object!");
        }
        shadow.GetComponent<SpriteRenderer>().sortingOrder = SortingOrderUtility.GetSortingOrder(LayerType.Shadow);
    }

    private void EnsureGlowMaterial()
    {
        if (glowMaterial != null) return;
        if (meshRenderer == null) return;

        var originalMaterial = meshRenderer.sharedMaterial;
        if (originalMaterial == null) return;

        glowMaterial = new Material(originalMaterial);
        skeletonAnim.CustomMaterialOverride[originalMaterial] = glowMaterial;
    }

    public void FlashHit()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
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

    public void SetGhostVisual()
    {
        shadow.SetActive(false);
        meshRenderer.sortingOrder = SortingOrderUtility.GetSortingOrder(LayerType.GhostPlant);
        SetAlpha(0.7f);
    }

    public void SetRealVisual(PlantType plantType, int row)
    {
        shadow.SetActive(true);
        var layerSorting = plantType.GetFieldType() == FieldType.Normal
                         ? LayerType.NormalPlant : LayerType.SupportPlant;
        meshRenderer.sortingOrder = SortingOrderUtility.GetSortingOrder(layerSorting, row);
        ApplyPlantTint(plantType);
        SetAlpha(1f);
    }

    public void SetDisplayVisual()
    {
        shadow.SetActive(false);
        meshRenderer.sortingOrder = SortingOrderUtility.GetSortingOrder(LayerType.DisplayPlant);
        SetAlpha(1f);
    }

    public void SetAlpha(float alpha)
    {
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
            _ => new Color(0.85f, 0.85f, 0.95f, 1f)
        };
        skeletonAnim.Skeleton.SetColor(tint);
    }

    public void ResetAll()
    {
        ResetGlow();
        if (skeletonAnim != null && skeletonAnim.Skeleton != null)
            skeletonAnim.Skeleton.SetColor(Color.white);
    }
}
