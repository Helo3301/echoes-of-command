using Godot;
using EchoesOfCommand.AI;
using EchoesOfCommand.Core;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Orders;
using EchoesOfCommand.Sensors;
using EchoesOfCommand.Ships;
using EchoesOfCommand.Subsystems;
using EchoesOfCommand.UI;
using EchoesOfCommand.Weapons;

namespace EchoesOfCommand.Mission;

/// <summary>
/// Manages the single demonstration mission: spawn fleets, enemy waves, win/loss conditions.
/// Attach to the Main scene root.
/// </summary>
public partial class MissionManager : Node3D
{
    [Export] public float SecondWaveTime { get; set; } = 150f; // 2:30
    [Export] public float VictoryTime { get; set; } = 300f; // 5:00

    private float _missionTimer;
    private bool _secondWaveSpawned;
    private bool _missionEnded;
    private int _nextShipId = 1;

    // Systems
    private LightSpeedSimulation _lightSim = null!;
    private OrderSystem _orderSystem = null!;
    private SensorSystem _sensorSystem = null!;
    private HUD _hud = null!;

    // Ships
    private PlayerShip? _playerShip;
    private readonly List<AIShip> _fleetShips = new();
    private readonly List<AIShip> _enemyShips = new();

    // Victory/Defeat
    private Control? _endScreen;

    public override void _Ready()
    {
        _lightSim = GetNode<LightSpeedSimulation>("LightSpeedSimulation");

        SetupOrderSystem();
        SetupSensorSystem();
        SetupHUD();
        SpawnPlayerFleet();
        SpawnEnemyWave(6, 4000f);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_missionEnded) return;

        _missionTimer += (float)delta;
        _hud.SetMissionTime(_missionTimer);

        // Second wave at 2:30
        if (!_secondWaveSpawned && _missionTimer >= SecondWaveTime)
        {
            _secondWaveSpawned = true;
            SpawnEnemyWave(4, 4000f);
        }

        CheckWinLoss();
    }

    private void SetupOrderSystem()
    {
        _orderSystem = new OrderSystem();
        AddChild(_orderSystem);
    }

    private void SetupSensorSystem()
    {
        _sensorSystem = new SensorSystem();
        AddChild(_sensorSystem);
    }

    private void SetupHUD()
    {
        _hud = new HUD();
        AddChild(_hud);
    }

    private void SpawnPlayerFleet()
    {
        // Player flagship (Battlecruiser) at origin
        _playerShip = CreatePlayerShip(ShipClass.Battlecruiser, Vector3.Zero);
        _orderSystem.SetFlagship(_playerShip);
        _sensorSystem.SetObserver(_playerShip);
        _hud.Initialize(_playerShip, _sensorSystem);

        // Fleet ships in V-formation behind flagship
        var fleetClasses = new[]
        {
            ShipClass.Battleship,
            ShipClass.HeavyCruiser,
            ShipClass.LightCruiser,
            ShipClass.Destroyer
        };

        for (int i = 0; i < fleetClasses.Length; i++)
        {
            int side = (i % 2 == 0) ? -1 : 1;
            int rank = (i / 2) + 1;
            var pos = new Vector3(
                side * GameConstants.FormationSpacing * rank * 0.5f,
                0,
                GameConstants.FormationSpacing * rank);

            var ship = CreateFleetShip(fleetClasses[i], pos, i);
            _fleetShips.Add(ship);
        }
    }

    private PlayerShip CreatePlayerShip(ShipClass shipClass, Vector3 position)
    {
        var ship = new PlayerShip();
        ship.ShipClass = shipClass;
        ship.ShipId = _nextShipId++;
        ship.GlobalPosition = position;
        ship.Faction = Faction.Player;

        // Add mesh placeholder
        AddShipMesh(ship, new Color(0.3f, 0.6f, 1f));

        // Add weapons
        var weaponSystem = new WeaponSystem();
        weaponSystem.AddChild(new LaserWeapon());
        weaponSystem.AddChild(new MissileWeapon());
        weaponSystem.AddChild(new ScattershotWeapon());
        ship.AddChild(weaponSystem);

        // Add damage visuals
        ship.AddChild(new DamageVisuals());

        // Add collision shape
        AddCollisionShape(ship);

        AddChild(ship);
        return ship;
    }

    private AIShip CreateFleetShip(ShipClass shipClass, Vector3 position, int formationIndex)
    {
        var ship = new AIShip();
        ship.ShipClass = shipClass;
        ship.ShipId = _nextShipId++;
        ship.GlobalPosition = position;
        ship.Faction = Faction.Player;

        AddShipMesh(ship, new Color(0.2f, 0.8f, 0.4f));

        var weaponSystem = new WeaponSystem();
        var stats = ShipClassDatabase.Get(shipClass);
        weaponSystem.AddChild(CreateWeapon(stats.PrimaryWeapon));
        weaponSystem.AddChild(CreateWeapon(stats.SecondaryWeapon));
        ship.AddChild(weaponSystem);

        ship.AddChild(new DamageVisuals());
        AddCollisionShape(ship);

        // Fleet AI
        var ai = new FleetAI();
        ai.FormationIndex = formationIndex;
        ship.AddChild(ai);

        // Wire order delivery to AI
        ship.OrderReceived += ai.OnOrderReceived;

        AddChild(ship);

        // Set flagship reference (player ship is already in tree)
        if (_playerShip != null)
            ai.SetFlagship(_playerShip);

        return ship;
    }

    private AIShip CreateEnemyShip(ShipClass shipClass, Vector3 position)
    {
        var ship = new AIShip();
        ship.ShipClass = shipClass;
        ship.ShipId = _nextShipId++;
        ship.GlobalPosition = position;
        ship.Faction = Faction.Enemy;

        AddShipMesh(ship, new Color(1f, 0.2f, 0.2f));

        var weaponSystem = new WeaponSystem();
        var stats = ShipClassDatabase.Get(shipClass);
        weaponSystem.AddChild(CreateWeapon(stats.PrimaryWeapon));
        weaponSystem.AddChild(CreateWeapon(stats.SecondaryWeapon));
        ship.AddChild(weaponSystem);

        ship.AddChild(new DamageVisuals());
        AddCollisionShape(ship);

        ship.AddChild(new EnemyAI());

        AddChild(ship);
        return ship;
    }

    private void SpawnEnemyWave(int count, float distance)
    {
        var enemyClasses = new[]
        {
            ShipClass.HeavyCruiser, ShipClass.LightCruiser, ShipClass.Destroyer,
            ShipClass.LightCruiser, ShipClass.Destroyer, ShipClass.Destroyer,
            ShipClass.HeavyCruiser, ShipClass.Battleship
        };

        float spread = 300f;
        for (int i = 0; i < count && i < enemyClasses.Length; i++)
        {
            float x = (i - count / 2f) * spread;
            var pos = new Vector3(x, 0, -distance);
            var ship = CreateEnemyShip(enemyClasses[i], pos);
            _enemyShips.Add(ship);
        }
    }

    private void CheckWinLoss()
    {
        // Defeat: player flagship destroyed
        if (_playerShip != null && _playerShip.Subsystems.IsDestroyed)
        {
            EndMission(false, "Flagship Destroyed");
            return;
        }

        // Defeat: all fleet ships destroyed (player still alive)
        if (_fleetShips.Count > 0 && _fleetShips.TrueForAll(s => s.Subsystems.IsDestroyed))
        {
            EndMission(false, "Fleet Lost");
            return;
        }

        // Victory: all enemies destroyed
        if (_enemyShips.Count > 0 && _secondWaveSpawned &&
            _enemyShips.TrueForAll(s => s.Subsystems.IsDestroyed))
        {
            EndMission(true, "All Enemies Destroyed");
            return;
        }

        // Victory: survive 5 minutes
        if (_missionTimer >= VictoryTime)
        {
            EndMission(true, "Sector Defended");
            return;
        }
    }

    private void EndMission(bool victory, string reason)
    {
        _missionEnded = true;
        GetTree().Paused = true;

        _endScreen = new Control();
        _endScreen.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _endScreen.ProcessMode = ProcessModeEnum.Always;

        // Dark overlay
        var overlay = new ColorRect();
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        overlay.Color = new Color(0, 0, 0, 0.7f);
        _endScreen.AddChild(overlay);

        // Result text
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.Center);
        vbox.Position = new Vector2(-150, -100);

        var titleLabel = new Label();
        titleLabel.Text = victory ? "VICTORY" : "DEFEAT";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 48);
        titleLabel.AddThemeColorOverride("font_color",
            victory ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.2f, 0.2f));
        vbox.AddChild(titleLabel);

        var reasonLabel = new Label { Text = reason };
        reasonLabel.HorizontalAlignment = HorizontalAlignment.Center;
        reasonLabel.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(reasonLabel);

        int mins = (int)(_missionTimer / 60);
        int secs = (int)(_missionTimer % 60);
        var timeLabel = new Label { Text = $"Time: {mins:D2}:{secs:D2}" };
        timeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(timeLabel);

        int fleetLost = _fleetShips.FindAll(s => s.Subsystems.IsDestroyed).Count;
        int enemiesKilled = _enemyShips.FindAll(s => s.Subsystems.IsDestroyed).Count;
        var statsLabel = new Label { Text = $"Fleet Lost: {fleetLost}  |  Enemies Destroyed: {enemiesKilled}" };
        statsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(statsLabel);

        // Restart button
        var restartBtn = new Button { Text = "RESTART MISSION" };
        restartBtn.CustomMinimumSize = new Vector2(200, 40);
        restartBtn.Pressed += () =>
        {
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
        };
        vbox.AddChild(restartBtn);

        _endScreen.AddChild(vbox);

        // Add to HUD layer so it renders on top
        _hud.AddChild(_endScreen);
    }

    private static void AddShipMesh(ShipBase ship, Color color)
    {
        var mesh = new MeshInstance3D();
        mesh.Name = "Mesh";

        // Simple arrow-like shape using prism
        var prism = new PrismMesh();
        prism.Size = new Vector3(8, 3, 15);
        mesh.Mesh = prism;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.EmissionEnabled = true;
        mat.Emission = color * 0.3f;
        mesh.SetSurfaceOverrideMaterial(0, mat);

        ship.AddChild(mesh);
    }

    private static void AddCollisionShape(ShipBase ship)
    {
        var shape = new BoxShape3D();
        shape.Size = new Vector3(8, 3, 15);
        var collider = new CollisionShape3D();
        collider.Shape = shape;
        ship.AddChild(collider);
    }

    private static WeaponBase CreateWeapon(WeaponType type)
    {
        return type switch
        {
            WeaponType.Laser => new LaserWeapon(),
            WeaponType.Missile => new MissileWeapon(),
            WeaponType.Scattershot => new ScattershotWeapon(),
            _ => new LaserWeapon()
        };
    }
}
