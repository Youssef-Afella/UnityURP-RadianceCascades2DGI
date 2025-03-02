using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("RC2DGI/Radiance Cascades 2D GI", typeof(UniversalRenderPipeline))]
public class RadianceCascades2DGIVolume : VolumeComponent
{
    public BoolParameter isActive = new BoolParameter(false);
    public IntParameter cascadeCount = new IntParameter(6);
    public ClampedFloatParameter renderScale = new ClampedFloatParameter(0.8f, 0.1f, 1f);
    public ClampedFloatParameter rayRange = new ClampedFloatParameter(1f, 0f, 2f);
    public BoolParameter skyRadiance = new BoolParameter(false);
    public ColorParameter skyColor = new ColorParameter(new Color(0.2f, 0.5f, 1f), true, false, true);
    public ColorParameter sunColor = new ColorParameter(new Color(1f, 0.7f, 0.1f) * 10, true, false, true);
    public ClampedFloatParameter sunAngle = new ClampedFloatParameter(2f, 0f, 6.28f);
}
