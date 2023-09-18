using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScopicManager : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    private float grasp;
    private float rotation;

    private const float delta = 0.04f;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        grasp = 0.0f;
        rotation = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            grasp = grasp - delta;
            if (grasp > 1.0f || grasp < -1.0f)
                grasp = Mathf.Sign(grasp) * 1.0f;

            animator.SetFloat("Grasp", grasp);
            Debug.Log("Gripper Close");
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            grasp = grasp + delta;
            if (grasp > 1.0f || grasp < -1.0f)
                grasp = Mathf.Sign(grasp) * 1.0f;

            animator.SetFloat("Grasp", grasp);
            Debug.Log("Gripper Open");
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            rotation = rotation - delta;
            if (rotation > 1.0f || rotation < -1.0f)
                rotation = Mathf.Sign(rotation) * 1.0f;

            animator.SetFloat("Rotate", rotation);
            Debug.Log("Rotate Clock-wise");
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            rotation = rotation + delta;
            if (rotation > 1.0f || rotation < -1.0f)
                rotation = Mathf.Sign(rotation) * 1.0f;

            animator.SetFloat("Rotate", rotation);
            Debug.Log("Rotate Anti-clock-wise");
        }
    }
}
