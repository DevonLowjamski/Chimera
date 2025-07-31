## **Construction & Facility Management Design Document v1.1**

## **Date:** May 24, 2025

**Introduction:** This document outlines the core systems, visual design, and player interaction principles for the construction and management of cultivation facilities within Project Chimera. It reflects the latest design decisions, aiming to create an intuitive, engaging, and visually coherent experience that supports the game's detailed simulation goals.

**I. Exterior Environment Design**

This section details the visual appearance and planned evolution of the environment surrounding player-controlled facilities.

**A. MVP Visuals & Mechanics (Initial Launch)**

* **Concept:** The game will feature a basic, realistic-styled exterior environment common to all initial map locations (e.g., Residential House, Warehouse).  
* **Visual Components:**  
  * **Ground Plane:** A simple green color to represent grass or earth.  
  * **Skybox:** A clear blue color for the sky.  
  * **Sun:** A static representation of the sun, acting as the primary directional light source for the exterior scene.  
  * **Day/Night Cycle:** The skybox and sun's position/intensity will visually cycle through day and night, synchronized with the player-selected in-game time acceleration.  
* **Gameplay Impact (MVP):** Purely visual. The exterior environment will have no direct impact on cultivation mechanics, resource availability, or other gameplay systems in the initial launch version.

**B. Future Vision (Post-MVP Regions & Themes)**

* **Dynamic Regions:** Post-launch, the plan is to introduce multiple distinct geographical "regions" from which players can choose at the start of a new game. This choice will be permanent for that playthrough.  
  * **Unique Characteristics:** Each region will feature:  
    * Unique visual themes reflecting its climate and geography (e.g., temperate forest, arid desert, tropical zone).  
    * Distinct natural environmental conditions (e.g., average ambient temperatures, humidity levels, natural sunlight intensity and duration, prevailing weather patterns). These conditions will be designed to influence gameplay, especially with the future introduction of outdoor cultivation.  
    * Balancing Factors: Regions with more favorable natural climates for cultivation may have gameplay balancing factors such as higher costs for land expansion or (if implemented) employee recruitment/wages.  
* **Novel Environment Themes (Potential DLC):** Further expansions, possibly as paid DLC, could introduce fantasy or novel themed environments (e.g., cultivation on the moon, in an underwater facility, within a vast cave system).

**C. Camera Limitations**

* A hard limit will be imposed on how far the player can zoom out from or move the camera away from their primary facility structure (e.g., house, warehouse).  
* The intent is to provide an additional visual perspective and a thematic backdrop, not to enable extensive exploration of the external landscape. This can also help thematically link the progression between different facility types (e.g., the house and warehouse could be perceived as being on the same larger, unseen property).

**II. Utility View & "X-Ray" Mode**

This system is crucial for the construction and management of complex utility networks.

**A. Scope and Availability**

* The "X-Ray" Utility View is **not** available in the initial "Residential House" map/level.  
* It is a feature unlocked concurrently with the player gaining access to the "Warehouse" map/level. This unlock can be tied to the player progression system (e.g., a specific objective completion, skill tree node) or occur automatically upon first entering the warehouse environment.  
* **Purpose:** The Utility View is essential for the from-scratch construction, detailed layout, and troubleshooting of interconnected utility networks (plumbing, electrical, HVAC) required in the more complex warehouse environment.

**B. Visual Style of "X-Ray" Mode**

* **1\. Structural Elements (Walls, Floors, Roofs):**  
  * When toggled on, these elements will adopt a "ghostly translucency." They will remain visually present and identifiable, retaining their base colors and textures, but will become sufficiently transparent to allow clear visibility of utility components routed behind or within them. They will not revert to wireframes or simple outlines.  
* **2\. Utility Highlighting:**  
  * **Color-Coding:** Each utility type will have a distinct, easily recognizable color (e.g., blue for water pipes, yellow for electrical wires/conduits, silver/grey for HVAC ducting) for immediate visual identification.  
  * **Selection/Active State:** Utility components currently selected by the player, or those part of an actively functioning system, will feature an emissive glow or a more boldened outline to emphasize their status.  
* **3\. Planned vs. Built Components:**  
  * **Proposed Placements:** When planning utility routes (before finalizing placement), components may appear more transparent, desaturated, or "ethereal."  
  * **Confirmed Placements:** Once built, utility components will appear solid (though still viewable through translucent structures in X-Ray mode if applicable).

**C. Information Display & Interaction**

* **Filtering:** Players will be able to toggle filters within the Utility View to display only specific utility types (e.g., "Show Electrical Only," "Show Plumbing Network," "Isolate HVAC"). This is critical for managing visual complexity in dense networks.  
* **Contextual Information:** Hovering the cursor over significant utility components (e.g., pumps, filters, main breakers, large duct sections, equipment connection points) will display a tooltip with key data (e.g., "Pump XYZ \- Capacity: 500 GPH," "Circuit Breaker \- Load: 75/100 Amps," "Pipe Segment \- Material: PVC, Diameter: 1 inch"). This will be implemented intuitively to avoid overwhelming the player with popups on every minor segment.

**D. Problem Highlighting**

* This is a critical diagnostic feature. Each utility system will have unique visual cues to indicate operational problems within the Utility View:  
  * **Electrical:** Red flashing or a specific icon for overloaded circuits or faulty connections.  
  * **Plumbing:** A dark, static section or a distinct icon for blocked pipes; a "leaking" visual effect (if feasible) for damaged sections; sputtering or no flow animation for low pressure or unconnected ends.  
  * **HVAC:** Visual indicators for blocked vents or ductwork, or areas not receiving adequate airflow.  
  * Successful connections and normal operational flow will also have clear positive visual feedback (e.g., steady flow animations, green status indicators on key components).

**E. View Transition**

* Toggling the Utility View on or off (via a dedicated hotkey or UI button) will result in a near-instantaneous visual switch, ensuring a responsive user experience.

**F. Interactivity & Purpose**

* The Utility View will be the **primary and sole interface** for players to construct, place, connect, rotate, and edit their utility networks.  
* Its enhanced information display (through highlighting, tooltips, and problem indicators) will also make it the main view for players to diagnose overarching systemic issues, understand flow dynamics, and optimize the layout and function of their utility infrastructure.

**III. Utility Flow Animations**

Simple, clear animations will provide immediate visual feedback on the operational status of utility systems.

**A. General Principles**

* Animations will be present when a utility system or segment is actively functioning.  
* They will be visually distinct for different utility types.  
* Their primary visibility may be enhanced or exclusive to the Utility View, especially for networks hidden within structures.  
* They serve as intuitive indicators of connectivity, flow direction, and basic operational status, not as precise physics simulations.

**B. Specific Utility Visuals**

* **1\. Water/Nutrient Pipes:**

  * **Flow Indication:** Moving pulses, directional chevrons, or dashes traveling along the pipe.  
  * **Color:** The animation (or the pipe's fill content) can adopt the color of the mixed nutrient solution from the reservoir, or a neutral blue for plain water.  
* **2\. HVAC Ducting:**

  * **Airflow:** Subtle, semi-transparent animated lines, particles, or "wisps" moving in the direction of airflow.  
  * **Temperature/CO2 (Subtle):** The color of the wisps could subtly tint (e.g., faint blue for cooled air, faint red for heated air) or incorporate a faint shimmer for CO2 distribution.  
* **3\. Electrical Wiring/Conduits:**

  * **Power Flow:** Small, bright pulses or stylized "energy sparks" moving along the wire from the power source to connected devices.  
  * **Load Indication (Subtle):** The frequency, intensity, or color of pulses could subtly change to indicate higher load, potentially becoming more agitated or shifting color (e.g., towards orange/red) if nearing or exceeding circuit capacity.

**C. Animation Behavior**

* **Active vs. Inactive:** Animations are present when the utility is intended to be active (e.g., pump is on, light switch is engaged). No animation or a static "idle" state if the system is off.  
* **Interruption:** Animations will visually stop at closed valves, unconnected ends, or identified blockages, clearly indicating the point of interruption.

**IV. Clutter Management & Facility Aesthetics**

Maintaining a clean and organized visual appearance is a key design goal.

**A. Core Aesthetic Principle**

* The game will strive for a clean, pristine, and professional visual aesthetic for facilities and equipment, avoiding excessive grime, rust, dirt, or spills.

**B. Design Strategies for Cleanliness**

* **1\. Spatial Design & Verticality:**  
  * Map layouts, including the Residential House rooms, will be designed to provide adequate space for initial setups without feeling overly cramped.  
  * The game will provide and encourage the use of vertical space through assets like wall-mounted equipment, stackable shelving, and multi-tiered benches.  
* **2\. Asset Design & Placement Logic:**  
  * All equipment assets will be designed with clean textures and clear, logical footprints.  
  * The defined grid system and robust snapping logic will facilitate orderly and aligned placement of equipment and utilities.  
* **3\. Storage Solutions:**  
  * Placeable storage assets (e.g., lockable cabinets, shelving units, utility carts) will be available. These can serve as functional inventory access points, allowing players to visually "store" unused tools or small consumables, keeping active workspaces tidy.

**C. Waste Management**

* **1\. MVP Mechanics:**  
  * Players will need to manage generated waste (e.g., pruned leaves, used growing mediums).  
  * This will involve manually transporting waste to placeable disposal assets such as dumpsters or trash cans.  
  * These waste containers could be equipment items that are potentially upgradeable (e.g., for larger capacity, different visual styles, or minor efficiency benefits).  
* **2\. Future Vision:**  
  * Explore mechanics for processing or re-using certain types of waste to create beneficial byproducts (e.g., composting plant matter into soil amendments) or to reduce disposal costs/earn revenue.

**V. Construction System**

This system defines how players build and modify their cultivation spaces and install infrastructure.

**A. Grid System Mechanics**

* **Base Unit:** The fundamental grid unit for all construction, placement, and scaling will be **one (1) foot**.  
* **Snapping:**  
  * A "Snap to Grid" toggle will be available for players.  
  * When active, snapping will occur to grid lines, grid intersections, and mid-points of grid cells.  
  * Snapping will also apply to relevant points on existing objects (e.g., edges, centers, pre-defined connection ports for utilities).

**B. Structural Asset Placement (Path-Based)**

* For elements like walls, players will primarily use a **path-based system**, especially in advanced (e.g., warehouse) scenarios.  
* **Process:**  
  1. Player selects a specific structural asset type (e.g., "Standard Drywall Section," "Insulated Industrial Panel"). This asset inherently defines properties like height and thickness.  
  2. Player clicks a start point on the grid.  
  3. Player drags the cursor to draw the desired path/length of the wall. The game will provide real-time visual feedback on the proposed placement, including its cost and constructability (e.g., highlighting collisions).  
  4. Player clicks to set subsequent points (for corners/segments) or to finalize the wall section.  
* This system allows for the creation of walls of varying lengths using standardized, asset-defined components.

**C. Dimensional Control & Asset-Based Properties**

* The height and thickness of structural elements like walls are primarily determined by the specific asset type selected by the player from the build menu.  
* To build a wall of a different height or thickness, the player must select a different wall asset (e.g., "8ft Drywall Section" vs. "10ft Insulated Panel"). These different assets may need to be researched or purchased.  
* This philosophy of asset-defined core properties applies to most construction elements and utilities (e.g., pipe diameters are set by the chosen pipe asset).

**D. Material Application Philosophy**

* The "material" of a structural element (e.g., drywall, concrete, metal paneling) is inherent to the specific asset chosen by the player for construction.  
* When a player selects an asset like "Concrete Wall Section," they are choosing both the form and the material simultaneously. The game will not feature a separate "paint" or "apply material" tool to change the fundamental material of an already placed structural asset in the MVP; material choice is made at the point of selecting the construction asset. (Cosmetic color variations for categories of items could be a simpler, separate system if desired later).

**E. Utility Placement (General)**

* **No Auto-Routing (MVP):** Automatic routing of utilities (e.g., the game suggesting paths or auto-adding elbows) will be deferred to post-MVP versions. Players will manually route all utilities.  
* **Segmented Components for Curves/Bends:** There will be no truly "free-dragging flexible" segments for utilities like hoses or flex duct. Instead, curves and bends will be achieved by using a selection of pre-designed straight, curved, and angled rigid sections that conform to the grid and connection logic.  
* **Vertical Runs:** Intuitive tools or UI affordances will be provided to facilitate routing utilities vertically (e.g., up/down walls, through floor/ceiling penetrations).

**F. Construction UI & Validation**

* **UI:** A clear, categorized build menu will allow easy selection of structural elements, utilities, and equipment. Previews of objects before placement, along with their resource costs, will be displayed.  
* **Validation & Feedback:** The system will provide immediate visual feedback for valid/invalid placements (e.g., highlighting objects red for collisions or inability to place due to unmet prerequisites). Clear indicators for connection compatibility between utility components will be provided.

**VI. Abstracted Microclimate Modeling**

This system simulates localized environmental variations within a larger controlled space.

**A. Core Principle**

* The system will simulate the *impact* of equipment, plant density, and room geometry on the plant environment, providing strategic levers for player optimization rather than achieving perfect physical fidelity.

**B. Zone Definition (Sub-Zones)**

* The primary method for defining localized climates will be through **sub-zones**.  
* Equipment such as fans, AC units, heaters, and even heat-producing lights will project a "radius of effect" or a directional "cone of influence" where their environmental impact (e.g., temperature change, airflow increase) is strongest, diminishing with distance or if obstructed.  
* Enclosed rooms will still form the larger boundary for overall environmental calculations.

**C. Key Influencing Factors**

* (As detailed in environmental simulation documents) Factors include: heat output from equipment (lights, pumps), airflow from fans/HVAC, plant density and transpiration (affecting local humidity and CO2), obstructions (walls, large equipment), and basic thermal properties of room construction.

**D. Update Frequency**

* The environmental parameters within these sub-zones will be recalculated periodically (e.g., every few in-game minutes, with the real-time interval depending on the active game speed and performance considerations). This ensures the simulation responds to changes in equipment status or facility layout.

**E. Visual Cues**

* **Heat Map Overlay:** A toggleable heat map overlay (potentially part of an advanced environmental data view or an upgrade) is a desired feature to visually represent temperature variations within a facility.  
* **Other Cues:** If feasible and clear, other subtle visual cues for microclimates (e.g., faint heat haze near hot equipment, slight condensation on surfaces in overly humid spots, airflow direction indicators) will be considered to enhance player understanding.