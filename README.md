## About Architect
Architect is a plugin for the Reclaimer tool. It adds a visual 3d scenario editor where you can move objects around, change their properties and switch them to different kinds of objects. It allows you to edit map objects such as vehicles, weapons, machines, bipeds, scenery crates etc as well as mission details like trigger volumes, spawn locations and starting profiles. It also adds a DirectX based model/bsp viewer that has far better performance than Reclaimer's standard one.

## Compatibility
Architect currently supports editing map files from Halo 3 and up on both Xbox 360 and MCC versions of the game. Beta versions of the games are not supported.
Architect can open the following tag types:
- **render_model, scenario_structure_bsp**
  - These will be opened in a model viewer similar to the one built in to Reclaimer, however the Architect one uses DirectX and is much more performant.
- **model, vehicle, weapon, equipment, biped, scenery, crate, machine, control**
  - These tags will be opened in a model viewer that has the ability to preview model variants including any attachments the model has. These models cannot be extracted.
- **scenario**
  - This tag will open the Architect scenario editor. This gives a 3d view of the entire map including all scenery and other objects. The editor is similar to the Sapien tool from Halo Custom Edition where you can view and edit details of what objects are on the map.
  - **IMPORTANT**: it is highly recommended to back up your map file before editing the scenario

## Controls
- Click and hold `R mouse button` anywhere in the viewport to take control of the camera
- While controlling the camera the use following controls to move around
  - Move the mouse to rotate the camera
  - Press and hold `WASD` for movement
  - Use `R` and `F` to move the camera up and down
  - While moving hold `Shift` to move faster and `Ctrl` to move slower
- Use `mouse scroll` to change the camera speed
- Viewport FOV can be changed in settings
  - Does not affect any viewports that are already open

## Installation
Put the **Architect** folder into the **Plugins** folder of your Reclaimer installation. You can find this in Reclaimer by going to `View > Application Directory` from the menu.

If the tag types mentioned above do not automatically open using Architect when you double-click them, you may need to set Architect as the default viewer by right-clicking on the tag, select `Open With` then select **Architect Viewer/Editor**.

If you do not see **Architect Viewer/Editor** option when using the `Open With` menu, make sure you have the newest version of both Architect and Reclaimer itself. This may also mean Architect has been blocked by Windows.

Use the following steps to unblock it:
- Delete the **Architect** folder in **Plugins**
- Right-click the downloaded Architect zip file, select Properties, then click `Unblock`
- Re-extract the zip file and place the extracted folder into the **Plugins** folder
- If the above doesn't work you may need to unblock each file individually after extracting them
