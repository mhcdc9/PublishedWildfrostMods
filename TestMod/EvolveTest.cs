using AssortedPatchesCollection;
using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;

namespace TestMod
{
    public class EvolveTest : WildfrostMod
    {
        private List<CardDataBuilder> cards;
        public EvolveTest(string modDirectory) : base(modDirectory)
        {
            /*StatusEffectEvolve ev = ScriptableObject.CreateInstance<StatusEffectEvolve>();
            ev.Autofill("Evolve to Taiga","Evolve <{a}>",this);
            ev.SetEvolution("Kokonut");
            ev.Confirm();*/

            /*StatusEffectEvolve ev2 = ScriptableObject.CreateInstance<StatusEffectEvolveOnUpgrade>();
            ev2.Autofill("Evolve to Tusk", this);
            ev2.SetEvolution("Tusk");
            ev2.Confirm();*/
        }

        protected void CreateModAssets()
        {
            /*KeywordData keywordAimless = (KeywordData) AddressableLoader.groups["KeywordData"].list[2];
            UnityEngine.Debug.Log("[Debug] " + (!keywordAimless).ToString());
            KeywordData keywordEvolve = ScriptableObject.CreateInstance<KeywordData>();
            keywordEvolve.showName = true;
            keywordEvolve.panelSprite = keywordAimless.panelSprite;
            keywordEvolve.panelColor = keywordAimless.panelColor;
            UnityEngine.Localization.Tables.StringTable collection = LocalizationHelper.GetCollection("Card Text", SystemLanguage.English);
            collection.SetString( "Evolve_title", "Evolve");
            UnityEngine.Debug.Log("[Debug] Maybe it fails here?");
            keywordEvolve.titleKey = collection.GetString("Evolve_title"); 
            collection.SetString("Evolve_text", "At the end of battles, this card evolves if certain conditions are met.");
            keywordEvolve.titleKey = collection.GetString("Evolve_text");
            AddressableLoader.AddToGroup<KeywordData>("StatusEffectData", keywordEvolve);*/

            StatusEffectEvolveFromKill ev3 = ScriptableObject.CreateInstance<StatusEffectEvolveFromKill> ();
            ev3.Autofill("Evolve From Consume", "<Evolve>: {a} cards consumed", this);
            ev3.SetConstraints(StatusEffectEvolveFromKill.ReturnTrueIfCardWasConsumed);
            ev3.anyKill = true;
            ev3.SetEvolution("Kokonut");
            ev3.Confirm();

            StatusEffectEvolveFromKill ev4 = ScriptableObject.CreateInstance<StatusEffectEvolveFromKill>();
            ev4.Autofill("Evolve From Boss Kill", "<Evolve>: {a} boss kills", this);
            ev4.SetConstraints(StatusEffectEvolveFromKill.ReturnTrueIfCardTypeIsBossOrMiniboss);
            ev4.SetEvolution("Kokonut");
            ev4.Confirm();

            StatusEffectEvolveFromMoney ev = ScriptableObject.CreateInstance<StatusEffectEvolveFromMoney>();
            ev.Autofill("Evolve from money", "<Evolve>: <{a}> money", this);
            ev.SetEvolution("Kokonut");
            ev.Confirm();

            StatusEffectEvolve ev2 = ScriptableObject.CreateInstance<StatusEffectEvolveEevee>();
            ev2.Autofill("Eevee evolve", "Evolve: ???", this);
            ev2.SetEvolution("f");
            ev2.Confirm();

            StatusEffectEvolve ev0 = ScriptableObject.CreateInstance<StatusEffectEvolve>();
            ev0.Autofill("Evolve To Taiga", "Evolve: <{a}> battle", this);
            ev0.SetEvolution("Kokonut");
            ev0.Confirm();

            cards = new List<CardDataBuilder>()
            {
                new CardDataBuilder(this).CreateUnit("eevee", "Eevee", "TargetModeBasic", "Blood Profile Normal")
                .SetSprites("tyrantrum.png", "tyrantrum BG.png")
                .SetStats(6, 4, 5)
                .SetStartWithEffect(new CardData.StatusEffectStacks(Get<StatusEffectData>("Eevee evolve"), 1 ))
                .IsPet((ChallengeData)null, true)
                .FreeModify(delegate(CardData data)
                    {
                        Debug.LogWarning($"Custom popups before " + data.customData);
                        data.SetCustomData("customPopups", new List<CustomCardPopup>()
                        {
                            new CustomCardPopup(this, "Evolve", title: "Evolve", body: "At the end of battle, this card evolves if certain conditions are met.")
                        });
                        Debug.LogWarning($"Custom popups after " + data.customData);
                    }).SubscribeToAfterAllBuildEvent(
                        delegate(CardData data)
                        {
                            Debug.LogWarning($"Custom popups before " + data.customData);
                            data.SetCustomData("customPopups", new List<CustomCardPopup>()
                            {
                                new CustomCardPopup(this, "Evolve", title: "Evolve", body: "At the end of battle, this card evolves if certain conditions are met.")
                            });
                            Debug.LogWarning($"Custom popups after " + data.customData);
                        }),

                new CardDataBuilder(this).CreateUnit("flareon", "Flareon", "TargetModeBasic", "Blood Profile Normal")
                .SetSprites("kyubey.png", "tyrantrum BG.png")
                .SetStats(6, 4, 5),

                new CardDataBuilder(this).CreateUnit("vaporeon", "Vaporeon", "TargetModeBasic", "Blood Profile Normal")
                .SetSprites("kyubey.png", "tyrantrum BG.png")
                .SetStats(6, 4, 5),

                new CardDataBuilder(this).CreateUnit("jolteon", "Jolteon", "TargetModeBasic", "Blood Profile Normal")
                .SetSprites("kyubey.png", "tyrantrum BG.png")
                .SetStats(6, 4, 5)
            };

        }

        public override void Load()
        {
            CreateModAssets();
            base.Load();
            var globe = this.Get<CardData>("Shwooper");
            globe.traits.Add(new CardData.TraitStacks(Get<TraitData>("Combo"), 1));
            Events.OnCardDataCreated += SetEvoOnSpike;
            Events.OnEntityOffered += KyubeyAppears;
            Events.OnBattleEnd += CheckEvolve;
            Events.PostBattle += DisplayEvolutions;

        }

        public override void Unload()
        {
            Events.OnCardDataCreated -= SetEvoOnSpike;
            Events.OnEntityOffered -= KyubeyAppears;
            Events.OnBattleEnd -= CheckEvolve;
            Events.PostBattle -= DisplayEvolutions;
            base.Unload();
        }

        private void KyubeyAppears(Entity entity)
        {
            if (entity.data.ModAdded != null && entity.data.ModAdded == this && entity.data.cardType.name == "Friendly")
            {
                string[] splitName = entity.data.name.Split('.');
                string trueName = splitName[3];
                Sprite sprite = this.ImagePath("shiny " + trueName + ".png").ToSprite();
                sprite.name = "shiny";
                entity.data.mainSprite = sprite;
                entity.GetComponent<Card>().mainImage.sprite = sprite;
            } 
        }

        private void DebugShiny()
        {
            foreach(CardDataBuilder card in cards)
            {

                string fileName = Path.Combine(ModDirectory, "shiny " + card._data.name + ".png");
                if (!System.IO.File.Exists(fileName))
                {
                    Debug.Log("[Pokefrost] WARNING: Shiny file for " + card._data.name + "does not exist.");
                }
                else
                {
                    Debug.Log("[Pokefrost] " + card._data.name + "has a shiny version.");
                }
            }
        }

        private void DisplayEvolutions(CampaignNode whatever)
        {
            if (StatusEffectEvolve.evolvedPokemonLastBattle.Count > 0)
            {
                References.instance.StartCoroutine(StatusEffectEvolve.EvolutionPopUp(this));
            }
        }

        private void CheckEvolve()
        {
            if (References.Battle.winner != References.Player)
                return;

            CardDataList list = References.Player.data.inventory.deck;
            List<CardData> slateForEvolution = new List<CardData>();
            List<StatusEffectEvolve> evolveEffects = new List<StatusEffectEvolve>();
            Debug.Log("[[Michael]] Searching...");
            foreach (CardData card in list)
            {
                foreach(CardData.StatusEffectStacks s in card.startWithEffects)
                {
                    if (s.data.type == "evolve1")
                    {
                        Debug.Log("[[Michael]] Found One!");
                        s.count -= 1;
                        if (s.count == 0)
                        {
                            if ( ((StatusEffectEvolve)s.data).ReadyToEvolve(card) )
                            {
                                Debug.Log("[[Michael]] Ready for evolution!");
                                slateForEvolution.Add(card);
                                evolveEffects.Add(((StatusEffectEvolve)s.data));
                            }
                            else
                            {
                                s.count += 1;
                                Debug.Log("[[Michael]] Conditions not met.");
                            }
                        }
                    }
                    if (s.data.type == "evolve2")
                    {
                        Debug.Log("[[Michael]] Found One!");
                        if (((StatusEffectEvolve)s.data).ReadyToEvolve(card))
                        {
                            Debug.Log("[[Michael]] Ready for evolution!");
                            slateForEvolution.Add(card);
                            evolveEffects.Add(((StatusEffectEvolve)s.data));
                        }
                        else
                        {
                            Debug.Log("[[Michael]] Conditions not met.");
                        }
                    }
                }
            }

            Debug.Log("[[Michael]] <Drum Roll>");
            int count = slateForEvolution.Count;

            for (int i=0; i<count; i++)
            {
                if (References.Player.data.inventory.deck.RemoveWhere((CardData a) => slateForEvolution[i].id == a.id))
                {
                    Debug.Log("[" + slateForEvolution[i].name + "] Removed From [" + References.Player.name + "] deck");
                    evolveEffects[i].Evolve(this, slateForEvolution[i]);
                }
            }
        }

        private void SetEvoOnSpike(CardData cardData)
        {
            if (cardData.name == "Jagzag")
            {
                Debug.Log("[[Michael]] Jagzag found!");
                StatusEffectData evoTaig = Get<StatusEffectData>("Evolve To Taiga");
                Debug.Log("[[Michael]] Hopefully find Taiga");
                Debug.Log("[[Michael]] " + evoTaig.type);
                cardData.startWithEffects = cardData.startWithEffects.AddItem(new CardData.StatusEffectStacks(evoTaig, 1)).ToArray();
                Debug.Log("[[Michael]] Applied!");
            }
            if (cardData.name == "DemonPet")
            {
                Debug.Log("[[Michael]] DemonPet found!");
                StatusEffectData evoTaig = Get<StatusEffectData>("Evolve From Boss Kill");
                Debug.Log("[[Michael]] Hopefully find Taiga");
                Debug.Log("[[Michael]] " + evoTaig.type);
                cardData.startWithEffects = cardData.startWithEffects.AddItem(new CardData.StatusEffectStacks(evoTaig, 1)).ToArray();
                Debug.Log("[[Michael]] Applied!");
            }
        }

        public override List<T> AddAssets<T, Y>()
        {
            var typeName = typeof(Y).Name;
            UnityEngine.Debug.Log("[Michael] " + typeName + " " + typeof(T).Name);
            if (typeName == nameof(CardData))
            {
                return cards.Cast<T>().ToList();
            }
            return null;
        }

        

        public override string GUID => "mhcdc9.wildfrost.evolve";

        public override string[] Depends => new string[0];

        public override string Title => "Test Evolve";

        public override string Description => "Testing the concept of idea.";


    }
}
