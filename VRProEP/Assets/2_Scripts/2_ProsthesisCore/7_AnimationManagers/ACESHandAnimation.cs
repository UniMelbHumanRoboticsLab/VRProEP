using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ACESHandAnimation : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    private float pinchLevel;

    [SerializeField]
    private float deltaLevel = 0.01f;
    [SerializeField]
    private float maxLevel = 1.0f;
    [SerializeField]
    private float minLevel = 0.0f;

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
            pinchLevel += deltaLevel;
            if (pinchLevel > maxLevel)
                pinchLevel = maxLevel;

            animator.SetFloat("InputAxis1", pinchLevel);
            
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            pinchLevel -= deltaLevel;
            if (pinchLevel < minLevel)
                pinchLevel = minLevel;

            animator.SetFloat("InputAxis1", pinchLevel);

        }


        
    }
}
