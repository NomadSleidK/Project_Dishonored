using System.Collections;
using System.Collections.Generic;
using System.Security.Claims;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(CharacterController))]
public class MovementCharacter : MonoBehaviour
{

    public Camera playerCamera;
    public GameObject BlockCamera;
    public GameObject Hands;
    public Climb climb;

    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float slideSpeed;

    public float lookSpeed = 2f;
    public float lookXLimit = 45f;

    public bool grabMovement = false;
    public bool normal = false;
    public bool IsGround;
    public bool IsCrouch;
    public bool IsSliding;

    public bool canMove = true;

    public float CrouchScale;
    private float BaseScale;

    private KeyCode Jumping = KeyCode.Space;
    private KeyCode crouchKey = KeyCode.LeftControl;
    private KeyCode SlideKeyShift = KeyCode.LeftShift;

    Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        BaseScale = transform.localScale.y;
        IsGround = true;
        IsCrouch = false;
        IsSliding = false;
    }

    void Update()
    {
        if (!grabMovement && !normal && !IsSliding)
        {
            WalkOrRun();
            CameraMovement();
            if (Input.GetKeyDown(crouchKey) && !IsCrouch)
            {
                CrouchMoveOn();
            }
            else if ((Input.GetKeyDown(crouchKey) || Input.GetButtonDown("Crouch")) && IsCrouch)
            {
                CrouchMoveOff();
            }

            if (Input.GetButtonDown("Crouch") && Input.GetKey(SlideKeyShift) && !IsSliding && !IsCrouch && IsGround)
            {
                Slide();
            }
        }
        else if (grabMovement && !normal)
        {
            GrabMovement();
        }
        else if (IsSliding && !normal)
        {
            GrabCameraMovement();
            characterController.Move(-transform.up);
            characterController.Move(transform.forward * slideSpeed * Time.deltaTime);
        }
    }


    //Slide functions

    private void Slide() //контроль подката, входы, выхода и его скорости
    {
        IsSliding = true;
        slideSpeed = runSpeed + 5;
        CrouchMoveOn();
        StartCoroutine(SlideSpeedControl()); //вызов куротины изменения скорости подката и выхода из него
    }

    IEnumerator SlideSpeedControl() //корутина изменения скорости в подкате и выхода из подката
    {
        yield return new WaitForSeconds(0.2f);
        slideSpeed -= 2;
        yield return new WaitForSeconds(0.2f);
        slideSpeed -= 3;
        yield return new WaitForSeconds(0.2f);
        slideSpeed -= 4;

        yield return new WaitForSeconds(0.2f);
        IsSliding = false;

        CrouchMoveOff();  //выход из подката
        BackRotation(); //поворот тела персонажа в строну направления камеры
        Invoke("BackCollDawn", 0.2f);
    }

    //GrabMovement functions

    private void GrabMovement() //передвижение когда персонаж висит на уступе
    {
        GrabCameraMovement();

        Ray rayMid = new Ray(transform.position + new Vector3(0, 0.3f, 0), transform.forward);
        Debug.DrawRay(transform.position + new Vector3(0, 0.3f, 0), transform.forward, Color.yellow);

        float x = Input.GetAxis("Horizontal");
        Vector3 movement = new Vector3(x, 0, 0);

        if (Input.GetKeyDown(Jumping)) //при нажатии пробела когда нажали прыжок
        {
            moveDirection.y = jumpPower;
            BackRotation();
            Invoke("BackCollDawn", 0.2f);
        }
        else if (Physics.Raycast(rayMid, climb.DistanceForwardVector, climb.GrabMask)) //пока есть вдоль чего перемещаться - перемещаемся
        {
            transform.Translate(movement * (walkSpeed - 2) * Time.deltaTime);
        }
        else //если уступ кончился - падаем
        {
            moveDirection.y = 0;
            BackRotation();
            Invoke("BackCollDawn", 0.2f);
        }

        if (Input.GetButtonDown("Crouch")) //если нажать С - падаем
        {
            moveDirection.y = 0;
            BackRotation();
            Invoke("BackCollDawn", 0.2f);
        }
    }

    public void RotationToGrab() //поворот персонажа перпендикулярно нормали к поверхности зацепа с сохранением поворота камеры
    {
        Quaternion rot = transform.rotation;

        transform.rotation = Quaternion.LookRotation(-climb.hitRotation.normal);
        BlockCamera.transform.rotation = rot;
    }

    private void BackRotation()//поворот персонажа в сторону взгляда камеры после прыжка
    {
        climb.coolDawn = true;
        Quaternion rot = BlockCamera.transform.rotation;
        transform.rotation = rot;
        BlockCamera.transform.rotation = rot;
        grabMovement = false;
    }

    private void BackCollDawn()//возвращает возможность цепляться за уступы
    {
        climb.coolDawn = false;
    }

    //CrouchMovement functions

    private void CrouchMoveOn() //переход в режим приседа
    {
        transform.localScale = new Vector3(transform.localScale.x, CrouchScale, transform.localScale.z);
        Hands.transform.localScale += new Vector3(0, CrouchScale, 0);
        IsCrouch = true;
    }

    private void CrouchMoveOff() //выход из режима приседа
    {
        Ray rayTop = new Ray(transform.position, transform.up);
        Debug.DrawRay(transform.position, transform.up * 1f, Color.magenta);

        if (!Physics.Raycast(rayTop, 1f)) //если луч up длиной 1 найдёт поверхность, то не выходим из приседа
        {
            transform.localScale = new Vector3(transform.localScale.x, BaseScale, transform.localScale.z);
            Hands.transform.localScale -= new Vector3(0, CrouchScale, 0);
            IsCrouch = false;
        }
    }

    //BaseMovement functions

    private void Gravity() //гравитация
    {
        if (!characterController.isGrounded) 
        {
            moveDirection.y -= gravity * Time.deltaTime;
            IsGround = false;
        }
        else
        {
            IsGround = true;
        }
    }

    private void WalkOrRun()//шаг, бег, прыжок
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        // Press Left Shift to run
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        
        if (Input.GetKeyDown(Jumping) && canMove && characterController.isGrounded) //прыжок 
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        Gravity();
        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void CameraMovement() //вращение камеры и тела персонажа (во время движения)
    {
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }

    private void GrabCameraMovement() // вращение камеры и дочернего куба к телу персонажа (когда персонаж висит на уступе)
    {
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        BlockCamera.transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }
}
