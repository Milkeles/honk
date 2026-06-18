using Godot;
using System;

namespace Presentation {
    public partial class MenuView : Control
    {
        private TransitionSceneView _transition;
        public override void _Ready()
        {
            _transition = GetNode<TransitionSceneView>("/root/TransitionScene");

            GetNode<Button>("%Play").Pressed += _Play;
            GetNode<Button>("%Quit").Pressed += _Quit;
        }

        private void _Play()
        {
            _transition.GoToScene("Gameplay");
        }

        private void _Quit()
        {
            GetTree().Quit();
        }
    }
}