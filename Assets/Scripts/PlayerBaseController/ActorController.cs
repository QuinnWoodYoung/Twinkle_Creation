using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorController : MonoBehaviour
{
    public GameObject model;
    public PlayerInput  pi;
    public StateManager sm;


    public float playerSpeed = 2.0f;//???????
    public float turningSpeed = 0.03f;//???????

    
    private Animator anim;
    private Rigidbody rb;
    private Vector3 movingVec;
    private Vector3 thrustVec; //????,???????????????

    private bool lockPlaner = false; //??????????
    public bool isDead;

    // Start is called before the first frame update
    void Awake()
    {
        pi = GetComponent<PlayerInput>();
        anim = model.GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        sm = GetComponent<StateManager>(); //???????????StateManager???
        //Cursor.lockState = CursorLockMode.Locked;//???????
        
    }
    void OnEnable()
    {
        GameManager.Instance.RigisterPlayer(sm);
        Cursor.lockState = CursorLockMode.Locked;//???????
    }
    void Start()
    {
        SaveManager.Instance.LoadPlayerData();
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }
    // Update is called once per frame
    void Update()
    {
        isDead = sm.HitPoint <= 0f;
        if (isDead == true)
        {
            GameManager.Instance.NotifyObservers();
        }
        SwitchAnimation();
        anim.SetFloat("forward", pi.signalForward) ;
        if (pi.jump)
        {
            anim.SetTrigger("jump");
        }
        if(pi.signalForward > 0.1f)//?????????????????????????????Update????????????????????????????
        {
            Vector3 targetForward = Vector3.Slerp(model.transform.forward,pi.signalVec, turningSpeed);//??????????????????????
            model.transform.forward = targetForward;
        }
        if(lockPlaner == false)
        {
            movingVec = pi.signalForward * model.transform.forward * playerSpeed;
        }
        else
        {
            movingVec = Vector3.zero;
        }

        if (pi.attack)
        {
            anim.SetTrigger("attack");
        }
    }

    void FixedUpdate()
    {
        //rb.position += movingVec * Time.fixedDeltaTime;
        rb.velocity = new Vector3(movingVec.x,rb.velocity.y,movingVec.z) + thrustVec;
        thrustVec = Vector3.zero;
    }

    private void SwitchAnimation()
    {
        anim.SetBool("dead", isDead);
    }
    public void OnWalkEnter()
    {
        pi.inputEnable = true;
    } 

    public void OnJumpEnter()
    {
        pi.inputEnable = false;
        thrustVec = new Vector3(0, 5f, 0);
    }

    public void OnJumpExit()
    {
        pi.inputEnable = true;

    }
    public void OnAttack01Enter()
    {

        pi.inputEnable = false;

    }

    public void OnAttack01Update() {
        thrustVec = model.transform.forward * anim.GetFloat("attackVelocity");
    }
    public void OnDeadEnter()
    {
            pi.inputEnable = false;
            lockPlaner = true;
    }


}
