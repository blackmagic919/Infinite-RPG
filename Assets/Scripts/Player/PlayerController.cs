using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //Test 1
    public float walkSpeed = 150;
    public float runSpeed = 300;
    public float gravity = -12f;
    public float jumpHeight = 7;

    public float turnSmoothTime = 0.2f;
    float turnSmoothVel;

    public float speedSmooothTime = 0.1f;
    float speedSmoothVel;
    float curSpeed;
    float velocityY;

    Animator animator;
    Transform cameraT;
    CharacterController controller;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        cameraT = Camera.main.transform;
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector2 inputDir = input.normalized;

        if(inputDir!=Vector2.zero){
            float targetRot = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraT.eulerAngles.y;
            transform.eulerAngles = Vector3.up*Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRot, ref turnSmoothVel, turnSmoothTime);
        }
        
        bool running = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = ((running) ? runSpeed : walkSpeed) * inputDir.magnitude;
        curSpeed = Mathf.SmoothDamp(curSpeed, targetSpeed, ref speedSmoothVel, speedSmoothVel);

        velocityY += Time.deltaTime*gravity;
        Vector3 velocity = transform.forward * curSpeed + Vector3.up*velocityY;

        controller.Move(velocity*Time.deltaTime);

        if(controller.isGrounded){
            velocityY = 0;
            if(Input.GetKeyDown(KeyCode.Space)) Jump();
        }


        float actualSpeed = new Vector2(controller.velocity.x, controller.velocity.z).magnitude;

        float animationSpeedPercent = ((running)? actualSpeed/runSpeed:actualSpeed/walkSpeed * 0.5f);

        animator.SetFloat("speedPercent", animationSpeedPercent, speedSmooothTime, Time.deltaTime);
    }

    void Jump(){
        float jumpVel = Mathf.Sqrt(-2*gravity*jumpHeight);
        velocityY = jumpVel;
    }
}
