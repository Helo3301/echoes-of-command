using Godot;
using EchoesOfCommand.Enums;
using EchoesOfCommand.Ships;
using EchoesOfCommand.Sensors;

namespace EchoesOfCommand.UI;

/// <summary>
/// Main HUD overlay. Fleet roster, weapon selection, minimap, mission timer.
/// </summary>
public partial class HUD : CanvasLayer
{
    private PlayerShip? _player;
    private SensorSystem? _sensors;

    // Fleet roster
    private VBoxContainer _fleetRoster = null!;
    // Weapon indicator
    private Label _weaponLabel = null!;
    // Mission timer
    private Label _timerLabel = null!;
    // Speed indicator
    private Label _speedLabel = null!;
    // Minimap
    private Control _minimapContainer = null!;

    private float _updateTimer;
    private const float UpdateInterval = 0.5f;

    public override void _Ready()
    {
        BuildUI();
    }

    public void Initialize(PlayerShip player, SensorSystem sensors)
    {
        _player = player;
        _sensors = sensors;
    }

    public override void _Process(double delta)
    {
        _updateTimer -= (float)delta;
        if (_updateTimer <= 0f)
        {
            _updateTimer = UpdateInterval;
            UpdateFleetRoster();
            UpdateMinimap();
        }

        UpdateWeaponIndicator();
        UpdateSpeedIndicator();
    }

    public void SetMissionTime(float seconds)
    {
        int mins = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        _timerLabel.Text = $"{mins:D2}:{secs:D2}";
    }

    private void BuildUI()
    {
        // Fleet roster (left side)
        _fleetRoster = new VBoxContainer();
        _fleetRoster.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _fleetRoster.Position = new Vector2(10, 10);
        _fleetRoster.CustomMinimumSize = new Vector2(200, 0);

        var rosterTitle = new Label { Text = "FLEET STATUS" };
        rosterTitle.AddThemeColorOverride("font_color", new Color(0.4f, 0.8f, 1f));
        _fleetRoster.AddChild(rosterTitle);
        AddChild(_fleetRoster);

        // Weapon indicator (bottom center)
        _weaponLabel = new Label();
        _weaponLabel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _weaponLabel.Position = new Vector2(0, -40);
        _weaponLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _weaponLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.3f));
        _weaponLabel.AddThemeFontSizeOverride("font_size", 18);
        AddChild(_weaponLabel);

        // Mission timer (top center)
        _timerLabel = new Label { Text = "00:00" };
        _timerLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        _timerLabel.Position = new Vector2(0, 10);
        _timerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _timerLabel.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        _timerLabel.AddThemeFontSizeOverride("font_size", 24);
        AddChild(_timerLabel);

        // Speed indicator (bottom left)
        _speedLabel = new Label { Text = "0 m/s" };
        _speedLabel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        _speedLabel.Position = new Vector2(10, -40);
        _speedLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.9f, 0.7f));
        AddChild(_speedLabel);

        // Minimap container (top right)
        _minimapContainer = new Control();
        _minimapContainer.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _minimapContainer.Position = new Vector2(-210, 10);
        _minimapContainer.CustomMinimumSize = new Vector2(200, 200);
        AddChild(_minimapContainer);
    }

    private void UpdateFleetRoster()
    {
        // Clear old entries (keep title)
        while (_fleetRoster.GetChildCount() > 1)
            _fleetRoster.GetChild(1).QueueFree();

        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is not ShipBase ship) continue;
            if (ship.Faction != Faction.Player) continue;

            var entry = CreateRosterEntry(ship);
            _fleetRoster.AddChild(entry);
        }
    }

    private HBoxContainer CreateRosterEntry(ShipBase ship)
    {
        var row = new HBoxContainer();

        // Ship name
        string name = ship is PlayerShip ? $"[YOU] {ship.ShipClass}" : ship.ShipClass.ToString();
        var nameLabel = new Label { Text = name };
        nameLabel.CustomMinimumSize = new Vector2(120, 0);

        var (hullHp, hullMax, _) = ship.Subsystems.GetSubsystemHealth(SubsystemType.Hull);
        float ratio = hullHp / hullMax;
        nameLabel.AddThemeColorOverride("font_color", GetHealthColor(ratio));
        row.AddChild(nameLabel);

        // HP bar
        var bar = new ProgressBar();
        bar.MinValue = 0;
        bar.MaxValue = hullMax;
        bar.Value = hullHp;
        bar.CustomMinimumSize = new Vector2(80, 16);
        bar.ShowPercentage = false;

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = GetHealthColor(ratio) * 0.8f;
        bar.AddThemeStyleboxOverride("fill", styleBox);
        row.AddChild(bar);

        return row;
    }

    private void UpdateWeaponIndicator()
    {
        if (_player == null) return;

        string weaponName = _player.SelectedWeapon switch
        {
            WeaponType.Laser => "[1] LASER",
            WeaponType.Missile => "[2] MISSILE",
            WeaponType.Scattershot => "[3] SCATTERSHOT",
            _ => "---"
        };

        _weaponLabel.Text = weaponName;
    }

    private void UpdateSpeedIndicator()
    {
        if (_player == null) return;
        float speed = _player.CurrentVelocity.Length();
        _speedLabel.Text = $"{speed:F0} m/s";
    }

    private void UpdateMinimap()
    {
        // Clear old dots
        foreach (var child in _minimapContainer.GetChildren())
            child.QueueFree();

        // Background
        var bg = new ColorRect();
        bg.Color = new Color(0.05f, 0.08f, 0.12f, 0.7f);
        bg.Size = new Vector2(200, 200);
        _minimapContainer.AddChild(bg);

        if (_player == null) return;

        float mapRadius = 5000f; // 10km diameter mapped to 200px
        var center = new Vector2(100, 100);

        foreach (var node in GetTree().GetNodesInGroup("ships"))
        {
            if (node is not ShipBase ship) continue;
            if (ship.Subsystems.IsDestroyed) continue;

            var offset = ship.GlobalPosition - _player.GlobalPosition;
            var mapPos = center + new Vector2(offset.X, offset.Z) / mapRadius * 100f;

            // Clamp to minimap bounds
            mapPos = mapPos.Clamp(Vector2.Zero, new Vector2(200, 200));

            var dot = new ColorRect();
            dot.Size = new Vector2(4, 4);
            dot.Position = mapPos - new Vector2(2, 2);
            dot.Color = ship.Faction == Faction.Player
                ? new Color(0.2f, 1f, 0.2f)
                : new Color(1f, 0.2f, 0.2f);

            if (ship is PlayerShip)
            {
                dot.Size = new Vector2(6, 6);
                dot.Position = mapPos - new Vector2(3, 3);
                dot.Color = new Color(0.2f, 0.8f, 1f);
            }

            _minimapContainer.AddChild(dot);
        }

        // Sensor contacts (delayed positions)
        if (_sensors != null)
        {
            foreach (var (_, contact) in _sensors.Contacts)
            {
                var offset = contact.ApparentPosition - _player.GlobalPosition;
                var mapPos = center + new Vector2(offset.X, offset.Z) / mapRadius * 100f;
                mapPos = mapPos.Clamp(Vector2.Zero, new Vector2(200, 200));

                var ghost = new ColorRect();
                ghost.Size = new Vector2(3, 3);
                ghost.Position = mapPos - new Vector2(1.5f, 1.5f);
                ghost.Color = new Color(1f, 0.5f, 0.2f, 0.5f); // Orange ghost
                _minimapContainer.AddChild(ghost);
            }
        }
    }

    private static Color GetHealthColor(float ratio)
    {
        if (ratio > 0.66f) return new Color(0.2f, 1f, 0.2f);
        if (ratio > 0.33f) return new Color(1f, 1f, 0.2f);
        return new Color(1f, 0.2f, 0.2f);
    }
}
