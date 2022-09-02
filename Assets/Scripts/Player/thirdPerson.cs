using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class thirdPerson : MonoBehaviour
{
    public bool lockCursor;
    public float mouseSensitivity = 10;
    public Transform target;
    public float dstFromTarget = 2;
    public Vector2 pitchClamp = new Vector2(-40, 85);

    public float rotationSmoothTime = 1.2f;
    Vector3 rotationSmoothVel;
    Vector3 curRot;

    float yaw;
    float pitch;

    void Start(){
        if(lockCursor){
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch += Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, pitchClamp.x, pitchClamp.y);

        curRot = Vector3.SmoothDamp(curRot, new Vector3 (pitch, yaw), ref rotationSmoothVel, rotationSmoothTime);
        transform.eulerAngles = curRot;

        transform.position = target.position - transform.forward * dstFromTarget;
    }
}
