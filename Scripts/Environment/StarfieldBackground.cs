using Godot;

namespace EchoesOfCommand.Environment;

/// <summary>
/// Generates a starfield using GPUParticles3D for a space background.
/// Attach to a GPUParticles3D node.
/// </summary>
public partial class StarfieldBackground : GpuParticles3D
{
    [Export] public int StarCount { get; set; } = 2000;
    [Export] public float SpreadRadius { get; set; } = 5000f;

    public override void _Ready()
    {
        Amount = StarCount;
        Lifetime = 1000f; // Effectively permanent
        Explosiveness = 1f; // All emit at once
        OneShot = true;
        FixedFps = 0;

        var material = new ParticleProcessMaterial();
        material.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
        material.EmissionSphereRadius = SpreadRadius;
        material.Gravity = Vector3.Zero;
        material.InitialVelocityMin = 0f;
        material.InitialVelocityMax = 0f;
        material.ScaleMin = 0.5f;
        material.ScaleMax = 2.0f;
        material.Color = new Color(0.8f, 0.85f, 1.0f, 1.0f);
        ProcessMaterial = material;

        // Simple quad mesh for each star
        var mesh = new QuadMesh();
        mesh.Size = new Vector2(2f, 2f);

        var meshMaterial = new StandardMaterial3D();
        meshMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        meshMaterial.AlbedoColor = new Color(1f, 1f, 1f, 1f);
        meshMaterial.BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled;
        meshMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mesh.Material = meshMaterial;

        DrawPass1 = mesh;
    }
}
