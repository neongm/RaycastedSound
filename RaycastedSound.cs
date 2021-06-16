
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RaycastedSound : UdonSharpBehaviour
{
    // defining public variables, that will be visible in Unity Edtior
    public AudioSource AudioSource;     // AudioSource to apply the effect

    [Header("Audio settings")]
    public bool Cutoff = true; // Cutoff by wall toggle
    [Range(0f, 1f)] public float WallCutoff = 0.5f; // Cutoff by single wall
    public bool VolumeDecrease = true; // Volume decrease by wall toggle
    public bool SpatialBlendChange = true; // Blending of sound direction toggle
    public float WallVolumeDecreaseSmoothing = 10f;  // Smoothness of volume changes
    public float CutoffChangeSmoothing = 10f;  // Smoothness of Cutoff changes
    public float SpatialBlendChangeSmoothing = 10f; // Smoothness of Spatial Blend changes
    public float CutoffStartDistance = 20f;  // Cutoff by distance starting distance

    [Range(0.01f, 1f)] public float DistanceCutoff = 0.3f; // Cutoff by distance multiplier
    [Range(0f, 1f)] public float VolDecPerWall = 0.05f; // Volume decrease by single wall
    [Range(0f, 1f)] public float SpatialBlendChangePerWall = 0.1f; // Spatial Blend change by single wall 
    [Range(0f, 2f)] public float RaycastPlayerHeightCorrection= 1f; // Raycast player height correction
    [Range(0f, 1f)] public float VolumeMax = 1f; // AudioSourve maximum volume
    [Range(0f, 1f)] public float VolumeMin = 0.01f; // AudioSorce minimum volume
    [Range(0f, 1f)] public float SpatialMax = 1f; // Upper bound of spatial blend



    void Start()
    {
        
    }
}
