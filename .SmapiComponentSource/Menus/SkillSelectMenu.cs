using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.Network.NetEvents;
using System.Collections.Generic;
using System.Linq;
using static StardewValley.Menus.CharacterCustomization;
using static SwordAndSorcerySMAPI.Menus.SkillSelectMenu.CharacterCustomizationPatch1;

namespace SwordAndSorcerySMAPI.Menus
{
    internal class SkillSelectMenu
    {
        public static class CharacterCustomizationPatch1
        {
            public static List<string> Skills = ["Artificer", "Druidics", "Bardics", "Sorcery", "Paladin"];

            public static List<ClickableComponent> Components = [];

            public static List<SpaceCore.Skills.Skill> Skills2 = [ModSnS.RogueSkill, ModCoT.DruidSkill, ModUP.BardicsSkill, ModTOP.SorcerySkill, ModTOP.PaladinSkill];
            public static Dictionary<string, bool> SkillsSelected = [];

            public static void Postfix(CharacterCustomization __instance, CharacterCustomization.Source source)
            {
                if (source != CharacterCustomization.Source.NewGame &&
                    source != CharacterCustomization.Source.HostNewFarm &&
                    source != CharacterCustomization.Source.NewFarmhand &&
                    source != CharacterCustomization.Source.Wizard)
                    return;

                foreach (var s in SkillsSelected.Keys)
                    SkillsSelected[s] = false;

                SkillsSelected = new()
                {
                    ["Artificer"] = false,
                    ["Druidics"] = false,
                    ["Bardics"] = false,
                    ["Sorcery"] = false,
                    ["Paladin"] = false,
                };


                Components.Clear();
                List<int> IDs = [];
                __instance.populateClickableComponentList();
                foreach (var c in __instance.allClickableComponents)
                    IDs.Add(c.myID);
                IDs.Sort();

                int LastId = IDs.First();

                for (int i = 0; i < 5; i++)
                {
                    int x = __instance.xPositionOnScreen - 256 - 32 + 50 + 4 - ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko) ? 25 : 0) + 24 - (source == CharacterCustomization.Source.HostNewFarm ? 268 : 0);
                    int y = __instance.yPositionOnScreen + IClickableMenu.borderWidth * 2 + 48 * (i + 1) + 4 * i + 24 + (__instance.source == CharacterCustomization.Source.HostNewFarm ? 68 : 0);

                    ClickableComponent c = new(new(x, y, 36, 36), Skills[i])
                    {
                        myID = i + LastId + 1,
                        downNeighborID = i >= 4 ? i + LastId + 2 : -99998,
                        rightNeighborID = source != Source.HostNewFarm ? __instance.leftSelectionButtons.First().myID : __instance.cabinLayoutButtons.First().myID,
                        upNeighborID = i > 0 ? i + LastId : -99998
                    };
                    Components.Add(c);
                }

                __instance.allClickableComponents.AddRange(Components);
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.draw), [typeof(SpriteBatch)])]
        static class CharacterCustomizationPatch2
        {
            public static void Postfix(CharacterCustomization __instance, SpriteBatch b)
            {
                int x = __instance.xPositionOnScreen - 256 - 32 + 4 - ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko) ? 25 : 0) - (__instance.source == CharacterCustomization.Source.HostNewFarm ? 268 : 0);
                int y = __instance.yPositionOnScreen + IClickableMenu.borderWidth * 2 + (__instance.source == CharacterCustomization.Source.HostNewFarm ? 68 : 0);
                IClickableMenu.drawTextureBox(b,
                x,
                y,
                288, 344,
                Color.White);

                Utility.drawTextWithShadow(b, I18n.SelectSkill(), Game1.dialogueFont,
                    new(x + IClickableMenu.borderWidth + 288 / 2 - SpriteText.getWidthOfString(I18n.SelectSkill()) / 2 + 6, y + 20), 
                    Game1.textColor);
                for (int i = 0; i < Components.Count; i++)
                {
                    ClickableComponent c = Components[i];
                    b.Draw(Game1.mouseCursors, new(c.bounds.X, c.bounds.Y), SkillsSelected[c.name] ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.41f);
                    b.Draw(Skills2[i].SkillsPageIcon, new(c.bounds.X - 50, c.bounds.Y - 2), Skills2[i].SkillsPageIcon.Bounds, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.4f);
                    Utility.drawTextWithShadow(b, Skills2[i].GetName(), Game1.smallFont, new(c.bounds.X + 44, c.bounds.Y + 8), Game1.textColor * (SkillsSelected[c.name] ? 1f : 0.33f));
                }
            }
        }
        
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.receiveLeftClick))]
        static class CharacterCustomizationPatch3
        {
            public static void Postfix(int x, int y, bool playSound = true)
            {
                if (Components.Any(c => c.containsPoint(x, y)))
                {
                    string c = Components.First(c => c.containsPoint(x, y)).name;
                    SkillsSelected[c] = !SkillsSelected[c];
                    if (playSound)
                    {
                        if (SkillsSelected[c])
                            Game1.playSound("bigSelect");
                        else
                            Game1.playSound("bigDeSelect");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.gameWindowSizeChanged))]
        static class CharacterCustomization4
        {
            public static void Postfix(CharacterCustomization __instance, Rectangle oldBounds, Rectangle newBounds)
            {
                Resize(__instance);
            }

            public static void Resize(CharacterCustomization cc)
            {
                for (int i = 0; i < 5; i++)
                {
                    ClickableComponent c = Components[i];

                    c.bounds.X = cc.xPositionOnScreen - 256 - 32 + 50 + 4 - ((LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.ko) ? 25 : 0) + 24 - (cc.source == CharacterCustomization.Source.HostNewFarm ? 268 : 0);
                    c.bounds.Y = cc.yPositionOnScreen + IClickableMenu.borderWidth * 2 + 48 * (i + 1) + 4 * i + 24 + (cc.source == CharacterCustomization.Source.HostNewFarm ? 68 : 0);
                }
                cc.populateClickableComponentList();
                cc.allClickableComponents.AddRange(Components);
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), "optionButtonClick")]
        static class CharacterCustomization5
        {
            public static void Postfix(string name)
            {
                if (name != "OK")
                    return;

                var ext = Game1.player.GetFarmerExtData();
                foreach (var s in SkillsSelected)
                {
                    switch (s.Key)
                    {
                        case "Artificer":
                            ext.StartingArtificer = s.Value;
                            break;
                        case "Druidics":
                            ext.StartingDruidics = s.Value;
                            break;
                        case "Bardics":
                            ext.StartingBardics = s.Value;
                            break;
                        case "Sorcery":
                            ext.StartingSorcery = s.Value;
                            break;
                        case "Paladin":
                            ext.StartingPaladin = s.Value;
                            break;
                    }
                    Game1.player.team.RequestSetMail(PlayerActionTarget.Current, $"Starting{s.Key}Skill", MailType.Received, s.Value);
                }
            }
        }
    }
}
