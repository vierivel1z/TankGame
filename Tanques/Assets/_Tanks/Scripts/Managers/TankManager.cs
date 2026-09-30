using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

using UnityEngine;
using UnityEngine.InputSystem.Users;
using UnityEngine.UIElements;

namespace Tanks.Complete
{
    [Serializable]
    public class TankManager
    {
        [HideInInspector] public Color m_PlayerColor;
        public Transform m_SpawnPoint;
        [HideInInspector] public int m_PlayerNumber;
        [HideInInspector] public string m_ColoredPlayerText;
        [HideInInspector] public GameObject m_Instance;
        [HideInInspector] public int m_Wins;
        [HideInInspector] public bool m_ComputerControlled;

        public int ControlIndex { get; set; } = 1;


        private TankMovement m_Movement;
        private TankShooting m_Shooting;
        private GameObject m_CanvasGameObject;

        private TankAI m_AI;
        private InputUser m_InputUser;


        public void Setup(GameManager manager)
        {

            if (m_Instance == null)
            {
                Debug.LogError("El TankManager no tiene una instancia de tanque asignada.");
                return;
            }


            Debug.Log("Configurando tanque: " + m_Instance.name);


            // Tank Movement
            m_Movement = m_Instance.GetComponent<TankMovement>();

            if (m_Movement == null)
            {
                Debug.LogError("El tanque " + m_Instance.name + " no tiene TankMovement");
                return;
            }


            // Tank Shooting
            m_Shooting = m_Instance.GetComponent<TankShooting>();

            if (m_Shooting == null)
            {
                Debug.LogError("El tanque " + m_Instance.name + " no tiene TankShooting");
                return;
            }


            // Tank AI
            m_AI = m_Instance.GetComponent<TankAI>();


            // Canvas
            Canvas canvas = m_Instance.GetComponentInChildren<Canvas>();

            if (canvas == null)
            {
                Debug.LogError("El tanque " + m_Instance.name + " no tiene Canvas");
                return;
            }

            m_CanvasGameObject = canvas.gameObject;



            // Input User
            var inputUser = m_Instance.GetComponent<TankInputUser>();

            if (inputUser == null)
            {
                Debug.LogError("El tanque " + m_Instance.name + " no tiene TankInputUser");
                return;
            }

            inputUser.SetNewInputUser(m_InputUser);



            // Configurar control IA
            m_Movement.m_IsComputerControlled = m_ComputerControlled;
            m_Shooting.m_IsComputerControlled = m_ComputerControlled;



            // Configurar jugador
            m_Movement.m_PlayerNumber = m_PlayerNumber;
            m_Movement.ControlIndex = ControlIndex;



            // Añadir IA si corresponde
            if (m_ComputerControlled)
            {
                if (m_AI == null)
                    m_AI = m_Instance.AddComponent<TankAI>();

                m_AI.Setup(manager);
            }



            // Texto del jugador
            m_ColoredPlayerText =
                "<color=#" +
                ColorUtility.ToHtmlStringRGB(m_PlayerColor) +
                ">PLAYER " +
                m_PlayerNumber +
                "</color>";



            // Cambiar color del tanque
            MeshRenderer[] renderers =
                m_Instance.GetComponentsInChildren<MeshRenderer>();


            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];

                for (int j = 0; j < renderer.materials.Length; j++)
                {
                    if (renderer.materials[j].name.Contains("TankColor"))
                    {
                        renderer.materials[j].color = m_PlayerColor;
                    }
                }
            }


            Debug.Log("Tanque configurado correctamente: " + m_Instance.name);
        }



        public void DisableControl()
        {
            m_Movement.enabled = false;
            m_Shooting.enabled = false;

            if (m_ComputerControlled && m_AI != null)
                m_AI.enabled = false;


            if (m_CanvasGameObject != null)
                m_CanvasGameObject.SetActive(false);
        }



        public void EnableControl()
        {
            m_Movement.enabled = true;
            m_Shooting.enabled = true;

            if (m_ComputerControlled && m_AI != null)
                m_AI.enabled = true;


            if (m_CanvasGameObject != null)
                m_CanvasGameObject.SetActive(true);
        }



        public void Reset()
        {
            m_Instance.transform.position = m_SpawnPoint.position;
            m_Instance.transform.rotation = m_SpawnPoint.rotation;


            m_Instance.SetActive(false);
            m_Instance.SetActive(true);
        }
    }



#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(TankManager))]
    public class TankManagerDrawer : PropertyDrawer
    {

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var itemSlot =
                new PropertyField(
                    property.FindPropertyRelative(nameof(TankManager.m_SpawnPoint))
                );

            return itemSlot;
        }

    }

#endif

}