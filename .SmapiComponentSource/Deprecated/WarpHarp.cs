#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace StardewValley.Tools // Required to spawn properly
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    //Unimplemented Warp Harp tool
    internal class WarpHarp : Tool
    {
        public WarpHarp()
        {
            InstantUse = true;
        }

        public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
        {
            Game1.player.FacingDirection = Game1.down;
            Game1.player.FarmerSprite.animateOnce(
            [
                new FarmerSprite.AnimationFrame(98, 150, secondaryArm: false, flip: false),
            ]);
            Game1.player.FarmerSprite.PauseForSingleAnimation = true;
            Game1.activeClickableMenu = null; // TODO
            return base.beginUsing(location, x, y, who);
        }

        protected override Item GetOneNew()
        {
            return new WarpHarp();
        }
    }
}
