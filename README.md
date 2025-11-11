# Project_AudioVisualizer
## It is highly recommended to have a dedicated GPU to try this project since HDRP is pretty heavy on performance. The build has not been tested on integrated GPUs and performance will most likely not be smooth.
To try the current build, download the .zip file from the link at the end of this readme, unzip it to a folder of your choice and then run it by executing **Project_AudioVisualizer.exe**.  
Before the audio visualizer activates, you must select an audio output device from the lower right corner of the program. While microphones (or different input devices) show up as well, choosing one of them will not do anything. Instead select your speakers or headphones and play some music on your device and the visualizer should start working.  
<img width="1708" height="962" alt="image" src="https://github.com/user-attachments/assets/1724b105-ba8f-4357-9724-62d9588a2f42" />

You can select different color schemes from the lower left corner of the screen and different audio bar normalization methods from the right side of the screen, above the audio selection menu.  
- Gain
  - This applies scaling according to the energy of the frequencies inside each frequency band. While technically this is the most accurate representation of audio, higher frequencies are not nearly as common as lower ones and the right side of visualizer isn't as active as the left side.
- Psychoacoustic
  - This applies scaling as well but this time according to an approximation of the human ear's ability to perceive the loudness of different frequencies.
- Adaptive
  - This applies a scaling relative to the average energy of each frequency band which causes a fairly balanced representation across all frequency bands. This doesn't have as much detail as the other methods but it uses a wider range of the visualizer bars.
 

Link to current build: [v0.1.0-alpha](https://github.com/eihaav/Project_AudioVisualizer/releases/tag/v0.1.0-alpha)
