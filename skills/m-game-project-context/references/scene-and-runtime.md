# Scene And Runtime

## Scenes

Gameplay scene files under `Assets/Scenes`:

- `Game.unity`
- `Room.unity`
- `forest.unity`
- `BOSS.unity`
- `Main Menu.unity`

Build settings currently enable only:

- `Assets/Scenes/Game.unity`
- `Assets/Scenes/Room.unity`

This matters because runtime code also loads `"Main Menu"` by name. If you are preparing a player build, verify build settings before assuming all string-loaded scenes are included.

## Startup And Scene Flow

Key runtime flows:

- `MainMenu.NewGame()` clears `PlayerPrefs` and calls `SceneController.TransitionToFirstLevel()`
- `SceneController.TransitionToFirstLevel()` loads `"Game"`
- `SceneController.TransitionToLoadGame()` loads the scene name restored by `SaveManager`
- `TransitionPoint` uses trigger presence plus `E` key to call `SceneController.TransitionToDestination(...)`

Player object registration now matters to several systems:

- `GameManager.RigisterPlayer(GameObject)` stores the runtime player as `PlayerUnit`
- `GameManager.PlayerTransform`, `PlayerState`, and `PlayerCharacterData` are compatibility accessors layered on top of `PlayerUnit`
- New UI/runtime code should prefer `GameManager.PlayerUnit` instead of searching for `StateManager` directly

## Manager Lifecycle

These classes call `DontDestroyOnLoad(this)` in `Awake()`:

- `GameManager`
- `SceneController`
- `SaveManager`

`InventoryManager` inherits the same persistence pattern through the shared singleton base and is expected to exist across scene changes as a singleton-backed UI/data owner.

When editing initialization code, check for duplicate singletons after scene reloads and prefab re-entry.

## Save And Load Behavior

Save/load currently behaves like this:

- `SaveManager.Save(...)` serializes an object to JSON and stores it in `PlayerPrefs`
- The current scene name is also saved in `PlayerPrefs`
- `SceneController.Transition(...)` saves player and inventory before scene changes
- On cross-scene transition, `SceneController` loads the scene, instantiates `playerPrefab`, then calls `SaveManager.LoadPlayerData()`
- `ActorController.Start()` also calls `SaveManager.LoadPlayerData()`
- `InventoryManager.Start()` loads inventory/equipment/action data and refreshes UI

Important implications:

- Player stats and inventory are not persisted by custom files or databases
- Runtime state depends on ScriptableObject overwrite order
- Multiple load calls can interact in non-obvious ways during scene initialization
- Save/load is still not fully blackboard-native; some flows still depend on legacy mirrored data for compatibility

## Transition Anchors

Scene transitions rely on `TransitionDestination.DestinationTag` values:

- `ENTER`
- `A`
- `B`
- `C`

`GameManager.GetEntrance()` specifically searches for the `ENTER` destination to place the player after some loads.

## Known Runtime Risks

- `SaveManager.SceneName` reads `PlayerPrefs.GetString(sceneName)` where the backing field starts as an empty string, so the saved scene name effectively uses an empty-key entry.
- `SceneController.LoadLevel(...)` saves player and inventory immediately after instantiating the player in the loaded scene, which can overwrite expectations about initial state.
- `Main Menu` exists as a scene asset but is not in current build settings.
- Scene overrides on prefab instances can still create confusing runtime behavior if `Team`, blackboard identity fields, and initializer values disagree.
