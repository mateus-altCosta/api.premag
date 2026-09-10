namespace Premag.Core;

/// <summary>JPEG: assinatura e SOF (largura/altura) sem biblioteca de imagem.</summary>
public static class JpegInfo
{
    public const int TetoBytes = 300 * 1024;

    public static bool EhJpeg(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

    public static (int Largura, int Altura) Dimensoes(ReadOnlySpan<byte> jpeg)
    {
        var i = 2;
        while (i + 8 < jpeg.Length)
        {
            if (jpeg[i] != 0xFF)
            {
                i++;
                continue;
            }

            var marcador = jpeg[i + 1];
            if (marcador == 0xD8 || marcador == 0xD9 || marcador is >= 0xD0 and <= 0xD7)
            {
                i += 2;
                continue;
            }

            if (i + 3 >= jpeg.Length)
                break;
            var len = (jpeg[i + 2] << 8) | jpeg[i + 3];
            if (marcador is 0xC0 or 0xC1 or 0xC2)
            {
                var altura = (jpeg[i + 5] << 8) | jpeg[i + 6];
                var largura = (jpeg[i + 7] << 8) | jpeg[i + 8];
                return (largura, altura);
            }

            i += 2 + Math.Max(0, len);
        }

        return (0, 0);
    }
}
