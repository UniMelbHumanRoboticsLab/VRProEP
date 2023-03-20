using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothespinManager : MonoBehaviour
{
    [SerializeField]
    private Animator animator;


    private float openLevel;

    [SerializeField]
    private float deltaLevel = 0.01f;
    [SerializeField]
    private float maxLevel = 1.0f;
    [SerializeField]
    private float minLevel = 0.0f;
    [SerializeField]
    private float initialLevel = 0.5f;


    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        openLevel = initialLevel;
        animator.SetFloat("InputAxis1", openLevel);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
