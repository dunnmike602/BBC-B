using static SDL2.SDL;

const int ScreenWidth = 640;
const int ScreenHeight = 256;
const int PixelScale = 2;

if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_AUDIO) < 0)
{
    Console.WriteLine("SDL could not initialize! SDL_Error: " + SDL_GetError());
    return;
}

var window = SDL_CreateWindow("BBC Micro Emulator",
    SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
    ScreenWidth * PixelScale, ScreenHeight * PixelScale,
    SDL_WindowFlags.SDL_WINDOW_SHOWN);

var renderer = SDL_CreateRenderer(window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
var texture = SDL_CreateTexture(renderer,
    SDL_PIXELFORMAT_ARGB8888,
    (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
    ScreenWidth, ScreenHeight);

var running = true;
var frameBuffer = new byte[ScreenWidth * ScreenHeight * 4]; // ARGB

while (running)
{
    while (SDL_PollEvent(out var e) == 1)
    {
        if (e.type == SDL_EventType.SDL_QUIT)
        {
            running = false;
        }
        // Handle input here
    }

    // Emulator step
    // cpu.Step();
    // render framebuffer...

    unsafe
    {
        fixed (byte* ptr = frameBuffer)
        {
            SDL_UpdateTexture(texture, IntPtr.Zero, (IntPtr)ptr, ScreenWidth * 4);
        }
    }

    SDL_RenderClear(renderer);
    SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
    SDL_RenderPresent(renderer);

    SDL_Delay(16); // ~60Hz
}

SDL_DestroyTexture(texture);
SDL_DestroyRenderer(renderer);
SDL_DestroyWindow(window);
SDL_Quit();