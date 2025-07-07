namespace BeeBoxSDL._6502.Assembler.Constants;

public static class RegularExpressionConstants
{
    public const string IndirectValueRegEx = @"^\([^ :,]*\)";
    public const string IndirectLabelRegEx = @"^\([a-zA-Z]{1}\w*:\)";
    public const string LabelRegEx = @"[a-zA-Z]{1}\w*:";
    public const string VariableRegEx = @"[a-zA-Z]{1}\w*[-\+]{0,1}\d{0,}([\<\>])?";
    public const string VariableNameRegEx = @"[a-zA-Z]{1}\w*";
    public const string IndexedIndirectValueRegEx = @"^\(\$\w*,X\)|^\(\w*,X\)";
    public const string IndexedIndirectLabelRegEx = @"^\([a-zA-Z]{1}\w*:,X\)";
    public const string IndirectIndexedValueRegEx = @"^\(\$\w*\),Y|^\(\w*\),Y";
    public const string IndirectIndexedLabelRegEx = @"^\([a-zA-Z]{1}\w*:\),Y";
    public const string ValueOnlyRegEx = @"[-a-zA-Z0-9]+";
}