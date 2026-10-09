using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.ViewModels;

namespace Soundboard.Avalonia.UI.Controls;

public partial class SoundPad : UserControl
{
    private bool _pointerDown;

    public SoundPad()
    {
        InitializeComponent();

        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _pointerDown = false;
    }

    private void OnPressed(object? s, PointerPressedEventArgs e) => _pointerDown = true;

    private void OnReleased(object? s, PointerReleasedEventArgs e)
    {
        var wasDown = _pointerDown;
        _pointerDown = false;

        if (wasDown && new Rect(Bounds.Size).Contains(e.GetPosition(this)))
        {
            if (DataContext is SoundModel sound)
            {
                var vm = (this.VisualRoot as Control)?.DataContext as SoundboardViewModel
                         ?? App.Host?.Services.GetService<SoundboardViewModel>();

                if (e.InitialPressMouseButton == MouseButton.Left)
                {
                    vm?.PlaySound(sound);
                    e.Handled = true;
                }
                else if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    vm?.StopSound(sound);
                    e.Handled = true;
                }
            }
        }
    }

    private void OnMoved(object? s, PointerEventArgs e)
    {
        if (!_pointerDown || AnyButtonDown(e)) return;
        _pointerDown = false;
    }

    private static bool AnyButtonDown(PointerEventArgs e)
    {
        var p = e.GetCurrentPoint(null).Properties;
        return p.IsLeftButtonPressed || p.IsRightButtonPressed || p.IsMiddleButtonPressed;
    }
}