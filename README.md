# UnityURP-RadianceCascades2DGI
A Realtime 2D Global Illumination for Unity URP.</br>
It's practically a 2D implementation of Radiance Cascades by [Alexander Sannikov](https://drive.google.com/file/d/1L6v1_7HY2X-LV3Ofb6oyTIxgEaP4LOI6/view) based on [GM Shaders](https://gmshaders.com/) articles.

## Preview Video
Tested on RTX 3060: https://youtu.be/Yp045Kanq5A

## How to Use
Import the "RC2DGI" folder to your project, then add the "RadianceCascades2DGI" renderer feature to your URP Renderer Asset.</br>
From there choose the layer mask of the elements you want to use for GI calculation.</br>
![image](https://github.com/user-attachments/assets/80f973c7-25c5-4bc9-a41d-fb1111e9de3f)

Next, go to your scene and add the Volume compoment to your Volume profile:</br>
From there you customize the settings as you want.</br>
(the RenderScale is the most important property there cause it's gonna affect the performance a lot)
![image](https://github.com/user-attachments/assets/38613329-2583-4588-8f64-243bd44e2bf4)

## Screenshots
![Image Sequence_001_0000](https://github.com/user-attachments/assets/781ef72a-ec19-40cb-8eef-f7b26aee20f8)
![Image Sequence_004_0000](https://github.com/user-attachments/assets/40be6907-e8f4-4de1-b1a9-1fc807ec233a)
![Image Sequence_002_0000](https://github.com/user-attachments/assets/4c8992e5-7a80-4039-aa9d-1ebfa18e03ee)

## References
- [Alexander Sannikov Paper](https://drive.google.com/file/d/1L6v1_7HY2X-LV3Ofb6oyTIxgEaP4LOI6/view)
- GM Shaders Articles : [Intro](https://mini.gmshaders.com/p/radiance-cascades), [Optimization](https://mini.gmshaders.com/p/radiance-cascades2)
- https://github.com/Yaazarai/GMShaders-Radiance-Cascades
- [NullTale GiLight Free Asset](https://assetstore.unity.com/packages/tools/particles-effects/gilight-2d-raytracing-and-lighting-system-268033)
