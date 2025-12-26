using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class ZombieMovement : MonoBehaviour
{
    public GameObject target;
    public float speed;


    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
    }
}
