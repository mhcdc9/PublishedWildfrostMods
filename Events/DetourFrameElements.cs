using Deadpan.Enums.Engine.Components.Modding;
using Detours;
using Mono.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using Detours.Misc;

namespace Detours
{
    public abstract class PanelSetter
    {
        public string target = "panel";
        public abstract bool Assign(GameObject panel);
        public abstract void Update(string frame, string subframe);
        public abstract void End();

        public static string PostProcess(string s, Color highlight)
        {
            int state = 0;
            int startIndex = -1;
            int endIndex = -1;
            for(int i=s.Length-1; i >= 0; i--)
            {
                if (s[i] == '}')
                {
                    state = 1;
                    endIndex = i;
                }
                else if(state == 1 && s[i] == '{')
                {
                    startIndex = i;
                    int highlighted = 0;
                    if (startIndex > 0 && endIndex < s.Length - 1)
                    {
                        string style = string.Join("",s[startIndex - 1], s[endIndex + 1]);
                        if (style == "<>") { highlighted = 1; }
                        if (style == "[]") { highlighted = 2; }
                    }
                    string s2 = s.Substring(startIndex, endIndex - startIndex + 1);
                    string s3 = s2.Substring(1, s2.Length-2);
                    if (Detour.current.GetString(ref s3))
                    {
                        if (highlighted == 1)
                        {
                            s3 = $"<color=#{highlight.ToHexRGB()}>" + s3 + "</color>";
                            startIndex--;
                            endIndex++;
                        }
                        else if (highlighted == 1)
                        {
                            s3 = $"<color=#{highlight.ToHexRGB()}>[" + s3 + "]</color>";
                            startIndex--;
                            endIndex++;
                        }
                        s = s.Substring(0, startIndex) + s3 + s.Substring(endIndex + 1);
                        
                        i = s.Length;
                    }
                    state = 0;
                }
            }
            return s;
        }
    }
    public class TextPanelSetter : PanelSetter
    {
        public float fontSize = 0.8f;
        public Color color = UI.MapLine;
        public Color highlight = new Color(0, 0, 0);
        public float padding = 0.2f;
        public TMPro.TextAlignmentOptions alignment = TMPro.TextAlignmentOptions.Center;
        public Dictionary<string, LocalizedString> texts = new Dictionary<string, LocalizedString>();

        protected GameObject panel;
        protected TextMeshProUGUI textbox;
        protected string lastFrame = "";
        protected string lastText = "";

        public TextPanelSetter()
        {
            target = "Title";
        }

        public void Add(params (string frame, LocalizedString text)[] pairs)
        {
            foreach (var pair in pairs)
            {
                texts[pair.frame] = pair.text;
            }
        }

        public override bool Assign(GameObject panel)
        {
            if (!panel.name.Contains(target))
            {
                return false;
            }
            this.panel = panel;
            panel.AddComponent<RectUpdater>().tps = this;
            if (target == "Title")
            {
                panel.GetComponent<RectUpdater>().mod = DetourHolder.current.mod;
            }
            textbox = panel.GetComponent<RectTransform>().WithText(fontSize, Vector3.zero, padding * Vector2.one, "", color, alignment).GetComponent<TextMeshProUGUI>();
            textbox.GetComponent<RectTransform>().sizeDelta = panel.GetComponent<RectTransform>().sizeDelta - padding * Vector2.one;
            textbox.transform.localPosition = Vector3.zero;
            lastText = texts[Detour.START].GetLocalizedString();
            lastText = PostProcess(lastText,highlight);
            return true;
        }
        public override void Update(string frame, string subframe)
        {
            string combined = frame + "|" + subframe;
            if (combined != lastFrame)
            {
                lastFrame = combined;
                lastText = texts.ContainsKey(combined) ? texts[combined].GetLocalizedString() : (texts.ContainsKey(frame) ? texts[frame].GetLocalizedString() : lastText);
            }

            textbox.SetText(PostProcess(lastText, highlight));
        }



        public override void End()
        {
            lastFrame = "";
        }

        private class RectUpdater : UIBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            internal TextPanelSetter tps;
            internal WildfrostMod mod;

            bool popped = false;

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (mod != null && !popped)
                {
                    Vector3 v = tps.textbox.textInfo.characterInfo[tps.textbox.textInfo.characterCount - 1].bottomRight;
                    Vector3 w = tps.textbox.transform.TransformPoint(v);
                    Vector3 right = tps.textbox.transform.InverseTransformPoint(w);
                    float farRight = tps.textbox.GetComponent<RectTransform>().offsetMax.x;
                    CardPopUp.AssignTo(tps.textbox.GetComponent<RectTransform>(), right.x/farRight + 0.1f, 0);
                    CardPopUp.AddPanel("Mod Added", "<color=#FFFFFF><size=0.2>Mod Added</size></color>", $"<color=#FFCA57><size=0.3>{mod.Title}</size></color>");
                    popped = true;
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (mod != null && popped)
                {
                    CardPopUp.RemovePanel("Mod Added");
                    popped = false;
                }
            }

            protected override void OnRectTransformDimensionsChange()
            {
                tps.textbox.GetComponent<RectTransform>().sizeDelta = tps.panel.GetComponent<RectTransform>().sizeDelta - tps.padding * Vector2.one;
                tps.textbox.transform.localPosition = Vector3.zero;
            }
        }
    }

    public class ImagePanelSetter : PanelSetter
    {
        public Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        protected Image image;
        protected string lastFrame = "";
        protected Sprite lastSprite;

        public ImagePanelSetter()
        {
            target = "Image";
        }

        public void Add(params (string frame, Sprite sprite)[] pairs)
        {
            foreach (var pair in pairs)
            {
                sprites[pair.frame] = pair.sprite;
            }
        }

        public override bool Assign(GameObject panel)
        {
            if (!panel.name.Contains(target))
            {
                return false;
            }
            image = panel.GetComponent<Image>();
            return true;
        }
        public override void Update(string frame, string subframe)
        {
            string combined = frame + "|" + subframe;
            if (combined != lastFrame)
            {
                lastFrame = combined;
                lastSprite = sprites.ContainsKey(combined) ? sprites[combined] : (sprites.ContainsKey(frame) ? sprites[frame] : lastSprite);
            }

            image.sprite = lastSprite;
        }

        public override void End()
        {
            lastFrame = "";
        }
    }

    public class ChoicePanelSetter : PanelSetter
    {
        protected GameObject panel;
        protected string lastFrame = "";
        protected Dictionary<string, FrameChoice[]> choices = new Dictionary<string, FrameChoice[]>();

        public ChoicePanelSetter()
        {
            target = "Choices";
        }

        public void Add(params (string frame, FrameChoice[] choice)[] pairs)
        {
            foreach (var pair in pairs)
            {
                choices[pair.frame] = pair.choice;
            }
        }

        public override bool Assign(GameObject panel)
        {
            if (!panel.name.Contains("Choices"))
            {
                return false;
            }
            this.panel = panel;
            return true;
        }
        public override void Update(string frame, string subframe)
        {
            string combined = frame + "|" + subframe;
            if (combined != lastFrame)
            {
                lastFrame = combined;
                for (int i = panel.transform.childCount - 1; i >= 0; i--)
                {
                    panel.transform.GetChild(i).gameObject.SetActive(false);
                }
            }
            FrameChoice[] array = choices.ContainsKey(combined) ? choices[combined] : (choices.ContainsKey(frame) ? choices[frame] : new FrameChoice[0]);
            foreach (var choice in array)
            {
                choice.Activate(panel);
            }
        }

        public override void End()
        {
            lastFrame = "";
        }

    }

    #region EventChoice
    public class FrameChoice
    {
        public string name;
        protected LocalizedString description;
        protected string toFrame;
        protected Button button;
        protected bool visibleIfDisabled = true;
        protected TextMeshProUGUI textbox;
        protected Color color = Color.white;
        protected Color highlight = UI.HighlightYellow;

        protected ChoiceCondition[] conditions;

        public FrameChoice(string name, string description, string toFrame, WildfrostMod mod)
        {
            StringTable table = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English);
            string key = string.Join(".", mod.GUID, name, "text");
            table.SetString(key, description);

            this.name = name;
            this.description = table.GetString(key);
            this.toFrame = toFrame;
        }

        public FrameChoice AddConditions(params ChoiceCondition[] conditions)
        {
            this.conditions = conditions;
            return this;
        }

        public FrameChoice SetVisibleIfDisabled(bool value)
        {
            visibleIfDisabled = value;
            return this;
        }

        public FrameChoice SetColor(Color color)
        {
            this.color = color;
            return this;
        }

        public FrameChoice SetHighlight(Color color)
        {
            this.highlight = color;
            return this;
        }

        public virtual void Start(GameObject panel)
        {
            button = panel.transform.WithButton(name, Vector3.zero, new Vector2(7, 0.8f), UI.MenuGray, Select ).GetComponent<Button>();
            button.GetComponent<RectTransform>().WithText(0.4f, Vector3.zero, Vector2.zero, description.GetLocalizedString(), Color.white, TextAlignmentOptions.Left);
            textbox = button.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            textbox.color = color;
            button.gameObject.AddComponent<RectUpdater>().Set(button, textbox, UI.MenuGray, color);
        }

        public virtual void Select()
        {
            Detour.current.PromptUpdate(this, toFrame);
        }

        public virtual void Activate(GameObject panel)
        {
            if (button == null)
            {
                Start(panel);
            }
            string desc = description.GetLocalizedString();
            bool enabled = CheckConditions(ref desc);
            button.gameObject.SetActive(visibleIfDisabled || enabled);
            button.enabled = enabled;
            textbox.GetComponent<RectTransform>().sizeDelta = button.GetComponent<RectTransform>().sizeDelta - new Vector2(0.45f, 0.1f);
            textbox.text = PanelSetter.PostProcess(desc, highlight);
        }

        public virtual string GetContext(int index)
        {
            if (conditions != null && conditions.Length > index)
            {
                return conditions[index].GetContext();
            }
            return "";
        }
        public virtual bool CheckConditions(ref string failDesc)
        {
            if (conditions == null)
            {
                return true;
            }
            for(int i=0; i<conditions.Length; i++)
            {
                ChoiceCondition cond = conditions[i];
                if (!cond.Check())
                {
                    failDesc = cond.GetText();
                    return false;
                }
                else
                {
                    string context = cond.GetContext();
                    failDesc = failDesc.Replace("[{c" + i + "}]", $"<color=#{highlight.ToHexRGB()}>[{context}]</color>");
                    failDesc = failDesc.Replace("<{c" + i + "}>", $"<color=#{highlight.ToHexRGB()}>{context}</color>");
                    failDesc = failDesc.Replace("{c" + i + "}", cond.GetContext());
                }
            }
            return true;
        }

        public virtual void Deactivate()
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        protected class RectUpdater : UIBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            internal Button button;
            internal TextMeshProUGUI textbox;
            internal Color buttonColor;
            internal Color textColor;

            public void Set(Button button, TextMeshProUGUI textbox, Color buttonColor, Color textColor)
            {
                this.button = button;
                this.textbox = textbox;
                this.buttonColor = buttonColor;
                this.textColor = textColor;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if(button.enabled)
                {
                    button.GetComponent<Image>().color = textColor;
                    textbox.color = buttonColor;
                }
            }

            protected override void OnRectTransformDimensionsChange()
            {
                button.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = button.GetComponent<RectTransform>().sizeDelta - new Vector2(0.5f, 0.1f);
                button.transform.GetChild(0).transform.localPosition = Vector3.zero;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                button.GetComponent<Image>().color = buttonColor;
                textbox.color = textColor;
            }
        }
    }

    public class FrameChoiceInputField : FrameChoice
    {
        public string saved;
        public FrameChoiceInputField(string name, string description, string toFrame, WildfrostMod mod) : base(name, description, toFrame, mod)
        {
        }

        private void Deselect(string s)
        {
            button.enabled = true;
            textbox.enabled = true;
            button.GetComponentInChildren<TMP_InputField>().gameObject.Destroy();
        }

        private void Submit(string s)
        {
            saved = s;
            button.enabled = true;
            textbox.enabled = true;
            button.GetComponentInChildren<TMP_InputField>().gameObject.Destroy();
            base.Select();
        }

        public override void Select()
        {
            button.enabled = false;
            textbox.enabled = false;
            RectTransform rectTransform = button.transform as RectTransform;
            TMP_InputField input = rectTransform.WithInputField(Vector3.zero, rectTransform.sizeDelta - new Vector2(0.5f, 0.1f), "Press [Enter] to confirm", 0.4f/(rectTransform.sizeDelta.y - 0.1f));
            button.gameObject.GetComponent<RectUpdater>().OnPointerExit(null);
            input.onSubmit.AddListener(Submit);
            input.onDeselect.AddListener(Deselect);
        }
    }

    public class FrameChoiceSelectCard : FrameChoice
    {
        Func<CardData[]> selectionFunction;
        public CardData selected;

        public FrameChoiceSelectCard(string name, string description, string toFrame, WildfrostMod mod, Func<CardData[]> selectFrom) : base(name, description, toFrame, mod)
        {
            selectionFunction = selectFrom;
        }

        public override void Start(GameObject panel)
        {
            selected = null;
            base.Start(panel);
        }

        public override void Select()
        {
            CardData[] selection = selectionFunction();
            DetourHolder.StartShowCardGrid(selection, Select);
        }

        public virtual void Select(Entity entity)
        {
            selected = entity.data;
            DetourHolder.HideCardGrid();
            base.Select();
        }

    }

#endregion EventChoice

    #region ChoiceConditions
    public abstract class ChoiceCondition
    {
        protected string name;
        protected LocalizedString description;

        public ChoiceCondition(WildfrostMod mod, string name, string description, SystemLanguage lang = SystemLanguage.English)
        {
            StringTable table = LocalizationHelper.GetCollection("UI Text", lang);
            string key = string.Join("_", mod.GUID, name, "condition");
            table.SetString(key, description);
            this.name = name;
            this.description = table.GetString(key);
        }

        public abstract bool Check();

        public virtual string GetText()
        {
            return "<color=#ff4040>" + description.GetLocalizedString() + "</color>";
        }

        public virtual string GetContext()
        {
            return "";
        }
    }


    public class ChoiceConditionCustom : ChoiceCondition
    {
        Func<bool> condition;
        
        public ChoiceConditionCustom(WildfrostMod mod, string name, string description,  Func<bool> condition, SystemLanguage lang = SystemLanguage.English) : base(mod, name, description, lang)
        {
            this.condition = condition;
        }

        public override bool Check()
        {
            return condition();
        }
    }

    public class ChoiceConditionInDeck : ChoiceCondition
    {
        TargetConstraint[] constraints;

        //Extremely Volatile!!! 
        private CardData cardData;

        public ChoiceConditionInDeck(WildfrostMod mod, string name, string description, TargetConstraint[] constraints, SystemLanguage lang = SystemLanguage.English) : base(mod, name, description, lang)
        {
            this.constraints = constraints;
        }

        public override bool Check()
        {
            foreach(CardData card in References.PlayerData.inventory.deck)
            {
                foreach(TargetConstraint constraint in constraints)
                {
                    if (Satisfy(card))
                    {
                        cardData = card;
                        return true;
                    }
                }
            }
            cardData = null;
            return false;
        }

        public bool Satisfy(CardData card)
        {
            foreach(TargetConstraint constraint in constraints)
            {
                if(!constraint.Check(card))
                {
                    return false;
                }
            }
            return true;
        }

        public CardData GetCardName()
        {
            foreach (CardData card in References.PlayerData.inventory.deck)
            {
                foreach (TargetConstraint constraint in constraints)
                {
                    if (Satisfy(card))
                    {
                        return card;
                    }
                }
            }
            return null;
        }

        public override string GetContext()
        {
            return cardData?.title ?? "";
        }
    }
    #endregion ChoiceConditions
}
