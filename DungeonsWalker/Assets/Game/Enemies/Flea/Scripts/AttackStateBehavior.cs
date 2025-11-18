using UnityEngine;

public class AttackStateBehaviour : StateMachineBehaviour
{
    // Cette méthode est appelée au moment où l'animation de cet état COMMENCE.
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // On cherche les scripts sur l'objet et on les désactive.
        // Cela empêche le script de suivi de se battre avec l'animation d'attaque.
        FleaFollow followScript = animator.GetComponent<FleaFollow>();
        if (followScript != null)
        {
            followScript.enabled = false;
        }

        FleaChargeAttack attackScript = animator.GetComponent<FleaChargeAttack>();
        if (attackScript != null)
        {
            attackScript.isAttacking = true; // On informe le script principal que l'attaque est en cours.
        }
    }

    // Cette méthode est appelée au moment où l'animation de cet état SE TERMINE.
    // C'EST LA PARTIE LA PLUS IMPORTANTE. ELLE EST GARANTIE DE S'EXÉCUTER.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // On cherche les scripts et on réactive tout.
        FleaFollow followScript = animator.GetComponent<FleaFollow>();
        if (followScript != null)
        {
            followScript.enabled = true;
        }

        FleaChargeAttack attackScript = animator.GetComponent<FleaChargeAttack>();
        if (attackScript != null)
        {
            attackScript.isAttacking = false;
        }
    }
}

