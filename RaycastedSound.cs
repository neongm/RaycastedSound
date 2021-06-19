
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class RaycastedSound : UdonSharpBehaviour
{
    // Defining public variables, that will be visible in Unity Edtior
    public AudioSource AudioSource;     //!< AudioSource to apply the effect

    [Header("Cutoff filter")]
    public bool Cutoff = true; //!< Cutoff by wall toggle
    [Range(0f, 1f)] public float WallCutoff = 0.56f; //!< Cutoff by single wall
    [Range(0.01f, 1f)] public float DistanceCutoff = 0.3f; //!< Cutoff by distance multiplier
    public float CutoffChangeSmoothing = 10f;  //!< Smoothness of Cutoff changes
    public float CutoffStartDistance = 20f;  //!< Cutoff by distance starting distance

    [Header("Volume filter")]
    public bool VolumeDecrease = true; //!< Volume decrease by wall 
    [Range(0f, 1f)] public float VolumeMax = 1f; //!< AudioSourve maximum volume
    [Range(0f, 1f)] public float VolumeMin = 0.01f; //!< AudioSorce minimum volume
    [Range(0f, 1f)] public float VolDecPerWall = 0.05f; //!< Volume decrease by single wall
    public float WallVolumeDecreaseSmoothing = 10f;  //!< Smoothness of Cutoff changes
    
    [Header("Spatial Blend filter")]
    public bool SpatialBlendChange = true; //!< Blending of sound direction toggle
    public float SpatialBlendChangeSmoothing = 10f; //!< Smoothness of Spatial Blend changes
    [Range(0f, 1f)] public float SpatialBlendChangePerWall = 0.042f; //!< Spatial Blend change by single wall
    [Range(0f, 1f)] public float SpatialMax = 1f; //!< Upper bound of spatial blend
    
    [Header("Other")]
    [Range(0f, 2f)] public float RaycastPlayerHeightCorrection= 1f; //!< Raycast player height correction    

    // variables that will be used for calculations
    private float Distance; //!< Distance from audio source to listener
    private float Distcut; //!< Target cutoff by distance
    private float Wallcut; //!< Target cutoff by walls

    // Variables for smoothing changes:
    private float CurrentVolume; //!< Current volume of audio source 
    private float TargetVolume; //!< Target volume of audio souce 
    private float CurrentCutoff; //!< Current cutoff
    private float TargetCutoff; //!< Target cutoff 
    private float CurrentBlend; //!< Current spatial blend
    private float TargetBlend;  //!< Target spatial blend
    // Player API:
    private VRCPlayerApi Player;  //!< API VRCPlayer 
    private Vector3 AudioListenerTransformPosition;  //!< Current position of the player

    // Raycast:
    private int hits; //!< Raycast hits array
    private Ray ray; //!< Raycast ray

    /*!
        \brief Method that called before the first frame.

        Default Unity Engine method
    */
    void Start()
    {
        // making sure that lowpass filter enabled and getting the local players VRCPlayerAPI
        gameObject.GetComponent<AudioLowPassFilter>().enabled = true; 
        Player = Networking.LocalPlayer;  
    }

    /*!
        \brief Method that called every 1/50s.

        Default Unity Engine method

        \warning if gameObject has no component AudioLowPassFilter - udon will throw runtime exception.
    */
    void FixedUpdate()
    {
        AudioListenerTransformPosition = Player.GetPosition() + Vector3.up * RaycastPlayerHeightCorrection; 
        Distance = Vector3.Distance(transform.position, AudioListenerTransformPosition);
        
        if(Distance < AudioSource.maxDistance){
            ApplyFilters();
        }
    }

    /*!
        \brief Calls filters if they're on.

        Checks if the each filter enabled in the settings and calls coresponging method for applying it, if it is.
    */
    private void ApplyFilters()
    {
        hits = GetRaycastHitsCount();
        if(VolumeDecrease) VolumeDecreaseFilter();
        if(Cutoff) CutoffFilter();
        if(SpatialBlendChange) SpatialBlendChangeFilter();
    }

    /*!
        \brief Applies Spatial Blend filter depending on the raycast.

        Calculates target value for Spatial Blend effect and applies it to AudioSource.
        Makes sound less directional if audio source are behind the wall.
        
        \warning This filter has major effect on overall volume of the sound, less directional audio becomes much louder. Use carefully.
    */
    private void SpatialBlendChangeFilter()
    {
        CurrentBlend = AudioSource.spatialBlend; 
        TargetBlend = SpatialMax - SpatialBlendChangePerWall * hits;  
        
        // if we have smoothing > 0 - calculating values depending on SpatialBlendChangeSmoothing
        // never use spatial blend change without smoothing.
        if(SpatialBlendChangeSmoothing != 0){
            AudioSource.spatialBlend = Smooth(CurrentBlend, TargetBlend, SpatialBlendChangeSmoothing);
        }else{ 
            AudioSource.spatialBlend = TargetBlend;
        }
    }

    /*!
        \brief Applies Cutoff filter depending on the raycast.

        Calculates target value for Cutoff effect and applies it to AudioSource.
        Makes sound deeper and less pronounced while behind the wall.
        
        \warning This filter has major effect on overall volume of the sound.
    */
    private void CutoffFilter()
    {
        // if distance cutoff isn't = 0 - calculating it
        if(DistanceCutoff != 0) Distcut = 22000 * DistcutSigmoidFunction(Distance);
        else Distcut = 22000;

        // if there is at least 1 wall - calculating cutoff
        if(hits != 0) Wallcut = WallcutFunction();
        else Wallcut = 0;

        // calculating target cutoff
        TargetCutoff = Distcut - Wallcut;
        CurrentCutoff = gameObject.GetComponent<AudioLowPassFilter>().cutoffFrequency; 
        
        // if smoothing !=0 changing it using smoothing function
        // or just changing cutoff filter immidiately (don't use that)
        if(CutoffChangeSmoothing != 0){
            gameObject.GetComponent<AudioLowPassFilter>().cutoffFrequency = Smooth(CurrentCutoff, TargetCutoff, CutoffChangeSmoothing);
        }else{  
            gameObject.GetComponent<AudioLowPassFilter>().cutoffFrequency = TargetCutoff;
        }
    }

    /*!
        \brief Applies Volume depending on the raycast.

        Calculates target value for overall volume and applies it to AudioSource.
        Makes sound quiter while behind the wall.
    */
    private void VolumeDecreaseFilter()
    {
        CurrentVolume = AudioSource.volume; 
        TargetVolume = VolumeMax - VolDecPerWall * hits; 

        // ensuring that target volume is not lower than minimum value  
        if(VolumeMin != 0 && TargetVolume < VolumeMin) TargetVolume = VolumeMin;

        // applying it to audioSource
        if(WallVolumeDecreaseSmoothing != 0){
            AudioSource.volume = Smooth(CurrentVolume, TargetVolume, WallVolumeDecreaseSmoothing);
        }else{
            AudioSource.volume = TargetVolume;
        }
    }

     /*!
        \brief Raycast hits counter.

        Calculates target value for Cutoff effect and applies it to AudioSource
        Makes sound deeper and less pronounced while behind the wall
        
        \warning Counts only hits to object on 11th layer

        \todo Make it possible to change the target layer in unity editor
    */
    private int GetRaycastHitsCount()
    {
        ray = new Ray(transform.position, AudioListenerTransformPosition - transform.position);
        return Physics.RaycastAll(ray, Distance, 1 << 11).Length; 
    }

    /*!
        \brief Sigmoid function for cutoff filter

        \param[in] Distance Distance between VRCPlayer and audioSource
        \return Target Cutoff multiplier 
    */
    private float DistcutSigmoidFunction(float distance)
    {  
        return 1 - 1 / (1 + Mathf.Pow(1 + DistanceCutoff, -distance + CutoffStartDistance));
    }

    /*!
        \brief Function for cutoff by raycast

        Mathematical function that calculates the target cutoff frequency depending on raycast data

        \return Target Cutoff value
    */
    private float WallcutFunction()
    {
        return Mathf.Atan(hits * (1 + 10 * WallCutoff)) * Distcut * WallCutoff;
    }

     /*!
        \brief Smoothing values when they change 

        \param[in] current Current value in the parameters of filter
        \param[in] target The target value
        \param[in] smoothing Coefficient of smoothing

        \return Next "current" Value
    */
    private float Smooth(float current, float target, float smoothing)
    {
        if(current < target){
            return current + ((target - current) / smoothing);
        }else{
            return current - ((current - target) / smoothing);
        }
    }
}
