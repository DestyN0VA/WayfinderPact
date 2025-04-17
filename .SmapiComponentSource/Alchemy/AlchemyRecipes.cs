using SpaceCore;
using StardewValley;
using System.Collections.Generic;

namespace SwordAndSorcerySMAPI.Alchemy
{
    internal static class AlchemyRecipes
    {
        public static Dictionary<string, AlchemyData> _RecipeData;
        private static Dictionary<string, AlchemyData> RecipeData
        {
            get
            {
                _RecipeData ??= Game1.content.Load<Dictionary<string, AlchemyData>>("DN.SnS/AlchemyRecipes");
                return _RecipeData;
            }
        }

        public static Dictionary<string, AlchemyData> Get()
        {
            Dictionary<string, AlchemyData> AdjustedRecipeData = [];

            foreach (var recipe in RecipeData)
            {
                var value = recipe.Value;
                Dictionary<string, int> ingreds = [];

                foreach (var ingred in value.Ingredients)
                {
                    ingreds.Add(ingred.Key, ingred.Value / (Game1.player.HasCustomProfession(SorcerySkill.ProfessionPhilosopherStone) && ingred.Value > 1 && (ItemRegistry.Create(ingred.Key).HasContextTag("essence_item") || ingred.Key == "(O)768" || ingred.Key == "(O)769") ? 2 : 1));
                }

                AlchemyData data = new()
                {
                    OutputItem = value.OutputItem,
                    OutputQuantity = value.OutputQuantity,
                    Ingredients = ingreds,
                    UnlockConditions = value.UnlockConditions
                };
                AdjustedRecipeData.Add(recipe.Key, data);
            }

            return AdjustedRecipeData;
        }
    }
}
