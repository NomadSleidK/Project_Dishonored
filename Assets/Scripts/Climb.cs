using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climb : MonoBehaviour
{
    private int vaultLayer;
    public Camera cam;
    public float DistanceForwardVector;
    public float DistanceUpVector;
    public LayerMask GrabMask;
    public MovementCharacter moveCharacter;

    public bool coolDawn;
    public RaycastHit hitRotation;

    void Start()
    {
        vaultLayer = LayerMask.NameToLayer("VaultLayer");
        vaultLayer = ~vaultLayer;
        coolDawn = false;

    }

    void Update()
    {
        if (!coolDawn && moveCharacter.grabMovement == false)
        {
            Vault();
        }
    }



    private void Vault()
    {

        //Ray rayBot = new Ray(transform.position + new Vector3(0, -1, 0), transform.forward);
        //Debug.DrawRay(transform.position + new Vector3(0, -1, 0), transform.forward * DistanceForwardVector, Color.red);

        Ray rayMid = new Ray(transform.position + new Vector3(0, 0.3f, 0), transform.forward);
        Debug.DrawRay(transform.position + new Vector3(0, 0.3f, 0), transform.forward * DistanceForwardVector, Color.yellow);

        Ray rayTop = new Ray(transform.position + new Vector3(0, 1, 0), transform.forward);
        Debug.DrawRay(transform.position + new Vector3(0, 1, 0), transform.forward * DistanceForwardVector, Color.green);

        if (Physics.Raycast(rayMid, DistanceForwardVector, GrabMask) && moveCharacter.IsGround == false && moveCharacter.grabMovement == false)
        {
            moveCharacter.grabMovement = true; //включен режим зацепа 
            Physics.Raycast(rayMid, out hitRotation);
            moveCharacter.RotationToGrab(); //вызов функции выравнивания персонажа перпендикулярно нормали к поверхности 
        }
    }
}


