using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using System;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.ViewModels;

namespace Soundboard.Avalonia.UI.Controls;

public partial class SoundPad : UserControl
{

    private const double LinesPerSecond = 1.0;
    private const double ReturnSpeedMultiplier = 15.0;
    private const double StartDelaySeconds = 0.15;
    private const double FontRatio = 1.2;
    private const double MaxFontSize = 18.0;
    private const double MinFontSize = 11.0;

    private bool _scrolling; // pointer is hovering
    private bool _pointerDown;
    private double _baseOffsetY;

    public SoundPad()
    {
        InitializeComponent();

        TitleTextBlock.RenderTransform = MakeTranslateY(0);

        TitleTextBlock.SizeChanged += (_, _) => UpdateVerticalCentering();
        TitleTextBoxBounds.SizeChanged += (_, _) => UpdateVerticalCentering();

        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
    }


    private void OnPressed(object? s, PointerPressedEventArgs e) => _pointerDown = true;

    private void OnReleased(object? s, PointerReleasedEventArgs e)
    {
        var wasDown = _pointerDown;
        _pointerDown = false;
        StopIfOutside(e);

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
        StopIfOutside(e);
    }

    private void InputElement_OnPointerEntered(object? s, PointerEventArgs e)
    {
        if (_scrolling) return;

        _scrolling = true;
        StartScrolling();
    }

    private void InputElement_OnPointerExited(object? s, PointerEventArgs e)
    {
        if (_pointerDown) return;

        StopScrolling();
    }

    private void StopIfOutside(PointerEventArgs e)
    {
        if (!_scrolling) return;

        if (!new Rect(Bounds.Size).Contains(e.GetPosition(this)))
            StopScrolling();
    }

    private static bool AnyButtonDown(PointerEventArgs e)
    {
        var p = e.GetCurrentPoint(null).Properties;
        return p.IsLeftButtonPressed || p.IsRightButtonPressed || p.IsMiddleButtonPressed;
    }

    private void StartScrolling()
    {
        var available = TitleTextBoxBounds.Bounds.Height
                        - TitleTextBoxBounds.Padding.Top
                        - TitleTextBoxBounds.Padding.Bottom;
        var overflow = Math.Max(0, TitleTextBlock.Bounds.Height - available);

        if (overflow <= 0)
        {
            _scrolling = false;
            return;
        }

        var speed = GetLineHeight() * LinesPerSecond;

        AnimateTo(overflow, speed, CurrentY > 0.5 ? 0 : StartDelaySeconds);
    }

    private void StopScrolling()
    {
        _scrolling = false;
        AnimateTo(0, GetLineHeight() * LinesPerSecond * ReturnSpeedMultiplier, 0);
    }

    /// <summary>Current scroll distance in px (positive = scrolled down).</summary>
    private double CurrentY => _baseOffsetY - (TitleTextBlock.RenderTransform?.Value.M32 ?? _baseOffsetY);

    private void AnimateTo(double targetY, double pxPerSecond, double delay)
    {
        var distance = Math.Abs(targetY - CurrentY);

        if (distance < 0.5 || pxPerSecond <= 0)
        {
            TitleTextBlock.Transitions = null;
            TitleTextBlock.RenderTransform = MakeTranslateY(targetY);
            return;
        }

        TitleTextBlock.Transitions =
        [
            new TransformOperationsTransition
            {
                Property = Visual.RenderTransformProperty,
                Duration = TimeSpan.FromSeconds(distance / pxPerSecond),
                Delay = TimeSpan.FromSeconds(delay),
                Easing = new LinearEasing()
            }
        ];

        TitleTextBlock.RenderTransform = MakeTranslateY(targetY);
    }

    private TransformOperations MakeTranslateY(double scrolledY)
    {
        var builder = TransformOperations.CreateBuilder(1);
        builder.AppendTranslate(0, _baseOffsetY - scrolledY);
        return builder.Build();
    }

    private double GetLineHeight()
    {
        if (!double.IsNaN(TitleTextBlock.LineHeight) && TitleTextBlock.LineHeight > 0)
            return TitleTextBlock.LineHeight;

        var layout = TitleTextBlock.TextLayout;
        var count = layout.TextLines.Count;
        return count > 0 ? layout.Height / count : 18;
    }

    private void UpdateVerticalCentering()
    {
        if (_scrolling) return;

        var available = TitleTextBoxBounds.Bounds.Height
                        - TitleTextBoxBounds.Padding.Top
                        - TitleTextBoxBounds.Padding.Bottom;
        var textHeight = TitleTextBlock.Bounds.Height;

        if (available > 0 && textHeight > 0 && textHeight < available)
        {
            _baseOffsetY = Math.Round((available - textHeight) / 2.0);
        }
        else
        {
            _baseOffsetY = 0;
        }

        TitleTextBlock.Transitions = null;
        TitleTextBlock.RenderTransform = MakeTranslateY(0);
    }

    private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var available = TitleTextBoxBounds.Bounds.Height
                        - TitleTextBoxBounds.Padding.Top
                        - TitleTextBoxBounds.Padding.Bottom;

        if (available <= 10) return;

        var maxFontSize = this.TryFindResource("PadTitleMaxFontSize", out var maxRes) && maxRes is double maxVal ? maxVal : MaxFontSize;
        var minFontSize = this.TryFindResource("PadTitleMinFontSize", out var minRes) && minRes is double minVal ? minVal : MinFontSize;

        var lines = 2;
        var lineHeight = available / lines;
        var fontSize = lineHeight / FontRatio;

        while (fontSize > maxFontSize)
        {
            lines++;
            lineHeight = available / lines;
            fontSize = lineHeight / FontRatio;
        }

        if (fontSize < minFontSize)
        {
            fontSize = minFontSize;
            lineHeight = fontSize * FontRatio;
        }

        if (Math.Abs(fontSize - TitleTextBlock.FontSize) >= 0.01)
        {
            TitleTextBlock.FontSize = fontSize;
            TitleTextBlock.LineHeight = lineHeight;
        }

        UpdateVerticalCentering();
    }
}