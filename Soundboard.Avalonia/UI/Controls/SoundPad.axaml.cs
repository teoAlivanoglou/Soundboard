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

    private static readonly TransformOperations ZeroTranslate = CreateTranslate(0);
    private static double? _cachedMaxFontSize;
    private static double? _cachedMinFontSize;

    private bool _scrolling; // pointer is hovering
    private bool _pointerDown;
    private double _baseOffsetY;

    public SoundPad()
    {
        InitializeComponent();

        TitleTextBlock.RenderTransform = ZeroTranslate;

        TitleTextBlock.SizeChanged += (_, _) => UpdateVerticalCentering();
        TitleTextBoxBounds.SizeChanged += (_, _) => UpdateVerticalCentering();

        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _scrolling = false;
        _pointerDown = false;
        UpdateVerticalCentering();
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

    private static TransformOperations CreateTranslate(double y)
    {
        var builder = TransformOperations.CreateBuilder(1);
        builder.AppendTranslate(0, y);
        return builder.Build();
    }

    private TransformOperations MakeTranslateY(double scrolledY)
    {
        var targetY = _baseOffsetY - scrolledY;
        if (Math.Abs(targetY) < 0.01)
            return ZeroTranslate;

        return CreateTranslate(targetY);
    }

    private double GetLineHeight()
    {
        if (!double.IsNaN(TitleTextBlock.LineHeight) && TitleTextBlock.LineHeight > 0)
            return TitleTextBlock.LineHeight;

        return TitleTextBlock.FontSize * FontRatio;
    }

    private void UpdateVerticalCentering()
    {
        if (_scrolling) return;

        var available = TitleTextBoxBounds.Bounds.Height
                        - TitleTextBoxBounds.Padding.Top
                        - TitleTextBoxBounds.Padding.Bottom;
        var textHeight = TitleTextBlock.Bounds.Height;

        var newOffsetY = (available > 0 && textHeight > 0 && textHeight < available)
            ? Math.Round((available - textHeight) / 2.0)
            : 0;

        // Skip re-applying transform if offset didn't change and no transition is running
        if (Math.Abs(_baseOffsetY - newOffsetY) < 0.5 && TitleTextBlock.RenderTransform != null && TitleTextBlock.Transitions == null)
            return;

        _baseOffsetY = newOffsetY;
        TitleTextBlock.Transitions = null;
        TitleTextBlock.RenderTransform = MakeTranslateY(0);
    }

    private static double GetResourceFontSize(string key, double fallback)
    {
        if (Application.Current is not null && Application.Current.TryFindResource(key, out var res) && res is double d)
            return d;
        return fallback;
    }

    private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Skip recalculation if height didn't change
        if (Math.Abs(e.PreviousSize.Height - e.NewSize.Height) < 0.5 && e.PreviousSize.Height > 0)
            return;

        var available = TitleTextBoxBounds.Bounds.Height
                        - TitleTextBoxBounds.Padding.Top
                        - TitleTextBoxBounds.Padding.Bottom;

        if (available <= 10) return;

        _cachedMaxFontSize ??= GetResourceFontSize("PadTitleMaxFontSize", MaxFontSize);
        _cachedMinFontSize ??= GetResourceFontSize("PadTitleMinFontSize", MinFontSize);

        var maxFontSize = _cachedMaxFontSize.Value;
        var minFontSize = _cachedMinFontSize.Value;

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
            // Setting FontSize/LineHeight causes TitleTextBlock.SizeChanged to fire,
            // which will call UpdateVerticalCentering automatically.
        }
        else
        {
            UpdateVerticalCentering();
        }
    }
}