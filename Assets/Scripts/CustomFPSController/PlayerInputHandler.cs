using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Action<Vector2> OnMove;
    public Action<Vector2> OnView;
    public Action OnJump;
    public Action OnCrouch;
    public Action OnSprint;
    public Action OnSprintRelease;
    public Action OnSlide;
    public Action OnGroundPound;

    private DefaultInput input;

    private void Awake()
    {
        input = new DefaultInput();
    }

    private void OnEnable()
    {
        input.Enable();

        input.Character.Movement.performed += ctx => OnMove?.Invoke(ctx.ReadValue<Vector2>());
        input.Character.Movement.canceled += ctx => OnMove?.Invoke(Vector2.zero);

        input.Character.View.performed += ctx => OnView?.Invoke(ctx.ReadValue<Vector2>());
        input.Character.View.canceled += ctx => OnView?.Invoke(Vector2.zero);

        input.Character.Jump.performed += _ => OnJump?.Invoke();
        input.Character.Crouch.performed += _ => OnCrouch?.Invoke();
        input.Character.Sprint.performed += _ => OnSprint?.Invoke();
        input.Character.SprintReleased.performed += _ => OnSprintRelease?.Invoke();
        input.Character.Slide.performed += _ => OnSlide?.Invoke();
        input.Character.GroundPound.performed += _ => OnGroundPound?.Invoke();
    }

    private void OnDisable()
    {
        input.Disable();
    }
}
