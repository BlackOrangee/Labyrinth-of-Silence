using UnityEngine;

public class HidingSpot : MonoBehaviour, IInteractable
{
    [Header("Hiding Spot Settings")]
    [Tooltip("Hiding spot name (e.g. 'Closet' or 'Table')")]
    public string spotName = "Hiding Spot";
    
    [Tooltip("Point where player appears on exit")]
    public Transform exitPoint;
    
    [Tooltip("Popup window manager")]
    public PopupManager popupManager;
    
    [Header("Sounds")]
    [Tooltip("Sound when entering hiding spot")]
    public AudioClip enterSound;
    
    [Tooltip("Sound when exiting hiding spot")]
    public AudioClip exitSound;
    
    [Range(0f, 1f)]
    [Tooltip("Sound volume")]
    public float soundVolume = 0.5f;
    
    private GameObject currentPlayer;
    private PlayerHideController playerHideController;
    private AudioSource audioSource;
    private bool isPlayerInside = false;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = soundVolume;
    }

    void Start()
    {
        if (popupManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            popupManager = UnityEngine.Object.FindFirstObjectByType<PopupManager>();
#else
            popupManager = FindObjectOfType<PopupManager>();
#endif
            if (popupManager == null)
            {
                Debug.LogWarning($"HidingSpot ({spotName}): PopupManager not found!");
            }
        }
        
        if (exitPoint == null)
        {
            Debug.LogWarning($"HidingSpot ({spotName}): Exit Point not set! Creating automatically.");
            CreateExitPoint();
        }
        
        Debug.Log($"HidingSpot: {spotName} initialized.");
    }

    private void CreateExitPoint()
    {
        GameObject exitObj = new GameObject($"{spotName}_ExitPoint");
        exitObj.transform.parent = transform;
        exitObj.transform.position = transform.position + transform.forward * 1.5f;
        exitPoint = exitObj.transform;
    }

    #region IInteractable Implementation

    public string GetInteractText()
    {
        return $"Press Enter to hide: {spotName}";
    }

    public void Interact(GameObject actor)
    {
        if (actor == null)
        {
            return;
        }
        
        PlayerHideController hideController = actor.GetComponent<PlayerHideController>();
        
        if (hideController == null)
        {
            Debug.LogWarning($"HidingSpot: {actor.name} doesn't have PlayerHideController!");
            return;
        }
        
        if (hideController.IsHiding)
        {
            Debug.Log($"HidingSpot: Player already hidden in another place.");
            return;
        }
        
        EnterHiding(actor, hideController);
    }

    public void OnInteract(GameObject actor)
    {
        Interact(actor);
    }

    #endregion

    public void EnterHiding(GameObject player, PlayerHideController hideController)
    {
        if (isPlayerInside)
        {
            Debug.LogWarning($"HidingSpot: Someone already hiding in {spotName}!");
            return;
        }
        
        currentPlayer = player;
        playerHideController = hideController;
        isPlayerInside = true;
        
        hideController.EnterHiding(this);
        
        PlaySound(enterSound);
        
        Debug.Log($"HidingSpot: Player entered {spotName}");
    }

    public void ExitHiding()
    {
        if (!isPlayerInside || currentPlayer == null)
        {
            Debug.LogWarning($"HidingSpot: No player in {spotName} to exit!");
            return;
        }
        
        if (exitPoint != null)
        {
            CharacterController controller = currentPlayer.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                currentPlayer.transform.position = exitPoint.position;
                currentPlayer.transform.rotation = exitPoint.rotation;
                controller.enabled = true;
            }
            else
            {
                currentPlayer.transform.position = exitPoint.position;
                currentPlayer.transform.rotation = exitPoint.rotation;
            }
        }
        
        PlaySound(exitSound);
        
        if (popupManager != null && playerHideController != null)
        {
            StartCoroutine(ShowTemporaryMessage());
        }
        
        currentPlayer = null;
        playerHideController = null;
        isPlayerInside = false;
        
        Debug.Log($"HidingSpot: Player left {spotName}");
    }

    private System.Collections.IEnumerator ShowTemporaryMessage()
    {
        if (popupManager != null)
        {
            popupManager.ShowPopup(
                $"Player left hiding spot: {spotName}",
                null,
                this,
                "",
                null
            );
        }
        
        yield return new WaitForSeconds(2f);
        
        if (popupManager != null)
        {
            popupManager.HidePopup(this);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
            Debug.Log($"HidingSpot: Playing sound '{clip.name}'");
        }
    }

    void OnDrawGizmos()
    {
        if (exitPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(exitPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, exitPoint.position);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(exitPoint.position, exitPoint.forward * 0.5f);
        }
    }

    public string GetSpotName()
    {
        return spotName;
    }
}