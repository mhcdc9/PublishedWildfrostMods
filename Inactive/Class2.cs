using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inactive
{
    internal class Class2
    {
        /*
StatusEffectNextPhase split = ScriptableObject.CreateInstance<StatusEffectNextPhase>();
split.splitCount = 2;
split.name = "HardSplitBossPhase2";
//split.splitOptions = new CardData[] { bam , boozle };
split.animation = ((StatusEffectNextPhase)Get<StatusEffectData>("SplitBossPhase2")).animation;
split.desc = "";
split.descColorHex = "";
split.textKey = new LocalizedString();
split.applyFormatKey = new LocalizedString();
split.keyword = "";
split.hiddenKeywords = new KeywordData[0];
split.iconGroupName = "";
split.eventPriority = 0;
AddressableLoader.AddToGroup<StatusEffectData>("StatusEffectData", split);

cards = new List<CardDataBuilder>
{
    new CardDataBuilder(this).CreateUnit("babySnow", "Baby Snowbo", "TargetModeBasic", "Blood Profile Normal")
    .SetSprites("babySnow2.png", "babySnow BG.png")
    .SetStats(3, 2, 1)
    .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Snow"), 2 ))
    .SetTraits(new CardData.TraitStacks(Get<TraitData>("Wild"),1))
    .WithCardType("Enemy"),

    new CardDataBuilder(this).CreateUnit("snowbo", "Snowbo", "TargetModeBasic", "Blood Profile Normal")
    .SetSprites("snowbo2.png", "snowbo BG.png")
    .SetStats(7, 3, 2)
    .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Snow"), 4 ))
    .SetTraits(new CardData.TraitStacks(Get<TraitData>("Wild"),1))
    .WithCardType("Enemy"),

    new CardDataBuilder(this).CreateUnit("wildSnoolf", "Wild Snoolf", "TargetModeBasic", "Blood Profile Normal")
    .SetSprites("wildSnoolf2.png", "wildSnoolf BG.png")
    .SetStats(7, 3, 2)
    .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Snow"), 2 ))
    .SetTraits(new CardData.TraitStacks(Get<TraitData>("Wild"),1))
    .WithCardType("Enemy"),

    new CardDataBuilder(this).CreateUnit("grouchy", "Grouchy", "TargetModeBasic", "Blood Profile Normal")
    .SetSprites("grouchy2.png", "grouchy BG.png")
    .SetStats(8, 3, 2)
    .SetStartWithEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Increase Attack While Damaged"), 6 ))
    .SetTraits(new CardData.TraitStacks(Get<TraitData>("Wild"),1))
    .WithCardType("Enemy"),

    new CardDataBuilder(this).CreateUnit("winterwyrm", "Winter Wyrm", "TargetModeBasic", "Blood Profile Normal")
    .SetSprites("winterwyrm2.png", "winterwyrm BG.png")
    .SetStats(18, 6, 4)
    .SetStartWithEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("When Hit Gain Attack To Self (No Ping)"), 4 ))
    .SetTraits(new CardData.TraitStacks(Get<TraitData>("Wild"),1))
    .WithCardType("Enemy"),

    new CardDataBuilder(this).CreateUnit("bamboozle", "Bamboozle", "TargetModeBasic", "Blood Profile Normal")
    .SetSprites("bamboozle2.png", "bamboozle BG.png")
    .SetStats(69, 10, 3)
    .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Snow"), 1 ) )
    .SetStartWithEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("ImmuneToSnow"), 1 )
    , new CardData.StatusEffectStacks(Get<StatusEffectData>("Hit All Enemies"), 1 )
    , new CardData.StatusEffectStacks(split, 1) )
    .SetTraits(new CardData.TraitStacks(Get<TraitData>("Wild"),1))
    .WithCardType("Boss"),

    new CardDataBuilder(this).CreateUnit("bam", "Bam", "TargetModeBasic", "Blood Profile Normal")
    .SetSprites("bam2.png", "bam BG.png")
    .SetStats(34, 7, 2)
    .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("ImmuneToSnow"), 1 ))
    .SetTraits(new CardData.TraitStacks(Get<TraitData>("Wild"), 1))
    .WithCardType("BossSmall"),

    new CardDataBuilder(this).CreateUnit("boozle", "Boozle", "TargetModeBasic", "Blood Profile Normal")
    .SetSprites("boozle2.png", "boozle BG.png")
    .SetStats(20, 5, 2)
    .SetStartWithEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("On Turn Apply Attack To Self"), 2))
    .SetAttackEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Snow"), 2))
    .SetTraits(new CardData.TraitStacks(Get<TraitData>("Wild"), 1))
    .WithCardType("BossSmall")
};
Debug.Log("[Michael] cards should not be empty");
Debug.Log(cards.Count.ToString());
*/

        /*string babySnow = Extensions.PrefixGUID("babySnow", this);
string snowbo = Extensions.PrefixGUID("snowbo", this);
string wildSnoolf = Extensions.PrefixGUID("wildSnoolf", this);
string grouchy = Extensions.PrefixGUID("grouchy", this);
string winterwyrm = Extensions.PrefixGUID("winterwyrm", this);
string bamboozle = Extensions.PrefixGUID("bamboozle", this);
string bam = Extensions.PrefixGUID("bam", this);
string boozle = Extensions.PrefixGUID("boozle", this);
BattleDataEditor bde = new BattleDataEditor(this, "Harder Bamboozle",0)
.SetSprite(this.ImagePath("HardBamboozle.png").ToSprite())
.SetNameRef("The Real Bamboozle Fight!")
.PossibleEnemies(bamboozle, winterwyrm, snowbo, babySnow, grouchy, wildSnoolf)
.StartWavePoolData(0,"Wave 1")
.ConstructWaves(3,-1, "012", "021", "013", "031" )
.StartWavePoolData(1, "Wave 2-3")
.ConstructWaves(3, 1,"12", "22", "34", "25" )
.AddBattleToLoader()
.RegisterBattle(2, mandatory : true);
Debug.Log("[Michael] Finalized.");
StatusEffectNextPhase split = (StatusEffectNextPhase)(Get<StatusEffectData>("HardSplitBossPhase2"));
CardData card2 = Get<CardData>(bam);
CardData card3 = Get<CardData>(boozle);
split.splitOptions = new CardData[] { card2, card3 };*/
        public override void Load()
        {
            base.Load();
            new BattleDataEditor(this, "Spare Shells")
            .SetSprite(this.ImagePath("Spare Shells.png").ToSprite())
            .SetNameRef("The Other Shelled Husks")
            .PossibleEnemies("Conker", "ShellWitch", "Pecan", "Prickle", "Bolgo")
            .StartWavePoolData(0, "The first of the husks")
            .ConstructWaves(3, 0, "011", "012")
            .StartWavePoolData(1, "Some more husks")
            .ConstructWaves(3, 1, "201", "021", "031", "301")
            .StartWavePoolData(2, "Bolgo is here!")
            .ConstructWaves(3, 9, "421", "401")
            .AddBattleToLoader().RegisterBattle(0, mandatory: true);
        }
    }
}
