using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Provider.Algorithm;

public static class FractalGenerator
{
    public class Complex(double r, double i)
    {
        public double R { get; set; } = r;
        public double I { get; set; } = i;

        public static Complex operator +(Complex a, Complex b)
        {
            return new Complex(a.R + b.R, a.I + b.I);
        }

        public static Complex operator *(Complex a, Complex b)
        {
            return new Complex(a.R * b.R - a.I * b.I, a.I * b.R + a.R * b.I);
        }

        public Complex Pow2()
        {
            return this * this;
        }

        public double Abs()
        {
            return Math.Sqrt(R * R + I * I);
        }
    }

    public static class JuliaFractal
    {
        private const int JuliaSetThreshold = 10;
        private const int JuliaSetMaxIter = 20;

        private static readonly Rgba32 EmptyColor = new(240, 240, 240, 255);

        public static string GenerateFractal(int imageSize, int quality, int seed)
        {
            Random random = new(seed);

            using var image = new Image<Rgba32>(imageSize, imageSize);
            double Offset() => -1 + random.NextDouble() * 2;
            var juliaSetOffset = new Complex(Offset(), Offset());

            var scale = new Complex(2.0 / imageSize, 0);

            var filledColor = GetRandomColor(random);

            for (int x = 0; x < imageSize; x++)
            {
                for (int y = 0; y < imageSize; y++)
                {
                    var pointInJuliaSet = new Complex(x - imageSize / 2, y - imageSize / 2) * scale;
                    var pointIntensity = EvaluateJuliaSetPoint(pointInJuliaSet, juliaSetOffset);

                    var color = InterpolateThreeColors(EmptyColor, filledColor, EmptyColor, pointIntensity);
                    image[x, y] = color;
                }
            }

            using MemoryStream ms = new();
            image.SaveAsWebp(
                ms,
                new SixLabors.ImageSharp.Formats.Webp.WebpEncoder { Quality = quality }
            );

            string base64WebP = Convert.ToBase64String(ms.ToArray());

            string dataUrl = $"data:image/webp;base64,{base64WebP}";

            return dataUrl;
        }

        private static double EvaluateJuliaSetPoint(Complex point, Complex offset)
        {
            var res = point;
            int i = 0;

            while (res.Abs() < JuliaSetThreshold && i < JuliaSetMaxIter)
            {
                res = res.Pow2() + offset;
                i++;
            }
            return (double)i / JuliaSetMaxIter;
        }

        private static Rgba32 GetRandomColor(Random random)
        {
            byte r = (byte)random.Next(70, 230);
            byte g = (byte)random.Next(70, 230);
            byte b = (byte)random.Next(70, 230);
            return new Rgba32(r, g, b, 255);
        }

        private static Rgba32 InterpolateThreeColors(Rgba32 colorA, Rgba32 colorB, Rgba32 colorC, double interpolator) =>
            interpolator < 0.5
                ? InterpolateTwoColors(colorA, colorB, interpolator * 2)
                : InterpolateTwoColors(colorB, colorC, (interpolator - 0.5) * 2);

        private static Rgba32 InterpolateTwoColors(Rgba32 colorA, Rgba32 colorB, double interpolator)
        {
            byte r = (byte)(colorA.R + (colorB.R - colorA.R) * interpolator);
            byte g = (byte)(colorA.G + (colorB.G - colorA.G) * interpolator);
            byte b = (byte)(colorA.B + (colorB.B - colorA.B) * interpolator);
            return new Rgba32(r, g, b, 255);
        }
    }
}
