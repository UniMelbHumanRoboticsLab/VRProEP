using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClothespinTaskManager : MonoBehaviour
{
    [SerializeField]
    private GameObject clothespinPrefab;

    [SerializeField]
    private List<ClothespinManager> clothespinList;

    private readonly string[] horizontalAttachPoint = { "AttachPoint_H2_1", "AttachPoint_H2_2" };
    private readonly string[] verticalAttachPoint = { "AttachPoint_H2_1", "AttachPoint_H2_2" };

    [SerializeField]
    private List<Transform> horizontalTargets;

    [SerializeField]
    private List<Transform> verticalTargets;


    // Start is called before the first frame update
    void Start()
    {
        GetAllTargetTransform();
        InitClothespin();
        int index = 0;
        SelectClothespin(index);
        SetClothespinTargetTransform(index, horizontalTargets[index], verticalTargets[index]);
    }

    // Update is called once per frame
    void Update()
    {

    }


    #region private methods
    //
    // Get target locations
    //
    private void GetAllTargetTransform()
    {
        foreach (string name in horizontalAttachPoint)
        {
            GameObject attachPoint = GameObject.Find(name);
            horizontalTargets.Add(attachPoint.transform);
        }

        foreach (string name in verticalAttachPoint)
        {
            GameObject attachPoint = GameObject.Find(name);
            verticalTargets.Add(attachPoint.transform);
        }

    }

    //
    // Set closthespin initial and target transform
    //
    private void SetClothespinTargetTransform(int index,Transform init, Transform target)
    {
        if (index > clothespinList.Count - 1)
            throw new System.ArgumentOutOfRangeException("The requested pin index is invalid.");

        clothespinList[index].SetTargetTransform(init, target);
            
    }



    //
    // Initialise pin locations
    //
    private void InitClothespin()
    {
        foreach (Transform target in horizontalTargets)
        {
            GameObject pinGO = Instantiate(clothespinPrefab,
                    target.transform.position, target.transform.rotation);
            clothespinList.Add(pinGO.GetComponentInChildren<ClothespinManager>());

        }

    }

    // Select clothespin as target
    private void SelectClothespin(int index)
    {
        clothespinList[index].SetSelect();
    }

    #endregion


}
