using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class HandManager : MonoBehaviour
{
    [SerializeField] private int maxHandSize = 10;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform spawnPoint;

    private readonly List<GameObject> handCards = new();

    public static HandManager handManager;

    private void Awake()
    {
        handManager = this;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rKey.wasPressedThisFrame)
        {
            DrawCard();
        }
    }

    private void DrawCard()
    {
        if (maxHandSize <= 0 || handCards.Count >= maxHandSize)
        {
            return;
        }

        GameObject card = Instantiate(
            cardPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        SpriteRenderer sr =
            card.GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            sr.sortingOrder = handCards.Count;
        }

        handCards.Add(card);
        UpdateCardPositions();
    }

    // null이면 전체 정렬, 카드가 전달되면 해당 카드만 정렬.
    public void UpdateCardPositions(hoveredCard onlyCard = null)
{
    if (handCards.Count == 0 || maxHandSize <= 0) return;

    float cardSpacing = 1f / maxHandSize;

    float firstCardPosition =
        0.5f - (handCards.Count - 1) * cardSpacing / 2f;

    for (int i = 0; i < handCards.Count; i++)
    {
        GameObject cardObject = handCards[i];
        if (cardObject == null) continue;

        hoveredCard card =
            cardObject.GetComponent<hoveredCard>();

        if (card == null) continue;
        if (onlyCard != null && card != onlyCard) continue;

        float p = firstCardPosition + i * cardSpacing;

        Vector3 position =
            splineContainer.EvaluatePosition(p);

        Vector3 forward =
            splineContainer.EvaluateTangent(p);

        Vector3 up =
            splineContainer.EvaluateUpVector(p);

        Quaternion rotation = Quaternion.LookRotation(
            up,
            Vector3.Cross(up, forward).normalized
        );

        // 여기! 표시 순서를 지정한 다음 위치와 회전을 전달.
        card.SetHandSortingOrder(i);
        card.SetHandPose(position, rotation);
    }
}

    private void OnDestroy()
    {
        if (handManager == this)
        {
            handManager = null;
        }
    }
}