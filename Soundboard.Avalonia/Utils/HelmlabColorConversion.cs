// Ported from Helmlab (c) Görkem YILDIZ, MIT License
// https://github.com/Grkmyldz148/helmlab

using Avalonia.Media;
using Avalonia.Media.Immutable;
using System;
using System.Collections.Generic;

namespace Soundboard.Avalonia.Utils;

internal static class HelmlabColorConversion
{
    private static readonly Dictionary<Color, ImmutableSolidColorBrushPair> Cache = new();
    
    // =========================================================
    // Public Static API
    // =========================================================
    
    /// <summary>
    /// Takes any arbitrary input color, normalizes it to a perceptual 500 base,
    /// and generates the dual-mode 400 (Dark Mode) and 600 (Light Mode) brushes.
    /// </summary>
    public static ImmutableSolidColorBrushPair GetDualModeBrushes(Color color)
    {
        if (Cache.TryGetValue(color, out var brushes))
            return brushes;

        var (dark, light) = GetDualModeColors(color);
        var result = new ImmutableSolidColorBrushPair(new ImmutableSolidColorBrush(light), new ImmutableSolidColorBrush(dark));
        Cache[color] = result;
        return result;
    }

    /// <summary>
    /// Generates the dual-mode (400 Dark, 600 Light) Color pair for an arbitrary color.
    /// </summary>
    public static (Color Dark, Color Light) GetDualModeColors(Color color)
    {
        var lab = new LabColor(color);
        var base500 = ToPerceptual500(lab);

        var darkLab = Scale(base500, 400);
        var lightLab = Scale(base500, 600);

        return (darkLab.ToColor(), lightLab.ToColor());
    }

    /// <summary>
    /// Normalizes any color to the "cusp" (maximum perceptual chroma) for its hue.
    /// </summary>
    public static LabColor ToPerceptual500(LabColor lab, double chromaFactor = 0.82)
    {
        double c = Math.Sqrt(lab.A * lab.A + lab.B * lab.B);
        // Neutral gray safeguard
        if (c < 0.02)
        {
            return new LabColor(0.55, 0.0, 0.0);
        }

        double h = Math.Atan2(lab.B, lab.A);
        var (cuspL, cuspC) = GetCusp(h);
        double targetC = cuspC * chromaFactor;

        return new LabColor(cuspL, targetC * Math.Cos(h), targetC * Math.Sin(h));
    }

    /// <summary>
    /// Interpolates lightness in GenLab space according to Helmlab scale levels (50–950).
    /// </summary>
    public static LabColor Scale(LabColor base500, int level)
    {
        const double L_light = 0.99;
        const double L_dark = 0.05;

        double targetL = level <= 500
            ? L_light + (level / 500.0) * (base500.L - L_light)
            : base500.L + ((level - 500) / 450.0) * (L_dark - base500.L);

        return new LabColor(targetL, base500.A, base500.B);
    }

    /// <summary>
    /// Evaluates the sRGB boundary cusp (L_cusp, C_cusp) for a given hue in radians.
    /// </summary>
    public static (double L, double C) GetCusp(double hueRadians)
    {
        double deg = ((hueRadians * 180.0 / Math.PI) % 360.0 + 360.0) % 360.0;
        int idx = (int)Math.Clamp(Math.Round(deg), 0, 359);
        return CuspTable[idx];
    }

    // =========================================================
    // LinearColor (sRGB gamma expansion & compression)
    // =========================================================

    internal readonly struct LinearColor
    {
        internal double R { get; init; }
        internal double G { get; init; }
        internal double B { get; init; }

        internal LinearColor(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

        internal LinearColor(Color color)
        {
            R = FromGammaByte(color.R);
            G = FromGammaByte(color.G);
            B = FromGammaByte(color.B);
        }

        internal Color ToColor()
        {
            return Color.FromRgb(ToGammaByte(R), ToGammaByte(G), ToGammaByte(B));
        }

        private static double FromGammaByte(byte channel)
        {
            double srgb = channel / 255.0;
            return srgb <= 0.04045
                ? srgb / 12.92
                : Math.Pow((srgb + 0.055) / 1.055, 2.4);
        }

        private static byte ToGammaByte(double linear)
        {
            linear = Math.Clamp(linear, 0.0, 1.0);
            double srgb = linear <= 0.0031308
                ? linear * 12.92
                : 1.055 * Math.Pow(linear, 1.0 / 2.4) - 0.055;
            return (byte)Math.Clamp(Math.Round(srgb * 255.0), 0, 255);
        }
    }

    // =========================================================
    // XYZColor (CIE 1931 D65)
    // =========================================================

    internal readonly struct XYZColor
    {
        internal double X { get; init; }
        internal double Y { get; init; }
        internal double Z { get; init; }

        internal XYZColor(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        internal XYZColor(Color color) : this(new LinearColor(color)) { }

        internal XYZColor(LinearColor linear)
        {
            X = 0.4124564390896922 * linear.R + 0.3575760776439511 * linear.G + 0.1804374832663989 * linear.B;
            Y = 0.2126728514056226 * linear.R + 0.7151521552878178 * linear.G + 0.0721750036064596 * linear.B;
            Z = 0.0193338955823293 * linear.R + 0.1191920258813418 * linear.G + 0.9503040785363679 * linear.B;
        }

        internal LinearColor ToLinearColor()
        {
            double r = 3.2404542 * X - 1.5371385 * Y - 0.4985314 * Z;
            double g = -0.9692660 * X + 1.8760108 * Y + 0.0415560 * Z;
            double b = 0.0556434 * X - 0.2040259 * Y + 1.0572252 * Z;
            return new LinearColor(r, g, b);
        }

        internal Color ToColor() => ToLinearColor().ToColor();
    }

    // =========================================================
    // LabColor (Helmlab GenLab Perceptual Space)
    // =========================================================

    internal class LabColor
    {
        internal double L { get; init; }
        internal double A { get; init; }
        internal double B { get; init; }

        public LabColor(double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }

        public LabColor(Color color) : this(new XYZColor(color)) { }
        public LabColor(LinearColor color) : this(new XYZColor(color)) { }

        public LabColor(XYZColor color)
        {
            // 1. XYZ -> LMS (M1 matrix)
            double lLms = Math.Max(0.0, 0.8154374735648701 * color.X + 0.3603221491264266 * color.Y - 0.12432703417946676 * color.Z);
            double mLms = Math.Max(0.0, 0.03298391207546648 * color.X + 0.9292940788255503 * color.Y + 0.03614494665290377 * color.Z);
            double sLms = Math.Max(0.0, 0.048184113668356454 * color.X + 0.26427748135788043 * color.Y + 0.6336388271114471 * color.Z);

            // 2. Transfer function: depcubic
            double lc = DepCubicFwd(lLms);
            double mc = DepCubicFwd(mLms);
            double sc = DepCubicFwd(sLms);

            // 3. LMS -> Raw Lab (M2 matrix)
            double rawL = 0.21193779684470104 * lc + 0.7992121834263127 * mc - 0.00410075161564345 * sc;
            double rawA = 2.4672018828033475 * lc - 2.9877348024830788 * mc + 0.520532919679731 * sc;
            double rawB = -0.11390787868068575 * lc + 1.3932982808117473 * mc - 1.279390402131062 * sc;

            // 4. Piecewise Lightness correction (PW L)
            double labL = PiecewiseLFwd(rawL);

            // 5. L-gated hue enrichment (blue-violet rotation)
            double c = Math.Sqrt(rawA * rawA + rawB * rawB);
            double h = Math.Atan2(rawB, rawA);
            const double enrCenter = 4.616395871525001; // 264.5 deg

            double tGate = Math.Clamp((labL - 0.37) / (1.0 - 0.37), 0.0, 1.0);
            double gate = Math.Pow(Math.Sin(Math.PI * tGate), 2);
            double dh = (h - enrCenter + Math.PI) % (2.0 * Math.PI) - Math.PI;
            double gauss = Math.Exp(-0.5 * Math.Pow(dh / 0.7, 2));
            double hNew = h + 0.058 * gate * gauss;

            double aEnr = c * Math.Cos(hNew);
            double bEnr = c * Math.Sin(hNew);

            // 6. Sublinear chroma compression (power = 0.978)
            double cEnr = Math.Sqrt(aEnr * aEnr + bEnr * bEnr + 1e-30);
            double scale = Math.Pow(cEnr, 0.978 - 1.0);

            L = labL;
            A = aEnr * scale;
            B = bEnr * scale;
        }

        public XYZColor ToXYZ()
        {
            // 1. Undo chroma power
            double c = Math.Sqrt(A * A + B * B + 1e-30);
            double cRaw = Math.Pow(c, 1.0 / 0.978);
            double scale = cRaw / c;
            double aCp = A * scale;
            double bCp = B * scale;

            // 2. Undo enrichment (Halley iteration)
            double cEnr = Math.Sqrt(aCp * aCp + bCp * bCp);
            double rawA = aCp;
            double rawB = bCp;
            if (cEnr >= 1e-12)
            {
                double hTarget = Math.Atan2(bCp, aCp);
                double tGate = Math.Clamp((L - 0.37) / (1.0 - 0.37), 0.0, 1.0);
                double gate = Math.Pow(Math.Sin(Math.PI * tGate), 2);
                const double sig2 = 0.7 * 0.7;
                const double enrCenter = 4.616395871525001;
                double h = hTarget;

                for (int i = 0; i < 8; i++)
                {
                    double dh = (h - enrCenter + Math.PI) % (2.0 * Math.PI) - Math.PI;
                    double gauss = Math.Exp(-0.5 * (dh * dh / sig2));
                    double ag = 0.058 * gate;
                    double f = h + ag * gauss - hTarget;
                    double fp = 1.0 + ag * gauss * (-dh / sig2);
                    double fpp = ag * gauss * (-1.0 / sig2 + (dh * dh) / (sig2 * sig2));
                    double denom = 2.0 * fp * fp - f * fpp;
                    if (Math.Abs(denom) < 1e-30) denom = 1.0;
                    h -= 2.0 * f * fp / denom;
                }
                rawA = cEnr * Math.Cos(h);
                rawB = cEnr * Math.Sin(h);
            }

            // 3. Undo PW L
            double rawL = PiecewiseLInv(L);

            // 4. Lab -> LMS (M2 inverse)
            double lc = rawL * 0.9930001151336144 + rawA * 0.3259932725305229 + rawB * 0.12945085631713896;
            double mc = rawL * 0.9930001151336143 - rawA * 0.08708353111074632 - rawB * 0.038613617430049534;
            double sc = rawL * 0.993000115133614 - rawA * 0.12386097008215027 - rawB * 0.8351991365871065;

            // 5. Undo depcubic
            double lLms = DepCubicInv(lc);
            double mLms = DepCubicInv(mc);
            double sLms = DepCubicInv(sc);

            // 6. LMS -> XYZ (M1 inverse)
            double x = lLms * 1.2326502723725545 - mLms * 0.5557414732179251 + sLms * 0.27356120084537067;
            double y = lLms * -0.040766587876246804 + mLms * 1.1122097325799587 - sLms * 0.07144314470371181;
            double z = lLms * -0.07673215022426164 - mLms * 0.42161885465584426 + sLms * 1.5871810048801056;

            return new XYZColor(x, y, z);
        }

        public Color ToColor() => ToXYZ().ToColor();

        private static double DepCubicFwd(double x, double alpha = 0.021)
        {
            double s = Math.Sqrt(alpha / 3.0);
            double t = x / (2.0 * s * s * s);
            double y = 2.0 * s * Math.Sinh(Math.Asinh(t) / 3.0);

            double f = y * y * y + alpha * y - x;
            double fp = 3.0 * y * y + alpha;
            double fpp = 6.0 * y;
            double denom = 2.0 * fp * fp - f * fpp;
            if (Math.Abs(denom) > 1e-30)
            {
                y -= 2.0 * f * fp / denom;
            }
            return y;
        }

        private static double DepCubicInv(double y, double alpha = 0.021) => y * y * y + alpha * y;

        private static readonly double[] PwIn =
        [
            0.0, 0.05, 0.1, 0.15, 0.2, 0.25, 0.3, 0.35, 0.4, 0.45,
            0.5, 0.55, 0.6, 0.65, 0.7, 0.75, 0.8, 0.85, 0.9, 0.95, 1.0
        ];

        private static readonly double[] PwOut =
        [
            0.0, 0.0094940135, 0.0256456984, 0.0552596617, 0.1057490153,
            0.1605585332, 0.2140596489, 0.2678623051, 0.3220435246, 0.3739052099,
            0.4302099778, 0.4835465162, 0.5399824670, 0.5956710081, 0.6542161666,
            0.7115380217, 0.7702762413, 0.8293313468, 0.8894063862, 0.9462829573, 1.0
        ];

        private static double PiecewiseLFwd(double L)
        {
            if (L <= 0.0) return 0.0;
            if (L >= 1.0) return 1.0;
            int idx = 0;
            while (idx < PwIn.Length - 2 && PwIn[idx + 1] < L) idx++;
            double t = (L - PwIn[idx]) / (PwIn[idx + 1] - PwIn[idx]);
            return PwOut[idx] + t * (PwOut[idx + 1] - PwOut[idx]);
        }

        private static double PiecewiseLInv(double L)
        {
            if (L <= PwOut[0]) return PwIn[0];
            if (L >= PwOut[^1]) return PwIn[^1];
            int idx = 0;
            while (idx < PwOut.Length - 2 && PwOut[idx + 1] < L) idx++;
            double t = (L - PwOut[idx]) / (PwOut[idx + 1] - PwOut[idx]);
            return PwIn[idx] + t * (PwIn[idx + 1] - PwIn[idx]);
        }
    }

    // =========================================================
    // Cusp Table (360 degrees: L_cusp, C_cusp)
    // =========================================================

    private static readonly (double L, double C)[] CuspTable = BuildCuspTable();

    private static (double L, double C)[] BuildCuspTable()
    {
        var table = new (double L, double C)[360];
        for (int i = 0; i < 360; i++)
        {
            double rad = i * Math.PI / 180.0;
            // Analytical fit of sRGB gamut cusp across 360 hues
            double l = 0.62 + 0.22 * Math.Sin(rad - 0.2) + 0.08 * Math.Sin(2.0 * rad - 1.2);
            double c = 0.35 + 0.10 * Math.Cos(rad - 1.0);
            table[i] = (Math.Clamp(l, 0.38, 0.88), Math.Clamp(c, 0.20, 0.48));
        }
        return table;
    }
}

public readonly record struct ImmutableSolidColorBrushPair
{
    public ImmutableSolidColorBrush Light { get; init; }
    public ImmutableSolidColorBrush Dark { get; init; }

    public ImmutableSolidColorBrushPair(ImmutableSolidColorBrush light, ImmutableSolidColorBrush dark)
    {
        Light = light;
        Dark = dark;
    }

    public ImmutableSolidColorBrushPair(Color color)
    {
        this = HelmlabColorConversion.GetDualModeBrushes(color);
    }
}