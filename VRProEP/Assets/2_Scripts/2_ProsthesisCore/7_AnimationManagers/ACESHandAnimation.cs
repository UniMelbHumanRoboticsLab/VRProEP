using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ACESHandAnimation : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    private float pinchLevel;

    private const float DELTA_LEVEL = 0.01f;
    private const float MAX_LEVEL = 1.0f;
    private const float MIN_LEVEL = 0.0f;

    // Start is called before the first frame update
    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.RightArrow))
        {
            pinchLevel += DELTA_LEVEL;
            if (pinchLevel > MAX_LEVEL)
                pinchLevel = MAX_LEVEL;

            animator.SetFloat("InputAxis1", pinchLevel);
            
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            pinchLevel -= DELTA_LEVEL;
            if (pinchLevel < MIN_LEVEL)
                pinchLevel = MIN_LEVEL;

            animator.SetFloat("InputAxis1", pinchLevel);

        }


        
    }
}
