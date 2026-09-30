using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;


namespace Tanks.Complete
{
    public class GameManager : MonoBehaviour
    {

        // Which state the game is currently in
        public enum GameState
        {
            MainMenu,
            Game
        }


        // Data about the selected tanks passed from the menu to the GameManager
        public class PlayerData
        {
            public bool IsComputer;
            public Color TankColor;
            public GameObject UsedPrefab;
            public int ControlIndex;
        }


        public int m_NumRoundsToWin = 5;
        public float m_StartDelay = 3f;
        public float m_EndDelay = 3f;

        public CameraControl m_CameraControl;


        [Header("Tanks Prefabs")]
        public GameObject m_Tank1Prefab;
        public GameObject m_Tank2Prefab;
        public GameObject m_Tank3Prefab;
        public GameObject m_Tank4Prefab;


        [FormerlySerializedAs("m_Tanks")]
        public TankManager[] m_SpawnPoints;


        private GameState m_CurrentState;

        private int m_RoundNumber;

        private WaitForSeconds m_StartWait;
        private WaitForSeconds m_EndWait;

        private TankManager m_RoundWinner;
        private TankManager m_GameWinner;


        private PlayerData[] m_TankData;

        private int m_PlayerCount = 0;

        private TextMeshProUGUI m_TitleText;



        private void Start()
        {

            m_CurrentState = GameState.MainMenu;


            var textRef = FindAnyObjectByType<MessageTextReference>(FindObjectsInactive.Include);


            if (textRef == null)
            {
                Debug.LogError(
                    "You need to add the Menus prefab in the scene to use the GameManager!"
                );

                return;
            }


            m_TitleText = textRef.Text;

            m_TitleText.text = "";


            if (m_Tank1Prefab == null ||
                m_Tank2Prefab == null ||
                m_Tank3Prefab == null ||
                m_Tank4Prefab == null)
            {
                Debug.LogError(
                    "You need to assign 4 tank prefab in the GameManager!"
                );
            }


            Debug.Log("GameManager iniciado correctamente");

        }




        void GameStart()
        {

            Debug.Log("GameStart iniciado");


            m_StartWait = new WaitForSeconds(m_StartDelay);

            m_EndWait = new WaitForSeconds(m_EndDelay);


            SpawnAllTanks();


            SetCameraTargets();


            StartCoroutine(GameLoop());

        }




        void ChangeGameState(GameState newState)
        {

            m_CurrentState = newState;


            switch (m_CurrentState)
            {

                case GameState.Game:

                    GameStart();

                    break;

            }

        }




        public void StartGame(PlayerData[] playerData)
        {

            Debug.Log("StartGame recibido");


            m_TankData = playerData;


            m_PlayerCount = m_TankData.Length;


            Debug.Log(
                "Cantidad de jugadores: " + m_PlayerCount
            );


            ChangeGameState(GameState.Game);

        }





        private void SpawnAllTanks()
        {

            Debug.Log("=== SPAWN ALL TANKS INICIADO ===");



            for (int i = 0; i < m_PlayerCount; i++)
            {


                var playerData = m_TankData[i];


                if(playerData.UsedPrefab == null)
                {
                    Debug.LogError(
                        "El jugador " + i +
                        " no tiene prefab asignado"
                    );

                    continue;
                }



                if(m_SpawnPoints[i].m_SpawnPoint == null)
                {
                    Debug.LogError(
                        "El SpawnPoint " + i +
                        " no tiene Transform"
                    );

                    continue;
                }



                Debug.Log(
                    "Creando tanque: " +
                    playerData.UsedPrefab.name
                );



                m_SpawnPoints[i].m_Instance =
                    Instantiate(
                        playerData.UsedPrefab,
                        m_SpawnPoints[i].m_SpawnPoint.position,
                        m_SpawnPoints[i].m_SpawnPoint.rotation
                    );



                Debug.Log(
                    "Tanque creado: " +
                    m_SpawnPoints[i].m_Instance.name
                );



                TankMovement mov =
                    m_SpawnPoints[i].m_Instance.GetComponent<TankMovement>();


                if(mov == null)
                {

                    Debug.LogError(
                        "El tanque " +
                        m_SpawnPoints[i].m_Instance.name +
                        " no tiene TankMovement"
                    );


                    continue;

                }



                mov.m_IsComputerControlled = false;



                m_SpawnPoints[i].m_PlayerNumber = i + 1;

                m_SpawnPoints[i].ControlIndex =
                    playerData.ControlIndex;

                m_SpawnPoints[i].m_PlayerColor =
                    playerData.TankColor;

                m_SpawnPoints[i].m_ComputerControlled =
                    playerData.IsComputer;


            }



            Debug.Log(
                "Todos los tanques creados. Iniciando Setup"
            );



            foreach(var tank in m_SpawnPoints)
            {

                if(tank.m_Instance == null)
                    continue;


                tank.Setup(this);

            }



            Debug.Log(
                "=== SETUP TERMINADO ==="
            );

        }
        private void SetCameraTargets()
        {

            Transform[] targets = new Transform[m_PlayerCount];


            for (int i = 0; i < targets.Length; i++)
            {

                targets[i] = m_SpawnPoints[i].m_Instance.transform;

            }


            m_CameraControl.m_Targets = targets;

        }




        private IEnumerator GameLoop()
        {

            yield return StartCoroutine(RoundStarting());


            yield return StartCoroutine(RoundPlaying());


            yield return StartCoroutine(RoundEnding());



            if (m_GameWinner != null)
            {

                SceneManager.LoadScene(0);

            }
            else
            {

                StartCoroutine(GameLoop());

            }

        }





        private IEnumerator RoundStarting()
        {

            ResetAllTanks();


            DisableTankControl();



            m_CameraControl.SetStartPositionAndSize();



            m_RoundNumber++;


            m_TitleText.text = "ROUND " + m_RoundNumber;



            yield return m_StartWait;

        }





        private IEnumerator RoundPlaying()
        {

            EnableTankControl();


            m_TitleText.text = string.Empty;



            while (!OneTankLeft())
            {

                yield return null;

            }

        }





        private IEnumerator RoundEnding()
        {

            DisableTankControl();



            m_RoundWinner = null;


            m_RoundWinner = GetRoundWinner();



            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;



            m_GameWinner = GetGameWinner();



            string message = EndMessage();



            m_TitleText.text = message;



            yield return m_EndWait;

        }





        private bool OneTankLeft()
        {

            int numTanksLeft = 0;



            for (int i = 0; i < m_PlayerCount; i++)
            {

                if (m_SpawnPoints[i].m_Instance.activeSelf)
                    numTanksLeft++;

            }



            return numTanksLeft <= 1;

        }





        private TankManager GetRoundWinner()
        {

            for (int i = 0; i < m_PlayerCount; i++)
            {

                if (m_SpawnPoints[i].m_Instance.activeSelf)
                    return m_SpawnPoints[i];

            }


            return null;

        }





        private TankManager GetGameWinner()
        {

            for (int i = 0; i < m_PlayerCount; i++)
            {

                if (m_SpawnPoints[i].m_Wins == m_NumRoundsToWin)
                    return m_SpawnPoints[i];

            }


            return null;

        }





        private string EndMessage()
        {

            string message = "DRAW!";



            if (m_RoundWinner != null)
                message =
                    m_RoundWinner.m_ColoredPlayerText +
                    " WINS THE ROUND!";



            message += "\n\n\n\n";



            for (int i = 0; i < m_PlayerCount; i++)
            {

                message +=
                    m_SpawnPoints[i].m_ColoredPlayerText +
                    ": " +
                    m_SpawnPoints[i].m_Wins +
                    " WINS\n";

            }



            if (m_GameWinner != null)
            {

                message =
                    m_GameWinner.m_ColoredPlayerText +
                    " WINS THE GAME!";

            }



            return message;

        }





        private void ResetAllTanks()
        {

            for (int i = 0; i < m_PlayerCount; i++)
            {

                m_SpawnPoints[i].Reset();

            }

        }





        private void EnableTankControl()
        {

            for (int i = 0; i < m_PlayerCount; i++)
            {

                m_SpawnPoints[i].EnableControl();

            }

        }





        private void DisableTankControl()
        {

            for (int i = 0; i < m_PlayerCount; i++)
            {

                m_SpawnPoints[i].DisableControl();

            }

        }

    }

}