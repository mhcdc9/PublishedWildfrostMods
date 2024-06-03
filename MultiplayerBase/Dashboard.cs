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

namespace MultiplayerBase
{
    //After the party is finalized, the dashboard will be the main means of communication. It lives in front of the inspection system.
    public class Dashboard : MonoBehaviour
    {

        GameObject background;
        Vector3 defaultPosition = new Vector3(7, 5, 0);
        public static GameObject buttonGroup;
        public static List<Button> buttons = new List<Button>();

        InspectSystem inspectsystem;
        public void Start()
        {
            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.position = defaultPosition;
            background = HelperUI.Background(transform, new Color(0f, 0f, 0f, .75f));
            background.SetActive(false);

            inspectsystem = GameObject.FindObjectOfType<InspectSystem>(true);

            float totalSize = HandlerSystem.friends.Length*(1.2f) - 0.2f;
            buttonGroup = HelperUI.HorizontalGroup("Friend Icons", transform, new Vector2(totalSize,1f));
            buttons.Clear();
            foreach (Friend friend in HandlerSystem.friends)
            {
                buttons.Add(HelperUI.ButtonTemplate(buttonGroup.transform, new Vector2(1, 1), Vector3.zero, "42", friend.Id == HandlerSystem.self.Id ? Color.white : Color.gray ));
                buttons[buttons.Count()-1].onClick.AddListener(() => FriendIconPressed(friend));
            }
            
            HandlerSystem.Initialize();
            Debug.Log("[Multiplayer] Dashboard is set up!");
        }

        public static void AddToButtons(Button button)
        {
            buttons.Add(button);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(buttons.Count(), 1);
        }

        public void FriendIconPressed(Friend friend)
        {
            Debug.Log($"[Multiplayer] Sending Message to {friend.Name}");
            if (InspectSystem.IsActive())
            {
                HandlerInspect.SelectDisp(inspectsystem.inspect);
            }
            else if (HandlerSystem.friendStates[friend] == PlayerState.Battle)
            {
                //HandlerBattle.instance.CreateController();
                HandlerBattle.instance.ToggleViewer(friend);
            }
            else
            {
                HandlerSystem.SendMessage("CHT", friend, Dead.PettyRandom.Range(0f, 1f).ToString());
                HandlerInspect.instance.Clear();
            }
            
        }

        
    }
}
