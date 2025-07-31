## **Cultivation Systems & Automation Design v1.1**

**Document Date:** May 24, 2025

**I. Introduction**

This document outlines the specific design and mechanics for Project Chimera's cultivation systems and the progressive automation thereof. The core philosophies guiding these systems are a commitment to realistic simulation, the critical role of Genotype x Environment (GxE) interaction, robust player agency in experimentation and decision-making, and a rewarding progression from manual tasks to sophisticated "Earned Automation". The aim is to create an engaging cycle where detailed management and optimization lead to tangible improvements in cultivation and facility efficiency, all at a player-controlled pace.

**II. Plant-Specific Environmental "Recipes" & Profiling**

The dynamic interplay between a plant's genetic makeup and its environment is central to Project Chimera. This section details how players will discover and utilize unique environmental needs for their plants.

**A. Defining Environmental "Recipes"**

* **Concept:** Each distinct cannabis strain, and potentially notable phenotypes discovered by the player, will possess a unique optimal "recipe." This recipe consists of ideal ranges for environmental parameters such as temperature, humidity, Vapor Pressure Deficit (VPD), light spectrum/intensity (PPFD), CO2 levels, and specific nutrient parameters (e.g., EC targets, NPK ratios) tailored for each growth stage.  
* **Strain Variation & Genetic Influence:**  
  * **Landrace Strains:** These foundational strains may exhibit wider optimal environmental ranges, signifying greater resilience and adaptability, though perhaps with a lower peak genetic potential if not finely tuned. Their "recipe" could be more forgiving for novice players.  
  * **Hybrids & Inbred Lines (IBLs):** As players engage in breeding and stabilization, the resulting strains might develop narrower and more specific optimal environmental windows. Achieving these precise conditions could be key to unlocking their highest genetic potential, impacting quality, yield, and cannabinoid/terpene profiles.  
  * **"Environmental Tolerance" Trait:** A plant's inherent "Environmental Tolerance" genetic trait will directly influence the breadth of these optimal ranges. High tolerance translates to a wider, more forgiving recipe, while low tolerance demands meticulous environmental control.  
* **Growth Stage Dynamics:** The environmental "recipe" is not static; it must adapt throughout the plant's lifecycle:  
  * **Seedling/Clone:** Typically require higher humidity and lower light intensity.  
  * **Vegetative:** Benefit from moderate humidity, strong blue-spectrum light, and higher nitrogen availability.  
  * **Flowering (Early to Late):** Demand lower humidity (especially late flower to prevent mold), intense red-spectrum light, higher phosphorus and potassium, and specific VPD targets to drive optimal transpiration and bud development.  
* **Data Representation:** Discovered "recipes" could be stored and accessed by the player through:  
  * An in-game "Strain Database" or "Grower's Journal" UI, specific to each cataloged strain/phenotype.  
  * Visual representations, such as ideal ranges highlighted on graphs within the Plant Detail UI when a specific strain is selected.

**B. "Environmental Profiling" Mechanic**

This mechanic empowers players to discover the unique "recipes" through active experimentation, data analysis, and learning.

* **Discovery Process:**  
  * **Initial State:** When a player acquires a new or unprofiled strain (especially from breeding efforts or rare acquisitions), its optimal environmental "recipe" will be largely unknown, presented as very broad estimates or entirely hidden.  
  * **Experimentation:** Players will need to consciously design experiments, setting up grow rooms or individual plant environments with specific, controlled parameters. This could involve A/B testing on clones of the same mother plant under slightly varied conditions.  
  * **Data Collection:** Success hinges on meticulous use of data collection tools:  
    * Environmental Data: Real-time sensor readings (temperature, humidity, CO2, PAR/PPFD).  
    * Growing Medium Data: Manual sampling for EC/PPM, pH, temperature, and Volumetric Water Content (VWC).  
    * Plant Responses: Close observation of growth rates, visual health indicators (leaf color, turgidity, stress signals), nutrient uptake rates (e.g., EC drop in hydroponic reservoirs), and crucial post-harvest data including yield, and simulated lab analysis of cannabinoid/terpene profiles.  
  * **Feedback Loop:** The game must provide clear, consistent feedback. If conditions deviate from the (hidden) optimum, plants should exhibit graduated negative responses (e.g., subtle stress, reduced growth, lower quality/yield). As players adjust conditions closer to the ideal, they should observe correspondingly improved outcomes.  
* **"Science" Skill Tree Integration:**  
  * Progression through the "Science" skill tree is vital for effective environmental profiling. Nodes such as "Observation & Record Keeping", "Data Interpretation & Diagnostics", and "Advanced Analytics & Research Methodology" will directly enhance the player's ability to design experiments, collect accurate data, and interpret results.  
  * Higher "Science" skills could unlock advanced UI tools that help visualize optimal environmental ranges as more data points are gathered (e.g., a dynamic graph that shades "optimal" zones based on logged experimental outcomes).  
* **UI for Profiling:**  
  * A dedicated "Research Log" or an advanced tab within the Plant Detail UI could allow players to formally record experimental parameters and observed outcomes for specific strains.  
  * The interface should facilitate side-by-side comparison of different grow cycles or experimental batches for the same strain.  
  * Once a player is confident they have "profiled" a strain, they should be able to "save" this optimal recipe. This saved recipe could then be used as a target for advanced automation systems or as a reference for AI assistants (if such a feature is pursued later).

**III. Automation Systems Design**

In line with Project Chimera's "Earned Automation" philosophy, players will progressively gain access to tools and systems that alleviate the "Burden of Consistency" inherent in manual cultivation, especially at scale or under accelerated time.

**A. Sensors**

Sensors are the eyes and ears of any automated system, providing the raw data necessary for informed decisions and control.

* **Types & Tiers:** A tiered approach to sensor availability and capability:  
  * **Basic Sensors:** Simple, standalone digital units providing readouts that require manual checking by the player (e.g., basic thermometer/hygrometer).  
  * **Intermediate Sensors:** Networked sensors capable of transmitting data to a central display panel or directly to simple controllers. These allow for more consistent monitoring.  
  * **Advanced Sensors:** Highly accurate, potentially multi-functional units (e.g., a single device measuring temperature, RH, VPD, and CO2). This tier would also include more specialized sensors such as:  
    * Leaf surface temperature sensors.  
    * Continuously-logging soil/medium moisture probes.  
    * Inline water sensors for pH/EC/temperature in irrigation systems.  
    * *Simulated* air particle/spore trap sensors for early warnings of potential airborne contaminants (a very advanced "Science" unlock).  
* **Functionality:** Provide real-time and logged data crucial for player understanding, decision-making, and the operation of automated controllers.  
* **Placement Mechanics:**  
  * Generally grid-based placement on walls, stands, or directly within the cultivation environment (e.g., soil probes).  
  * Ambient sensors (e.g., for air temperature, RH, CO2) should have a defined "radius of accuracy" or "zone of influence," encouraging players to use multiple sensors in larger or complex rooms to accurately map microclimates. This ties into the Abstracted Microclimate Modeling.  
  * Sensor placement can itself be a minor skill element; suboptimal placement (e.g., a temperature sensor too close to a hot light) might provide skewed or less useful readings.  
* **Progression & Unlocks:** The availability, accuracy, and types of sensors will be tied to player progression, primarily through the "Science" skill tree (for data accuracy, new sensing capabilities like VPD) and the "Environment" skill tree (for tools related to climate monitoring).

**B. Controllers**

Controllers are the brains of automation, interpreting sensor data and actuating equipment according to player-defined logic.

* **Progression & Types:**  
  * **Simple Timers:** Basic on/off scheduling, primarily for lighting systems.  
  * **Basic Controllers (Thermostats, Humidistats):** Simple on/off control based on a single sensor input and a single setpoint (e.g., turn on AC if temperature exceeds X). These are unlocked via the "Environmental Automation (Sensors & Controllers \- Basic)" node in the "Environment" skill tree.  
  * **Intermediate Controllers (Abstracted Programmable Logic Controllers \- PLCs):** Capable of handling multiple sensor inputs and controlling multiple pieces of equipment with simple conditional logic (e.g., IF temperature \> X AND lights \= ON, THEN increase exhaust fan speed to Y%). These would be part of the "Precision Climate Management" node.  
  * **Advanced Controllers (Central Computer System / Advanced PLCs):** Represents the pinnacle of environmental control. These systems can manage multiple distinct zones, integrate with a wide array of sensors and equipment, execute complex multi-step schedules derived from player-defined "recipes," and potentially offer advanced data logging and analysis features. This would be a very late-game unlock, likely tied to "Precision Climate Management" and potentially requiring high-tier "Construction" skills for the necessary infrastructure.  
* **Logic & Linking User Interface (UI):**  
  * An intuitive UI is essential for players to configure automation. This might involve a dedicated "Automation" screen or an enhanced "Utility View".  
  * Players should be able to visually link specific sensors to controllers, and controllers to pieces of equipment (e.g., via a drag-and-drop interface or logical node graph).  
  * The UI must allow players to define setpoints, operational ranges, and conditional logic (e.g., "IF-THEN-ELSE" statements, time-based conditions).

**C. Scheduled Events (Beyond Basic Light & Fertigation Cycles)**

Advanced controllers will enable players to automate more nuanced aspects of the cultivation process.

* **Preventative Integrated Pest Management (IPM):**  
  * Players could schedule automated, periodic releases of beneficial insects (if this becomes a consumable asset) or the automated application of organic pesticides/fungicides via integrated spraying systems within grow rooms.  
  * This capability would be unlocked alongside advanced IPM nodes in the "Cultivation" skill tree.  
  * *Considerations:* Automated spraying would necessitate specific equipment (e.g., fixed nozzle systems) and might carry inherent risks if misconfigured (e.g., spraying too close to harvest, or causing plant stress if environmental conditions during application are not optimal).  
* **Dynamic Environmental Adjustments:**  
  * Schedule gradual or stepped changes to environmental parameters like temperature or humidity to occur at specific times of day or during particular weeks of a growth stage (e.g., simulating natural diurnal temperature variations, or carefully lowering humidity during late flowering). This level of control is tied to "Precision Climate Management".  
* **Automated System Maintenance Cycles:**  
  * For hydroponic systems, schedule periodic nutrient solution flushes or automated reservoir top-offs and changes.  
* **Programming Interface:** These complex schedules would be programmed and managed through the UIs of the advanced controllers.

**D. Alerts & Notifications System**

A robust alert system is critical for informing the player of issues, especially when managing larger facilities or relying on automation during accelerated or offline time.

* **Detection & Progression:**  
  * **Early Game:** Relies heavily on direct player observation of plants and basic environmental readouts.  
  * **Mid-Late Game:** Advanced sensors can *assist* in early detection of potential problems, augmenting player skill rather than replacing it.  
* **Advanced Sensor-Based Detection Examples:**  
  * ***Simulated*** **Spore Traps / Air Particle Sensors:** A very late-game "Science" or "Advanced IPM" unlock. These sensors would not definitively identify a specific disease but could trigger an alert such as: "Warning: Unusually high airborne particulate/spore levels detected in Flower Room 3\. Microscopic inspection of plants and environment recommended."  
  * ***Simulated*** **Canopy Monitoring Sensors (e.g., advanced thermal or spectral imaging):** Extremely high-tier technology. These could detect subtle, widespread changes in leaf surface temperature or spectral reflectance that *might* indicate early, systemic stress, nutrient deficiency, or a developing disease outbreak *before* it's easily visible to the naked eye. Alert example: "Informational: Anomalous leaf surface temperature patterns detected across Bench C in Veg Room 1\. Monitor plants closely and verify nutrient delivery."  
  * **Automated EC/pH Fluctuation Alerts:** If a hydroponic reservoir's EC or pH shifts drastically and unexpectedly outside of programmed parameters, it could indicate issues like rapid nutrient depletion, salt buildup, or even early signs of root problems affecting nutrient uptake.  
* **Alert System Design:**  
  * **Tiered Alerts:** Clearly differentiate alerts by severity (e.g., Blue/Informational, Yellow/Warning, Red/Critical).  
  * **Logging:** All alerts should be logged with timestamps in the facility's event log.  
  * **Actionable Information:** Alerts should, where possible, provide context or direct the player to the relevant UI or physical location (e.g., "Critical Alert: Pump P-05 Failure in Nutrient Mixing Station. \[Go to Utility View\] \[Go to Equipment Location\]").  
  * **Customization:** Players might be able to customize notification preferences for different alert severities.

**IV. Buffers, Redundancy, and Risk Management**

These systems become paramount as players scale their operations and utilize time acceleration features, where minor issues can quickly escalate into significant crop-threatening problems.

**A. Backup Power Systems**

* **Generators:** Diesel/Gas generators are available assets.  
  * **Mechanics:** Require a consistent fuel supply. Should automatically activate upon grid power failure. Their output capacity will be limited, forcing players to make strategic decisions about which essential systems (e.g., life support in flower rooms, critical fertigation pumps) are connected to backup circuits.  
  * **Tiers:** Could include basic models (louder, less fuel-efficient, possibly requiring manual prime/start for very early game versions) and advanced models (quieter, more efficient, with automatic transfer switches).  
* **Battery Banks / Uninterruptible Power Supplies (UPS):** Listed in assets. These provide a short-term power buffer for critical systems during brief power outages or the switchover period to generator power. Essential for sensitive electronic controllers and data logging.

**B. Redundant Water Pumps**

* While not explicitly a single asset, players should be able to design redundancy into their plumbing.  
* Alternatively, a "Dual Pump Manifold" or similar could be a higher-tier, researchable piece of equipment.  
* **Mechanics:** If one pump in a redundant setup fails (equipment can have a mean time between failure, MTBF, mechanic), the secondary pump should automatically take over. The system should generate an alert flagging the initial pump failure so the player can address it.

**C. Water & Nutrient Reservoirs as Buffers**

* The strategic use of larger reservoirs for water and mixed nutrient solutions naturally creates a buffer against frequent manual refilling. This becomes especially crucial when operating under accelerated time scales, where daily consumption happens much faster in real-world time.

**D. Equipment Malfunction & Environmental Issue Alerts**

* This is a cornerstone of proactive risk management. The alert system (detailed above) must flag:  
  * Equipment Failures: Pump seizures, fan motor burnouts, light ballast/driver failures, empty CO2 tanks, critically low reservoir levels, tripped circuit breakers.  
  * Environmental Deviations: Alerts if critical environmental parameters (monitored by sensors) go significantly out of the player-defined optimal range despite automation attempts (e.g., AC unit is on, but room temperature continues to climb, indicating an undersized unit or an external issue).  
  * These alerts are vital for providing feedback on the facility's status, especially when returning after a period of offline progression.

**E. Time Acceleration "Transition Inertia" System**

* The existing "Transition Inertia" system for changing time scales is a key risk management feature. It encourages deliberate decision-making and prevents players from trivially escaping the negative consequences of poor planning or system failures when operating at high speeds.

**V. Workflow Management (Late-Game Automation)**

This category represents the zenith of "Earned Automation," allowing players to manage significantly larger and more complex facilities by automating highly repetitive, labor-intensive tasks. These systems are typically unlocked very late in the game.

**A. Automated Potting & Transplanting**

* **Equipment:** May include assets like robotic potting machines, soil/medium dispensers, and conveyor systems to move pots.  
* **Mechanics:** Players would configure these systems by defining the "recipe" (e.g., pot size, growing medium type, target plant stage). The system would require designated input zones for supplies (empty pots, bags of soil, trays of clones/seedlings) and output zones for the potted plants.  
* **Progression:** Unlocked via very high-tier nodes in the "Construction" (for the infrastructure) and "Cultivation" (for the advanced horticultural process understanding) skill trees.

**B. Automated Harvesting & Trimming**

* **Equipment:** Automated trimming machines are already listed as assets. This could be expanded with robotic arms for careful plant cutting and movement, or conveyor systems to feed plants into trimmers and move finished products to drying areas.  
* **Mechanics:** Players would designate harvest-ready plants or entire zones for automated harvesting. Trimming machines would have adjustable settings for trim aggressiveness, which could impact processing speed versus final product quality (a potential trade-off).  
* **Progression:** Unlocked via high-tier nodes in the "Harvest" skill tree, such as "Post-Harvest Efficiency (Bulk Processing & Basic Auto.)".

**C. Automated Plant Movement**

* **Equipment:** Could involve conveyor belts running between rooms, robotic platforms, or overhead gantry systems capable of picking up and moving pots or trays.  
* **Mechanics:** Primarily for efficiently moving large quantities of plants between dedicated growth stages/rooms (e.g., from a propagation area to a vegetative room, then to a flowering room, and finally to a drying room). This would require significant upfront facility design and investment.  
* **Progression:** Unlocked via very late-game "Construction" nodes like "Specialized Facility Development" or potentially a dedicated high-tier "Logistics & Material Handling" node if the system becomes very complex.

**D. Integration with Player Progression & Facility Design**

* These advanced workflow automation systems should feel like significant, aspirational achievements. They are the ultimate solutions to the "Burden of Consistency" when operating at a massive industrial scale.  
* Their implementation will often require players to design their facilities with automation in mind from the outset (e.g., specific room layouts, wide corridors for robotic movement, dedicated utility pathways).

**VI. Conclusion**

The detailed cultivation and automation systems outlined above aim to provide Project Chimera players with a deeply engaging, challenging, and rewarding experience. By emphasizing realistic environmental interactions, player-driven experimentation and discovery, and a tangible progression from laborious manual care to sophisticated, earned automation, these systems will form the backbone of the core gameplay loop. They are designed to be complex yet understandable, offering both novice and veteran simulation players a rich environment in which to learn, optimize, and ultimately master the art and science of virtual cannabis cultivation.

