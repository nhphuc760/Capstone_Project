
using UnityEngine;
using DG.Tweening;

public class ElectricMesh : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private float startScale = 1.3f;
    [SerializeField] private float endScale = 0f;
    [SerializeField] private float scaleDuration = 1.2f;

    [Header("Movement")]
    [SerializeField] private float moveHeight = 1.5f;
    [SerializeField] private float moveDuration = 1.2f;

    [Header("Animation")]
    [SerializeField] private Ease scaleEase = Ease.InBack;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    private Sequence sequence;

    private void OnEnable()
    {
        Play();
        transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
    }

    private void Play()
    {
        transform.localScale = Vector3.zero;

        Vector3 startPosition = transform.position;

        sequence = DOTween.Sequence();

        // Phóng to trước
        sequence.Append(
            transform.DOScale(startScale, 0.2f)
                .SetEase(Ease.OutBack)
        );

        // Sau đó thu nhỏ và di chuyển lên trên
        sequence.Append(
            transform.DOScale(endScale, scaleDuration)
                .SetEase(scaleEase)
        );

        sequence.Join(
            transform.DOMoveY(
                startPosition.y + moveHeight,
                moveDuration
            ).SetEase(moveEase)
        );

        sequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void OnDisable()
    {
        sequence?.Kill();
    }
}
