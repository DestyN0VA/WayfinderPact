using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Monsters;
using SwordAndSorcerySMAPI.Framework.MercenaryPort.Actions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace SwordAndSorcerySMAPI.Framework.MercenaryPort
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    public class MercenaryEngine(IModHelper helper)
    {
        public static ConditionalWeakTable<Farmer, List<Vector2>> trails = [];

        public static int TrailingDistance = 10;

        private bool timeJustChanged = false;

        public void InitMercenary()
        {
            helper.Events.Content.AssetRequested += Content_AssetRequested;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.Player.Warped += Player_Warped;
            helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            helper.Events.Multiplayer.PeerDisconnected += Multiplayer_PeerDisconnected;
            helper.Events.GameLoop.DayEnding += GameLoop_DayEnding;
            helper.Events.GameLoop.TimeChanged += GameLoop_TimeChanged;
            helper.Events.Input.ButtonPressed += Input_ButtonPressed;

            MercenaryActionData.ActionTypes.Add("MeleeAttackNearestMonster", (merc, actionData) =>
            {
                string weaponId = "DN.SnS_keytoscarp";
                // TODO: Change based on merc.CorrespondingNpc (the npc ID)?

                //var actionParams = actionData.Parameters.ToObject<MeleeAttackMercenaryActionParameters>(js);
                MeleeAttackMercenaryActionParameters actionParams = new()
                {
                    MeleeWeaponId = weaponId,
                };

                if (merc.targeting != null)
                {
                    if (Vector2.Distance(merc.targeting.Position, Game1.player.Position) > actionParams.MinIgnoreRadius * Game1.tileSize)
                        merc.targeting = null;
                }

                if (merc.targeting == null)
                {
                    List<Monster> targets = [];
                    foreach (var target in Game1.player.currentLocation.characters)
                    {
                        if (target is Monster monster)
                        {
                            if (Vector2.Distance(monster.Position, Game1.player.Position) > actionParams.MaxEngagementRadius * Game1.tileSize)
                                continue;

                            if (actionParams.MonsterAllowList != null &&
                                 !actionParams.MonsterAllowList.Contains(monster.Name) &&
                                 !actionParams.MonsterAllowList.Contains(monster.GetType().FullName))
                                continue;

                            if (actionParams.MonsterBlockList != null &&
                                 (actionParams.MonsterBlockList.Contains(monster.Name) ||
                                   actionParams.MonsterBlockList.Contains(monster.GetType().FullName)))
                                continue;

                            targets.Add(monster);
                        }
                    }

                    if (targets.Count == 0)
                        return false;

                    targets.Sort((a, b) => Math.Sign(Vector2.DistanceSquared(a.Position, merc.Position) - (int)Vector2.DistanceSquared(b.Position, merc.Position)));
                    for (int i = 0; i < targets.Count; ++i)
                    {
                        merc.targeting = targets[i];
                        merc.untilNextPathfind = 0;
                        if (merc.DoPathfind(Game1.player.currentLocation))
                            break;
                    }
                }

                if (merc.attackId != actionData.Id)
                {
                    merc.attackId = actionData.Id;
                    merc.attackWeapon = new StardewValley.Tools.MeleeWeapon(actionParams.MeleeWeaponId);
                    foreach (string enchTypeName in actionParams.WeaponEnchantments)
                    {
                        var enchType = AccessTools.TypeByName(enchTypeName);
                        var ench = (BaseEnchantment)enchType.GetConstructor([]).Invoke([]);
                        merc.attackWeapon.AddEnchantment(ench);
                    }
                    foreach (var data in actionParams.WeaponModData)
                    {
                        merc.attackWeapon.modData.Add(data.Key, data.Value);
                    }
                    merc.showWeapon = actionParams.ShowWeapon;

                    merc.dummy.CurrentTool = merc.attackWeapon;
                    merc.dummy.enchantments.Clear();
                    merc.dummy.ReequipEnchantments();
                }

                // TODO: Redo once weapon changes are in place
                merc.swingTime = (400 - merc.attackWeapon.speed.Value * 40) /*/ (merc.attackWeapon.type.Value == 2 ? 5 : 8)*/ / 1000f * 1.75f;

                return merc.targeting != null;
            });
        }

        private void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var sc = helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            sc.RegisterSerializerType(typeof(Mercenary));
        }

        private void Player_Warped(object sender, WarpedEventArgs e)
        {
            if (e.Player != Game1.player)
                return;

            foreach (var merc in e.Player.GetCurrentMercenaries())
            {
                merc.Position = e.Player.Position;
                trails.GetOrCreateValue(e.Player).Clear();
            }
        }

        private void GameLoop_TimeChanged(object sender, TimeChangedEventArgs e)
        {
            timeJustChanged = true;
        }

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady && !Game1.HostPaused)
                return;
            if (!Context.IsMultiplayer && Game1.activeClickableMenu != null)
                return;
            //var mercData = Game1.content.Load<Dictionary<string, MercenaryData>>("spacechase0.MageDelve/Mercenaries");

            foreach (var player in Game1.getOnlineFarmers())
            {
                int i = 0;
                List<int> toRemove = [];
                foreach (var merc in player.GetCurrentMercenaries())
                {
                    if (timeJustChanged)
                    {
                        if (player == Game1.player && false)
                        //!GameStateQuery.CheckConditions(mercData[merc.CorrespondingNpc].CanRecruit, player: player))
                        {
                            merc.OnLeave();
                            toRemove.Add(i);
                        }
                    }

                    merc.UpdateForFarmer(player, i, Game1.currentGameTime);
                    ++i;
                }

                foreach (int index in toRemove)
                    player.GetCurrentMercenaries().RemoveAt(index);

                var trail = trails.GetOrCreateValue(player);
                if (trail.Count == 0 || trail[0] != player.Position)
                {
                    trail.Insert(0, player.Position);
                    if (trail.Count > player.GetCurrentMercenaries().Count * TrailingDistance)
                        trail.RemoveAt(trail.Count - 1);
                }
            }

            timeJustChanged = false;
        }

        private void Multiplayer_PeerDisconnected(object sender, PeerDisconnectedEventArgs e)
        {
            if (Game1.IsMasterGame)
            {
                var farmer = Game1.GetPlayer(e.Peer.PlayerID); // I don't know if they count as offline or not at this point
                foreach (var merc in farmer.GetCurrentMercenaries())
                {
                    merc.OnLeave();
                }
                farmer.GetCurrentMercenaries().Clear();
            }
        }

        private void GameLoop_DayEnding(object sender, DayEndingEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;

            foreach (var player in Game1.getOnlineFarmers())
            {
                foreach (var merc in player.GetCurrentMercenaries())
                {
                    merc.OnLeave();
                }
                player.GetCurrentMercenaries().Clear();
            }
        }
        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            /*
            if ( e.Button.IsActionButton() )
            {
                foreach (var player in Game1.getOnlineFarmers())
                {
                    if (player.currentLocation != Game1.currentLocation)
                        continue;

                    foreach (var merc in player.GetCurrentMercenaries().ToList())
                    {
                        if (merc.GetBoundingBox().Contains( e.Cursor.AbsolutePixels ))
                        {
                            if (player == Game1.player && Mod.Config.MercenaryInteractModifier.IsDown() )
                            {
                                List<Response> responses = new();
                                if ( merc.GetMercenaryData().CurrentDialogueString != null )
                                    responses.Add(new("Talk", I18n.Mercenary_Interact_Talk()));
                                responses.Add(new("Dismiss", I18n.Mercenary_Interact_Dismiss()));
                                responses.Add(new("Cancel", I18n.Mercenary_Interact_Cancel()));

                                Game1.currentLocation.afterQuestion = (farmer, answer) =>
                                {
                                    Game1.activeClickableMenu = null;
                                    Game1.player.CanMove = true;

                                    if (answer == "Talk")
                                    {
                                        var mercNpc = Game1.getCharacterFromName(merc.CorrespondingNpc);
                                        Game1.activeClickableMenu = new DialogueBox(new Dialogue(mercNpc, null, merc.GetMercenaryData().CurrentDialogueString));
                                        Game1.player.Halt();
                                        Game1.player.canMove = false;
                                        Game1.currentSpeaker = mercNpc;
                                    }
                                    else if ( answer == "Dismiss" )
                                    {
                                        merc.OnLeave(); // TODO: This should probably send a message in MP to do it on the host...
                                        farmer.GetCurrentMercenaries().Remove(merc);
                                    }
                                };
                                Game1.currentLocation.createQuestionDialogue(Game1.getCharacterFromName(merc.CorrespondingNpc).displayName, responses.ToArray(), "mercenary-interact");
                            }
                            else
                            {
                                Game1.getCharacterFromName(merc.CorrespondingNpc).checkAction(Game1.player, Game1.player.currentLocation);
                            }
                            return;
                        }
                    }
                }
            }
            */
        }
    }

    [HarmonyPatch(typeof(GameLocation), "drawCharacters")]
    public static class GameLocationDrawMercsPatch
    {
        public static void Postfix(GameLocation __instance, SpriteBatch b)
        {
            if (__instance.shouldHideCharacters() || __instance.currentEvent != null)
                return;

            foreach (var farmer in __instance.farmers)
            {
                foreach (var merc in farmer.GetCurrentMercenaries())
                {
                    merc.draw(b);
                }
            }
        }
    }
}
