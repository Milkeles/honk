using Godot;

namespace Presentation;

public partial class TransitionSceneView : CanvasLayer
{
    [Export] private float _switchDuration = 1.0f;

    private ColorRect _colorRect;

    public override void _Ready()
    {
        base._Ready();
        GD.Print("Called!");
        _colorRect = GetNode<ColorRect>("ColorRect");
        _colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        _colorRect.Modulate = new Color(1, 1, 1, 0);
    }

    public async void GoToScene(string scene)
    {
        _colorRect.MouseFilter = Control.MouseFilterEnum.Stop;
        var tween = GetTree().CreateTween();
        tween.TweenProperty(_colorRect, "modulate",
            new Color(1, 1, 1, 1), _switchDuration / 2f);

        await ToSignal(tween, Tween.SignalName.Finished);

        GetTree().ChangeSceneToFile($"res://scenes/{scene}.tscn");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        tween = GetTree().CreateTween();
        tween.TweenProperty(_colorRect, "modulate",
            new Color(1, 1, 1, 0), _switchDuration / 2f);

        await ToSignal(tween, Tween.SignalName.Finished);

        _colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
    }
}