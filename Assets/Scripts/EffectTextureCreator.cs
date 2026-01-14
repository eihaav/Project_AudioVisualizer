using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class EffectTextureCreator : MonoBehaviour
{
    public Camera EffectCamera;
    public RenderTexture EffectRenderTexture;//, EffectRenderTexture2;
    public Material FadeMaterial;
    public List<SpriteRenderer> _spriteRenderers = new List<SpriteRenderer>();
    public float FadeAmount = 0.005f;
    private bool blitOrder = true;
    [Header("Debug Settings")]
    public MeshRenderer TexPreviewMesh;
    public bool previewDestTexture = false;
    private bool _textureInitialized = false;

    private void Awake()
    {
        //FadeMaterial = new Material(Shader.Find("Hidden/FadeAlpha"));
    }
    private void Start()
    {
        EffectCamera.aspect = 1f;
        if (EffectRenderTexture == null)
        {
            EffectRenderTexture = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGBHalf);
            //EffectRenderTexture.enableRandomWrite = true;
            EffectRenderTexture.Create();
            //EffectRenderTexture2 = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGBHalf);
            ////EffectRenderTexture2.enableRandomWrite = true;
            //EffectRenderTexture2.Create();
        }
        SetTargetPreviewTex(EffectRenderTexture);
        //ClearRT(EffectRenderTexture);
        //ClearRT(EffectRenderTexture2);
    }
    public void AddRipple(GameObject rippleObj)
    {
        SpriteRenderer sr = rippleObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            _spriteRenderers.Add(sr);
        }
    }
    public void RemoveRipple(GameObject rippleObj)
    {
        SpriteRenderer sr = rippleObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            _spriteRenderers.Remove(sr);
        }
    }
    public void SetMaterialRT(Material mat, string propertyName)
    {
        if (!_textureInitialized)
            mat.SetTexture(propertyName, EffectRenderTexture);
    }
    void ClearRT(RenderTexture rt)
    {
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;
    }
    private void Update()
    {
        RenderToTexture();
    }
    private void SetTargetPreviewTex(RenderTexture rt)
    {
        Material previewMat = TexPreviewMesh.material;
        previewMat.SetTexture("_PreviewTex", rt);
    }
    private void RenderToTexture()
    {
        CommandBuffer cb = new CommandBuffer();
        RenderTexture source = EffectRenderTexture;
        //RenderTexture dest = EffectRenderTexture2;
        //cb.SetRenderTarget(dest);
        //cb.ClearRenderTarget(true, true, Color.clear);

        int tempRT = Shader.PropertyToID("_TempRT");
        //int tempRT2 = Shader.PropertyToID("_TempRT2");
        cb.GetTemporaryRT(tempRT, EffectRenderTexture.width, EffectRenderTexture.height, 0, EffectRenderTexture.filterMode, EffectRenderTexture.format);
        //cb.GetTemporaryRT(tempRT2, EffectRenderTexture.width, EffectRenderTexture.height, 0, FilterMode.Bilinear, EffectRenderTexture.format);
        FadeMaterial.SetFloat("_FadeAmount", FadeAmount);
        cb.Blit(source, tempRT, FadeMaterial);
        cb.Blit(tempRT, source);
        //cb.Blit(source, dest);
        //cb.Blit(dest, tempRT, FadeMaterial);
        //cb.Blit(tempRT, dest, FadeMaterial);
        FadeMaterial.SetFloat("_FadeAmount", FadeAmount);
        //cb.Blit(dest, source);
        //cb.Blit(EffectRenderTexture2, tempRT2, FadeMaterial);
        //cb.Blit(tempRT2, EffectRenderTexture2);

        cb.SetupCameraProperties(EffectCamera);
        cb.SetRenderTarget(source);
        //if (blitOrder)
        //{
        //    cb.SetRenderTarget(EffectRenderTexture);
        //}
        //else
        //{
        //    cb.SetRenderTarget(EffectRenderTexture2);
        //}
        //cb.SetRenderTarget(EffectRenderTexture);
        
        //cb.ClearRenderTarget(true, true, Color.clear);
        for (int i = 0; i < _spriteRenderers.Count; i++)
        {
            cb.DrawRenderer(_spriteRenderers[i], _spriteRenderers[i].material);
        }
        //cb.ReleaseTemporaryRT(tempRT);
        //if (blitOrder)
        //{
        //    cb.Blit(EffectRenderTexture, EffectRenderTexture2, FadeMaterial);
        //    blitOrder = false;
        //}
        //else
        //{
        //    cb.Blit(EffectRenderTexture2, EffectRenderTexture, FadeMaterial);
        //    blitOrder = true;
        //}
        cb.ReleaseTemporaryRT(tempRT);
        //cb.ReleaseTemporaryRT(tempRT2);
        Graphics.ExecuteCommandBuffer(cb);
        cb.Release();
        //if (previewDestTexture)
        //{
        //    SetTargetPreviewTex(dest);
        //}
        //else
        //{
        //    SetTargetPreviewTex(source);
        //}
    }
}
