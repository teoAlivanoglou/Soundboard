using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Soundboard.UI;

public class MaskColorLerpEffect : ShaderEffect
{
    // private static readonly PixelShader _pixelShader = new PixelShader() { UriSource = MakePackUri("UI/MaskColorEffect.fx.ps") };


    public MaskColorLerpEffect()
    {
        PixelShader = new PixelShader { UriSource = MakePackUri("UI/MaskColorLerpEffect.fx.ps") };
        // BlurEffect e;
        UpdateShaderValue(InputProperty);
        UpdateShaderValue(AdjustBlackProperty);
        UpdateShaderValue(AdjustWhiteProperty);
        UpdateShaderValue(ColorWhiteProperty);
        UpdateShaderValue(ColorWhiteProperty);
        UpdateShaderValue(MaskProperty);
    }

    // MakePackUri is a utility method for computing a pack uri
    // for the given resource. 
    public static Uri MakePackUri(string relativeFile)
    {
        var a = typeof(MaskColorLerpEffect).Assembly;

        // Extract the short name.
        var assemblyShortName = a.ToString().Split(',')[0];

        var uriString = "pack://application:,,,/" +
                        assemblyShortName +
                        ";component/" +
                        relativeFile;

        return new Uri(uriString);
    }


    ///////////////////////////////////////////////////////////////////////

    #region Input dependency property

    public Brush Input
    {
        get => (Brush)GetValue(InputProperty);
        set
        {
            SetValue(InputProperty, value);
            UpdateShaderValue(InputProperty);
        }
    }

    public static readonly DependencyProperty InputProperty =
        ShaderEffect.RegisterPixelShaderSamplerProperty(nameof(Input), typeof(MaskColorLerpEffect), 0);

    #endregion


    ///////////////////////////////////////////////////////////////////////

    #region ColorBlack dependency property

    public Color ColorBlack
    {
        get => (Color)GetValue(ColorBlackProperty);
        set
        {
            SetValue(ColorBlackProperty, value);
            UpdateShaderValue(ColorBlackProperty);
        }
    }

    public static readonly DependencyProperty ColorBlackProperty =
        DependencyProperty.Register(nameof(ColorBlack), typeof(Color), typeof(MaskColorLerpEffect),
            new UIPropertyMetadata(Colors.Black, PixelShaderConstantCallback(0)));

    // private static void OnShaderValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    // {
    //     if (d is ShaderEffect effect)
    //     {
    //         effect.UpdateShaderValue(e.Property);
    //     }
    // }

    #endregion

    ///////////////////////////////////////////////////////////////////////

    #region ColorWhite dependency property

    public Color ColorWhite
    {
        get => (Color)GetValue(ColorWhiteProperty);
        set
        {
            SetValue(ColorWhiteProperty, value);
            UpdateShaderValue(ColorWhiteProperty);
        }
    }

    public static readonly DependencyProperty ColorWhiteProperty =
        DependencyProperty.Register(nameof(ColorWhite), typeof(Color), typeof(MaskColorLerpEffect),
            new UIPropertyMetadata(Colors.White, PixelShaderConstantCallback(1)));

    #endregion


    ///////////////////////////////////////////////////////////////////////

    #region AdjustBlack dependency property

    public double AdjustBlack
    {
        get => (double)GetValue(AdjustBlackProperty);
        set
        {
            SetValue(AdjustBlackProperty, value);
            UpdateShaderValue(AdjustBlackProperty);
        }
    }

    public static readonly DependencyProperty AdjustBlackProperty =
        DependencyProperty.Register(nameof(AdjustBlack), typeof(double), typeof(MaskColorLerpEffect),
            new UIPropertyMetadata(0.0, PixelShaderConstantCallback(2)));

    #endregion


    ///////////////////////////////////////////////////////////////////////

    #region AdjustWhite dependency property

    public double AdjustWhite
    {
        get => (double)GetValue(AdjustWhiteProperty);
        set
        {
            SetValue(AdjustWhiteProperty, value);
            UpdateShaderValue(AdjustWhiteProperty);
        }
    }

    public static readonly DependencyProperty AdjustWhiteProperty =
        DependencyProperty.Register(nameof(AdjustWhite), typeof(double), typeof(MaskColorLerpEffect),
            new UIPropertyMetadata(1.0, PixelShaderConstantCallback(3)));

    #endregion


    public Brush Mask
    {
        get => (Brush)GetValue(MaskProperty);
        set
        {
            SetValue(MaskProperty, value);
            UpdateShaderValue(MaskProperty);
        }
    }


    public static readonly DependencyProperty MaskProperty =
        DependencyProperty.Register(nameof(Mask), typeof(Brush), typeof(MaskColorLerpEffect),
            new UIPropertyMetadata(null, PixelShaderSamplerCallback(1)));
}

public class ThresholdEffect : ShaderEffect
{
    public ThresholdEffect()
    {
        PixelShader = new PixelShader() { UriSource = MakePackUri("UI/ThresholdEffect2.fx.ps") };

        UpdateShaderValue(InputProperty);
        UpdateShaderValue(ThresholdProperty);
        UpdateShaderValue(BlankColorProperty);
    }

    // MakePackUri is a utility method for computing a pack uri
    // for the given resource. 
    public static Uri MakePackUri(string relativeFile)
    {
        var a = typeof(ThresholdEffect).Assembly;

        // Extract the short name.
        var assemblyShortName = a.ToString().Split(',')[0];

        var uriString = "pack://application:,,,/" +
                        assemblyShortName +
                        ";component/" +
                        relativeFile;

        return new Uri(uriString);
    }

    ///////////////////////////////////////////////////////////////////////

    #region Input dependency property

    public Brush Input
    {
        get => (Brush)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    public static readonly DependencyProperty InputProperty =
        RegisterPixelShaderSamplerProperty("Input", typeof(ThresholdEffect), 0);

    #endregion

    ///////////////////////////////////////////////////////////////////////

    #region Threshold dependency property

    public double Threshold
    {
        get => (double)GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    public static readonly DependencyProperty ThresholdProperty =
        DependencyProperty.Register(nameof(Threshold), typeof(double), typeof(ThresholdEffect),
            new UIPropertyMetadata(0.5, PixelShaderConstantCallback(0)));

    #endregion

    ///////////////////////////////////////////////////////////////////////

    #region BlankColor dependency property

    public Color BlankColor
    {
        get => (Color)GetValue(BlankColorProperty);
        set => SetValue(BlankColorProperty, value);
    }

    public static readonly DependencyProperty BlankColorProperty =
        DependencyProperty.Register(nameof(BlankColor), typeof(Color), typeof(ThresholdEffect),
            new UIPropertyMetadata(Colors.Transparent, PixelShaderConstantCallback(1)));

    #endregion
}