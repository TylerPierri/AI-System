using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))] // requires this to run
[RequireComponent(typeof(AudioSource))] // requires this to play audio
[DefaultExecutionOrder(1)]

public class EnemyAgent : MonoBehaviour
{
    private NavMeshAgent MeshAgent;
    private Game_Info info;
    private AIManager ManagerAI;
    private AudioSource audioSource;

    public Vector3 PlayerLocation;
    public Vector3 LatestPlayerLocation;
    public enum Enemy_Type // chosen enemy type
    {
        HoverBot,
        Drone,
        HeavyBot
    }

    [Header("Enemy info")]
    public Enemy_Type EnemyType;
    public int EnemyLevel;
    public Text EnemyLevelText;
    [Range(1,20)] public float DetectionRadius;
    public bool Hostel;
    public bool EnemyInPlayerView = false;
    public AudioClip MovingSFX;

    //shield bot notification
    public bool ShieldBotActive = false;
    public Vector3 ShieldGenerateLocation;

    [Header("Health info")]
    public float MaxHealth;
    public float CurrentHealth;
    [Range(10,50)] public float RegenerationRate;
    private float LowHealthAlert;
    private float RunAwayChance;
    private bool RunAway = false;
    [HideInInspector] public bool RunningAwayForRegen = false;
    private bool HealthChange = false;
    public Slider HealthSlider;
    public Slider BackGroundHealthSlider;

    [Header("Attack info")]
    public bool Alerted = false;
    [Range(1, 30)] public float AlertRadius;
    [Range(0, 2)] public float Accuracy; // 0 is pinpoint accuracte
    public bool AllowedToAttack = false;
    public LayerMask PlayerMask;
    public LayerMask ObsticleMask;
    public LayerMask EnemyMask;

    [Header("Projectile info")]
    public GameObject Projectile; // prefabs to spawn in
    public float SpeedOfProjectile;
    public float Damage;
    [Range(0, 5)] public float Bloom;
    public float interval;
    public float startInterval; // timer for set intervals 
    public GameObject[] BarrelLocations;
    public AudioClip[] ShootSFX;


    [HideInInspector] public bool Chasing = false;
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
        interval = startInterval;

        //audioSource.clip = MovingSFX;
        //audioSource.Play();
    }

    void SetDifficulty() // changes the values based of difficulty chosen
    {
        switch (info.difficulty)
        {
            case Game_Info.Difficulty.Easy: // easy
                MeshAgent.speed = MeshAgent.speed / 1.2f;
                DetectionRadius = DetectionRadius / 1.2f;
                AlertRadius = AlertRadius / 1.2f;
                Accuracy = Accuracy * 1.5f;
                MaxHealth = MaxHealth / 1.2f;
                Damage = Damage / 1.2f;
                LowHealthAlert = MaxHealth / 1.9f;
                RunAwayChance = Random.Range(0, 0.8f);
                RegenerationRate = RegenerationRate / 1.2f;
                Bloom = Bloom + (Bloom * 1.2f);
                break;

            case Game_Info.Difficulty.Normal: // normal
                //normal shouldnt change stats
                LowHealthAlert = MaxHealth / 1.6f;
                RunAwayChance = Random.Range(0, 0.5f);
                break;

            case Game_Info.Difficulty.Hard: // hard
                MeshAgent.speed = MeshAgent.speed * 1.2f;
                DetectionRadius = DetectionRadius * 1.2f;
                AlertRadius = AlertRadius * 1.2f;
                Accuracy = Accuracy / 1.5f;
                MaxHealth = MaxHealth * 1.2f;
                Damage = Damage * 1.2f;
                LowHealthAlert = MaxHealth / 1.4f;
                RunAwayChance = Random.Range(0, 0.2f);
                RegenerationRate = RegenerationRate * 1.2f;
                Bloom = Bloom - (Bloom * 1.8f);
                break;
        }

        //sets values extra based off level
        DiffernceInLevel = (1 - (info.PlayerLevel / EnemyLevel)) * 2; // checks player level compaired to enemy level
        MaxHealth = MaxHealth + (MaxHealth * DiffernceInLevel);
        Damage = Damage + (Damage * DiffernceInLevel);
        RegenerationRate = RegenerationRate + (RegenerationRate * DiffernceInLevel);
        XpReward = XpReward + (XpReward * DiffernceInLevel);
    }

    void SetAgent()
    {
        ManagerAI.enemies.Add(gameObject.GetComponent<EnemyAgent>());

        switch (EnemyType) // updates the correct enemy type
        {
            case Enemy_Type.HoverBot:
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

            case Enemy_Type.Drone:
                foreach (GameObject ScoutPos in GameObject.FindObjectsOfType(typeof(GameObject)))
                {
                    // add something to detech if its finding the right area as itll now look for every scout position in scene
                    if (ScoutPos.tag == "Drone Scout Point")
                    {
                        //Debug.Log("Found: " + ScoutPos.name);
                        ScoutPositions.Add(ScoutPos);
                    }

                }

                if (ScoutPositions == null)
                    Debug.LogError("No Scout Points found for: " + EnemyType);


                break;

            case Enemy_Type.HeavyBot:
                foreach (GameObject ScoutPos in GameObject.FindObjectsOfType(typeof(GameObject)))
                {
                    // add something to detech if its finding the right area as itll now look for every scout position in scene
                    if (ScoutPos.tag == "Heavy Bot Scout Point")
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
        if (Vector3.Distance(transform.position, PlayerLocation) < DetectionRadius && Hostel)// Chase Player
        {
            if (CurrentHealth < LowHealthAlert && Random.value < RunAwayChance && !RunAway)// enemy will run when too low on health
            {               
                RunningAwayForRegen = true;
            }
            else
            {
                if (!RunningAwayForRegen)
                {
                    switch (EnemyType) // updates the correct enemy type
                    {
                        case Enemy_Type.HoverBot:
                            MoveAi();
                            break;

                        case Enemy_Type.Drone:
                            MoveAi();
                            break;

                        case Enemy_Type.HeavyBot:
                            MoveAi();
                            break;
                    }
                }
            }
        }
        else //patrol
        {
            if(!Alerted && !RunningAwayForRegen)
                Patrol();
        }

        Health();
        CheckIfInSight();

        if(RunningAwayForRegen && EnemyType != Enemy_Type.HeavyBot) // if its a heavy bot it wont run away
            StartCoroutine(RunningAway());

        if(EnemyType == Enemy_Type.Drone) // makes the drone go up and down with the destination
            gameObject.transform.GetChild(0).GetComponentInChildren<DroneHover>().posOffset = transform.position;


    }

    IEnumerator RunningAway()
    {       
        if (!HealthChange) // alerts others for help
        {
            HealthChange = true;
            MeshAgent.speed = MeshAgent.speed / 1.5f;
            if (ManagerAI.enemies != null)
                StartCoroutine(AlertSurrondingAI());
        }

        if (!ShieldBotActive) // is there a shield bot active nearby?
        {
            if (Vector3.Distance(transform.position, ScoutPositions[randomPoint].transform.position) > 1f)
            {
                MeshAgent.SetDestination(ScoutPositions[randomPoint].transform.position); // run to a point
            }
            else
            {
                MeshAgent.SetDestination(transform.position); // stay in one place

                if (CurrentHealth < (MaxHealth / 1.4f)) // begin regen
                {
                    CurrentHealth += RegenerationRate * Time.deltaTime;
                    //Debug.Log(MaxHealth);
                }
                else
                {
                    RunningAwayForRegen = false;
                    //Debug.Log("Finished Regen");
                    yield return new WaitForSeconds(2f); //allows the ai to get back with some health
                    RunAway = false;
                }

            }
        }
        else
        {
            if (Vector3.Distance(transform.position, ShieldGenerateLocation) > 1f)
            {
                MeshAgent.SetDestination(ShieldGenerateLocation); // run to a point
            }
            else
            {
                MeshAgent.SetDestination(transform.position); // stay in one place

                if (CurrentHealth < (MaxHealth / 1.4f)) // begin regen
                {
                    CurrentHealth += RegenerationRate * Time.deltaTime;
                    //Debug.Log(CurrentHealth);
                }
                else
                {
                    RunningAwayForRegen = false;
                    //Debug.Log("Finished Regen");
                    yield return new WaitForSeconds(2f); //allows the ai to get back with some health
                    RunAway = false;
                }

            }
        }
    }

    void CheckIfInSight()
    {
        if (EnemyInPlayerView)
        {
            if (CountDown > 0)
                CountDown -= 1 * Time.deltaTime;
            else
                StartCoroutine(OutOfSight(0.5f));
        }
    }

    public void Insights(float Delay)
    {
        if (!RunningAwayForRegen)
        {
            CountDown = Delay; // resets clock

            if (CountDown > 0 && !EnemyInPlayerView)
            {
                EnemyInPlayerView = true;
                //Debug.Log("Target In Sights");
            }
        }
    }

    public IEnumerator OutOfSight(float Delay)
    {            
        EnemyInPlayerView = false;
        Debug.Log("Target has looked away");
        yield return new WaitForSeconds(Delay);

        if (AllowedToAttack)
        {
            int index = ManagerAI.AttackingEnemies.IndexOf(gameObject.GetComponent<EnemyAgent>());

            if (ManagerAI.AttackingEnemies != null)
                ManagerAI.AttackingEnemies.RemoveAt(index);
        }

        AllowedToAttack = false;
        //CountDown = Delay;
        
    }

    IEnumerator AlertSurrondingAI()
    {
        Debug.Log("Alerted");
        Vector3 AlertLocation;
        for (int i = 0; i < ManagerAI.enemies.Count; i++)
        {
            if (Vector3.Distance(transform.position, ManagerAI.enemies[i].transform.position) < AlertRadius) // finds enemies within this enemies distance to alert
            {
                if (!ManagerAI.enemies[i].GetComponent<EnemyAgent>().RunningAwayForRegen)
                    ManagerAI.enemies[i].GetComponent<EnemyAgent>().Alerted = true;

                AlertLocation = new Vector3(
                    transform.position.x + 4 * Mathf.Cos(2 * Mathf.PI * i / ManagerAI.enemies.Count),
                    transform.position.y,
                    transform.position.z + 4 * Mathf.Sin(2 * Mathf.PI * i / ManagerAI.enemies.Count));

                if (!ManagerAI.enemies[i].GetComponent<EnemyAgent>().RunningAwayForRegen)
                {
                    ManagerAI.enemies[i].GetComponent<NavMeshAgent>().SetDestination(AlertLocation);
                    ManagerAI.enemies[i].GetComponent<NavMeshAgent>().speed = ManagerAI.enemies[i].GetComponent<NavMeshAgent>().speed * 3;
                    ManagerAI.enemies[i].GetComponent<EnemyAgent>().Chasing = false;
                }
            }
        }

        yield return new WaitForSeconds(3);
        Alerted = false;
        //HealthChange = false;
    }
    void Patrol() // patrol to different locations
    {
        MeshAgent.SetDestination(ScoutPositions[randomPoint].transform.position); // moves the enemy to the randomly chosen point

        Quaternion RotTarget = Quaternion.LookRotation(ScoutPositions[randomPoint].transform.position - transform.position);
        //Debug.Log(RotTarget);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, RotTarget, 90 * Time.deltaTime);

        //transform.LookAt(ScoutPositions[randomPoint].transform.position);

        if (!Chasing)
        {
            MeshAgent.speed = MeshAgent.speed / 3f;
            Chasing = true;
        }
        

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

    void MoveAi() // moves Ai accordingly
    {

        if (!EnemyInPlayerView)
        {
            if (Chasing)
            {
                Debug.Log("Player Found");
                MeshAgent.speed = MeshAgent.speed * 3f;
                Alerted = false;
                Chasing = false;
            }
            ManagerAI.MakeAgentCircleTarget(); // get the circle location for the player
            StartCoroutine(GetPlayerLocation());
        }
        else
        {
            //if (Vector3.Distance(transform.position, PlayerLocation) > 5f)
                AmIAttack();
            //else
                //MeshAgent.SetDestination(ScoutPositions[randomPoint].transform.position);
        }
    }
    IEnumerator GetPlayerLocation()
    {
        LatestPlayerLocation = PlayerLocation; // gets the player position
        yield return new WaitForSeconds(Accuracy); // adds a delay (accuracy against player)
        if(Vector3.Distance(transform.position, LatestPlayerLocation) < 2f)
        {

            if (EnemyInPlayerView)  // if the enemy is within the player view
            {             
                AmIAttack();
            }

        }
        else
        {
            MeshAgent.SetDestination(LatestPlayerLocation);
        }
        
    }

    void AmIAttack()
    {
        //transform.LookAt(GameObject.FindWithTag("Player").transform.position);
        //Vector3 newDirection = Vector3.RotateTowards(transform.forward, LatestPlayerLocation, 90, 0.0f);
        //transform.rotation = Quaternion.LookRotation(newDirection);

        //find a way to make the nav mesh look at the player when attacking!---------------------------------------------------------------
        
        if(!AllowedToAttack)
        {
            if (ManagerAI.AttackingEnemies.Count < ManagerAI.MaxAmountOfEnemiesAttacking || ManagerAI.AttackingEnemies == null)
                ManagerAI.AttackingEnemies.Add(gameObject.GetComponent<EnemyAgent>());
        }
           

        ManagerAI.CheckForAttackPermission();// check that there is a maximum amount of enemies that are allowed to shoot at a given time



        if(AllowedToAttack)
        {
            //check if player doesnt have anything inbetween them

            Collider[] TargetsInViewRadius = Physics.OverlapSphere(transform.position, DetectionRadius, PlayerMask);

            for (int i = 0; i < TargetsInViewRadius.Length; i++)
            {
                Transform Target = GameObject.FindWithTag("Player").transform;
                Vector3 DirectionToTarget = (Target.position - transform.position).normalized;

                float DistanceToTarget = Vector3.Distance(transform.position, Target.position);

                if (!Physics.Raycast(transform.position, DirectionToTarget, DistanceToTarget, ObsticleMask) &&
                    !Physics.Raycast(transform.position, DirectionToTarget, DistanceToTarget, EnemyMask))
                {
                    // the player is in view with no obsticales in the way

                    MeshAgent.SetDestination(transform.position);
                    Attacking();
                }
                else
                    return;
            }

           
        }
        else
        {
            MeshAgent.SetDestination(LatestPlayerLocation);
        }
    }

    void Attacking()
    {
        //Debug.Log("Attacking");

        switch (EnemyType) // updates the correct enemy type
        {
            case Enemy_Type.HoverBot:
                ShootPlayer(new Vector3(
                    GameObject.FindWithTag("Player").transform.position.x + Random.Range(-Bloom, Bloom),
                    GameObject.FindWithTag("Player").transform.position.y + 1f + Random.Range(-Bloom, Bloom),
                    GameObject.FindWithTag("Player").transform.position.z + Random.Range(-Bloom, Bloom)
                    ));
                break;

            case Enemy_Type.Drone:
                ShootPlayer(new Vector3(
                    GameObject.FindWithTag("Player").transform.position.x + Random.Range(-Bloom, Bloom),
                    GameObject.FindWithTag("Player").transform.position.y + 1f + Random.Range(-Bloom, Bloom),
                    GameObject.FindWithTag("Player").transform.position.z + Random.Range(-Bloom, Bloom)
                    ));
                break;

            case Enemy_Type.HeavyBot:
                ShootPlayer(new Vector3(
                    GameObject.FindWithTag("Player").transform.position.x + Random.Range(-Bloom, Bloom), 
                    GameObject.FindWithTag("Player").transform.position.y - 0.2f + Random.Range(-Bloom, Bloom), 
                    GameObject.FindWithTag("Player").transform.position.z + Random.Range(-Bloom, Bloom)
                    ));
                break;
        }
    }

    public void ShootPlayer(Vector3 Target)
    {
        // look at the players position when firing
        //transform.LookAt(GameObject.FindWithTag("Player").transform.position);

        Quaternion RotTarget = Quaternion.LookRotation(GameObject.FindWithTag("Player").transform.position - transform.position);
        //Debug.Log(RotTarget);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, RotTarget, 90 * Time.deltaTime);

        //Vector3 newDirection = Vector3.RotateTowards(transform.forward, LatestPlayerLocation, 90, 0.0f);
        //transform.rotation = Quaternion.LookRotation(newDirection);

        int i = 0;
        if (BarrelLocations.Length == 1)
            i = 0;
        else
        {
            if (i > BarrelLocations.Length)
                i = 0;
            else
                i++;
        }

        if (interval <= 0) // when the interval allows another bullet to spawn
        {
            if (Projectile != null)
            {
                GameObject ProjectileSpawn = Instantiate(Projectile, BarrelLocations[i].transform.position, Quaternion.identity); //spawns a clone bullet, at a specific location, with identical rotation
                ProjectileSpawn.GetComponent<EnemyProjectile>().SetUp(Target);
                ProjectileSpawn.GetComponent<EnemyProjectile>().Damage = Damage;
                ProjectileSpawn.GetComponent<EnemyProjectile>().SpeedOfProjectile = SpeedOfProjectile;
                audioSource.clip = ShootSFX[Random.Range(0, ShootSFX.Length)];
                audioSource.pitch = Random.Range(0.5f, 1.5f);
                audioSource.Play();
            }    
            else
                Debug.LogError("No Projectile to spawn in");

            interval = startInterval; // sets the interval timer
        }
        else
        {
            interval -= Time.deltaTime; //counts down the interval
        }
    }

    void DeSpawn() // despawn the enemy
    {
        int index = ManagerAI.enemies.IndexOf(gameObject.GetComponent<EnemyAgent>());
        if(ManagerAI.enemies != null)
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
        
        if(DoesThisDrop)
        {
            //random object on death
            item Item = null;           
            if(Random.value < 0.5f) // chance to spawn
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
