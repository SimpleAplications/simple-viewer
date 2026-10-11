using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace ReiksViewer;

/// <summary>Brightness, contrast, saturation, gamma and warmth, done by the pixel shader in Adjust.fx.</summary>
public sealed class AdjustEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty =
        RegisterPixelShaderSamplerProperty("Input", typeof(AdjustEffect), 0);

    public static readonly DependencyProperty BrightnessProperty = Constant(nameof(Brightness), 0, 0.0);
    public static readonly DependencyProperty ContrastProperty = Constant(nameof(Contrast), 1, 1.0);
    public static readonly DependencyProperty SaturationProperty = Constant(nameof(Saturation), 2, 1.0);
    public static readonly DependencyProperty GammaProperty = Constant(nameof(Gamma), 3, 1.0);
    public static readonly DependencyProperty WarmthProperty = Constant(nameof(Warmth), 4, 0.0);

    static DependencyProperty Constant(string name, int register, double neutral) =>
        DependencyProperty.Register(name, typeof(double), typeof(AdjustEffect),
                                    new UIPropertyMetadata(neutral, PixelShaderConstantCallback(register)));

    public AdjustEffect()
    {
        var shader = new PixelShader();
        using (var stream = typeof(AdjustEffect).Assembly.GetManifestResourceStream("Adjust.ps"))
            shader.SetStreamSource(stream);
        PixelShader = shader;
        UpdateShaderValue(InputProperty);
        UpdateShaderValue(BrightnessProperty);
        UpdateShaderValue(ContrastProperty);
        UpdateShaderValue(SaturationProperty);
        UpdateShaderValue(GammaProperty);
        UpdateShaderValue(WarmthProperty);
    }

    public Brush Input { get => (Brush)GetValue(InputProperty); set => SetValue(InputProperty, value); }
    public double Brightness { get => (double)GetValue(BrightnessProperty); set => SetValue(BrightnessProperty, value); }
    public double Contrast { get => (double)GetValue(ContrastProperty); set => SetValue(ContrastProperty, value); }
    public double Saturation { get => (double)GetValue(SaturationProperty); set => SetValue(SaturationProperty, value); }
    public double Gamma { get => (double)GetValue(GammaProperty); set => SetValue(GammaProperty, value); }
    public double Warmth { get => (double)GetValue(WarmthProperty); set => SetValue(WarmthProperty, value); }

    public bool IsNeutral => Brightness == 0 && Contrast == 1 && Saturation == 1 && Gamma == 1 && Warmth == 0;
}
