using UnityEngine;

public class AnimatorScript : MonoBehaviour
{
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    void Update()
    {

    }
    public void PlayEatAnimation()
    {
        animator.SetTrigger("eat");
    }
    public void PlayDieAnimation()
    {
        animator.SetTrigger("die");
    }
}
