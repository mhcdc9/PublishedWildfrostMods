using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleEditor
{
    //Pretty much SpriteSetterBattle but with 
    internal class SpriteSetterCustom : MapNodeSpriteSetterBattle
    {
        public override void Set(MapNode mapNode)
        {
            if (mapNode.campaignNode.type is CampaignNodeTypeBattle && mapNode.campaignNode.data.TryGetValue("battle", out var value) && value is string assetName)
            {
                BattleData battleData = AddressableLoader.Get<BattleData>("BattleData", assetName);
                if (battleData != null)
                {
                    if (battleData.ModAdded == null)
                    {
                        icon.sprite = mapNode.spriteOptions[0];
                        return;
                    }
                    icon.sprite = battleData.sprite ?? mapNode.spriteOptions[0]; //Change 1
                    icon.transform.Find("Eye")?.gameObject?.SetActive(battleData.sprite == null); //Change 2 (Just for the frost guardian fight)
                    if (battleNameString != null && battleData.nameRef != null) //Change 3
                    {
                        battleNameString.StringReference = battleData.nameRef;
                    }
                }
            }
        }
    }
}
