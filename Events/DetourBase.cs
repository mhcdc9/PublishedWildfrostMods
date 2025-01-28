﻿using Deadpan.Enums.Engine.Components.Modding;
using Detours.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

namespace Detours
{
    public abstract class Detour
    {
        #region RunX and Routine Methods
        public delegate IEnumerator ChoiceHandler(FrameChoice choice, Detour @event);

        public static event ChoiceHandler OnChoiceSelected;

        public static IEnumerator InvokeChoiceSelected(FrameChoice choice, Detour @event)
        {
            if (OnChoiceSelected == null) { yield break; }

            Delegate[] delegates = OnChoiceSelected.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                if (delegates[i] is ChoiceHandler ch)
                {
                    yield return ch(choice, @event);
                }
            }
        }
        
        public virtual bool RunChoiceSelected()
        {
            return true;
        }

        public virtual bool HasChoiceSelectedRoutine => false;

        public virtual IEnumerator ChoiceSelectedRoutine()
        {
            yield break;
        }

        public virtual bool HasFrameRoutine => false;

        public virtual bool RunFrame()
        {
            return true;
        }

        public virtual IEnumerator FrameRoutine()
        {
            yield break;
        }
        #endregion RunX and Routine Methods

        public string name = "Nop";
        public static Detour current = null;
        public WildfrostMod mod;

        public string QualifiedName => mod.GUID + name;

        protected CampaignNode node;
        protected string currentFrame = "";
        internal protected FrameChoice selectedChoice;
        public string nextFrame = "";
        public string subFrame = "";
        public bool promptUpdate = false;

        public static readonly string START = "START";
        public static readonly string END = "END";
        public static readonly string SKIP = "SKIP";

        public float Priority => Dead.Random.Range(0f, 1f);

        public bool allowedBeforeBattle = false;

        public virtual bool CheckAllowed(CampaignNode node)
        {
            return true;
        }

        public virtual void Setup(CampaignNode node) { }

        public void SetData(CampaignNode node, string key, object value)
        {
            key = QualifiedName + ": " + key;
            if (node.data.TryGetValue(key, out var data))
            {
                node.data.Remove(key);
            }
            node.data.Add(key, value);
        }

        public bool TryGetData<T>(CampaignNode node, string key, out T value)
        {
            if (node.data.TryGetValue(QualifiedName + ": " + key, out object protoValue))
            {
                value = (T)protoValue;
                return true;
            }
            value = default(T);
            return false;
        }
        public abstract IEnumerator Run(CampaignNode node, string startFrame = "START");
        public virtual bool MissingData(CampaignNode node)
        {
            return false;
        }

        public virtual void PromptUpdate(FrameChoice choice, string frame)
        {
            nextFrame = frame;
            subFrame = "";
            selectedChoice = choice;
            promptUpdate = true;
        }

        public virtual GameObject MakeLayout()
        {
            GameObject obj = new GameObject("Main", new Type[] { typeof(Image) });
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(16f, 10f);
            obj.GetComponent<Image>().color = UI.MapLine;

            obj.GetComponent<RectTransform>()
                .AddVGroup(spacing: 0.2f, color: UI.MapLine)
                    .AddPanel("Title", Vector2.one, new Vector2(16, 1), UI.Map)
                .AddHGroup(spacing: 0.1f, color: UI.Map)
                    .AddPanel("Image", Vector2.zero, new Vector2(6, 16), Color.white)
                    .AddVGroup(spacing: 0.1f, color: UI.Map)
                        .AddPanel("Flavor", Vector2.zero, new Vector2(10, 10), UI.Map)
                        .AddVGroup(spacing: 0.1f, color: UI.Map)
                        .name = "Choices";

            return obj;
        }

        public virtual bool GetString(ref string variableName)
        {
            FieldInfo field = this.GetType().GetField(variableName);
            variableName = field?.GetValue(this)?.ToString() ?? "???";
            return (field != null);
        }
    }

    public class DetourBasic : Detour
    {

        protected GameObject main;
        protected TextPanelSetter titleSetter;
        protected TextPanelSetter descriptionSetter;
        protected ImagePanelSetter imageSetter;
        protected ChoicePanelSetter choiceSetter;

        public DetourBasic(string name, WildfrostMod mod)
        {
            this.name = name;
            this.mod = mod;
            titleSetter = new TextPanelSetter();
            descriptionSetter = new TextPanelSetter
            {
                fontSize = 0.4f,
                color = UI.MapMid,
                padding = 0.5f,
                alignment = TMPro.TextAlignmentOptions.TopLeft,
                target = "Flavor"
            };
            imageSetter = new ImagePanelSetter();
            choiceSetter = new ChoicePanelSetter();
        }


        # region BuilderMethods
        public DetourBasic SetTitle(string title, SystemLanguage lang = SystemLanguage.English)
        {
            StringTable table = LocalizationHelper.GetCollection("UI Text", lang);
            string key = string.Join(".", mod.GUID, name, "title");
            table.SetString(key, title);
            titleSetter.Add((START, table.GetString(key)));
            return this;
        }

        public DetourBasic SetFlavor(SystemLanguage lang, params(string frame, string text)[] pairs)
        {
            StringTable table = LocalizationHelper.GetCollection("UI Text", lang);
            descriptionSetter.Add(
            pairs.Select((p) =>
            {
                string key = string.Join(".", mod.GUID, name, p.frame);
                table.SetString(key, p.text);
                return (p.frame, table.GetString(key));
            })
            .ToArray());
            return this;
        }

        public DetourBasic SetImages(params (string frame, Sprite sprite)[] pairs)
        {
            imageSetter.Add(pairs);
            return this;
        }

        public DetourBasic SetChoices(params (string frame, FrameChoice[] choice)[] pairs)
        {
            choiceSetter.Add(pairs);
            return this;
        }

        public DetourBasic SetFrame(string frame, Sprite sprite = null, string text = null, FrameChoice[] choices = null,  SystemLanguage lang = SystemLanguage.English)
        {
            if (text != null)
            {
                SetFlavor(lang, (frame, text));
            }
            if (sprite != null)
            {
                SetImages((frame, sprite));
            }
            if (choices != null)
            {
                SetChoices((frame, choices));
            }
            return this;
        }
        #endregion BuilderMethods

        public override void Setup(CampaignNode node)
        {
            
        }

        public virtual IEnumerator GenerateUI()
        {
            main = MakeLayout();
            foreach (Transform t in main.GetComponentsInChildren<Transform>())
            {
                if (titleSetter.Assign(t.gameObject) || descriptionSetter.Assign(t.gameObject) || imageSetter.Assign(t.gameObject) || choiceSetter.Assign(t.gameObject))
                {
                    continue;
                }
            }
            yield break;
        }

        public override IEnumerator Run(CampaignNode node, string startFrame)
        {
            this.node = node;
            current = this;
            nextFrame = startFrame;
            yield return GenerateUI();
            main.transform.SetParent(DetourHolder.instance.transform, false);
            while (nextFrame != END && nextFrame != SKIP)
            {
                if (RunFrame() && HasFrameRoutine)
                {
                    yield return FrameRoutine();
                }
                promptUpdate = false;
                currentFrame = nextFrame;
                Update();
                yield return new WaitUntil(() => promptUpdate);
                if (RunChoiceSelected() && HasChoiceSelectedRoutine)
                {
                    yield return ChoiceSelectedRoutine();
                }
                yield return InvokeChoiceSelected(selectedChoice, this);
            }
            DetourHolder.skip = (nextFrame == SKIP);
            End();
        }

        

        internal virtual void Update()
        {
            choiceSetter.Update(currentFrame, subFrame);
            titleSetter.Update(currentFrame, subFrame);
            descriptionSetter.Update(currentFrame, subFrame);
            imageSetter.Update(currentFrame, subFrame);
            
        }

        internal virtual void End()
        {
            subFrame = "";
            titleSetter.End();
            descriptionSetter.End();
            imageSetter.End();
            choiceSetter.End();
            main.Destroy();
        }
    }
}
