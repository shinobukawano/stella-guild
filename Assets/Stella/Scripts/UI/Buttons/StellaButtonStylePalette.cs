using StellaGuild.Design;
using UnityEngine;

namespace StellaGuild.UI.Buttons
{
    public static class StellaButtonStylePalette
    {
        public static Color GetFaceColor(StellaButtonVisualStyle style)
        {
            return style switch
            {
                StellaButtonVisualStyle.ActionRed => StellaColorTokens.Get(ColorToken.Attention),
                StellaButtonVisualStyle.NavigationBlue => StellaColorTokens.Get(ColorToken.NavigationPrimary),
                StellaButtonVisualStyle.MainPrimary => StellaColorTokens.Get(ColorToken.MainButtonPrimary),
                StellaButtonVisualStyle.MainSecondary => StellaColorTokens.Get(ColorToken.MainButtonSecondary),
                _ => StellaColorTokens.Get(ColorToken.Attention)
            };
        }

        public static Color GetShadowColor()
        {
            return StellaColorTokens.Get(ColorToken.TextShadow);
        }

        public static Color GetLabelColor(StellaButtonVisualStyle style)
        {
            return style switch
            {
                StellaButtonVisualStyle.ActionRed => StellaColorTokens.Get(ColorToken.BaseBackground),
                StellaButtonVisualStyle.NavigationBlue => StellaColorTokens.Get(ColorToken.BaseBackground),
                StellaButtonVisualStyle.MainPrimary => StellaColorTokens.Get(ColorToken.TextShadow),
                StellaButtonVisualStyle.MainSecondary => StellaColorTokens.Get(ColorToken.TextShadow),
                _ => StellaColorTokens.Get(ColorToken.BaseBackground)
            };
        }
    }
}
