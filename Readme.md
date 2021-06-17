# Raycasted Sound
Simple script that makes sound way more realistic. Sound will change depending on the walls between player and audio source, Becoming deeper, basier, less directional and quiter if you are behind the wall, as it does in real life. All effects are adjustable.

Use udon# to compile it, you can get it here: [UdonSharp](https://github.com/MerlinVR/UdonSharp) 

To use the script add it to udon behavour on the AudioSource object, put the this object into "Audio Source" field, and add the LowPass filter.
> Note: Only the object on Environment (11) layer will affect the sound.

Default values seems to be optimal in most of the situations, but you might want to tweak it.
