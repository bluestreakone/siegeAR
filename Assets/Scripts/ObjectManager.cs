using UnityEngine;
using System.Collections;
using TMPro;
using System.Drawing;
using Color = UnityEngine.Color;

public class ObjectManager : MonoBehaviour
{
    [Header("OBJECT CONTROLS")]
    [Tooltip("Check this box to enable all blocks.")]
    [SerializeField] private bool activateBlocks = false;
    [SerializeField] private bool activateCannonball = false;

    [Header("CANNONBALL")]
    [Tooltip("Assign cannonball GameObject or prefab here.")]
    [SerializeField] private GameObject cannonball;


    
    private bool previousBlockActivationState = false;
    private bool previousCannonballActivationState = false;
    Rigidbody[] allBlocks; //list of blocks in scene
    

    [Header("Timer UI Reference")]
    [SerializeField] private TMP_Text m_TimerText;
    [SerializeField] private Color prepTimerColor = Color.gray;
    [SerializeField] private Color defaultTimerColor = Color.yellow;
    [SerializeField] private Color warningTimerColor = Color.red;

    private Color currentColor;
    private float m_RemainingTime;
    private bool m_IsTimerRunning = false;
    public int buildingTimeLimit = 90;

    private int selectedTime;

    public static ObjectManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // activate the modeselection panel
        ModeSelection.ShowPanel();

        // get the timer text
        FindTimerText();

        if (m_TimerText != null)
        {
            m_TimerText.gameObject.SetActive(false);
        }

        currentColor = defaultTimerColor;
    }

    private void FindTimerText()
    {
        if (m_TimerText != null) return;

        //find the current clone of gamecore
        GameObject gameCore = GameObject.Find("GameCore(Clone)");
        if (gameCore == null)
        {
            gameCore = GameObject.Find("GameCore");
        }

        //once it is found, get the text component that is TimerText
        if (gameCore != null)
        {
            // Search children of GameCore (including cannon and timer text)
            TMP_Text[] texts = gameCore.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text txt in texts)
            {
                if (txt.gameObject.name == "TimerText")
                {
                    m_TimerText = txt;
                    Debug.Log("[ObjectManager] Successfully bound m_TimerText to CannonTimerText inside GameCore!");
                    m_TimerText.color = currentColor;
                    return;
                }  
            }
        }

        Debug.LogWarning("[ObjectManager] Could not find 'CannonTimerText' inside GameCore(Clone). Check hierarchy naming.");
    }

    public void OnDurationSelected(int seconds)
    {
        Debug.Log($"GameCore received duration: {seconds}s. Starting game now.");

        FindTimerText();

        buildingTimeLimit = seconds;
        selectedTime = seconds;

        // Sync countdown with 10s BlockTimer delay
        m_RemainingTime = buildingTimeLimit + 10f; 
        m_IsTimerRunning = true;

        FindTimerText();

        if (m_TimerText != null)
        {
            m_TimerText.gameObject.SetActive(true);
            UpdateTimerDisplay();
        }
    
        StartCoroutine(BlockTimerRoutine());
    }


    // Update is called once per frame
    void Update()
    {
        //if the checkbox for activating blocks is NEWLY pressed
        if(activateBlocks && !previousBlockActivationState)
        {
            enableAllBlocks();
            previousBlockActivationState = true;
            StartCoroutine(BuildingTimerRoutine());
        }

        // if the cannonball is newly activated, activate the cannonball and delete any blocks outside of build area
        if(activateCannonball && !previousCannonballActivationState)
        {
            enableCannonball();
            destroyInvalidBlocks();
            previousCannonballActivationState = true;
        }
        

        // Timer countdown
        if (m_IsTimerRunning)
        {
            m_RemainingTime -= Time.deltaTime;

            if (m_RemainingTime <= 0f)
            {
                m_RemainingTime = 0f;
                m_IsTimerRunning = false;
                UpdateTimerDisplay();
                OnTimerComplete();
            }
            else
            {
                UpdateTimerDisplay();
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        if (m_TimerText == null) return;
        int totalSeconds = Mathf.CeilToInt(m_RemainingTime);
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        m_TimerText.text = $"{minutes:00}:{seconds:00}";

        if(totalSeconds > selectedTime && currentColor != prepTimerColor)
        {
            currentColor = prepTimerColor;
            m_TimerText.color = currentColor;
        }
        else if(totalSeconds <= 15 && currentColor != warningTimerColor)
        {
            currentColor = warningTimerColor;
            m_TimerText.color = currentColor;
        }
        else if(totalSeconds <= selectedTime && totalSeconds > 15 && currentColor != defaultTimerColor)
        {
            currentColor = defaultTimerColor;
            m_TimerText.color = currentColor;
        }
    }

    private void OnTimerComplete()
    {
        Debug.Log("Session duration expired!");
    }
    
    IEnumerator BlockTimerRoutine()
    {
        // wait for 10 seconds (works great on mobile without needing the Inspector)
        yield return new WaitForSeconds(10f);

        // activate the blocks and signal build timer to start
        activateBlocks = true;
    }

    IEnumerator BuildingTimerRoutine()
    {
        // wait for the designated building time
        yield return new WaitForSeconds(buildingTimeLimit);

        // activate the cannonball
        activateCannonball = true;
    }

    private void enableAllBlocks()
    {
        // finds all rigidbodies in the scene (or can filter by tag/component)
        allBlocks = FindObjectsOfType<Rigidbody>();

        // for every block, enable all core features
        foreach(Rigidbody rb in allBlocks)
        {
            // safety check: skip the cannonball so it doesn't activate early
            if (cannonball != null && rb.gameObject == cannonball) continue;

            rb.useGravity = true;
            rb.GetComponent<MeshRenderer>().enabled = true;
            rb.GetComponent<Collider>().enabled = true;
            rb.WakeUp();
        }
    }

    private void enableCannonball()
    {
        if (cannonball != null)
        {
            // activate the game object
            cannonball.SetActive(true);

            // enable physics components if attached
            Rigidbody rb = cannonball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.WakeUp();
            }

            MeshRenderer mr = cannonball.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;

            Collider col = cannonball.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // explicitly tell the cannonball script to fire
            launchAndImpactHandler cbScript = cannonball.GetComponent<launchAndImpactHandler>();
            if (cbScript != null)
            {
                cbScript.Launch();
            }
            else
            {
                Debug.LogWarning("Cannonball GameObject is missing the Cannonball script component.");
            }
        }
        else
        {
            Debug.LogWarning("Cannonball object reference is missing in the ObjectManager Inspector.");
        }
    }

    private void destroyInvalidBlocks()
    {
        // for every block, call deletion function if not touching the building area
        foreach(Rigidbody rb in allBlocks)
        {
            if (rb == null) continue;

            // skip the cannonball
            if (cannonball != null && rb.gameObject == cannonball) continue;

            // call the public method on the block's own script
            BlockValidator validator = rb.GetComponent<BlockValidator>();
            if (validator != null)
            {
                validator.DestructIfInvalid();
            }
        }
    }
}
