using UnityEngine;

namespace StellaGuild.Design
{
    public static class StellaColorTokens
    {
        public static readonly Color32 BaseBackground = new(0xFE, 0xF5, 0xE8, 0xFF);
        public static readonly Color32 SecondaryBackground = new(0xC6, 0xB1, 0x98, 0xFF);
        public static readonly Color32 Point = new(0xEA, 0xCB, 0x02, 0xFF);
        public static readonly Color32 Accent = new(0x02, 0x21, 0x6A, 0xFF);
        public static readonly Color32 Attention = new(0x9F, 0x16, 0x00, 0xFF);
        public static readonly Color32 TextShadow = new(0x28, 0x19, 0x0A, 0xFF);
        public static readonly Color32 NavigationPrimary = new(0x1B, 0x2D, 0x57, 0xFF);
        public static readonly Color32 MainButtonPrimary = new(0xE1, 0xC4, 0x1A, 0xFF);
        public static readonly Color32 MainButtonSecondary = new(0xC4, 0xAF, 0x97, 0xFF);

        public static Color Get(ColorToken token)
        {
            return token switch
            {
                ColorToken.BaseBackground => BaseBackground,
                ColorToken.SecondaryBackground => SecondaryBackground,
                ColorToken.Point => Point,
                ColorToken.Accent => Accent,
                ColorToken.Attention => Attention,
                ColorToken.TextShadow => TextShadow,
                ColorToken.NavigationPrimary => NavigationPrimary,
                ColorToken.MainButtonPrimary => MainButtonPrimary,
                ColorToken.MainButtonSecondary => MainButtonSecondary,
                _ => TextShadow
            };
        }

        public static string GetHex(ColorToken token)
        {
            return token switch
            {
                ColorToken.BaseBackground => "#FEF5E8",
                ColorToken.SecondaryBackground => "#C6B198",
                ColorToken.Point => "#EACB02",
                ColorToken.Accent => "#02216A",
                ColorToken.Attention => "#9F1600",
                ColorToken.TextShadow => "#28190A",
                ColorToken.NavigationPrimary => "#1B2D57",
                ColorToken.MainButtonPrimary => "#E1C41A",
                ColorToken.MainButtonSecondary => "#C4AF97",
                _ => "#28190A"
            };
        }
    }
}
