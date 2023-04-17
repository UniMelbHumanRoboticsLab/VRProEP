using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRProEP.GameEngineCore;


public class ADLPoseListManager : MonoBehaviour
{
    // List of poses
    [SerializeField]
    private Sprite[] poseSpriteList;
    public int PoseNumber { get => poseSpriteList.Length; }

    private List<string> poses = new List<string>();// List of poses
    private List<AudioClip> poseAudioClips = new List<AudioClip>();
    AudioSource audio;
    private SpriteRenderer spriteRenderer;
    // Signs
    private bool hasSelected = false; // Whether a bottle has been selected
    private int selectedIndex = -1;
    private bool selectedTouched = false;

    

    // Accessor
    public bool SelectedTouched { get => selectedTouched; }
    public int TargetNumber
    {
        get
        {
                return poses.Count;
        }
    }

    public string GetPoseName(int index)
    {
        return poses[index];
    }
  
    public AudioClip GetPoseAudio(int index)
    {
        return poseAudioClips[index]; //Play the audio
    }

    public string SelectedPose(int index)
    {
        return poses[index];
    }
            

   

    void Start()
    {
        // Debug
        /*
        GenerateBottleLocations();
        SpawnBottleGrid();
        ResetBottleSelection();
        */
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        poseSpriteList = Resources.LoadAll<Sprite>("HandGestures");
       
    }

    void Update()
    {
        // Debug: Change selected bottle for debug
        /*
        if (Input.GetKeyDown(KeyCode.F1))
        {
            selectedIndex = selectedIndex + 1;
            if (selectedIndex > poses.Count-1) selectedIndex = 0;
            SelectPose(selectedIndex);

        }
        */
    }


    

    #region public methods
    /// <summary>
    /// Add the locations of the grid
    /// </summary>
    /// <param >
    /// <returns 
    public void AddPose(string pose)
    {
        poses.Add(pose);

        Debug.Log("Add pose:" + pose);
    }

    public void AddPose(string pose, AudioClip poseAudio)
    {
        poses.Add(pose);
        poseAudioClips.Add(poseAudio);
        Debug.Log("Add pose:" + pose);
    }




    /// <summary>
    /// Select a bottle by index
    /// </summary>
    /// <param int index>
    /// <returns>
    public void SelectPose(int index)
    {
        // Reset previous selections
        ResetPoseSelection();
        // Change selection index
        selectedIndex = index;
        // Change the sprite
        spriteRenderer.sprite = poseSpriteList[index];

        hasSelected = true;
        selectedTouched = false;
        
    }

    /// <summary>
    /// Clears the ball selection
    /// </summary>
    public void ResetPoseSelection()
    {

        hasSelected = false;
        selectedTouched = false;
    }


    #endregion
}
