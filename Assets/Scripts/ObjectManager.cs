using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Netcode;

public class ObjectManager : NetworkBehaviour
{
    public static ObjectManager Instance { get; private set; }

    [Header("TIMER UI REFERENCE")]
    [SerializeField] private TMP_Text m_TimerText;
    private GameObject modeSelectionPanel;
    [SerializeField] private Color prepTimerColor = Color.gray;
    [SerializeField] private Color defaultTimerColor = Color.yellow;
    [SerializeField] private Color warningTimerColor = Color.red;

    [Header("CANNONBALL & GAME CONTROLS")]
    [SerializeField] private GameObject cannonball;

    public NetworkVariable<float> NetRemainingTime = new NetworkVariable<float>(
        0f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private Dictionary<int, NetworkBlock> blockDict = new Dictionary<int, NetworkBlock>();
    private Dictionary<int, ulong> blockOwners = new Dictionary<int, ulong>();
    private const ulong NO_OWNER = ulong.MaxValue;

    private Coroutine activeTimerCoroutine;
    private float currentBuildingDuration = 60f;
    private bool prepPhaseCompleted = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterModeSelectionUI(GameObject panel)
    {
        modeSelectionPanel = panel;
    }

    public override void OnNetworkSpawn()
    {
        NetRemainingTime.OnValueChanged += OnTimerValueChanged;
        
        // Auto-locate cannonball if assigned in scene
        if (cannonball == null)
        {
            GameObject foundBall = GameObject.FindWithTag("Cannonball") ?? GameObject.Find("cannonball");
            cannonball = foundBall;
        }

        // CRITICAL FIX: Auto-register all NetworkBlocks instantly on network spawn 
        // to prevent client-side race conditions where DropBlocksClientRpc fires before Start() completes.
        NetworkBlock[] existingBlocks = FindObjectsOfType<NetworkBlock>();
        foreach (var block in existingBlocks)
        {
            RegisterBlock(block.BlockId, block);
        }
    }

    public override void OnNetworkDespawn()
    {
        NetRemainingTime.OnValueChanged -= OnTimerValueChanged;
    }

    public void RegisterTimer(TMP_Text timerTextComp)
    {
        m_TimerText = timerTextComp;
        UpdateTimerUI(NetRemainingTime.Value);
    }

    public void OnDurationSelected(float duration)
    {
        currentBuildingDuration = duration;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            StartGameSequence(duration);
            return;
        }

        if (IsServer)
        {
            StartGameSequence(duration);
        }
        else
        {
            RequestStartGameServerRpc(duration);
        }
    }

    public void ForceSyncBlockTransform(int blockId, Vector3 localPos, Quaternion localRot)
    {
        if (IsServer)
        {
            SyncBlockTransformClientRpc(blockId, localPos, localRot, true);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestStartGameServerRpc(float duration)
    {
        if (!IsServer) return;
        StartGameSequence(duration);
    }

    private void StartGameSequence(float duration)
    {
        if (activeTimerCoroutine != null)
        {
            StopCoroutine(activeTimerCoroutine);
        }

        prepPhaseCompleted = false;
        float totalTime = duration + 10f;
        NetRemainingTime.Value = totalTime;

        HideModeSelectionClientRpc();

        activeTimerCoroutine = StartCoroutine(GameTimerRoutine(duration));
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    private void HideModeSelectionClientRpc()
    {
        if (ModeSelection.Instance != null)
        {
            ModeSelection.Instance.gameObject.SetActive(false);
        }
        else
        {
            GameObject panel = GameObject.Find("modeSelectionPanel");
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (!IsServer && m_TimerText != null)
        {
            UpdateTimerUI(NetRemainingTime.Value);
        }
    }

    #region Timer & Game State Coroutine

    private IEnumerator GameTimerRoutine(float buildingDuration)
    {
        while (NetRemainingTime.Value > 0f)
        {
            yield return new WaitForSeconds(1.0f);
            NetRemainingTime.Value -= 1.0f;

            if (!prepPhaseCompleted && NetRemainingTime.Value <= buildingDuration)
            {
                prepPhaseCompleted = true;
                OnPrepPhaseComplete();
            }
        }

        NetRemainingTime.Value = 0f;
        OnBuildPhaseComplete();
    }

    private void OnTimerValueChanged(float previousValue, float newValue)
    {
        UpdateTimerUI(newValue);
    }

    private void UpdateTimerUI(float timeRemaining)
    {
        if (m_TimerText == null) return;

        float clampedTime = Mathf.Max(0f, timeRemaining);
        int totalSeconds = Mathf.FloorToInt(clampedTime);
        int milliseconds = Mathf.FloorToInt((clampedTime % 1f) * 100f);

        m_TimerText.text = string.Format("{0:00}:{1:00}", totalSeconds, milliseconds);

        if (clampedTime > currentBuildingDuration)
        {
            m_TimerText.color = prepTimerColor;
        }
        else if (clampedTime > 10f)
        {
            m_TimerText.color = defaultTimerColor;
        }
        else if (clampedTime > 0f)
        {
            m_TimerText.color = warningTimerColor;
        }
        else
        {
            m_TimerText.color = warningTimerColor;
            m_TimerText.text = "00:00";
        }
    }

    private void OnPrepPhaseComplete()
    {
        DropBlocksClientRpc();
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    private void DropBlocksClientRpc()
    {
        foreach (var kvp in blockDict)
        {
            if (kvp.Value != null)
            {
                kvp.Value.DropToGround();
            }
        }
    }

    private void OnBuildPhaseComplete()
    {
        DisableBuildingClientRpc();
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    private void DisableBuildingClientRpc()
    {
        if (cannonball != null)
        {
            cannonball.SetActive(true);
        }

        foreach (var block in blockDict.Values)
        {
            if (block != null)
            {
                block.DisableBuilding();
            }
        }
    }

    #endregion

    #region Block Registration & Networking

    public void RegisterBlock(int id, NetworkBlock block)
    {
        if (!blockDict.ContainsKey(id))
        {
            blockDict.Add(id, block);
            blockOwners.Add(id, NO_OWNER);
        }
        else
        {
            blockDict[id] = block;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestBlockOwnershipServerRpc(int blockId, ulong clientId)
    {
        if (blockOwners.ContainsKey(blockId))
        {
            if (blockOwners[blockId] == NO_OWNER || blockOwners[blockId] == clientId)
            {
                blockOwners[blockId] = clientId;
                GrantOwnershipClientRpc(blockId, clientId);
            }
        }
    }

    [ClientRpc]
    private void GrantOwnershipClientRpc(int blockId, ulong clientId)
    {
        if (blockDict.TryGetValue(blockId, out NetworkBlock block))
        {
            bool isLocal = (clientId == NetworkManager.Singleton.LocalClientId);
            block.SetOwnershipState(isLocal, true);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReleaseBlockOwnershipServerRpc(int blockId, ulong clientId, Vector3 velocity, Vector3 angularVelocity)
    {
        if (blockOwners.ContainsKey(blockId) && blockOwners[blockId] == clientId)
        {
            blockOwners[blockId] = NO_OWNER;
            ReleaseOwnershipClientRpc(blockId, velocity, angularVelocity);
        }
    }

    [ClientRpc]
    private void ReleaseOwnershipClientRpc(int blockId, Vector3 velocity, Vector3 angularVelocity)
    {
        if (blockDict.TryGetValue(blockId, out NetworkBlock block))
        {
            block.HandleRemoteRelease(velocity, angularVelocity);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SyncBlockTransformServerRpc(int blockId, Vector3 localPos, Quaternion localRot)
    {
        SyncBlockTransformClientRpc(blockId, localPos, localRot, false);
    }

    [ClientRpc]
    private void SyncBlockTransformClientRpc(int blockId, Vector3 localPos, Quaternion localRot, bool isRestSnapshot)
    {
        if (blockDict.TryGetValue(blockId, out NetworkBlock block))
        {
            block.UpdateRemoteTransform(localPos, localRot, isRestSnapshot);
        }
    }

    #endregion
}