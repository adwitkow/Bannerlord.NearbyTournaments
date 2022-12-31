using System;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using System.Linq;

namespace Bannerlord.NearbyTournaments
{
    public class SubModule : MBSubModuleBase
    {
        private const string TownArenaMenu = "town_arena";
        private const string NearbyTournamentsMenu = "menu_town_nearby_tournaments";

        private const string NearbyTournamentsOption = "nearby_tournaments";
        private const string LeaveOption = "nearby_tournaments_leave";

        private const string NearbyTournamentsText = "{=kn5mme59}Check the nearby tournaments";
        private const string ManyTournamentsText = $"{{=pinSMuMe}}Well, there's one starting up at {{{ClosestTournamentVariable}}}, then another at {{{NextClosestTournamentVariable}}}. You should probably be able to get to either of those, if you move quickly.";
        private const string SingleTournamentText = $"{{=2WnruiBw}}I know of one starting up at {{{ClosestTournamentVariable}}}. You should be able to get there if you move quickly enough.";
        private const string NoTournamentsText = $"{{=tGI135jv}}Ah - I don't know of any right now. That's a bit unusual though. Must be the wars.";
        private const string LeaveText = "{=3sRdGQou}Leave";

        private const string NearbyTournamentsVariable = "NEARBY_TOURNAMENTS";
        private const string ClosestTournamentVariable = "CLOSEST_TOURNAMENT";
        private const string NextClosestTournamentVariable = "NEXT_CLOSEST_TOURNAMENT";

        public override void OnAfterGameInitializationFinished(Game game, object starterObject)
        {
            base.OnAfterGameInitializationFinished(game, starterObject);

            if (game.GameType is Campaign)
            {
                var gameStarter = (CampaignGameStarter)starterObject;
                gameStarter.AddGameMenuOption(TownArenaMenu,
                    NearbyTournamentsOption,
                    NearbyTournamentsText,
                    GetCondition(args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown;
                    }),
                    GetConsequence(args => GameMenu.SwitchToMenu(NearbyTournamentsMenu)),
                    false,
                    1);

                gameStarter.AddGameMenu(NearbyTournamentsMenu,
                    $"{{{NearbyTournamentsVariable}}}",
                    new OnInitDelegate(this.GetNearbyTournaments));

                gameStarter.AddGameMenuOption(NearbyTournamentsMenu,
                    LeaveOption,
                    LeaveText,
                    GetCondition(args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                        return true;
                    }),
                    GetConsequence(args => GameMenu.SwitchToMenu(TownArenaMenu)),
                    true);
            }
        }

        private void GetNearbyTournaments(MenuCallbackArgs args)
        {
            var currentPosition = Settlement.CurrentSettlement.Position2D;
            var tournamentSettlements = Town.AllTowns.Where(town => HasActiveTournament(town))
                .Select(town => town.Settlement)
                .OrderBy(settlement => settlement.Position2D.DistanceSquared(currentPosition))
                .Take(3)
                .ToList();

            TextObject textObject;
            if (tournamentSettlements.Count > 1)
            {
                textObject = new TextObject(ManyTournamentsText);
                textObject.SetTextVariable(ClosestTournamentVariable, tournamentSettlements[0].EncyclopediaLinkWithName);
                textObject.SetTextVariable(NextClosestTournamentVariable, tournamentSettlements[1].EncyclopediaLinkWithName);
            }
            else if (tournamentSettlements.Count == 1)
            {
                textObject = new TextObject(SingleTournamentText);
                textObject.SetTextVariable(ClosestTournamentVariable, tournamentSettlements[0].EncyclopediaLinkWithName);
            }
            else
            {
                textObject = new TextObject(NoTournamentsText);
            }

            GameTexts.SetVariable(NearbyTournamentsVariable, textObject);
        }

        private static bool HasActiveTournament(Town town)
        {
            return Campaign.Current.TournamentManager.GetTournamentGame(town) != null
                && town != Settlement.CurrentSettlement.Town;
        }

        private static GameMenuOption.OnConditionDelegate GetCondition(Func<MenuCallbackArgs, bool> method)
        {
            return new GameMenuOption.OnConditionDelegate(method);
        }

        private static GameMenuOption.OnConsequenceDelegate GetConsequence(Action<MenuCallbackArgs> method)
        {
            return new GameMenuOption.OnConsequenceDelegate(method);
        }
    }
}