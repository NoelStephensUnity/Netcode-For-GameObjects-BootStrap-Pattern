# A Netcode For GameObjects BootStrap Usage Pattern (Bootstrap Pattern)
This provides you with an "out of the box" project template to get started with [Netcode for GameObjects v1.0](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects).  
## About The Bootstrap Usage Pattern
With unity there are two different scene loading "modes":
- Single: This is when you use LoadSceneMode.Single mode to load a scene.  
  - If there are any scenes loaded prior to loading a new scene in this mode, they will be unloaded.
  - This is also often referred to as "scene transitioning" (i.e. transition from one single mode loaded scene to the next)
  - This mode is often combined with loading scenes additively (i.e. LoadSceneMOde.Additive)
- Additive: This is when you use LoadSceneMode.Additve mode to load a scene.
  - Loading a scene additively has no impact on any scenes currently loaded.
  - This can be especially useful if you want to "preload" scenes.

The "Bootstrap Usage Pattern" involves having one (1) scene, typically set to index 0 of the Build Settings-->Scene in Build list, that is the only scene loaded in single mode for the entire duration of your runtime instance.  All other scenes are always loaded additively.

## About This Implementation
There are several additional features in this implementation that provides you with a "good starting point" without having to completely start from scratch.  If you are looking for a project that you can clone/download, build, run, and be able to start a network session without having to build your own UI, handle starting up and shutting down the NetworkManager, and to have a more WYSIWYG way of loading scenes...then this might be exactly what you are looking for!

### NOTE ON PROJECT STATE:
As time permits, I will be updating this project with additional documentation, improving the existing code base, and in the "near future" I will be adding additional commonly used/created generic netcode "aware" components to help accelerate you into the world of Netcode for GameObjects!
(please be patient)  
:)

### The UI Flow ("Out-of-the-Box")

![image](https://user-images.githubusercontent.com/73188597/180092900-5ca16d52-76e1-44ee-8484-ab332c9aace9.png)<br/>
When you first build and run (or enter play mode) this project you will be presented with a simple UI that provides you with the option of starting a new (network) session or exiting (i.e. exit playmode or exit the application for builds).

![image](https://user-images.githubusercontent.com/73188597/180092695-af537522-5fcf-4cc0-a7a2-1e9450035694.png)<br/>
When you select "New Session" it provides you with the option to start the new session as a server, host, or client.

![image](https://user-images.githubusercontent.com/73188597/180093146-7d30ee3c-238d-4adc-9b5f-1e0807a82dc4.png)<br/>
If you select "Start Host" it will:
- Start a new network session as a host (which is both a client and server running as a single instance) that will start listening for other clients to join
- Spawns the host player prefab
  - The player prefab is a simple implementation that just automatically moves the local player around in a circle.
    - The player prefab has a NetworkTransform component to synchronize the movement between the client(s) and server-host.

You will also notice two buttons that only appear on the host or server side:
- Hide Client HUD: For any connected clients (other than the host-client), when you click this button on a server or host all of the connected client's HUDs will be hidden.
- Show Client HUD: For any connected clients (other than the host-client), when you click this button on a server or host all of the connected client's HUDs will be visible again.

Both of these buttons have a `NetcodeButton` component attached to the GameObject. The `NetcodeButton` is just an example component of how you can pretty much make "anything" netcode aware.

Additionally, if you look at the top right corner you will see an "X" button.  When clicked, it will trigger a series of events that will "roll back" to the previous menu interface. If you are rolling back to a menu interface (scene) that does not require "server synchronization" (i.e. no network session) from an scene that does require "sever synchronization" (i.e. an established network session), then it will automatically handle the netcode shutdown sequence.  If it is a host or a server, then it disconnects all clients and upon all cients being disconnected it will then shutdown its local `NetworkManager` and transition back to the "non-server synchronized" scene (typically a UI scene). Alternately, you will discover that if you progress forward (i.e. from the Session Menu into a network session) where you are progressing from a scene that does not require server synchronization to one that does, it will automatically handle starting the NetworkManager for you.

## Scenes and SceneEntries
Part of this Bootstrap Pattern has a heavy focus on:
- How scenes are loaded and "associating" other scenes with a "primary scene" being loaded.
- The use of `UnityEvents` to trigger other component methods when a scene is loaded, unloaded, before it is unloaded, etc.
- Whether the scene being loaded requires a network session to be loaded or not (i.e. via `NetworkSceneManager`).
  - For a server or host, this means starting a network session and listening for connecting clients.
  - For a client, this means connecting to an already existing network session<br/>
![image](https://user-images.githubusercontent.com/73188597/180095458-0ccd4511-0179-48ab-a0e1-207a56543798.png)<br/>
In the above screenshot you will notice that almost all of the scenes have a `SceneEntry` (`ScriptableObject`) that is associated with each scene.  You will also notice the BootStrap scene does not.  

### Bootstrap Scene
![image](https://user-images.githubusercontent.com/73188597/180095817-725f06e2-9757-479a-bdbc-f8932b572bc9.png)<br/>
The BootStrap scene is fairly straight forward.  It contains the `NetworkManager` and a "BootStrapSceneLoad" object that defines the resolution (or any other property specific to your project that you might add to it) as well as the first scene to be loaded additively.  The default setting for the template is to load the "DefaultActiveScene" `SceneEntry` `ScriptableObject`.

### Default Active Scene
![image](https://user-images.githubusercontent.com/73188597/180097206-38d61393-e3d5-4c86-8fca-1e3862a37930.png)<br/>
With Unity there can only be one "currently active scene" at a time but you can still have "many scenes" loaded (additively). When a scene is the currently active scene this means that, by default, any time you instantiate a new `GameObject` it will be instantiated in the currently active scene.  Once instantiated you can migrate a `GameObject` into any other scene that is loaded.  So, when you think about the "currently active scene" you should always remember the default instantiation target scene is the currently active scene.  The only caveat to this rule is if the `GameObject` is already defined within the a scene being loaded.  Under this case the `GameObject` will default to the scene it was placed in via the Unity editor.

### SceneEntry and NetcodeSceneLoader
A "SceneEntry" is derived from `ScriptableObject` that provides you with the ability to build logical relationships between other SceneEntries, Buttons, and Scripts. SceneEntries have to be associated with a `NetcodeSceneLoader` component in order for them to function properly. 

- Scene Asset To Load: This is the scene that is associated with the SceneEntry and will be loaded if the SceneEntry is being "loaded".
- Load Scene When: This defines "when the scene is loaded" and there are two ways this is handled.
  - Triggered: The scene will be loaded via the `SceneEntry.SceneLoadTriggered` method.
    - Typically, this is invoked via a button or some other component with an associated UnityEvent that could be "triggered" by a script.
  - Start: As stated before, a SceneEntry must be associatd with a `NetcodeSceneLoader` that typically contains at least one "triggered" SceneEntry and then one or more SceneEntries that load when the `NetcodeSceneLoader` invokes its `NetcodeSceneLoader.Start` method.

To better understand the relationship between `SceneEntry` components and a `NetcodeSceneLoader` component, the below screenshot shows you the DefaultActiveScene's contents:<br/>
![image](https://user-images.githubusercontent.com/73188597/180101496-c14ea870-cdc2-412c-82b5-3a08e0b6ae9a.png)<br/>
If you look at the SceneLoader object inspector view above, you will see that within the SceneEntry list is an actual reference to the DefaultActiveScene's `SceneEntry`, and then there are 4 more `SceneEntry` references in that list.  If you were to look at any one of the other 4 `SceneEntry` assets in the inspector view, you would see that they are all set to "Load (the) Scene When" the `NetcodeSceneLoader.Start` method is invoked. (this property name and associated enum types will most likely be changed when I come up with better names for them). Below is a screenshot of the MainMenu `SceneEntry`:<br/>
![image](https://user-images.githubusercontent.com/73188597/180101853-595bfbe7-d9b3-4144-bc51-f3a9bdeea7b7.png)<br/>

You might feel confused at this point, but the following "logical flow" might help clear things up:
1. The BootStrap scene is loaded which, in turn, loads the DefaultActiveScene.
2. When the DefaultActive scene is loaded, the SceneLoader object is instantiated and the `NetcodeSceneLoader.Start` method is invoked.
3. `NetcodeSceneLoader` parses through its assigned SceneEntries, skips over the DefaultActiveScene (it was already loaded which is the same as being loaded by a "trigger" event), and then it will load all of the SceneEntries configured to load at "Start"(i.e. when the associated `NetcodeSceneLoader.Start` method is invoked).

At this point there is one more concept to understand about a `SceneEntry`, which we will look at the SessionMenu `SceneEntry` for this.  The SessionMenu is the menu interface that provides you with the option to start a server, host, or client.  When a scene is loaded additively, all of the GameObjects instantiated will typically be immediately "visible" unless you have some form of script to disable them during the `Start` method (or the like).  With a `SceneEntry` you have an additional handy method you can invoke to "show or hide" all GameObjects instantiated when the scene is loaded (i.e. in-scene placed NetworkObjects).  This is accomplished via the `SceneEntry.EnableSceneObjects` method.<br/>
![image](https://user-images.githubusercontent.com/73188597/180102994-1c6d26e1-c2a6-426e-a8a2-cc921da2d1e6.png)<br/>
Looking at the inspector view of the SessionMenu `SceneEntry`, we can see that the "On Loaded Trigger" `UnityEvent` has a single entry that will invoke the `SceneEntry.EnableSceneObjects` method and pass a "false" to that method (checkbox un-checked) which will disable all `GameObjects` instantiated when the SessionMenu is loaded.  

The idea behind this is that we are "pre-loading" certain scenes that we know we will use at some point in the "near" future but we don't want anything within the loaded scenes to be visible or to consume any processing cycles until we are "ready".  If you refer back to the MainMenu `SceneEntry` above, you will see that it has no `On Loaded Trigger Events` which means the main menu will be visible by default. Let's walk through the loading process once more with a little more detail:<br/>

- The BootStrap scene is the first scene in the Scenes in Build list within the Build Settings and so it will load by default when a runtime build first starts
  - There is additional code that automatically loads the `BootStrap` scene when you enter into play mode as well
    - When in the editor, it will also automatically progress you to whatever scene you have opened when you entered into play mode.
- The Bootstrap scene then loads the DefaultActiveScene (via `SceneManager`) which is also treated like it was "triggered" to be loaded. (special case here)
  - Upon the DefaultActiveScene being loaded, the SceneLoader proceeds to load the rest of the SceneEntries assigned to it.
    - Unless the `SceneEntry` is set to load when triggered. Under this scenario it won't be loaded, but it still will be "registered/associated" with the `NetcodeSceneLoader` component attached to the SceneLoader `GameObject`.
  - If you click the "New Session" button then the MainMenu scene has all of its `GameObjects` disabled and the SessionMenu has all of its `GameObjects` enabled:<br/>
![image](https://user-images.githubusercontent.com/73188597/180104220-2a85d4cf-73f3-4170-870d-2b290ce1a90f.png)<br/>

Looking at the above screenshot of the `MainMenu` scene's `New Session` button, we can see the button's "On Click" actions list contains two "actions" that will invoke the:
- MainMenu's `SceneEntry.EnableSceneObjects` passing false (i.e. disables them)
- SessionMenu's `SceneEntry.EnableSceneObjects` passing true (i.e. enables them)

And with that...we switched between "scenes" without having to load a scene when the button is clicked or unload a scene if we want to bring another "scene into view".
This is one, of several, benefits that comes with using a Bootstrap usage pattern (and this project template). You can even "pre-design" your scene flows without having to have all content populated within the scenes, and as you add content to scenes it is relatively easy to determine "does this scene need to have a network session (i.e. be synchronized by the server) or not?" and it simplifies the loading and unloading of scenes to the point where you don't even have to write any code to do this!

_(More To Come As Time Permits)_







