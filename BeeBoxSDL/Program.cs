using BeeBoxSDL.Core;
using BeeBoxSDL.Mapper;
using MLDComputing.Emulators.BBCSim.Beeb;
using SDL2;

var keyMapper = InitkKeyMapper();

var vm = new BeebEm();
var osPath = Path.Combine(AppContext.BaseDirectory, "roms", string.Intern("os12.rom"));
var basicPath = Path.Combine(AppContext.BaseDirectory, "roms", "basic2.rom");
var fontPath = Path.Combine(AppContext.BaseDirectory, "roms", "mode7font.rom");

vm.LoadRoms(osPath, basicPath);

using var renderer = new TeletextSdlRenderer();
renderer.Create(fontPath);

vm.FrameReady += (o, eventArgs) =>
{
    while (SDL.SDL_PollEvent(out var sdlEvent) == 1)
    {
        var keycode = sdlEvent.key.keysym.sym;
        var mod = sdlEvent.key.keysym.mod;

        var virtualKey = SdlKeyTranslator.SdlToVirtualKey(keycode); // Same as KeyInterop.VirtualKeyFromKey
        var shiftHeld = (mod & SDL.SDL_Keymod.KMOD_LSHIFT) != 0 || (mod & SDL.SDL_Keymod.KMOD_RSHIFT) != 0;
        var mapping = keyMapper.ProcessKeyPress(virtualKey, shiftHeld, sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN);

        switch (sdlEvent.type)
        {
            case SDL.SDL_EventType.SDL_QUIT:
                vm.MachineIsRunning = false;
                return;

            case SDL.SDL_EventType.SDL_KEYDOWN:
                vm.KeyboardMatrix.PressKey(mapping.Row, mapping.Column);
                break;

            case SDL.SDL_EventType.SDL_KEYUP:
                vm.KeyboardMatrix.ReleaseKey(mapping.Row, mapping.Column);
                break;
        }
    }

    renderer.Render(vm.Memory.GetVideoBuffer());
};

vm.Start();

KeyMapper InitkKeyMapper()
{
    var keyMapper = new KeyMapper();
    keyMapper.InitKeyMaps();
    return keyMapper;
}