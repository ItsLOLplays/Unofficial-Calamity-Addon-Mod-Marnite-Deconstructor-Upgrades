using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Tools;
using MarniteDeconstructorUpgradesCalamity.Projectiles.Melee;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MarniteDeconstructorUpgradesCalamity.Content.Items.Tools;

public class HellstoneDeconstructor : ModItem, ILocalizedModType
{
    public static int ArmorPenetration = 15;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ArmorPenetration);
    public new string LocalizationCategory => "Items.Tools";

    public override void SetDefaults()
    {
        Item.width = 36;
        Item.height = 18;
        Item.damage = 24;
        Item.DamageType = DamageClass.Melee;
        Item.ArmorPenetration = ArmorPenetration;
        Item.hammer = 75;
        Item.tileBoost = 15;
        Item.useAnimation = 25;
        Item.useTime = 4;
        Item.knockBack = 0.5f;
        Item.shoot = ModContent.ProjectileType<HellstoneDeconstructorProj>();
        Item.shootSpeed = 40f;

        Item.UseSound = SoundID.Item23;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.channel = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;

        Item.value = CalamityGlobalItem.RarityOrangeBuyPrice;
        Item.rare = ItemRarityID.Orange;
    }
    
    public override void HoldItem(Player player)
    {
        player.Calamity().mouseWorldListener = true;
    }
    
    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient<MarniteDeconstructor>()
            .AddIngredient(ItemID.HellstoneBar, 10)
            .AddIngredient<DubiousPlating>(3)
            .AddIngredient<MysteriousCircuitry>(3)
            .AddIngredient(ItemID.Ruby, 5)
            .AddTile(TileID.Anvils)
            .Register();
    }
}