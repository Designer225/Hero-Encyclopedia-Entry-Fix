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
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.Engine;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;

namespace EncyclopediaEntryFix
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            var harmony = new Harmony("d225.bannerlord.encyclopediaheropagefix");
            harmony.PatchAll();
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

    [HarmonyPatch(typeof(HeroViewModel), nameof(HeroViewModel.FillFrom))]
    public static class HeroViewModel_FillFromPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ble)
                {
                    codes.RemoveRange(0, i+1);
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(CharacterViewModel), nameof(CharacterViewModel.FillFrom))]
    public static class CharacterViewModel_FillFromPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ble)
                {
                    codes.RemoveRange(0, i+1);
                    break;
                }
            }

            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(HeroVM), MethodType.Constructor, new Type[] { typeof(Hero), typeof(bool) } )]
    public static class HeroVM_Constructor_Patch
    {
        public static void Postfix(HeroVM __instance, Hero hero)
        {
            if (hero != null)
                __instance.IsChild = hero.Age < 3;
            __instance.RefreshValues();
        }
    }

    [HarmonyPatch(typeof(ImageIdentifierTextureProvider), nameof(ImageIdentifierTextureProvider.CreateImageWithId))]
    public static class CreateImageWithIdPatch
    {
        // The original method is async, but the problem spot is not, so...
        public static void Postfix(ImageIdentifierTextureProvider __instance, string id, int typeAsInt, string additionalArgs, ref CharacterCode ____characterCode)
        {
            if (typeAsInt == 5)
            {
                CharacterCode from = CharacterCode.CreateFrom(id);
                ____characterCode = from;

                if (TaleWorlds.Core.FaceGen.GetMaturityTypeWithAge(from.BodyProperties.Age) <= BodyMeshMaturityType.Child && from.BodyProperties.Age >= 3)
                    TableauCacheManager.Current.BeginCreateCharacterTexture(from, new Action<Texture>(__instance.OnTextureCreated), __instance.IsBig);
            }
        }

        private static void OnTextureCreated(this ImageIdentifierTextureProvider instance, Texture texture)
        {
            AccessTools.Method(typeof(ImageIdentifierTextureProvider), "OnTextureCreated").Invoke(instance, new object[] { texture });
        }
    }

    [HarmonyPatch(typeof(ClanLordItemVM), nameof(ClanLordItemVM.UpdateProperties))]
    public static class ClanLordItemVMUpdatePropertiesPatch
    {
        public static void Postfix(ClanLordItemVM __instance, Hero ____hero)
        {
            __instance.IsChild = ____hero.Age < 3; // seriously TaleWorlds don't you think a picture of a baby is weird for a 3+ year old?
        }
    }
}
