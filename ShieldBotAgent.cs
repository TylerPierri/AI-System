using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))] // requires this to run
[RequireComponent(typeof(AudioSource))] // requires this to play audio
[DefaultExecutionOrder(2)]
public class ShieldBotAgent : MonoBehaviour
{
    private NavMeshAgent MeshAgent;
    private Game_Info info;
    private AIManager ManagerAI;
    private AudioSource audioSource;

    public Vector3 PlayerLocation;
    public enum Enemy_Type // chosen enemy type
    {
        ShieldBot
    }

    [Header("SHield Bot info")]
    public Enemy_Type EnemyType;
    public int EnemyLevel;
    public Text EnemyLevelText;
    [Range(1, 20)] public float DetectionRadius;
    public bool EnemyInPlayerView = false;
    public bool ShieldGenerated = false;
    public GameObject Shield;
    public float ShieldSize;

    [Header("Health info")]
    public float MaxHealth;
    public float CurrentHealth;
    //[Range(10, 50)] public float RegenerationRate;
    private float LowHealthAlert;
    private bool RunAway = false;
    [HideInInspector] public bool RunningAwayForShield = false;
    [HideInInspector] public bool RunningTowardsForShield = false;
    public Slider HealthSlider;
    public Slider BackGroundHealthSlider;

    [Header("Attack info")]
    public bool Alerted = false;
    [Range(1, 30)] public float AlertRadius;

    [Header("Projectile info")]
    public float Damage;

    [HideInInspector] public float CountDown = 1;

    [Header("Scout Routes")]
    public List<GameObject> ScoutPositions = new List<GameObject>();
    private int randomPoint; // random point out of the array
    private float IdleTime; // current idle time
    private float StartIdleTime = 3; // how long the full idle sequence last for

    [Header("Loot Settings")]
    public float XpReward;
    private float DiffernceInLevel;
    public bool DoesThisDrop;
    public GameObject SpawnKeyObject;
    public Loot_Table ChosenLootTable;
    //Loot Table

    private void Awake()
    {
        MeshAgent = GetComponent<NavMeshAgent>();
        info = FindObjectOfType<Game_Info>();
        ManagerAI = FindObjectOfType<AIManager>();
        audioSource = GetComponent<AudioSource>();
        SetDifficulty();
        SetAgent();

        randomPoint = Random.Range(0, ScoutPositions.Count); // picks a random random point inbetween 0 and the length of the array
        CurrentHealth = MaxHealth;
        HealthSlider.maxValue = MaxHealth;
        BackGroundHealthSlider.maxValue = MaxHealth;

        EnemyLevelText.text = EnemyLevel.ToString();
        Shield.SetActive(false);
    }

    void SetDifficulty() // changes the values based of difficulty chosen
    {
        switch (info.difficulty)
        {
            case Game_Info.Difficulty.Easy: // easy
                MeshAgent.speed = MeshAgent.speed / 1.2f;
                DetectionRadius = DetectionRadius / 1.2f;
                AlertRadius = AlertRadius / 1.2f;
                MaxHealth = MaxHealth / 1.2f;
                Damage = Damage / 1.2f;
                LowHealthAlert = MaxHealth / 1.9f;
                break;

            case Game_Info.Difficulty.Normal: // normal
                //normal shouldnt change stats
                LowHealthAlert = MaxHealth / 1.6f;
                break;

            case Game_Info.Difficulty.Hard: // hard
                MeshAgent.speed = MeshAgent.speed * 1.2f;
                DetectionRadius = DetectionRadius * 1.2f;
                AlertRadius = AlertRadius * 1.2f;
                MaxHealth = MaxHealth * 1.2f;
                Damage = Damage * 1.2f;
                LowHealthAlert = MaxHealth / 1.4f;
                break;
        }

        //sets values extra based off level
        DiffernceInLevel = (1 - (info.PlayerLevel / EnemyLevel)) * 2; // checks player level compaired to enemy level
        MaxHealth = MaxHealth + (MaxHealth * DiffernceInLevel);
        Damage = Damage + (Damage * DiffernceInLevel);
        XpReward = XpReward + (XpReward * DiffernceInLevel);
    }

    void SetAgent()
    {
        switch (EnemyType) // updates the correct enemy type
        {
            case Enemy_Type.ShieldBot:
                foreach (GameObject ScoutPos in GameObject.FindObjectsOfType(typeof(GameObject)))
                {
                    // add something to detech if its finding the right area as itll now look for every scout position in scene
                    if (ScoutPos.tag == "Hover Bot Scout Point")
                    {
                        //Debug.Log("Found: " + ScoutPos.name);
                        ScoutPositions.Add(ScoutPos);
                    }

                }

                if (ScoutPositions == null)
                    Debug.LogError("No Scout Points found for: " + EnemyType);

                break;

        }
    }
    private void FixedUpdate()
    {
        PlayerLocation = GameObject.FindWithTag("Player").transform.position;
        Health();
        CheckIfInSight();
        gameObject.transform.GetChild(0).GetComponentInChildren<DroneHover>().posOffset = transform.position;
        gameObject.transform.GetChild(0).transform.Rotate(0, 60 * Time.deltaTime, 0);

        if (!ShieldGenerated)
        {
            if (Vector3.Distance(transform.position, PlayerLocation) < DetectionRadius)// Chase Player
            {
                RunningTowardsForShield = true;
            }
            else //patrol
            {
                if (!Alerted && !RunningAwayForShield && !RunningTowardsForShield)
                    Patrol();
            }
           

            //if (RunningAwayForShield)
                //StartCoroutine(RunningAway());

            if (EnemyInPlayerView || RunningTowardsForShield)
            {
                StartCoroutine(RunningTowards());
            }
                
        }
        if(ShieldGenerated) 
        {
            Shield.SetActive(true);
            MeshAgent.SetDestination(transform.position);

            if (Shield.transform.localScale.x < ShieldSize)
            {
                Shield.transform.localScale = new Vector3(
                    Shield.transform.localScale.x + ShieldSize * Time.deltaTime, 
                    Shield.transform.localScale.y + ShieldSize * Time.deltaTime, 
                    Shield.transform.localScale.z + ShieldSize * Time.deltaTime);
            }

            if(Vector3.Distance(transform.position, PlayerLocation) < ShieldSize / 5)// if player is within generated shield
            {
                Debug.Log("Deal Damage to player");
            }

            for (int i = 0; i < ManagerAI.enemies.Count; i++) // heals enemies within
            {
                if (Vector3.Distance(transform.position, ManagerAI.enemies[i].transform.position) < ShieldSize / 6)
                {
                    if(ManagerAI.enemies[i].CurrentHealth < ManagerAI.enemies[i].MaxHealth)
                    {
                        //ManagerAI.enemies[i].CurrentHealth += 5 * Time.deltaTime;
                    }
                }
            }
        }
    }

    IEnumerator RunningTowards()
    {

        MeshAgent.SetDestination(PlayerLocation);

        Quaternion RotTarget = Quaternion.LookRotation(PlayerLocation - transform.position);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, RotTarget, 90 * Time.deltaTime);
        //transform.LookAt(PlayerLocation);

        yield return new WaitForSeconds(2);
        if (ManagerAI.enemies != null)
            AlertSurrondingAI();
    }

    void AlertSurrondingAI()
    {
        Debug.Log("Activating Shield");
        Vector3 ShieldLocation;
        for (int i = 0; i < ManagerAI.enemies.Count; i++)
        {
            if (Vector3.Distance(transform.position, ManagerAI.enemies[i].transform.position) < AlertRadius) // finds enemies within this enemies distance to alert
            {               
                ShieldLocation = new Vector3(
                    transform.position.x + (ShieldSize / 5) * Mathf.Cos(2 * Mathf.PI * i / ManagerAI.enemies.Count),
                    transform.position.y,
                    transform.position.z + (ShieldSize / 5) * Mathf.Sin(2 * Mathf.PI * i / ManagerAI.enemies.Count));

                if (!ManagerAI.enemies[i].GetComponent<EnemyAgent>().RunningAwayForRegen)
                {
                    ManagerAI.enemies[i].GetComponent<EnemyAgent>().ShieldGenerateLocation = ShieldLocation;
                    ManagerAI.enemies[i].GetComponent<EnemyAgent>().ShieldBotActive = true;
                }
            }
        }
        ShieldActivation();
    }

    void ShieldActivation()
    {
        Debug.Log("Shield Activated");
        ShieldGenerated = true;
    }
    void CheckIfInSight()
    {
        if (EnemyInPlayerView)
        {
            if (CountDown > 0)
                CountDown -= 1 * Time.deltaTime;
            else
                EnemyInPlayerView = false;
        }
    }
    public void Insights(float Delay)
    {
        if (!ShieldGenerated)
        {
            CountDown = Delay; // resets clock

            if (CountDown > 0 && !EnemyInPlayerView)
            {
                EnemyInPlayerView = true;
                //Debug.Log("Target In Sights");
            }
        }
    }
    
    void Patrol() // patrol to different locations
    {
        MeshAgent.SetDestination(ScoutPositions[randomPoint].transform.position); // moves the enemy to the randomly chosen point

        Quaternion RotTarget = Quaternion.LookRotation(ScoutPositions[randomPoint].transform.position - transform.position);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, RotTarget, 90 * Time.deltaTime);
        //transform.LookAt(ScoutPositions[randomPoint].transform.position);

        if (Vector3.Distance(transform.position, ScoutPositions[randomPoint].transform.position) < 4f) // if the enemy is too close too the chosen point
        {
            if (IdleTime <= 0) // stops until idle time hits 0
            {
                randomPoint = Random.Range(0, ScoutPositions.Count); // select a new random point
                IdleTime = StartIdleTime; // sets the length of the idle time
            }
            else
            {
                IdleTime -= StartIdleTime; // begins count down of idle time
            }
        }
    }

    void DeSpawn() // despawn the enemy
    {
        if (Shield.transform.localScale.x > 0)
        {
            Shield.transform.localScale = new Vector3(
                Shield.transform.localScale.x - ShieldSize * Time.deltaTime,
                Shield.transform.localScale.y - ShieldSize * Time.deltaTime,
                Shield.transform.localScale.z - ShieldSize * Time.deltaTime);
        }
        Shield.SetActive(false);

        int index = ManagerAI.enemies.IndexOf(gameObject.GetComponent<EnemyAgent>());
        if (ManagerAI.enemies != null)
            ManagerAI.enemies.RemoveAt(index);
        Loot();
    }

    void Health() // checks on enemy health
    {

        if (CurrentHealth <= 0) // check for health
            DeSpawn();

        HealthSlider.value = CurrentHealth;

        if (BackGroundHealthSlider.value > HealthSlider.value)
            BackGroundHealthSlider.value -= (CurrentHealth - LowHealthAlert) * Time.deltaTime;
        else
            BackGroundHealthSlider.value = HealthSlider.value;
    }

    public void TakeDamage(float Damage)
    {
        CurrentHealth -= Damage;
    }

    void Loot()
    {
        if (SpawnKeyObject != null) // if this enemy must spawn a item needed for progress in game
            Instantiate(SpawnKeyObject, transform.position, Quaternion.identity);

        info.XpProgress += XpReward;

        if (DoesThisDrop)
        {
            //random object on death
            item Item = null;
            if (Random.value < 0.5f) // chance to spawn
            {
                Item = ChosenLootTable.GetDrop();
                Instantiate(Item, transform.position, Quaternion.identity);

                if (Random.value < 0.8f) // chance to spawn an additional reward
                {
                    Item = ChosenLootTable.GetDrop();
                    Instantiate(Item, transform.position, Quaternion.identity);
                }
            }

        }


    }
}
