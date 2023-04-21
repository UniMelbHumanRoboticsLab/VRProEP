using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// SteamVR
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ACESHandAnimation : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private int udpChannel = 0;

    [SerializeField]
    private bool pinchAction;
    public bool PinchAction { get => pinchAction; set { pinchAction = value; } }

    [SerializeField]
    private bool externalControl;

    private float pinchLevel;

    [SerializeField]
    private float deltaLevel = 0.01f;
    [SerializeField]
    private float maxLevel = 1.0f;
    [SerializeField]
    private float minLevel = 0.0f;

    protected SteamVR_Action_Boolean padAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("InterfaceEnableButton");
    protected SteamVR_Action_Boolean buttonAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("ObjectInteractButton");

    // Start is called before the first frame update
    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.RightArrow) || buttonAction.GetState(SteamVR_Input_Sources.Any) || 
            (UDPClothespinManager.ClothespinState)UDPInputSystem.GetInput(UDPInputSystem.InputType.UDPClothespinButton, udpChannel) == UDPClothespinManager.ClothespinState.Pinch)
        {
            Pinch();
        }
        else
        {
            Open();
        }
        /*
        else if (Input.GetKey(KeyCode.LeftArrow) || padAction.GetState(SteamVR_Input_Sources.Any))
        {
            Open();
        }
        */
    }

    public void Pinch()
    {
        pinchLevel += deltaLevel;
        if (pinchLevel > maxLevel)
            pinchLevel = maxLevel;

        animator.SetFloat("InputAxis1", pinchLevel);
    }


    public void Open()
    {
        pinchLevel -= deltaLevel;
        if (pinchLevel < minLevel)
            pinchLevel = minLevel;

        animator.SetFloat("InputAxis1", pinchLevel);
    }

}
