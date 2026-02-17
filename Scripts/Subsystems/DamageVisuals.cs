using Godot;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Ships;

namespace EchoesOfCommand.Subsystems;

/// <summary>
/// Attaches to a ShipBase and provides visual feedback for subsystem damage.
/// Changes mesh color and spawns sparks/smoke when subsystems are damaged or destroyed.
/// </summary>
public partial class DamageVisuals : Node3D
{
    [Export] public Color HealthyColor { get; set; } = new(0.6f, 0.7f, 0.8f);
    [Export] public Color DamagedColor { get; set; } = new(0.9f, 0.5f, 0.1f);
    [Export] public Color CriticalColor { get; set; } = new(0.9f, 0.1f, 0.1f);

    private ShipBase? _ship;
    private MeshInstance3D? _meshInstance;
    private GpuParticles3D? _sparks;
    private GpuParticles3D? _smoke;
    private StandardMaterial3D? _material;

    public override void _Ready()
    {
        _ship = GetParent<ShipBase>();
        _meshInstance = _ship?.GetNodeOrNull<MeshInstance3D>("Mesh");

        if (_meshInstance?.GetSurfaceOverrideMaterial(0) is StandardMaterial3D mat)
        {
            _material = mat;
        }
        else if (_meshInstance != null)
        {
            _material = new StandardMaterial3D { AlbedoColor = HealthyColor };
            _meshInstance.SetSurfaceOverrideMaterial(0, _material);
        }

        SetupSparks();
        SetupSmoke();

        if (_ship != null)
        {
            _ship.Subsystems.SubsystemDestroyed += OnSubsystemDestroyed;
        }
    }

    public override void _Process(double delta)
    {
        if (_ship == null || _material == null)
            return;

        // Overall health ratio (average of all subsystems)
        float healthRatio = GetOverallHealthRatio();

        // Interpolate color based on health
        if (healthRatio > 0.66f)
            _material.AlbedoColor = HealthyColor;
        else if (healthRatio > 0.33f)
            _material.AlbedoColor = HealthyColor.Lerp(DamagedColor, (0.66f - healthRatio) / 0.33f);
        else
            _material.AlbedoColor = DamagedColor.Lerp(CriticalColor, (0.33f - healthRatio) / 0.33f);

        // Sparks when below 50% hull
        var (hullHp, hullMax, _) = _ship.Subsystems.GetSubsystemHealth(SubsystemType.Hull);
        if (_sparks != null)
            _sparks.Emitting = hullHp / hullMax < 0.5f;

        // Smoke when engine destroyed
        if (_smoke != null)
            _smoke.Emitting = !_ship.Subsystems.IsSubsystemFunctional(SubsystemType.Engine);
    }

    private float GetOverallHealthRatio()
    {
        if (_ship == null) return 1f;

        float total = 0f;
        float max = 0f;
        foreach (SubsystemType type in System.Enum.GetValues<SubsystemType>())
        {
            var (current, maxHp, _) = _ship.Subsystems.GetSubsystemHealth(type);
            total += current;
            max += maxHp;
        }
        return max > 0f ? total / max : 1f;
    }

    private void OnSubsystemDestroyed(SubsystemType type)
    {
        // Flash red briefly on destruction
        if (_material != null)
        {
            _material.EmissionEnabled = true;
            _material.Emission = CriticalColor;
            _material.EmissionEnergyMultiplier = 2f;

            // Reset after 0.3s
            GetTree().CreateTimer(0.3).Timeout += () =>
            {
                if (_material != null)
                {
                    _material.EmissionEnabled = false;
                    _material.EmissionEnergyMultiplier = 1f;
                }
            };
        }
    }

    private void SetupSparks()
    {
        _sparks = new GpuParticles3D();
        _sparks.Emitting = false;
        _sparks.Amount = 20;
        _sparks.Lifetime = 0.5f;

        var mat = new ParticleProcessMaterial();
        mat.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
        mat.EmissionSphereRadius = 5f;
        mat.Gravity = Vector3.Zero;
        mat.InitialVelocityMin = 10f;
        mat.InitialVelocityMax = 30f;
        mat.ScaleMin = 0.3f;
        mat.ScaleMax = 0.8f;
        mat.Color = new Color(1f, 0.7f, 0.2f);
        _sparks.ProcessMaterial = mat;

        var mesh = new QuadMesh();
        mesh.Size = new Vector2(1f, 1f);
        var meshMat = new StandardMaterial3D();
        meshMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        meshMat.AlbedoColor = new Color(1f, 0.8f, 0.3f);
        meshMat.BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled;
        mesh.Material = meshMat;
        _sparks.DrawPass1 = mesh;

        AddChild(_sparks);
    }

    private void SetupSmoke()
    {
        _smoke = new GpuParticles3D();
        _smoke.Emitting = false;
        _smoke.Amount = 15;
        _smoke.Lifetime = 2f;

        var mat = new ParticleProcessMaterial();
        mat.EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere;
        mat.EmissionSphereRadius = 3f;
        mat.Gravity = Vector3.Zero;
        mat.InitialVelocityMin = 2f;
        mat.InitialVelocityMax = 8f;
        mat.ScaleMin = 2f;
        mat.ScaleMax = 5f;
        mat.Color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        _smoke.ProcessMaterial = mat;

        var mesh = new QuadMesh();
        mesh.Size = new Vector2(3f, 3f);
        var meshMat = new StandardMaterial3D();
        meshMat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        meshMat.AlbedoColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
        meshMat.BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled;
        meshMat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        mesh.Material = meshMat;
        _smoke.DrawPass1 = mesh;

        AddChild(_smoke);
    }
}
