using UnityEngine;
using System.Collections.Generic;

public class AI_LOD_Manager : MonoBehaviour
{
    public static AI_LOD_Manager Instance { get; private set; }

    public float midPriorityRange = 20f;
    public float lowPriorityRange = 40f;

    // How many frames to skip between updates for each priority level
    public int midPriorityUpdateRate = 3;
    public int lowPriorityUpdateRate = 10;

    void Awake() { Instance = this; }
}