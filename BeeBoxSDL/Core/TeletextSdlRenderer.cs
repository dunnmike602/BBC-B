namespace BeeBoxSDL.Core;

using SDL2;

public class TeletextSdlRenderer : IDisposable
{
    private const int Columns = 40;
    private const int Rows = 25;
    private const int CharWidth = 12;
    private const int CharHeight = 20;
    private const int GlyphWidth = 12;
    private const int GlyphHeight = 20;

    private readonly ushort[,,] _font = new ushort[3, 96, 20];
    private IntPtr _renderer;
    private IntPtr _texture;
    private int _textureHeight;

    public void Dispose()
    {
        SDL.SDL_DestroyRenderer(_renderer);
        SDL.SDL_DestroyWindow(_renderer);
        SDL.SDL_Quit();
    }

    private void LoadFontFile(string fontPath)
    {
        using var fs = File.OpenRead(fontPath);

        for (var character = 32; character <= 127; character++)
        {
            var charIndex = character - 32;
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
        SDL.SDL_LockTexture(_texture, IntPtr.Zero, out var pixels, out var pitch);
        unsafe
        {
            var span = new Span<byte>((void*)pixels, _textureHeight * pitch);
            span.Clear();

            for (var row = 0; row < Rows; row++)
            {
                for (var col = 0; col < Columns; col++)
                {
                    var ch = screenBuffer[row * Columns + col];
                    if (ch is < 32 or > 127)
                    {
                        continue;
                    }

                    RenderGlyph((byte*)pixels, pitch, ch, col * CharWidth, row * CharHeight, fontBank);
                }
            }
        }

        SDL.SDL_UnlockTexture(_texture);
        SDL.SDL_RenderClear(_renderer);
        SDL.SDL_RenderCopy(_renderer, _texture, IntPtr.Zero, IntPtr.Zero);
        SDL.SDL_RenderPresent(_renderer);
    }

    private unsafe void RenderGlyph(byte* buffer, int pitch, byte ch, int x, int y, int fontBank)
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
                    var offset = py * pitch + px * 4;
                    buffer[offset + 0] = 255; // B
                    buffer[offset + 1] = 255; // G
                    buffer[offset + 2] = 255; // R
                    buffer[offset + 3] = 255; // A
                }
            }
        }
    }

    public void Create(string fontPath)
    {
        LoadFontFile(fontPath);

        SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
        var window = SDL.SDL_CreateWindow("BeeBox", SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED, Columns * CharWidth, Rows * CharHeight, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
        _renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

        var textureWidth = Columns * CharWidth;
        _textureHeight = Rows * CharHeight;
        _texture = SDL.SDL_CreateTexture(_renderer, SDL.SDL_PIXELFORMAT_ARGB8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, textureWidth, _textureHeight);
    }
}