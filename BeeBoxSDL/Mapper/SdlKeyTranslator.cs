namespace BeeBoxSDL.Mapper;

using SDL2;

public static class SdlKeyTranslator
{
    public static int SdlToVirtualKey(SDL.SDL_Keycode keycode)
    {
        return keycode switch
        {
            SDL.SDL_Keycode.SDLK_RETURN => 0x0D, // VK_RETURN
            SDL.SDL_Keycode.SDLK_ESCAPE => 0x1B, // VK_ESCAPE
            SDL.SDL_Keycode.SDLK_BACKSPACE => 0x08, // VK_BACK
            SDL.SDL_Keycode.SDLK_TAB => 0x09, // VK_TAB
            SDL.SDL_Keycode.SDLK_SPACE => 0x20, // VK_SPACE
            SDL.SDL_Keycode.SDLK_LEFT => 0x25, // VK_LEFT
            SDL.SDL_Keycode.SDLK_UP => 0x26, // VK_UP
            SDL.SDL_Keycode.SDLK_RIGHT => 0x27, // VK_RIGHT
            SDL.SDL_Keycode.SDLK_DOWN => 0x28, // VK_DOWN
            SDL.SDL_Keycode.SDLK_INSERT => 0x2D, // VK_INSERT
            SDL.SDL_Keycode.SDLK_DELETE => 0x2E, // VK_DELETE
            SDL.SDL_Keycode.SDLK_HOME => 0x24, // VK_HOME
            SDL.SDL_Keycode.SDLK_END => 0x23, // VK_END
            SDL.SDL_Keycode.SDLK_PAGEUP => 0x21, // VK_PRIOR
            SDL.SDL_Keycode.SDLK_PAGEDOWN => 0x22, // VK_NEXT
            SDL.SDL_Keycode.SDLK_LSHIFT => 0xA0, // VK_LSHIFT
            SDL.SDL_Keycode.SDLK_RSHIFT => 0xA1, // VK_RSHIFT
            SDL.SDL_Keycode.SDLK_LCTRL => 0xA2, // VK_LCONTROL
            SDL.SDL_Keycode.SDLK_RCTRL => 0xA3, // VK_RCONTROL
            SDL.SDL_Keycode.SDLK_LALT => 0xA4, // VK_LMENU
            SDL.SDL_Keycode.SDLK_RALT => 0xA5, // VK_RMENU
            SDL.SDL_Keycode.SDLK_CAPSLOCK => 0x14, // VK_CAPITAL

            // Function keys
            SDL.SDL_Keycode.SDLK_F1 => 0x70,
            SDL.SDL_Keycode.SDLK_F2 => 0x71,
            SDL.SDL_Keycode.SDLK_F3 => 0x72,
            SDL.SDL_Keycode.SDLK_F4 => 0x73,
            SDL.SDL_Keycode.SDLK_F5 => 0x74,
            SDL.SDL_Keycode.SDLK_F6 => 0x75,
            SDL.SDL_Keycode.SDLK_F7 => 0x76,
            SDL.SDL_Keycode.SDLK_F8 => 0x77,
            SDL.SDL_Keycode.SDLK_F9 => 0x78,
            SDL.SDL_Keycode.SDLK_F10 => 0x79,
            SDL.SDL_Keycode.SDLK_F11 => 0x7A,
            SDL.SDL_Keycode.SDLK_F12 => 0x7B,

            // ASCII keys (0-9, A-Z)
            >= SDL.SDL_Keycode.SDLK_0 and <= SDL.SDL_Keycode.SDLK_9 => (int)keycode,
            >= SDL.SDL_Keycode.SDLK_a and <= SDL.SDL_Keycode.SDLK_z => char.ToUpperInvariant((char)keycode),

            // Punctuation keys
            SDL.SDL_Keycode.SDLK_COMMA => 0xBC, // ,
            SDL.SDL_Keycode.SDLK_PERIOD => 0xBE, // .
            SDL.SDL_Keycode.SDLK_SLASH => 0xBF, // /
            SDL.SDL_Keycode.SDLK_SEMICOLON => 0xBA, // ;
            SDL.SDL_Keycode.SDLK_EQUALS => 0xBB, // =
            SDL.SDL_Keycode.SDLK_MINUS => 0xBD, // -
            SDL.SDL_Keycode.SDLK_LEFTBRACKET => 0xDB, // [
            SDL.SDL_Keycode.SDLK_RIGHTBRACKET => 0xDD, // ]
            SDL.SDL_Keycode.SDLK_BACKSLASH => 0xDC, // \
            SDL.SDL_Keycode.SDLK_QUOTE => 0xDE, // '
            SDL.SDL_Keycode.SDLK_BACKQUOTE => 0xC0, // `

            // Default fallback
            _ => 0
        };
    }
}