using UnityEngine;

public class Spawn : MonoBehaviour
{
    public int numberOfEnemy;
    public GameObject objectToSpawn;


    private void Start()
    {
        for (int i = 0; i < numberOfEnemy; i++)
        {
            Vector3 randomPosition = new Vector3(Random.Range(11, -11), 0 , Random.Range(10, -11));
            Instantiate(objectToSpawn, randomPosition, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
