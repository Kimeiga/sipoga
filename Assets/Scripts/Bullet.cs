using UnityEngine;
using DG.Tweening;

public class Bullet : MonoBehaviour
{
    public GameObject bulletActual;
    public Renderer ren;
    private Material mat;
    public Color toColor = Color.black;
    public Color fromColor = Color.red;
    public float tweenTime = 1;
    public float scaleMod = 0.5f;

    // Use this for initialization
    void Start()
    {
        mat = ren.material;

        mat.SetColor("_Color", fromColor);

        DOTween.To(() => mat.color, x =>
        {
            mat.color = x;
            mat.SetColor("_Color", x);
        }, toColor, tweenTime).SetEase(Ease.OutExpo);
        gameObject.transform.DOScale(gameObject.transform.localScale * scaleMod, tweenTime).SetEase(Ease.OutExpo);


        // LeanTween.value(gameObject, mat.color, toColor, 1).setEase(LeanTweenType.easeOutExpo)
        //     .setOnUpdate((Color val) => { mat.SetColor("_Color", val); });
        //
        // LeanTween.scale(gameObject, gameObject.transform.localScale * scaleMod, tweenTime)
        //     .setEase(LeanTweenType.easeOutExpo);
    }
}