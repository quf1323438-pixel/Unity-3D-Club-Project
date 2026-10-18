using UnityEngine;
using UnityEngine.InputSystem;

public class CameraClick : MonoBehaviour
{
    [SerializeField] private Vector3 mousepos;
    [SerializeField] private Ray ray;
    [SerializeField] private LayerMask cardlayermask;

    [SerializeField] private bool mouseclick;
    [SerializeField] private bool cardonmouse;

    public RaycastHit hit;

    private hoveredCard hoveredCard;
    private hoveredCard ClickCard;

    private Plane dragPlane;
    private Vector3 dragOffset;

    private bool waitForMouseMove;
    private Vector2 releaseMousePosition;

    private void Update()
    {
        var mouse = Mouse.current;
        var cam = Camera.main;

        if (mouse == null || cam == null) return;

        Vector2 screenPosition = mouse.position.ReadValue();

        mousepos = screenPosition;
        ray = cam.ScreenPointToRay(mousepos);
        mouseclick = mouse.leftButton.isPressed;

        // マウス 아래의 카드 찾기.
        hoveredCard currentCard = null;

        if (Physics.Raycast(
            ray,
            out hit,
            Mathf.Infinity,
            cardlayermask))
        {
            currentCard =
                hit.collider.GetComponent<hoveredCard>();

            if (currentCard != null &&
                (!currentCard.isActiveAndEnabled ||
                 currentCard.IsReturning))
            {
                currentCard = null;
            }
        }

        cardonmouse = currentCard != null;

        // 집은 카드는 마우스 아래 대상이 바뀌어도 유지.
        if (ClickCard != null)
        {
            if (!ClickCard.isActiveAndEnabled)
            {
                ClickCard = null;
                hoveredCard = null;
                return;
            }

            if (dragPlane.Raycast(ray, out float distance))
            {
                ClickCard.transform.position =
                    ray.GetPoint(distance) + dragOffset;
            }

            if (!mouseclick)
            {
                ClickCard.EndDrag();

                ClickCard = null;
                hoveredCard = null;

                waitForMouseMove = true;
                releaseMousePosition = screenPosition;
            }

            return;
        }

        // 놓은 직후에는 기본 상태 유지.
        if (waitForMouseMove)
        {
            if (screenPosition == releaseMousePosition &&
                !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }

            waitForMouseMove = false;
        }

        // 호버 대상 전환.
        if (currentCard != hoveredCard)
        {
            if (hoveredCard != null)
            {
                hoveredCard.hovered_Exit();
            }

            hoveredCard = currentCard;

            if (hoveredCard != null)
            {
                hoveredCard.hovered_Enter();
            }
        }

        // 누른 순간 카드 집기.
        if (mouse.leftButton.wasPressedThisFrame &&
            currentCard != null)
        {
            Plane plane = new Plane(
                cam.transform.forward,
                currentCard.transform.position
            );

            if (plane.Raycast(ray, out float distance))
            {
                ClickCard = currentCard;
                dragPlane = plane;

                dragOffset =
                    ClickCard.transform.position -
                    ray.GetPoint(distance);

                ClickCard.BeginDrag();
            }
        }
    }

    private void OnDisable()
    {
        if (ClickCard != null)
        {
            ClickCard.EndDrag();
        }
        else if (hoveredCard != null)
        {
            hoveredCard.hovered_Exit();
        }

        ClickCard = null;
        hoveredCard = null;

        waitForMouseMove = false;
        mouseclick = false;
        cardonmouse = false;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        bool hasHit = Physics.Raycast(
            ray,
            out RaycastHit gizmoHit,
            Mathf.Infinity,
            cardlayermask
        );

        Gizmos.color = hasHit ? Color.green : Color.red;

        Vector3 endPoint = hasHit
            ? gizmoHit.point
            : ray.GetPoint(100f);

        Gizmos.DrawLine(ray.origin, endPoint);

        if (hasHit)
        {
            Gizmos.DrawSphere(gizmoHit.point, 0.05f);
        }
    }
}