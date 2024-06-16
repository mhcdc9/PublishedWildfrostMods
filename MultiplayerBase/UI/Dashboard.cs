using JetBrains.Annotations;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using MultiplayerBase.Handlers;
using UnityEngine.SceneManagement;

namespace MultiplayerBase.UI
{
    //After the party is finalized, the dashboard will be the main means of communication. It lives in front of the inspection system.
    //SetInsetAndSizeFromParentEdge
    public class Dashboard : MonoBehaviour
    {

        GameObject background;
        Vector3 buttonPosition = new Vector3(7, 5, 0);
        Vector3 friendIconPosition = new Vector3(-10, 4.5f, 0);
        public static GameObject buttonGroup;
        public static List<Button> buttons = new List<Button>();
        public static GameObject friendIconGroup;
        public static Button visibleButton;
        public static Dictionary<Friend, FriendIcon> friendIcons = new Dictionary<Friend, FriendIcon>();
        

        InspectSystem inspectsystem;
        public void Start()
        {
            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            background = HelperUI.Background(transform, new Color(0f, 0f, 0f, .75f));
            gameObject.AddComponent<RectTransform>();
            background.SetActive(false);
            gameObject.AddComponent<WorldSpaceCanvasSafeArea>().parent = transform.parent.GetComponent<RectTransform>();

            inspectsystem = GameObject.FindObjectOfType<InspectSystem>(true);

            //float totalSize = HandlerSystem.friends.Length*(1.2f) - 0.2f;
            friendIconGroup = HelperUI.VerticalGroup("Friend Icons", transform, new Vector2(0f, 0f));
            friendIconGroup.transform.position = friendIconPosition;
            friendIconGroup.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            friendIconGroup.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0.2f, 1);
            visibleButton = HelperUI.ButtonTemplate(friendIconGroup.transform, new Vector2(1f, 0.3f), Vector3.zero, "", Color.white);
            visibleButton.transform.SetParent(friendIconGroup.transform, false);
            visibleButton.onClick.AddListener(ToggleVisibility);
            foreach (Friend friend in HandlerSystem.friends)
            {
                friendIcons.Add(friend, FriendIcon.Create(transform, Vector2.one, Vector3.zero, friend));
                friendIcons[friend].transform.SetParent(friendIconGroup.transform, false);
            }

            buttonGroup = HelperUI.HorizontalGroup("Friend Icons", transform, new Vector2(0f, 0f));
            buttonGroup.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;
            buttonGroup.transform.position = buttonPosition;
            buttonGroup.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 4, 1);
            buttons.Clear();
            buttons.Add(HelperUI.ButtonTemplate(transform, new Vector2(1, 1), Vector3.zero, "DP", Color.gray)); //Display
            buttons[0].transform.SetParent(buttonGroup.transform, false);
            buttons.Add(HelperUI.ButtonTemplate(transform, new Vector2(1, 1), Vector3.zero, "0", Color.gray)); //Update
            buttons[1].transform.SetParent(buttonGroup.transform, false);
            buttons.Add(HelperUI.ButtonTemplate(transform, new Vector2(1, 1), Vector3.zero, "FT", Color.gray)); //Fetch
            buttons[2].transform.SetParent(buttonGroup.transform, false);

            buttonGroup.SetActive(false);

            HandlerSystem.Initialize();
            Debug.Log("[Multiplayer] Dashboard is set up!");
        }

        public static void AddToButtons(Button button)
        {
            buttons.Add(button);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(buttons.Count(), 1);
        }

        public static void ToggleVisibility()
        {
            foreach (Friend friend in HandlerSystem.friends)
            {
                friendIcons[friend].gameObject.SetActive(!friendIcons[friend].gameObject.activeSelf);
            }
        }

        
    }
}
