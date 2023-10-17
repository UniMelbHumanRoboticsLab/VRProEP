using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// SteamVR
using Valve.VR;
using Valve.VR.InteractionSystem;
using VRProEP.ProsthesisCore;

public class ACESHandAnimation : MonoBehaviour
{
    public enum HandStates { Open, Pinch, Null };

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

    private HandStates state;
    public HandStates State { get => state; }

    private const int ZMQ_REF_CHN = 3;

    // Start is called before the first frame update
    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        bool externalInput = false;

        if (ZMQSystem.PullerCount >0 )
        {
            externalInput = ZMQSystem.GetLatestPulledData(MLKinematicSynergy.ZMQ_PULL_PORT)[ZMQ_REF_CHN] == 1.0f;
        }
        else if (UDPInputSystem.UDPPinCount > 0)
        {
            externalInput = (UDPClothespinManager.ClothespinState)UDPInputSystem.GetInput(UDPInputSystem.InputType.UDPClothespinButton, udpChannel) == UDPClothespinManager.ClothespinState.Pinch;
        }
          

        if (Input.GetKey(KeyCode.RightArrow) || buttonAction.GetState(SteamVR_Input_Sources.Any) || externalInput)
        {
            state = HandStates.Pinch;
            Pinch();
        }
        else
        {
            state = HandStates.Open;
            Open();
        }

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
