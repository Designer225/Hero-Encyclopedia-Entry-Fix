using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;

namespace EncyclopediaEntryFix
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            new Harmony("d225.bannerlord.encyclopediaheropagefix").PatchAll();
        }
    }

    [HarmonyPatch(typeof(DefaultEncyclopediaHeroPage), nameof(DefaultEncyclopediaHeroPage.GetListItems))]
    public static class GetListItems_Patch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            int stage = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (stage == 0 && codes[i].opcode == OpCodes.Callvirt
                    && codes[i].operand is MethodInfo && (codes[i].operand as MethodInfo) == AccessTools.PropertyGetter(typeof(Hero), nameof(Hero.Age)))
                {
                    codes[i].operand = AccessTools.PropertyGetter(typeof(Hero), nameof(Hero.IsChild));
                    stage++;
                }
                else if (stage == 1 && codes[i].opcode == OpCodes.Ldc_R4)
                {
                    codes[i] = new CodeInstruction(OpCodes.Nop);
                    stage++;
                }
                else if (stage == 2 && codes[i].opcode == OpCodes.Blt_Un_S)
                {
                    codes[i].opcode = OpCodes.Brtrue;
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }
}
