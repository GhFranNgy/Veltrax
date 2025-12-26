using UnityEngine;

public class Spawn : MonoBehaviour
{
    //Gameobject und Menge von Zombie
    public int numberOfEnemy;
    public GameObject objectToSpawn;

    //Follow-Target, speed, Abstand
    public Transform player;
    public float speed;
    public float separationDistance = 1.2f; 
    public float separationForce = 2f;

    private GameObject[] Enemies;

    void Start()
    {
        Enemies = new GameObject[numberOfEnemy];

        for (int i = 0; i < numberOfEnemy; i++)
        {
            Vector3 randomPosition = new Vector3(Random.Range(11, -11), 0f , Random.Range(10, -11));//Position to Spawn
            Enemies[i] = Instantiate(objectToSpawn, randomPosition, Quaternion.identity);//Spawn
        };
    }

    void Update()
    {
        if (player == null) return; 

        for (int i = 0; i < Enemies.Length; i++) 
        { 
            GameObject obj = Enemies[i]; 
            if (obj == null) continue;
             // Follow movement 
             Vector3 direction = (player.position - obj.transform.position).normalized; 
             Vector3 move = direction * speed; 
             
             // Separation 
             Vector3 separation = Vector3.zero;
            
            for (int j = 0; j < Enemies.Length; j++) 
            { 
                if (i == j) continue;
                GameObject other = Enemies[j]; 

                if (other == null) continue;
                float dist = Vector3.Distance(obj.transform.position, other.transform.position);

                if (dist < separationDistance) 
                { 
                    Vector3 pushAway = (obj.transform.position - other.transform.position).normalized; 
                    separation += pushAway * (separationDistance - dist) * separationForce; 
                } 
            } 
            
            // Apply movement 
            obj.transform.position += (move + separation) * Time.deltaTime; } 
            }
    }

