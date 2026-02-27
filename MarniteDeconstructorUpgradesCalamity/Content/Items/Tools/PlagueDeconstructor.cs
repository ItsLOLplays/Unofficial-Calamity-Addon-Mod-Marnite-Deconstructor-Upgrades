using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Materials;
using MarniteDeconstructorUpgradesCalamity.Projectiles.Melee;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MarniteDeconstructorUpgradesCalamity.Content.Items.Tools;

public class PlagueDeconstructor : ModItem, ILocalizedModType
{
    public static int ArmorPenetration = 25;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ArmorPenetration);
    public new string LocalizationCategory => "Items.Tools";

    public override void SetDefaults()
    {
        Item.width = 36;
        Item.height = 18;
        Item.damage = 137;
        Item.DamageType = DamageClass.Melee;
        Item.ArmorPenetration = ArmorPenetration;
        Item.hammer = 95;
        Item.tileBoost = 20;
        Item.useAnimation = 25;
        Item.useTime = 4;
        Item.knockBack = 0.5f;
        Item.shoot = ModContent.ProjectileType<PlagueDeconstructorProj>();
        Item.shootSpeed = 40f;

        Item.UseSound = SoundID.Item23;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.channel = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;

        Item.value = CalamityGlobalItem.RarityYellowBuyPrice;
        Item.rare = ItemRarityID.Yellow;
    }

    public override void HoldItem(Player player)
    {
        player.Calamity().mouseWorldListener = true;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient<HellstoneDeconstructor>()
            .AddIngredient<PlagueCellCanister>(17)
            .AddIngredient<InfectedArmorPlating>(11)
            .AddIngredient<AlchemicalDecanter>()
            .AddTile(TileID.MythrilAnvil)
            .Register();
    }
}