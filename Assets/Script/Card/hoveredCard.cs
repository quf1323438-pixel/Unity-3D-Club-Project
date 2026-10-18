using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(BoxCollider))]
public class hoveredCard : MonoBehaviour
{
    [SerializeField] private Transform visual;

    [SerializeField] private float scaleMultiplier = 1.2f;
    [SerializeField] private float cardsizeMultiplier = 1.2f;

    [SerializeField] private float duration = 0.15f;
    [SerializeField] private float returnDuration = 0.25f;

    [SerializeField] private Vector3 hoverOffset =
        new Vector3(0f, 0.5f, 0f);

    private BoxCollider col;
    private SpriteRenderer sr;

    // 자식 Visual의 기본 상태.
    private Vector3 cardpos;
    private Vector3 cardsize;
    private Quaternion visualRotation;

    // Collider의 기본 상태.
    private Vector3 collidersize;
    private Vector3 colliderCenter;

    // HandManager가 계산한 월드 위치와 회전.
    private Vector3 handPosition;
    private Quaternion handRotation;

    private int originalSortingOrder;

    private bool initialized;
    private bool isHovered;
    private bool isDragging;

    public bool IsReturning { get; private set; }

    private Tween moveTween;
    private Tween scaleTween;
    private Tween rotationTween;
    private Tween visualRotationTween;
    private Sequence returnTween;

    private void Awake()
    {
        col = GetComponent<BoxCollider>();

        if (visual == null || visual.parent != transform)
        {
            Debug.LogError(
                "Visual에 카드 본체의 직속 자식 Transform을 연결해주세요.",
                this
            );

            enabled = false;
            return;
        }

        sr = visual.GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogError(
                "Visual에 SpriteRenderer가 필요합니다.",
                this
            );

            enabled = false;
            return;
        }

        cardpos = visual.localPosition;
        cardsize = visual.localScale;
        visualRotation = visual.localRotation;

        collidersize = col.size;
        colliderCenter = col.center;

        handPosition = transform.position;
        handRotation = transform.rotation;

        originalSortingOrder = sr.sortingOrder;
        initialized = true;
    }

    // HandManager가 계산한 최신 손패 위치를 전달받는다.
    public void SetHandPose(Vector3 position, Quaternion rotation)
    {
        if (!initialized) return;

        handPosition = position;
        handRotation = rotation;

        // 호버나 드래그 중에는 목적지만 갱신한다.
        if (isHovered || isDragging) return;

        if (!isActiveAndEnabled)
        {
            ResetImmediately();
            return;
        }

        // 기본 상태에서 변경된 표시 순서를 기억한다.
        if (!IsReturning)
        {
            originalSortingOrder = sr.sortingOrder;
        }

        ReturnToHand(returnDuration);
    }

    public void SetHandSortingOrder(int order)
    {
        if (!initialized) return;

        originalSortingOrder = order;

        // 강조 효과가 없는 상태라면 즉시 적용.
        if (!isHovered && !isDragging && !IsReturning)
        {
            sr.sortingOrder = order;
        }
    }

    public void hovered_Enter()
    {
        if (!initialized || !isActiveAndEnabled ||
            isHovered || IsReturning)
        {
            return;
        }

        originalSortingOrder = sr.sortingOrder;
        isHovered = true;

        KillTweens();

        moveTween = visual
            .DOLocalMove(cardpos + hoverOffset, duration)
            .SetEase(Ease.OutCubic);

        scaleTween = visual
            .DOScale(cardsize * scaleMultiplier, duration)
            .SetEase(Ease.OutCubic);

        var cam = Camera.main;

        if (cam != null)
        {
            // 본체를 카메라 화면과 평행하게 정렬.
            rotationTween = transform
                .DORotateQuaternion(cam.transform.rotation, duration)
                .SetEase(Ease.OutCubic);

            // 자식 Visual에 적용된 추가 기울기도 제거.
            visualRotationTween = visual
                .DOLocalRotateQuaternion(Quaternion.identity, duration)
                .SetEase(Ease.OutCubic);
        }

        col.size = collidersize * cardsizeMultiplier;
        col.center = colliderCenter + hoverOffset / 2f;

        sr.sortingOrder = 1000;
    }

    public void hovered_Exit()
    {
        if (!initialized || !isHovered || isDragging) return;

        isHovered = false;
        ReturnToHand(duration);
    }

    public void BeginDrag()
    {
        if (!initialized || !isActiveAndEnabled ||
            isDragging || IsReturning)
        {
            return;
        }

        hovered_Enter();
        isDragging = true;
    }

    public void EndDrag()
    {
        if (!initialized || !isDragging) return;

        isDragging = false;
        isHovered = false;

        // 현재 손패 개수와 스플라인 기준으로 자기 자리를 다시 계산.
        if (HandManager.handManager != null)
        {
            HandManager.handManager.UpdateCardPositions(this);
        }
        else
        {
            ReturnToHand(returnDuration);
        }
    }

    private void ReturnToHand(float time)
    {
        KillTweens();

        IsReturning = true;

        col.size = collidersize;
        col.center = colliderCenter;

        returnTween = DOTween.Sequence();

        returnTween.Join(
            transform.DOMove(handPosition, time)
                .SetEase(Ease.OutCubic)
        );

        returnTween.Join(
            transform.DORotateQuaternion(handRotation, time)
                .SetEase(Ease.OutCubic)
        );

        returnTween.Join(
            visual.DOLocalMove(cardpos, time)
                .SetEase(Ease.OutCubic)
        );

        returnTween.Join(
            visual.DOScale(cardsize, time)
                .SetEase(Ease.OutCubic)
        );

        returnTween.Join(
            visual.DOLocalRotateQuaternion(visualRotation, time)
                .SetEase(Ease.OutCubic)
        );

        returnTween.OnComplete(() =>
        {
            IsReturning = false;
            returnTween = null;

            sr.sortingOrder = originalSortingOrder;
        });
    }

    private void KillTweens()
    {
        moveTween?.Kill();
        scaleTween?.Kill();
        rotationTween?.Kill();
        visualRotationTween?.Kill();
        returnTween?.Kill();

        moveTween = null;
        scaleTween = null;
        rotationTween = null;
        visualRotationTween = null;
        returnTween = null;
    }

    private void ResetImmediately()
    {
        KillTweens();

        isHovered = false;
        isDragging = false;
        IsReturning = false;

        transform.position = handPosition;
        transform.rotation = handRotation;

        if (visual != null)
        {
            visual.localPosition = cardpos;
            visual.localScale = cardsize;
            visual.localRotation = visualRotation;
        }

        if (col != null)
        {
            col.size = collidersize;
            col.center = colliderCenter;
        }

        if (sr != null)
        {
            sr.sortingOrder = originalSortingOrder;
        }
    }

    private void OnDisable()
    {
        if (!initialized) return;

        ResetImmediately();
    }
}