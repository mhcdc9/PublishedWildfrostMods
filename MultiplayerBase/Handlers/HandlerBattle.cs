using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerBase.Handlers
{
    public class HandlerBattle : MonoBehaviour
    {
        public static HandlerBattle instance;

        GameObject background;

        private CardControllerSelectCard cc;
        int lanes = 2;
        OtherCardViewer[] playerLanes;
        OtherCardViewer[] enemyLanes;

        Vector3 defaultPosition = new Vector3(0, 0, 10);
        Vector3 viewerPosition = new Vector3(0, 0, 2);

        protected void Start()
        {
            //Events.OnSceneUnload += DisableController;
            
            instance = this;

            cc = gameObject.AddComponent<CardControllerSelectCard>();
            cc.pressEvent = new UnityEventEntity();
            cc.hoverEvent = new UnityEventEntity();
            cc.unHoverEvent = new UnityEventEntity();

            transform.SetParent(GameObject.Find("CameraContainer/CameraMover/MinibossZoomer/CameraPositioner/CameraPointer/Animator/Rumbler/Shaker/InspectSystem").transform);
            transform.SetAsFirstSibling();
            transform.position = defaultPosition;

            background = HelperUI.Background(transform, new Color(1f, 1f, 1f, 0.75f));

            playerLanes = new OtherCardViewer[lanes];
            for (int i=0; i<playerLanes.Length; i++)
            {
                playerLanes[i] = HelperUI.OtherCardViewer($"Player Row {i + 1}", background.transform, cc);
                playerLanes[i].transform.localPosition = new Vector3(-0.75f, -0.15f + 0.3f * i, 0);
            }

            enemyLanes = new OtherCardViewer[lanes];
            for (int i = 0; i < enemyLanes.Length; i++)
            {
                enemyLanes[i] = HelperUI.OtherCardViewer($"Enemy Row {i + 1}", background.transform, cc);
                enemyLanes[i].transform.localPosition = new Vector3(0.25f, -0.15f + 0.3f * i, 0);
            }
            background.SetActive(false);
        }

        public void ToggleViewer()
        {
            if (background.activeSelf)
            {
                background.SetActive(false);
                background.transform.SetParent(transform);
            }
            else
            {
                background.transform.SetParent(GameObject.Find("Battle/Canvas/CardController/Board/Canvas").transform);
                background.transform.localPosition = defaultPosition;
                background.SetActive(true);
                LeanTween.moveLocal(background, viewerPosition, 0.5f).setEase(LeanTweenType.easeInOutQuart);
            }
            
        }

        public void CreateController()
        {
            if (cc == null)
            {
                cc = gameObject.AddComponent<CardControllerSelectCard>();
                cc.pressEvent = new UnityEventEntity();
                cc.hoverEvent = new UnityEventEntity();
                cc.unHoverEvent = new UnityEventEntity();
                foreach (OtherCardViewer ocv in playerLanes) 
                {
                    ocv.AssignController(cc);
                }
                foreach (OtherCardViewer ocv in enemyLanes)
                {
                    ocv.AssignController(cc);
                }
            }
        }

        private void DisableController(Scene scene)
        {
            cc.Destroy();
        }
    }
}
