using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothespinManager : MonoBehaviour
{
    public enum ClothespinState { Idle, Selected, Correct, Wrong}


    //
    // Color and display
    //
    [Header("Colour configuration")]
    [SerializeField]
    private Color idleColour;
    [SerializeField]
    private Color selectedColour;
    [SerializeField]
    private Color correctColour;
    [SerializeField]
    private Color wrongColour;

    private ClothespinState pinState = ClothespinState.Idle;
    private Renderer[] pinRenderer;


    //
    // Animation
    //
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

    //
    // State control
    //
    private bool indexFingerTouched = false;
    private bool thumbFingerTouched = false;


    // Start is called before the first frame update
    void Awake()
    {
        minLevel = initialLevel;

        animator = GetComponent<Animator>();
        openLevel = initialLevel;
        animator.SetFloat("InputAxis1", openLevel);

        pinState = ClothespinState.Selected;
        pinRenderer = GetComponentsInChildren<Renderer>();

        
    }

    // Collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if touched by subject
        //other.tag == "IndexFingerCollider" ||
        if (other.tag == "IndexFingerCollider")
        {
            indexFingerTouched = true;
            //Debug.Log("Index touched!");
        }

        if (other.tag == "ThumbFingerCollider")
        {
            thumbFingerTouched = true;
            //Debug.Log("Thumb touched!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if touched by subject
        //other.tag == "IndexFingerCollider" ||
        if (other.tag == "IndexFingerCollider")
        {
            indexFingerTouched = false;
            //Debug.Log("Index leave!");
        }

        if (other.tag == "ThumbFingerCollider")
        {
            thumbFingerTouched = false;
            //Debug.Log("Thumb leave!");
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        bool grasped = indexFingerTouched && thumbFingerTouched;
        switch (pinState)
        {
            case ClothespinState.Idle:
                if (grasped)
                {
                    pinState = ClothespinState.Wrong;
                    ChangeClothespinColor(wrongColour);
                }
                else
                {
                    pinState = ClothespinState.Idle;
                    ChangeClothespinColor(idleColour);
                    CloseClothespin();
                }
                
                break;
            case ClothespinState.Selected:
                if (grasped)
                {
                    pinState = ClothespinState.Correct;
                    ChangeClothespinColor(correctColour);
                }
                else
                {
                    pinState = ClothespinState.Selected;
                    ChangeClothespinColor(selectedColour);
                    CloseClothespin();
                }
                break;
            case ClothespinState.Correct:
                if (grasped)
                {
                    OpenClothespin();
                }
                else
                {
                    pinState = ClothespinState.Selected;
                    ChangeClothespinColor(selectedColour);
                    CloseClothespin();
                }
                break;
            case ClothespinState.Wrong:
                if (grasped)
                {
                    OpenClothespin();
                }
                else
                {
                    pinState = ClothespinState.Idle;
                    ChangeClothespinColor(idleColour);
                    CloseClothespin();
                }
                break;
        }
        
    }

    private void ChangeClothespinColor(Color color)
    {
        foreach (Renderer renderer in pinRenderer)
        {
            renderer.material.color = color;
        }
    }

    private void OpenClothespin()
    {
        openLevel += deltaLevel ;
        if (openLevel > maxLevel)
            openLevel = maxLevel;
        else if (openLevel < minLevel)
            openLevel = minLevel;
        animator.SetFloat("InputAxis1", openLevel);
    }

    private void CloseClothespin()
    {
        openLevel -= deltaLevel * 2;
        if (openLevel > maxLevel)
            openLevel = maxLevel;
        else if (openLevel < minLevel)
            openLevel = minLevel;
        animator.SetFloat("InputAxis1", openLevel);
    }
}
