// InputManager.cs - Touch, swipe, and keyboard input handling for mobile and desktop.
using System;
using UnityEngine;

namespace JungleDash
{
    public class InputManager : MonoBehaviour
    {
        public Action<int, int> OnSwipe; // dx (-1 or 1), dy (-1 or 1)
        public Action OnTap;
        public Action OnPauseToggle;

        Vector2 pointerDownPos;
        bool isPointerActive;
        bool hasSwiped;
        const float MinSwipeDistance = 24f; // In screen pixels

        public void Tick()
        {
            ReadKeyboard();
            ReadTouchAndMouse();
        }

        void ReadKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                OnSwipe?.Invoke(-1, 0);

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                OnSwipe?.Invoke(1, 0);

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
                OnSwipe?.Invoke(0, 1);

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                OnSwipe?.Invoke(0, -1);

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
                OnPauseToggle?.Invoke();
        }

        void ReadTouchAndMouse()
        {
            // Mobile Touch
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began)
                {
                    PointerDown(t.position);
                }
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                {
                    PointerMove(t.position);
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    PointerUp();
                }
                return;
            }

            // Desktop Mouse Click/Drag
            if (Input.GetMouseButtonDown(0))
            {
                PointerDown(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                PointerMove(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                PointerUp();
            }
        }

        void PointerDown(Vector2 pos)
        {
            isPointerActive = true;
            hasSwiped = false;
            pointerDownPos = pos;
        }

        void PointerMove(Vector2 pos)
        {
            if (!isPointerActive) return;

            Vector2 delta = pos - pointerDownPos;
            if (delta.sqrMagnitude >= MinSwipeDistance * MinSwipeDistance)
            {
                hasSwiped = true;
                pointerDownPos = pos;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    OnSwipe?.Invoke(delta.x > 0 ? 1 : -1, 0);
                }
                else
                {
                    OnSwipe?.Invoke(0, delta.y > 0 ? 1 : -1);
                }
            }
        }

        void PointerUp()
        {
            if (isPointerActive && !hasSwiped)
            {
                bool isPauseArea = pointerDownPos.x > Screen.width - 80f && pointerDownPos.y > Screen.height - 75f;
                if (!isPauseArea)
                {
                    OnTap?.Invoke();
                }
            }
            isPointerActive = false;
            hasSwiped = false;
        }
    }
}
