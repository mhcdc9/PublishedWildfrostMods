using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace TestMod
{
    internal class StatusEffectEvolve : StatusEffectData
    {
        public string evolutionCardName;
        public static List<string> evolvedPokemonLastBattle = new List<string>(3);
        public static List<string> pokemonEvolvedIntoLastBattle = new List<string>(3);
        public virtual void Autofill(string n, string descrip, WildfrostMod mod)
        {
            UnityEngine.Debug.Log("[Michael] Wonder if it worked?");
            targetConstraints = new TargetConstraint[0];
            name = n;
            UnityEngine.Localization.Tables.StringTable collection = LocalizationHelper.GetCollection("Card Text", SystemLanguage.English);
            collection.SetString(name + "_text", descrip);
            textKey = collection.GetString(name + "_text");
            ModAdded = mod;
            textInsert = "Who knows what this does.";
            applyFormat = "";
            type = "evolve1";
            canBeBoosted = false;
        }


        public virtual void SetEvolution(string cn)
        {
            evolutionCardName = cn;
        }

        public virtual void Confirm()
        {
            AddressableLoader.AddToGroup<StatusEffectData>("StatusEffectData", this);
        }

        public virtual bool ReadyToEvolve(CardData cardData)
        {
            return true;
        }

        public virtual void Evolve(WildfrostMod mod, CardData preEvo)
        {
            CardData evolution = mod.Get<CardData>(evolutionCardName).Clone();
            if (preEvo.mainSprite.name == "shiny")
            {
                string[] splitName = evolutionCardName.Split('.');
                string trueName = splitName[splitName.Length-1];
                Sprite sprite = mod.ImagePath("shiny " + trueName + ".png").ToSprite();
                sprite.name = "shiny";
                evolution.mainSprite = sprite;
            }
            foreach(CardUpgradeData upgrade in preEvo.upgrades)
            {
                if (upgrade.CanAssign(evolution))
                {
                    upgrade.Assign(evolution);
                }
            }
            Card card = CardManager.Get(evolution, null, References.Player, false, true);

            //Checks for renames
            CardData basePreEvo = mod.Get<CardData>(preEvo.name);
            if (basePreEvo.title != preEvo.title)
            {
                evolution.forceTitle = preEvo.title;
                if (card != null)
                {
                    card.SetName(preEvo.title);
                    UnityEngine.Debug.Log("[Pokefrost] renamed evolution to " + preEvo.title);
                }
                Events.InvokeRename(card.entity, preEvo.title);
            }


            Debug.Log("[[Michael]] Evolution Found?");
            References.Player.data.inventory.deck.Add(card.entity.data);
            Debug.Log("[[Michael]] Added!");
            Events.InvokeEntityChosen(card.entity);
            Events.InvokeEntityShowUnlocked(card.entity);
            evolvedPokemonLastBattle.Add(preEvo.name);
            pokemonEvolvedIntoLastBattle.Add(evolutionCardName);

        }

        public static IEnumerator EvolutionPopUp(WildfrostMod mod)
        {
            yield return SceneManager.WaitUntilUnloaded("CardFramesUnlocked");
            yield return SceneManager.Load("CardFramesUnlocked", SceneType.Temporary);
            CardFramesUnlockedSequence sequence = GameObject.FindObjectOfType<CardFramesUnlockedSequence>();
            TextMeshProUGUI titleObject = sequence.GetComponentInChildren<TextMeshProUGUI>(true);
            if (evolvedPokemonLastBattle.Count == 1)
            {
                string preEvo = mod.Get<CardData>(evolvedPokemonLastBattle[0]).title;
                string evo = mod.Get<CardData>(pokemonEvolvedIntoLastBattle[0]).title;
                titleObject.text = "<size=0.55>What? <#ff0>"+preEvo+"</color> has\n evolved into <#ff0>"+evo+"</color>!";
            }
            else
            {
                titleObject.text = "<size=0.55>What? <#ff0>"+ evolvedPokemonLastBattle.Count + "</color> Pokemon have evolved!";
            }
            yield return sequence.StartCoroutine("CreateCards", pokemonEvolvedIntoLastBattle.ToArray());
            yield return SceneManager.WaitUntilUnloaded("CardFramesUnlocked");
            evolvedPokemonLastBattle.Clear();
            pokemonEvolvedIntoLastBattle.Clear();
        }
    }
}
