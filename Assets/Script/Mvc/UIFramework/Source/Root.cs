namespace Client.UIFramework.UI
{ 

    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;

    public class Root : MonoBehaviour {

        private static Root m_Instance = null;
        public Transform root;
        public Camera uiCamera;
        public Transform fixedRoot;
        public Transform normalRoot;
        public Transform popupRoot;


        public static Root Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    InitRoot();
                }
                return m_Instance;
            }
        }

        static void InitRoot()
        {
            GameObject go = new GameObject("Root");
            go.layer = LayerMask.NameToLayer("UI");
            m_Instance = go.AddComponent<Root>();
            go.AddComponent<RectTransform>();
            m_Instance.root = go.transform;

            Canvas can = go.AddComponent<Canvas>();
            can.renderMode = RenderMode.ScreenSpaceCamera;
            can.pixelPerfect = false;
            GameObject camObj = new GameObject("UICamera");
            camObj.layer = LayerMask.NameToLayer("UI");
            camObj.transform.parent = go.transform;
            camObj.transform.localPosition = new Vector3(0, 0, -100f);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Depth;
            cam.orthographic = true;
            cam.farClipPlane = 200f;
            can.worldCamera = cam;
            m_Instance.uiCamera = cam;
            cam.cullingMask = 1 << 5;
            cam.nearClipPlane = -50f;
            cam.farClipPlane = 50f;

            camObj.AddComponent<AudioListener>();
            camObj.AddComponent<GUILayer>();

            CanvasScaler cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(Bind.ScreenWidth, Bind.ScreenHeight);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;


            GameObject subRoot = CreateSubCanvasForRoot(go.transform, 250);
            subRoot.name = "FixedRoot";
            m_Instance.fixedRoot = subRoot.transform;

            subRoot = CreateSubCanvasForRoot(go.transform, 0);
            subRoot.name = "NormalRoot";

            m_Instance.normalRoot = subRoot.transform;

            subRoot = CreateSubCanvasForRoot(go.transform, 500);
            subRoot.name = "PopupRoot";
            m_Instance.popupRoot = subRoot.transform;

        }

        static GameObject CreateSubCanvasForRoot(Transform root, int sort)
        {
            GameObject go = new GameObject("canvas");
            go.transform.parent = root;
            go.layer = LayerMask.NameToLayer("UI");

            Canvas can = go.AddComponent<Canvas>();
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 0);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            can.overrideSorting = true;
            can.sortingOrder = sort;

            go.AddComponent<GraphicRaycaster>();

            return go;
        }

        void OnDestroy()
        {
            m_Instance = null;
        }
    }

}
