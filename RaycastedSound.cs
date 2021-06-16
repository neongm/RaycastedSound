
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RaycastedSound : UdonSharpBehaviour
{
    // defining public variables, that will be visible in Unity Edtior
    public AudioSource AudioSource;     // AudioSource to apply the effect

    [Header("Cutoff filter")]
    public bool Cutoff = true; // Cutoff by wall toggle
    [Range(0f, 1f)] public float WallCutoff = 0.5f; // Cutoff by single wall
    [Range(0.01f, 1f)] public float DistanceCutoff = 0.3f; // Cutoff by distance multiplier
    public float CutoffChangeSmoothing = 10f;  // Smoothness of Cutoff changes
    public float CutoffStartDistance = 20f;  // Cutoff by distance starting distance

    [Header("Volume filter")]
    public bool VolumeDecrease = true; // Volume decrease by wall 
    [Range(0f, 1f)] public float VolumeMax = 1f; // AudioSourve maximum volume
    [Range(0f, 1f)] public float VolumeMin = 0.01f; // AudioSorce minimum volume
    [Range(0f, 1f)] public float VolDecPerWall = 0.05f; // Volume decrease by single wall
    public float CutoffChangeSmoothing = 10f;  // Smoothness of Cutoff changes
    
    [Header("Spatial Blend filter")]
    public bool SpatialBlendChange = true; // Blending of sound direction toggle
    public float SpatialBlendChangeSmoothing = 10f; // Smoothness of Spatial Blend changes
    [Range(0f, 1f)] public float SpatialBlendChangePerWall = 0.1f; // Spatial Blend change by single wall
    [Range(0f, 1f)] public float SpatialMax = 1f; // Upper bound of spatial blend
    
    [Header("Other")]
    [Range(0f, 2f)] public float RaycastPlayerHeightCorrection= 1f; // Raycast player height correction    

    // variables that will be used for calculations
    private float Distance; // for distance from audio source to listener
    private float Distcut; // for target cutoff by distance
    private float Wallcut; // for target cutoff by walls
    // variables for smoothing changes:
    private float CurrentVolume; // current volume of audio source 
    private float TargetVolume; // target volume of audio souce 
    private float CurrentCutoff; // current cutoff
    private float TargetCutoff; // target cutoff 
    private float CurrentBlend; // current spatial blend
    private float TargetBlend;  // target spatial blend
    // player API:
    private VRCPlayerApi Player;  // VRCPlayerAPI
    private Vector3 AudioListenerTransformPosition;  // current position of the player
    // raycat:
    private RaycastHit[] hits; // raycast hits array
    private Ray ray; // raycast ray


    void Start()
    {
        gameObject.GetComponent<AudioLowPassFilter>().enabled = true; // making sure that lowpass filter enabled
        Player = Networking.LocalPlayer;  // getting the local players VRCPlayerAPI
    }

    void FixedUpdate()
    {
        AudioListenerTransformPosition = Player.GetPosition()+Vector3.up*RaycastPlayerHeightCorrection; 
        Distance = Vector3.Distance(transform.position, AudioListenerTransformPosition);
        
        if(distance < AudioSource.maxDistance)
        {
            ApplyFilters();
        }
    }

    private void ApplyFilters()
    {
        hits = GetRaycastHits();
        if(VolumeDecrease) VolumeDecreaseFilter();
        if(Cutoff) CutoffFilter();
        if(SpatialBlendChange) SpatialBlendChangeFilter();
        
    }

    private void SpatialBlendChangeFilter()
    {
        CurrentBlend = AudioSource.spatialBlend;  // getting current spatal blend
        TargetBlend = SpatialMax - SpatialBlendChangePerWall * hits.Length;   // calculating target, but not higher than SpatialMax
        
        // as before if we have smoothing >0 - calculating values depending on SpatialBlendChangeSmoothing
        if(SpatialBlendChangeSmoothing!=0){
            AudioSource.spatialBlend = Smooth(CurrentBlend, TargetBlend, SpatialBlendChangeSmoothing);
        }else{  // or just changing it instantly, but it's a bad idea because it will change 
                // volume as well, so it will be very noticeable
            AudioSource.spatialBlend = TargetBlend;
        }
    }

    private void CutoffFilter()
    {
        // if distance cutoff isn't = 0 - calculating it
        if(DistanceCutoff!=0) distcut = 22000*DistcutSigmoidFunction(distance);
        else distcut = 22000;

        // if there is at least 1 wall - calculating cutoff
        if(hits.Length!=0) wallcut = WallcutFunction(distcut); //distcut*(1-(1-WallCutoff)^hits.length)^2
        else wallcut = 0;

        // calculating target cutoff
        TargetCutoff = distcut - wallcut;
        CurrentCutoff = gameObject.GetComponent<AudioLowPassFilter>().cutoffFrequency;  // get current cutoff
        
        // if smoothing !=0 changing it using smoothing function
        if(CutoffChangeSmoothing!=0){
            gameObject.GetComponent<AudioLowPassFilter>().cutoffFrequency = Smooth(CurrentCutoff, TargetCutoff, CutoffChangeSmoothing);
        }else{  // or just changing cutoff filter to target instantly (bad idea)
            gameObject.GetComponent<AudioLowPassFilter>().cutoffFrequency = TargetCutoff;
        }
    }

    private void VolumeDecreaseFilter()
    {
        CurrentVolume = AudioSource.volume;      // getting current volume 
        TargetVolume = VolumeMax - VolDecPerWall * hits.Length;  // calculating target volume 

        if(VolumeMin!=0 && TargetVolume<VolumeMin)
        {   // ensuring that target volume is not lower than minimum value 
            TargetVolume=VolumeMin;
        }

        if(WallVolumeDecreaseSmoothing!=0)
        {
            AudioSource.volume = Smooth(CurrentVolume, TargetVolume, WallVolumeDecreaseSmoothing);
        }else
        {   // or just changing volume to the target instantly (bad idea)
            // please, use smoothing values > 0
            AudioSource.volume = TargetVolume;
        }
    }

    private int GetrRaycastHits()
    {
        // this method counts all the raycast hits 
        // between local player and target audio source
        // only obejects that on 11th (environment) layer count 
        ray = new Ray(transform.position, AudioListenerTransformPosition-transform.position); // making the raycast ray
        return Physics.RaycastAll(ray, distance, 1<<11); // counting the hits on environment layer of this ray
    }
}
