using System.Collections.Generic;

namespace StickIt.Services
{
    public static class NoteColors
    {
        public enum NoteColor
        {
            NeonPink,
            Mulberry,
            FireballFuchsia,
            Poppy,
            Saffron,
            Curry,
            ThreeMYellow,
            CanaryYellow,
            NeonGreen,
            Asparagus,
            Emerald,
            MediterraneanBlue
        }

        public static readonly Dictionary<NoteColor, string> Hex = new()
        {
            { NoteColor.NeonPink,           "#dc39e5" },
            { NoteColor.Mulberry,           "#974f70" },
            { NoteColor.FireballFuchsia,    "#dd4b99" },
            { NoteColor.Poppy,              "#c67d4d" },
            { NoteColor.Saffron,            "#cb5147" },
            { NoteColor.Curry,              "#f0b555" },
            { NoteColor.ThreeMYellow,       "#f7e03d" },
            { NoteColor.CanaryYellow,       "#f9f8bc" },
            { NoteColor.NeonGreen,          "#a6fc41" },
            { NoteColor.Asparagus,          "#809b40" },
            { NoteColor.Emerald,            "#62bdb8" },
            { NoteColor.MediterraneanBlue,  "#4077c4" }
        };
    }
}
