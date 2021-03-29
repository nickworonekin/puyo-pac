using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuyoPac
{
    internal class Dependency
    {
        private static readonly List<Matcher> matchers;

        static Dependency()
        {
            matchers = new List<Matcher>
            {
                new Matcher(
                    new [] // Dependencies
                    {
                        @"ui\ui_cutin_cmn",
                        @"ui\ui_resident",
                        @"ui\ui_system",
                    },
                    new [] // Includes
                    {
                        "ability_cutin",
                        "ability_duel",
                        "bigbang_4p_",
                        "bracket",
                        "charaselect_1p2p",
                        "classic_4p_",
                        "entry_controller",
                        "field_chara_bg",
                        "gamesetting_1p2p",
                        "gamesetting_4p",
                        "gamesetting_8p",
                        "greeting",
                        "mix_4p_",
                        "mouse",
                        "party_4p_",
                        "platform_rpl_texture",
                        "player",
                        "playericon_feb",
                        "playericon_jan",
                        "playericon_mar",
                        "puyo2p_common_",
                        "puzzleleague",
                        "result",
                        "result_tournament",
                        "seiseki",
                        "sousai_2p",
                        "sousai_4p",
                        "standby_club_2p",
                        "standby_club_4p",
                        "standby_greeting",
                        "tokoton_list_1p_",
                        "tokoton_list_4p_",
                        "tv",
                        "ui_ability_buff",
                        "ui_ability_duel_",
                        "ui_ability_duel_cutin",
                        "ui_adv_window",
                        "ui_battle_history",
                        "ui_button",
                        "ui_charaselect_base_",
                        "ui_charaselect_icon",
                        "ui_charaselect_team",
                        "ui_cutin_2p_",
                        "ui_cutin_4p_",
                        "ui_device_command",
                        "ui_entry_controller_",
                        "ui_int_gamesetting",
                        "ui_int_greeting_watching",
                        "ui_int_puzzleleague_rpl_texture",
                        "ui_int_watching_list",
                        "ui_int_watching_lobby_",
                        "ui_internet_",
                        "ui_internet_popup",
                        "ui_kansyou_",
                        "ui_lesson_guide_",
                        "ui_lesson_ingame_",
                        "ui_logo_",
                        "ui_menu_",
                        "ui_minnade_",
                        "ui_minnade_boss",
                        "ui_multi_popup",
                        "ui_option_patch_",
                        "ui_party2p_",
                        "ui_puyo_skin",
                        "ui_replay_operation",
                        "ui_result_04_",
                        "ui_staffroll_",
                        "ui_team_select_1p2p_",
                        "ui_team_select_8p_",
                        "ui_team_select_boss_",
                        "ui_teamsetting_",
                        "ui_title_",
                        "ui_watching",
                        "ui_win_base_2p_",
                        "ui_win_base_4p_",
                    },
                    new [] // Excludes
                    {
                        "ability_duel_2p_",
                        "playericon",
                        "ui_cutin_4p_pos",
                        "ui_menu_bg",
                    }),

                new Matcher(
                    new [] // Dependencies
                    {
                        @"ui\ui_resident",
                        @"ui\ui_system",
                    },
                    new [] // Includes
                    {
                        "ui_cutin_cmn",
                    }),

                new Matcher(
                    new [] // Dependencies
                    {
                        @"ui\adventure\ui_adv_area_common",
                        @"ui\ui_resident",
                    },
                    new [] // Includes
                    {
                        "ui_adv_area_common_loc_",
                        "ui_adv_map_parts_",
                    }),

                new Matcher(
                    new [] // Dependencies
                    {
                        @"ui\ui_resident",
                    },
                    new [] // Includes
                    {
                        "ability_duel_2p_",
                        "adv_stage",
                        "attention",
                        "bigbang_2p_",
                        "classic_2p_",
                        "item_card_",
                        "mix_2p_",
                        "party_2p_",
                        "playericon",
                        "puyo8p_common_",
                        "sousai_bg",
                        "staffroll",
                        "swap_2p_",
                        "swap_4p_",
                        "tetris2p_common_",
                        "tetris8p_common_",
                        "tokoton_list_2p_",
                        "tokoton_uptimer_",
                        "ui_adv_area_common",
                        "ui_adv_area_unlock",
                        "ui_adv_mission",
                        "ui_adv_mz_chara_",
                        "ui_adv_mz_common",
                        "ui_adv_mz_hukidashi",
                        "ui_adv_onepoint_tutorial_texture_",
                        "ui_adv_shortcut_",
                        "ui_adv_unlock_",
                        "ui_bg_change",
                        "ui_bigbang2p_",
                        "ui_bracket",
                        "ui_call_",
                        "ui_color_icon",
                        "ui_com_select",
                        "ui_cutin_4p_pos",
                        "ui_gamesetting_1p2p_",
                        "ui_gamesetting_4p_",
                        "ui_gamesetting_8p_",
                        "ui_gamesetting_cmn",
                        "ui_hint",
                        "ui_hint_texture_",
                        "ui_hitoride_",
                        "ui_index_",
                        "ui_ingame_bg_",
                        "ui_lesson_",
                        "ui_lesson_call_",
                        "ui_lesson_finish_",
                        "ui_lesson_try_",
                        "ui_loading",
                        "ui_menu_bg",
                        "ui_mydata_",
                        "ui_newrecord_",
                        "ui_pause_menu_",
                        "ui_player",
                        "ui_replay_4p",
                        "ui_result_01",
                        "ui_result_02",
                        "ui_result_03",
                        "ui_result_06",
                        "ui_savecheck",
                        "ui_seiseki_",
                        "ui_shop_",
                        "ui_sousai_2p",
                        "ui_sousai_4p",
                        "ui_swap2p_",
                        "ui_sys_window02",
                        "ui_system",
                        "ui_tet_skin",
                        "ui_tetris_notice_",
                        "ui_tournament_",
                        "ui_window_popup",
                    },
                    new [] // Excludes
                    {
                        "item_card_param",
                        "ui_adv_area_common_loc_",
                        "ui_lesson_field",
                        "ui_lesson_guide_",
                        "ui_lesson_ingame_",
                    }),
            };
        }

        public static IEnumerable<string> GetDependencies(string s)
        {
            return matchers
                .Where(x => x.HasMatch(s))
                .SelectMany(x => x.Dependencies);
        }

        private class Matcher
        {
            private readonly IEnumerable<string> dependencies;
            private readonly IEnumerable<string> includes;
            private readonly IEnumerable<string> excludes;

            public Matcher(IEnumerable<string> dependencies,
                IEnumerable<string> includes)
                : this(dependencies, includes, null)
            {
            }

            public Matcher(IEnumerable<string> dependencies,
                IEnumerable<string> includes,
                IEnumerable<string> excludes)
            {
                this.dependencies = dependencies;
                this.includes = includes;
                this.excludes = excludes ?? Enumerable.Empty<string>();
            }

            public IEnumerable<string> Dependencies => dependencies;

            public bool HasMatch(string s) =>
                includes.Any(x => s.StartsWith(x, StringComparison.OrdinalIgnoreCase))
                && excludes.All(x => !s.StartsWith(x, StringComparison.OrdinalIgnoreCase));
        }
    }
}
