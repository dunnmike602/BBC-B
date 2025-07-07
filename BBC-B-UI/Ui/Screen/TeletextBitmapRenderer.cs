namespace MLDComputing.Emulators.BeebBox.Ui.Screen;

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class TeletextBitmapRenderer
{
    private const int Columns = 40;
    private const int Rows = 25;
    private const int CharWidth = 12; // 6x2 scaling
    private const int CharHeight = 20; // 10x2 scaling
    private const int GlyphWidth = 12; // 12 pixels wide for 16-bit glyphs
    private const int GlyphHeight = 20; // 20 rows

    private readonly ushort[,,] _font = new ushort[3, 96, 20]; // [bank, char, row]

    public TeletextBitmapRenderer(string fontPath)
    {
        LoadFontFile(fontPath);
    }

    public WriteableBitmap Bitmap { get; } = new(
        Columns * CharWidth,
        Rows * CharHeight,
        96, 96,
        PixelFormats.Bgr32,
        null);

    private void LoadFontFile(string fontPath)
    {
        using var fs = File.OpenRead(fontPath);

        for (var character = 32; character <= 127; character++)
        {
            var charIndex = character - 32;

            // First two lines are blank
            _font[0, charIndex, 0] = 0;
            _font[0, charIndex, 1] = 0;
            _font[1, charIndex, 0] = 0;
            _font[1, charIndex, 1] = 0;
            _font[2, charIndex, 0] = 0;
            _font[2, charIndex, 1] = 0;

            for (var y = 2; y < 20; y++)
            {
                var lo = fs.ReadByte();
                var hi = fs.ReadByte();
                if (lo == -1 || hi == -1)
                {
                    throw new EndOfStreamException("Unexpected EOF");
                }

                var bitmap = (ushort)(lo | (hi << 8));
                _font[0, charIndex, y] = bitmap;
                _font[1, charIndex, y] = bitmap;
                _font[2, charIndex, y] = bitmap;
            }
        }

        for (var character = 0; character < 96; character++)
        {
            if ((character & 32) == 0)
            {
                var row1 = 0;
                var row2 = 0;
                var row3 = 0;

                if ((character & 0x01) != 0)
                {
                    row1 |= 0xFC0;
                }

                if ((character & 0x02) != 0)
                {
                    row1 |= 0x03F;
                }

                if ((character & 0x04) != 0)
                {
                    row2 |= 0xFC0;
                }

                if ((character & 0x08) != 0)
                {
                    row2 |= 0x03F;
                }

                if ((character & 0x10) != 0)
                {
                    row3 |= 0xFC0;
                }

                if ((character & 0x40) != 0)
                {
                    row3 |= 0x03F;
                }

                for (var y = 0; y < 6; y++)
                {
                    _font[1, character, y] = (ushort)row1;
                }

                for (var y = 6; y < 14; y++)
                {
                    _font[1, character, y] = (ushort)row2;
                }

                for (var y = 14; y < 20; y++)
                {
                    _font[1, character, y] = (ushort)row3;
                }

                row1 &= 0x3CF;
                row2 &= 0x3CF;
                row3 &= 0x3CF;

                for (var y = 0; y < 5; y++)
                {
                    _font[2, character, y] = (ushort)row1;
                }

                for (var y = 5; y < 13; y++)
                {
                    _font[2, character, y] = (ushort)row2;
                }

                for (var y = 13; y < 19; y++)
                {
                    _font[2, character, y] = (ushort)row3;
                }
            }
        }
    }

    public void Render(byte[] screenBuffer, int fontBank = 0)
    {
        Bitmap.Lock();

        var pBackBuffer = Bitmap.BackBuffer;
        var stride = Bitmap.BackBufferStride;

        unsafe
        {
            var clearSpan = new Span<byte>((void*)pBackBuffer, Bitmap.PixelHeight * stride);
            clearSpan.Clear(); // sets all bytes to 0 (black)
        }

        for (var row = 0; row < Rows; row++)
        {
            for (var col = 0; col < Columns; col++)
            {
                var ch = screenBuffer[row * Columns + col];
                if (ch is < 32 or > 127)
                {
                    continue;
                }

                RenderGlyph(pBackBuffer, stride, ch, col * CharWidth, row * CharHeight, fontBank);
            }
        }

        Bitmap.AddDirtyRect(new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight));
        Bitmap.Unlock();
    }

    private void RenderGlyph(IntPtr buffer, int stride, byte ch, int x, int y, int fontBank)
    {
        var glyphIndex = ch - 32;
        for (var row = 0; row < GlyphHeight; row++)
        {
            var rowBits = _font[fontBank, glyphIndex, row];
            for (var bit = 0; bit < GlyphWidth; bit++)
            {
                if ((rowBits & (1 << (11 - bit))) != 0)
                {
                    var px = x + bit;
                    var py = y + row;
                    SetPixel(buffer, stride, px, py, Colors.White);
                }
            }
        }
    }

    private unsafe void SetPixel(IntPtr buffer, int stride, int x, int y, Color color)
    {
        var pixelOffset = y * stride + x * 4;
        var pixel = (byte*)buffer + pixelOffset;
        pixel[0] = color.B;
        pixel[1] = color.G;
        pixel[2] = color.R;
        pixel[3] = 255;
    }
}