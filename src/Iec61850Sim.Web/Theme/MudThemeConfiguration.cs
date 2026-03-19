using MudBlazor;

namespace Iec61850Sim.Web.Theme;

public static class MudThemeConfiguration
{
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0EA5E9",        // sky blue
            Secondary = "#6366F1",      // indigo (complementary)
            Tertiary = "#0D9488",       // teal
            AppbarBackground = "#0EA5E9",
            AppbarText = "#FFFFFF",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#38BDF8",
            Secondary = "#818CF8",
            Tertiary = "#2DD4BF",
            AppbarBackground = "#0C4A6E",
        }
    };
}
