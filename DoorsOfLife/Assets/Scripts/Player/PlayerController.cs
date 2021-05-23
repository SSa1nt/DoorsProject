using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine;



public class PlayerController : MonoBehaviour
{
    //public static PlayerController Instance { get; private set; } = null;

    //Other Components
    private PlayerMovement playerMovement;
    private PlayerWeapon playerWeapon;
    private PlayerAnimation playerAnimation;
    private PlayerHealth playerHealthComponent;
    private PlayerHearts playerHeartsComponent;


    public PlayerMovement PlayerMovementComponent => playerMovement; // <<--
    public PlayerWeapon PlayerWeaponComponent => playerWeapon; //<<--
    public PlayerAnimation PlayerAnimationComponent =>playerAnimation;
    public PlayerHealth PlayerHealthComponent => playerHealthComponent;
    public PlayerHearts PlayerHeartsComponent => playerHeartsComponent;

    //Input
    [Header("Input Settings")]
    [SerializeField]
    private bool isXInverted = false;
    [SerializeField]
    private bool isYInverted = false;
    private Vector2 rawMovementInput;
    private Vector2 lastMovementInput;

    //----------------------------------- //[SerializeField]
    //private float speedInteractCheck = 8; // 1/8 = 0.125 ms
    [Header("Player Interaction")]
    [SerializeField]
    private float offsetXInteract = 0, offsetYInteract = 0;
    [SerializeField]
    private float interactReach = 1f;
    [SerializeField]
    private LayerMask interactibleLayer;
    public InteractionData interactionData;
    private GameObject interactCanvas=null;
    [SerializeField]
    private Image imgPrompt=null;
    [SerializeField]
    private TextMeshProUGUI textPrompt=null;

    //Input components
    private PlayerControls myPlayerControls;
    private PlayerInput myInput;

    public PlayerInput PlayerInputComponent
    {
        get => myInput;
    }

    public bool inCutscene=false;

    private void OnDestroy()
    {
        interactionData.ResetData();
        //Instance = null;
        interactCanvas = null;
    }


    private string currentControlScheme; //Basically useless variable, delete when time available

    #region ActionMaps
    private string currentActionMap;
    private string actionMapGameplay = "Gameplay";
    private string actionMapMenu = "Menu";
    private string actionMapInteraction = "InInteraction";
    #endregion


    public Vector2 GetPositionVector2()
    {
        return transform.position;
    }
    public Vector3 GetPositionVector3()
    {
        return transform.position;
    }


    #region debugFunctions
    private void DebugGameOver()
    {
        if(Debug.isDebugBuild)
        GameManager.Instance.ShowGameOver();
    }

    private void debugCompletePuzzle()
    {
        if (!Debug.isDebugBuild) return;
        PuzzlePlayMaster pz=
        FindObjectOfType<PuzzlePlayMaster>();
        if (pz)
        {
            pz.debugCompletePuzzle();
        }
    }

    private void DebugFunc3()
    {
        if (!Debug.isDebugBuild) return;
        PuzzleChessMaster pz = FindObjectOfType<PuzzleChessMaster>();
        if (pz) pz.debugCompletePuzzle();
    }

    bool s = true;

    private void DebugFunc4()
    {
        if (s)
        {
            GetComponentInParent<HealthSystemHeartBase>().TakeDamage(2);
        }
        else
        {
            GetComponentInParent<HealthSystemHeartBase>().Heal(1);
        }
        //s = !s;   
    }

    private void DebugFunc5()
    {
        GetComponentInParent<HealthSystemHeartBase>().UpgradeHealth(1);
    }
    #endregion

    #region showingPrompt

    public void ShowPrompt(string text)
    {
        interactCanvas.SetActive(true);
        imgPrompt.sprite = UIManager.Instance.getInteractSprite();
        textPrompt.text = text;
    }
    public void HidePrompt()
    {
        interactCanvas.SetActive(false);
    }

    #endregion

    private void OnEnable()
    {
        #region Debug
        myPlayerControls.Gameplay.DebugL1.started += cntx => DebugGameOver();

        myPlayerControls.Gameplay.DebugR1.started += cntx => debugCompletePuzzle();

        myPlayerControls.Gameplay.Debug3.started += cntx => DebugFunc3();

        myPlayerControls.Gameplay.Debug4.started += cntx => DebugFunc4();

        myPlayerControls.Gameplay.Debug5.started += cntx => DebugFunc5();
        #endregion

        #region Gameplay
        myPlayerControls.Gameplay.Movement.performed += cntx => OnMovement(cntx.ReadValue<Vector2>());
        myPlayerControls.Gameplay.Movement.canceled += cntx => onStopMovement();

        myPlayerControls.Gameplay.Interact.started += cntx => OnInteract();

        myPlayerControls.Gameplay.Attack.started += cntx => OnAttack();

        myPlayerControls.Gameplay.Pause.started += cntx => GameManager.Instance.PauseToggle();
        #endregion

        #region Menu

        myPlayerControls.Menu.UnPause.started += cntx => GameManager.Instance.PauseToggle();

        #endregion

        #region Health Component
        playerHeartsComponent.OnDamageTaken += OnDamageTaken;
        playerHeartsComponent.OnDeath += OnIsDead;


        #endregion
        #region In Interaction
        myPlayerControls.InInteraction.ContinueInteract.started += cntx => ContinueInteract();

        #endregion

        myInput.controlsChangedEvent.AddListener(OnControlsChanged);
    }


    void OnDamageTaken()
    {
        playerAnimation.PlayHitAnimation();
    }

    // Start is called before the first frame update
    private void Awake()
    {

        #region Getting Components
        playerMovement = GetComponent<PlayerMovement>();
        playerWeapon = GetComponent<PlayerWeapon>();
        playerAnimation = GetComponent<PlayerAnimation>();
        playerHealthComponent = GetComponent<PlayerHealth>();
        playerHeartsComponent = FindObjectOfType<PlayerHearts>();

        myPlayerControls = new PlayerControls();
        myInput = GetComponent<PlayerInput>();

        interactCanvas = GetComponentInChildren<Canvas>().gameObject;

        Debug.Log(interactCanvas);
        #endregion

        myInput.actions = myPlayerControls.asset;

        myPlayerControls.Enable();


        currentActionMap = actionMapGameplay; //Lets assume its always the gameplay starting

        myInput.actions.FindActionMap(actionMapInteraction).Disable();
        myInput.actions.FindActionMap(actionMapMenu).Disable();

        Debug.Log(currentActionMap);
        currentControlScheme = myInput.currentControlScheme; 

        HidePrompt();

        //Other components
    }

    private void Start()
    {
        StartCoroutine(nameof(RoutineCheckInteractible));
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateMovement();
        UpdateLastMovement();
    }

    private void UpdateLastMovement()
    {
        if (rawMovementInput.normalized != Vector2.zero)
        {
            lastMovementInput = rawMovementInput.normalized;
            //Debug.Log(lastMovementInput);
            lastMovementInput = new Vector2(Mathf.RoundToInt(lastMovementInput.x), Mathf.RoundToInt(lastMovementInput.y));

            if (lastMovementInput.x == lastMovementInput.y || lastMovementInput.x == -lastMovementInput.y)
            {
                lastMovementInput = lastMovementInput / 1.5f;
            } //The original idea with this piece of code was that the sides should have been 0.5 instead of 1 but thinking it further this is the right way, making it the trignometric circle
        }
    }

    private void UpdateMovement()
    {
        playerMovement.UpdateMovementData(rawMovementInput);

        playerAnimation.UpdateMovementAnimation(rawMovementInput);
    }

    private IEnumerator RoutineCheckInteractible() //Plan was for this to be a coroutine, turns out it gave huge gameplay problems
    {
        while (enabled)
        {
            if(!inCutscene && !GameManager.Instance.GameIsPaused)
            {
                CheckForInteractible();
            }
            yield return new WaitForEndOfFrame();//WaitForSeconds(1/speedInteractCheck);
        }
    }

    //IInteractibleBase interactible; 
    private void CheckForInteractible()
    {
        Vector3 startpos = transform.position + new Vector3(offsetXInteract, offsetYInteract);
        RaycastHit2D _Hits;
        _Hits = Physics2D.Raycast(startpos, lastMovementInput,interactReach,interactibleLayer);

        

        if (_Hits && _Hits.transform.CompareTag("Interactible"))
        {
            IInteractibleBase interactible = _Hits.transform.GetComponent<IInteractibleBase>();
            if (interactible)
            {
                if (interactionData.IsEmpty())
                {
                    interactionData.Interactible = interactible;
                    ShowPrompt(interactible.InteractionText);
                    Debug.DrawRay(startpos, lastMovementInput * interactReach, Color.green, 0.2f);
                }
                else
                {
                    if (!interactionData.IsSameInteractible(interactible))
                    {
                        interactionData.Interactible = interactible;
                        ShowPrompt(interactible.InteractionText);
                        Debug.DrawRay(startpos, lastMovementInput * interactReach, Color.green, 0.2f);
                    }
                    else
                    {
                        Debug.DrawRay(startpos, lastMovementInput * interactReach, Color.green, 0.2f);
                        return;
                    }
                }
            }
        }
        else
        {
            Debug.DrawRay(startpos, lastMovementInput * interactReach, Color.red, 0.2f);
            HidePrompt();
            interactionData.ResetData();
        }
    }

    //Events

    public Vector2 ReturnFacingDirection()
    {
        return lastMovementInput;
    }

#region Events
    private void OnInteract()
    {
        if (!interactionData.IsEmpty())
        {
            Debug.Log("Interacted");
            interactionData.Interact();
            HidePrompt();
        }
        
    }

    public void IsInteracting(InteractionType type)
    {
        switch (type)
        {
            case InteractionType.DialogInteraction:
                EnableDialogControls();
                break;

        }
    }

    public void StoppedInteracting()
    {
        interactionData.ResetData();
        EnableGameplayControls();
    }
    public void OnMovement(Vector2 context)
    {
        //interactionData.ResetData(); //Lazy way to clear value
        Vector2 tempValue = context;
        rawMovementInput = new Vector2(isXInverted ? -tempValue.x : tempValue.x,
            isYInverted ? -tempValue.y : tempValue.y);
    }

    private void onStopMovement()
    {
        rawMovementInput = Vector2.zero;
    }

    private void OnAttack()
    {
        playerWeapon.PlayerAttack(lastMovementInput);
        playerAnimation.PlayAttackAnimation();
    }


    private void ContinueInteract()
    {
        interactionData.ContinueInteract();
    }

    public void OnIsDead()
    {
        playerAnimation.PlayDeathAnimation();
        EnableMenuControls();
    }

    public void ResetDeath()
    {

    }

    #endregion

    
#region UIRelated
    public void OnControlsChanged(PlayerInput arg1)
    {
        if (currentControlScheme != myInput.currentControlScheme)
        {
            Debug.Log(myInput.devices[0].ToString());
            currentControlScheme = myInput.currentControlScheme;
            Debug.Log(currentControlScheme);

            imgPrompt.sprite = UIManager.Instance.getInteractSprite();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.PromptsChange();
            }
            if (GamepadRumbler.Instance != null) 
            { 
                GamepadRumbler.Instance.SetGamepad(); 
            }

            //interactionData.ControlsChanged();
        }
    }

    #endregion
    //Switching Control types

    public void EnableGameplayControls()
    {
        myInput.SwitchCurrentActionMap(actionMapGameplay);
        myInput.actions.FindActionMap(currentActionMap).Disable();
        currentActionMap = actionMapGameplay;
    }

    public void EnableMenuControls() 
    {
        myInput.SwitchCurrentActionMap(actionMapMenu);
        myInput.actions.FindActionMap(currentActionMap).Disable();
        currentActionMap = actionMapMenu;
    }

    public void EnableDialogControls()
    {
        myInput.SwitchCurrentActionMap(actionMapInteraction);
        myInput.actions.FindActionMap(currentActionMap).Disable();
        currentActionMap = actionMapInteraction;
    }


    private void OnDrawGizmos()
    {
        Vector3 startpos = transform.position + new Vector3(offsetXInteract, offsetYInteract);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(startpos, lastMovementInput * interactReach);
    }
}
