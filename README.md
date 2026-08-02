# BestInTheWorld — Code Review & Deep Explanation

A review of the 5 scripts in `Assets/Scripts/`, written for a Unity beginner.
**Nothing in your project was changed.** This file explains every script line-by-line,
asks you questions about your decisions, and lists concrete improvements you can
apply later.

---

## Table of contents

1. [What your game actually is](#1-what-your-game-actually-is)
2. [The script-by-script deep explanation](#2-the-script-by-script-deep-explanation)
   - [PlayerController.cs](#playercontrollercs)
   - [SpawnManager.cs](#spawnmanagercs)
   - [FoodController.cs](#foodcontrollercs)
   - [CameraSwitcher.cs](#cameranswitchercs)
   - [LevelData.cs](#leveldatacs)
3. [How the scripts connect (data flow)](#3-how-the-scripts-connect-data-flow)
4. [What you did well](#4-what-you-did-well)
5. [Questions about your decisions — please answer](#5-questions-about-your-decisions--please-answer)
6. [What could be better (no changes made)](#6-what-could-be-better-no-changes-made)
7. [Key Unity concepts (the mental models to remember)](#7-key-unity-concepts-the-mental-models-to-remember)
8. [Glossary](#8-glossary)

---

## 1. What your game actually is

Reading only the scripts, this is what I understand:

- A **top-down arcade game**. A round character (a "ball") sits on a flat plane.
  - The camera is set up so you look down at it.
  - The intro camera and gameplay camera are both **Cinemachine** cameras, and a
    **Timeline** (`PlayableDirector`) plays an intro cutscene first.
- **Movement:** WASD / left stick. The character is a physics object (`Rigidbody`).
  You push it with force, it rolls/spins faster the faster it travels, and its speed
  is capped.
- **Food:** The `SpawnManager` continuously spawns food near the player (up to 20
  alive at once). Food despawns if it gets too far from the player. Eating food
  destroys it.
- **Character switching:** You have a list of character prefabs and a method to
  swap the current character (probably bound to the **1** and **2** keys / gamepad
  d-pad, which exist in your input actions file).

The scripts: `PlayerController`, `SpawnManager`, `FoodController`, `CameraSwitcher`,
`LevelData`.

---

## 2. The script-by-script deep explanation

### PlayerController.cs

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
```

- `using UnityEngine.InputSystem;` — you use the **new Input System** package
  (Unity 6's default). This is the modern way to read input.
- `[RequireComponent(typeof(Rigidbody))]` — this is an **attribute** (metadata).
  It tells Unity: "this GameObject cannot exist without a Rigidbody." If you attach
  this script to an object that has no Rigidbody, Unity **automatically adds one**.
  It protects you from a `null` Rigidbody later.

```csharp
private SpawnManager spawnManager;
private Rigidbody playerRb;

[SerializeField] private GameObject currentPlayer;
[SerializeField] private GameObject[] playersList;

[SerializeField] private InputAction movementAction;

[SerializeField] private float speed = 0.2f;
[SerializeField] private float maxSpeed = 0.5f;
private float horizontalInput;
private float verticalInput;

[SerializeField] private float spinSpeed = 10f;
[SerializeField] private float speedMultiplier = 2f;
private float spinDirection = 1f;
private float currentZRotation = 180f;
```

- `[SerializeField]` makes a **private** field visible and editable in the Inspector.
  Without it, private fields are hidden in the editor. This is the correct pattern
  for exposing tunable values while keeping them private (encapsulation).
- `currentPlayer` — the current character visual, a **child** of this object.
- `playersList` — array of character **prefabs** you can swap between.
- `movementAction` — a reference to an `InputAction`. You drag the "Move" action
  from your `.inputactions` asset onto this field in the Inspector.
- `horizontalInput` / `verticalInput` — plain private fields (not serialized, not
  tuned in Inspector). They are the **bridge** between `Update()` (reads input) and
  `FixedUpdate()` (moves physics).
- The `spin` fields: `spinSpeed` (base spin rate), `speedMultiplier` (how much
  speed adds to spin), `spinDirection` (+1 or -1 based on left/right input),
  `currentZRotation` (accumulator that counts up in degrees — starts at 180°).

```csharp
private void Start()
{
    playerRb = GetComponent<Rigidbody>();
    spawnManager = GameObject.Find("SpawnManager").GetComponent<SpawnManager>();

    movementAction.Enable();
}
```

- `Start()` runs **once**, on the first frame the object is active.
- `GetComponent<Rigidbody>()` — finds the Rigidbody attached to *this same
  GameObject* and caches it in `playerRb`. Caching avoids calling `GetComponent`
  every frame (it is not expensive, but caching is cleaner and standard).
- `GameObject.Find("SpawnManager")` — searches the **entire scene** by name for a
  GameObject called "SpawnManager", then grabs its `SpawnManager` component.
  ⚠️ This works, but it is fragile (see [section 6](#6-what-could-be-better-no-changes-made)).
- `movementAction.Enable()` — an `InputAction` is **disabled by default**. It will
  not read any input until you call `.Enable()`. This is a step people often forget.

```csharp
public void ReplacePlayer(int index)
{
    Vector3 localPos = currentPlayer.transform.localPosition;

    Destroy(currentPlayer);

    currentPlayer = Instantiate(playersList[index], transform);

    currentPlayer.transform.localPosition = localPos;
}
```

- Public method — so something *outside* this script can call it (a UI button, an
  animation event, or another script). **Note:** nothing in your current scripts
  calls it — it must be wired up in the scene.
- `localPosition` is saved **before** destroying the old character. Good order.
- `Destroy(currentPlayer)` — asks Unity to delete it at the end of the frame.
- `Instantiate(playersList[index], transform)` — creates a new copy of the chosen
  prefab as a **child** of the player object (the `transform` argument makes it a
  child, so it inherits position/rotation from the Rigidbody object).
- Then it re-applies the saved local position so the new character appears in the
  same spot the old one was.
- **No bounds check** — if `index` is bigger than the array length, this errors.

```csharp
void Update()
{
    Vector2 movement = movementAction.ReadValue<Vector2>();

    horizontalInput = -movement.x;
    verticalInput = movement.y;

    if (horizontalInput > 0.01f)
        spinDirection = 1f;
    else if (horizontalInput < -0.01f)
        spinDirection = -1f;

    float spin = spinSpeed + playerRb.linearVelocity.magnitude * speedMultiplier;
    currentZRotation += spin * spinDirection * Time.deltaTime;

    transform.rotation = Quaternion.Euler(-90, 0, currentZRotation);
}
```

- `Update()` runs **every rendered frame** (~60+ times/second). Good place for
  *reading input* and *visual* changes.
- `ReadValue<Vector2>()` returns the joystick/WASD axis as an X and Y value
  (e.g. pressing D gives roughly `(1, 0)`).
- `horizontalInput = -movement.x;` — the **minus sign** inverts horizontal input.
  See [question Q1](#5-questions-about-your-decisions--please-answer).
- The `0.01f` dead-zone check ignores near-zero stick drift, and only then decides
  which way the character should *roll* (spin).
- `playerRb.linearVelocity.magnitude` — the speed of the ball right now. So the
  faster it moves, the faster it spins — a **rolling-ball illusion**. (Note: on
  Unity 6 the property is `linearVelocity`; on older Unity it was `velocity`.)
- `currentZRotation += spin * spinDirection * Time.deltaTime;` — a running
  accumulator in **degrees**. `Time.deltaTime` converts "per second" values into
  "per frame" values, so the spin speed is framerate-independent. This is the
  classic pattern: `accumulator += valuePerSecond * Time.deltaTime`.
- `transform.rotation = Quaternion.Euler(-90, 0, currentZRotation);` — sets the
  object's rotation every frame. The `-90` on X tilts the object (probably to lay
  the character "flat" / face it into the camera view), and `currentZRotation` is
  the roll around its own axis. Because you *set absolute rotation* from an
  accumulator, the ball spins continuously.

```csharp
void FixedUpdate()
{
    Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput);

    playerRb.AddForce(moveDirection * speed);

    if (playerRb.linearVelocity.magnitude > maxSpeed)
    {
        playerRb.linearVelocity = playerRb.linearVelocity.normalized * maxSpeed;
    }
}
```

- `FixedUpdate()` runs at a **fixed physics rate** (default 50 times per second).
  **All physics/Rigidbody changes must go here**, not in `Update()`.
- `moveDirection` builds a 3D direction from the 2D input: `X = horizontal`,
  `Y = 0` (flat plane), `Z = vertical`.
- `AddForce(moveDirection * speed)` — pushes the ball. Default `ForceMode.Force`
  means the acceleration = `force / mass`. So `speed` here is really a **force**
  value (0.2), and the actual speed depends on the Rigidbody's mass.
- Then it **caps velocity**: if the ball is going faster than `maxSpeed`, it
  scales the velocity down to exactly `maxSpeed` while keeping direction.
- Because `horizontalInput`/`verticalInput` were filled in `Update()` (which runs
  every frame) and read in `FixedUpdate()`, there is no lost input.

```csharp
private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Food"))
    {
        Destroy(other.gameObject);
        spawnManager.ObjectDestroyed();
    }
}
```

- `OnTriggerEnter` is a **physics callback** automatically called by Unity when
  this object's collider (set to "Is Trigger") overlaps another collider.
  `other` is the thing we hit.
- `CompareTag("Food")` — cheap way to check what we hit. Requires every food prefab
  to have the **"Food" tag** in the Inspector.
- Eat the food: destroy it, and tell the `SpawnManager` that one food object just
  died so it can spawn a replacement.

---

### SpawnManager.cs

```csharp
public class SpawnManager : MonoBehaviour
{
    private Transform playerTransform;

    [SerializeField] private LevelData[] levels;
    [SerializeField] private int maxSpawnedObjects = 20;
    [SerializeField] private float spawnInterval = 0.1f;
    private int currentSpawnedObjects = 0;
    [SerializeField] private int currentLevel = 1;
```

- `levels` — array of `LevelData`. Each element describes one level's spawnable
  food. This is your (very early) "level system".
- `maxSpawnedObjects` — how many food objects may exist at once.
- `spawnInterval` — seconds between spawn attempts.
- `currentSpawnedObjects` — a manual counter of how many are currently alive.
- `currentLevel` — which level (index into `levels`, 1-based here) is active.
  It is serialized (editable in Inspector) but **nothing ever changes it** — a
  leftover of a planned level system.

```csharp
void Start()
{
    playerTransform = GameObject.Find("Player").transform;
    InvokeRepeating(nameof(SpawnObject), 0f, spawnInterval);
}
```

- Finds the player the same fragile way (`GameObject.Find("Player")`).
- `InvokeRepeating(nameof(SpawnObject), 0f, spawnInterval)` — calls
  `SpawnObject()` the first time after 0 seconds, then every `spawnInterval`
  seconds forever. `nameof(SpawnObject)` is good practice: it compiles the method
  name so a rename/typo breaks the build instead of silently failing at runtime.

```csharp
void SpawnObject()
{
    if (currentSpawnedObjects >= maxSpawnedObjects)
        return;

    int randomIndex = Random.Range(0, levels[currentLevel - 1].spawnables.Length);
    GameObject prefab = levels[currentLevel - 1].spawnables[randomIndex];

    float spawnRadiusX = 10f;
    float spawnRadiusZ = 5f;

    Vector3 randomPosition = new Vector3(
        playerTransform.position.x + Random.Range(-spawnRadiusX, spawnRadiusX),
        0f,
        playerTransform.position.z + Random.Range(-spawnRadiusZ, spawnRadiusZ)
    );

    GameObject obj = Instantiate(prefab, randomPosition, Quaternion.identity);
    obj.transform.localScale = new Vector3(1, 1, 1);

    currentSpawnedObjects++;
}
```

- If already at the cap, do nothing (the repeat keeps firing every 0.1s, but it's
  cheap).
- `levels[currentLevel - 1]` — gets the current level's data. Since `currentLevel`
  defaults to 1, that's `levels[0]`. If `currentLevel` were 0 or greater than the
  array length, this would crash (`IndexOutOfRangeException`) — another reason to
  guard the index.
- `Random.Range(-10, 10)` picks a random float between -10 and 10. For **floats**
  the maximum is inclusive; for **ints** it is exclusive. (Here it doesn't matter.)
- The food spawns on a 20×10 rectangle centered on the player, at height `0`.
- `Quaternion.identity` = no rotation, straight up.
- `Instantiate(prefab, randomPosition, Quaternion.identity)` — copy the prefab into
  the world at that position.
- `obj.transform.localScale = new Vector3(1, 1, 1);` — this is **redundant**,
  since 1,1,1 is the default scale already (unless the prefab has a different
  default scale baked in — if you set scale in the prefab, this line *overwrites*
  it to 1).
- `currentSpawnedObjects++` — track that a new food exists.

```csharp
public void ObjectDestroyed()
{
    currentSpawnedObjects--;
}
```

- Public method so other scripts (PlayerController, FoodController) can tell the
  SpawnManager "one food died, I'm free to spawn another."

---

### FoodController.cs

```csharp
public class FoodController : MonoBehaviour
{
    [SerializeField] private float maxSpawnDistance = 15;

    private Transform playerTransform;
    private SpawnManager spawnManager;

    void Start()
    {
        playerTransform = GameObject.Find("Player").transform;
        spawnManager = GameObject.Find("SpawnManager").GetComponent<SpawnManager>();
    }
```

- This script lives on each **food prefab**. Every food does `GameObject.Find`
  twice in its own `Start()`. With up to 20 foods alive, that's 20 objects each
  searching the scene — wasteful and fragile.

```csharp
    void Update()
    {
        if (Vector3.Distance(playerTransform.position, transform.position) > maxSpawnDistance)
        {
            spawnManager.ObjectDestroyed();
            Destroy(gameObject);
            Debug.Log("Destroyed");
        }
    }
}
```

- Every frame, every food checks "am I farther than 15 units from the player?" If
  yes: tell the SpawnManager the count drops, delete itself, and log "Destroyed".
- `Vector3.Distance` computes a square root every frame per object. A cheaper trick
  is `sqrMagnitude` (see section 6).
- ⚠️ **Potential double-count bug:** if the player eats the food and, on the *same
  frame*, this food's `Update()` also runs its range check, `ObjectDestroyed()`
  can be called **twice** for one food, making `currentSpawnedObjects` go negative.
  (Not catastrophic, but a real subtle bug — see [Q11](#5-questions-about-your-decisions--please-answer)).
- `Debug.Log("Destroyed")` — a leftover debug line. Fine while developing, but it
  will spam the console; remove it for release.

---

### CameraSwitcher.cs

```csharp
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private CinemachineCamera introCamera;
    [SerializeField] private CinemachineCamera gameplayCamera;

    private PlayableDirector director;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();
        director.stopped += OnTimelineFinished;
    }
```

- `using Unity.Cinemachine;` — **Cinemachine 3** (the new package namespace).
- `PlayableDirector` is the Timeline controller. This script sits on the same
  GameObject as the Timeline so `GetComponent` finds it.
- `Awake()` runs when the object is created (before `Start`). Good place to hook
  events.
- `director.stopped += OnTimelineFinished;` — subscribe to an **event**. When the
  timeline stops playing, Unity calls `OnTimelineFinished`. The `+=` syntax is how
  you "sign up" for an event in C#.

```csharp
    void OnTimelineFinished(PlayableDirector director)
    {
        introCamera.Priority = 10;
        gameplayCamera.Priority = 20;
    }
}
```

- When the intro cutscene ends, both cameras get priorities.
- Cinemachine rule: **the camera with the highest priority wins.** Since 20 > 10,
  the gameplay camera takes over after the intro. That's the whole mechanism —
  Cinemachine cameras never "turn off"; you just raise the priority of the one
  you want.
- The `director` parameter here is the same one you already cached — you could
  ignore it. (The parameter exists because the event's signature demands it.)
- **No unsubscribe** — you subscribe in `Awake` but never `-=` in `OnDestroy`.
  In a single-scene game this barely matters, but it is a memory-leak habit.
- Setting `introCamera.Priority = 10` is actually unnecessary — once gameplay is
  20 and intro stays where it was (say 0 or 10), gameplay always wins anyway.
  Setting it to 10 does nothing useful unless something later raises intro above 20.

---

### LevelData.cs

```csharp
using UnityEngine;

[System.Serializable]
public class LevelData
{
    public GameObject[] spawnables;
}
```

- A tiny **data class**. `[System.Serializable]` makes it visible in the Inspector
  when used as a field, so you can fill the array in the editor without dragging
  scripts.
- `spawnables` — the list of prefabs this level is allowed to spawn.
- That's it. No methods, no other data. It is the seed of a level system: the
  natural next step is to add per-level values like "max food", "spawn interval",
  "spawn radius", or a timer.
- Note it uses `public` fields (fine for a pure data holder; for a beginner this
  is acceptable, though `[SerializeField] private` is the more defensive style).

---

## 3. How the scripts connect (data flow)

Here's the whole game as one story:

1. **Timeline plays the intro.** When it stops, `CameraSwitcher` raises the
   gameplay camera's priority → the gameplay camera becomes active.
2. **PlayerController** reads input each frame, rolls the ball with `AddForce`,
   caps its speed, and spins the model.
3. **SpawnManager** (via `InvokeRepeating`) spawns food every 0.1s near the
   player, up to 20 alive.
4. Each **FoodController** watches its own distance to the player and self-
   destructs beyond 15 units (telling the SpawnManager to free a slot).
5. When the player **touches** food (`OnTriggerEnter`), the food is destroyed and
   again the SpawnManager is told the count dropped.
6. `ReplacePlayer` swaps the visible character prefab (wired externally).

So the **SpawnManager's counter is the single source of truth** for "how many
foods are alive," and two different scripts are responsible for keeping it correct.

---

## 4. What you did well

- **Small, readable scripts.** Each file has one clear job. That is genuinely good
  architecture instinct.
- **Modern input.** You used the new Input System, not the ancient
  `Input.GetKeyDown`. Good call.
- **Physics in `FixedUpdate`.** A very common beginner mistake is moving a
  Rigidbody in `Update()`. You did it right.
- **Caching references in `Start()`** instead of calling `GetComponent` every
  frame.
- **`nameof(SpawnObject)`** instead of the magic string `"SpawnObject"` — this is
  an advanced, safe habit.
- **`[SerializeField]` over `public`** for most fields — you're exposing tunables
  without breaking encapsulation.
- **`[RequireComponent(typeof(Rigidbody))]`** — you're protecting your own script
  from a missing dependency.
- **Saving `localPosition` before `Destroy`** in `ReplacePlayer` — correct ordering.
- **Clear variable names.** `maxSpawnDistance`, `currentSpawnedObjects`, etc.
  are self-documenting.

---

## 5. Questions about your decisions — please answer

Answer these in chat (or just think about them) — they tell me *why* the code
looks the way it does, and the answers shape what "better" means for you.

- **Q1.** Why is horizontal input negated? `horizontalInput = -movement.x;`
  Pressing D spins the ball *backwards*? Is this to compensate for the -90°
  camera pitch, or a deliberate "rolling opposite" feel? If it's to fix a camera
  issue, it may be masking a camera setup problem instead of being intentional.
- **Q2.** Why `AddForce` + a manual speed clamp instead of just setting
  `linearVelocity` directly, or using `MovePosition`? Did you want the ball to
  feel "pushy/heavy" (arcade momentum), or was it the first method you found?
- **Q3.** The ball only **adds** force and never slows down (no drag, no
  deceleration when you release the key). Is the "coasts forever" behavior
  intentional?
- **Q4.** Why `Quaternion.Euler(-90, 0, currentZRotation)`? Is the -90° X-pitch
  to flatten the model against the camera? Did you confirm the spin axis looks
  right, or was it trial-and-error? (Also: why start at `180°`?)
- **Q5.** The spin direction only depends on **left/right** input. Should moving
  up/down also spin it? A real rolling ball would.
- **Q6.** Why `speed = 0.2f` and `maxSpeed = 0.5f`? These are tiny numbers — is
  the world scale very small (units ≈ centimeters), or are these leftover from
  tuning? Movement will feel slow unless everything is tiny.
- **Q7.** Why a manual alive-counter instead of using `FindObjectsOfType` /
  tracking children / a pool? Did you know about **object pooling**? (Great
  candidate for it, see section 6.)
- **Q8.** Why `GameObject.Find("Player")` and `GameObject.Find("SpawnManager")`
  in three places instead of dragging references in the Inspector or using a
  singleton? Did you know `Find` is slow and silently breaks if the name changes?
- **Q9.** `InvokeRepeating` — did you pick it deliberately over a **coroutine**
  (`while(true) { yield return WaitForSeconds(...); }`)? Both work; coroutines
  are easier to pause/stop and are more common in modern code.
- **Q10.** `LevelData` only holds an array. Do you plan to add per-level spawn
  rates, max counts, timers? The `currentLevel` field is set to 1 and never
  changes — is level progression just not built yet?
- **Q11.** Both the player *and* the FoodController can call
  `ObjectDestroyed()` for the same food in one frame (eating vs. range-check
  racing). Did you know this can double-decrement your counter?
- **Q12.** `ReplacePlayer` is public but nothing in code calls it. Is it wired
  to the **1**/**2** keys, a UI button, or an animation event in the scene? If it
  should respond to the `Previous`/`Next` input actions (keys 1/2, dpad), note
  those actions are **currently never read** by any script.
- **Q13.** The `Debug.Log("Destroyed")` in FoodController — debug spam left in,
  or do you want logging there permanently?
- **Q14.** In `CameraSwitcher`, why also set `introCamera.Priority = 10`? Since
  gameplay is 20 and intro would be its default, it's redundant — unless you
  planned to re-trigger the intro (restart?).

---

## 6. What could be better (no changes made)

All of these are *suggestions*. Nothing was changed. They're roughly ordered from
"will save you the most pain" to "nice-to-have."

### 6.1 Stop using `GameObject.Find` (the biggest one)

Used in `PlayerController.Start`, `SpawnManager.Start`, and `FoodController.Start`.
Problems:
- It searches the whole scene **every call** — slow if called often.
- It **silently returns `null`** if the name changes or the object is inactive →
  you get a NullReferenceException far from the cause.
- You can't rename objects without breaking code.

Better options:
1. **Serialized references** — `[SerializeField] private SpawnManager spawnManager;`
   and drag it in the Inspector. Best for references that exist in the scene.
2. **`FindFirstObjectByType<SpawnManager>()`** (Unity 6) — finds by component type
   instead of name. Cleaner than `Find`.
3. **Singleton pattern** for managers:
   ```csharp
   public class SpawnManager : MonoBehaviour
   {
       public static SpawnManager Instance;
       void Awake() => Instance = this;
   }
   ```
   then any script can use `SpawnManager.Instance`. Simple and effective for
   game managers, even if purists prefer the Inspector.
4. For the *food* specifically, the SpawnManager could pass itself (or the player
   transform) to the food in `SpawnObject()` — no searching at all.

### 6.2 Use `sqrMagnitude` instead of `Vector3.Distance`

In `FoodController.Update`, `Vector3.Distance` runs a square root every frame for
every food. Comparing squared distances gives the identical answer and skips the
sqrt:

```csharp
float sqrMax = maxSpawnDistance * maxSpawnDistance;
if ((playerTransform.position - transform.position).sqrMagnitude > sqrMax) { ... }
```

### 6.3 Move the range check out of every food's `Update`

20 foods × 1 distance check each frame. Instead, the **SpawnManager** could check
all spawns once per second (or the food could check far less often), or the
distance could be checked only when the player moves. Fewer objects running
`Update()` = better performance as you add more foods.

### 6.4 Consider object pooling

You constantly `Instantiate` (expensive) and `Destroy` food. The classic beginner
upgrade is an **object pool**:

- Pre-create N foods at Start, keep them inactive.
- "Spawning" = `SetActive(true)` and position it. "Eating" = `SetActive(false)`.
- No allocations, no GC spikes, instant reuse.

This also lets the pool own the alive-count, removing the manual counter and the
double-decrement race.

### 6.5 Fix the double-decrement race

If you keep the manual counter, make `ObjectDestroyed()` safe:

```csharp
public void ObjectDestroyed()
{
    currentSpawnedObjects = Mathf.Max(0, currentSpawnedObjects - 1);
}
```

Or — cleaner — let **one** system own destruction (e.g., only the food destroys
itself and reports, or only the player's trigger does).

### 6.6 Guard the level index and player list index

- `levels[currentLevel - 1]` crashes if `currentLevel` is 0 or > length.
  Clamp it: `int levelIndex = Mathf.Clamp(currentLevel - 1, 0, levels.Length - 1);`
- `ReplacePlayer(int index)` crashes if `index` is out of range. Add a bounds
  check and early return.

### 6.7 Clean up

- Remove the redundant `obj.transform.localScale = new Vector3(1, 1, 1);` (default
  is already 1,1,1 — and if the prefab's scale differs, this *overwrites* your
  prefab setting, which is probably not what you want).
- Remove `Debug.Log("Destroyed")` (or wrap it in a debug flag).
- Remove the unnecessary `introCamera.Priority = 10` line (or comment why it's
  there).

### 6.8 Unsubscribe events

```csharp
void Awake() => director.stopped += OnTimelineFinished;
void OnDestroy() => director.stopped -= OnTimelineFinished;
```

Harmless in a one-scene game, but a good lifelong habit. Leaked event
subscriptions keep objects alive after "destroy".

### 6.9 Physics tuning options

- If you want the ball to coast, add small **`Rigidbody.drag`** instead of leaving
  it at 0 (or clamp-to-max is fine as an arcade feel — just know it's a hack).
- Consider `MovePosition`/`velocity` assignment if the "heavy" pushy feel is
  unwanted.
- If the spin should reflect actual rolling, derive it from distance travelled:
  `angle += (speed / radius) * Time.deltaTime` — that's the real rolling-ball math.

### 6.10 Use the Input System more fully

- Generate the C# wrapper class for `InputSystem_Actions` (check "Generate C# Class"
  in the .inputactions inspector). Then you get a typed `InputSystem_Actions`
  class and compile-time safety on action/map names.
- Consider using `movementAction.started/performed/canceled` **events** or a
  `PlayerInput` component instead of polling `ReadValue` in `Update`.
- If `ReplacePlayer` is meant to use the `Previous`/`Next` actions, wire them up —
  right now those actions exist in the asset but no script reads them.

### 6.11 Keep the level system honest

Either finish it (add per-level spawn data, a way to advance `currentLevel`) or
simplify it (a single `spawnables` array) until you're ready. Half-built systems
confuse future-you.

---

## 7. Key Unity concepts (the mental models to remember)

These are the ideas behind the code — learn these and you can rewrite all five
scripts from memory.

### Lifecycle order
`Awake()` → `OnEnable()` → `Start()` → frames (`Update()`/`FixedUpdate()`) →
`OnDestroy()`.
- `Awake` — the object is created. Register events, cache references. Runs even if
  the object is disabled later.
- `Start` — before the first `Update`. Do things that need to happen once, like
  finding the player.
- `Update` — every rendered frame. Read input, animate visuals.
- `FixedUpdate` — fixed physics rate (~50 Hz). **Any Rigidbody change goes here.**

### Update vs FixedUpdate
Physics steps happen at a fixed 50 Hz regardless of framerate. If you move physics
in `Update`, movement becomes dependent on your monitor's refresh rate. That's the
entire reason for the split you made.

### Time.deltaTime vs Time.fixedDeltaTime
`Time.deltaTime` = seconds since the last rendered frame (~0.016). Used to make
per-frame changes framerate-independent:
```
value += ratePerSecond * Time.deltaTime;
```
`Time.fixedDeltaTime` is the fixed physics step (0.02 by default), used the same
way inside `FixedUpdate`.

### Rigidbody & AddForce
- `AddForce(force)` → acceleration = force / mass (default `ForceMode.Force`).
  Piles up velocity over time → gives momentum.
- `linearVelocity` (Unity 6) / `velocity` (older) → the object's current speed
  vector. Clamping it = a speed cap.
- `[RequireComponent(typeof(Rigidbody))]` guarantees the component exists.

### GetComponent vs GameObject.Find vs direct references
- `GetComponent<T>()` — grab a component **on the same GameObject** (fast).
- `GameObject.Find("name")` — search the scene by name (slow, fragile). Avoid.
- Inspector-dragged `[SerializeField]` references — no runtime search at all.
  Prefer this.

### The Input System
- Actions are defined in a `.inputactions` asset.
- An action must be **enabled** before it returns data: `action.Enable();`.
- `action.ReadValue<Vector2>()` polls the current value.
- Actions can be serialized directly into a MonoBehaviour (`[SerializeField]
  InputAction`), or referenced through a generated C# wrapper class.

### Destroy vs SetActive
- `Destroy(obj)` — schedules removal at end of frame. The object is gone for real.
- `obj.SetActive(false)` — hides it, keeps it alive. Cheaper, reusable (pooling).
- `Instantiate(prefab, position, rotation)` — create a copy; with a `transform`
  parent argument, the copy becomes a child.

### Serialization / [SerializeField]
`[SerializeField] private float speed = 0.2f;` shows the private field in the
Inspector so you can tune it without code. It's the recommended way to expose
settings while keeping fields private.

### Tags
`other.CompareTag("Food")` checks the collider's tag (set in the Inspector on the
prefab). Strings here are exact — a typo in a tag silently never matches. Unity's
tag system is more forgiving than `GameObject.Find`, but tag strings are still
magic strings.

### Cinemachine priorities
Cinemachine cameras don't switch on/off. Every `CinemachineCamera` has a
`Priority`; the highest wins. To switch cameras, change priorities. This is what
`CameraSwitcher` does.

### Timeline / PlayableDirector
A Timeline is played by a `PlayableDirector`. You can subscribe to events like
`stopped` to react when the cutscene finishes. Always pair `+=` with `-=` in
`OnDestroy`.

### Events
C# events (`director.stopped += Handler`) let one object say "this happened" and
any number of others react. The game could use more of these (e.g. an
`OnFoodEaten` event on the SpawnManager) so scripts don't have to know about each
other.

### The magic number warning
Values that come from tuning (0.2 speed, 10/5 spawn radius, 180° start rotation,
-90° pitch) should either be `[SerializeField]` fields or named constants. Hard-
coded numbers inside logic are hard to tune and easy to forget.

---

## 8. Glossary

| Term | Meaning |
|------|---------|
| **MonoBehaviour** | Base class for scripts you attach to GameObjects; gives lifecycle methods (`Start`, `Update`, etc.). |
| **GameObject** | A container in the scene; has a name, a Transform, and components. |
| **Transform** | Position, rotation, scale of an object; also defines parent/child. |
| **Component** | A behavior/data module on a GameObject (Rigidbody, Collider, your scripts). |
| **Prefab** | A template GameObject stored in Assets; `Instantiate` copies it. |
| **Rigidbody** | Physics component — makes an object move under forces, collisions, gravity. |
| **Collider** | Shape used for physics collisions/triggers (box, sphere...). |
| **Trigger** | A collider with "Is Trigger" on — detects overlaps without physical push. |
| **Attribute** | `[X]` metadata: `[SerializeField]`, `[RequireComponent]`, `[System.Serializable]`. |
| **Serialize** | Make a field visible/savable in the Inspector. |
| **Vector3/Vector2** | 3D/2D value with x, y (and z). |
| **Quaternion** | Rotation value. `Quaternion.Euler(x, y, z)` builds one from degrees. |
| **Coroutine** | A method that can pause (`yield`) and resume over frames — an alternative to `InvokeRepeating`. |
| **Singleton** | Pattern where one instance is globally reachable (e.g. `SpawnManager.Instance`). |
| **Object pooling** | Pre-creating and reusing objects instead of `Instantiate`/`Destroy`. |
| **GC spike** | A frame freeze from garbage collection; pooling reduces it. |
| **NullReferenceException** | Crash from using a variable that is `null`. The #1 beginner enemy. |

---

*End of review. To respond to the questions in section 5, just reply in chat
with your answers (e.g. "Q1: ... Q2: ..."). I won't change any code unless you
ask.*
