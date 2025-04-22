using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace SwordAndSorcerySMAPI.IgnoreMarriageSchedule
{
    public class IgnoreMarriageScheduleAssetModel
    {
        public bool IgnoreMarriageSchedule { get; set; } = false;
        public bool IgnoreMarriageDialogue { get; set; } = false;
        public string CanVisitFarmToday { get; set; } = null;
    }

    internal class IgnoreMarriageScheduleAssetManager
    {
        private static Dictionary<string, IgnoreMarriageScheduleAssetModel> _IgnoreMarriageAsset = null;
        public static Dictionary<string, IgnoreMarriageScheduleAssetModel> IgnoreMarriageAsset
        {
            get
            { 
                _IgnoreMarriageAsset ??= Game1.content.Load<Dictionary<string, IgnoreMarriageScheduleAssetModel>>("DN.SnS/IgnoreMarriageSchedule");
                return _IgnoreMarriageAsset;
            }
        }

        public static void AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("DN.SnS/IgnoreMarriageSchedule"))
            {
                e.LoadFrom(() => new Dictionary<string, IgnoreMarriageScheduleAssetModel>(), AssetLoadPriority.Exclusive);
            }
        }

        public static void AssetInvalidated(object sender, AssetsInvalidatedEventArgs e)
        {
            if (e.NamesWithoutLocale.Any(a => a.IsEquivalentTo("DN.SnS/IgnoreMarriageSchedule")))
                _IgnoreMarriageAsset = null;
        }
    }
}
