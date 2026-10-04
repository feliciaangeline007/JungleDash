using UnityEngine;
using UnityStandardAssets.Cameras;
using UnityStandardAssets.Vehicles.Car;

namespace AdventureGame
{
    public class VehicleMount : MonoBehaviour
    {
        [Header("Vehicle Config")]
        public GameObject carObject;
        public Transform exitPoint;
        public float enterDistance = 3.5f;

        [Header("UI Prompt")]
        public string enterMessage = "Press [E] to Drive SkyCar";
        public string exitMessage = "Press [E] to Exit SkyCar";

        private GameObject m_Player;
        private bool m_InVehicle = false;
        private CarUserControl m_CarControl;
        private CarAudio m_CarAudio;
        private AbstractTargetFollower m_CameraRig;
        private CompleteGameHUD m_HUD;

        private void Start()
        {
            m_Player = GameObject.FindGameObjectWithTag("Player");
            m_HUD = Object.FindAnyObjectByType<CompleteGameHUD>();
            m_CameraRig = Object.FindAnyObjectByType<AbstractTargetFollower>();

            if (carObject == null) carObject = gameObject;

            m_CarControl = carObject.GetComponent<CarUserControl>();
            m_CarAudio = carObject.GetComponent<CarAudio>();

            // Disable car control until player enters
            if (m_CarControl != null) m_CarControl.enabled = false;
            if (m_CarAudio != null) m_CarAudio.enabled = false;
        }

        private void Update()
        {
            if (m_Player == null)
            {
                m_Player = GameObject.FindGameObjectWithTag("Player");
                return;
            }

            if (!m_InVehicle)
            {
                float dist = Vector3.Distance(m_Player.transform.position, transform.position);
                if (dist <= enterDistance)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        EnterVehicle();
                    }
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
                {
                    ExitVehicle();
                }
            }
        }

        public void EnterVehicle()
        {
            if (m_InVehicle || m_Player == null) return;
            m_InVehicle = true;

            m_Player.SetActive(false);
            if (m_CarControl != null) m_CarControl.enabled = true;
            if (m_CarAudio != null) m_CarAudio.enabled = true;

            if (m_CameraRig != null)
            {
                m_CameraRig.SetTarget(carObject.transform);
            }

            if (m_HUD != null)
            {
                m_HUD.ShowNotification("Entered SkyCar! Drive around the island circuit! (Press E to exit)");
            }
        }

        public void ExitVehicle()
        {
            if (!m_InVehicle || m_Player == null) return;
            m_InVehicle = false;

            Vector3 spawnPos = exitPoint != null 
                ? exitPoint.position 
                : transform.position - transform.right * 3f + Vector3.up * 0.5f;

            m_Player.transform.position = spawnPos;
            m_Player.SetActive(true);

            if (m_CarControl != null) m_CarControl.enabled = false;
            if (m_CarAudio != null) m_CarAudio.enabled = false;

            // Stop car physics
            var rb = carObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (m_CameraRig != null)
            {
                m_CameraRig.SetTarget(m_Player.transform);
            }

            if (m_HUD != null)
            {
                m_HUD.ShowNotification("Exited SkyCar.");
            }
        }
    }
}
