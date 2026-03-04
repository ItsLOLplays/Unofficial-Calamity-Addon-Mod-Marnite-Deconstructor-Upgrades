using Terraria.ModLoader.Config;
using System.ComponentModel;

namespace MarniteDeconstructorUpgradesCalamity.Config;

public class MduConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [DefaultValue(true)]
    public bool EnableTileHammering;

    [DefaultValue(true)]
    public bool EnableWallHammering;

    [DefaultValue(false)] 
    public bool DisableHammerPower;
}