using System;
using UnityEngine;

/// <summary>
/// Automatically drives an enemy Animator with clips ending in Idle, Walk and Attack.
/// State names are derived from clip names, so prefabs no longer store fragile strings.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemySpriteAnimator : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float walkSpeedThreshold = 0.01f;

    private Animator animator;
    private Rigidbody2D body;
    private MeleeAttack meleeAttack;
    private RangedAttack rangedAttack;
    private VisionComponent vision;

    private string idleState;
    private string walkState;
    private string attackState;
    private float attackDuration = 0.5f;
    private float attackUntil;
    private string currentState;
    private bool statesReady;

    /// <summary>Actual length of the resolved attack clip.</summary>
    public float AttackDuration => attackDuration;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        meleeAttack = GetComponent<MeleeAttack>();
        rangedAttack = GetComponent<RangedAttack>();
        vision = GetComponent<VisionComponent>();

        ResolveStates();
    }

    private void OnEnable()
    {
        if (meleeAttack != null) meleeAttack.OnAttackStarted += PlayAttack;
        if (rangedAttack != null) rangedAttack.OnAttackStarted += PlayAttack;
        if (vision != null)
        {
            vision.OnArenaWindupStarted += PlayAttack;
            vision.OnArenaWindupCancelled += ResumeMovement;
        }
    }

    private void OnDisable()
    {
        if (meleeAttack != null) meleeAttack.OnAttackStarted -= PlayAttack;
        if (rangedAttack != null) rangedAttack.OnAttackStarted -= PlayAttack;
        if (vision != null)
        {
            vision.OnArenaWindupStarted -= PlayAttack;
            vision.OnArenaWindupCancelled -= ResumeMovement;
        }
    }

    private void Update()
    {
        if (!statesReady || Time.time < attackUntil) return;

        bool isWalking = body != null
            && body.velocity.sqrMagnitude > walkSpeedThreshold * walkSpeedThreshold;
        PlayState(isWalking ? walkState : idleState);
    }

    private void ResolveStates()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"[EnemySpriteAnimator] {name} has no Runtime Animator Controller.", this);
            return;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        AnimationClip idleClip = FindClip(clips, "Idle");
        AnimationClip walkClip = FindClip(clips, "Walk");
        AnimationClip attackClip = FindClip(clips, "Attack");

        if (idleClip == null || walkClip == null || attackClip == null)
        {
            Debug.LogError(
                $"[EnemySpriteAnimator] {name} controller must contain clips ending in " +
                $"Idle, Walk and Attack.", this);
            return;
        }

        string layerName = animator.GetLayerName(0);
        idleState = $"{layerName}.{idleClip.name}";
        walkState = $"{layerName}.{walkClip.name}";
        attackState = $"{layerName}.{attackClip.name}";
        attackDuration = Mathf.Max(0.01f, attackClip.length);

        bool statesExist = animator.HasState(0, Animator.StringToHash(idleState))
            && animator.HasState(0, Animator.StringToHash(walkState))
            && animator.HasState(0, Animator.StringToHash(attackState));

        if (!statesExist)
        {
            Debug.LogError(
                $"[EnemySpriteAnimator] {name} Animator state names must match their clip names. " +
                $"Expected: {idleClip.name}, {walkClip.name}, {attackClip.name}.", this);
            return;
        }

        statesReady = true;
        PlayState(idleState, true);
    }

    private static AnimationClip FindClip(AnimationClip[] clips, string suffix)
    {
        if (clips == null) return null;

        foreach (AnimationClip clip in clips)
        {
            if (clip != null && clip.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return clip;
        }

        return null;
    }

    private void PlayAttack()
    {
        if (!statesReady) return;

        attackUntil = Time.time + attackDuration;
        PlayState(attackState, true);
    }

    private void ResumeMovement()
    {
        attackUntil = 0f;
        currentState = null;
    }

    private void PlayState(string stateName, bool restart = false)
    {
        if (!statesReady && !restart) return;
        if (animator == null || string.IsNullOrEmpty(stateName)) return;
        if (!restart && currentState == stateName) return;

        animator.Play(stateName, 0, 0f);
        currentState = stateName;
    }
}
