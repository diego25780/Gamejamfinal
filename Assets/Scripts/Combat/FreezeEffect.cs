using System.Collections;
using UnityEngine;

/// <summary>
/// Efeito de congelamento / lentidão aplicado a inimigos comuns ou Boss.
/// Reduz a velocidade e tinge o sprite de ciano/azul-gelo.
/// </summary>
public class FreezeEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private EnemyBase enemyBase;
    private BossController bossController;

    private float originalSpeed = 0f;
    private bool isFrozen = false;
    private Coroutine freezeRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyBase = GetComponent<EnemyBase>();
        bossController = GetComponent<BossController>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public static void Apply(GameObject target, float duration = 2.5f, float slowFactor = 0.3f)
    {
        if (target == null) return;

        var effect = target.GetComponent<FreezeEffect>();
        if (effect == null)
        {
            effect = target.AddComponent<FreezeEffect>();
        }

        effect.StartFreeze(duration, slowFactor);
    }

    public void StartFreeze(float duration, float slowFactor)
    {
        if (freezeRoutine != null)
        {
            StopCoroutine(freezeRoutine);
        }
        freezeRoutine = StartCoroutine(FreezeRoutine(duration, slowFactor));
    }

    private IEnumerator FreezeRoutine(float duration, float slowFactor)
    {
        isFrozen = true;

        if (spriteRenderer != null)
        {
            // Cor de gelo / ciano
            spriteRenderer.color = new Color(0.4f, 0.85f, 1f, 1f);
        }

        // Aplica lentidão no EnemyBase
        if (enemyBase != null)
        {
            enemyBase.ApplySpeedMultiplier(slowFactor);
        }

        yield return new WaitForSeconds(duration);

        // Restaura
        if (enemyBase != null && slowFactor > 0f)
        {
            enemyBase.ApplySpeedMultiplier(1f / slowFactor);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        isFrozen = false;
        freezeRoutine = null;
    }
}
