using System;
using CalamityMod;
using CalamityMod.Items.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace MarniteDeconstructorUpgradesCalamity.Projectiles.Melee;

public class HellstoneDeconstructorProj : ModProjectile
{
    // Textures used by the projectile (cached)
    public static Asset<Texture2D> GlowmaskTex; // The weapon's bloom along the beam
    public static Asset<Texture2D> BloomTex; // The central glow circle
    public static Asset<Texture2D> SelectionTex; // Square representing hitbox/selection

    // Display name and base texture path
    public override LocalizedText DisplayName => Language.GetText("Mods.YourModName.Items.HellstoneObliterator.DisplayName");
    public override string Texture => "MarniteDeconstructorUpgradesCalamity/Content/Items/Tools/HellstoneDeconstructor";

    // Convenience references
    public Player Owner => Main.player[Projectile.owner];

    // Local AI references
    public ref float MoveInIntervals => ref Projectile.localAI[0];
    public ref float SpeenBeams => ref Projectile.localAI[1];
    public ref float Timer => ref Projectile.ai[0];

    // Set default properties for the projectile
    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 30;
        Projectile.friendly = true; // Can hit enemies
        Projectile.penetrate = -1; // Infinite penetration
        Projectile.tileCollide = false; // Passes through tiles
        Projectile.hide = true; // Hide the default projectile sprite
        Projectile.ownerHitCheck = true; // Only hits if player can hit
        Projectile.DamageType = DamageClass.Melee;
        Projectile.usesIDStaticNPCImmunity = true;
        Projectile.idStaticNPCHitCooldown = 10; // Cooldown between hits on same NPC
    }

    // Prevent the game from automatically moving the projectile
    public override bool ShouldUpdatePosition() => false;

    // Main AI Function
    public override void AI()
    {
        Timer++;

        // Animate the spinning beams; slower animation after 140 ticks
        SpeenBeams += Timer > 140 ? 1 : 1 + 2f * (float)Math.Pow(1 - Timer / 140f, 2);

        // Play the weapon's use sound periodically
        if (Projectile.soundDelay <= 0)
        {
            SoundEngine.PlaySound(MarniteObliterator.UseSound, Projectile.Center);
            Projectile.soundDelay = 23;
        }

        // Lighting and visual effects along the projectile
        if ((Owner.Center - Projectile.Center).Length() >= 5f)
        {
            if ((Owner.MountedCenter - Projectile.Center).Length() >= 30f)
            {
                // Draw a blue light line along the beam
                DelegateMethods.v3_1 = Color.Blue.ToVector3() * 0.5f;
                Utils.PlotTileLine(Owner.MountedCenter + Owner.MountedCenter.DirectionTo(Projectile.Center) * 30f,
                    Projectile.Center, 8f, DelegateMethods.CastLightOpen);
            }

            // Add light at projectile's center
            Lighting.AddLight(Projectile.Center, Color.Blue.ToVector3() * 0.7f);
        }

        // Countdown delay between position updates
        if (MoveInIntervals > 0f)
            MoveInIntervals -= 1f;

        // If player cannot use holdout items, kill projectile
        if (Owner.CantUseHoldout())
            Projectile.Kill();

        // Update projectile position if ready
        else if (MoveInIntervals <= 0f && Main.myPlayer == Projectile.owner)
            UpdateProjectileVelocity();

        // Handle player arm direction & rotation
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Math.Sign(Projectile.velocity.X));
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
            Projectile.velocity.ToRotation() * Owner.gravDir - MathHelper.PiOver2);
        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full,
            Projectile.velocity.ToRotation() * Owner.gravDir - MathHelper.PiOver2 -
            MathHelper.PiOver4 * 0.5f * Owner.direction);

        // Keep item use timer active
        Owner.SetDummyItemTime(2);

        // Rotate projectile along its velocity and move it to match player
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = Owner.MountedCenter + Projectile.velocity;
    }

    // Separate function to update the projectile's velocity/position
    private void UpdateProjectileVelocity()
    {
        var newVelocity = Owner.Calamity().mouseWorld - Owner.MountedCenter;

        // Handle special wall targeting
        if (Main.tile[Player.tileTargetX, Player.tileTargetY].WallType != WallID.None)
        {
            newVelocity = new Vector2(Player.tileTargetX, Player.tileTargetY) * 16f + Vector2.One * 8f -
                          Owner.MountedCenter;
            MoveInIntervals = 2f;
        }

        // Smooth movement
        newVelocity = Vector2.Lerp(newVelocity, Projectile.velocity, 0.7f);
        if (float.IsNaN(newVelocity.X) || float.IsNaN(newVelocity.Y))
            newVelocity = -Vector2.UnitY;

        // Minimum range
        if (newVelocity.Length() < 50f)
            newVelocity = newVelocity.SafeNormalize(-Vector2.UnitY) * 50f;

        // Clamp to tile reach
        var tileBoost = Owner.inventory[Owner.selectedItem].tileBoost;
        var fullRangeX = (Player.tileRangeX + tileBoost - 1) * 16 + 11;
        var fullRangeY = (Player.tileRangeY + tileBoost - 1) * 16 + 11;
        newVelocity.X = Math.Clamp(newVelocity.X, -fullRangeX, fullRangeX);
        newVelocity.Y = Math.Clamp(newVelocity.Y, -fullRangeY, fullRangeY);

        // Network sync
        if (newVelocity != Projectile.velocity)
            Projectile.netUpdate = true;

        Projectile.velocity = newVelocity;
    }

    // Draws a single animated beam from the weapon, including the spinning square effect and chromatic aberration
    public void DrawBeam(Texture2D beamTex, Vector2 direction, float beamProgress)
    {
        // Calculate start position of the beam
        var startPos = Owner.MountedCenter
                       + direction * 30f
                       + direction.RotatedBy(MathHelper.PiOver2) *
                       (float)Math.Cos(MathHelper.TwoPi * beamProgress + SpeenBeams * 0.06f) * 8f;

        // Animate square movement along the beam (height + width)
        var squareHeight = (beamProgress + SpeenBeams * 0.02f) % 1;
        if (squareHeight < 0.25)
            squareHeight = 0;
        else if (squareHeight < 0.5)
            squareHeight = (squareHeight - 0.25f) / 0.25f;
        else if (squareHeight < 0.75)
            squareHeight = 1;
        else
            squareHeight = 1 - (squareHeight - 0.75f) / 0.25f;

        var squareWidth = (beamProgress + SpeenBeams * 0.02f) % 1;
        if (squareWidth < 0.25)
            squareWidth /= 0.25f;
        else if (squareWidth < 0.5)
            squareWidth = 1;
        else if (squareWidth < 0.75)
            squareWidth = 1 - (squareWidth - 0.5f) / 0.25f;
        else
            squareWidth = 0;

        // Determine end position of the beam
        float squareTiles = 2f; // Number of tiles for square hitbox
        float squareSize = 16f * squareTiles; // Convert tiles to pixels
        float halfSize = squareSize / 2f;

        var endPos = Projectile.Center
                     + new Vector2(squareWidth * squareSize, squareHeight * squareSize)
                     - Vector2.One * halfSize;

        // Calculate rotation and scale for beam texture
        var rotation = (endPos - startPos).ToRotation();
        var beamOrigin = new Vector2(beamTex.Width / 2f, beamTex.Height);
        var beamScale = new Vector2(5.4f, (startPos - endPos).Length() / beamTex.Height);

        // Draw beam with chromatic aberration effect
        CalamityUtils.DrawChromaticAberration(direction.RotatedBy(MathHelper.PiOver2), 1f,
            delegate(Vector2 offset, Color colorMod)
            {
                // Primary beam color
                var beamColor = Color.Lerp(Color.Blue, Color.Goldenrod,
                    0.5f + 0.5f * (float)Math.Sin(SpeenBeams * 0.2f));
                beamColor *= 0.54f;
                beamColor = beamColor.MultiplyRGB(colorMod);

                // Draw the first beam pass
                Main.EntitySpriteDraw(beamTex,
                    startPos + offset - Main.screenPosition,
                    null,
                    beamColor,
                    rotation + MathHelper.PiOver2,
                    beamOrigin,
                    beamScale,
                    SpriteEffects.None);

                // Draw second pass with smaller width and phase offset for chromatic effect
                beamScale.X = 2.4f;
                beamColor = Color.Lerp(Color.Navy, Color.Crimson,
                    0.5f + 0.5f * (float)Math.Sin(SpeenBeams * 0.2f + 1.2f));
                beamColor = beamColor.MultiplyRGB(colorMod);

                Main.EntitySpriteDraw(beamTex,
                    startPos + offset - Main.screenPosition,
                    null,
                    beamColor,
                    rotation + MathHelper.PiOver2,
                    beamOrigin,
                    beamScale,
                    SpriteEffects.None);
            });
    }

    // Handles all visual effects for the weapon while held out
    public override bool PreDraw(ref Color lightColor)
    {
        if (!Projectile.active)
            return false;

        // Determine direction and weapon texture
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.Zero);
        Texture2D weaponTexture = TextureAssets.Projectile[Type].Value;
        Vector2 weaponOrigin = new(9f, weaponTexture.Height / 2f); // pivot point for rotation
        SpriteEffects weaponEffects = SpriteEffects.None;

        if (Owner.direction * (double)Owner.gravDir < 0.0)
            weaponEffects = SpriteEffects.FlipVertically;

        Main.EntitySpriteDraw(weaponTexture,
            Owner.MountedCenter + direction * 10f - Main.screenPosition,
            null,
            Projectile.GetAlpha(lightColor),
            Projectile.rotation,
            weaponOrigin,
            Projectile.scale,
            weaponEffects);

        // Switch to additive blending for bloom effects
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate,
            BlendState.Additive,
            Main.DefaultSamplerState,
            DepthStencilState.None,
            Main.Rasterizer,
            null,
            Main.GameViewMatrix.TransformationMatrix);

        // Draw the main weapon bloom
        if (GlowmaskTex == null)
            GlowmaskTex = ModContent.Request<Texture2D>(
                "MarniteDeconstructorUpgradesCalamity/Content/Items/Tools/HellstoneDeconstructorBloom"
            );

        Texture2D bloomTexture = GlowmaskTex.Value;

        // Calculate bloom intensity based on timer and a sine wave for subtle flicker
        float bloomIntensity = (float)(Math.Pow(Math.Clamp(Timer / 100f, 0.0f, 1f), 2.0) *
                                       (0.85f + (0.5 + 0.5 * Math.Sin(Main.GlobalTimeWrappedHourly))) * 0.8f);
        Color bloomColor = Color.Lerp(Color.Purple, Color.ForestGreen,
            (float)(0.5 + 0.5 * Math.Sin(SpeenBeams * 0.2f + 1.2f)));

        // Offset along weapon's facing direction
        Vector2 bloomOffset = direction * 16f;
        Vector2 bloomOrigin = new Vector2(bloomTexture.Width / 2f, bloomTexture.Height / 2f);

        // Handle mirroring if weapon is flipped horizontally
        SpriteEffects bloomEffects = weaponEffects;
        if ((bloomEffects & SpriteEffects.FlipHorizontally) != 0)
            bloomOffset.X = -bloomOffset.X;

        Vector2 bloomPos = Owner.MountedCenter + bloomOffset - Main.screenPosition;

        Main.EntitySpriteDraw(bloomTexture,
            bloomPos,
            null,
            bloomColor * bloomIntensity,
            Projectile.rotation,
            bloomOrigin,
            Projectile.scale,
            bloomEffects);

        // Draw the bloom circle at the center of the square
        if (BloomTex == null)
            BloomTex = ModContent.Request<Texture2D>(
                "MarniteDeconstructorUpgradesCalamity/Particles/HellstoneBloomCircle"
            );
        Texture2D bloomCircle = BloomTex.Value;
        Main.EntitySpriteDraw(bloomCircle,
            Projectile.Center - Main.screenPosition,
            null,
            Color.DeepSkyBlue * 0.3f,
            1.5f,
            bloomCircle.Size() / 2f,
            0.3f * Projectile.scale,
            SpriteEffects.None);

        // Draw the selection square
        if (SelectionTex == null)
            SelectionTex = ModContent.Request<Texture2D>(
                "MarniteDeconstructorUpgradesCalamity/Content/Items/Tools/HellstoneDeconstructorSelection"
            );

        Texture2D selectionTexture = SelectionTex.Value;

        CalamityUtils.DrawChromaticAberration(Vector2.UnitX, 2f,
            (offset, colorMod) => Main.EntitySpriteDraw(selectionTexture,
                Projectile.Center + offset - Main.screenPosition,
                null,
                bloomColor.MultiplyRGB(colorMod),
                0.0f,
                selectionTexture.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None));

        // Draw the animated beams
        var beamTex = ModContent.Request<Texture2D>(
            "CalamityMod/ExtraTextures/GreyscaleGradients/SimpleGradient"
        ).Value;

        for (var index = 0; index < 2; ++index)
            DrawBeam(beamTex, direction, index / 2f);

        // Restore default spritebatch settings
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        return false;
    }

    // On hit, apply debuff
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        target.AddBuff(BuffID.OnFire, 300);
    }
}